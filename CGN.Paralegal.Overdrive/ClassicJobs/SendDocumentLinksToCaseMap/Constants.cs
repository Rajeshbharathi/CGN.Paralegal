//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Malarvizhi</author>
//      <description>
//          Constants for SendDocumentLinksToCaseMap
//      </description>
//      <changelog>
//          <date value="CreatedDate"></date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="12/04/2012">Fix for bug 111563</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.BatchJobs.SendDocumentLinksToCaseMap
{
    class Constants
    {

        private Constants()
        {
        }

        #region Event Log
        internal const string JobTypeName = "SendDocumentLinksToCaseMap";
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
        internal const string SearchResultsFileType = "SearchResultsFileType";
        internal const string DocumentViewerFileType = "DocumentViewerFileType";
        internal const string NearNativeFileType = "NearNativeFileType";
        internal const string DataSetName = "DataSetName";
        internal const string JobRunId = "JobRunId";
        internal const string TaskName = "Task for sending Document links To CaseMap";
        internal const string RequestByUser = " requested By: ";
        internal const string OnDate = " started on date: ";
        internal const string ForDataset = " for the dataset: ";
        internal const string NotificationErrorMessageFormat = "{0} failed";
        internal const string NotificationSuccessMessageFormat = " {0} is now ready.{1} document(s) were included in the file.  <a target=_self href='{2}{3}&FileTypeName={4}'> Click here to download</a>";
        internal const string NotificationSuccessMessageFormatZeroDocs = "0 documents were processed in the {0}";
        internal const string SearchQuery = "SearchQuery";
        internal const string Document = "document";
        public const string LineBreak = "<br/>";
        public const string CreatedBy = "CreatedBy";
        public const string DocumentIncludedInXml = "DocumentIncludedInXml";
        public const string TaskEndTime = "Task EndTime";
        public const string TaskStartTime = "Task StartTime";
        internal const string Relevance = "Relevance";

        #region AuditLog
        internal const string TimeStamp = "TimeStamp";
        internal const string AuditUser = "Who";
        internal const string NumberOfDocuments = "NumberOfDocumentsIncluded";
        internal const string DocumentId = "DocumentId";
        internal const string XmlBlob = "XmlBlob";
        #endregion
    }
}
