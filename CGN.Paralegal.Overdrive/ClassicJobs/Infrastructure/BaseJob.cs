//-----------------------------------------------------------------------------------------
// <copyright file="BaseJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the BaseJob class for any type of job.
//      </description>
//      <changelog>
//          <date value="28-March"></date>
//          <date value="07/27/2011">Fix for Bug# 88625.</date>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//          <date value="03/26/2012">Dataset delete job issue fixed</date>
//          <date value="05/11/2012">Fix for bug 100606</date>
//          <date value="05/28/2012">Fix for bug 100606</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.NotificationManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.WebOperationContextManagement;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.ServiceImplementation.DatasetManagement;
using LexisNexis.Evolution.ServiceImplementation.Document;
using LexisNexis.Evolution.ServiceImplementation.JobMgmt;
using LexisNexis.Evolution.ServiceImplementation.MatterManagement;
using LexisNexis.Evolution.ServiceImplementation.PrinterManagement;
using LexisNexis.Evolution.ServiceImplementation.ReviewerSearch;
using LexisNexis.Evolution.ServiceImplementation.ReviewSet;
using LexisNexis.Evolution.ServiceImplementation.UserManagement;
using Moq;

namespace LexisNexis.Evolution.Infrastructure.Jobs
{

    #region Namespaces

    #endregion

    /// <summary>
    ///     This is the Base class for any type of job.
    /// </summary>
    /// <typeparam name="JobParametersType">The business entity type for a job.</typeparam>
    /// <typeparam name="TaskType">The task business entity type for a job.</typeparam>
    /// <remarks>
    ///     All job classes MUST inherit from this BaseJob class.
    ///     All virtual methods may be overriden if required to meet specific needs to a derived job.
    ///     ================================================================================================================================================
    ///     About Virtual Methods:
    ///     DoWork() - This method may be overridden in the derived class to manage specific steps of execution for a given job
    ///     if requried.
    ///     GetTaskList() - This method can be used to get a list of tasks specific to a job if job had run previously (or) a
    ///     new list of tasks
    ///     if job is running for the first time.
    ///     UpdateCurrentStatus() - This method may be used to update the current status of a job by providing a task list.
    ///     HandleStop() - This method can be used to perform the activities required after a STOP command is issued and job
    ///     has stopped.
    ///     ================================================================================================================================================
    ///     About Abstract Methods:
    ///     Initialize() - This method must be implemented in the derived class to retireve basic input parameters / settings
    ///     for a job
    ///     GenerateTasks() - This method must be implemented in the derived class to break down the job into divisible atomic
    ///     tasks.
    ///     DoAtomicWork() - This method must be implemented in the derived class to perform an atomic task.
    ///     ================================================================================================================================================
    /// </remarks>
    public abstract class BaseJob<JobParametersType, TaskType> where TaskType : new()
    {
        #region Variables

        private readonly string m_UserName = string.Empty;
        private bool m_AllTasksSucceeded;
        private DataSetService m_DatasetService;
        private DocumentService m_DocumentService;
        private int m_JobID;
        private LogInfo m_JobLogInfo;
        private int m_JobRunID;
        private int m_JobStatus = Constants.Completed;
        private JobMgmtService m_Jobservice;
        private MatterService m_MatterServices;
        private PrinterManagementService m_PrinterServices;
        private ReviewerSearchService m_ReviewerSearchService;
        private ReviewSetService m_ReviewsetService;
        private RVWTagService m_TagService;
        private DateTime? m_TaskEndTime;
        private LogInfo m_TaskLogInfo;
        private int m_TaskNumber;
        private DateTime? m_TaskStartTime;
        private string m_UserGuID = string.Empty;
        private UserService m_UserService;
        private MockWebOperationContext m_WebContext;

        #endregion

        #region Properties

        /// <summary>
        ///     Property to get or set a notification Message.
        /// </summary>
        public string CustomNotificationMessage { get; set; }

        /// <summary>
        ///     Property to get Job Log
        /// </summary>
        public LogInfo JobLogInfo
        {
            get { return m_JobLogInfo; }
        }

        /// <summary>
        ///     Property to get Task Log
        /// </summary>
        public LogInfo TaskLogInfo
        {
            get { return m_TaskLogInfo; }
        }

        /// <summary>
        ///     Property to get the job status
        /// </summary>
        public int JobCurrentStatus
        {
            get { return m_JobStatus; }
        }

        /// <summary>
        ///     Property to identify whether Progress percentage has to be managed by batch jOb or FrameWork(BaseJob)
        /// </summary>
        public bool IsCustomProgressPercentage { get; set; }

        /// <summary>
        ///     Mocks the web operation object
        /// </summary>
        public MockWebOperationContext WebContext
        {
            get { return m_WebContext; }
        }

        /// <summary>
        ///     Read-only property of user service
        /// </summary>
        public UserService UserService
        {
            get { return m_UserService; }
        }

        /// <summary>
        ///     Read-only property of ReviewerSearchService
        /// </summary>
        public ReviewerSearchService ReviewerSearchService
        {
            get { return m_ReviewerSearchService; }
        }

        /// <summary>
        ///     Read-only property of tag service
        /// </summary>
        public RVWTagService RVWTagService
        {
            get { return m_TagService; }
        }

        /// <summary>
        ///     Read-only property of job service
        /// </summary>
        public JobMgmtService JobService
        {
            get { return m_Jobservice; }
        }

        /// <summary>
        ///     Read-only property of ReviewSet service
        /// </summary>
        public ReviewSetService ReviewSetService
        {
            get { return m_ReviewsetService; }
        }

        /// <summary>
        ///     Read-only property of document service
        /// </summary>
        public DocumentService DocumentService
        {
            get { return m_DocumentService; }
        }


        /// <summary>
        ///     Read-only property of dataSet service
        /// </summary>
        public DataSetService DataSetService
        {
            get { return m_DatasetService; }
        }


