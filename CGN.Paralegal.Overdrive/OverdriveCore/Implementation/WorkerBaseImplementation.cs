#define RECEIVE_WAITS

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

#endregion

namespace LexisNexis.Evolution.Overdrive
{
    using BusinessEntities;

    /// <summary>
    /// This is the worker base class (internal).
    /// </summary>
    /// <remarks></remarks>
    public abstract partial class WorkerBase : MarshalByRefObject
    {
        // http://social.msdn.microsoft.com/Forums/en-US/netfxremoting/thread/3ab17b40-546f-4373-8c08-f0f072d818c9/
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public const int GracefulQuitExitCode = 0x13131313;

        #region Fields

        protected WorkAssignment WorkAssignment { get; private set; }

        #endregion
        #region Properties

        /// <summary>
        /// Gets the worker id.
        /// </summary>
        /// <value>The worker id.</value>
        /// <remarks></remarks>

        protected string WorkerId
        {
            get
            {
                if (null == WorkAssignment)
                {
                    return "UNDEFINED";
                }
                return WorkAssignment.WorkerId;
            }
        }

        /// <summary>
        /// Gets the pipeline id.
        /// </summary>
        /// <value>The pipeline id.</value>
        /// <remarks></remarks>
        protected string PipelineId { get; private set; }

        /// <summary>
        /// Gets the type of the pipeline.
        /// </summary>
        /// <value>The type of the pipeline.</value>
        /// <remarks></remarks>
        protected PipelineType PipelineType { get; private set; }

        /// <summary>
        /// Gets the type of the role.
        /// </summary>
        /// <value>The type of the role.</value>
        /// <remarks></remarks>
        protected RoleType RoleType { get; private set; }

        /// <summary>
        /// Gets the boot parameters.
        /// </summary>
        /// <value>The boot parameters.</value>
        /// <remarks></remarks>
        protected string BootParameters { get; private set; }

