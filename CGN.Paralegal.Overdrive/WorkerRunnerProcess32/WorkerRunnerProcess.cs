using System;
using System.IO;
using System.Threading;
using System.Reflection;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Overdrive
{
    using System.Diagnostics;

    class WorkerRunnerProcess
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        static int Main(string[] args)
        {
            WindowsServiceShim.Init("Overdrive.config");

            const int NRes = WorkerBase.GracefulQuitExitCode;

            if (args.Length < 3)
            {
                Tracer.Error("WorkerRunnerProcess started with less than three parameters");
                return NRes;
            }

            WorkerId = args[0];

            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = WorkerId;
            }

            WorkerAssemblyFullPath = args[1];
            WorkerTypeName = args[2];
            Tracer.Debug("WorkerRunnerProcess started for worker {0}, {1}, {2}", WorkerId, WorkerAssemblyFullPath, WorkerTypeName);

            // if (WorkerId == "LoadFileParser_LNGSEAD152422A_0") Debugger.Launch();

            string workerAssemblyDirectory = Path.GetDirectoryName(WorkerAssemblyFullPath);
            Uri workerAssemblyDirectoryUri = new Uri(workerAssemblyDirectory, UriKind.Absolute);
            WorkerFolder = workerAssemblyDirectoryUri.LocalPath;

#pragma warning disable 618 // AppendPrivatePath is deprecated but we are willing to live with it 
            AppDomain.CurrentDomain.AppendPrivatePath(WorkerFolder);
#pragma warning restore 618

            Assembly workerAssembly = Assembly.LoadFrom(WorkerAssemblyFullPath);
            if (workerAssembly == null)
            {
                Tracer.Error("WorkerRunnerProcess failed to load assembly {0}", WorkerAssemblyFullPath);
                return NRes;
            }
            object obj = workerAssembly.CreateInstance(WorkerTypeName);
            if (obj == null)
            {
                Tracer.Error("WorkerRunnerProcess failed to load type {0} from assembly {1}", WorkerTypeName, WorkerAssemblyFullPath);
                return NRes;
            }
            WorkerBaseInstance = obj as WorkerBase;
            if (WorkerBaseInstance == null)
            {
                Tracer.Error("WorkerRunnerProcess failed to cast the type {0} from assembly {1} to WorkerBase", 
                    WorkerTypeName, WorkerAssemblyFullPath);
                return NRes;
            }

            try
            {
                WorkerBaseInstance.StartWithWorkerId(WorkerId);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
            finally
            {
                IDisposable disp = WorkerBaseInstance as IDisposable;
                if (null != disp)
                {
                    disp.Dispose();
                }
            }

            Tracer.Debug("WorkerRunnerProcess (PID = {0}) is exiting with the exit code = {1}.", Process.GetCurrentProcess().Id, NRes);

            return NRes;
        }

        static public string WorkerId { get; set; }
        static public string WorkerAssemblyFullPath { get; set; }
        static public string WorkerFolder { get; set; }
        static public string WorkerTypeName { get; set; }
        static public WorkerBase WorkerBaseInstance { get; set; }
    }
}