        public PrinterManagementService PrinterServices
        {
            get { return m_PrinterServices; }
        }

        /// <summary>
        ///     Read-only property of matter service
        /// </summary>
        public MatterService MatterServices
        {
            get { return m_MatterServices; }
        }

        #endregion

        #region Virtual Methods [DO NOT MODIFY]

        /// <summary>
        ///     This method performs the main processing of a job.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters for the job.</param>
        /// <param name="jobRunDuration">Duration for which the job should run.</param>
        /// <param name="createdByGuid"> Created By Guid</param>
        /// <param name="notificationId"> Notification ID</param>
        /// <param name="scheduleType">Schedule Type</param>
        public virtual void DoWork(int jobId, int jobRunId, string bootParameters, int jobRunDuration,
            string createdByGuid, long notificationId, string scheduleType)
        {
            #region Variable Decleration

            int issuedCommandId = 0;
            // Declare an integer for getting Issued Command Id after updating the job execution status.
            int previouslyCommittedTaskCount; //for getting the saved tasks count in the earlier iteration. 
            string notificationMessage = Constants.JobSuccess;
            string jobName = string.Empty;
            DateTime jobRunStartTime;
            JobBusinessEntity jobDetails = null;
            bool exeStatus = false;
            bool isLogged = false;

            #endregion

            JobParametersType jobParameters = default(JobParametersType);
            m_JobID = jobId;
            m_JobRunID = jobRunId;
            m_UserGuID = createdByGuid;
            try
            {
                #region Assigning Priority for the job

                Thread.CurrentThread.Priority = ThreadPriority.Normal;
                jobDetails = GetJobDetails(jobId);
                jobName = jobDetails.Name;
                AssignPriority(jobDetails.Priority);

                #endregion

                //Updating Job Schedule Master Status(Whole Job Status)
                Helper.UpdateJobMasterStatus(jobId, (int) JobController.JobStatus.Running);
                //Authenticate the user
                Authenticate(createdByGuid);
                // Get the time when the job started.
                jobRunStartTime = DateTime.UtcNow;
                m_JobLogInfo = LogInfo.CreateJobLogInfo(jobId, jobRunId);
                // Call Initialize() implemented by the derived job class and get the job specific parameters / settings.
                jobParameters = Initialize(jobId, jobRunId, bootParameters, createdByGuid);
                // Update the broker that the job has started.
                Helper.UpdateJobExecutionStatus(jobParameters, JobController.JobStatus.Running,
                    Constants.BrokerType.Database, out issuedCommandId);
                // Check if the job can continue.
                if (!CanJobContinue(issuedCommandId, jobParameters))
                {
                    return;
                }
                Tasks<TaskType> allTasks;
                // Call GenerateTasks() implemented by the derived job class and get the job specific task list. Order the tasks by the task number.
                allTasks = GenerateTasks(jobParameters, out previouslyCommittedTaskCount);
                Tasks<TaskType> pendingTasks = null;
                pendingTasks = ValidateGeneratedTasks(jobId, jobRunId, jobParameters, previouslyCommittedTaskCount,
                    allTasks, out notificationMessage);
                if (pendingTasks != null && pendingTasks.Count > 0)
                {
                    // Create a new failed task list instance to collect the error tasks.
                    var failedTasks = new Tasks<TaskType>();
                    // Declare a local processNextTask flag.
                    bool processNextTask = true;
                    // Declare a variable to track progress.
                    double progressPercent;
                    progressPercent =
                        ConfigurationManager.AppSettings[Constants.ConfigurationKeyJobBehaviour].Equals(
                            Constants.OneTimeJob)
                            ? Convert.ToDouble(Helper.GetParameterValue(jobParameters,
                                Constants.PropertyNameJobProgressPercent))
                            : 0.0;
                    int localTasksCount = 0;
                    //Change the log category to Save the log details in task table.
                    // For each task that was generated for the specific job handle the processing, status commit and error log.
                    for (localTasksCount = 0; localTasksCount < pendingTasks.Count; localTasksCount++)
                    {
                        // Get the new task to be processed and assign it to the variable task.
                        TaskType task = pendingTasks[localTasksCount];
                        //TaskType currentTask;
                        m_TaskNumber = Convert.ToInt32(Helper.GetParameterValue(task, Constants.PropertyNameTaskNumber));
                        // Call the DoAtomicWork() implemented by the derived job class by passing the job parameters / settings and the unit task.
                        m_TaskStartTime = DateTime.UtcNow;
                        try
                        {
                            m_TaskLogInfo = LogInfo.CreateTaskLogInfo(jobId, jobRunId, m_TaskNumber);
                            Tracer.Trace("DoAtomicWork: jobId = {0}, jobRunId = {1}, m_TaskNumber = {2}", jobId,
                                jobRunId, m_TaskNumber);
                            exeStatus = DoAtomicWork(task, jobParameters);
                        }
                        catch (EVTaskException taskException)
                        {
                            EVJobExceptionHandleWrapper(jobId, jobRunId, m_UserName, m_TaskNumber, m_TaskStartTime,
                                DateTime.UtcNow, taskException);
                            isLogged = true;
                        }
                        catch (EVJobException evJobException)
                        {
                            evJobException.Trace();
                            //Throw the job exception to terminate the job in the middle of task execution.
                            throw;
                        }
                        catch (Exception exception)
                        {
                            TaskLogInfo.TaskKey = Constants.MessageTaskNumber +
                                                  m_TaskNumber.ToString(CultureInfo.InvariantCulture);
                            var taskException = new EVTaskException(Constants.ErrorCodeTask, exception, TaskLogInfo);
                            EVJobExceptionHandleWrapper(jobId, jobRunId, m_UserName, m_TaskNumber, m_TaskStartTime,
                                DateTime.UtcNow, taskException);
                            isLogged = true;
                        }
                        finally
                        {
                            //Log Task Message
                            if (!isLogged)
                                LogTaskMessage(TaskLogInfo);
                        }
                        isLogged = false;
                        m_TaskEndTime = DateTime.UtcNow;
                        if (!exeStatus)
                        {
                            failedTasks.Add(task);
                        }
                        // Set the status of the current task to Complete.
                        Helper.SetParameterValue(task, Constants.PropertyNameTaskComplete, Constants.Yes);
                        // Increment the progress percent by adding the current task's percent to the accumulate progress percent.
                        progressPercent +=
                            Convert.ToDouble(Helper.GetParameterValue(task, Constants.PropertyNameTaskPercent));
                        // Set the progress percent property in the job parameters / settings.
                        Helper.SetParameterValue(jobParameters, Constants.PropertyNameJobProgressPercent,
                            progressPercent);
                        // Call UpdateCurrentStatus() in Helper to update the current status of the job.
                        if (UpdateCurrentStatus(allTasks, jobParameters, m_TaskNumber, out processNextTask,
                            out issuedCommandId, IsCustomProgressPercentage))
                        {
                            if (processNextTask &&
                                (scheduleType.Equals(Constants.RecurrenceTypeOnetime) ||
                                 scheduleType.Equals(Constants.RecurrenceTypeNow)))
                                continue;
                            // If the processed next task flag is set to NO then stop processing the subsequent tasks in the task list.
                            if (!processNextTask ||
                                (Convert.ToInt32((DateTime.UtcNow - jobRunStartTime).TotalMinutes) >= jobRunDuration))
                            {
                                switch (issuedCommandId)
                                {
                                    case (int) JobController.JobCommand.Pause:
                                        notificationMessage = Constants.PausedSuccess;
                                        m_JobStatus = Constants.Paused;
                                        break;
                                    case (int) JobController.JobCommand.Stop:
                                        notificationMessage = Constants.CancelledStopped;
                                        m_JobStatus = Constants.Stopped;
                                        break;
                                    default:
                                        m_JobStatus = Constants.Running;
                                        break;
                                }
                                break;
                            } // End if processNextTask check.
                        } // End if UpdateCurrentStatus() check.
                    } // End foreach tasks.
                    //For One Time jobs, if localTasksCount == pendingTasks.Count then we can update the job status as completed in EV_JOB_JOBSCHEDULEMASTER
                    UpdateJobStatusForSpecificJobs(jobId, failedTasks, pendingTasks, localTasksCount, jobParameters);
                    //if there is atleast 1 failed task, then set notification heading to error message in bulk print job
                    m_AllTasksSucceeded = !(failedTasks != null && failedTasks.Count > 0);
                    // If the failed task list contains at least 1 task then log the failed tasks in the specified broker.
                    HandleGeneratedFailedTasks(jobParameters, failedTasks);
                    Helper.SetParameterValue(jobParameters, Constants.PropertyNameCurrentStatus,
                        Convert.ToString(m_JobStatus));
                } //IF -- Tasks.Count >0
            } // End try
            catch (EVJobException jobException)
            {
                EvLog.WriteEntry(jobName + " - " + jobId,
                    Constants.Failed + " - " + jobException.ErrorCode + "- " + jobException.StackTrace,
                    EventLogEntryType.Error);
                notificationMessage = Constants.JobFailure; //setting notification message
                m_JobStatus = Constants.Failed;
                //Saving the exception details into to Database.
                jobException.LogMessge.ErrorCode = jobException.ErrorCode;
                jobException.LogMessge.StackTrace = jobException.InnerException != null
                    ? jobException.InnerException.Message + Constants.HtmlBreak +
                      jobException.InnerException.Source + Constants.HtmlBreak +
                      jobException.InnerException.StackTrace
                    : jobException.Message + Constants.HtmlBreak +
                      jobException.Source + Constants.HtmlBreak +
                      jobException.StackTrace;
                isLogged = true;
                EVJobExceptionHandleWrapper(jobId, jobRunId, m_UserName, m_TaskNumber, m_TaskStartTime, DateTime.UtcNow,
                    jobException);
            }
            catch (Exception exception)
            {
                EvLog.WriteEntry(jobName + " - " + jobId,
                    Constants.Failed + " - " + exception.Message + "- " + exception.StackTrace, EventLogEntryType.Error);
                //Notification message assignment
                notificationMessage = Constants.JobFailure;
                m_JobStatus = Constants.Failed;
                //Saving the log info into database
                var evJobException = new EVJobException(Constants.ErrorCodeJob, exception, JobLogInfo);
                isLogged = true;
                EVJobExceptionHandleWrapper(jobId, jobRunId, m_UserName, m_TaskNumber, m_TaskStartTime, DateTime.UtcNow,
                    evJobException);
                //Saving the log into external file.
                exception.AddResMsg(Constants.ErrorInJob).Trace();
            } // End catch
            finally
            {
                ShutDownAndNotify(jobId, jobRunId, createdByGuid, notificationId, ref notificationMessage,
                    ref jobDetails, isLogged, jobParameters);
            } // End finally
        }

