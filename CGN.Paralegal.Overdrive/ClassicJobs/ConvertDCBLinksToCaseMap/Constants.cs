#region FileHeader
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Malar</author>
//      <description>
//          Constants for Convert DCB Links job
//      </description>
//      <changelog>
//          <date value="25-Mar-2011">Created</date>
//          <date value="30-Mar-2011">New constants added</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

namespace LexisNexis.Evolution.BatchJobs.ConvertDCBLinksToCaseMap
{
    class Constants
    {

        private Constants()
        {

        }
        #region Event Log
        internal const string JobTypeName = "DCBLinksToCaseMap";
        internal const string Event_Job_Initialize_Start = "Job Intialize Start";
        internal const string Event_Job_Initialize_Success = "Job Intialize successfully";
        internal const string Event_Job_Initialize_Failed = "Job Intialize Failed";
        internal const string Event_Job_GenerateTask_Start = "Generate Task Start";
        internal const string Event_Job_GenerateTask_Success = "Generate Task completed successfully";
        internal const string Event_Job_GenerateTask_Failed = "Generate Task Failed";
        internal const string Event_Job_DoAtomicWork_Start = "Do Atomic Work Start";
        internal const string Event_Job_DoAtomicWork_Success = "Do Atomic Work completed successfully";
        internal const string Event_Job_DoAtomicWork_Failed = "Do Atomic Work Failed";
        internal const string Event_Job_ShutDown = "Job Shut Down";
        #endregion
        internal const string XmlNotWellFormed = "Xml not well formed";
        internal const string ReviewerSearchService = "ReviewerSearchService";
        internal const string CaseMapUrl = "CaseMapUrl";
        internal const string FileType = "FileType";
        internal const string DataSetId = "DataSetId";
        internal const string DataSetName = "DataSetName";
        internal const string AuditEventId = "AuditEventId";
        internal const string JobRunId = "JobRunId";
        internal const string TaskName = "The task for mapping DCB-CaseMap links to Concordance EV has been completed";
        internal const string RequestByUser = " requested By: ";
        internal const string OnDate = " started on date: ";
        internal const string ForDataset = " for the dataset: ";
        internal const string NotificationErrorMessageFormat = "{0} failed";
        internal const string NotificationSuccessMessageFormat = " {0}. {1} document(s) were included in the file.  <a target=_self href='{2}{3}'>Ok</a>";
        internal const string SearchQuery = "SearchQuery";
        public const string LineBreak = "<br/>";
        public const string CreatedBy = "CreatedBy";
        public const string DocumentIncludedInXml = "DocumentIncludedInXml";
        public const string TaskEndTime = "Task EndTime";
        public const string TaskStartTime = "Task StartTime";

        #region AuditLog
        internal const string TimeStamp = "TimeStamp";
        internal const string AuditUser = "Who";
        internal const string NumberOfDocuments = "NumberOfDocumentsIncluded";
        internal const string DocumentId = "DocumentId";
        #endregion
    }
}
