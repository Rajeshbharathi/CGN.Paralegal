#region Header
//---------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//		Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//		<author>Kokila Bai S L</author>
//		<description>
//          This class has constant values
//		</description>
//		<changelog>
//	        <date value="10-3-2011">Updated constants file for bug fix</date>
//	        <date value="12-19-2011">Bug Fix #81330</date>
//	        <date value="02-14-2012">Fix for bugs 96511, 96517</date>
//	        <date value="02/24/2012">Fix for bugs 96992, 96996, 96998</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="10/09/2012">Task 109236 - Tag all families and Threads in results view</date>
//          <date value="07-17-2013">Bug # 147760 - Fix to tag documents in the manage conversion
//	</changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
namespace LexisNexis.Evolution.BatchJobs.ReviewerBulkTag
{
    public sealed class Constants
    {
        #region Private Constructor
        private Constants()
        {
        }
        #endregion

        #region Job related constants
        internal const string JobTypeName = "Reviewer BulkTag Job";
        internal const string JobName = "Reviewer BulkTag Job";
        internal const string JobInitializationKey = "Reviewer Bulk Tag Job Initialization";
        internal const string JobInitializationValue = "Reviewer Bulk Tag Job Initialization in progress for job ID : ";
        internal const string AuditBootParameterKey = "BootParamater";
        internal const string AuditBootParameterValue = "Boot parameter parsed successfully";
        internal const string EventXmlNotWellFormed = "Xml string is not well formed";
        internal const string EventInitializationExceptionValue = "Reviewer Bulk Tag Job - Exception in Initialize Method - ";
        internal const string AuditGenerateTaskValue = "Task Generation initialization";
        internal const string AuditInDoAtomicWorkValue = "In DoAtomicWork";
        internal const string TaskNumber = "Task Number - ";
        internal const string JobEndMessage = " - Completed at : ";
        internal const string AuditInShutdownValue = "In ShutDown";
        internal const string Duplicate = "Duplicate";
        internal const string DcnFieldType = "3000";
        internal const string LocalDomain = "N/A";
     
        /// <summary>
        /// specifies document relationship type: thread
        /// </summary>
        internal const string OutlookEmailThread = "OutlookEmailThread";
        internal const string LotusNotesEmailThread = "LotusNotesEmailThread";
        #endregion

        #region Audit Constants
        internal const string AuditUser = "User";
        internal const string AuditNumberOfDocumentsExpected = "NumberOfDocumentsExpected";
        internal const string AuditTagName = "TagName";
        internal const string AuditNumberOfDocumentsTaggedUntagged = "NumberOfDocumentsTaggedOrUntagged";
        internal const string AuditNumberOfDocumentsFailed = "NumberOfDocumentsFailed";
        internal const string AuditTagNotFound = "The specified tag could not be found";
        internal const string AuditTagDeletedHead = "NOTE ";
        internal const string AuditTagDeleted = "The specified tag has been deleted";
        internal const string AuditTagCanceled = "The specified tag has been canceled by";
        internal const string AuditTagCompleted = "The specified tag has been completed by";
        #endregion

        #region Others
        internal const string Comma = ",";
        internal const string BulkTaggingWindowSize = "BulkTaggingWindowSize";
        internal const string IndexCompletionMaxPollMinutes = "IndexCompletionMaxPollMinutes";
        internal const int DefaultWindowSize = 1000;
        internal const string Relevance = "Relevance";
        internal const string Zero = "0";
        internal const string One = "1";
        internal const string Colon = ":";
        internal const string Space = " ";
        internal const string Instance = " Instance: ";
        internal const string SpaceHiphenSpace = " - ";
        internal const string NotificationMessageHeadingForJobCancel = "Bulk Tagging Cancelled";
        internal const string NotificationMessageHeadingForUntagJobCancel = "Bulk Untagging Cancelled";
        internal const string NotificationMessageForTaggingJobCancel = "Your bulk tagging operation has been cancelled";
        internal const string NotificationMessageForUntaggingJobCancel = "Your bulk untagging operation has been cancelled";
        internal const string NotificationMessageHeadingForTagging = "Bulk Tagging Completed";
        internal const string NotificationMessageHeadingForTaggingSuspend = "Bulk Tagging Suspended";
        internal const string NotificationMessageForTagging = "Your tagging operation has completed.";
        internal const string NotificationMessageForTaggingSuspend = "Your tagging operation has been suspended";
        internal const string NotificationMessageForTaggingDocumentsTagged = "Documents Tagged: ";
        internal const string NotificationMessageForTaggingDocumentsAlreadyTagged = "Documents Already Tagged: ";
        internal const string NotificationMessageForTaggingDocumentsFailed = "Documents Failed To Tag: ";
        internal const string NotificationMessageHeadingForUntagging = "Bulk Untagging Completed";
        internal const string NotificationMessageHeadingForUntaggingSuspend = "Bulk Untagging Suspended";
        internal const string NotificationMessageForUntagging = "Your untagging operation has completed.";
        internal const string NotificationMessageForUntaggingSuspend = "Your untagging operation has been suspended";
        internal const string NotificationMessageForUntaggingDocumentsUnTagged = "Documents Untagged: ";
        internal const string NotificationMessageForUntaggingDocumentsNotUntagged = "Documents Not Untagged: ";
        internal const string NotificationMessageForNoDocumentsToTag = "No documents / tags were found to perform the bulk tagging operation";
        internal const string NotificationMessageForTagDeleted = " because the tag was deleted during the course of the bulk tagging job.";
        internal const string NotificationMessageForTagDeletedBeforeTaskStart = " because the tag was deleted before the bulk tagging job could be started.";

        internal const string HtmlBold = "<b>";
        internal const string HtmlCloseBold = "</b>";
        internal const string NotificationMessageStartTime = "Task Start Time: ";
        internal const string NotificationMessageEndTime = "Task End Time: ";
        internal const string NotificationMessageLocation = "Location: ";
        internal const string NotificationMessageTagName = "Tag Name: ";

        internal const string BackSlash = @"\\";
        internal const string EvolutionJobUpdateStatus = "STA";
        internal const int JobStatusCancelled = 8;
        internal const string DuplicateField = "duplicate";
        internal const string DcnField = "DCN";
        internal const string AxlFilePrefix = "mk-";
        internal const string AlphabetD = "D";
        internal const string XmlExtension = ".xml";
       
        #region For Tag All Duplicates
        internal const string TagAllDuplicates = "Tag All Duplicates";
        internal const string TagAllDuplicatesBehaviorName = "TAD";
        internal const string TagAllFamilyBehaviorName = "TAF";
        internal const string TagAllThreadBehaviorName = "TADF";
        #endregion

        #region For Tag All Family
        internal const string TagAllFamily = "Tag All Family";
        internal const string UntagAllFamily = "Untag All Family";
        #endregion
        internal const string TAFandTAD = "Tag all family and Tag all duplicates";

        internal const string NotApplicable = "N/A";
        internal const string ConversionReprocess = "ConversionReprocess";
        #endregion

        #region Notification Table constants
        internal const string Table = "<table>";
        internal const string Row = "<tr>";
        internal const string Column = "<td>";
        internal const string CloseColumn = "</td>";
        internal const string CloseRow = "</tr>";
        internal const string CloseTable = "</table>";
        internal const string Header = "<th align=\"left\">";
        internal const string CloseHeader = "</th>";
        #endregion
    }
}