        /// <summary>
        ///     Perform the job shutdown and send notification
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <param name="jobRunId">Job Run ID</param>
        /// <param name="createdByGuid">User GUID</param>
        /// <param name="notificationId">Notification id</param>
        /// <param name="notificationMessage">Notification Message</param>
        /// <param name="jobDetails">Job details</param>
        /// <param name="isLogged">true if log details, false otherwise</param>
        /// <param name="jobParameters">Job Parameters</param>
        private void ShutDownAndNotify(int jobId, int jobRunId, string createdByGuid, long notificationId,
            ref string notificationMessage, ref JobBusinessEntity jobDetails, bool isLogged,
            JobParametersType jobParameters)
        {
            // Update the final status for the job instance and move those jobs to completed jobs                
            if (jobParameters != null)
            {
                Shutdown(jobParameters);
            }
            //custom notification message
            if (!string.IsNullOrEmpty(CustomNotificationMessage))
                //If any custom notification message is required to sent from batch job.
            {
                notificationMessage = notificationMessage + Constants.HtmlBreak + CustomNotificationMessage;
            }
            //Log Job Message
            if (!isLogged)
            {
                LogJobMessage(JobLogInfo);
            }
            SendNotifications(jobId, jobRunId, notificationMessage, createdByGuid, notificationId, jobDetails);
            jobDetails = null;
        }

