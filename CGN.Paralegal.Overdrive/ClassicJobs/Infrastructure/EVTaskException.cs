using System;

namespace LexisNexis.Evolution.Infrastructure.Jobs
{
    [Serializable]
    public class EVTaskException : EVJobException
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public EVTaskException()
        {
        }

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="erroCode">ErrorCode</param>
        /// <param name="ex">Exception</param>
        public EVTaskException(string erroCode, Exception ex)
            : base(erroCode, ex)
        {
            ErrorCode = erroCode;

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errorCode">Resource keys to fetch Error description from resource file</param>
        /// <param name="ex">Actual Exception</param>
        /// <param name="logInfo">Loginfo Object</param>
        public EVTaskException(string errorCode, Exception ex, LogInfo logInfo)
            : base(errorCode, ex)
        {
            this.ErrorCode = errorCode;
            logInfo.StackTrace = ex.Source + @"<br/>" + ex.Message + @"<br/>" + ex.StackTrace;
            logInfo.ErrorCode = errorCode;
            logInfo.AddParameters(ex.Message);
            logInfo.IsError = true;
            LogMessge = logInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EVJobException"/> class.
        /// </summary>
        /// <param name="erroCode">The error code</param>
        public EVTaskException(string erroCode)
            : base(erroCode)
        {
            ErrorCode = erroCode;
        }
    }
}
