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
//	        <date value="18-4-2011">File Added</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="05/10/2013">BugFix 130823 - Tag delete performance issue fix</date>
//	</changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
namespace LexisNexis.Evolution.BatchJobs.BulkTagDelete
{
    public sealed class Constants
    {
        #region Private Constructor
        private Constants()
        {
        }
        #endregion

        #region Job related constants
        internal const string NotApplicable = "N/A";
        internal const string JobTypeName = "Tag Delete Job";
        internal const string JobName = "Tag Delete Job";
        internal const string JobInitializationKey = "Tag Delete Job Initialization";
        internal const string JobInitializationValue = "Tag Delete Job Initialization in progress for job ID : ";
        internal const string AuditBootParameterKey = "BootParamater";
        internal const string AuditBootParameterValue = "Boot parameter parsed successfully";
        internal const string EventXmlNotWellFormed = "Xml string is not well formed";
        internal const string EventInitializationExceptionValue = "Tag Delete Job - Exception in Initialize Method - ";
        internal const string AuditGenerateTaskValue = "Task Generation initialization";
        internal const string AuditInDoAtomicWorkValue = "In DoAtomicWork";
        internal const string TaskNumber = "Task Number - ";
        internal const string JobEndMessage = " - Completed at : ";
        internal const string AuditInShutdownValue = "In ShutDown";
        #endregion

        #region Audit Constants
        internal const string AuditUser = "User";
        internal const string AuditNumberOfDocumentsExpected = "NumberOfDocumentsExpected";
        internal const string AuditTagName = "TagName";
        internal const string AuditDocumentId = "Document ID";
        internal const string AuditNumberOfDocumentsUntagged = "NumberOfDocumentsUntagged";
        internal const string AuditNumberOfDocumentsFailed = "NumberOfDocumentsFailed";
        internal const string AuditTagNotFound = "The specified tag could not be found";
        internal const string AuditTagDeletedHead = "NOTE ";
        internal const string AuditTagDeleted = "The specified tag has been deleted";
        #endregion

        #region Others
        internal const string DcnField = "DCN";
        internal const string AxlFilePrefix = "mk-";
        internal const string AlphabetD = "D";
        internal const string XmlExtension = ".xml";
        internal const string TagFamilyReviewed = "Reviewed";
        internal const string TagFamilyNotReviewed = "Not Reviewed";
        internal const string DoubleQuote = "\"";
        internal const string Relevance = "Relevance";
        internal const string Zero = "0";
        internal const string Colon = ":";
        internal const string Space = " ";
        internal const string Instance = " Instance: ";
        internal const string SpaceHiphenSpace = " - ";
        internal const string NotificationMessageHeadingForJobCancel = "Tag Delete Cancelled";
        internal const string NotificationMessageForBulkTagDeleteJobCancel = "Your tag delete operation has been cancelled";
        internal const string NotificationMessageForBulkTagDeleteSuspend = "Your tag delete operation has been suspended";
        internal const string NotificationMessageHeadingForBulkTagDelete = "Tag Delete Completed";
        internal const string NotificationMessageHeadingForBulkTagDeleteSuspend = "Tag Delete Suspended";
        internal const string NotificationMessageForBulkTagDelete = "Your tag delete operation has been completed.";
        internal const string NotificationMessageForDocumentsUnTagged = "Documents Untagged: ";
        internal const string NotificationMessageForDocumentsNotUntagged = "Documents Not Untagged: ";
        internal const string NotificationMessageForNoTagToDelete = "No tag was found to perform the tag delete operation";
        internal const string NotificationMessageForTagDeletedBeforeTaskStart = " because the tag was already deleted before the tag delete job could be started.";
        internal const string NotificationMessageForTagDeleteFailure = " because the tag could not be deleted.";

        internal const string HtmlBold = "<b>";
        internal const string HtmlCloseBold = "</b>";
        internal const string NotificationMessageStartTime = "Task Start Time: ";
        internal const string NotificationMessageEndTime = "Task End Time: ";
        internal const string NotificationMessageLocation = "Location: ";
        internal const string NotificationMessageTagName = "Tag Name: ";

        internal const string BackSlash = @"\\";
        internal const string EvolutionJobUpdateStatus = "STA";
        internal const int JobStatusCancelled = 8;

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
