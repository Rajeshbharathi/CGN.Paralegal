using System.Diagnostics;

namespace CGN.Paralegal.Infrastructure
{
    public class EvLog
    {
        /// <summary>
        /// DEPRECATED! DO NOT USE! Use Tracer instead.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        public static void WriteEntry(string source, string message)
        {
            WriteEntry(source, message, EventLogEntryType.Information);
        }

        /// <summary>
        /// DEPRECATED! DO NOT USE! Use Tracer instead.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public static void WriteEntry(string source, string message, EventLogEntryType type)
        {
            string fullMessage = source + ": " + message;

            switch (type)
            {
                case EventLogEntryType.Information:
                    Tracer.Info(fullMessage);
                    break;

                    case EventLogEntryType.Warning:
                    Tracer.Warning(fullMessage);
                    break;

                    case EventLogEntryType.Error:
                    Tracer.Error(fullMessage);
                    break;

                default:
                    Tracer.Error(fullMessage);
                    break;
            }
        }
    }
}
