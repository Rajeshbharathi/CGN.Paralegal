
namespace LexisNexis.Evolution.BatchJobs.FindReplaceRedactionXML
{
    public sealed class Constants
    {
        #region private Constructor
        private Constants()
        {
        }
        #endregion

        internal const string MarkUpBlockout = "Blockout";
        internal const string MarkupRedactionComment = "comment";
        #region Audit Log Constants

        internal const string JOB_INITIALIZATION_KEY = "Global Find and Replace Redaction reason Job Initialization";
        internal const string JOB_INITIALIZATION_VALUE = "Global Find and Replace Redaction reason Job Initialization in progress for job ID : ";
        internal const string EVENT_INITIALIZATION_EXCEPTION_VALUE = "Global Find and Replace Redaction reason - Exception in Initialize Method - ";
        internal const string AUDIT_BOOT_PARAMETER_KEY = "BootParamater";
        internal const string AUDIT_BOOT_PARAMETER_VALUE = "Boot parameter parsed successfully";
        internal const string AUDIT_GENERATE_TASK_VALUE = "Task Generation initialization";
        internal const string AUDIT_IN_DO_ATOMIC_WORK_VALUE = "In DoAtomicWork";
        internal const string EV_AUDIT_ACTUAL_STRING = "Actual String";
        internal const string EV_AUDIT_REPLACE_STRING = "Replace String";
        public const string JobEndMessage = "Completed at : ";

        #endregion

        #region Other

        internal const string JOB_NAME = "Global Find and Replace redaction Job";
        internal const string Job_TYPE_NAME = "Global Find and Replace redaction Job";

        #endregion

        #region Log COnstant

        internal const string JOB_NAME_TAG = "JobName";

        #endregion
    }
}
