
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Dynamic;
using System.Text;
using NLog;

namespace CGN.Paralegal.UI.Log.Instrumentation
{
    /// <summary>
    ///     This class contains methods for instrumentation. It used NLog to write instrumentation records to CSV file.
    /// </summary>
    public class Instrumentation : IDisposable
    {
        private const string InstrumentationLogName = "PC.Web.Instrumentation";

        /// <summary>
        ///     A boolean flag indicating whether Instrumentation is on or off. By default, it is off. User can turn on
        ///     instrumentation by
        ///     setting this flag to be on.
        /// </summary>
        public static bool InstrumentationOn
        {
            get
            {
                bool instrumentationOn;
                Boolean.TryParse(ConfigurationManager.AppSettings.Get("PC.Web.InstrumentationOn"), out instrumentationOn);
                return instrumentationOn;
            }
        }


        private readonly Stopwatch _stopWatch; // used for measuring time of the operation.
        private readonly string _operationName; // Name of the operation, e.g."AddDocsToIndex"
        private readonly Object[] _args; // additional arguments to write to instrumentation file

        // Flag: Has Dispose already been called? 
        private bool _disposed;

        /// <summary>
        ///     Create an instance of instrumentation with the specified operationName.
        /// </summary>
        /// <param name="operationName">
        ///     User defined operation Name, e.g. "AddDocsToIndex". The operationName will be logged
        ///     in the CSV file along with the time it took.
        /// </param>
        /// <param name="args">Additional arguments to write to instrumentation file</param>
        public Instrumentation(string operationName, params Object[] args)
        {
            _operationName = operationName;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            if (args != null)
            {
                _args = args;
            }
        }


        /// <summary>
        ///     Start the measuring the time for the operation.
        /// </summary>
        /// <returns></returns>
        public Instrumentation Start()
        {
            _stopWatch.Start();
            return this;
        }

        /// <summary>
        ///     Stop measuring the time of the operation.
        /// </summary>
        public void Stop()
        {
            _stopWatch.Stop();
        }

        public void End(params Object[] args)
        {
            if (InstrumentationOn)
            {
                _stopWatch.Stop();
                var myargs = new List<Object>
                             {
                                 _operationName,
                                 _stopWatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                                 _stopWatch.Elapsed.TotalMinutes.ToString(CultureInfo.InvariantCulture),
                                 _stopWatch.Elapsed.TotalHours.ToString(CultureInfo.InvariantCulture)
                             };
                if (_args != null && _args.Any())
                {
                    myargs.AddRange(_args);
                }
                if (args != null && args.Any())
                {
                    myargs.AddRange(args);
                }
                string msg = ToCsvLine(myargs);
                var logger = LogManager.GetLogger(InstrumentationLogName);
                logger.Info(msg);
                _stopWatch.Reset();
            }
        }

        /// <summary>
        /// To the CSV line.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static string ToCsvLine(IEnumerable<Object> args)
        {
            var buf = new StringBuilder();
            int count = 0;
            foreach (var arg in args)
            {
                count++;
                if (count > 1)
                {
                    buf.Append(",");
                }
                buf.Append(EscapeCsvText(arg.ToString()));

            }
            return buf.ToString();
        }

        /// <summary>
        /// Escapes the CSV text.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        private static string EscapeCsvText(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            if (s.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
            {
                s = s.Replace("\r\n", "  ");
                s = s.Replace("\n", "  ");
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        #region methods in IDisposable
        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                End();
            }

            // Free any unmanaged objects here. 
            //
            _disposed = true;
        }
        #endregion

    }
}