        private void HandleGeneratedFailedTasks(JobParametersType jobParameters, Tasks<TaskType> failedTasks)
        {
            if (failedTasks.Count > Constants.NoTask)
            {
                // Call LogFailedTasks() and pass the failed task list, job parameters / settings.
                HandleFailedTasks(failedTasks, jobParameters);
                // Clear the failed task list.
                failedTasks.Clear();
            } // End if faileTasksList check
        }

        private void UpdateJobStatusForSpecificJobs(int jobId, Tasks<TaskType> failedTasks, Tasks<TaskType> pendingTasks,
            int localTasksCount,
            JobParametersType jobParameters)
        {
            if (
                !Helper.GetParameterValue(jobParameters, Constants.TableJobTypeMasterColumnJobTypeName).
                    Equals(Constants.JobTypeNameBulkPrintJobs)) //check is only for bulk print.
            {
                int jobStatusValue = 0;
                //By default status is set as 0 so that it will check the status only for cluster job. Need to change once cluster job is splitted as different task
                if (
                    Helper.GetParameterValue(jobParameters, Constants.TableJobTypeMasterColumnJobTypeName).
                        Equals(Constants.JobTypeNameClusterJob))
                    //If job already cancelled then dont update the status as completed
                {
                    jobStatusValue = Helper.GetJobStatus(jobId.ToString(CultureInfo.InvariantCulture));
                }
                if (
                    ConfigurationManager.AppSettings[Constants.ConfigurationKeyJobBehaviour].Equals(Constants.OneTimeJob) &&
                    localTasksCount == pendingTasks.Count && failedTasks.Count == Constants.NoTask)
                {
                    if (jobStatusValue != 8) //If job already cancelled then don't update the status as completed
                    {
                        Helper.UpdateJobMasterStatus(jobId, Constants.Completed);
                    }
                }
                else if (
                    Helper.GetParameterValue(jobParameters, Constants.TableJobTypeMasterColumnJobTypeName)
                        .Equals(Constants.JobTypeNameClusterJob))
                {
                    if (jobStatusValue != 8) //If job already cancelled then don't update the status as failed
                    {
                        //For cluster job if a task failed then update the status as failed in database
                        Helper.UpdateJobMasterStatus(jobId, Constants.Failed);
                    }
                }
            }
        }


        /// <summary>
        ///     method to check the generated tasks
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="jobRunId"></param>
        /// <param name="jobParameters"></param>
        /// <param name="previouslyCommittedTaskCount"></param>
        /// <param name="allTasks"></param>
        /// <param name="notificationMessage"></param>
        /// <returns></returns>
        private Tasks<TaskType> ValidateGeneratedTasks(int jobId, int jobRunId, JobParametersType jobParameters,
            int previouslyCommittedTaskCount, Tasks<TaskType> allTasks, out string notificationMessage)
        {
            notificationMessage = Constants.JobSuccess;
            Tasks<TaskType> pendingTasks = null;
            if (allTasks != null)
            {
                //If it is first time then add all the tasks to task table
                if (previouslyCommittedTaskCount == 0)
                {
                    Helper.InsertTaskDetails(allTasks, jobRunId);
                }
                else if (allTasks.Count > 0 && allTasks.Count > previouslyCommittedTaskCount)
                {
                    Helper.InsertTaskDetails(GetRangeTasks(allTasks, previouslyCommittedTaskCount + 1), jobRunId);
                }
                if (allTasks.Count > 0)
                {
                    // Filter the tasks marked as complete.
                    pendingTasks = OrderAndFilterTasks(allTasks);
                }
            }
            return pendingTasks;
        }

        // End DoWork().

        /// <summary>
        ///     Authenticates the user based on the process type in config i.e INPROC or Otherwise
        /// </summary>
        /// <param name="createdByGuid"></param>
        private void Authenticate(string createdByGuid)
        {
            var userProp = new UserBusinessEntity();
            userProp.UserGUID = createdByGuid;
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            userProp = UserBO.AuthenticateUsingUserGuid(createdByGuid);
            var userSession = new UserSessionBEO();
            SetUserSession(createdByGuid, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            m_WebContext = new MockWebOperationContext();
            EVHttpContext.CurrentContext = mockContext.Object;
            if (WebContext != null && WebContext.Object != null)
            {
                m_ReviewerSearchService = new ReviewerSearchService(WebContext.Object);
                m_UserService = new UserService(WebContext.Object);
                m_TagService = new RVWTagService(WebContext.Object);
                m_Jobservice = new JobMgmtService();
                m_Jobservice.SetWebOperationContext(WebContext.Object);
                m_ReviewsetService = new ReviewSetService();
                m_ReviewsetService.SetWebOperationContext(WebContext.Object);
                m_DocumentService = new DocumentService(WebContext.Object);
                m_DatasetService = new DataSetService(WebContext.Object);
                m_PrinterServices = new PrinterManagementService(WebContext.Object);
                m_MatterServices = new MatterService(WebContext.Object);
            }

            EVUnityContainer.RegisterInstance("UserService", m_UserService);
            EVUnityContainer.RegisterInstance("RVWTagService", m_TagService);
            EVUnityContainer.RegisterInstance("JobService", m_Jobservice);
            EVUnityContainer.RegisterInstance("ReviewSetService", m_ReviewsetService);
            EVUnityContainer.RegisterInstance("DocumentService", m_DocumentService);
            EVUnityContainer.RegisterInstance("ReviewerSearchService", m_ReviewerSearchService);
            EVUnityContainer.RegisterInstance("DatasetService", m_DatasetService);
        }

        /// <summary>
        ///     Sets the usersession object using the UserBusinessEntity details
        /// </summary>
        /// <param name="createdByGuid"></param>
        /// <param name="userProp"></param>
        /// <param name="userSession"></param>
        private static void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.EntityTypeId = userProp.AuthorizedBEO.EntityTypeId;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
        }

