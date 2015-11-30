namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// JobTaskWorkerLog.
    /// </summary> 
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute]
    [System.Xml.Serialization.XmlTypeAttribute("JobWorkerLog")]
    [System.Xml.Serialization.XmlRootAttribute]

    // Konstantin: Making this class Generic is a complete nonsense, but I have no time to fix it.
    public class JobWorkerLog<T>
    {
        private string message;
        private long jobRunId;
        private long correlationId;
        private string workerInstanceId;
        private string workerRoleType;
        private bool success;
        private T logInfo;
        private string createdBy;
        private bool isMessage;

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        /// <summary>
        /// Job Run Id
        /// </summary>
        public long JobRunId
        {
            get { return this.jobRunId; }
            set { this.jobRunId = value; }
        }


        /// <summary>
        /// Correlation Id
        /// </summary>
        public long CorrelationId
        {
            get { return this.correlationId; }
            set { this.correlationId = value; }
        }

        /// <summary>
        /// Worker run instance Id
        /// </summary>
        public string WorkerInstanceId
        {
            get { return this.workerInstanceId; }
            set { this.workerInstanceId = value; }
        }

        /// <summary>
        /// Worker role type
        /// </summary>
        public string WorkerRoleType
        {
            get { return this.workerRoleType; }
            set { this.workerRoleType = value; }
        }

        /// <summary>
        /// Status : Sucess/Failure
        /// </summary>
        public bool Success
        {
            get { return this.success; }
            set { this.success = value; }
        }


        /// <summary>
        /// Error code
        /// </summary>
        public int ErrorCode
        {
            get; set;
        }

        /// <summary>
        /// Log information
        /// </summary>
        public T LogInfo
        {
            get { return this.logInfo; }
            set { this.logInfo = value; }
        }

        /// <summary>
        /// Log Created By
        /// </summary>
        public string CreatedBy
        {
            get { return this.createdBy; }
            set { this.createdBy = value; }
        }

        /// <summary>
        /// Is Message
        /// </summary>
        public bool IsMessage
        {
            get { return this.isMessage; }
            set { this.isMessage = value; }
        }
    }
}
