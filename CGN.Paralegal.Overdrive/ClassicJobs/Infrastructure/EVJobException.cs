using System;
using System.Runtime.Serialization;

namespace LexisNexis.Evolution.Infrastructure.Jobs
{

    /// <summary>
    /// EvJobException
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    [Serializable]
    public class EVJobException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public EVJobException()
        {
        }

        protected EVJobException (SerializationInfo info, StreamingContext context): base(info, context)
        {
            if (info != null)
            {
                this.ErrorCode = info.GetString("EVErrorCode");
                this.LogMessge = (LogInfo)info.GetValue("LogMessage", typeof(LogInfo));
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info != null)
            {
                info.AddValue("EVErrorCode", this.ErrorCode);
                info.AddValue("LogMessage", this.LogMessge);
            }
        }        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EVJobException"/> class.
        /// </summary>
        /// <param name="erroCode">The error code.</param>
        public EVJobException(string erroCode)
        {
            ErrorCode = erroCode;
        }


        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        /// <value>The error code.</value>
        public string ErrorCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the log message.
        /// </summary>
        /// <value>The log message.</value>
        /// <remarks></remarks>
        public LogInfo LogMessge
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errorCode">Error Code</param>
        /// <param name="ex">Exception</param>
        public EVJobException(string errorCode, Exception ex)
            : base(errorCode, ex)
        {
            this.ErrorCode = errorCode;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errorCode">Resource keys to fetch Error description from resource file</param>
        /// <param name="ex">Actual Exception</param>
        /// <param name="logInfo">Log info Object</param>
        public EVJobException(string errorCode, Exception ex, LogInfo logInfo)
            : base(errorCode, ex)
        {
            this.ErrorCode = errorCode;
            logInfo.StackTrace = ex.Source + @"<br/>" + ex.Message + @"<br/>" + ex.StackTrace;
            logInfo.AddParameters(ex.Message);
            logInfo.ErrorCode = errorCode;
            logInfo.IsError = true;
            LogMessge = logInfo;
        }
    }
}
