using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using CGN.Paralegal.Infrastructure.ExceptionManagement;
using System.Reflection;

namespace CGN.Paralegal.Infrastructure
{
    using System.Configuration;

    public static class ExceptionIntercept
    {
        public static void Init()
        {
            if (initialized)
            {
                return;
            }

            string str = ConfigurationManager.AppSettings["FirstChanceExceptionIntercept"];
            // ReSharper disable once SimplifyConditionalTernaryExpression
            bool firstChanceExceptionIntercept = String.IsNullOrEmpty(str) ? false : bool.Parse(str);
            if (firstChanceExceptionIntercept)
            {
                AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
                Tracer.Debug("First chance exception intercept initialized for AppDomain {0}", AppDomain.CurrentDomain.FriendlyName);
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            initialized = true;
        }

        // If the UnhandledException event is handled in the default application domain, it is raised there for any unhandled exception in any thread, 
        // no matter what application domain the thread started in. If the thread started in an application domain that has an event handler 
        // for UnhandledException, the event is raised in that application domain. If that application domain is not the default application domain, 
        // and there is also an event handler in the default application domain, the event is raised in both application domains.
        static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Exception ex = (Exception)unhandledExceptionEventArgs.ExceptionObject;
            Tracer.Fatal("LASTChanceExceptionIntercept: {0}", ex.ToDebugString());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void CurrentDomainOnFirstChanceException(object sender, FirstChanceExceptionEventArgs firstChanceExceptionEventArgs)
        {
            if (ShallBypass())
            {
                return;
            }

            try
            {
                Exception ex = firstChanceExceptionEventArgs.Exception;
                Debug.Assert(null != ex);

                // This commented code should stay here for debugging 
                //if (ex is NullReferenceException)
                //{
                //    Tracer.Debug("NullReferenceException");
                //}

                // Skipping reporting of the known exceptions
                if (IsExceptionKnown(ex))
                {
                    return; 
                }

                if (IsExceptionOriginatedFromSystemAssembly(ex))
                {
                    Tracer.Trace("First chance exception from SYSTEM assembly: {0}", ex.ToDebugString());
                }
                else
                {
                    Tracer.Trace("First chance exception from USER assembly: {0}", ex.ToDebugString());
                }
            } catch (Exception ex)
            {
                // We must not let ANY exception to escape this handler or we get infinite recursion
                Debug.WriteLine("Exception in exception notification handler!");
                ex.Trace();
            }
        }

        private static bool IsExceptionOriginatedFromSystemAssembly(Exception ex)
        {
            if (null == ex.TargetSite) return false;
            Assembly asm = ex.TargetSite.Module.Assembly;

            object[] assemblyCompanyAttributes = asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if (assemblyCompanyAttributes.Length == 0)
            {
                // AssemblyCompany attribute is not present
                return false;
            }

            AssemblyCompanyAttribute assemblyCompany = assemblyCompanyAttributes[0] as AssemblyCompanyAttribute;

            if (null != assemblyCompany && assemblyCompany.Company.Contains("Microsoft"))
            {
                return true;
            }

            return false;
        }

        private static bool IsExceptionKnown(Exception ex)
        {
            System.Threading.ThreadAbortException threadAbortException = ex as System.Threading.ThreadAbortException;
            if (null != threadAbortException)
            {
                return true;
            }

            System.Messaging.MessageQueueException messageQueueException = ex as System.Messaging.MessageQueueException;
            if (null != messageQueueException && messageQueueException.MessageQueueErrorCode == System.Messaging.MessageQueueErrorCode.IOTimeout)
            {
                return true;
            }

            System.Xml.XmlException xmlException = ex as System.Xml.XmlException;
            if (null != xmlException && xmlException.Message.StartsWith("Name cannot begin with the '<' character"))
            {
                return true;
            }

            System.IO.FileNotFoundException fileNotFoundException = ex as System.IO.FileNotFoundException;
            if (null != fileNotFoundException && 
                null != fileNotFoundException.FileName &&
                fileNotFoundException.FileName.Contains("XmlSerializers"))
            {
                return true;
            }

            return false;
        }

        private static bool ShallBypass()
        {
            StackTrace stackTrace = new StackTrace();           // get call stack

            // This commented code should stay here for debugging 
            //Debug.WriteLine(String.Format("Dumping {0:D2} frames:", stackTrace.FrameCount));
            //for (int f = 0; f < stackTrace.FrameCount; f++)
            //{
            //    MethodBase methodBase = stackTrace.GetFrame(f).GetMethod();
            //    if (null == methodBase || null == methodBase.DeclaringType || null == methodBase.DeclaringType.FullName) continue;
            //    Debug.WriteLine(String.Format("Frame #{0:D2} = {1}.{2}", f, methodBase.DeclaringType.FullName, methodBase.Name));
            //}

            // If stack layer count less 3 , recursion impossible.
            if (stackTrace.FrameCount < 3)
            {
                return false;
            }

            IntPtr directCallerMethodHandle = stackTrace.GetFrame(1).GetMethod().MethodHandle.Value;
            for (int frame = 2; frame < stackTrace.FrameCount; frame++)
            {
                MethodBase methodBase = stackTrace.GetFrame(frame).GetMethod();

                if (null == methodBase || null == methodBase.DeclaringType || null == methodBase.DeclaringType.FullName) continue;

                if (methodBase.DeclaringType.FullName.StartsWith("NLog."))
                {
                    // This can cause infinite recursion if Glimpse intercepts it 
                    //Debug.WriteLine("NLog detected on the stack. Bypass exception interception.");
                    return true;
                }

                if (methodBase.MethodHandle.Value == directCallerMethodHandle)
                {
                    // This can cause infinite recursion if Glimpse intercepts it 
                    //Debug.WriteLine("Reentrance detected in Exception Interceptor. Probably a problem with logging configuration.");
                    return true;
                }
            }
            return false;
        }

        private static bool initialized = false;
    }
}
