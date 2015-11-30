using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

using NLog;

namespace CGN.Paralegal.Infrastructure
{
    using CGN.Paralegal.Infrastructure.ExceptionManagement;

    public static class WindowsServiceShim
    {
        public static void Run(Action logicalEntryPoint)
        {
            if (RunAsService)
            {
                ShimmedService.LogicalEntryPoint = logicalEntryPoint;
                ServiceBase[] servicesToRun = { ShimmedService };
                ServiceBase.Run(servicesToRun);
            }
            else
            {
                // Execute as console application
                logicalEntryPoint();
            }
        }

        public static void Init()
        {
            // Check for null here is necessary, because thread name can only be set once and unit test runner tends to do so before us
            // We better not set it here at all to let WorkerRunnerProcess32 to set it.
            //if (Thread.CurrentThread.Name == null)
            //{
            //    Thread.CurrentThread.Name = "Main thread";
            //}

            Tracer.Create(Process.GetCurrentProcess().ProcessName);

            DebugConsole.AllocateConsoleIfNecessary();

            Tracer.Info("Process {0} with ID {1} starts.", Process.GetCurrentProcess().ProcessName, Process.GetCurrentProcess().Id);

            RunAsService = ParentProcessFinder.RunAsService();
            if (RunAsService)
            {
                ShimmedService = new ShimmedService { ServiceName = Process.GetCurrentProcess().ProcessName };
            }
        }

        public static void Init(string configFileName)
        {
            try
            {
                if (!String.IsNullOrEmpty(configFileName))
                {
                    SetConfigFile(configFileName);
                }
                Init();
            }
            catch (Exception ex)
            {
                ex.Trace();
                Environment.Exit(-2);
            }
        }

        private static void SetConfigFile(string configFileName)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string location = currentAssembly.Location;
            string strFolder = Path.GetDirectoryName(location);
            Debug.Assert(strFolder != null, "strFolder != null");
            string strConfigFilePath = Path.Combine(strFolder, configFileName);

            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", strConfigFilePath);
            typeof(ConfigurationManager).GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0);
            typeof(ConfigurationManager).GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
            typeof(ConfigurationManager).Assembly.GetTypes().Where(x => x.FullName == "System.Configuration.ClientConfigPaths").First().GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(strConfigFilePath);
        }

        public static void PreventMultipleInstances()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                string message = String.Format("Detected an attempt to start {0} (Process ID = {1}) while another instance is already running. Quitting.",
                    Process.GetCurrentProcess().ProcessName, Process.GetCurrentProcess().Id);
                Tracer.Fatal(message);
                Environment.Exit(-1);
            }
        }

        public static bool RunAsService { get; private set; }
        public static ShimmedService ShimmedService { get; private set; }
        public static Action LogicalEntryPoint {
            set
            {
                if (ShimmedService != null) ShimmedService.LogicalEntryPoint = value;
            }
        }
        public static EventWaitHandle ServiceStopCommand
        {
            get
            {
                return ShimmedService != null ? ShimmedService.ServiceStopCommand : null;
            }
        }
    }

    public sealed class ShimmedService : ServiceBase
    {
        public ShimmedService()
        {
            ServiceStopCommand = new EventWaitHandle(false, EventResetMode.ManualReset);

            EventLog.Source = Process.GetCurrentProcess().ProcessName;
            if (!EventLog.SourceExists(EventLog.Source))
            {
                EventLog.CreateEventSource(EventLog.Source, "Application");
            }
        }

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();
            if (null != ServiceStopCommand)
            {
                ServiceStopCommand.Reset();
            }

            windowsServiceThread = new Thread(new ThreadStart(LogicalEntryPoint)) { Name = "Windows service thread" };
            windowsServiceThread.Start();
        }

        protected override void OnStop()
        {
            if (null != ServiceStopCommand)
            {
                ServiceStopCommand.Set();
            }

            if (windowsServiceThread != null)
            {
                windowsServiceThread.Join();
            }
        }

        internal EventWaitHandle ServiceStopCommand { get; private set; }
        internal Action LogicalEntryPoint { get; set; }
        private Thread windowsServiceThread;
    }

    /// <summary>
    /// A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParentProcessFinder
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessFinder processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            Process process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessFinder pbi = new ParentProcessFinder();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }

        public static bool RunAsService()
        {
            Process parentProcess = GetParentProcess();
            //Tracer.Trace("Parent Process name is " + parentProcess.ProcessName);
            if (parentProcess.ProcessName == "services")
            {
                return true;
            }
            return false;
        }
    }

    public static class DebugConsole
    {
        static DebugConsole()
        {
            Available = false;
        }

        public static bool Available { get; private set; }

        /// <remarks>
        /// 
        ///  USAGE: Place inside your program's main static class
        ///  and call AllocateConsole whenever you want.
        /// </remarks>

        /// <summary>
        /// allocates a new console for the calling process.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. 
        /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        /// <summary>
        /// Detaches the calling process from its console
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. 
        /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();
        /// <summary>
        /// Attaches the calling process to the console of the specified process.
        /// </summary>
        /// <param name="dwProcessId">[in] Identifier of the process, usually will be ATTACH_PARENT_PROCESS</param>
        /// <returns>If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. 
        /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AttachConsole(uint dwProcessId);
        /// <summary>Identifies the console of the parent of the current process as the console to be attached.
        /// always pass this with AttachConsole in .NET for stability reasons and mainly because
        /// I have NOT tested interprocess attaching in .NET so dont blame me if it doesnt work! </summary>
        const uint ATTACH_PARENT_PROCESS = 0xffffffff;
        /// <summary>
        /// calling process is already attached to a console
        /// </summary>
        const int ERROR_ACCESS_DENIED = 5;
        /// <summary>
        /// Allocate a console if application started from within windows GUI. 
        /// Detects the presence of an existing console associated with the application and
        /// attaches itself to it if available.
        /// </summary>
        public static void AllocateConsoleIfNecessary()
        {
            if (Debugger.IsAttached)
            {
                Available = false;
                return; // Don't create console under debugger
            }

            if (ParentProcessFinder.RunAsService())
            {
                Available = false;
                return; // Don't create console if running as a service
            }

            ForceAllocateConsole();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke")]
        public static void ForceAllocateConsole()
        {

            // Try to attach to existing console
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                Available = true;
                return; // Success - using existing console
            }

            int lastError = Marshal.GetLastWin32Error();
            //MessageBox.Show("GetLastError " + lastError);

            // FYI: Environment.UserInteractive == false for service, but 
            // it is true if the service has "Allow service to interact with desktop" checked

            // Console does not exists - try create a new one
            if (AllocConsole())
            {
                Available = true;
                return; // Success - created new console
            }

            //int lastError = Marshal.GetLastWin32Error();
            Available = false;
            return; // Continue without console
        }
    }
}
