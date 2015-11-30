using System.Text;
using System.Web.Http.ExceptionHandling;
using NLog;

namespace CGN.Paralegal.UI.Log.Exception
{
    public class WebExceptionLogger : ExceptionLogger
    {
        private static readonly Logger Nlog = LogManager.GetLogger("PCWeb");
        /// <summary>
        /// When overridden in a derived class, logs the exception synchronously.
        /// </summary>
        /// <param name="context">The exception logger context.</param>
        public override void Log(ExceptionLoggerContext context)
        {
            Nlog.Log(LogLevel.Error, ErrorDetails(context));
        }

        /// <summary>
        /// Errors the details.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private static string ErrorDetails(ExceptionLoggerContext context)
        {
            var message = new StringBuilder();
            if (context.Request.Method != null)
                message.Append(context.Request.Method);

            if (context.Request.RequestUri != null)
                message.Append(" ").Append(context.Request.RequestUri);

            message.Append(" ").Append(context.Exception);

            return message.ToString();
        }
    }
}