        protected BaseJobBEO JobParameters { get; private set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <remarks></remarks>
        private WorkerState _state = WorkerState.Constructed;
        protected WorkerState State
        {
            get { return _state; }
            private set
            {
                if (_state == value) return;
                _state = value;
                ReportStateChange();
                HandleState(State);
            }
        }

        /// <summary>
        /// Gets or sets the input data pipe receive timeout.
        /// </summary>
        /// <value>The input data pipe receive timeout.</value>
        /// <remarks></remarks>
        protected TimeSpan InputDataPipeReceiveTimeout { get; set; }

        /// <summary>
        /// Gets or sets the time interval after which worker should send heartbeat
        /// </summary>
        protected TimeSpan StatisticsMinTimeInterval { get; set; }

        /// <summary>
        /// </summary>
        protected TimeSpan PauseSleep { get; set; }

        /// <summary>
        /// Gets the input data pipe.
        /// </summary>
        /// <value>The input data pipe.</value>
        /// <remarks></remarks>
        public Pipe InputDataPipe { get; private set; }

        private List<OutputSection> _outputSections;

        public Pipe OutputDataPipe 
        { 
            get
            {
                if (null == _outputSections || _outputSections.Count == 0)
                {
                    return null;
                }
                return _outputSections[0].DataPipe;
            }
        }

        public Pipe GetOutputDataPipe(string outputSectionName)
        {
            if (null == _outputSections || _outputSections.Count == 0)
            {
                return null;
            }

            OutputSection outputSection = _outputSections.Find(item => item.Name == outputSectionName);
            if (null == outputSection)
            {
                return null;
            }

            return outputSection.DataPipe;
        }

        public Pipe ReportPipe { get; private set; }

        public Pipe LogPipe { get; private set; }

        protected IManagerCoreServices ManagerCoreServicesProxy { get; set; }

        #endregion

        public void StartWithWorkerId(string workerId)
        {
            var managerCoreServicesClient = new ManagerCoreServicesClient();
            var managerCoreServicesProxy = managerCoreServicesClient.ManagerCoreServicesProxy;
            ManagerCoreServicesProxy = managerCoreServicesProxy;
            WorkAssignment = ManagerCoreServicesProxy.GetWorkAssignment(workerId);

            Init();

            State = WorkerState.Starting;
            try
            {
                BeginWork();
            }
            catch (Exception ex)
            {
                ProblemReport problemReport = new ProblemReport(WorkerId, WorkerStage.BeginWork, ex);
                problemReport.SendProblemReport(ReportPipe);
                ex.Trace().Swallow();

                Tracer.Debug("WorkerBase: Worker {0} is cleaning and quitting now", WorkerId);
                CleanAndQuit();
                return;
            }
            State = WorkerState.Started;

            RunWorker();

            CleanAndQuit();
        }

        private void CleanAndQuit()
        {
            State = WorkerState.CleaningUp;
            try
            {
                EndWork();
            }
            catch (Exception ex)
            {
                ProblemReport problemReport = new ProblemReport(WorkerId, WorkerStage.EndWork, ex);
                problemReport.SendProblemReport(ReportPipe);
                ex.Trace().Swallow();
            }
            State = WorkerState.Quit;

            //Tracer.Trace("Worker {0} reports Quit", WorkerId);
        }

        #region Init

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <remarks></remarks>
        private void Init()
        {
            try
            {
                InputDataPipeReceiveTimeout = new TimeSpan(0, 0, 10);
                PauseSleep = new TimeSpan(0, 0, 0, 10);

                RoleType = new RoleType(Utils.RemoveSuffix(GetType().Name, "Worker"));

                Debug.Assert(WorkerId != null);
                PipelineType = WorkAssignment.WorkRequest.PipelineType;
                PipelineId = WorkAssignment.WorkRequest.PipelineId;
                BootParameters = WorkAssignment.WorkRequest.BootParameters;
                JobParameters = WorkAssignment.WorkRequest.JobParameters;

                ReportPipe = new Pipe(WorkAssignment.WorkRequest.ReportPipeName);
                ReportPipe.Open();

                WorkerStatistics = new WorkerStatistics(WorkAssignment, Environment.MachineName, ReportPipe);

                ReportPipe.Before = WorkerStatistics.PauseNetTime;
                ReportPipe.After = WorkerStatistics.ResumeNetTime;

                SetupInputDataPipe(WorkAssignment.WorkRequest.InputDataPipeName);
                SetupOutputSections(WorkAssignment.WorkRequest.OutputSections);
                SetupLogPipe(WorkAssignment.WorkRequest.LogDataPipeName);

            }
            catch (Exception ex)
            {
                Tracer.Fatal("Unable to initialize worker {0}. Exception: {1}", WorkerId, ex);
                throw;
            }
        }

        #region Setup pipes

        #region Setup Input Data Pipe

        /// <summary>
        /// Setups the input data pipe.
        /// </summary>
        /// <param name="inputDataPipeName">Name of the input data pipe.</param>
        /// <remarks></remarks>
        private void SetupInputDataPipe(PipeName inputDataPipeName)
        {
            if (null == inputDataPipeName)
            {
                InputDataPipe = null;
                return;
            }
            InputDataPipe = new Pipe(inputDataPipeName);
            InputDataPipe.Open();

            InputDataPipe.Before = WorkerStatistics.PauseNetTime;
            InputDataPipe.After = WorkerStatistics.ResumeNetTime;
        }

        #endregion

        #region Setup Output Pipe

        /// <summary>
        /// Setups the output data pipe.
        /// </summary>
        /// <param name="outputDataPipeName">Name of the output data pipe.</param>
        /// <remarks></remarks>
        private void SetupOutputSections(List<WorkRequest.OutputSection> outputSections)
        {
            _outputSections = new List<OutputSection>();
            foreach (var workRequestOutputSection in outputSections)
            {
                OutputSection outputSection = new OutputSection(workRequestOutputSection.Name, workRequestOutputSection.DataPipeName);
                outputSection.DataPipe.Before = WorkerStatistics.PauseNetTime;
                outputSection.DataPipe.After = WorkerStatistics.ResumeNetTime;
                _outputSections.Add(outputSection);
            }
        }

        #endregion

        #region Setup Log Pipe
        private void SetupLogPipe(DataPipeName logDataPipeName)
        {
            if (null == logDataPipeName)
            {
                LogPipe = null;
                return;
            }
            LogPipe = new Pipe(logDataPipeName);
            LogPipe.Open();

            LogPipe.Before = WorkerStatistics.PauseNetTime;
            LogPipe.After = WorkerStatistics.ResumeNetTime;
        }
        #endregion

        #endregion

        #endregion

        // We have two similar workflows here:
        // One is for the first worker - it starts with GenerateMessageGrossTimed()
        // Another is for the next workers - it starts with ProcessMessageGrossTimed()

        // GenerateMessageGrossTimed - measuring gross time (includes sending) spent producing messages.
        //    GenerateMessageSafe - here exceptions are handled
        //        GenerateMessageNetTimed - here we measure the time spent in the user method alone

        // ProcessMessageGrossTimed - measuring gross time (includes receiving and sending) spent processing messages.
        //   ProcessMessageTrans - Create transaction for message receive, so that current message is still counted against the input queue
        //                         until we are fully done with it.                                  
        //      ProcessMessageSafe - here exceptions are handled.
        //          ProcessMessageNoTrans - Prevent propagation of the ambient transaction used for remote transactional receive to concrete workers 
        //                                  ProcessMessage method.
        //            ProcessMessageNetTimed - here we measure the time spent in the user method alone.

        private void RunWorker()
        {
            // This is worker life cycle
            while(true)
            {
                Command command = GetCommandFromManager();
                if (command == Command.Cancel)
                {
                    break;
                }

                if (command == Command.Pause)
                {
                    State = WorkerState.Paused;
                    Thread.Sleep(PauseSleep);
                    continue;
                }

                Guid prevActivityId = Trace.CorrelationManager.ActivityId;
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();

                bool quitLifeCycle;
                if (null == InputDataPipe)
                {
                    quitLifeCycle = GenerateMessageGrossTimed();
                }
                else
                {
                    quitLifeCycle = ProcessMessageGrossTimed();
                }

                Trace.CorrelationManager.ActivityId = prevActivityId;

                if (quitLifeCycle) break;
            }

            // Last stat for worker
            WorkerStatistics.ForceSend();
        }

        private Command GetCommandFromManager()
        {
            Command command;
            while (true)
            {
                try
                {
                    command = ManagerCoreServicesProxy.GetWorkerState(WorkerId);
                    break;
                }
                catch (TimeoutException)
                {
                    Tracer.Warning("WorkerBase: Worker {0} experienced timeout talking to WorkerManager. Retrying communication.", WorkerId);
                }
            }

            return command;
        }

        private bool GenerateMessageGrossTimed()
        {
            bool quitLifeCycle;
            try
            {
                WorkerStatistics.PunchInGross();
                quitLifeCycle = GenerateMessageSafe();
            } finally
            {
                WorkerStatistics.PunchOutGross();
            }
            return quitLifeCycle;
        }

        // This method never throws - possible exception already handled inside.
        private bool GenerateMessageSafe()
        {
            bool quitLifeCycle = false;
            try
            {
                State = WorkerState.Busy;
                bool completed = GenerateMessageNetTimed();
                WorkerStatistics.Send();
                if (completed)
                {
                    Tracer.Info("First worker {0} reports completion!", WorkerId);
                    State = WorkerState.Completed;
                    quitLifeCycle = true;
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ProblemReport problemReport = new ProblemReport(WorkerId, WorkerStage.WorkerStep, ex);
                problemReport.SendProblemReport(ReportPipe);
                quitLifeCycle = true; // Worker which caused unhandled exception should quit rather than try to process more messages
            }
            return quitLifeCycle;
        }

        private bool GenerateMessageNetTimed()
        {
            bool completed;
            try
            {
                //Tracer.Trace("First Worker {0} processing message", WorkerId);
                WorkerStatistics.PunchInNet();
                completed = GenerateMessage();
                //Tracer.Trace("First Worker {0} processed message", WorkerId);
            } finally
            {
                WorkerStatistics.PunchOutNet();
            }
            return completed;
        }

#if !RECEIVE_WAITS
        private readonly TimeSpan _zeroWait = new TimeSpan(0);
#endif

        private bool ProcessMessageGrossTimed()
        {
            bool quitLifeCycle;
            try
            {
                WorkerStatistics.PunchInGross();
                quitLifeCycle = this.ProcessMessageTrans();
            }
            finally
            {
                WorkerStatistics.PunchOutGross();
            }
            return quitLifeCycle;
        }

        private bool ProcessMessageTrans()
        {
            bool quitLifeCycle;
            using (var transaction = OverdriveTransactionScope.CreateNew())
            {
                quitLifeCycle = this.ProcessMessageSafe();
                transaction.Complete();
            }
            return quitLifeCycle;
        }

        private void SlowDownPostbacks(PipeMessageEnvelope message)
        {
            if (message.IsPostback)
            {
                TimeSpan timeSinceTheMessageWasSent = DateTime.UtcNow - message.SentTime.ToUniversalTime();
                if (timeSinceTheMessageWasSent < postbackProcessingDelay)
                {
                    int remainingTimeToWait =
                        (int)((postbackProcessingDelay - timeSinceTheMessageWasSent).TotalMilliseconds);
                    Thread.Sleep(remainingTimeToWait);
                }
            }
        }

        // This method never throws - possible exception already handled inside.
        private bool ProcessMessageSafe()
        {
            bool quitLifeCycle = false;
            try
            {
#if RECEIVE_WAITS
                PipeMessageEnvelope message = InputDataPipe.Receive(InputDataPipeReceiveTimeout);
#else
                PipeMessageEnvelope message = InputDataPipe.Receive(_zeroWait);
#endif
                if (null != message)
                {
                    State = WorkerState.Busy;
                    SlowDownPostbacks(message);
                    ProcessMessageNoTrans(message);
                }
                else
                {
                    State = WorkerState.Idle;
#if RECEIVE_WAITS
                    WorkerStatistics.RecordIdleTime(InputDataPipeReceiveTimeout);
#else
                    Thread.Sleep(InputDataPipeReceiveTimeout);    
#endif
                }
                WorkerStatistics.Send();
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ProblemReport problemReport = new ProblemReport(WorkerId, WorkerStage.WorkerStep, ex);
                problemReport.SendProblemReport(ReportPipe);
                quitLifeCycle = true; // Worker which caused unhandled exception should quit rather than try to process more messages
            }
            return quitLifeCycle;
        }

        private void ProcessMessageNoTrans(PipeMessageEnvelope message)
        {
            using (var transactionSuppression = OverdriveTransactionScope.Suppress())
            {
                ProcessMessageNetTimed(message);
                transactionSuppression.Complete();
            }
        }

        private void ProcessMessageNetTimed(PipeMessageEnvelope message)
        {
            //long envelopeSize = Utils.BinSizeOf(message);
            //long bodySize = Utils.BinSizeOf(message.Body);
            //Tracer.Trace("Worker {0} received message of the size {1}/{2}", WorkerId, envelopeSize, bodySize);
            try
            {
                //Tracer.Trace("Worker {0} processing message", WorkerId);
                WorkerStatistics.PunchInNet();
                ProcessMessage(message);
                //Tracer.Trace("Worker {0} processed message", WorkerId);
            }
            finally
            {
                WorkerStatistics.PunchOutNet();
                WorkerStatistics.RecordInputTraffic(message);
            }
        }

        private void ReportStateChange()
        {
            WorkerStateChangedMessage message = new WorkerStateChangedMessage(WorkAssignment, State);
            var envelope = new PipeMessageEnvelope()
            {
                Body = message,
                Label = WorkerStateChangedMessage.MessageLabel,
            };

            ReportPipe.Send(envelope);
        }

        protected void IncreaseProcessedDocumentsCount(int addedDocumentsCount)
        {
            if (WorkerStatistics != null)
            {
                WorkerStatistics.IncreaseProcessedDocumentsCount(addedDocumentsCount);
            }
        }

        /// <summary>
        /// Sets the total document count.
        /// </summary>
        /// <param name="totalDocumentCount">The total document count.</param>
        private void SetTotalDocumentCount(int totalDocumentCount)
        {
            if (WorkerStatistics == null) return;
            WorkerStatistics.PunchInNet();
            WorkerStatistics.SetTotalDocumentCount(totalDocumentCount);
            WorkerStatistics.PunchOutNet();
            WorkerStatistics.Send();
        }


        /// <summary>
        /// Reports the progress.
        /// </summary>
        /// <param name="totalDocumentCount">The total document count.</param>
        /// <param name="processedDocumentCount">The processed document count.</param>
        protected void ReportProgress(int totalDocumentCount, int processedDocumentCount)
        {

            IncreaseProcessedDocumentsCount(processedDocumentCount);
            SetTotalDocumentCount(totalDocumentCount);
            
        }

        public void ReportToDirector(string message)
        {
            WorkerMessage workerMessage = new WorkerMessage(WorkerId, message);
            var envelope = new PipeMessageEnvelope()
            {
                Body = workerMessage,
                Label = WorkerMessage.MessageLabel,
            };

            if (null != ReportPipe)
            {
                ReportPipe.Send(envelope);
            }
        }

        protected void ReportToDirector(string format, params Object[] args)
        {
            string message = Utils.SafeFormat(format, args);
            ReportToDirector(message);
        }

        protected void ReportToDirector(Exception ex)
        {
            ReportToDirector(ex.ToDebugString());
        }

        protected WorkerStatistics WorkerStatistics { get; set; }

        private static readonly PipelinesSharedData PipelinesSharedData = new PipelinesSharedData();

        protected PipelineProperty GetPipelineSharedProperty(string propertyName)
        {
            lock (PipelinesSharedData)
            {
                return PipelinesSharedData.GetProperty(PipelineId, propertyName);
            }
        }

        protected int ReleasePipelineSharedProperty(string propertyName)
        {
            lock (PipelinesSharedData)
            {
                return PipelinesSharedData.PropertyRelease(PipelineId, propertyName);
            }
        }

        [Serializable]
        public class OutputSection
        {
            public OutputSection(string name, DataPipeName dataPipeName)
            {
                Name = name;
                DataPipeName = dataPipeName;

                if (null != DataPipeName)
                {
                    DataPipe = new Pipe(DataPipeName);
                    DataPipe.Open();
                }
            }

            public string Name { get; private set; }
            /// <summary>
            /// Gets or sets the output data pipe name.
            /// </summary>
            /// <value>The output data pipe name.</value>
            public DataPipeName DataPipeName { get; private set; }
            public Pipe DataPipe { get; private set; }
        }


        /// <summary>
        /// Minimum time interval between checking the same document batch again
        /// </summary>
        private readonly TimeSpan postbackProcessingDelay = new TimeSpan(0, 0, 10);
    }
}
