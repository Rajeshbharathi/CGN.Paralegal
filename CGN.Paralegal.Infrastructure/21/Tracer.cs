using System;
using System.Diagnostics;
using System.Threading;
using CGN.Paralegal.Infrastructure.ExceptionManagement;
using NLog;

namespace CGN.Paralegal.Infrastructure
{
    public static class Tracer
    {
        private static NamedTracer _globalTracer;
        private static NamedTracer GlobalTracer
        {
            get
            {
                return _globalTracer ?? (_globalTracer = new NamedTracer("Default"));
            }
        }

        public static void Create(string loggerName)
        {
            _globalTracer = new NamedTracer(loggerName);

            ExceptionIntercept.Init();

            Utils.RegisterProcessExitHandlerAndMakeItFirstInLineToExecute(CurrentDomainProcessExit);
        }

        static void CurrentDomainProcessExit(object sender, EventArgs e)
        {
            Info("Process ID = {0} exits.", Process.GetCurrentProcess().Id);
        }

        public static void Trace(string format, params Object[] args)
        {
            GlobalTracer.Trace(format, args);
        }

        public static void Debug(string format, params Object[] args)
        {
            GlobalTracer.Debug(format, args);
        }

        public static void Info(string format, params Object[] args)
        {
            GlobalTracer.Info(format, args);
        }

        public static void Warning(string format, params Object[] args)
        {
            GlobalTracer.Warning(format, args);
        }

        public static void Error(string format, params Object[] args)
        {
            GlobalTracer.Error(format, args);
        }

        public static void Fatal(string format, params Object[] args)
        {
            GlobalTracer.Fatal(format, args);
        }

        public static void LogException(Exception ex)
        {
            GlobalTracer.LogException(ex);
        }

        public static void LogSwallowedException(Exception ex)
        {
            GlobalTracer.LogSwallowedException(ex);
        }
    }

    public class NamedTracer
    {
        private readonly Logger loggerInstance;
        public NamedTracer(string loggerName)
        {
            loggerInstance = LogManager.GetLogger(loggerName);
        }
        public void Trace(string format, params Object[] args)
        {
            Log(LogLevel.Trace, format, args);
        }

        public void Debug(string format, params Object[] args)
        {
            Log(LogLevel.Debug, format, args);
        }

        public void Info(string format, params Object[] args)
        {
            Log(LogLevel.Info, format, args);
        }

        public void Warning(string format, params Object[] args)
        {
            Log(LogLevel.Warn, format, args);
        }

        public void Error(string format, params Object[] args)
        {
            Log(LogLevel.Error, format, args);
        }

        public void Fatal(string format, params Object[] args)
        {
            Log(LogLevel.Fatal, format, args);
        }

        private void Log(LogLevel logLevel, string format, params Object[] args)
        {
            if (null == loggerInstance)
            {
                System.Diagnostics.Debug.Assert(false, "Attempt to Log to NamedTracer before initialization.");
                return;
            }

            LogEventInfo theEvent = new LogEventInfo
            {
                TimeStamp = DateTime.Now,
                LoggerName = loggerInstance.Name,
                Level = logLevel,
                Message = format,
                Parameters = args
            };

            if (System.Diagnostics.Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                // If System.Diagnostics.Trace.CorrelationManager.ActivityId is not set, then let's set it and use it
                System.Diagnostics.Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }
            theEvent.Properties["CorrelationId"] = System.Diagnostics.Trace.CorrelationManager.ActivityId;
            SetSubprocessOriginator(theEvent);
            DebugMessageIntercept(theEvent);

            loggerInstance.Log(theEvent);
        }

        public void LogException(Exception ex)
        {
            if (null == loggerInstance)
            {
                System.Diagnostics.Debug.Assert(false, "Attempt to LogException to NamedTracer before initialization.");
                return;
            }

            ThreadAbortException threadAbortException = ex as ThreadAbortException;
            if (null != threadAbortException)
            {
                return;
            }

            LogEventInfo theEvent = new LogEventInfo
            {
                                                       TimeStamp = DateTime.Now,
                                                       LoggerName = loggerInstance.Name,
                                                       Level = LogLevel.Error, 
                                                       Message = ex.ToDebugString(), 
                                                       Exception = ex
                                                     };
            theEvent.Properties["CorrelationId"] = ex.GetCorrelationId();
            SetSubprocessOriginator(theEvent);
            DebugMessageIntercept(theEvent);

            loggerInstance.Log(theEvent);
        }

        public void LogSwallowedException(Exception ex)
        {
            if (null == loggerInstance)
            {
                System.Diagnostics.Debug.Assert(false, "Attempt to LogSwallowedException to NamedTracer before initialization.");
                return;
            }

            ThreadAbortException threadAbortException = ex as ThreadAbortException;
            if (null != threadAbortException)
            {
                return;
            }

            LogEventInfo theEvent = new LogEventInfo
            {
                                                       TimeStamp = DateTime.Now,
                                                       LoggerName = loggerInstance.Name,
                                                       Level = LogLevel.Trace, 
                                                       Message = "SWALLOWED Exception: " + ex.ToDebugString(),
                                                       Exception = ex
                                                     };
            theEvent.Properties["CorrelationId"] = ex.GetCorrelationId();
            SetSubprocessOriginator(theEvent);
            DebugMessageIntercept(theEvent);

            loggerInstance.Log(theEvent);
        }

        public bool IsDebugEnabled
        {
            get
            {
                if (null == loggerInstance)
                {
                    return false;
                }
                return loggerInstance.IsDebugEnabled;
            }
        }

        private static void SetSubprocessOriginator(LogEventInfo theEvent)
        {
            if (!String.IsNullOrEmpty(Thread.CurrentThread.Name))
            {
                theEvent.Properties["SubprocessOriginator"] = Thread.CurrentThread.Name;
                return;
            }

            theEvent.Properties["SubprocessOriginator"] = String.Format("PID = {0}", Process.GetCurrentProcess().Id);
        }

        private static void DebugMessageIntercept(LogEventInfo theEvent)
        {
            //if (theEvent.Message.Contains("$$$"))
            //{
            //    System.Diagnostics.Debug.WriteLine("DebugMessageIntercept 1");
            //}

            //if (null == theEvent.Parameters)
            //{
            //    return;
            //}

            //foreach(var param in theEvent.Parameters)
            //{
            //    if (param.ToString().Contains("$$$"))
            //    {
            //        System.Diagnostics.Debug.WriteLine("DebugMessageIntercept 2");
            //    }
            //}
        }
    }
}