        /// <summary>
        ///     Gets the task list.
        /// </summary>
        /// <typeparam name="JP">The type of the JobParameter.</typeparam>
        /// <typeparam name="TT">The type of the Task Parameter.</typeparam>
        /// <param name="jobParameters">The job parameters.</param>
        /// <returns>List of Tasks</returns>
        protected virtual Tasks<TT> GetTaskList<JP, TT>(JP jobParameters) where TT : class, new()
        {
            // Declare a local tasks instance.
            Tasks<TT> tasks = null;

            // Get the last committed tasks for this job. This may contain nothing if the job is running for the first time.
            //tasks = Helper.GetJobLastCommittedStatus<JobParametersType, TaskType>(jobParameters);
            tasks = Helper.GetJobTaskDetails<JP, TT>(jobParameters);

            // Return a new list instance of the tasks specific to the job if the job is running for the first time.
            if (tasks == null || tasks.Count <= 0)
            {
                tasks = new Tasks<TT>();
            } // End if

            // Return the tasks list instance.
            return tasks;
        } // End GetTaskList()


        /// <summary>
        ///     This method will update the Status of the job
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="jobParameters"></param>
        /// <param name="continueJob"></param>
        /// <param name="issuedCommandId"></param>
        /// <returns></returns>
        protected virtual bool UpdateCurrentStatus(Tasks<TaskType> tasks, JobParametersType jobParameters, int taskId,
            out bool continueJob, out int issuedCommandId, bool isCustomProgressPercentage)
        {
            // Declare a local variable for issued command id.
            issuedCommandId = 0;
            var brokerType =
                (Constants.BrokerType) Helper.GetParameterValue(jobParameters, Constants.PropertyNameStatusBrokerType);
            // Call the PersistStatus method provided by the Helper class to store the processed tasks in the specified broker.
            bool output = Helper.PersistStatus<JobParametersType, TaskType>(jobParameters, taskId, brokerType,
                out issuedCommandId, m_TaskStartTime, isCustomProgressPercentage);
            // Default that the job should continue after commit unless other wise a command is issued to STOP / PAUSE.
            continueJob = CanJobContinue(issuedCommandId, jobParameters);
            // Return the status of the update operation.
            return output;
        }

        /// <summary>
        ///     This method performs the activities required after a STOP command is issued and job has stopped.
        /// </summary>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected virtual void HandleStop(JobParametersType jobParameters)
        {
            // NOTE: Job specific STOP activities if any will only be handled in the derived job class.
        } // End HandleStop()

        /// <summary>
        ///     This method handles the failed tasks specific to a job.
        /// </summary>
        /// <param name="tasks">List of tasks that failed.</param>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected virtual void HandleFailedTasks(Tasks<TaskType> tasks, JobParametersType jobParameters)
        {
            // NOTE: Job specific failed tasks handling activities if any will only be handled in the derived job class.
        } // End HandleFailedTasks()

        /// <summary>
        ///     This method performs the shutdown activities for a job, if any.
        /// </summary>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected virtual void Shutdown(JobParametersType jobParameters)
        {
            // NOTE: Job specific failed tasks handling activities if any will only be handled in the derived job class.
        } // End Shutdown()


        /// <summary>
        ///     Job exception handle wrapper.
        /// </summary>
        /// <param name="jobId">The job id.</param>
        /// <param name="jobRunId">The job run id.</param>
        /// <param name="userGuId">The user gu id.</param>
        /// <param name="taskNo">The task number.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="ex">The ex.</param>
        private void EVJobExceptionHandleWrapper(int jobId, int jobRunId, string userGuId, int taskNo,
            DateTime? startDate, DateTime? endDate, Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            var taskException = ex as EVTaskException;
            if (taskException != null)
            {
                if (taskException.LogMessge != null)
                {
                    taskException.LogMessge.IsError = true;
                    Helper.UpdateTaskLog(jobId, jobRunId, taskNo,
                        string.IsNullOrEmpty(taskException.LogMessge.TaskKey)
                            ? string.Empty
                            : taskException.LogMessge.TaskKey, Helper.SerializeObject(taskException.LogMessge), true,
                        startDate, endDate, taskException.ErrorCode);
                }

                return;
            }

            //If exception is from Job
            var jobException = ex as EVJobException;
            if (jobException != null)
            {
                if (jobException.LogMessge != null)
                {
                    jobException.LogMessge.IsError = true;
                }
                Helper.JobLog(jobId, jobRunId, jobException.ErrorCode, Helper.SerializeObject(jobException.LogMessge),
                    userGuId, true, false);
            }
        }

        /// <summary>
        ///     This method is used to log the job messages
        /// </summary>
        /// <param name="logInfo">Information to be logged</param>
        private void LogJobMessage(LogInfo logInfo)
        {
            if (logInfo != null)
            {
                Helper.JobLog(m_JobID, m_JobRunID, string.Empty, Helper.SerializeObject(logInfo), m_UserGuID,
                    logInfo.IsError, false);
            }
        }


