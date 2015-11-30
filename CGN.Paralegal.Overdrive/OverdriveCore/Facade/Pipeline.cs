using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Messaging;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Overdrive
{
    public class RolePlan
    {
        public RolePlan (RoleType roleType, string name, uint slots, List<string> outputSectionNames)
        {
            RoleType = roleType;
            Name = name;
            Slots = slots;
            OutputSectionNames = outputSectionNames;
        }

        public RolePlan(RoleType roleType, string name, List<string> outputSectionNames)
            : this(roleType, name, 1, outputSectionNames)
        {
        }

        public RoleType RoleType { get; private set; }
        public string Name { get; private set; }
        public uint Slots { get; private set; }
        public List<string> OutputSectionNames; 
    }

    public class PipelineBuildOrder
    {
        public int PipelineId { get; set; }
        public PipelineType PipelineType { get; set; }
        public List<RolePlan> RolePlans = new List<RolePlan>();
    }

    [Serializable]
    public class RoleSlotToken
    {
        public RoleSlotToken(uint slotId)
        {
            SlotId = slotId;
        }

        public uint SlotId { get; private set; }
    }

    public class WorkerStatus
    {
        public WorkerBadge WorkerBadge { get; set; }
        public WorkerState WorkerState { get; set; }
        public WorkerStatistics WorkerStatistics { get; set; }
    }

    public class PipelineSection
    {
        public PipelineSection(string machineName, uint orderNumber, PipelineType pipelineType, string pipelineId, RolePlan rolePlan)
        {
            OrderNumber = orderNumber;
            RoleType = rolePlan.RoleType;
            Name = rolePlan.Name;
            OutputSectionNames = rolePlan.OutputSectionNames ?? new List<string>();

            HiringPipeName = new HiringPipeName(machineName, pipelineId, Name);
            using (var hiringPipe = new Pipe(HiringPipeName))
            {
                hiringPipe.Create();

                for (uint slotNumber = 0; slotNumber < rolePlan.Slots; slotNumber++)
                {
                    RoleSlotToken roleSlot = new RoleSlotToken(slotNumber);
                    var envelope = new PipeMessageEnvelope()
                    {
                        Body = roleSlot,
                        Label = "RoleSlot " + slotNumber,
                    };

                    hiringPipe.Send(envelope);
                }
            }

            if (orderNumber > 0) // HACK - first role is not supposed to have data pipe
            {
                DataPipeName = new DataPipeName(machineName, pipelineType, pipelineId, Name);
                using (var dataPipe = new Pipe(DataPipeName))
                {
                    dataPipe.Create();
                }
            }
            
        }

        public void DeletePipes()
        {
            if (DataPipeName != null)
            {
                using (var dataPipe = new Pipe(DataPipeName))
                {
                    try
                    {
                        dataPipe.Delete();
                    }
                    catch (MessageQueueException)
                    {
                        // Ignore all private queues we could not delete for whatever reason
                    }
                }
            }

            if (HiringPipeName != null)
            {
                using (var hiringPipe = new Pipe(HiringPipeName))
                {
                    try
                    {
                        hiringPipe.Delete();
                    }
                    catch (MessageQueueException)
                    {
                        // Ignore all private queues we could not delete for whatever reason
                    }
                }
            }
        }

        public RoleType RoleType { get; private set; }
        public string Name { get; private set; }
        public DataPipeName DataPipeName { get; private set; }
        public HiringPipeName HiringPipeName { get; private set; }
        // WorkerId -> WorkerStatus
        public Dictionary<string, WorkerStatus> WorkerStatuses = new Dictionary<string, WorkerStatus>();
        public uint OrderNumber { get; private set; }
        public List<string> OutputSectionNames; 

        #region Statistics

        public IEnumerable<WorkerStatus> GetStatusList(string machineName)
        {
            return from workerStatus in WorkerStatuses.Values
                   where
                       workerStatus != null && workerStatus.WorkerBadge != null && workerStatus.WorkerBadge.MachineName != null &&
                       (String.IsNullOrEmpty(machineName) || workerStatus.WorkerBadge.MachineName == machineName)
                   select workerStatus;
        }

        public IEnumerable<WorkerStatistics> GetStatisticsList(string machineName)
        {
            return 
                from workerStatus in GetStatusList(machineName)
                   where
                       workerStatus.WorkerStatistics != null 
                   select workerStatus.WorkerStatistics;
        }

        public int GetNumberOfWorkersTotal(string machineName)
        {
            return GetStatusList(machineName).Count();
        }

        public int GetNumberOfBusyWorkers(string machineName)
        {
            return GetStatusList(machineName).Count(workerStatus => workerStatus.WorkerState == WorkerState.Busy);
        }

        public uint WorkersNotQuit
        {
            get { return (uint)WorkerStatuses.Values.Count(workerStatus => workerStatus.WorkerState != WorkerState.Quit); }
        }

        public uint QueueCount
        {
            get
            {
                if (DataPipeName == null) return 0;
                return DataPipeName.Count;
            }
        }

        public long GetInputTraffic(string machineName)
        {
            return GetStatisticsList(machineName).Sum(workerStatistics => workerStatistics.InputTraffic);
        }

        public long GetCompressedInputTraffic(string machineName)
        {
            return GetStatisticsList(machineName).Sum(workerStatistics => workerStatistics.CompressedInputTraffic);
        }

        private string GetHumanReadableTraffic(long traffic)
        {
            if (0 == traffic) return "0";
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB" };
            int place = Convert.ToInt32(Math.Floor(Math.Log(traffic, 1024)));
            double num = Math.Round(traffic / Math.Pow(1024, place), 1);
            string readable = num.ToString(CultureInfo.InvariantCulture) + suf[place];
            return readable;
        }

        public string GetHumanReadableTraffic(string machineName)
        {
            long inputTraffic = GetInputTraffic(machineName);
            string strTraffic = GetHumanReadableTraffic(inputTraffic);

            long compressedInputTraffic = GetCompressedInputTraffic(machineName);
            if (compressedInputTraffic != 0 && compressedInputTraffic != inputTraffic)
            {
                strTraffic += "/" + GetHumanReadableTraffic(GetCompressedInputTraffic(machineName));
            }

            return strTraffic;
        }

        public long GetNumberOfProcessedDocuments(string machineName)
        {
            return GetStatisticsList(machineName).Sum(workerStatistics => workerStatistics.ProcessedDocuments);
        }

        /// <summary>
        /// Gets the total number of documents.
        /// </summary>
        /// <param name="machineName">Name of the machine.</param>
        /// <returns>Total number of documents </returns>
        public long GetTotalNoOfDocuments(string machineName)
        {
            return GetStatisticsList(machineName).Sum(workerStatistics => workerStatistics.TotalDocuments);
        }
        private double GetSafeSeconds(TimeSpan timeSpan)
        {
            if (timeSpan.Ticks > 0)
            {
                return timeSpan.TotalSeconds;
            }

            // If given value is zero ticks we return the number of seconds which represent one single tick, which is 100 nanoseconds.
            return 1.0 / TimeSpan.TicksPerSecond;
        }

        public double GetSpeedNet(string machineName)
        {
            return GetStatisticsList(machineName).Sum(workerStatistics => workerStatistics.ProcessedDocuments/GetSafeSeconds(workerStatistics.WorkTimeNet));
        }

        public double GetSpeedGross(string machineName)
        {
            return GetStatisticsList(machineName).Sum(workerStatistics => workerStatistics.ProcessedDocuments / GetSafeSeconds(workerStatistics.WorkTimeGross));
        }

        #endregion
    }

    public enum PipelineState
    {
        Empty,
        Built,
        Running, // Pipeline started but nothing in the pipes yet
        FirstWorkerCompleted,
        Completed,
        Cancelled,
        ProblemReported,
        AllWorkersQuit
    };

    public class PipelineStatus
    {
        public PipelineStatus()
        {
            PipelineState = new PipelineState();
            ProblemReports = new List<ProblemReport>();
            WorkerMessages = new List<WorkerMessage>();
        }

        private readonly StateHolder<PipelineState> pipelineStateHolder = new StateHolder<PipelineState>();
        public PipelineState PipelineState
        {
            get
            {
                PipelineState nextState = pipelineStateHolder.State;
                return nextState;
            }
            set
            {
                // Pipeline states can only go one direction
                if (value > pipelineStateHolder.GetMostRecentState())
                {
                    pipelineStateHolder.State = value;
                }
            }
        }

        internal PipelineState GetMostRecentState()
        {
            return pipelineStateHolder.GetMostRecentState();
        }

        public List<ProblemReport> ProblemReports { get; private set; }
        public List<WorkerMessage> WorkerMessages { get; private set; }
    }

    class StateHolder<T> where T: IComparable
    {
        private T mostRecentState;
        private readonly Queue<T> previousStates = new Queue<T>();

        public T State
        {
            get
            {
                if (!previousStates.Any())
                {
                    return mostRecentState;
                }

                return previousStates.Dequeue();
            }
            set
            {
                if (value.Equals(mostRecentState))
                {
                    return;
                }

                mostRecentState = value;
                previousStates.Enqueue(value);
            }
        }

        internal T GetMostRecentState()
        {
            return mostRecentState;
        }
    }

    public class Pipeline
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        /// <remarks>Arun: Made the constructor public because Activator.CreateInstance in Director requires this.</remarks>
        public Pipeline()
        {
            PipelineStatus = new PipelineStatus { PipelineState = PipelineState.Empty };
            PipelineSections = new List<PipelineSection>();
            StatisticsAvailable = false;
        }

        public PipelineStatus PipelineStatus { get; private set; }

        public PipelineType PipelineType { get; private set; }
        public string PipelineId { get; private set; }
        public DataPipeName LogDataPipeName { get; private set; }
        public Pipe ReportPipe { get; private set; }

        public List<PipelineSection> PipelineSections { get; set; }

        // Set to true after the first stat. message from any worker
        private bool StatisticsAvailable { get; set; } 

        public void Build(PipelineBuildOrder pipelineBuildOrder)
        {
            string machineName = Environment.MachineName;
            PipelineType = pipelineBuildOrder.PipelineType;
            PipelineId = pipelineBuildOrder.PipelineId.ToString(CultureInfo.InvariantCulture);

            ReportPipeName = new ReportPipeName(machineName, PipelineType, PipelineId);
            using (ReportPipe = new Pipe(ReportPipeName))
            {
                ReportPipe.Create();
            }

            uint orderNumber = 0;
            foreach (var rolePlan in pipelineBuildOrder.RolePlans)
            {
                PipelineSection pipelineSection = new PipelineSection(machineName, orderNumber, PipelineType, PipelineId, rolePlan);
                PipelineSections.Add(pipelineSection);

                if (rolePlan.Name == "Log")
                {
                    LogDataPipeName = new DataPipeName(machineName, PipelineType, PipelineId, "Log" /* section name*/);
                }

                orderNumber++;
            }

            PipelineStatus.PipelineState = PipelineState.Built;
        }

        public List<WorkRequest> GenerateWorkRequests()
        {
            try
            {
                List<WorkRequest> workRequests = new List<WorkRequest>();

                foreach (PipelineSection pipelineSection in PipelineSections)
                {
                    WorkRequest workRequest = new WorkRequest
                        (
                        pipelineSection.Name,
                        pipelineSection.DataPipeName,
                        pipelineSection.HiringPipeName,
                        LogDataPipeName,
                        ReportPipe.Name as ReportPipeName,
                        pipelineSection.RoleType,
                        PipelineType,
                        PipelineId,
                        null
                        ) {OutputSections = new List<WorkRequest.OutputSection>()};
                    foreach (string outputSectionName in pipelineSection.OutputSectionNames)
                    {
                        PipelineSection outputPipelineSection = PipelineSections.Find(item => item.Name == outputSectionName);
                        workRequest.OutputSections.Add(new WorkRequest.OutputSection(outputSectionName, outputPipelineSection.DataPipeName));
                    }
                    workRequests.Add(workRequest);
                }
                return workRequests;
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Work request generation failed.").Trace().Swallow();
                return null; // Calling method handles this gracefully.
            }
        }

        // This is separate thread!
        public void Run()
        {
            try
            {
                PipelineStatus.PipelineState = PipelineState.Running;
                _runTime.Start();
                _timeSinceTheLastProgressReport.Start();
                bool continueMessagePumping;
                do
                {
                    PipeMessageEnvelope envelope = ReportPipe.Receive(_reportPipeTimeout);
                    lock (this)
                    {
                        UpdatePipelineState(envelope, out continueMessagePumping);
                    }
                } while (continueMessagePumping);

                ForceReportPipelineStatus("FINAL PIPELINE STATUS: ", true);
            }
            catch (Exception ex)
            {
                ex.Trace();
            }
        }

        private void UpdatePipelineState(PipeMessageEnvelope envelope, out bool continueMessagePumping)
        {
            continueMessagePumping = true;
            if (envelope != null)
            {
                ProcessReport(envelope);
                if (PipelineStatus.GetMostRecentState() == PipelineState.AllWorkersQuit)
                {
                    continueMessagePumping = false;
                }
            }
            else
            {
                // No reports to process, so we have a little idle time
                CheckPipelineForCompletion();
            }

            ReportPipelineStatus("PIPELINE STATUS: ", false);
        }

        protected virtual void PeriodicPipelineServicingHook()
        {

        }

        protected void ReportPipelineStatus(string header, bool finalStatus)
        {
            if (!StatisticsAvailable)
            {
                return; // Nothing to report yet
            }

            if (PipelineStatus.GetMostRecentState() != PipelineState.Running && 
                PipelineStatus.GetMostRecentState() != PipelineState.FirstWorkerCompleted)
            {
                // We don't need to report pipeline status unless it is Running or FirstWorkerCompleted
                return;
            }

            if (_timeSinceTheLastProgressReport.Elapsed < _speedReportInterval)
            {
                return;
            }

            ForceReportPipelineStatus(header, finalStatus);

            PeriodicPipelineServicingHook();
        }

        protected void ForceReportPipelineStatus(string header, bool finalStatus)
        {
            string reportPipeStat = String.Format("ReportPipe(Q:{0} Messages:{1}, Time:{2:0.##}, Speed:{3:0.##})",
                                                  ReportPipeName.Count,
                                                  _reportPipeMessageCounter,
                                                  _timeSinceTheFirstReportReceived.Elapsed.TotalSeconds,
                                                  _reportPipeMessageCounter /
                                                  _timeSinceTheFirstReportReceived.Elapsed.TotalSeconds);
            string pipelineStatus = header + reportPipeStat;

            List<string> machinesList = GetMachinesList();
            int maxMachineNameLength = machinesList.Max(str => str.Length);

            foreach (var machineName in machinesList)
            {
                pipelineStatus += Environment.NewLine + StatPerMachine(machineName, maxMachineNameLength);
            }

            if (machinesList.Count > 1)
            {
                pipelineStatus += Environment.NewLine + StatPerMachine(null, maxMachineNameLength);
            }

            if (finalStatus)
            {
                StatTracer.Debug(pipelineStatus);
            }
            else
            {
                StatTracer.Trace(pipelineStatus);
            }

            _timeSinceTheLastProgressReport.Restart();
        }

        private const string MachineNameDelimeter = " -";
        private string StatPerMachine(string machineName, int maxMachineNameLength)
        {
            string pipelineStatus = new string(' ', maxMachineNameLength + MachineNameDelimeter.Length);
            if (!String.IsNullOrEmpty(machineName))
            {
                string paddedMachineName = machineName.PadRight(maxMachineNameLength);
                pipelineStatus = paddedMachineName + MachineNameDelimeter;    
            }
            
            //int sectionCounter = 0;
            foreach (var pipelineSection in PipelineSections)
            {
                //if (sectionCounter % 3 == 0) pipelineStatus += Environment.NewLine;

                string sectionStatus = String.Format(" {0}(Q:{1}, T:{2}, W:{3}/{4}, D:{5}, S:{6:0.##}/{7:0.##})",
                    pipelineSection.Name, 
                    pipelineSection.QueueCount,
                    pipelineSection.GetHumanReadableTraffic(machineName),
                    pipelineSection.GetNumberOfBusyWorkers(machineName),
                    pipelineSection.GetNumberOfWorkersTotal(machineName), 
                    pipelineSection.GetNumberOfProcessedDocuments(machineName),
                    pipelineSection.GetSpeedNet(machineName),
                    pipelineSection.GetSpeedGross(machineName)
                    );
                pipelineStatus += sectionStatus;
                //sectionCounter++;
            }
            return pipelineStatus;
        }

        private List<string> GetMachinesList()
        {
            List<string> machinesList = new List<string>();
            foreach (var pipelineSection in PipelineSections)
            {
                foreach (var workerStatus in pipelineSection.WorkerStatuses.Values)
                {
                    string machineName = workerStatus.WorkerBadge.MachineName;
                    if (!machinesList.Contains(machineName))
                    {
                        machinesList.Add(machineName);
                    }
                }
            }
            //machinesList.Add("DUMMY");
            return machinesList;
        }

        protected virtual bool Completed()
        {
            // Hook for derived classes
            return true;
        }

        protected bool CheckPipelineForCompletion()
        {
            if (PipelineStatus.GetMostRecentState() != PipelineState.FirstWorkerCompleted)
            {
                return false;
            }
            
            // Debugging
            //foreach (var pipelineSection in PipelineSections)
            //{
            //    if (pipelineSection.DataPipeName != null /*&& pipelineSection.DataPipeName.Count > 0*/)
            //    {
            //        Tracer.Debug("Pipeline section {0} contains {1} messages.", pipelineSection.Name, pipelineSection.DataPipeName.Count);
            //    }
            //}

            // When in the Running state we can check all data pipes sequentially and if they all are empty this means we are done
            if (PipelineSections.Any(pipelineSection => pipelineSection.DataPipeName != null && pipelineSection.DataPipeName.Count > 0))
            {
                return false;
            }

            Tracer.Info("Pipeline detected that there are no more messages in the pipes.");
            
            try
            {
                if (!Completed())
                {
                    Tracer.Info("Custom Pipeline completion handler requested continuation of pipeline operation.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ex.Trace();
                ProblemReport problemReport = new ProblemReport("Pipeline completion", WorkerStage.EndWork, ex);
                PipelineStatus.ProblemReports.Add(problemReport);
            }

            if (PipelineStatus.ProblemReports.Any())
            {
                Tracer.Warning("Pipeline detected job completion WITH ERRORS in {0:0.##} seconds.", _runTime.Elapsed.TotalSeconds);
                PipelineStatus.PipelineState = PipelineState.ProblemReported;
                return true;
            }

            Tracer.Warning("Pipeline detected job completion without errors in {0:0.##} seconds.", _runTime.Elapsed.TotalSeconds);
            PipelineStatus.PipelineState = PipelineState.Completed;
            return true;
        }

        private void ProcessReport(PipeMessageEnvelope envelope)
        {
            if (_reportPipeMessageCounter++ == 0) _timeSinceTheFirstReportReceived.Start();

            object message = envelope.Body;

            WorkerStateChangedMessage workerStateChangedMessage = message as WorkerStateChangedMessage;
            if (workerStateChangedMessage != null) ProcessWorkerStateChangeReports(workerStateChangedMessage);

            WorkerStatistics workerStatistics = message as WorkerStatistics;
            if (workerStatistics != null) ProcessWorkerStatistics(workerStatistics);

            WorkerMessage workerMessage = message as WorkerMessage;
            if (workerMessage != null) ProcessWorkerMessage(workerMessage);

            ProblemReport problemReport = message as ProblemReport;
            if (problemReport != null) ProcessProblemReport(problemReport);
        }

        private void ProcessProblemReport(ProblemReport problemReport)
        {
            PipelineStatus.ProblemReports.Add(problemReport);
            Tracer.Error("Pipeline received problem report from worker {0} {1}: {2}", problemReport.WorkerId,
                problemReport.WorkerStage.ToString(), problemReport.ExceptionString);

            PipelineStatus.PipelineState = PipelineState.ProblemReported;
        }

        private void ProcessWorkerMessage(WorkerMessage workerMessage)
        {
            PipelineStatus.WorkerMessages.Add(workerMessage);
            Tracer.Info("Pipeline received worker message {0}", workerMessage.ToString());
        }

        private void ProcessWorkerStatistics(WorkerStatistics workerStatistics)
        {
            PipelineSection pipelineSection = FindPipelineSection(workerStatistics.WorkerBadge.SectionName);
            if (pipelineSection == null)
            {
                return;
            }

            WorkerStatus workerStatus;
            bool found = pipelineSection.WorkerStatuses.TryGetValue(workerStatistics.WorkerBadge.WorkerId, out workerStatus);
            if (found)
            {
                workerStatus.WorkerStatistics = workerStatistics;
            }
            else
            {
                workerStatus = new WorkerStatus() { WorkerStatistics = workerStatistics };
                pipelineSection.WorkerStatuses.Add(workerStatistics.WorkerBadge.WorkerId, workerStatus);
            }

            StatisticsAvailable = true;
            //Tracer.Trace("Worker {0} stat: processed {1} documents, NetTime {2:0.#} seconds, NetSpeed {3:0.##} docs/sec",
            //    workerStatistics.WorkerId,
            //    workerStatistics.ProcessedDocuments,
            //    workerStatistics.WorkTimeNet.TotalSeconds,
            //    workerStatistics.PersonalSpeedNet
            //    );
        }

        protected void ProcessWorkerStateChangeReports(WorkerStateChangedMessage message)
        {
            PipelineSection pipelineSection = FindPipelineSection(message.WorkerBadge.SectionName);
            if (pipelineSection == null)
            {
                return;
            }

            WorkerStatus workerStatus;
            bool found = pipelineSection.WorkerStatuses.TryGetValue(message.WorkerBadge.WorkerId, out workerStatus);
            if (found)
            {
                workerStatus.WorkerState = message.WorkerState;
            }
            else
            {
                workerStatus = new WorkerStatus() { WorkerBadge = message.WorkerBadge, WorkerState = message.WorkerState };
                pipelineSection.WorkerStatuses.Add(message.WorkerBadge.WorkerId, workerStatus);
                Tracer.Trace("Pipeline {0}, section {1} added new worker {2} with state {3}", 
                    PipelineId, pipelineSection.Name, message.WorkerBadge.WorkerId, workerStatus.WorkerState);
            }

            // Debug
            //Tracer.Warning("Worker " + message.WorkerBadge.WorkerId + " reported state " + message.WorkerState);

            if (pipelineSection.OrderNumber == 0 && message.WorkerState == WorkerState.Started)
            {
                Tracer.Info("Pipeline initialization completed in {0:0.##} seconds. First worker started filling up the pipe.",
                    _runTime.Elapsed.TotalSeconds);
            }

            // Handling first worker completion
            if (pipelineSection.OrderNumber == 0 && message.WorkerState == WorkerState.Completed)
            {
                // First worker reported that it generated at least one message to the pipe.
                PipelineStatus.PipelineState = PipelineState.FirstWorkerCompleted;
                CheckPipelineForCompletion();
            }

            // Handling worker quit
            if (message.WorkerState == WorkerState.Quit)
            {
                if (pipelineSection.WorkersNotQuit == 0)
                {
                    long remainingWorkers = PipelineSections.Aggregate<PipelineSection, long>(0, (current, curPipelineSection) => current + curPipelineSection.WorkersNotQuit);

                    if (remainingWorkers == 0)
                    {
                        PipelineStatus.PipelineState = PipelineState.AllWorkersQuit;
                    }
                }
            }
        }

        protected PipelineSection FindPipelineSection(string sectionName)
        {
            if (PipelineSections != null)
                foreach (PipelineSection pipelineSection in PipelineSections.Where(pipelineSection => pipelineSection.Name == sectionName))
                {
                    return pipelineSection;
                }
            Debug.Assert(false, "Pipeline received message from worker with unexpected section name");
            return null;
        }

        public void Delete()
        {
            if (ReportPipe != null) ReportPipe.Delete();

            foreach (PipelineSection pipelineSection in PipelineSections)
            {
                pipelineSection.DeletePipes();
            }

            // Just in case
            PipelineSections.Clear();
        }

        // How much time passed since Pipeline got into the Running state
        private Stopwatch _runTime = new Stopwatch();

        // How much time passed since the last time we reported Pipeline progress
        private Stopwatch _timeSinceTheLastProgressReport = new Stopwatch();
        private readonly TimeSpan _speedReportInterval = new TimeSpan(0, 0, 5); // 5 Seconds

        // How long we wait for report pipe if there is no message readily available
        private readonly TimeSpan _reportPipeTimeout = new TimeSpan(0, 0, 1); // 1 Second

        private int _reportPipeMessageCounter = 0;
        private Stopwatch _timeSinceTheFirstReportReceived = new Stopwatch();

        private ReportPipeName ReportPipeName { get; set; }

        private static readonly NamedTracer StatTracer = new NamedTracer("OverdrivePerformanceTracer");
    }
}
