#region File Header

//---------------------------------------------------------------------------------------------------
// <copyright file="Director.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun</author>
//      <description>
//          This class represents the Director which handles the loaded jobs.
//      </description>
//      <changelog>
//          <date value="12/29/2011">91955 Fix-Made change also in "EV_JOB_GetNext_JobFromQueue" </date>
//          <date value="09/04/2012">Task # 108300 Overdrive jobs notification fix</date>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.JobManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.WebOperationContextManagement;
using LexisNexis.Evolution.Overdrive.ScheduleMonitor;
using LexisNexis.Evolution.TraceServices;
using Moq;

namespace LexisNexis.Evolution.Overdrive
{
    #region Namespaces

    

    #endregion

    internal sealed class Director : IDisposable
    {
        #region Variables

        #region For timers

        private readonly TimeSpan processNewJobsTimeInterval;
        private EventWaitHandle ServiceStopCommand { get; set; }

        #endregion

        private readonly DirectorCoreServicesHost directorCoreServicesHost;
        private Dictionary<int, Type> jobTypePipelineMapping;
        internal List<ActiveJob> ActiveJobList { get; set; }

        #region For DB and API access

        private readonly DataAccess dataAccess;
        private readonly NotificationWrapper notificationAccess;

        #endregion

        #region For Debug Job

        private readonly bool runTestJob;
        private readonly string testJobType;

        #endregion

        #region For worker isolation levels

        //private readonly List<string> _workersWithIsolationLevelDefault =
        //    ConfigurationManager.AppSettings[ConfigKeyWorkerIsolationLevelDefault].Split(',').Select(p => p.Trim().ToLowerInvariant()).ToList();

        private readonly List<string> workersWithIsolationLevelAppDomain;

        private readonly List<string> workersWithIsolationLevelProcess;
        private readonly List<string> workersWithIsolationLevelThread;

        #endregion

        #region For configurations of pipeline mapping and build order

        private readonly List<JobTypePipelineMappingConfigurationItem> jobTypePipelineMappingConfig;
        private readonly List<PipelineBuildOrderConfigurationItem> pipelineBuildOrderConfig;

        #endregion

        #region Job statuses

        /// <summary>
        ///     Job Statuses.
        /// </summary>
        internal enum JobStatus
        {
            Loaded = 1,
            Running = 2,
            Stopped = 3,
            Paused = 4,
            Completed = 5,
            Failed = 6,
            Deleted = 7,
            Cancelled = 8,
            Scheduled = 9,
            CompletedWithErrors=13
        };

        #endregion

        #endregion

        #region Constants

        private const string ConfigKeyScheduleNewJobBasedOnJobId = "ScheduleNewJobBasedOnJobId";
        private const string ConfigKeyWorkerIsolationLevelThread = "WorkerIsolationLevelThread";
        private const string ConfigKeyWorkerIsolationLevelAppDomain = "WorkerIsolationLevelAppDomain";
        private const string ConfigKeyWorkerIsolationLevelProcess = "WorkerIsolationLevelProcess";
        private const string ConfigKeyJobServerIds = "JobLoaderServerIDs";
        private const string ConfigKeyJobQueuePollIntervalInMilliseconds = "JobQueuePollIntervalInMilliseconds";

        #endregion

        #region Constructor

        /// <summary>
        ///     Constructor for Director.
        /// </summary>
        private Director()
        {
            try
            {
                dataAccess = new DataAccess();
                directorCoreServicesHost = new DirectorCoreServicesHost();
                ActiveJobList = new List<ActiveJob>();
                notificationAccess = new NotificationWrapper();
                jobTypePipelineMappingConfig =
                    ConfigurationManager.GetSection("JobTypePipelineMappings") as
                        List<JobTypePipelineMappingConfigurationItem>;
                pipelineBuildOrderConfig =
                    ConfigurationManager.GetSection("PipelineBuildOrders") as List<PipelineBuildOrderConfigurationItem>;
                SetupDefaultJobTypePipelineMapping();

                int jobQueuePollIntervalInMilliseconds =
                    Convert.ToInt32(ConfigurationManager.AppSettings[ConfigKeyJobQueuePollIntervalInMilliseconds]);
                processNewJobsTimeInterval = new TimeSpan(0, 0, 0, 0, jobQueuePollIntervalInMilliseconds);

                testJobType = ConfigurationManager.AppSettings["TestJobType"];
                runTestJob = !string.IsNullOrEmpty(testJobType);

                workersWithIsolationLevelThread =
                    ConfigurationManager.AppSettings[ConfigKeyWorkerIsolationLevelThread].Split(',')
                        .Select(p => p.Trim().ToLowerInvariant())
                        .ToList();
                workersWithIsolationLevelAppDomain =
                    ConfigurationManager.AppSettings[ConfigKeyWorkerIsolationLevelAppDomain].Split(',')
                        .Select(p => p.Trim().ToLowerInvariant())
                        .ToList();
                workersWithIsolationLevelProcess =
                    ConfigurationManager.AppSettings[ConfigKeyWorkerIsolationLevelProcess].Split(',')
                        .Select(p => p.Trim().ToLowerInvariant())
                        .ToList();
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("***********Director construction failed!***********").Trace();
                throw;
            }
        }

        #endregion

        #region Director Singleton

        private static volatile Director _instance;
        private static readonly object SingletonLockObject = new Object();