        /// <summary>
        ///     This method is used to log the job messages
        /// </summary>
        /// <param name="logInfo">Information to be logged</param>
        /// <param name="isXmlLog">Determines XmlLog Information to be logged</param>
        protected void LogJobMessage(LogInfo logInfo, bool isXmlLog)
        {
            if (logInfo != null)
            {
                Helper.JobLog(m_JobID, m_JobRunID, string.Empty, logInfo.CustomMessage, m_UserGuID, logInfo.IsError,
                    isXmlLog);
            }
        }

        /// <summary>
        ///     This method is used to log the task messages
        /// </summary>
        /// <param name="logInfo">Information to be logged</param>
        private void LogTaskMessage(LogInfo logInfo)
        {
            if (logInfo != null)
            {
                Helper.UpdateTaskLog(m_JobID, m_JobRunID, m_TaskNumber,
                    string.IsNullOrEmpty(logInfo.TaskKey) ? string.Empty : logInfo.TaskKey,
                    Helper.SerializeObject(logInfo), logInfo.IsError, m_TaskStartTime, DateTime.UtcNow, string.Empty);
            }
        }

        #endregion

        #region Abstract Methods [DO NOT MODIFY]

        /// <summary>
        ///     This method gets all job input parameters / settings required to run a job.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters for the job.</param>
        /// <param name="createdByGuid">Created By Guid</param>
        /// <returns>Parameters / Settings for a given job.</returns>
        protected abstract JobParametersType Initialize(int jobId, int jobRunId, string bootParameters,
            string createdByGuid);

        /// <summary>
        ///     This method breaks down the job into divisible atomic / repeatable tasks.
        /// </summary>
        /// <param name="jobParameters">Input parameters / settings for a job.</param>
        /// <param name="isFirstTime">Determine this iteration is executing first time or is Paused one</param>
        /// <returns>List of tasks for a given job.</returns>
        /// <summary>
        ///     This method breaks down the job into divisible atomic / repeatable tasks.
        /// </summary>
        /// <param name="jobParameters">Input parameters / settings for a job.</param>
        /// <param name="previouslyCommittedTaskCount">
        ///     previouslyCommittedTaskCount to identify the job is running first time or
        ///     not and also used to insert the new tasks added in the middle of iterations
        /// </param>
        /// <returns>>List of tasks for a given job.</returns>
        protected abstract Tasks<TaskType> GenerateTasks(JobParametersType jobParameters,
            out int previouslyCommittedTaskCount);


        /// <summary>
        ///     This method performs a unit task for a given job.
        /// </summary>
        /// <param name="task">Instance of a unit task to be performed by a given job.</param>
        /// <param name="jobParameters">Input parameters / settings for a job.</param>
        /// <returns>Indicator if the method execution was successful.</returns>
        protected abstract bool DoAtomicWork(TaskType task, JobParametersType jobParameters);

        #endregion

        #region Private Methods [DO NOT MODIFY]

        /// <summary>
        ///     Find if a job can continue based on an issued command id.
        /// </summary>
        /// <param name="commandId">Command Identifier.</param>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        /// <returns>Indicates if a job can continue execution.</returns>
        private bool CanJobContinue(int commandId, JobParametersType jobParameters)
        {
            // Declare a local flag
            bool continueJob = true;

            // Check if the job should continue by calling the GetJobCommand() in Helper
            switch ((JobController.JobCommand) commandId)
            {
                    // Set continueJob to YES if a START command was issued.
                case JobController.JobCommand.Start:
                {
                    // Set continueJob to true.
                    continueJob = Constants.Yes;
                    // Store the last executed command to Start.
                    Helper.SetParameterValue(jobParameters, Constants.PropertyNameLastExecutedCommand,
                        JobController.JobCommand.Start);

                    // Update the job's current status id as RUNNING in the broker specified.
                    Helper.UpdateJobExecutionStatus(jobParameters, JobController.JobStatus.Running,
                        Constants.BrokerType.Database);
                    break;
                } // End case COMMAND_START

                    // Set continueJob to NO if a STOP command was issued.
                case JobController.JobCommand.Stop:
                {
                    // Set continueJob to false.
                    continueJob = Constants.No;
                    // Store the last executed command as Stop.
                    Helper.SetParameterValue(jobParameters, Constants.PropertyNameLastExecutedCommand,
                        JobController.JobCommand.Stop);

                    // Update the job's current status id as STOPPED in the broker specified.
                    Helper.UpdateJobExecutionStatus(jobParameters, JobController.JobStatus.Stopped,
                        Constants.BrokerType.Database);

                    // Perform the activities required after a STOP command is issued and job has stopped 
                    HandleStop(jobParameters);
                    break;
                } // End case COMMAND_STOP

                    // Set continueJob to NO if a PAUSE command was issued.
                case JobController.JobCommand.Pause:
                {
                    // Set continueJob to false.
                    continueJob = Constants.No;

                    // Store the last executed command as Pause.
                    Helper.SetParameterValue(jobParameters, Constants.PropertyNameLastExecutedCommand,
                        JobController.JobCommand.Pause);

                    // Update the job's current status id as PAUSED in the broker specified.
                    Helper.UpdateJobExecutionStatus(jobParameters, JobController.JobStatus.Paused,
                        Constants.BrokerType.Database);
                    break;
                } // End case COMMAND_PAUSE
            } // End switch

            // Return the flag denoting if a job can continue.
            return continueJob;
        } // End CanJobContinue()

