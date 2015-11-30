using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    using System.Messaging;

    using LexisNexis.Evolution.BusinessEntities;
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    public class JobSection
    {
        public JobSection(WorkRequest workRequest, WorkerCard workerCard)
        {
            WorkRequest = workRequest;
            WorkerCard = workerCard;

            WorkerIDs = new List<string>();

            HiringPipe = new Pipe(workRequest.HiringPipeName);
            HiringPipe.Open();
        }

        public RoleSlotToken GetFreeSlot()
        {
            try
            {
                PipeMessageEnvelope envelope = HiringPipe.Receive(hiringPipeTimeout);
                if (envelope != null)
                {
                    object message = envelope.Body;
                    RoleSlotToken roleSlot = message as RoleSlotToken;
                    Debug.Assert(null != roleSlot);
                    return roleSlot;
                }

                // Debugging
                //if (SectionName == "S1")
                //{
                //    throw new EVException().AddDbgMsg("Test");
                //}
            }
            catch (Exception ex)
            {
                MessageQueueException messageQueueException = ex as MessageQueueException;
                if (messageQueueException != null && (uint)messageQueueException.ErrorCode == 0x80004005)
                {
                    Tracer.Debug("Cannot find hiring pipe {0}.{1}, so skip processing it.", 
                        WorkRequest.HiringPipeName.SectionName, WorkRequest.PipelineId);
                }
                else
                {
                    ProblemReport problemReport = new ProblemReport(WorkRequest.SectionName, WorkerStage.Unknown, ex);
                    using (Pipe reportPipe = new Pipe(WorkRequest.ReportPipeName))
                    {
                        try
                        {
                            reportPipe.Open();
                            problemReport.SendProblemReport(reportPipe);
                        }
                        catch (Exception innerEx)
                        {
                            innerEx.Trace().Swallow();
                        }
                    }
                }
                throw;
            }
            return null;
        }

        public string SectionName
        {
            get { return WorkRequest.SectionName; }
        }
        public RoleType RoleType
        {
            get { return WorkRequest.RoleType; }
        }
        public WorkRequest WorkRequest { get; private set; }
        public WorkerCard WorkerCard { get; private set; }
        public List<string> WorkerIDs { get; private set; }
        public Pipe HiringPipe { get; private set; }

        // How long we wait for hiring pipe if there is no message readily available
        private readonly TimeSpan hiringPipeTimeout = TimeSpan.Zero;
    }

    public class RunningJob
    {
        public RunningJob(JobInfo jobInfo)
        {
            PipelineId = jobInfo.PipelineId;
            BootParameters = jobInfo.BootParameters;
            JobParameters = jobInfo.JobParameters;
            Sections = new List<JobSection>();
            State = States.Running;

            // Information required to start classic jobs
            JobId = jobInfo.JobId;
            ScheduleRunDuration = jobInfo.ScheduleRunDuration;
            ScheduleCreatedBy = jobInfo.ScheduleCreatedBy;
            NotificationId = jobInfo.NotificationId;
            Frequency = jobInfo.Frequency;
        }

        public string PipelineId { get; private set; }
        public string BootParameters { get; private set; }
        public BaseJobBEO JobParameters { get; private set; }
        public List<JobSection> Sections { get; private set; }
        internal enum States
        {
            Running,
            WaitingForWorkersToQuit,
            ReadyForDispose,
        }

        internal States State { get; set; }

        // Information required to start classic jobs
        public int JobId { get; private set; }
        public int ScheduleRunDuration { get; private set; }
        public string ScheduleCreatedBy { get; private set; }
        public long NotificationId { get; private set; }
        public string Frequency { get; private set; }
    }

    internal partial class WorkerManager 
    {
        public void ProcessInfoFromDirector(OpenJobs openJobs)
        {
            RemoveWorkerRunnersForNotPresentWorkers();

            ProcessDirectorsOpenJobsList(openJobs);

            ProcessWorkerManagerRunningJobsList(openJobs);
        }

        private void ProcessDirectorsOpenJobsList(OpenJobs openJobs)
        {
            foreach (OpenJob openJob in openJobs.Jobs)
            {
                try
                {
                    RunningJob runningJob = FindRunningJob(openJob);
                    if (runningJob == null)
                    {
                        // We don't have this job
                        if (openJob.Command == Command.Run)
                        {
                            // And it is in the running state
                            JobInfo jobInfo = directorCoreServicesClient.GetJobInfo(openJob.PipelineId, Environment.MachineName);
                            if (jobInfo == null)
                            {
                                // This should not happen, but still was observed at least once in P3, so let's add safety blanket here.
                                Tracer.Warning("Received null JobInfo when requested details of the JobRunId {0} from Director.", openJob.PipelineId);
                                continue;
                            }
                            CreateRunningJob(jobInfo);
                        }
                    }
                    else
                    {
                        UpdateRunningJob(runningJob, openJob);
                    }
                }
                catch (Exception ex)
                {
                    MessageQueueException messageQueueException = ex as MessageQueueException;
                    if (messageQueueException != null && (uint)messageQueueException.ErrorCode == 0x80004005)
                    {
                        Tracer.Debug("Cannot find pipeline with ID = {0}, so skip processing it.", openJob.PipelineId);
                    }
                    else
                    {
                        ex.Trace().Swallow();
                        Tracer.Warning("Previous exception in the job handling causes WorkerManager to Cancel job with PipelineId = {0}", openJob.PipelineId);
                    }
                    openJob.Command = Command.Cancel;
                }
            }
        }

        private void ProcessWorkerManagerRunningJobsList(OpenJobs openJobs)
        {
            List<RunningJob> updatedListOfRunningJobs = new List<RunningJob>();
            foreach (RunningJob runningJob in RunningJobs)
            {
                OpenJob openJob = FindJobInfo(openJobs, runningJob.PipelineId);
                if (openJob == null || openJob.Command == Command.Cancel)
                {
                    if (StopWorkersForJob(runningJob))
                    {
                        updatedListOfRunningJobs.Add(runningJob);
                    }
                }
                else
                {
                    updatedListOfRunningJobs.Add(runningJob);
                }
            }
            RunningJobs = updatedListOfRunningJobs;
        }

        private void CreateRunningJob(JobInfo jobInfo)
        {
            if (jobInfo == null)
            {
                return;
            }

            Tracer.Info("Starting workers for a new job. PipelineId = {0}, JobId = {1}, JobTypeId = {2}", 
                jobInfo.PipelineId, jobInfo.JobId, jobInfo.JobTypeId);

            RunningJob runningJob = new RunningJob(jobInfo);

            foreach (WorkRequest workRequest in jobInfo.WorkRequests)
            {
                workRequest.BootParameters = jobInfo.BootParameters;
                workRequest.JobParameters = jobInfo.JobParameters;
                WorkerCard workerCard = WorkersInventory.Instance.FindWorkerForRole(workRequest.RoleType);
                if (null == workerCard)
                {
                    Tracer.Info("This worker manager does not have workers suitable for the role {0}", workRequest.RoleType);
                    continue;
                }
                JobSection jobSection = new JobSection(workRequest, workerCard);
                runningJob.Sections.Add(jobSection);
            }
            RunningJobs.Add(runningJob);

            if (jobInfo.Command == Command.Run)
            {
                EmployWorkersForJob(runningJob);
            }
        }

        private void EmployWorkersForJob(RunningJob runningJob)
        {
            foreach (JobSection jobSection in runningJob.Sections)
            {
                if (null == jobSection.WorkerCard) continue; // We don't have workers for this role
                uint availableWorkers = jobSection.WorkerCard.MaxNumOfInstances - GetWorkingWorkersNumber(jobSection.RoleType);
                if (availableWorkers == 0) continue; // All available workers for that role are busy

                // Try to hire ONE worker for the role.
                // We should not hire more than one worker for the role in one iteration to give other WorkerManagers a chance to take some workload.
                RoleSlotToken roleSlotToken = null;

                var transaction = new CommittableTransaction(TransactionManager.MaximumTimeout);
                Transaction oldAmbient = Transaction.Current;
                Transaction.Current = transaction;
                try
                {
                    roleSlotToken = jobSection.GetFreeSlot();
                }
                finally
                {
                    Transaction.Current = oldAmbient;
                    if (null == roleSlotToken)
                    {
                        transaction.Rollback();
                        transaction.Dispose();
                    }
                }

                if (null == roleSlotToken)
                {
                    continue;
                }
                WorkAssignment workAssignment = new WorkAssignment(jobSection.WorkRequest, roleSlotToken,
                    runningJob.JobId, runningJob.ScheduleRunDuration, runningJob.ScheduleCreatedBy, runningJob.NotificationId, runningJob.Frequency);
                EmployWorker(jobSection.WorkerCard, workAssignment, transaction);
                jobSection.WorkerIDs.Add(workAssignment.WorkerId);
            }
        }

        private void UpdateRunningJob(RunningJob runningJob, OpenJob openJob)
        {
            if (runningJob != null)
            {
                if (runningJob.Sections != null)
                {
                    foreach (JobSection jobSection in runningJob.Sections)
                    {
                        foreach (string workerID in jobSection.WorkerIDs)
                        {
                            WorkerRunner workerRunner = FindWorkerRunner(workerID);
                            if (null != workerRunner)
                            {
                                workerRunner.Command = openJob.Command;
                            }
                        }
                    }
                }
                if (openJob.Command == Command.Run)
                {
                    EmployWorkersForJob(runningJob);
                }
            }
        }

        private uint GetWorkingWorkersNumber(RoleType roleType)
        {
            uint workerCounter = 0;
            if (AllWorkerRunners != null)
                foreach (WorkerRunner workerRunner in AllWorkerRunners.Values.Where(workerRunner => workerRunner.WorkAssignment.WorkRequest.RoleType == roleType && workerRunner.IsPresent))
                {
                    workerCounter++;
                }
            return workerCounter;
        }

        private void RemoveWorkerRunnersForNotPresentWorkers()
        {
            foreach (var workerRunner in AllWorkerRunners.Values.Where(wr => !wr.IsPresent).ToList())
            {
                AllWorkerRunners.Remove(workerRunner.WorkAssignment.WorkerId);
                Tracer.Info("Removed WorkerRunner for the worker {0} as it is not present anymore.", workerRunner.WorkAssignment.WorkerId);
                if (workerRunner.QuitGracefully)
                {
                    workerRunner.CommittableTransaction.Commit();
                    //Tracer.Debug("Employment transaction commited for worker {0}.", workerRunner.WorkAssignment.WorkerId);
                }
                else
                {
                    workerRunner.CommittableTransaction.Rollback();
                    Tracer.Warning("Employment transaction rolled back for worker {0}.", workerRunner.WorkAssignment.WorkerId);
                }
                workerRunner.CommittableTransaction.Dispose();
            } 
        }

        // Returns false when runningJob is ready to be removed from the list of running jobs and disposed 
        private bool StopWorkersForJob(RunningJob runningJob)
        {
            Tracer.Info("Stopping workers for a job. PipelineId = {0}", runningJob.PipelineId);
            runningJob.State = RunningJob.States.ReadyForDispose; // Just an unconfirmed attempt so far
            if (runningJob.Sections != null)
            {
                foreach (JobSection jobSection in runningJob.Sections)
                {
                    foreach (string workerID in jobSection.WorkerIDs)
                    {
                        WorkerRunner workerRunner = FindWorkerRunner(workerID);
                        if (null == workerRunner) continue;
                        if (workerRunner.IsPresent)
                        {
                            //Tracer.Trace("StopWorkersForJob: Worker {0} is still active. Commanding it to quit.", workerRunner.WorkAssignment.WorkerId);
                            workerRunner.Command = Command.Cancel;
                            runningJob.State = RunningJob.States.WaitingForWorkersToQuit;
                        }
                        else
                        {
                            Tracer.Info("StopWorkersForJob: Worker {0} quit. Removing worker runner object.", workerRunner.WorkAssignment.WorkerId);
                            AllWorkerRunners.Remove(workerRunner.WorkAssignment.WorkerId);
                            workerRunner.CommittableTransaction.Commit();
                            workerRunner.CommittableTransaction.Dispose();
                        }
                    }
                }
            }
            //Tracer.Trace("runningJob.PipelineId = {0}, runningJob.State = {1}", runningJob.PipelineId, runningJob.State.ToString());
            if (runningJob.State == RunningJob.States.ReadyForDispose)
            {
                // TODO: Properly dispose JobSections here
                Tracer.Info("Disposed its resources pertained to job with PipelineId = {0}", runningJob.PipelineId);
                return false;
            }
            return true;
        }

        private RunningJob FindRunningJob(OpenJob openJob)
        {
            return RunningJobs.FirstOrDefault(runningJob => openJob.PipelineId == runningJob.PipelineId);
        }

        private OpenJob FindJobInfo(OpenJobs openJobs, string pipelineId)
        {
            return openJobs.Jobs.FirstOrDefault(openJob => pipelineId == openJob.PipelineId);
        }
    }
}