        /// <summary>
        ///     Director singleton istance.
        /// </summary>
        public static Director Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SingletonLockObject)
                    {
                        if (_instance == null)
                            _instance = new Director();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Run

        private static void Main()
        {
            WindowsServiceShim.Init("Overdrive.config");
            WindowsServiceShim.PreventMultipleInstances();

            Instance.ServiceStopCommand = WindowsServiceShim.ServiceStopCommand;
            WindowsServiceShim.Run(Instance.LogicalEntryPoint);
        }

        /// <summary>
        ///     Director runner.
        /// </summary>
        public void LogicalEntryPoint()
        {
            ScheduleWarmUpJob();
            BeginRecovery();

            if (runTestJob)
            {
                RunTestJob(10);
                //RunTestJob(11);

                notificationAccess.DebugMode = runTestJob;
            }

            var stopWatch = new Stopwatch();

            while (true)
            {
                try
                {
                    stopWatch.Restart();

                    Monitor();

                    if (!runTestJob)
                    {
                        ProcessNewJobs();
                    }

                    if (null != ActiveJobList && ActiveJobList.Count > 0)
                    {
                        RefreshJobs();
                        HandlePipelineState();
                        HandleJobProgress();
                    }

                    stopWatch.Stop();
                    TimeSpan getSomeRest = TimeSpan.Zero;
                    if (stopWatch.Elapsed < processNewJobsTimeInterval)
                    {
                        getSomeRest = processNewJobsTimeInterval - stopWatch.Elapsed;
                    }

                    if (ServiceStopCommand == null)
                    {
                        Thread.Sleep(getSomeRest);
                    }
                    else
                    {
                        if (ServiceStopCommand.WaitOne(getSomeRest))
                        {
                            BeginGracefulExit();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Trace().Swallow();
                }
            }
        }

        #endregion

        #region Monitor Schedule

        private void Monitor()
        {
            // Get the jobs to be loaded from the Schedule Master.
            var jobsToLoad = JobScheduleMonitorDAO.GetJobsToLoad();

            // Return if no there are no new jobs to load.
            if (null == jobsToLoad)
            {
                return;
            }

            // Get the count of jobs to run now.
            var countOfJobsToRunNow = jobsToLoad.Count(job => (job.NextRunDate != Constants.MinDate));


            // Get the jobs which have been scheduled previously.
            if (countOfJobsToRunNow > 0)
            {
                Tracer.Trace("Found {0} new scheduled jobs to run now.", countOfJobsToRunNow);
            }

            foreach (var jobToLoad in jobsToLoad)
            {
                if (jobToLoad.NextRunDate != Constants.MinDate)
                {
                    Tracer.Info("Scheduler found job id [{0}]-{1} of type [{2}]-{3} scheduled to run at {4}.",
                        jobToLoad.JobId, jobToLoad.JobName, jobToLoad.JobTypeId, jobToLoad.JobTypeName,
                        jobToLoad.NextRunDate);

                    // Add the current job to the Load Queue.
                    var jobRunId = 0;
                    JobScheduleMonitorDAO.AddJobToLoadQueue(jobToLoad.JobId, jobToLoad.JobTypeId,
                        jobToLoad.JobRunDuration, jobToLoad.JobServerId, out jobRunId, jobToLoad.BootParameters,
                        DateTime.UtcNow);

                    Tracer.Trace(
                        "Added job [{0}]-{1} of type [{2}]-{3} to the load queue. The Job Run Id is {4}. Director will start this job shortly.",
                        jobToLoad.JobId, jobToLoad.JobName, jobToLoad.JobTypeId, jobToLoad.JobTypeName, jobRunId);
                } // End If

                // Calculate the current job's next run date.
                var nextRunDate = CalculateNextRunDateForJob(jobToLoad);

                if ((jobToLoad.ActualOccurenceCount >= jobToLoad.RequestedRecurrenceCount) &&
                    (jobToLoad.RequestedRecurrenceCount != 0)) continue;

                // Update the next run date for the current job added to Load Queue.
                JobScheduleMonitorDAO.UpdateJobNextRun(jobToLoad.JobId, nextRunDate);

                if (jobToLoad.NextRunDate != Constants.MinDate)
                {
                    Tracer.Trace("Scheduled next run for job [{0}]-{1} on {2}.", jobToLoad.JobId, jobToLoad.JobName,
                        jobToLoad.NextRunDate);
                }
            }
        }

        #endregion

        #region Calculate Next Run Date For Job

        /// <summary>
        ///     This method computes the next run date for a given job.
        /// </summary>
        /// <param name="job">Job Schedule info.</param>
        /// <returns>Next run date for a given job.</returns>
        private static DateTime CalculateNextRunDateForJob(JobSchedule job)
        {
            DateTime jobNextRunDate = job.NextRunDate;

            // Schedules a job for the specified start date.
            if (job.NextRunDate == Constants.MinDate)
            {
                jobNextRunDate = job.JobStartDate;
            }
            else if (job.Hourly > Constants.None)
            {
                // Schedules a job once in every specified number of hours from now.
                jobNextRunDate = jobNextRunDate.AddHours(Convert.ToDouble(job.Hourly));
                if (jobNextRunDate <= DateTime.UtcNow)
                {
                    jobNextRunDate = DateTime.UtcNow.AddHours(1);
                }
            }
            else if (job.Daily > Constants.None)
            {
                // Schedules a job once in every specified number of days from now.
                jobNextRunDate = jobNextRunDate.AddDays(Convert.ToDouble(job.Daily));
                if (jobNextRunDate <= DateTime.UtcNow)
                {
                    jobNextRunDate = DateTime.UtcNow.AddDays(1);
                }
            }
            else
            {
                // Schedules a job for weekly and monthly scenarios.
                if (job.ScheduleDetails.Count == 1)
                {
                    // Get the job schedule detail.
                    JobScheduleDetails jobScheduleDetails = job.ScheduleDetails[Constants.First];

                    // Process based on the Week/Month indicator set in the job schedule.
                    switch (jobScheduleDetails.WeekMonthIndicator)
                    {
                            // Handle the scenarios for weekly frequencies.
                        case Constants.Week:
                        {
                            // Schedules job every specific day of week, skipping weeks by amount specified for RepeatEvery.
                            jobNextRunDate = jobNextRunDate.AddDays(7*jobScheduleDetails.RepeatEvery);
                            if (jobNextRunDate <= DateTime.UtcNow)
                            {
                                jobNextRunDate = DateTime.UtcNow.AddDays(7*jobScheduleDetails.RepeatEvery);
                            }
                            break;
                        }

                            // Handles 2 types of scenarios for monthly frequencies.
                        case Constants.Month:
                        {
                            // Schedules job every specific date of months, skipping months by amount specified for RepeatEvery.
                            // Examples scenarios
                            // Scheduling job to run every 3rd of every month.
                            // Scheduling job to run on the 10th day once in 3 months.
                            if (jobScheduleDetails.DayDateIndicator == Constants.Date)
                            {
                                jobNextRunDate = jobNextRunDate.AddMonths(jobScheduleDetails.RepeatEvery);
                            }
                            else if (jobScheduleDetails.DayDateIndicator == Constants.Day)
                            {
                                // Schedules job every specific day of week in every month, skipping days by amount specified for RepeatEvery.
                                // Examples scenarios
                                // Scheduling job to run every first Sunday of every month.
                                // Scheduling job to run every 3rd Saturday of every month.

                                // Get the current day of the month.
                                int currentMonthDay = jobNextRunDate.Day;

                                // Get the day of week that the job is marked to run next time.
                                DayOfWeek markedDayOfWeek = jobScheduleDetails.DateValue.DayOfWeek;

                                // Get the next month.
                                int nextMonth = jobNextRunDate.Month + 1;

                                // Get the days in the next month.
                                int daysInNextMonth = DateTime.DaysInMonth(jobNextRunDate.Year, nextMonth);

                                // Set the job next run date to the first day of next month.
                                jobNextRunDate = currentMonthDay <= daysInNextMonth
                                    ? jobNextRunDate.AddMonths(1).AddDays(-currentMonthDay + 1)
                                    : jobNextRunDate.AddMonths(1).AddDays(-daysInNextMonth + 1);

                                // Skip until the day of week that the job is marked to run in the next month taking into consideration the RepeatEvery value set.
                                while (jobNextRunDate.Day <= daysInNextMonth &&
                                       jobScheduleDetails.RepeatEvery > Constants.None)
                                {
                                    jobNextRunDate = jobNextRunDate.AddDays(1);
                                    if (jobNextRunDate.DayOfWeek == markedDayOfWeek)
                                    {
                                        jobScheduleDetails.RepeatEvery--;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                else if (job.ScheduleDetails.Count > 1)
                {
                    // FOR FUTURE USE: THIS BLOCK IS RESERVED FOR MORE COMPLEX SCHEDULING SCENARIOS.
                }
                else if (job.ScheduleDetails.Count == 0)
                    //rk:in case of now, calculate the next rundate, to void extra iterations
                {
                    jobNextRunDate = jobNextRunDate.AddDays(Convert.ToDouble(job.JobRunDuration));
                }
            }

            // Return the calculated job next run date.
            return jobNextRunDate;
        }

        #endregion

        #region Run Test Job

        private void RunTestJob(int testJobRunId)
        {
            var random = new Random();
            var jobId = 100;
            var jobRunId = testJobRunId;
            var jobTypeId = int.Parse(testJobType);
            var pipelineType = jobTypePipelineMapping[jobTypeId];
            var evPipeline = CreatePipeline(pipelineType, jobRunId, jobTypeId, false) as EVPipeline;
            evPipeline.ShouldNotBe(null);
            var workRequests = GenerateWorkRequests(evPipeline, pipelineType);
            SetupWorkerIsolationLevel(workRequests);

            var baseJobBEO = new ServerStatusJobBusinessEntity
                             {
                                 JobId = jobId,
                                 JobRunId = jobRunId,
                                 BootParameters = testJobRunId.ToString(),
                                 JobScheduleRunDuration = 13,
                                 JobScheduleCreatedBy =
                                     UserBO.GetAllUsers()
                                     .First(user => user.UserId.ToLowerInvariant() == "systemadmin")
                                     .UserGUID,
                                 JobNotificationId = 13,
                                 JobFrequency = "13"
                             };

            var sb = new StringBuilder();
            var xs = new XmlSerializer(baseJobBEO.GetType());
            using (XmlWriter xwriter = XmlWriter.Create(sb))
            {
                xs.Serialize(xwriter, baseJobBEO);
                xwriter.Flush();
            }

            string bootParameters = sb.ToString();

            var newActiveJob = new ActiveJob {Beo = baseJobBEO};
            var jobInfo = new JobInfo(jobId, jobRunId, jobTypeId, workRequests, bootParameters, baseJobBEO);
            newActiveJob.JobInfo = jobInfo;
            newActiveJob.EVPipeline = evPipeline;
            RunPipeline(evPipeline);
            lock (ActiveJobList)
            {
                ActiveJobList.Add(newActiveJob);
            }
        }

        #endregion

        #region Refresh Jobs

        /// <summary>
        ///     Refresh running jobs with up-to-date issued commands.
        /// </summary>
        public void RefreshJobs()
        {
            try
            {
                if (null != dataAccess)
                {
                    #region Refresh active job list

                    if (!runTestJob)
                    {
                        ActiveJobList.ForEach(
                            activeJob =>
                            {
                                lock (activeJob)
                                {
                                    activeJob.Beo = dataAccess.GetActiveJobInfo(activeJob.Beo);
                                }
                            });
                    }

                    #endregion

                    #region Handle command change if any

                    ActiveJobList.ForEach(activeJob =>
                                          {
                                              var previousCommand = activeJob.JobInfo.Command;
                                              Command currentCommand;
                                              if (!runTestJob)
                                              {
                                                  currentCommand =
                                                      GetCommandFromIssuedCommandId(activeJob.Beo.IssuedCommandId);
                                              }
                                              else
                                              {
                                                  currentCommand = Command.Run;

                                                  //TimeSpan runningTime = DateTime.Now - _directorStartTime;
                                                  //if (runningTime.TotalSeconds > 20) {currentCommand = Command.Cancel;}
                                              }
                                              if (currentCommand != previousCommand)
                                              {
                                                  lock (activeJob)
                                                  {
                                                      activeJob.JobInfo.Command = currentCommand;
                                                  }
                                                  HandleCommandChange(activeJob);
                                              }
                                          });

                    #endregion
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Director failed to refresh running jobs.").Trace().Swallow();
            }
        }

        #endregion

        #region Private Methods

        #region Schedule Warm Up Job

        private void ScheduleWarmUpJob()
        {
            try
            {
                var oldJobId = ConfigurationManager.AppSettings[ConfigKeyScheduleNewJobBasedOnJobId];
                int jobId;
                if (string.IsNullOrEmpty(oldJobId) || !int.TryParse(oldJobId, out jobId))
                {
                    return;
                }

                var jobDetails = JobMgmtBO.GetJobDetails(oldJobId.ToString(CultureInfo.InvariantCulture));

                //var stream = new StringReader(jobDetails.BootParameters);
                //var xmlStream = new XmlSerializer(typeof(LawImportBEO));
                //var jobParamObject = (LawImportBEO)xmlStream.Deserialize(stream);

                #region Get User Guid

                var userGuid = string.Empty;
                try
                {
                    var userName = jobDetails.CreatedBy;
                    var userBusinessEntity = UserBO.GetUser(userName);
                    userGuid = userBusinessEntity.UserGUID;
                }
                catch (Exception ex)
                {
                    Tracer.Error(
                        "Unable to get User GUID. Ensure User GUID can be obtained as it is required for auditing and notifications. Exception details: {0}",
                        ex.ToDebugString());
                }

                #endregion

                var jobBusinessEntity = new JobBusinessEntity
                                        {
                                            Name = "DebugJob-" + DateTime.Now.Ticks,
                                            FolderID = jobDetails.FolderID,
                                            Description = "This is a debug job created automatically",
                                            BootParameters = jobDetails.BootParameters,
                                            Type = jobDetails.Type,
                                            NotificationId = 0,
                                            CreatedById = userGuid,
                                            Priority = 1,
                                            JobFrequency = jobDetails.JobFrequency,
                                            JobScheduleType = BaseJobBEO.ScheduleType.Now,
                                            Visibility = true,
                                            NotificationRequired = false,
                                            JobScheduleStartDate = DateTime.UtcNow,
                                            JobScheduleEndDate = DateTime.UtcNow.AddDays(10),
                                            JobScheduleRequestedRecurrenceCount = 1,
                                        };
                var strServerIdOverride = GetServerIdOverride();
                JobMgmtDAO.CreateNewJob(jobBusinessEntity, strServerIdOverride);
                Tracer.Info("Scheduled warm up debug job.");
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Scheduling debug job failed!").Trace().Swallow();
            }
        }

        #endregion

        #region Get Server Id Override

        private static string GetServerIdOverride()
        {
            ConfigurationManager.RefreshSection("WindowsUserNameToServerIdOverride");
            var WindowsUserNameToServerIdOverride =
                (Hashtable) ConfigurationManager.GetSection("WindowsUserNameToServerIdOverride");
            var objServerIdOverride = (null != WindowsUserNameToServerIdOverride)
                ? WindowsUserNameToServerIdOverride[Environment.UserName]
                : null;
            var strServerIdOverride = (null != objServerIdOverride) ? objServerIdOverride.ToString() : null;
            return strServerIdOverride;
        }

        #endregion

        #region Begin Recovery

        private void BeginRecovery()
        {
            try
            {
                var directorFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var fullPathToRecover = Path.Combine(directorFolder, "recover.bin");
                //Tracer.Trace("Director: Full path to recover.bin is: {0}", fullPathToRecover);

                if (File.Exists(fullPathToRecover))
                {
                    //Tracer.Info("Found jobs to be recovered.");
                    //using (var stream = new FileStream(fullPathToRecover, FileMode.Open, FileAccess.Read, FileShare.Read))
                    //{
                    //    ActiveJobList = (List<ActiveJob>)new BinaryFormatter().Deserialize(stream);
                    //    stream.Close();
                    //}
                    //File.Delete(fullPathToRecover);
                    //if (ActiveJobList.Count > 0)
                    //{
                    //    ActiveJobList.ForEach((activeJob) => ResumeJob(activeJob.Beo.JobRunId));
                    //}
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Recovery of persisted active jobs failed.").Trace().Swallow();
            }
        }

        #endregion

        #region Process New Jobs

        /// <summary>
        ///     Processes the new jobs.
        /// </summary>
        /// <remarks></remarks>
        private void ProcessNewJobs()
        {
            try
            {
                var strServerIdOverride = GetServerIdOverride();
                foreach (var item in jobTypePipelineMapping)
                {
                    var strServerId = strServerIdOverride ??
                                      ApplicationConfigurationManager.GetValue(item.Key.ToString(),
                                          ConfigKeyJobServerIds, false);
                    if (null == strServerId)
                    {
                        continue;
                            // Here we are skipping debug jobs for which we generally don't have server ID configured
                    }
                    var serverId = new Guid(strServerId);
                    var jobFromQueue = dataAccess.GetNextJobFromQueue(item.Key, serverId);
                    var pipelineTypeToInstantiate = item.Value;
                    if (null != jobFromQueue && !ActiveJobList.Any(j => j.PipelineId.Equals(jobFromQueue.JobRunId)))
                    {
                        ProcessNewJob(pipelineTypeToInstantiate, jobFromQueue);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to retrieve and process new jobs.").Trace().Swallow();
            }
        }

        #region Process New Job

        private void ProcessNewJob(Type pipelineTypeToInstantiate, BaseJobBEO jobFromQueue)
        {
            try
            {
                #region Initialize local variables

                var newActiveJob = new ActiveJob {Beo = jobFromQueue}; // Initialize new active job variable.
                var jobId = jobFromQueue.JobId; // Job Id.
                var jobRunId = jobFromQueue.JobRunId; // Job Run Id.
                var jobTypeId = jobFromQueue.JobTypeId; // Job Type Id.
                var jobRecurrenceType = jobFromQueue.JobFrequency;
                    // Job Frequency. This is actually Recurrence Type from EV_JOB_JobMaster table.
                var jobScheduleRunDuration = jobFromQueue.JobScheduleRunDuration;
                    // Schedule Run Duration. From EV_JOB_JobMaster table.
                var jobNotificationId = jobFromQueue.JobNotificationId; // Notification Id. From EV_JOB_JobMaster table.
                var bootParameters = jobFromQueue.BootParameters; // Boot Parameters.
                var isOverlay = IsOverlayJob(jobTypeId, bootParameters);
                Tracer.Debug("Director found new job: JobName = {0}, JobTypeName = {1}, JobId = {2}, JobRunId = {3}",
                    jobFromQueue.JobName, jobFromQueue.JobTypeName, jobId, jobRunId);

                #endregion

                var pipeline = CreatePipeline(pipelineTypeToInstantiate, jobRunId, jobTypeId, isOverlay);
                pipeline.ShouldNotBe(null);
                pipeline.ShouldBeTypeOf<EVPipeline>();
                var evPipeline = pipeline as EVPipeline;
                if (null != evPipeline)
                {
                    evPipeline.SetGeneralProperties(jobId, jobTypeId);
                    var workRequests = GenerateWorkRequests(evPipeline, pipelineTypeToInstantiate);

                    if (null != workRequests && workRequests.Count > 0)
                    {
                        SetupWorkerIsolationLevel(workRequests);

                        #region Get User Guid

                        var jobDetails = JobMgmtBO.GetJobDetails(jobId.ToString(CultureInfo.InvariantCulture));
                        var userGuid = string.Empty;
                        try
                        {
                            var userName = jobDetails.CreatedBy;
                            var userBusinessEntity = UserBO.GetUser(userName);
                            userGuid = userBusinessEntity.UserGUID;
                        }
                        catch (Exception ex)
                        {
                            ex.AddDbgMsg(
                                "Unable to get User GUID. Ensure User GUID can be obtained as it is required for auditing and notifications.")
                                .Trace()
                                .Swallow();
                        }

                        #endregion

                        #region Get Matter Id

                        try
                        {
                            newActiveJob.MatterId = 0;
                            // Skip for "System Folder".
                            if (jobDetails.FolderID != 1)
                            {
                                newActiveJob.MatterId =
                                    DataSetBO.GetDataSetDetailForDataSetId(jobDetails.FolderID).Matter.FolderID;
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.AddDbgMsg("jobDetails.FolderID = {0}", jobDetails.FolderID).Trace().Swallow();
                            Tracer.Error(
                                "Matter Id cannot be obtained for this type of job. Setting it to 0, so that the job can internally handle it.");
                            // Matter Id in Director is primarily required for Overdrive Jobs - Imports, Export, Production, Reviewset
                            // Matter Id is required for audit log purposes, so that Director can use it for audit logging.
                            // Matter Id cannot be obtained in some scenarios (ex: Refresh Reports), those will be for Classic jobs.
                            // For such classic jobs, if Matter Id is required, it will be obtained internally. So we set Matter Id as 0 here.
                        }

                        #endregion

                        EstablishSession(userGuid);

                        #region Handle special cases for Import Jobs

                        // Clustering job is not in scope.
                        // Commenting out just in case it is required in the future.
                        //if (isImportJob)
                        //{
                        //    CancelClusterJob(jobId, jobDetails.FolderID.ToString(CultureInfo.InvariantCulture), userGuid);
                        //    UpdateClusterStatus(jobFromQueue);
                        //}

                        #endregion

                        newActiveJob.EVPipeline = evPipeline;
                        //Tracer.Trace("Added pipeline {0} to in-memory reference list.", evPipeline.PipelineId);

                        var jobInfo = new JobInfo(jobId, jobRunId, jobTypeId, workRequests, bootParameters,
                            newActiveJob.Beo)
                                      {
                                          Frequency = jobRecurrenceType,
                                          ScheduleRunDuration = Convert.ToInt32(jobScheduleRunDuration),
                                          NotificationId = jobNotificationId,
                                          ScheduleCreatedBy = userGuid
                                      };

                        newActiveJob.JobInfo = jobInfo;
                        //Tracer.Trace("Added job id: {0}, job run id: {1} to in-memory reference list.", jobId, jobRunId);

                        evPipeline.SetPipelineTypeSpecificParameters(newActiveJob);

                        RunPipeline(evPipeline);

                        lock (ActiveJobList)
                        {
                            ActiveJobList.Add(newActiveJob);
                        }

                        if (null != dataAccess)
                        {
                            dataAccess.UpdateJobStatus(jobId, (int) JobStatus.Running);
                            var issuedCommand = 0;
                            dataAccess.UpdateJobExecutionStatus(jobRunId, (int) JobStatus.Running, out issuedCommand);
                            Tracer.Trace("Updated job id: {0} status to running.", jobId);
                        }
                        Tracer.Info(
                            "Director started job: JobName = {0}, JobTypeName = {1}, JobId = {2}, JobRunId = {3}, with {4} work requests.",
                            jobFromQueue.JobName, jobFromQueue.JobTypeName, jobId, jobRunId, workRequests.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to process new job. Id: {0}, Run Id: {1}", jobFromQueue.JobId,
                    jobFromQueue.JobRunId).Trace().Swallow();
            }
        }

        #endregion

        #endregion

        #region Generate Work Requests

        private List<WorkRequest> GenerateWorkRequests(Pipeline pipeline, Type pipelineTypeToInstantiate)
        {
            try
            {
                //Tracer.Trace("Created Pipeline Id: {0} of type: {1}", pipeline.PipelineId, pipeline.PipelineType);
                var workRequests = pipeline.GenerateWorkRequests();
                //Tracer.Trace("Pipeline {0} has {1} work requests.", pipeline.PipelineId, workRequests.Count);
                return workRequests;
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to generate work requests for job run id: {0}", pipeline.PipelineId)
                    .Trace()
                    .Swallow();
                return null;
            }
        }

        #endregion

        #region Run Pipeline

        private void RunPipeline(Pipeline pipeline)
        {
            try
            {
                var pipelineRunThread = new Thread(pipeline.Run) {Name = "PipelineId = " + pipeline.PipelineId};
                pipelineRunThread.Start();
                Tracer.Trace("Started pipeline {0}", pipeline.PipelineId);
            }
            catch (Exception ex)
            {
                Tracer.Error("Unable to start pipeline id: {0}. Exception details: {1}", pipeline.PipelineId,
                    ex.ToDebugString());
            }
        }

        #endregion

        #region Setup Worker Isolation Level

        private void SetupWorkerIsolationLevel(List<WorkRequest> workRequests)
        {
            if (null != workRequests && workRequests.Count > 0)
            {
                var pipelineId = workRequests[0].PipelineId;
                workRequests.ForEach(wr => wr.WorkerIsolationLevel = WorkerIsolationLevel.Default);
                try
                {
                    foreach (var workRequest in workRequests)
                    {
                        var roleType = workRequest.RoleType.ToString().ToLowerInvariant();
                        if (workersWithIsolationLevelThread.Contains(roleType))
                        {
                            workRequest.WorkerIsolationLevel = WorkerIsolationLevel.SeparateThread;
                            continue;
                        }
                        if (workersWithIsolationLevelAppDomain.Contains(roleType))
                        {
                            workRequest.WorkerIsolationLevel = WorkerIsolationLevel.SeparateAppDomain;
                            continue;
                        }
                        if (workersWithIsolationLevelProcess.Contains(roleType))
                        {
                            workRequest.WorkerIsolationLevel = WorkerIsolationLevel.SeparateProcess;
                        }
                    }
                }
                catch
                {
                    Tracer.Error(
                        "Unable to set worker isolation level correctly. Set all workers to Default worker isolation level for Pipeline Id: {0}.",
                        pipelineId);
                }
            }
        }

        #endregion

        #region Handle Command Change

        private void HandleCommandChange(ActiveJob activeJob)
        {
            try
            {
                var jobInfo = activeJob.JobInfo;
                var command = activeJob.JobInfo.Command;
                var jobId = jobInfo.JobId;
                if (null != dataAccess && null != notificationAccess)
                {
                    #region Handle new command

                    Tracer.Info("Handling {0} command for Job Id: {1}, pipelineId: {2}", command.ToString(), jobId,
                        activeJob.EVPipeline.PipelineId);
                    switch (command)
                    {
                        case Command.Pause:

                            #region Update database and send notifications

                            dataAccess.UpdateJobStatus(jobId, (int) JobStatus.Paused);
                            Tracer.Info("Updated status for Job Id: {0} to paused state.", jobId);
                            activeJob.BusinessEntity = dataAccess.GetJobSubScriptionDetails(jobId);
                            notificationAccess.SendNotifications(activeJob, JobStatus.Paused, "Paused successfully.");
                            Tracer.Info("Sent notifications that Job Id: {0} has been paused successfully.", jobId);

                            #endregion

                            break;

                        case Command.Cancel:
                            lock (activeJob.EVPipeline)
                            {
                                activeJob.EVPipeline.PipelineStatus.PipelineState = PipelineState.Cancelled;
                            }
                            // Next call to HandlePipelineState will do all the necessary work
                            // Due to the State > Completed Pipeline is excluded from the running jobs list 
                            // When pipeline is free of workers it will be deleted
                            break;

                        case Command.Run:

                            #region Update database

                            dataAccess.UpdateJobStatus(jobId, (int) JobStatus.Running);
                            Tracer.Info("Updated status for Job Id: {0} to running state.", jobId);

                            #endregion

                            break;
                    }

                    #endregion
                }
            }
            catch
            {
                if (null != activeJob)
                    Tracer.Error("Unable to handle change in command for Job Id: {0}", activeJob.JobId);
            }
        }

        #endregion

        #region Handle Job Progress

        private void HandleJobProgress()
        {
            try
            {
                PipelineSection lastPipelineSection = null;
                PipelineSection firstPipelineSection = null;
                foreach (var activeJob in ActiveJobList.Where(j => j.IsFirstWorkerComplete))
                {
                    lock (activeJob.EVPipeline)
                    {
                        #region Identify first and last pipeline sections based on job type

                        switch (activeJob.JobTypeId)
                        {
                            case 2:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "DcbSlicer");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "Conversion");
                                break;
                            case 8:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "EDocsFileParser");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "Conversion");
                                break;
                            case 14:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "LoadFileParser");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "Conversion");
                                break;
                            case 9:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "ProductionStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "ProductionVaultIndexingUpdate");
                                break;
                            case 23:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "ExportStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "ExportLoadFileWriter");
                                break;
                            case 17:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "ReviewsetStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "IndexUpdate");
                                break;
                            case 27:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "PrintStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "PrintValidation");
                                break;
                            case 35:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "LawStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "Conversion");
                                break;
                            case 37:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "NearDuplicationStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "NearDuplicationEVUpdate");

                                break;
                            case 51:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "IncludeDocumentsStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "IncludeDocumentsUpdate");
                                break;
                            case 52:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "IncludeSubSystemsStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "IncludeSubSystemsFinal");
                                break;
                            case 56:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "CategorizeProjectDocuments");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "CategorizeUpdateFields");
                                break;
                            }

                        #endregion
                    }

                    #region Calculate progress percentage

                    if (null != firstPipelineSection && null != lastPipelineSection)
                    {
                        lock (activeJob)
                        {
                            activeJob.TotalNumberOfDocumentsToProcess =
                                firstPipelineSection.GetNumberOfProcessedDocuments(null);
                            activeJob.NumberOfDocumentsProcessed =
                                lastPipelineSection.GetNumberOfProcessedDocuments(null);
                        }
                    }
                    UpdateProgress(activeJob);

                    #endregion
                }

                var sJobs = new List<int> {42, 45, 46,48, 55, 57,59};
                var singleWorkerJobs = ActiveJobList.FindAll(j => sJobs.Contains(j.JobTypeId));

                foreach (var activeSingleWokerJob in singleWorkerJobs)
                {
                    lock (activeSingleWokerJob.EVPipeline)
                    {
                        switch (activeSingleWokerJob.JobTypeId)
                        {
                            case 42:
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "AnalyticsProject");
                                break;
                            case 45: //Create Control set
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "ControlSet");
                                break;
                            case 48: //Create Qc set
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "QcSet");
                                break;
                            case 46: //Create Training set
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "CreateTrainingset");
                                break;
                            case 55: //Categorize Control set
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "CategorizeControlSet");
                                break;
                            case 57: //Categorize Analysis set
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "CategorizeAnalysisset");
                                break;
                            case 59: //Train Model
                                firstPipelineSection =
                                    activeSingleWokerJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection => pipelineSection.RoleType.ToString() == "TrainModel");
                                break;
                            
                        }
                        firstPipelineSection.ShouldNotBe(null);
                        activeSingleWokerJob.TotalNumberOfDocumentsToProcess =
                            firstPipelineSection.GetTotalNoOfDocuments(null);
                        activeSingleWokerJob.NumberOfDocumentsProcessed =
                            firstPipelineSection.GetNumberOfProcessedDocuments(null);
                    }
                    UpdateProgress(activeSingleWokerJob);
                }

                UpdateJobProgressForJobsWithPreCalculatedTotalMessageCount();
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to compute progress.").Trace().Swallow();
            }
        }

        /// <summary>
        /// Update job progress for job have pre calculated total message count, 
        /// Total message count (processing document count) will be pre calculated in 'Begin work' method on 'Startup Worker'.
        /// So no need to wait for Startup worker to complete for calculate progress percentage
        /// </summary>
        private void UpdateJobProgressForJobsWithPreCalculatedTotalMessageCount()
        {
            try
            {
                PipelineSection lastPipelineSection = null;
                PipelineSection firstPipelineSection = null;
                var jobs = new List<int> {51, 52}; 
                foreach (
                    var activeJob in ActiveJobList.FindAll(j => jobs.Contains(j.JobTypeId)))
                {
                    lock (activeJob.EVPipeline)
                    {
                        switch (activeJob.JobTypeId)
                        {
                            case 51:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "IncludeDocumentsStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "IncludeDocumentsUpdate");
                                break;
                            case 52:
                                firstPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "IncludeSubSystemsStartup");
                                lastPipelineSection =
                                    activeJob.EVPipeline.PipelineSections.FirstOrDefault(
                                        pipelineSection =>
                                            pipelineSection.RoleType.ToString() == "IncludeSubSystemsFinal");
                                break;
                        }
                    }

                    if (null != firstPipelineSection && null != lastPipelineSection)
                    {
                        lock (activeJob)
                        {
                            activeJob.TotalNumberOfDocumentsToProcess =
                                firstPipelineSection.GetNumberOfProcessedDocuments(null);
                            activeJob.NumberOfDocumentsProcessed =
                                lastPipelineSection.GetNumberOfProcessedDocuments(null);
                        }
                    }
                    UpdateProgress(activeJob);
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to compute progress for jobs have pre calculated total message count.").Trace().Swallow();
            }
        }

        /// <summary>
        ///     Updates the progress.
        /// </summary>
        /// <param name="activeJob">The active job.</param>
        private void UpdateProgress(ActiveJob activeJob)
        {
            if (activeJob.TotalNumberOfDocumentsToProcess > 0)
            {
                var progressPercentage = (Convert.ToDouble(activeJob.NumberOfDocumentsProcessed)/
                                          Convert.ToDouble(activeJob.TotalNumberOfDocumentsToProcess))*100;
                // activeJob.ProgressPercentage = progressPercentage <= 100 ? progressPercentage : 100.0;
                activeJob.ProgressPercentage = progressPercentage < 100 ? progressPercentage : 99.0;

                /*100.0 means complete state, after process all batches (i.e. messages) still have pending work for complete state .
                        So we moved to 99% here, once remaining completed job status moved to “Completed” state */
                dataAccess.UpdateProgressPercentage(activeJob.PipelineId, activeJob.ProgressPercentage);
            }
        }

        #endregion

        #region Handle Pipeline State

        private void HandlePipelineState()
        {
            try
            {
                var newActiveJobList = new List<ActiveJob>();
                foreach (var activeJob in ActiveJobList)
                {
                    var pipeline = activeJob.EVPipeline;
                    lock (pipeline)
                    {
                        activeJob.CurrentPipelineState = pipeline.PipelineStatus.PipelineState;
                        if (activeJob.CurrentPipelineState != activeJob.PreviousPipelineState)
                        {
                            HandlePipelineStateChange(activeJob);
                            activeJob.PreviousPipelineState = activeJob.CurrentPipelineState;
                        }

                        // Handle possible messages from workers
                        foreach (var workerMessage in activeJob.EVPipeline.PipelineStatus.WorkerMessages)
                        {
                            Tracer.Info("Message from worker {0}", workerMessage.ToString());
                        }
                        pipeline.PipelineStatus.WorkerMessages.Clear();

                        if (activeJob.CurrentPipelineState == PipelineState.AllWorkersQuit)
                        {
                            pipeline.Delete();
                            Tracer.Info("Disposed pipeline id: {0} for Job Id: {1}", pipeline.PipelineId,
                                activeJob.JobId);
                        }
                        else
                        {
                            newActiveJobList.Add(activeJob);
                        }
                    } // lock
                } // foreach
                ActiveJobList = newActiveJobList;
            }
            catch (Exception ex)
            {
                ex.Trace();
                // Caller will handle this exception gracefully.
                throw;
            }
        }

        #endregion

        #region Handle Pipeline State Change

        private void HandlePipelineStateChange(ActiveJob activeJob)
        {
            if (null == activeJob)
            {
                Debug.Assert(null != activeJob);
                return;
            }

            // Debug
            //Tracer.Warning("HandlePipelineStateChange: {0}", activeJob.CurrentPipelineState);

            UpdateFinalStatusInDatabase(activeJob);
            SendFinalNotification(activeJob);
        }

        #endregion

        #region Update job status in database, send notifications and insert audit record

        private void UpdateFinalStatusInDatabase(ActiveJob activeJob)
        {
            switch (activeJob.CurrentPipelineState)
            {
                case PipelineState.Completed:
                    dataAccess.UpdateProgressPercentage(activeJob.PipelineId, 100);
                    var jobStatusId = IsJobCompletedWithFailures(activeJob) ?
                        (int)JobStatus.CompletedWithErrors : 
                        (int)JobStatus.Completed;
                    dataAccess.UpdateJobFinalStatus(activeJob.JobId, int.Parse(activeJob.JobInfo.PipelineId),
                        jobStatusId);
                    Tracer.Info("Updated job id: {0} status to {1} in database.", activeJob.JobId,
                        activeJob.CurrentPipelineState.ToString());
                    break;

                case PipelineState.Cancelled:
                    dataAccess.UpdateJobFinalStatus(activeJob.JobId, int.Parse(activeJob.JobInfo.PipelineId),
                        (int) JobStatus.Cancelled);
                    Tracer.Info("Updated job id: {0} status to {1} in database.", activeJob.JobId,
                        activeJob.CurrentPipelineState.ToString());
                    break;

                case PipelineState.ProblemReported:
                    // No need to duplicate problem reports tracing here - it is already done by the pipeline itself.
                    //var report = new StringBuilder(String.Format("Found problem report(s) for pipeline id: {0}.", activeJob.EVPipeline.PipelineId));
                    //List<ProblemReport> problemReports = activeJob.EVPipeline.PipelineStatus.ProblemReports;
                    //for (int problemReportNumber = 0; problemReportNumber < problemReports.Count; problemReportNumber++)
                    //{
                    //    report.AppendLine();
                    //    report.AppendLine("Problem Report #" + problemReportNumber + ": ");
                    //    ProblemReport problemReport = problemReports[problemReportNumber];
                    //    report.Append(problemReport.ToString());
                    //}
                    //Tracer.Error(report.ToString());
                    dataAccess.UpdateJobFinalStatus(activeJob.JobId, int.Parse(activeJob.JobInfo.PipelineId),
                        (int) JobStatus.Failed);
                    Tracer.Info("Updated job id: {0} status to {1} in database.", activeJob.JobId,
                        activeJob.CurrentPipelineState.ToString());
                    break;
            }
        }

        private bool IsJobCompletedWithFailures(ActiveJob activeJob)
        {
            try
            {
                var sJobs = new List<int> { 45, 48, 51, 52, 53, 54, 55, 56, 57,59,46};
                if (sJobs.Contains(activeJob.JobTypeId))
                {
                    var jobLog = JobMgmtBO.GetJobLogSummary(activeJob.JobId.ToString(CultureInfo.InvariantCulture));
                    if (jobLog != null && (jobLog.FailDocumentsCount > 0 || jobLog.SuccessDocumentCount == 0))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                Tracer.Error("Unable to check job is completed with failures or not.");
                return false;
            }
        }

        private void SendFinalNotification(ActiveJob activeJob)
        {
            switch (activeJob.CurrentPipelineState)
            {
                case PipelineState.Completed:
                    activeJob.BusinessEntity = dataAccess.GetJobSubScriptionDetails(activeJob.JobId);
                    var message = IsJobCompletedWithFailures(activeJob) ?
                        "Job completed with errors." :
                        "Job completed successfully.";
                    notificationAccess.SendNotifications(activeJob, JobStatus.Completed,
                       message);
                    Tracer.Info("Sent notification for Job Id: {0}.", activeJob.JobId);
                    break;

                case PipelineState.Cancelled:
                    activeJob.BusinessEntity = dataAccess.GetJobSubScriptionDetails(activeJob.JobId);
                    notificationAccess.SendNotifications(activeJob, JobStatus.Cancelled, "Job cancelled.");
                    Tracer.Info("Sent notification for Job Id: {0}.", activeJob.JobId);
                    break;

                case PipelineState.ProblemReported:
                    activeJob.BusinessEntity = dataAccess.GetJobSubScriptionDetails(activeJob.JobId);
                    notificationAccess.SendNotifications(activeJob, JobStatus.Failed, "Job failed.");
                    Tracer.Info("Sent notification for Job Id: {0}.", activeJob.JobId);
                    break;
            }
        }

        #endregion

        #region Get Command from Issued Command Id

        /// <summary>
        ///     Gets the command from issued command id.
        /// </summary>
        /// <param name="issuedCommandId">The issued command id.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private Command GetCommandFromIssuedCommandId(int issuedCommandId)
        {
            switch (issuedCommandId)
            {
                case 2:
                    return Command.Run;
                case 3:
                    return Command.Cancel;
                case 4:
                    return Command.Pause;
            }
            return Command.Run;
        }

        #endregion

        #region Is Overlay Job

        private bool IsOverlayJob(int jobTypeId, string bootParameters)
        {
            try
            {
                bool isOverlay;
                switch (jobTypeId)
                {
                    case 2: // DCB Import
                        isOverlay = !(Utils.SmartXmlDeserializer(bootParameters) as ProfileBEO).IsAppend;
                        break;
                    case 14: // Load File Import
                        isOverlay = !(Utils.SmartXmlDeserializer(bootParameters) as ImportBEO).IsAppend;
                        break;
                    default:
                        isOverlay = false;
                        break;
                }
                return isOverlay;
            }
            catch
            {
                Tracer.Error("Unable to determine if job is append or overlay.");
                return false;
            }
        }

        #endregion

        #region Create Pipeline

        /// <summary>
        ///     Creates the pipeline.
        /// </summary>
        /// <param name="pipelineTypeToInstantiate">The pipeline type to instantiate.</param>
        /// <returns>An instance of the pipeline.</returns>
        /// <remarks></remarks>
        private Pipeline CreatePipeline(Type pipelineTypeToInstantiate, int jobRunId, int jobTypeId, bool isOverlayJob)
        {
            try
            {
                var pipeline = (Pipeline) Activator.CreateInstance(pipelineTypeToInstantiate);
                var jobTypePipelineMappingConfig =
                    this.jobTypePipelineMappingConfig.FirstOrDefault(x => x.JobTypeId == jobTypeId);

                PipelineType pipelineType = jobTypePipelineMappingConfig.PipelineType;

                var pipelineTypePostFix = string.Empty;
                if (pipelineType.ToString() == "ImportLoadFile")
                {
                    pipelineTypePostFix = isOverlayJob ? "Overlay" : "Append";
                }
                pipeline.Build(CreatePipelineBuildOrder(jobRunId, pipelineType, pipelineType + pipelineTypePostFix));
                return pipeline;
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to create pipeline for Job Run Id: {0}.", jobRunId).Trace().Swallow();
                return null; // Calling method handles this condition gracefully.
            }
        }

        #endregion

        #region Create Pipeline Build Order

        private PipelineBuildOrder CreatePipelineBuildOrder(int pipelineId, PipelineType pipelineType,
            string pipelineTypeToQueryBuildOrderConfig)
        {
            PipelineBuildOrder pipelineBuildOrder = null;
            try
            {
                var pipelineBuildOrderConfig =
                    this.pipelineBuildOrderConfig.FirstOrDefault(
                        x => x.PipelineType.Equals(pipelineTypeToQueryBuildOrderConfig));

                var rolePlans = new List<RolePlan>();
                pipelineBuildOrderConfig.RolePlans.ForEach(
                    rolePlan =>
                    {
                        rolePlans.Add(new RolePlan(rolePlan.RoleType, rolePlan.Name, rolePlan.DesiredInstance,
                            rolePlan.OutputSectionsNames));
                    });

                pipelineBuildOrder = new PipelineBuildOrder
                                     {
                                         PipelineId = pipelineId,
                                         PipelineType = pipelineType,
                                         RolePlans = rolePlans
                                     };
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Unable to create pipeline build order for Pipeline Id: {0} and Pipeline Type: {1}.",
                    pipelineId, pipelineType);
                ex.Trace().Swallow();
            }
            return pipelineBuildOrder;
        }

        #endregion

        #region Setup Default Job Type and Pipeline Mapping

        /// <summary>
        ///     Setups the default job type pipeline mapping.
        /// </summary>
        /// <remarks></remarks>
        private void SetupDefaultJobTypePipelineMapping()
        {
            try
            {
                jobTypePipelineMapping = new Dictionary<int, Type>();
                jobTypePipelineMappingConfig.ForEach(
                    item => jobTypePipelineMapping.Add(item.JobTypeId, item.PipelineClassToInstantiate));
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to establish Job Type Pipeline mapping.").Trace().Swallow();
            }
        }

        #endregion

        #region Begin Graceful Exit

        private void BeginGracefulExit()
        {
            Tracer.Info("Director entering into graceful exit mode.");
            if (null != ActiveJobList)
            {
                Tracer.Info("Director found {0} active (running) jobs at {1}.", ActiveJobList.Count, DateTime.Now);
                if (ActiveJobList.Count > 0)
                {
                    PersistActiveJobs();
                }
            }
            Dispose();
            Tracer.Info("Director successfully completed graceful exit.");
        }

        #endregion

        #region Persist Active Jobs

        private void PersistActiveJobs()
        {
            try
            {
                const string fileName = "recover.bin";
                var directorFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (null != directorFolder)
                {
                    var fullPathToRecovery = Path.Combine(directorFolder, fileName);
                    //Tracer.Trace("Director: Full path to recover.bin is: {0}", fullPathToRecovery);
                    if (null != ActiveJobList && ActiveJobList.Count > 0)
                    {
                        var jobRunIds = new StringBuilder();
                        ActiveJobList.ForEach(activeJob =>
                                              {
                                                  jobRunIds.Append(activeJob.PipelineId);
                                                  jobRunIds.AppendLine();
                                                  dataAccess.UpdateJobStatus(activeJob.JobId, (int) JobStatus.Paused);
                                                  PauseJob(activeJob.Beo.JobRunId);
                                              });
                        if (jobRunIds.Length > 0)
                        {
                            File.WriteAllText(fullPathToRecovery, jobRunIds.ToString());
                            Tracer.Info("Director paused {0} active jobs.", ActiveJobList.Count);
                            Tracer.Info("Persisted active jobs.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Persisting active jobs failed.").Trace().Swallow();
            }
        }

        #endregion

        #region Pause Job

        private void PauseJob(int jobRunId)
        {
            try
            {
                if (dataAccess != null)
                    dataAccess.UpdateJobCommand(jobRunId, 4);
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to pause Job Run Id: {0}.", jobRunId).Trace().Swallow();
            }
        }

        #endregion

        #region Resume Job

        private void ResumeJob(int jobRunId)
        {
            try
            {
                if (dataAccess != null)
                    dataAccess.UpdateJobCommand(jobRunId, 5);
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to resume Job Run Id: {0}.", jobRunId).Trace().Swallow();
            }
        }

        #endregion

        #endregion

        #region MockHttpCurrentContext

        /// <summary>
        ///     mocks the HTTP current context
        /// </summary>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        private IWebOperationContext EstablishSession(string createdBy)
        {
            var userProp = new UserBusinessEntity {UserGUID = createdBy};
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            userProp = UserBO.AuthenticateUsingUserGuid(createdBy);
            var userSession = new UserSessionBEO();
            SetUserSession(createdBy, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
            return new MockWebOperationContext().Object;
        }

        private void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Tracer.Info("Director starting clean up activities.");

            if (null != directorCoreServicesHost)
            {
                directorCoreServicesHost.Dispose();
            }

            if (null != dataAccess)
            {
                dataAccess.Dispose();
            }

            if (null != notificationAccess)
            {
                notificationAccess.Dispose();
            }
            Tracer.Info("Director successfully Disposed.");
        }

        #endregion
    }
}