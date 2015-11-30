#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Nagaraju</author>
//      <description>
//          This file contains DCB Parser worker constants
//      </description>
//      <changelog>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker
{
    internal static class Constants
    {
        #region Dcb

        internal const string Event_Job_Initialize_Start = "Job Intialize Start";
        internal const string EventJobInitializationValue = "In Initialaization method";
        internal const string Event_Job_Initialize_Success = "Import DCB File Job Intialize successfully";
        internal const string EventJobInitializationKey = "Import DCB File JobInitialization";
        internal const string ExportPathFull = "Cannot write to Export Path";

        internal const string EventXmlNotWellFormedValue = "Xml string is not well formed. Unable to recreate object.";
        internal const string ErrorAccessFolder = "Error while creating or accessing the folder.";
        internal const string ErrorCreatingFile = "Error while creating file.";
        internal const string GenerateTask = "Import DCB File GenerateTask";
        internal const string EventJobGenerateTaskValue = "In GenerateTask method";
        internal const string DoAutomicWork = "Import DCB File Do Atmoic work";
        internal const string DoAutomicWorkValue = "In Do Atmoic work";
        internal const string Shutdown = "Import DCB file Shutdown";
        internal const string ShutdownValue = "In Shutdown";
        internal const string EventGenerateTasks = "Exception in generate task";
        internal const string EventLogFolderPathMissing = "Log folder path missing";
        internal const string EventLogFolderPathNotSpecified = "Log folder path not specified";
        internal const string FilePath = "Filepath=";
        internal const string ErrorWritingTextFile = "Error while creating or writing text file";
        internal const string ErrorWritingBinaryFile = "Error while writing binary file error:";
        internal const string LogFileNameMissing = "Log file name is missing";
        internal const string LogFilehasInvalidCharacters = "File name has some invalid characters";
        internal const string ErrorGettingDatasetDetails = "Error in getting dataset detail for a dataSet id (Service error)";
        internal const string ErrorGettingDatasetDetailsNullReturned = "GetDataSetDetailForDataSetId returned a null value.Specified dataset may not exist";
        internal const string ErrorGettingCollectionMatterDetails = "Error while getting collection and matter details";
        internal const string ErrorGettingAllTags = "Error while getting all tags (Service error)";
        internal const string ErrorGettingAllReasonForRedaction = "Error while getting all reason for redaction (Service error)";
        internal const string ErrorFetchingHeaderDetails = "Error while fetching header details";
        internal const string ErrorGettingUserDetails = "Error while getting user details (Service error)";
        internal const string ErrorGettingAllSavedSearch = "Error while getting all saved search (Service error)";
        internal const string ErrorGettingDocumentFilterOptionNotSet = "Document filter option is not set";
        internal const string ErrorGettingDocuments = "Error while getting documents";
        internal const string ErrorGettingDocumentMetadata = "Error while getting documents metadata";
        internal const string ErrorGettingDocumentFieldData = "Error while getting document field data (Service error)";
        internal const string ErrorGettingDocumentTags = "Error while getting document tag data (Service error)";
        internal const string ErrorGettingDocumentFieldValues = "Error while getting document field values";
        internal const string ErrorGettingDocumentTagValues = "Error while getting document tag values";
        internal const string ErrorGettingDocumentRedactionReasonValues = "Error while getting document redaction reason values";
        internal const string ErrorInInitialize = "Error in initialize method";
        internal const string ErrorGenerateTask = "Error in generate task method";
        internal const string ErrorDoAtomicWork = "Error in do atomic work method";
        internal const string ErrorShutdown = "Error in shutdown method";
        internal const string DOCUMENT_SERVICE = "DocumentService";
        internal const string DOCUMENT = "document";
        internal const string BinaryReference = "BinaryReference";
        internal const string ErrorGettingDocumentBinary = "Error getting document binary (Service error)";
        internal const string ErrorGettingDocumentBinaryReference = "Error getting document binary reference (Service error)";
        internal const string ImportErrorGettingTag = "Error while getting Tag";
        internal const string ErrorGettingDocumentMetaData = "Error getting document meta data (Service error)";
        internal const int FieldTypeIdForPrivilegeField1 = 3001; //Reason Code
        internal const int FieldTypeIdForPrivilegeField2 = 3002; //Document description
        internal const string BackwardSlash = @"\";
        internal const string Anscii = "windows-1252";
        internal const string Unicode = "unicode";
        internal const int ReasonFieldType = 3001;
        internal const int DescriptionFieldType = 3002;
        internal const int ContentFieldType = 2000;
        internal const string DatasetFieldKey = "DatasetFieldKey";
        internal const string TextFilekey = "TextFilekey";
        internal const string NativeFilekey = "NativeFilekey";
        internal const string ImageFilekey = "ImageFilekey";
        internal const string PrdImgFilekey = "PrdImgFilekey";
        internal const string TagKey = "Tagkey";
        internal const string PrdImg = "PrdImg";
        internal const string ProductionFileType = "3";
        internal const string Native = "Native";
        internal const string NativeFileType = "1";
        internal const string Text = "Text";
        internal const string Images = "Images";
        internal const string ImagesFileType = "2";
        internal const string RegexForAscii = "[\u0000-\u007F]";
        internal const string RegexForUnicodePassword = @"(\p{L})+\p{N}*\p{S}*";
        internal const string UserError = "Error while getting user";
        internal const string BoorParameterNull = "Boor parameter is null";
        internal const string DocumentError = "Error while getting document";
        internal const string WriteHelperError = "Error while writing ";
        internal const string ImportErrorConvertDate = "Error while convert string in to date in dataset field";
        internal const string WriteHelperErrorMsg = "binary file into source error:= ";
        internal const string CommaSeparator = ",";
        internal const string DocumentBreak = "Y";
        internal const string FileExtension = ".txt";
        internal const int DateDataType = 61;
        internal const string SearchAndKey = " AND ";
        internal const string NotifictioninfoDataset = "Documents from Dataset ";
        internal const string NotifictioninfoLocation = " have been Imported to the location ";
        internal const string NotifictioninfoImport = "as part of the Import Job:";
        internal const string DateType = "date";
        internal const string Relevance = "Relevance";

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
        internal const string DocumentService = "DocumentService";
        internal const string Document = "document";
        internal const string QuestionMark = "?";
        internal const string IncludeHiddenField = "hiddenField=";
        internal const string DocumentViewerDocument = "Document";
        internal const string JobTypeName = "Import DCB File Job";
        internal const string JobName = "Import DCB File";
        internal const string UserServiceUri = "UserService";
        internal const string Users_Uri = "users";

        #endregion
        //internal const string <variable-name> = "string content";
        #region Audit
        internal const string AuditUserId = "User ID";
        internal const string AudDateTime = @"Date/Time";
        internal const string AudDatasetId = "Dataset ID";
        internal const string AudDatasetName = "Dataset Name";
        internal const string AuditImportJobName = "Import Job Name";
        internal const string AudNumberOfDocumentsSuccesfull = "Number of Docs Successful";
        internal const string AudNumberOfDocumentsFailed = "Number of Docs Failed";
        internal const string AuditFailureReason = "Failure Reason";
        internal const string AuditImportJobId = "Import Job Id";
        internal const string AuditDocumentExpStatus = "Document Imported Status";
        internal const string AuditSuccess = "Success";
        internal const string AuditDocId = "Document Id";
        internal const string AuditMattername = "Matter Name";
        internal const string AuditSource = "Source File path";
        internal const string AuditUNPws = "UserNames-Passwords";

        #region New Constants
        internal const string ErrorFieldsExtraction = "DCB credentials are invalid";
        internal const string ErrorUNPWLog = ", username/Password :";
        internal const string ErrorFacadeInitializationSecuredDCB = "Error occurred while initializing the dcb facade for the given Secured dcb file, file - {0}";
        internal const string ErrorFacadeInitialization = "Error occurred while initializing the dcb facade for the given dcb file, file - {0}";
        internal const string ErrorFacadeGetDocument = "Error occurred while trying to get document details, file - {0}";
        internal const string ErrorEVAddDocument = "Error occurred while trying to add document details to EV, evdocid - {0} collection id - {1}";
        internal const string ErrorEVAddImage = "Error occurred while trying to add image details to document, evdocid - {0} ImagesetId - {1}";
        internal const string ErrorSetDocumentTag = "Error occurred while trying to set tagid - {0} to document - {1}";
        internal const string ErrorGetDocumentTagId = "Error occurred while trying to get tagid for tagname - {0}";
        internal const string ErrorCreateDocumentTag = "Error occurred while trying to create tagname - {0}";
        internal const string ErrorDocumentTagsImport = "Error occurred while trying to import tags for document - {0}";
        internal const string ErrorDocumentRelationsImport = "Error occurred while trying to import relations for document - {0}";
        internal const string ErrorDocumentCommentsImport = "Error occurred while trying to import comments for document - {0}";
        internal const string ErrorDocumentTextLevlTagsImportForUnmappedFields = "The tags {0} are not processed since they are tied up to unmapped field : {1} for the document {2}";
        internal const string ErrorDocumentImagesmport = "Error occurred while trying to import images for document - {0}";
        internal const string ErrorDocumentRedlinesmport = "Error occurred while trying to import redlines for document - {0}";
        internal const string ErrorCreateDocumentComments = "Error occurred while trying to create comments for document - {0}";
        internal const string ErrorCreateDocumentImages = "Error occurred while trying to import images for document - {0}";
        internal const string ErrorCreateDocumentSet = "Error occurred while trying to create document set - {0}";
        internal const string ErrorCreateDocumentMarkup = "Error occurred while trying to create document Markup for document - {0}";
        internal const string ErrorTaskExecution = "Error occurred while executing task to add document - {0}";
        internal const string ErrorInvalidNativeFile = "Error occurred while trying to retrieve native file for the document - {0}";
        internal const string ErrorEVTaskFailed = "Error occurred while attempting add document - {0} to EV";
        internal const string ErrorDCBTaskFailed = "Error occurred while attempting get document - {0} from DCB";
        internal const string ErrorDcbImportDocumentRelationshipFailed = "Error occurred while attempting to add document relationship for document - {0}";
        internal const string ErrorDcbFacadeInitializationSimulAccessFailed = "Error occurred while attempting to access the dcb file - {0} which is already in use";
        internal const string DCB_IMPORT_TYPE = "2";
        internal const string CDATA_BEGIN_TAG = "<![CDATA[";
        internal const string CDATA_END_TAG = "]]>";
        internal const int INTERNAL_TAG_RETURN_ERROR_CODE = -2000;
        internal const int TAG_NOT_AVAILABLE = 0;
        internal const string DCBCommentsFont = "DCBCommentsFont";
        internal const string DCBCommentsFontSize = "DCBCommentsFontSize";
        internal const string DCBCommentsFontColor = "DCBCommentsFontColor";
        internal const string DcbParentDocIdFieldName = "PARENT_DOCID";
        internal const string DcbDocIdFieldName = "DOCID";
        internal const string ImageType = "3";

        //Auditlog constants
        internal const string CreatedBy = "Created By";
        internal const string DatasetName = "Dataset Name";
        internal const string AppendOverlay = "Append/Overlay";
        internal const string TotalDocuments = "Total Documents";
        internal const string Append = "Append";
        internal const string Overlay = "Overlay";
        internal const string Cancelled = "Cancelled";
        internal const string Paused = "Paused";
        internal const string Completed = "Completed";
        internal const string Failed = "Failed";
        internal const string JobStatus = "Job Status";
        internal const string SourcePath = "Source Path";
        internal const string UNPWEncryption = "EncryptionKey";
        internal const string UNPWDataSecurity = "Data Security";

        //internal const int ContentFieldType = 2000;
        #endregion

        #region Document

        internal const string File_Ext_Excel = "xls";
        internal const string File_Ext_Doc = "doc";
        internal const string File_Ext_Txt = "txt";
        internal const string File_Ext_Xml = "xml";
        internal const string File_Ext_Outlook = "msg";
        internal const string File_Ext_Ppt = "ppt";
        internal const string File_Ext_Tiff = "tif";
        internal const string File_Ext_Jpeg = "jpg";
        internal const string File_Ext_Bmp = "bmp";
        internal const string File_Ext_Pdf = "pdf";
        internal const string File_Ext_Html = "htm";
        internal const string File_Ext_docm = "docm";
        internal const string File_Ext_docx = "docx";
        internal const string File_Ext_dotm = "dotm";
        internal const string File_Ext_dotx = "dotx";
        internal const string File_Ext_potm = "potm";
        internal const string File_Ext_potx = "potx";
        internal const string File_Ext_ppam = "ppam";
        internal const string File_Ext_ppsm = "ppsm";
        internal const string File_Ext_ppsx = "ppsx";
        internal const string File_Ext_pptm = "pptm";
        internal const string File_Ext_pptx = "pptx";
        internal const string File_Ext_xlam = "xlam";
        internal const string File_Ext_xlsb = "xlsb";
        internal const string File_Ext_xlsm = "xlsm";
        internal const string File_Ext_xlsx = "xlsx";
        internal const string File_Ext_xltm = "xltm";
        internal const string File_Ext_xltx = "xltx";
        internal const string File_Ext_zip = "zip";
        internal const string File_Ext_png = "png";
        internal const string File_Ext_gif = "gif";
        internal const string File_Ext_Unknown = "unknown";
        internal const string MimeType_Excel = "application/vnd.ms-excel";
        internal const string MimeType_OpenXml = "application/x-vnd.openxmlformat";
        internal const string MimeType_Word = "application/msword";
        internal const string MimeType_Text = "text/plain";
        internal const string MimeType_Xml = "application/xml";
        internal const string MimeType_Html = "text/html";
        internal const string MimeType_Outlook = "application/vnd.ms-outlook";
        internal const string MimeType_Ppt = "application/vnd.ms-powerpoint";
        internal const string MimeType_XTiff = "image/x-tiff";
        internal const string MimeType_Tiff = "image/tiff";
        internal const string MimeType_Jpeg = "image/jpeg";
        internal const string MimeType_Bmp = "image/bmp";
        internal const string MimeType_Gif = "image/gif";
        internal const string MimeType_Pdf = "application/pdf";
        internal const string MimeType_Zip = "application/zip";
        internal const string MimeType_docm = "application/vnd.ms-word.document.macroEnabled.12";
        internal const string MimeType_docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        internal const string MimeType_dotm = "application/vnd.ms-word.template.macroEnabled.12";
        internal const string MimeType_dotx = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
        internal const string MimeType_potm = "application/vnd.ms-powerpoint.template.macroEnabled.12";
        internal const string MimeType_potx = "application/vnd.openxmlformats-officedocument.presentationml.template";
        internal const string MimeType_ppam = "application/vnd.ms-powerpoint.addin.macroEnabled.12";
        internal const string MimeType_ppsm = "application/vnd.ms-powerpoint.slideshow.macroEnabled.12";
        internal const string MimeType_ppsx = "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
        internal const string MimeType_pptm = "application/vnd.ms-powerpoint.presentation.macroEnabled.12";
        internal const string MimeType_pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        internal const string MimeType_xlam = "application/vnd.ms-excel.addin.macroEnabled.12";
        internal const string MimeType_xlsb = "application/vnd.ms-excel.sheet.binary.macroEnabled.12";
        internal const string MimeType_xlsm = "application/vnd.ms-excel.sheet.macroEnabled.12";
        internal const string MimeType_xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        internal const string MimeType_xltm = "application/vnd.ms-excel.template.macroEnabled.12";
        internal const string MimeType_xltx = "application/vnd.openxmlformats-officedocument.spreadsheetml.template";

        #endregion


        internal const string DateFormatType1 = "MMddyyyy";
        internal const string DateFormatType2 = "ddMMyyyy";
        internal const string DateFormatType3 = "yyyyMMdd";

        internal const string USCulture = "en-US";
        internal const string RUCulture = "ru-RU";
        internal const string JACulture = "ja-JP";

        internal const string EVDateTimeFormat = "yyyyMMddHHmmss";
        internal const double KBConversionConstant = 1024.0;
        #endregion

        internal const short One = 1;
        internal const string ReturnAndNewLineFeed = "\r\n";
        internal const string NewLineFeed = "\n";
        internal const string HtmlBreakWithNewLine = "<br type='n'/>";
        internal const string Image = "Image";
        internal const string TagImportFailed =
            "DcbParser: Text level tag import for unmapped fields failed. Document Id: {0}, Field: {1}, Tags:{2}";
        #endregion

        #region Edocs
        internal const string Message = "Message";
        internal const string ErrorCode = "ErrorCode";
        internal const string DiskFullErrorMessage = "There is not enough space on the disk";
        internal const string EDOCSExtractionRoleType = "EdocsExtra-113e-4217-9863-ec58c3f7vw89";
        #endregion
    }
}
