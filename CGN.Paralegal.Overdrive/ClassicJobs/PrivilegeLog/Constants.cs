//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Vikash Gupta</author>
//      <description>
//          Constants for Privilege Log Job
//      </description>
//      <changelog>
//          <date value="CreatedDate"></date>
//          <date value="20/02/2012">97039- BVT issue fix</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/26/2013">BugFix#144515</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.BatchJobs.PrivilegeLog
{
    /// <summary>
    /// Constants for privilege log job
    /// </summary>
    public static class Constants
    {
        #region Event Log Constants
        internal const string Relevance = "Relevance";
        internal const string EventJobInitializationKey = "PrivilegeLog JobInitialization";
        internal const string Event_Job_Initialize_Start = "Job Intialize Start";
        internal const string Event_Job_Initialize_Success = "Job Intialize successfully";
        internal const string EventJobInitializationValue = "In Initialaization method";
        internal const string EventXmlNotWellFormedValue = "Xml string is not well formed. Unable to recreate object.";
        internal const string GenerateTask = "PrivilegeLog GenerateTask";
        internal const string EventJobGenerateTaskValue = "In GenerateTask method";
        internal const string EventGenerateTasksAddDocumentTaskValue = "Exception in adding document to the task";
        internal const string EventLogFolderPathMissing = "Log folder path missing";
        internal const string EventLogFolderPathNotSpecified = "Log folder path not specified";

        internal const string LogFileNameMissing = "Log file name is missing";
        internal const string LogFilehasInvalidCharacters = "File name has some invalid characters";
        internal const string ErrorGettingDatasetDetails = "Error in getting dataset detail for a dataSet id (Service error)";
        internal const string ErrorGettingDatasetDetailsNullReturned = "GetDataSetDetailForDataSetId returned a null value.Specified dataset may not exist";
        internal const string ErrorGettingCollectionMatterDetails = "Error while getting collection and matter details";
        internal const string ErrorGettingAllTags = "Error while getting all tags (Service error)";
        internal const string ErrorGettingAllReasonForRedaction = "Error while getting all reason for redaction (Service error)";
        internal const string ErrorFetchingHeaderDetails = "Error while fetching header details";
        internal const string ErrorCreatingCsvFile = "Error creating csv file";
        internal const string ErrorGettingUserDetails = "Error while getting user details (Service error)";
        internal const string ErrorGettingAllSavedSearch = "Error while getting all saved search (Service error)";
        internal const string ErrorGettingDocumentFilterOptionNotSet = "Document filter option is not set";
        internal const string ErrorGettingDocuments = "Error while getting documents";
        internal const string ErrorGettingDocumentFieldData = "Error while getting document field data (Service error)";
        internal const string ErrorGettingDocumentTags = "Error while getting document tag data (Service error)";
        internal const string ErrorGettingDocumentFieldValues = "Error while getting document field values";
        internal const string ErrorGettingDocumentTagValues = "Error while getting document tag values";
        internal const string ErrorGettingDocumentRedactionXml = "Error while getting document redaction xml (Service error)";
        internal const string ErrorGettingDocumentRedactionReasonValues = "Error while getting document redaction reason values";
        internal const string ErrorInInitialize = "Error in initialize method";
        internal const string ErrorGenerateTask = "Error in generate task method";
        internal const string ErrorDoAtomicWork = "Error in do atomic work method";
        internal const string ErrorShutdown = "Error in shutdown method";
        internal const string ErrorGettingDocumentProductionFieldValues = "Error while getting document production field values";

        internal const string DocumentNotExists = "Document does not exists";
        internal const string CSVNotCreated = "Problem in CSV file creation";
        #endregion

        internal const string JobTypeName = "Privilege Log Job";
        internal const string JobName = "Privilege Log Job";

        internal const int FieldTypeIdForPrivilegeField1 = 3001; //Reason Code
        internal const int FieldTypeIdForPrivilegeField2 = 3002; //Document description
        internal const string XpathPrevilegePage = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='Blockout']/@comment";
        internal const string xmlVersionString = @"<?xml version=\""1.0\"" encoding=\""UTF-8\""?>";
        internal const string XmlNamespace = "http://www.infograph.com";
        internal const string XmlNamespacePrefix = "in";

        internal const string BackwardSlash = @"\";
        internal const string LogDocumentLabel = "Privilege Log File Path Selected:";

        internal const string JobStartedMessage = "Job started.";
        internal const string TaskCompletedMessage = "Task completed for 1 document. Task#";
        internal const string JobEndedMessage = "Job ended.";

        internal const string StartingBatesFieldName = "StartingBates";
        internal const string EndingBatesFieldName = "EndingBates";
        internal const string DocProductionNumber = "DocProductionNumber";
        internal const string CollectionNotSpecifiedMessage = "Production collection identifier is not specified.";
        internal const int BatesBeginTypeId = 3005;
        internal const int BatesEndTypeId = 3006;
        internal const int BatesRangeTypeId = 3007;
        internal const int DPNTypeId = 3008;

        #region Service call constants

        internal const string ReviwerSearchService = "ReviewerSearchService";
        internal const string SavedSearch = "savedsearch";
        internal const string SLASH = @"/";

        internal const string TagService = "RVWTagService";
        internal const string Dataset = "dataset";
        internal const string TagScope = "tagscope";
        internal const string Tags = "tags";

        internal const string DatasetService = "DatasetService";

        internal const string DefaultLanguageId = "1";
        internal const string KnowledgeService = "KnowledgeService";
        internal const string RedactionReason = "redactionreason";

        internal const string DocumentService = "DocumentService";
        internal const string Document = "document";
        internal const string QuestionMark = "?";
        internal const string IncludeHiddenField = "hiddenField=";

        internal const string DocumentViewerDocument = "Document";

        internal const string RegexForAscii = "[\u0000-\u007F]";
        internal const string RegexForUnicodePassword = @"(\p{L})+\p{N}*\p{S}*";
        internal const string UserServiceUir = "UserService";
        internal const string Users_Uir = "users";
        internal const string Quote = "\"";
     
        #endregion

        #region Audit
        internal const string AuditUserId = "User ID";
        internal const string AudDateTime = @"Date/Time";
        internal const string AudDatasetId = "Dataset ID";
        internal const string AudNumberOfDocumentsSuccesfull = "Number of Docs Successful";
        internal const string AudNumberOfDocumentsFailed = "Number of Docs Failed";
        internal const string AudDCNFailedDocuments = "DCN of failed documents";
        internal const string AuditFailureReason = "Failure Reason";

        #endregion

    }

   
}
