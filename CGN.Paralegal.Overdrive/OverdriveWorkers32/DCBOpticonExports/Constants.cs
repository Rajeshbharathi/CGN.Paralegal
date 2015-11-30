//TODO - Update the Header with appropriate authorname, description and CreatedDate.

//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Thanikairajan</author>
//      <description>
//          Constants for JobName
//      </description>
//      <changelog>
//          <date value="CreatedDate"></date>
//          <date value="02-17-2012">DCB export job fix for 96454 </date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.BatchJobs.DcbOpticonExports
{
    class Constants
    {

        private Constants()
        {
        }

        internal const string Relevance = "Relevance";
        internal const string EventJobInitializationKey = "Export DCB File JobInitialization";
        internal const string Event_Job_Initialize_Start = "Job Initialize Start";
        internal const string Event_Job_Initialize_Success = "Export DCB File Job Initialize successfully";
        internal const string EventJobInitializationValue = "In Initialization method";
        internal const string ErrorAccessFolder = "Error while creating or accessing the folder.";
        internal const string GenerateTask = "Export DCB File GenerateTask";
        internal const string EventJobGenerateTaskValue = "In GenerateTask method";
        internal const string DoAutomicWork = "Export DCB File Do Atomic work";
        internal const string DoAutomicWorkValue = "In Do Atomic work";
        internal const string Shutdown = "Export DCB file Shutdown";
        internal const string ShutdownValue = "In Shutdown";
        internal const string EventGenerateTasks = "Exception in generate task";
        internal const string FilePath = "Filepath=";
        internal const string ErrorWritingBinaryFile = "Error while writing binary file error:";
        internal const string LogFileNameMissing = "Log file name is missing";
        internal const string ErrorGettingUserDetails = "Error while getting user details (Service error)";
        internal const string ErrorGettingDocumentFilterOptionNotSet = "Document filter option is not set";
        internal const string ErrorGettingDocuments = "Error while getting documents";
        internal const string ErrorGettingDocumentMetadata = "Error while getting documents metadata";
        internal const string ErrorDoAtomicWork = "Error in do atomic work method";
        internal const string ErrorShutdown = "Error in shutdown method";
        internal const string ErrorGettingDocumentBinary = "Error getting document binary (Service error)";
        internal const string ErrorGettingDocumentMetaData = "Error getting document meta data (Service error)";
        internal const int ReasonFieldType = 3001;
        internal const int DescriptionFieldType = 3002;
        internal const int ContentFieldType = 2000;
        internal const string NativeFilekey = "NativeFilekey";
        internal const string ImageFilekey = "ImageFilekey";
        internal const string PrdImgFilekey = "PrdImgFilekey";
        internal const string PrdImg = "PrdImg";
        internal const string ProductionFileType = "2";
        internal const string Native = "Native";
        internal const string NativeFileType = "2";
        internal const string Text = "Text";
        internal const string ImagesFileType = "2";
        internal const string UserError = "Error while getting user";
        internal const string DocumentError = "Error while getting document";
        internal const string WriteHelperError = "Error while writing ";
        internal const string WriteHelperErrorMsg = "binary file into source error:= ";
        internal const string SearchAndKey = " AND ";
        internal const string NotifictioninfoDataset = "Documents from Dataset ";
        internal const string NotifictioninfoLocation = " have been exported to the location ";
        internal const string NotifictioninfoExport = "as part of the Export Job:";
        internal const string SavedSearchSortOrder = "Ascending";
        internal const string SSColumn = "CreatedDate";
        internal const string SSIndex = "1";
        internal const string FileSpecialCharactersRegex = @"[\\\/:\*\?""'<>|]";

        internal const string DateFormatType1 = "mmddyyyy";
        internal const string DateFormatType2 = "ddmmyyyy";
        internal const string DateFormatType3 = "yyyymmdd";

        internal const string USCulture = "en-US";
        internal const string RUCulture = "ru-RU";
        internal const string JACulture = "ja-JP";

        internal const string QuestionMark = "?";

        #region Service call constants
        internal const int MAX_DCB_PARASIZE = 12582912;
        internal const int MAX_CONTENT_FIELDS = 5;
        #endregion

        #region Audit
        internal const string AudDatasetId = "Dataset ID";
        internal const string AudNumberOfDocumentsSuccesfull = "Number of Docs Successful";
        internal const string AudNumberOfDocumentsFailed = "Number of Docs Failed";
        internal const string AuditFailureReason = "Failure Reason";
        internal const string AuditExportJobId = "Export Job Id";
        internal const string AuditDocumentExpStatus = "Document Exported Status";
        internal const string AuditSuccess = "Success";
        internal const string AuditDocId = "Document Id";

        #endregion
    }
}