        /// <summary>
        ///     Filter out completed tasks.
        ///     Order the filtered tasks by task number.
        /// </summary>
        /// <param name="tasks">List of tasks specific to a job.</param>
        /// <returns>List of tasks ordered by task number and filtered out by incomplete tasks.</returns>
        private Tasks<TaskType> OrderAndFilterTasks(Tasks<TaskType> tasks)
        {
            // Cast the list of tasks to enumerable type to enable filtering and ordering.
            IEnumerable<TaskType> localTasks = tasks.Cast<TaskType>();

            // Filter out completed tasks.
            localTasks =
                localTasks.Where(
                    task =>
                        (Convert.ToBoolean(Helper.GetParameterValue(task, Constants.PropertyNameTaskComplete)) ==
                         Constants.No));

            // Order by task number.
            localTasks = localTasks.OrderBy(task => Helper.GetParameterValue(task, Constants.PropertyNameTaskNumber));

            // Create a new list of tasks
            var orderedTasks = new Tasks<TaskType>();

            // Add the filtered and ordered enumerable type to this list.
            orderedTasks.Add(localTasks);

            // Return the filtered and ordered tasks.
            return orderedTasks;
        } // End OrderAndFilterTasks()

        /// <summary>
        ///     This method is sued to filter and return a set of tasks
        /// </summary>
        /// <param name="tasks">List of tasks</param>
        /// <returns>List of <TaskType> object</returns>
        private Tasks<TaskType> GetRangeTasks(Tasks<TaskType> tasks, int fromTaskNumber)
        {
            IEnumerable<TaskType> filteredTasks = tasks.Cast<TaskType>();
            filteredTasks =
                filteredTasks.Where(
                    task =>
                        (Convert.ToInt32(Helper.GetParameterValue(task, Constants.PropertyNameTaskNumber)) >=
                         fromTaskNumber));
            var newTasks = new Tasks<TaskType>();
            newTasks.Add(filteredTasks);
            return newTasks;
        }


        /// <summary>
        ///     This method will send notifications.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="jobStatus">Job Status.</param>
        /// <param name="notificationMessage">Notification message to be sent.</param>
        /// <param name="createdByGuid">Created By Guid</param>
        /// <param name="notificationID"> Notification ID</param>
        private void SendNotifications(int jobId, int jobRunId, string notificationMessage, string createdByGuid,
            long notificationID, JobBusinessEntity jobSubScriptionDetails)
        {
            string jobName = string.Empty;
            string jobType = string.Empty;
            bool output = Helper.UpdateJobFinalStatus(jobId, jobRunId, JobCurrentStatus);
            if (output)
            {
                //Get Job Details required for Notification.
                JobBusinessEntity jobDetails = JobMgmtBO.GetJobDetails(jobId.ToString(CultureInfo.InvariantCulture));
                jobName = jobDetails.Name;
                jobType = jobDetails.TypeName;
                // If we don't have job parameters then we will not be able to send the notification also.
                UserBusinessEntity objUser = UserBO.GetUserUsingGuid(createdByGuid);
                var objNotificationMessage = new NotificationMessageBEO();
                objNotificationMessage.NotificationId = notificationID;
                objNotificationMessage.CreatedByUserGuid = createdByGuid; //objUser.UserGUID;
                objNotificationMessage.CreatedByUserName = (objUser.DomainName.Equals(Constants.NA))
                    ? objUser.UserId
                    : objUser.DomainName + Constants.BackSlash + objUser.UserId; //User/Loging Name.
                //Subscription details
                objNotificationMessage.SubscriptionTypeName = jobSubScriptionDetails.TypeName;
                objNotificationMessage.FolderId = jobSubScriptionDetails.FolderID;
                //Send notification only if all the mandatory values for notification are populated 
                if ((objNotificationMessage.NotificationId != 0 ||
                     !string.IsNullOrEmpty(objNotificationMessage.SubscriptionTypeName)) &&
                    !string.IsNullOrEmpty(objNotificationMessage.CreatedByUserGuid))
                {
                    /*Default Message format -  <Job Type> job <Job Name>  for <folder/matter/dataset> Status <status> */
                    var defaultmessage = new StringBuilder();
                    defaultmessage.Append(string.IsNullOrEmpty(jobType)
                        ? string.Empty
                        : Constants.Type +
                          (jobDetails.Visibility ? jobType : jobType.Replace(Constants.Job, Constants.Task)) +
                          Constants.HtmlBreak);
                    defaultmessage.Append(jobName + Constants.Instance + jobRunId);
                    defaultmessage.Append(Constants.HtmlBreak + "Folder: " + jobSubScriptionDetails.FolderName +
                                          Constants.StrSpace);

                    SetNotificationBodyAndSubject(jobDetails, objNotificationMessage, defaultmessage,
                        notificationMessage);
                    string notificationValues = objNotificationMessage.NotificationId + " - " +
                                                objNotificationMessage.CreatedByUserGuid + " - " +
                                                objNotificationMessage.NotificationSubject + " - " +
                                                objNotificationMessage.NotificationBody;

                    objNotificationMessage.SendDefaultMessage = (m_JobStatus == Constants.Completed);
                    EvLog.WriteEntry(jobName + " - " + jobId, notificationValues, EventLogEntryType.Information);
                    if (!string.IsNullOrEmpty(objNotificationMessage.NotificationSubject) &&
                        !string.IsNullOrEmpty(objNotificationMessage.NotificationBody))
                    {
                        NotificationBO.SendNotificationMessage(objNotificationMessage);
                    }
                }
                objNotificationMessage = null;

                jobSubScriptionDetails = null;
            }
        } // End SendNotifications() 

