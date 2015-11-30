# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file=" Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Bharani</author>
//      <description>
//          Backend process which does the  find and replace,
//                      The assembly, containing constants for the global replace job
//      </description>
//      <changelog>
//          <date value="28-Jul-2010">created</date>
//          <date value="11-04-2012">Bug Fix #98767</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion

#region Namespaces

#endregion

namespace LexisNexis.Evolution.BatchJobs.GlobalReplace
{
    public sealed class Constants
    {
        private Constants()
        {
        }

        #region Audit Log Constants

        internal const string AUDIT_BOOT_PARAMETER_KEY = "BootParamater";
        internal const string AUDIT_BOOT_PARAMETER_VALUE = "Boot parameter parsed successfully";
        internal const string EV_AUDIT_ACTUAL_STRING = "Actual String";
        internal const string EV_AUDIT_REPLACE_STRING = "Replace String";

        #endregion

        #region Other

        internal const string JOB_NAME = "Global Replace Job";
        internal const string Job_TYPE_NAME = "Global Replace Job";

        internal const string ResultsPageSize = "ResultsPageSize";
        internal const string SearchTextNotFound = "The searched text '{0}' is not found within the documents.";
        internal const string SearchTextFound = "System has completed its search of the document and has made '{0}' replacements.";
        internal const string ShutdownErrorMessage = "Shutdown Fails";
        internal const string InitializationStartMessage = "Initialization start...";
        internal const string InitializationFailMessage = "Job initialization fails";
        internal const string TaskGenerationStartedMessage = "Tasks generation Started";
        internal const string NoTaskToExecuteError = "Tasks generation Error : No Task to execute ";
        internal const string TaskGenerationCompletedMessage = "Tasks generation completed : No. of Task Generated : {0}";
        internal const string TaskGenerationFails = "Task Generation Fails";
        internal const string DoAtomicWorkStartMessage = "DoAtomic tasks started for task # : {0}";
        internal const string SearchDoneForTask = " Browse Search Result Done... for Task # : {0}";
        internal const string DoAtomicWorkCompletedMessage = "DoAtomic tasks completed for task # : {0}";
        internal const string DoAtomicWorkFailMessage = "DoAtomic task fail";
        internal const string ShutdownLogMessage = "Shutdown method : Writing audit log";
        internal const int ContentFieldType = 2000;
        internal const string OrginatorFieldName = "GolbalFindAndReplaceOrginator";


        #endregion

        #region Log COnstant

        internal const string JOB_NAME_TAG = "JobName";
        internal const string NextLineCharacter = "\n";
        internal const string CollectionId = "Collection id :";
        internal const string DocumentId = " DocumentId :";
        internal const string DoAtomicWorkNamespace = "LexisNexis.Evolution.BatchJobs.FindandReplaceJob.DoAtomicWork";
        internal const string TaskNumber = " for Task Number -  : ";
        internal const string JobSummary = "Job Summary ..";
        internal const string JobName = "Job Name";
        internal const string IndexingValidation = "Indexing Validation";
        internal const string StartIndexingValidationMessage = "Starting Indexing Validation";
        internal const string IndexingValidationCompletedMessage = "Indexing Validation Completed";

        #endregion

    }
}
