using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Transactions;
using LexisNexis.Evolution.Overdrive.DirectorCoreServices;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Overdrive
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal partial class WorkerManager : IManagerCoreServices, IDisposable
    {
        static void Main()
        {
            WindowsServiceShim.Init("Overdrive.config");
            WindowsServiceShim.PreventMultipleInstances();

            WorkerManager workerManager = new WorkerManager(WindowsServiceShim.ServiceStopCommand);
            WindowsServiceShim.Run(workerManager.LogicalEntryPoint);
        }

        protected WorkerManager(EventWaitHandle serviceStopCommand)
        {
            ServiceStopCommand = serviceStopCommand;
        }

        public void LogicalEntryPoint()
        {
            //Tracer.Trace("WorkerManager BeforeInit");
            Init();
            //Tracer.Trace("WorkerManager AfterInit");

            var stopWatch = new Stopwatch();
            while (true)
            {
                stopWatch.Restart();

                bool directorServiceFailure = false;
                try
                {
                    if (directorCoreServicesClient == null)
                    {
                        directorCoreServicesClient = new DirectorCoreServicesClient("NetTcpBinding_IDirectorCoreServices");
                    }

                    //Tracer.Info("Worker Manager polls Director for jobs");
                    OpenJobs openJobs = directorCoreServicesClient.GetOpenJobs(Environment.MachineName);

                    declaredStateOfEmergency = false;

                    if (null != openJobs)
                    {
                        //Tracer.Trace("openJobs bin size is {0}", Utils.BinSizeOf(openJobs));
                        //Tracer.Trace("WorkerManager received {0} open jobs from Director", openJobs.Jobs.Count);
                        ProcessInfoFromDirector(openJobs);
                    }
                }
                catch (TimeoutException)
                {
                    directorServiceFailure = true;
                }
                catch (CommunicationException) // EndpointNotFoundException also comes here
                {
                    directorServiceFailure = true;
                }
                catch (Exception ex)
                {
                    Tracer.Error("WorkerManager: Unhandled exception: " + ex.ToDebugString());

                    // If we want the service to stop we need to throw here and let exception be unhandled

                    Tracer.Info("WorkerManager: trying to continue after unhandled exception");
                    continue;
                }

                if (directorServiceFailure)
                {
                    Debug.Assert(directorCoreServicesClient != null, "_directorCoreServicesClient != null");
                    directorCoreServicesClient.Abort();
                    ((IDisposable)directorCoreServicesClient).Dispose();
                    directorCoreServicesClient = null;

                    //EmergencyFreeze();
                    EmergencyEvacuation();
                }

                stopWatch.Stop();
                TimeSpan getSomeRest = TimeSpan.Zero;
                if (stopWatch.Elapsed < directorPollingInterval)
                {
                    getSomeRest = directorPollingInterval - stopWatch.Elapsed;
                }

                if (ServiceStopCommand == null)
                {
                    Thread.Sleep(getSomeRest);
                }
                else
                {
                    if (ServiceStopCommand.WaitOne(getSomeRest))
                    {
                        break;
                    }
                }
            }
        }

        private bool declaredStateOfEmergency = false;

        private void EmergencyFreeze()
        {
            if (!declaredStateOfEmergency)
            {
                Tracer.Error("Director unavailable. Worker Manager pausing workers.");
                declaredStateOfEmergency = true;
            }

            if (AllWorkerRunners != null)
            {
                foreach (WorkerRunner workerRunner in AllWorkerRunners.Values.Where(workerRunner => workerRunner != null))
                {
                    workerRunner.Command = Command.Pause;
                }
            }
        }

        private void EmergencyEvacuation()
        {
            Debug.Assert(null != RunningJobs);

            if (RunningJobs.Count == 0) return;

            if (!declaredStateOfEmergency)
            {
                Tracer.Error("Director unavailable. Worker Manager starts emergency evacuation of workers.");
                declaredStateOfEmergency = true;
            }

            List<RunningJob> updatedListOfRunningJobs = RunningJobs.Where(runningJob => StopWorkersForJob(runningJob)).ToList();
            RunningJobs = updatedListOfRunningJobs;

            if (RunningJobs.Count > 0) return; // Wait for jobs to stop

            Tracer.Warning("Director unavailable. All workers successfully evacuated from the factory.");
            // All workers evacuated
        }

        internal void EmployWorker(WorkerCard workerCard, WorkAssignment workAssignment, CommittableTransaction committableTransaction)
        {
            WorkerRunner workerRunner = null;

            switch (workAssignment.WorkRequest.WorkerIsolationLevel)
            {
                case WorkerIsolationLevel.SeparateAppDomain:
                    workerRunner = new WorkerRunnerAppDomain(workerCard, workAssignment, committableTransaction);
                    break;
                case WorkerIsolationLevel.SeparateProcess:
                    workerRunner = new WorkerRunnerProcess(workerCard, workAssignment, committableTransaction);

                    // Debug
                    //workerRunner = new WorkerRunnerThread(workerCard, WorkAssignment, committableTransaction);
                    break;
                case WorkerIsolationLevel.SeparateThread:
                case WorkerIsolationLevel.Default:
                default:
                    workerRunner = new WorkerRunnerThread(workerCard, workAssignment, committableTransaction);
                    break;
            }

            try
            {
                AllWorkerRunners.Add(workAssignment.WorkerId, workerRunner);
            }
            catch (Exception ex)
            {
                Tracer.Error("AllWorkerRunners.Add({0}) caused exception: {1}", workAssignment.WorkerId, ex);
                foreach (string workerId in AllWorkerRunners.Keys)
                {
                    Tracer.Error("AllWorkerRunners contains worker {0}", workerId);                    
                }
                throw;
            }
            workerRunner.Run();
        }

        private void Init()
        {
            AllWorkerRunners = new Dictionary<string, WorkerRunner>();
            ManagerCoreServicesHost = new ManagerCoreServicesHost(this);

            RunningJobs = new List<RunningJob>();
        }

        private EventWaitHandle ServiceStopCommand { get; set; }
        private DirectorCoreServicesClient directorCoreServicesClient;
        private Dictionary<string, WorkerRunner> AllWorkerRunners { get; set; } // WorkerId to WorkerRunner
        private ManagerCoreServicesHost ManagerCoreServicesHost { get; set; }
        public List<RunningJob> RunningJobs { get; set; }

        private readonly TimeSpan directorPollingInterval = new TimeSpan(0, 0, 5); // 5 sec. 

        #region IManagerCoreServices Members

        private WorkerRunner FindWorkerRunner(string workerId)
        {
            WorkerRunner workerRunner;
            bool found = AllWorkerRunners.TryGetValue(workerId, out workerRunner);
            if (found)
            {
                return workerRunner;
            }
            return null;
        }

        WorkAssignment IManagerCoreServices.GetWorkAssignment(string workerId)
        {
            WorkerRunner workerRunner = FindWorkerRunner(workerId);
            if (null != workerRunner)
            {
                return workerRunner.WorkAssignment;
            }

            Tracer.Error("WorkerManager got GetWorkRequest from worker {0}, but it does not know about worker with that name!", workerId);
            return null;
        }

        Command IManagerCoreServices.GetWorkerState(string workerId)
        {
            WorkerRunner workerRunner = FindWorkerRunner(workerId);
            if (null != workerRunner)
            {
                return workerRunner.Command;
            }

            Tracer.Error("WorkerManager got GetWorkerState from worker {0}, but it does not know about worker with that name!", workerId);
            return Command.Cancel;
        }

        #endregion

        public void Dispose()
        {
            if (null != ManagerCoreServicesHost)
            {
                ManagerCoreServicesHost.Dispose();
            }

            if (null != directorCoreServicesClient)
            {
                directorCoreServicesClient.Close();
                directorCoreServicesClient = null;
            }
        }
    }
}
