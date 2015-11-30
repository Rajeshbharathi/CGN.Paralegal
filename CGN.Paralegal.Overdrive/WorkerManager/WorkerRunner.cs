using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using System.Transactions;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Overdrive
{
    internal abstract class WorkerRunner
    {
        internal WorkerRunner(WorkerCard workerCard, WorkAssignment workAssignment, CommittableTransaction committableTransaction)
        {
            WorkerCard = workerCard;
            WorkAssignment = workAssignment;
            CommittableTransaction = committableTransaction;
            Command = Command.Run;
            quitGracefully = false;
        }

        public virtual void Run()
        {
            string assemblyDirectory = Path.GetDirectoryName(WorkerCard.AssemblyPath);
            Uri assemblyDirectoryUri = new Uri(assemblyDirectory, UriKind.Absolute);
            WorkerFolder = assemblyDirectoryUri.LocalPath;
            //Tracer.Trace("WorkerRunner is starting workerId = {0}, Role = {1}, Path = {2}", WorkerId, WorkRequest.RoleType.ToString(), WorkerFolder);
        }

        public WorkerCard WorkerCard { get; private set; }
        public WorkAssignment WorkAssignment { get; private set; }
        public CommittableTransaction CommittableTransaction { get; private set; }

        public Command Command { get; set; }

        public abstract bool IsPresent { get; }

        private bool quitGracefully;
        public virtual bool QuitGracefully
        {
            get
            {
                return this.quitGracefully;
            }
            protected set
            {
                this.quitGracefully = value;
            }
        }

        protected string WorkerFolder { get; private set; }

        private static string _workerManagerFolder;

        public static string WorkerManagerFolder
        {
            get
            {
                if (_workerManagerFolder == null)
                {
                    string workerManagerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                    Uri workerManagerDirectoryUri = new Uri(workerManagerDirectory, UriKind.Absolute);
                    _workerManagerFolder = workerManagerDirectoryUri.LocalPath;
                }
                return _workerManagerFolder;
            }
        }
    }

    internal class WorkerRunnerThread : WorkerRunner
    {
        internal WorkerRunnerThread(WorkerCard workerCard, WorkAssignment workAssignment, CommittableTransaction committableTransaction)
            : base(workerCard, workAssignment, committableTransaction)
        {
        }

        public override void Run()
        {
            base.Run();

            InstantiateWorker();

            if (null == WorkerBaseInstance)
            {
                return;
            }
            WorkerThread = new Thread(Start);
            WorkerThread.Name = WorkAssignment.WorkerId;
            WorkerThread.Start();

            Tracer.Info("Started Worker " + WorkAssignment.WorkerId + " for PipelineId " + WorkAssignment.WorkRequest.PipelineId);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        protected virtual void InstantiateWorker()
        {
#pragma warning disable 618 // AppendPrivatePath is deprecated but we are willing to live with it
            AppDomain.CurrentDomain.AppendPrivatePath(WorkerFolder);
#pragma warning restore 618

            Assembly workerAssembly = Assembly.LoadFrom(WorkerCard.AssemblyPath);
            if (workerAssembly == null)
            {
                Tracer.Error("Failed to load assembly {0}", WorkerCard.AssemblyPath);
                return;
            }

            object obj = null;
            try
            {
                obj = workerAssembly.CreateInstance(WorkerCard.TypeName);
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to load type {0} from assembly {1}. Exception: {2}",
                    WorkerCard.TypeName, WorkerCard.AssemblyPath, ex);
                return;
            }
            if (obj == null)
            {
                Tracer.Error("Failed to load type {0} from assembly {1}", WorkerCard.TypeName, WorkerCard.AssemblyPath);
                return;
            }

            WorkerBaseInstance = obj as WorkerBase;
            if (WorkerBaseInstance == null)
            {
                Tracer.Error("Failed to cast the type {0} from assembly {1} to WorkerBase",
                    WorkerCard.TypeName, WorkerCard.AssemblyPath);
                return;
            }
        }

        // This method runs on its own thread
        private void Start()
        {
            try
            {
                WorkerBaseInstance.StartWithWorkerId(WorkAssignment.WorkerId);

                QuitGracefully = true;
            }
            catch (Exception ex)
            {
                Tracer.Error("Unhandled exception in worker {0}. Exception: {1}", WorkAssignment.WorkerId, ex.ToDebugString());
            }
            finally
            {
                IDisposable disp = WorkerBaseInstance as IDisposable;
                if (null != disp)
                {
                    disp.Dispose();
                }
            }
        }

        public override bool IsPresent
        {
            get
            {
                if (WorkerThread == null) return false;
                return WorkerThread.IsAlive;
            }
        }

        protected WorkerBase WorkerBaseInstance { get; set; }
        protected Thread WorkerThread { get; set; }
    }

    internal class WorkerRunnerAppDomain : WorkerRunnerThread
    {
        internal WorkerRunnerAppDomain(WorkerCard workerCard, WorkAssignment workAssignment, CommittableTransaction committableTransaction)
            : base(workerCard, workAssignment, committableTransaction)
        {
        }

        protected override void InstantiateWorker()
        {
            AppDomainSetup appDomainSetup = new AppDomainSetup();
            Evidence adevidence = AppDomain.CurrentDomain.Evidence;

            appDomainSetup.ApplicationBase = WorkerManagerFolder;
            appDomainSetup.PrivateBinPath = WorkerFolder;

            appDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            appDomainSetup.ApplicationName = WorkAssignment.WorkerId;
            AppDomain appDomain = AppDomain.CreateDomain(WorkAssignment.WorkerId, adevidence, appDomainSetup);
            if (appDomain == null)
            {
                Tracer.Error("Failed to CrateDomain for worker {0}", WorkAssignment.WorkerId);
                return;
            }

            object obj = null;
            try
            {
                obj = appDomain.CreateInstanceFromAndUnwrap(WorkerCard.AssemblyPath, WorkerCard.TypeName);
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to load type {0} from assembly {1}. Exception: {2}", WorkerCard.TypeName, WorkerCard.AssemblyPath, ex);
                return;
            }
            if (obj == null)
            {
                Tracer.Error("WorkerManager failed to load type {0} from assembly {1}", WorkerCard.TypeName, WorkerCard.AssemblyPath);
                return;
            }

            WorkerBaseInstance = obj as WorkerBase;
            if (WorkerBaseInstance == null)
            {
                Tracer.Error("Failed to cast the type {0} from assembly {1} to WorkerBase",
                    WorkerCard.TypeName, WorkerCard.AssemblyPath);
                return;
            }
        }
    }

    internal class WorkerRunnerProcess : WorkerRunner
    {
        internal WorkerRunnerProcess(WorkerCard workerCard, WorkAssignment workAssignment, CommittableTransaction committableTransaction)
            : base(workerCard, workAssignment, committableTransaction)
        {
        }

        public override void Run()
        {
            base.Run();

            string workerRunnerProcessFolder = Path.Combine(WorkerManagerFolder, @".");
            Uri workerRunnerProcessFolderUri = new Uri(workerRunnerProcessFolder, UriKind.Absolute);
            workerRunnerProcessFolder = workerRunnerProcessFolderUri.LocalPath;
            string workerRunnerProcessExeFullPath = Path.Combine(workerRunnerProcessFolder, WorkerRunnerProcessExeName);

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = workerRunnerProcessExeFullPath;
            processStartInfo.WorkingDirectory = WorkerFolder;
            processStartInfo.Arguments =
                WorkAssignment.WorkerId + 
                " " +
                "\"" + WorkerCard.AssemblyPath + "\"" +
                " " +
                WorkerCard.TypeName;

            // Process invisibility
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                WorkerProcess = Process.Start(processStartInfo);
                Tracer.Info("Started WorkerRunnerProcess Id = {0} for worker {1} for PipelineId {2}",
                    WorkerProcess.Id, WorkAssignment.WorkerId, WorkAssignment.WorkRequest.PipelineId);
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Failed to create WorkerRunnerProcess for worker {0}", WorkAssignment.WorkerId);
                ex.Trace().Swallow();
            }
        }

        public override bool IsPresent
        {
            get
            {
                if (WorkerProcess == null) return false;
                return !WorkerProcess.HasExited;
            }
        }

        public override bool QuitGracefully
        {
            get
            {
                if (WorkerProcess == null) return false;

                return WorkerProcess.HasExited;

                // In some cases with failed PST import ExitCode here is always zero, so we can't rely on it.
                // For now we just consider any worker runner process exit as graceful to prevent endless respawning 
                // of the worker processes in case of error
                //if (WorkerProcess.HasExited && WorkerProcess.ExitCode == WorkerBase.GracefulQuitExitCode) return true;
                //return false;
            }
        }

        protected Process WorkerProcess { get; set; }
        internal const string WorkerRunnerProcessExeName = "WorkerRunnerProcess32.exe";
    }
}
