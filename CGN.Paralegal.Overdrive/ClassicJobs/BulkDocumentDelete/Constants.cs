#region Header
//---------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//		Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//		<author>Kokila Bai S L</author>
//		<description>
//          This class has constant values used in BulkDocumentDelete Job
//		</description>
//		<changelog>
//	        <date value="26-5-2011">Added and Updated constants file for job</date>
//	</changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
namespace LexisNexis.Evolution.BatchJobs.BulkDocumentDelete
{
    public sealed class Constants
    {
        #region Private Constructor
        private Constants()
        {
        }
        #endregion

        #region Job related constants
        internal const string JobTypeName = "Reviewer Bulk Delete Job";
        internal const string JobName = "Reviewer Bulk Delete Job";
        internal const string JobInitializationKey = "Reviewer Bulk Delete Job Initialization";
        internal const string JobInitializationValue = "Reviewer Bulk Delete Job Initialization in progress for job ID : ";
        internal const string AuditBootParameterKey = "BootParamater";
        internal const string AuditBootParameterValue = "Boot parameter parsed successfully";
        internal const string EventXmlNotWellFormed = "Xml string is not well formed";
        internal const string EventInitializationExceptionValue = "Reviewer Bulk Delete Job - Exception in Initialize Method - ";
        internal const string AuditGenerateTaskValue = "Task Generation initialization";
        internal const string AuditInDoAtomicWorkValue = "In DoAtomicWork";
        internal const string TaskNumber = "Task Number - ";
        internal const string JobEndMessage = " - Completed at : ";
        internal const string AuditInShutdownValue = "In ShutDown";
        #endregion

        #region Others
        internal const string Relevance = "Relevance";
        internal const string Colon = ":";
        internal const string Space = " ";
        internal const string Instance = " Instance: ";
        internal const string SpaceHiphenSpace = " - ";
        internal const string BulkDeleteTitle = "Bulk Deletion Completed";
        internal const string BulkDeleteMessage = "Bulk document deletion operation has completed";
        internal const string BulkDeleteSuccessCount = "Documents Deleted: ";
        internal const string BulkDeleteFailCount = "Documents Not Deleted: ";

        internal const string HtmlBold = "<b>";
        internal const string HtmlCloseBold = "</b>";
        internal const string HtmlNonBreakingSpace = "&nbsp;";
        internal const string NotificationMessageStartTime = "Task Start Time: ";
        internal const string NotificationMessageEndTime = "Task End Time: ";
        internal const string NotificationMessageLocation = "Location: ";
        #region Policy Constants
        internal const string CONTEXT = "Context";
        #endregion
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