        /// <summary>
        ///     Set notification's body and subject content
        /// </summary>
        /// <param name="jobDetails">Current job details</param>
        /// <param name="objNotificationMessage">Notification message object</param>
        /// <param name="defaultmessage">Message to display</param>
        private void SetNotificationBodyAndSubject(JobBusinessEntity jobDetails,
            NotificationMessageBEO objNotificationMessage, StringBuilder defaultmessage, string notificationMessage)
        {
            string notificationSubject = string.Empty;
            string notificationBody = string.Empty;
            switch (jobDetails.Type)
            {
                case 16:
                case 24:
                case 25:
                case 28:
                case 29:
                case 30:
                {
                    string notificationStatus = notificationMessage.Substring(0,
                        notificationMessage.IndexOf(Constants.HtmlBreak));
                    defaultmessage.Append(Constants.HtmlBreak + "Status: " + notificationStatus);
                    notificationSubject = defaultmessage.ToString()
                        .Replace(Constants.HtmlBreak, Constants.StrSpace + Constants.StrSpace);
                    notificationBody = CustomNotificationMessage;
                    break;
                }
                default:
                {
                    if (jobDetails.Type != 27)
                    {
                        defaultmessage.Append(Constants.HtmlBreak);
                        defaultmessage.Append(Constants.StatusColon);
                        defaultmessage.Append(notificationMessage);
                    }
                    else
                    {
                        defaultmessage.Append(Constants.HtmlBreak);
                        defaultmessage.Append(Constants.StatusColon);
                        if (!m_AllTasksSucceeded && notificationMessage.IndexOf(Constants.JobSuccess) == 0)
                        {
                            notificationMessage = notificationMessage.Replace(Constants.JobSuccess, Constants.JobFailed);
                        }
                        else if (!m_AllTasksSucceeded && notificationMessage.IndexOf(Constants.JobFailure) == 0 &&
                                 notificationMessage.Length > Constants.JobFailure.Length)
                        {
                            notificationMessage = notificationMessage.Replace(Constants.JobFailure, Constants.JobFailed);
                        }
                        defaultmessage.Append(notificationMessage.Replace(string.Format("{0}{1}", Constants.HtmlBreak,
                            CustomNotificationMessage), string.Empty));
                    }
                    notificationSubject = defaultmessage.ToString().Replace(Constants.HtmlBreak, string.Format("{0}{1}",
                        Constants.StrSpace, Constants.StrSpace));
                    if (jobDetails.Type == Constants.BulkPrintJobType)
                    {
                        defaultmessage.Clear();
                        if (notificationMessage.IndexOf(Constants.JobSuccess) == 0)
                        {
                            defaultmessage.Append(
                                notificationMessage.Replace(string.Format("{0}{1}", Constants.JobSuccess,
                                    Constants.HtmlBreak), string.Empty));
                        }
                        else
                        {
                            defaultmessage.Append(
                                notificationMessage.Replace(string.Format("{0}{1}", Constants.JobFailed,
                                    Constants.HtmlBreak), string.Empty));
                        }
                    }
                    notificationBody = defaultmessage.ToString();
                    break;
                }
            }
            objNotificationMessage.NotificationSubject = notificationSubject;
            objNotificationMessage.NotificationBody = notificationBody;
        }

        /// <summary>
        ///     This method will return all the details required to process the job.
        ///     JobSubSciptionNamae, FolderId, Visibility, JobPriority etc.
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <returns>Priority</returns>
        private JobBusinessEntity GetJobDetails(int jobId)
        {
            return (Helper.GetJobDetails(jobId.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        ///     This method will assign priority to the job
        /// </summary>
        /// <param name="priority">The priority of the job.</param>
        /// <remarks></remarks>
        private void AssignPriority(int priority)
        {
            if (priority == Constants.JobPriorityNormal) // where Constanst.MEDIUM_PRIORITY_JOB will be 2.
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }
            else if (priority == Constants.JobPriorityLow) // where Constants.LOW_PRIORITY_JOB will be 3.
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            }
            else if (priority == Constants.JobPriorityHigh)
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
        }

        #endregion

        // The following are the methods implemented by BaseJob class that can be overriden by a derived job class.
        // DoWork() - This method may be overridden in the derived class to manage specific steps of execution for a given job if requried.
        // GetTaskList() - This method can be used to get a list of tasks specific to a job if job had run previously (or) a new list of tasks if job is running for the first time.
        // UpdateCurrentStatus() - This method may be used to update the current status of a job by providing a task list.
        // HandleStop() - This method can be used to perform the activities required after a STOP command is issued and job has stopped.
        // HandleFailedTasks() - This method is to handle the failed tasks specific to a job.
        // Shutdown() - This method can be used to perform shutdown activities for a job if any.

        // The following are the methods that MUST be implemented by all derived job classes.
        // Initialize() - This method must be implemented in the derived class to retireve basic input parameters / settings for a job.
        // GenerateTasks() - This method must be implemented in the derived class to break down the job into divisible atomic tasks.
        // DoAtomicWork() - This method must be implemented in the derived class to perform an atomic task.
    }


    /// <summary>
    ///     This class will be used to Mock the Operation context object
    /// </summary>
    public class MockWebOperationContext : Mock<IWebOperationContext>
    {
        private readonly Mock<IIncomingWebRequestContext> m_RequestContextMock = new Mock<IIncomingWebRequestContext>();

        private readonly Mock<IOutgoingWebResponseContext> m_ResponseContextMock =
            new Mock<IOutgoingWebResponseContext>();

        /// <summary>
        ///     Constructor
        /// </summary>
        public MockWebOperationContext()
        {
            SetupGet(webContext => webContext.IncomingRequest).Returns(m_RequestContextMock.Object);
            SetupGet(webContext => webContext.OutgoingResponse).Returns(m_ResponseContextMock.Object);
            var requestHeaders = new WebHeaderCollection();
            var responseHeaders = new WebHeaderCollection();
            m_RequestContextMock.SetupGet(requestContext => requestContext.Headers).Returns(requestHeaders);
            m_ResponseContextMock.SetupGet(responseContext => responseContext.Headers).Returns(responseHeaders);
        }

        /// <summary>
        ///     Get the Incoming Request
        /// </summary>
        public Mock<IIncomingWebRequestContext> IncomingRequest
        {
            get { return m_RequestContextMock; }
        }

        /// <summary>
        ///     Get the Outgoing Response
        /// </summary>
        public Mock<IOutgoingWebResponseContext> OutgoingResponse
        {
            get { return m_ResponseContextMock; }
        }
    } //End MockWebOperationContext
} // End Namespace