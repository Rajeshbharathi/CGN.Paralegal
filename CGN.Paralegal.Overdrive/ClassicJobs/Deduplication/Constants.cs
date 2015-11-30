#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Deepthi Bitra</author>
//      <description>
//          This file has constants related to Deduplication Job
//      </description>
//      <changelog>
//          <date value="1/4/2011">Created</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

namespace LexisNexis.Evolution.BatchJobs.Deduplication
{
    public sealed class Constants
    {
        #region private Constructor

        private Constants()
        {
        }
        #endregion

        internal const string JobTypeName = "DeDuplication Job";
        internal const string JobName = "DeDuplication Job";
        internal const string Algorithm_MD5 = "MD5";
        internal const string Algorithm_SHA1 = "SHA1";
        internal const string CompareType_Fields = "Fields";
        internal const string CompareType_OriginalDocuments = "Original Document";

        internal const string Action_Delete_Type = "Delete";
        internal const string Action_Tag_Type = "Tagging";
        internal const string Action_GROUP_Type = "Grouping";

        internal const string DUP_GROUP_ORGDOC_MD5 = "Group_OriginalDoc_MD5";
        internal const string DUP_GROUP_ORGDOC_SHA1 = "Group_OriginalDoc_SHA1";

        internal const string Search_Sub_System_Dup_Key_Name = "Duplicate";


        internal const string DUP_FIELDS = "Fields";
        internal const string Dup_FieldsOrAlgorithm = "Fields/Algorithm";
        internal const string EV_COMMA = ",";
        internal const string Dtnsfr_Aud_Dataset = "Dataset";

        #region Event Log Constants
        internal const string EVENT_JOB_INITIALIZATION_KEY = "Deduplication JobInitialization :Xml string is not well formed";
        internal const string EVENT_JOB_INITIALIZATION_VALUE = "Deduplication JobInitialization";
        internal const string EVENT_JOB_GENERATETASK_VALUE = "Deduplication GenerateTask";
        internal const string EVENT_INITIALIZATION_EXCEPTION_VALUE = "Deduplication Exception Initialize Method - ";
        internal const string EVENT_GENERATE_TASK_KEY = "Deduplication - Generate Task";
        internal const string EVENT_JOB_SHUTDOWN_KEY = "Deduplication Job Shutdown";
        internal const string EVENT_GENERATE_TASKS_EXCEPTION_VALUE = "Exception in Deduplication GenerateTasks Method";
        internal const string EVENT_DO_ATOMIC_WORK_EXCEPTION_VALUE = "Deduplication Exception DoAtomicWork Method - ";
        internal const string DO_ATOMIC_TASK = "DoAtomicTask";
        internal const string DO_ATOMIC_ERR_DEL_DUP_DOC_VAULT = "Error in delete duplicate document in Vault";
        internal const string DoAtomicErrDelDupDocSearch = "Error in deleting duplicate document from search sub-system";
        internal const string DO_ATOMIC_ERR_GET_COL_FIELD = "Error in get collection fields";
        internal const string DO_ATOMIC_ERR_INS_DUP_FIELD_VAULT = "Error in insert duplicate fields value in vault";
        internal const string DoAtomicErrInsDupFieldSearch = "Error in insert duplicate fields value into search sub-system";
        #endregion

        internal const string EV_AUDIT_DEDUPE_ACTION = "DeduplicationAction";
        internal const string EV_AUDIT_DEDUPE_METHOD = "Method";
        internal const string FAILED_TASK = "FailedTask";
        internal const string AUDIT_FAILED_TASK_HANDLER_VALUE = "Handler for Failed task";

        #region Deduplication Constants
        internal const string ErrorForDocumentHashValues = "Error while getting document hash values";
        internal const string ErrorForOriginalDocuments = "Error while getting original documents count";
        internal const string GenerateTaskGrouping = "Generated multiple tasks for grouping";
        internal const string GenerateTaskNoDuplicates = "GenerateTask :No duplicates";
        internal const string ErrorForBatesNumber = "GenerateTask :Error while getting Bates Number";
        internal const string GenerateTaskDelete = "Generated multiple tasks for delete operation";
        internal const string ErrorForOriginalDocument = "Error while getting original documents list";
        internal const string ErrorForOriginalDocumentForDelete = "Error while getting original document list for delete operation";
        internal const string ErrorForDuplicateDocumentsCount = "Error while getting duplicate documents count";
        internal const int JobCompleted = 5;
        #endregion
    }
}
