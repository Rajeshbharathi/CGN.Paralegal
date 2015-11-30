#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Nagaraju</author>
//      <description>
//          This file contains the Search Index Update Worker Constants 
//      </description>
//      <changelog>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="02/26/2013">Bug Fix # 130801 </date>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//          <date value="09/06/2013">CNEV 2.2.2 - Split Reviewset NFR fix - babugx</date>
//          <date value="01/02/2014">Task 159667 - ADM-EXPORT-005</date>
//          <date value="05/26/2014">Bug Fix # 168718 </date>
//          <date value="06/17/2014">NLog Exception fixing to avoid exception due to maximum querystring</date>
//          <date value="03/24/2015">Bug Fix 184140 - Publish blank pages</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
using System;

namespace LexisNexis.Evolution.Worker
{
    internal class Constants
    {
        internal const string SearchIndexUpdateWorkerRoleId = "72D8BC58-5FF4-43D4-8A6D-258A93425D63";
        internal const string NotReviewed = "Not Reviewed";
        internal const string Reviewed = "Reviewed";
        internal const string Comma = ",";
        internal const char CharacterComma = ',';
        internal const int Active = 1;
        internal const int Archive = 2;

        internal const string DocumentVaultManager = "DocumentVaultManager";
        internal const string VaultDocumentText = "DocumentVaultManager";

        internal const string ReviewsetSearchRoleId = "97D73522-8DAA-4C31-80B9-06E412036B04";
        internal const string OutputDirNotAccessible = "Axl output directory is invalid or not accessible to write the results.";

        public const string BatesNumber = "Bates Number";
        public const string BatesBegin = "Bates Begin";
        public const string BatesEnd = "Bates End";
        public const string BatesRange = "Bates Range";
        public const string DPN = "DPN";
        public const string ProductionPath = "ProductionPath";

        public enum FieldTypeIds
        {
            BatesNumber = 3004,
            BatesBegin = 3005,
            BatesEnd = 3006,
            BatesRange = 3007,
            DPN = 3008
        }

        #region Event Log
        internal const string PrintProcessRoleId = "print0fc6-113e-4217-9863-ec58c3f7pw89";
        internal const string BulkPrintJobTypeName = "Bulk Print Job";
        internal const string Event_Job_Initialize_Start = "Job Initialize Start";
        internal const string Event_Job_Initialize_Failed = "Job Initialize Failed";
        internal const string Event_Job_GenerateTask_Start = "Generate Task Start";
        internal const string Event_Job_GenerateTask_Failed = "Generate Task Failed";
        internal const string Event_Job_DoAtomicWork_Success = "Do Atomic Work successfully";
        internal const string Event_Job_DoAtomicWork_Failed = "Do Atomic Work Failed";
        #endregion

        #region Constants for REST Calls
        internal const string EqualTo = "=";
        internal const string QuestionMark = "?";
        internal const string Ampersand = "&";
        internal const string Services = "services";
        internal const string DataSetService = "DatasetService";
        internal const string DataSet = "dataset";
        internal const string Collection = "collection";
        internal const string PrinterManagementService = "PrinterManagementService";
        internal const string PrinterManagement = "printer-management";
        internal const string MappedPrinters = "mapped-printers";
        internal const string DocumentService = "DocumentService";
        internal const string Document = "document";
        internal const string BinaryType = "binarytype";
        internal const string BinaryReferenceId = "referenceid";
        internal const string IncludeHiddenField = "hiddenField=";
        internal const string DocumentSet = "documentset";
        internal const int TitleFieldType = 3003;
        #endregion

        #region General
        internal const string UnsuccessfullNotificationBody = "One or more documents were not queued successfully to the printer. Please check the job log for more details.";
        internal const string PrinterUsed = "Printer Used";
        internal const string BulkPrintTypeName = "Bulk Print Job";
        internal const string TaskNumber = " for Task Number -  ";
        internal const string CallBackUri = "CALLBACK_URI";
        internal const string RedactItUri = "REDACTIT_URI";
        internal const string QueueServerUrl = "QueueServerUrl";
        internal const string SuccessfulDocumentQueued = "Document queued successfully to printer";
        internal const string UnsuccessfulDocumentQueued = "Document not queued successfully to printer";
        internal const string Hyphen = "-";
        internal const string XmlNotWellFormed = "In Initialization job xml not well formed, Job Failed";
        internal const string BulkPrintJobInitialisation = "Bulk Print Job Initialization";
        internal const char Split = '/';
        internal const string NativeSetBinaryType = "2";
        internal const string ProductionSetBinaryType = "3";
        internal const string ImageSetBinaryType = "2";
        internal const string SearchResultsSortByRelevance = "Relevance";
        internal const string TitleNotAvailable = "No title available";

        internal const string BulkPrintJobNameForAudit = "Name of bulk print job";
        internal const string DCN = "DCN";
        internal const string ImageSetName = "Name of Image set";
        internal const string ProductionSetName = "Name of Production set";
        internal const string DocumentCount = "Number of documents";
        internal const string FailureReason = "Reason for failure";
        internal const string ImageSelectionCriteria = "Image Selection Criteria: ";
        internal const string DocumentSelectionCriteria = "Document Selection Criteria: ";
        internal const string BulkPrintJobRunnedBy = "Bulk print job scheduled by: ";
        internal const string UserUri = "user-";
        internal const string DomainName = "\\";
        internal const string DomainNameNotApplicable = "N/A";
        internal const int Failed = 6;
        internal const string BulkPrintJobFailedUnexpectedly = "Encountered unexpected error";
        internal const string UnableToFetchDocumentCount = "Unable to fetch document count";
        internal const string IsConceptSearchingEnabled = "AND IsConceptSearchingEnabled:";
        internal const string IncludeFamily = "AND IncludeFamily:";
        internal const string True = "True";
        internal const string STA = "STA";
        internal const string JobService = "JobService";
        internal const string Job = "job";
        internal const string JobOperation = "?oper=";
        internal const int SentToPrinter = 10;
        internal const int SentToPrinterWithErrors = 11;
        internal const int SendingToPrinter = 12;
        internal const int Completed = 5;
        internal const string NotApplicable = "N/A";
        internal const string DocumentAuthor = "author";
        internal const string DocumentTitle = "title";
        internal const int DCNFieldTypeId = 3000;
        internal const string Zero = "0";
        internal const string HtmlHeadForseparatorSheet = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd""><html xmlns=""http://www.w3.org/1999/xhtml""><head><title>Print Document</title>" +
                          @"<style type=""text/css"">body{margin: 0px auto;}div{margin: 26% 0%;width: 100%;text-align: center;vertical-align: middle;position: relative;}table{vertical-align: middle;margin: 0px auto;}table td{text-align: left;}" +
                          @".fontBold{font-weight: bold;}</style></head><body>";

        internal const string HtmlContentForDCN = @"<div><table width=""100%""><tr><td><table cellpadding=""3"" border=""1"" cellspacing=""0"" width=""100%"" style=""border: 2px solid #8064A2;""><tr bgcolor=""#8064A2;""><td width=""100%"">&nbsp;</td><td width=""100%"">&nbsp;</td>" +
                          @"</tr><tr bgcolor=""#DFD8E8;""><td class=""fontBold"">Document Control Number</td><td>";

        internal const string HtmlContentForTitle = @"</td></tr><tr class=""alternateRow""><td class=""fontBold"">Title</td><td>";
        internal const string HtmlContentForAuthor = @"</td></tr><tr bgcolor=""#DFD8E8;""><td class=""fontBold"">Author</td><td>";
        internal const string HtmlContentForTags = @"</td></tr></table></td></tr><tr><td></td></tr><tr><td></td></tr><tr><td></td></tr><tr><td></td></tr><tr><td></td></tr><tr><td>" +
                          @"<table cellpadding=""3"" border=""1"" cellspacing=""0"" width=""100%""><tr bgcolor=""#8064A2;""><td style=""color: #fff; font-weight: bold; text-align: center"">List of Tags on the Document</td></tr>";

        internal const string HtmlContentForEndOfseparatorSheet = @"</table></td></tr></table></div></body></html>";
        internal const string HtmlContentForTagNameStart = @"<tr bgcolor=""#DFD8E8""><td>";
        internal const string HtmlContentForTagNameEnd = "</td></tr>";
        internal const string HtmlContentForTagNameStartAlternate = @"<tr class=""alternateRow""><td>";
        internal const string separatorHtml = "separator.html";
        internal const string AdminTagScope = "admin";
        #endregion

        #region Web Request Constants
        internal const string UrlEncodedContentType = "application/x-www-form-urlencoded";
        internal const string Post = "POST";
        internal const string Source0 = "source0=";
        internal const string Source1 = "&source1=";
        internal const string Target = "&target=";
        internal const string CSFName = "&CSFName=";
        internal const string PdfAndNotificationUrl = ".pdf&outputformat=pdf&notificationurl=";
        internal const string BulkPrintRequestId = "&requestid=";
        internal const string MatterId = "&matterid=";
        internal const string CollectionId = "&collectionid=";
        internal const string DocumentId = "&documentId=";
        internal const string Title = "&title=";
        internal const string NotificationVerb = "&notificationverb=post";
        internal const string Ok = "OK";
        internal const string CallPushMethodCalled = "CallPush method is called";
        internal const string RedactitServerResponseWithStatus = ",server responded with status-";
        internal const string separatorTextFileName = "separatorSheet.txt";
        internal const string RegexForUnicodePassword = @"(\p{L})+\p{N}*\p{S}*";
        internal const string RegexForASCII = "[\u0000-\u007F]";
        internal const string UserService = "UserService";
        internal const string Users = "users";
        internal const string Source = "source";
        internal const string BaseSharedPath = "BASE_SHAREPATH";
        internal const string BulkPrintFolder = "BulkPrint";
        internal const string SourceDirectoryPath = "SourceDirectory";
        internal const string TargetDirectoryPath = "TargetDirectory";
        internal const string CleanFolderInHours = "CLEAN_FOLDER_IN_HOURS";
        internal const string ErrorInDeletingPDFFiles = "Error in deleting Pdf files :";
        internal const string DueTo = " due to ";
        internal const string WcfHostUrl = "WCF-HostURL";

        #endregion

        #region ProductionJob

        internal const string Break = "<br/>";
        internal const string Asterik = "*";
        internal const string ProductionsetStartError = "ProductionStartupWorker: Error while accessing the path: ";
        internal const string DirectoryMaxLimitError = "Generate Message: The following share path(s) exceeds 248 characters. Hence no documents will be prodcued in these path(s)<br/>";
        internal const string AllDirectoryMaxLimitError = "All share paths exceed 248 characters hence no documents will be produced";
        internal const string ProductionsetGenerateDocError = "Generate Message: Failed to get all documents for production";
        internal const string ProductionsetGenerateError = "ProductionStartupWorker: GenerateMessage: {0}";
        internal const string ProductionsetGenerateSuccess = "Generate Message: Get all the document for production successfully";
        internal const int DirectoryMaxLimit = 248;
        internal const string ProductionPreProcessRoleId = "prod0fc6-113e-4217-9863-ec58c3f7pw89";
        internal const string ProductionImagingWokerRoleId = "prod0fc6-113e-4217-9863-ec58c3f7iw89";
        internal const string ProductionVaultIndexingUpdateWokerRoleId = "prod0fc6-113e-4217-9863-ec58c3f7vw89";
        internal const string ProductionPreFailure = "failed to send  for production";
        internal const string ProductionPlaceholderFailure = "failed to get production placeholder fields";
        internal const string EmptyPagesInDoc = "-Error while loading the document";
        #endregion

        #region For error handling
        internal const string Message = "Message";
        internal const string ErrorCode = "ErrorCode";
        internal const string ErrorInGetQualifiedDocuments = "There was an error in getting qualified documents";
        #endregion

        #region ProductionPreprocessWorker

        #region Audit Log Constants

        internal const string JobTypeName = "Production Job";
        internal const string AuditDoAtomicWorkExceptionValue = "Exception while executing task ";
        internal const string BatesAuditPregix = "BatesNumber";
        internal const string DocumentProductionNumberAuditPrefix = "Document Production Number";
        internal const string DCNAuditPrefix = "DCN";
        internal const string ProductionNameAuditPrefix = "Production Name";
        internal const string DatasetNameAuditPrefix = "Dataset Name";

        #endregion

        #region Event Log Constants

        internal const string EventJobInitializationKey = "Production JobInitialization";
        internal const string EventXmlNotWellFormedValue = "Xml string is not well formed. Unable to recreate object.";
        internal const string EventJobInitializationValue = "In Initialaization method";
        internal const string EventDoAtomicWorkExceptionKey = "Production DoAtomicWork";
        internal const string EventDoAtomicWorkExceptionValue = "Production Exception DoAtomicWork Method - ";
        internal const string AuditBootParameterKey = "Production BootParamater";
        internal const string AuditBootParameterValue = "Boot parameter parsed successfully";
        internal const string EventGenerateTasksExceptionValue = "Exception in Production GenerateTasks Method - ";
        internal const string EventGenerateTasksAddDocumentTaskValue = "Exception in adding document to the task";
        internal const string GenerateTask = "Production GenerateTask";
        internal const string EventJobGenerateTaskValue = "In GenerateTask method";
        internal const string EventCreateHeaderFooterConfigError = "Error while creating the headerfooter config file ";
        internal const string EventCreateMarkUpError = "Error while creating the markup file ";
        internal const string EventCreateXdlError = "Error while getting near native document - "; //"Error while creating the xdl file - "; 
        internal const string EventXdlFileMissing = "Near native document is needed for production. Document does not exist or it is under conversion process"; //"Xdl file missing in vault";
        internal const string EventMissingOriginalCollectionDetails = "The original document reference id and original collection id are missing";
        internal const string EventBaseSharePathMissing = "The base share path is missing";
        internal const string EventDocumentNotCreatedByRedActIt = "The document was not created in the specified time in the server";
        internal const string EventCallPushMethod = "CallPush method is called";
        internal const string EventWasForDocMessage = "This error was for document titled";
        internal const string EventRedactItServerErrorResponse = "RedactIt server responded with error";
        internal const string EventDeleteTemporaryFolderError = "Error in deleting temporary folder";

        #endregion


        #region XML Constants
        #endregion

        #region Error Constants
        internal const string ErrorFetchingArchivePathRedactableSet = "Error while fetching the archive path and redactable set";
        internal const string ErrorArchivePathNotExist = "Archive path does not exist for the dataset";
        internal const string ErrorCurrentredactableSetUnavailable = "Current redactable set not available";
        internal const string ErrorDuringPerformingSearch = "Error occured during search.Search string: ";
        #endregion

        #region Others

        internal const string JobName = "Production Job";
        internal const string EncodingFormatUTF8 = "utf-8";
        internal const string EncodingFormatUTF16 = "utf-16";
        internal const string XrlExtension = ".xrl";
        internal const string BravaBinaryTypeId = "4";
        internal const string XdlExtn = "xdl";
        internal const string ZdlExtn = "zdl";
        internal const string XdlWithDotExtn = ".xdl";
        internal const string Slash = @"\";
        internal const char Colon = ':';
        internal const string xmlVersionString = @"<?xml version=\""1.0\"" encoding=\""UTF-8\""?>";
        internal const string QueryStringSourcePrefix = "source=";
        internal const string QueryStringMarkupFileNamePrefix = @"&markupFileName=";
        internal const string QueryStringSecurityXmlFileNamePrefix = "&securityxmlfilename=";
        internal const string QueryStringTargetPrefix = "&target=";
        internal const string QueryStringNotificationUrlPrefix = "&notificationurl=";
        internal const string QueryStringOutputFormatPrefix = "&outputformat=";
        internal const string QueryStringThumbFormatPrefix = "&thumbformat=";
        internal const string QueryStringThumbPagesPrefix = "&thumbpages=";
        internal const string QueryStringThumbNamePrefix = "&thumbname=";
        internal const string QueryStringThumbQualityPrefix = "&thumbquality=";
        internal const string QueryStringThumbSizesPrefix = "&thumbsizes=";
        internal const string QueryStringTiffBppPrefix = "&tiffbpp=";
        internal const string QueryStringTiffMonochromePrefix = "&forcetiffmonochrome=";
        internal const string QueryStringTifDPI = "&tiffdpi=300";
        internal const string QueryStringOneFilePerPagePrefix = "&exportfileperpage=";
        internal const string QueryStringScrubbedText = "&getscrubbedtext=";
        internal const string QueryStringFitWithinBannersSetToTrue = "&fitwithinbanners=true";
        internal const string QueryStringDocumentDetails = "&documentdetails=";  //This path is used by the production service to determine the matter collection document ids
        internal const string QueryStringNotificationVerb = "&notificationverb=get";
        internal const string QueryStringDestinationFileName = "&csfname=";
        internal const string QueryStringHeartBeatFileName = "&heartbeat=";
        internal const string QueryStringPriority = "&priority=";

        internal const string ThumbQuality = "100";
        internal const string ThumbPagesAll = "a";
        internal const string ThumbDefaultPageName = "ThumbNail";
        internal const string ThumbDefaultSizes = "700,900";

        internal const string Blank = "";
        internal const string SourceKeyword = "source";
        internal const string DestinationKeyword = "destination";
        internal const string ProductionDocumentBinaryType = "3";

        internal const string BatesFormat = @"{0}%batespgno({1})";
        internal const string EmptyBatesFormat = "%batespgno()";
        internal const string DateFormat = @"%Date";
        internal const string TimeFormat = @"%Time";
        internal const string DateTimeFormat = @"%Date %Time";
        internal const string PageNumberFormat = @"%Page";

        internal const string PdfKeyword = "pdf";
        internal const string TiffKeyword = "tiff";
        internal const string JpgKeyword = "jpg";
        internal const string PngKeyword = "png";
        internal const string TifKeyword = "tif";

        internal const string Pdfextension = ".pdf";
        internal const string Tiffextension = ".tiff";
        internal const string Jpgextension = ".jpeg";
        internal const string Pngextension = ".png";
        internal const string Tifextension = ".tif";

        internal const string TrueString = "true";
        internal const string OkKeyword = "OK";

        internal const string EvAuditTimeStamp = "Timestamp";
        internal const string EvAuditSource = "Dataset";
        internal const string EvAuditProductionNumber = "Production Number";
        internal const string EvAuditProfile = "Profile";
        internal const string EvAuditWho = "Who";
        internal const string EvAuditProductionSetName = "Production Set";
        internal const string EvAuditProductionStatus = "Status";
        internal const string EvAuditProductionSuccess = "Success";
        internal const string EvAuditProductionFailure = "Failure";

        internal const string PrevilegeLogdateFormat = @"MM/dd/yyyy";
        internal const string PrevilegeLogTitleKeyword = "Title";
        internal const string PrevilegeLogNameKeyword = "Name";
        internal const string PrevilegeLogAuthorKeyword = "Author";
        internal const string PrevilegeLogYesKeyword = "Yes";
        internal const string PrevilegeLogNoKeyword = "No";
        internal const int DeleteJobCommand = 7;
        internal const int FileMaxLimit = 260;
        internal const string ErrorInValidFilePath = "File name exceeds beyond windows limit of 260 characters and hence this document was not produced";
        internal const string FullStop = ".";
        internal const string ProductionPreSucess = "successfully sent  for production";
        internal const string ProductionPreException = "Production Preprocess Worker: DCN: {0} Exception: {1}";
        internal const string ProductionCallBackURL = "ProductionCallBackURL";
        internal const string PublishBlankPages = "PublishBlankPages";
        internal const string PublishBlankPagesQueryString = "&publishblankpages=";
        internal const string Tilde = "~";

        #region Placeholder constants

        internal const string PlaceHolderFileLine1 = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd""><html xmlns=""http://www.w3.org/1999/xhtml""><head><title>Print Document</title>";
        internal const string PlaceHolderFileLine2 = @"<style type=""text/css"">body{margin: 0px auto;}div{margin: 26% 0%;width: 100%;text-align: center;vertical-align: middle;position: relative;}table{vertical-align: middle;margin: 0px auto;}table td{text-align: left;}";
        internal const string PlaceHolderFileLine3 = @".fontBold{font-weight: bold;}</style></head><body>";
        internal const string PlaceHolderFileLine4 = @"<div><table width=""60%""><tr><td><table cellpadding=""3"" border=""1"" cellspacing=""0"" width=""100%"" style=""border: 2px solid #8064A2;""><tr bgcolor=""#8064A2;""><td width=""30%"">&nbsp;</td><td width=""30%"">&nbsp;</td>";
        internal const string PlaceHolderFileLine5 = @"</tr>";

        internal const string PlaceHolderFileLine6Portion1 = @"<tr bgcolor=""#DFD8E8;""><td class=""fontBold"">Document Production Number</td><td>";
        //PlaceHolderFileLine6Portion2 is a dynamic string
        internal const string PlaceHolderFileLine6Portion3 = @"</td></tr>";

        internal const string PlaceHolderFileLine7Portion1 = @"<tr class=""alternateRow""><td class=""fontBold"">";
        internal const string PlaceHolderFileLine7Portion2 = "Document Control Number";
        internal const string PlaceHolderFileLine7Portion3 = @"</td><td>";
        //PlaceHolderFileLine7Portion4 is a dynamic string
        internal const string PlaceHolderFileLine7Portion5 = @"</td></tr>";

        internal const string PlaceHolderFileLine8 = @"</table></td></tr>";
        internal const string PlaceHolderFileLine9 = @"</table></div></body></html>";
        

        #endregion


        #endregion

        #region XPaths for markups

        internal const string XmlNamespace = "http://www.infograph.com";
        internal const string XmlNamespacePrefix = "in";

        internal const string XpathArrow = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='ArrowLine']";
        internal const string XpathBoxes = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='NonEditPolygon' or local-name()='Rectangle' or local-name()='Polygon']";
        internal const string XpathHighlights = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='GeometryGroup']";
        internal const string XpathLines = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='Line' or local-name()='Polyline']";
        internal const string XpathRedactions = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='Blockout']";
        internal const string XpathTextBox = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='Text']";
        internal const string XpathRubberStamp = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='Stamp']";
        internal const string XpathNotes = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*[local-name()='Changemark']";


        internal const string XpathComment = @"/in:IGCMarkupDocument/in:PageList/in:Page/in:AuthorList/in:Author/in:*/@comment";
        internal const string XpathPrevilegePage = @"/in:IGCMarkupDocument/in:PageList/in:Page[@index='#']/in:AuthorList/in:Author/in:*[local-name()='Blockout']/@comment";

        #endregion

        #region Mime types
        #region Mime Extensions
        internal const string MimeExcel = "application/vnd.ms-excel";
        internal const string MimeWord = "application/msword";
        internal const string MimeText = "text/plain";
        internal const string MimeXml = "application/xml";
        internal const string MimeOutlook = "application/vnd.ms-outlook";
        internal const string MimePowerPoint = "application/vnd.ms-powerpoint";
        internal const string MimeTiff = "image/x-tiff";
        internal const string MimeJpeg = "image/jpeg";
        internal const string MimePdf = "image/pdf";
        internal const string MimeBitmap = "application/bmp";

        internal const string MimeExcelTypeName = "Excel";
        internal const string MimeWordTypeName = "Word";
        internal const string MimeTextTypeName = "Text";
        internal const string MimeXmlTypeName = "Xml";
        internal const string MimeOutlookTypeName = "Outlook";
        internal const string MimePowerPointTypeName = "Powerpoint";
        internal const string MimeTiffTypeName = "Tiff";
        internal const string MimeJpegTypeName = "Jpeg";
        internal const string MimePdfTypeName = "Pdf";
        internal const string MimeBitmapTypeName = "Bitmap";

        internal const string Mime_Pdf = "application/pdf";
        internal const string Mime_Tiff = "image/tiff";
        internal const string Mime_Jpg = "image/jpeg";
        internal const string Mime_Png = "image/png";
        internal const string UserSessionInfo = "UserSessionInfo";

        #endregion

        #endregion

        internal const string ProductionSetFolder = "ProductionSet";
        internal const string BackwardSlash = @"\";

        internal const string JobStartedMessage = "Job started.";
        internal const string TaskCompletedMessage = "Task completed for 1 document. Task#";
        internal const string JobEndedMessage = "Job ended.";
        internal const string DatasetNamePrefix = "Dataset Name:";
        internal const string JobStatusCompleteStatus = "Job Status: Completed";
        internal const string JobStatusFailedStatus = "Job Status: Failed";
        internal const string ErrorFetchingProductionNameMessage = "Error while fetching productionset name.";
        internal const string Relevance = "Relevance";
        internal const int JobPaused = 4;
        internal const int JobStopped = 3;
        internal const int JobCompleted = 5;
        internal const int JobFailed = 6;

        internal const string Case1 = "1";
        internal const string Case2 = "2";
        internal const string Case3 = "3";
        internal const string UnderScore = "-";
        internal const string TextFileExtension = ".txt";
        #endregion

        #region OverlayWorker
        internal const string IMAGE_FILE_TYPE = "Image";
        internal const string DEFAULT_PAGE_SIZE = "10";
        internal const string DEFAULT_PAGE_NUMBER = "0";
        internal const string FALSE = "false";
        internal const string MessageNoMatchRecord = "No matching record.";
        internal const string MessageMatchRecord = "Record match found";
        internal const string MessageMoreThanOneRecord = "Search return more than one record";
        internal const string SearchAndCondition = "AND";
        internal const string ErrorConstrcutLogData = "Failed on construct Log data :";
        internal const string OverlaySuccessMessage = "Overlay search successfully completed.";
        internal const string OverlayFailureMessage = "Failled during document search .";
        internal const string OverlayWorkerRoleType = "257e709e-a386-480c-a5ef-2f86db4f9e41";
        internal const string OverlayNoActionMessage = " Document not added/updated.";
        internal const string ErrorMessageInvalidPipeLine = "Invalid pipline process.";
        internal const string ErrorMessageSearch = "Failled on Search Document ";
        internal const string ErrorMessageBulkSearch = "Failed on bulk search in Overlay worker";
        internal const string TEXT_FILE_TYPE = "Text";
        internal const string ExtractedText_FILE_TYPE = "ExtractedText";
        internal const string ErrorMessageDuplicateSearch = "Document matching more than once was not updated.";
        internal const string ErrorMessageSearchNoMatch = "Exact record was not matched for the selected matching field.";
        internal const string SearchORCondition = " OR ";
        #endregion

        #region LoadFile
        #region LoadFile
        internal const string HelperFile = "HelperFile";
        internal const string NoTextImport = "NoTextImport";
        internal const string BackSlash = @"\";
        internal const int BulkInsertRecordCount = 4;
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
        internal const string File_Ext_csv = "csv";
        internal const string File_Ext_rtf = "rtf";
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
        internal const string MimeType_csv = "text/comma-separated-values";
        internal const string MimeType_rtf = "application/rtf";
        internal const double KBConversionConstant = 1024.0;
        internal const string ALL_FILE_TYPE = "all";
        internal const string STR_DOT = ".";
        internal const string RecordSplitter = "\r\n";
        internal const string ConcordanceFieldSplitter = "\n";
        internal const string StringPattern = @"{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*[^{1}]*$)";
        internal const string LoadFileDateFormat = "MMddyyyyHHmmss";
        internal const string LoadFileRecordParserWorkerRoleType = "c605ca73-2eb6-4074-b4eb-c0c5a392d591";
        internal const string LawImportStartupWorkerRoleType = "0b3aa9c8-659e-476f-b502-7cae7019d866";
        internal const string RecordParserSuccessMessage = "Document record parsed successfully.";
        internal const string RecordParserFailureMessage = "Failed during parse record.";
        internal const string NATIVE_FILE_TYPE = "Native";
        #endregion

        #region Error
        internal const string Error_ParseLoadFileRecord = "Error occurred on parse Load File record - ";
        internal const string MissingFileMessage = "</br>DCN for missing file(s): {0}.";
        internal const string MissingNativeFileMessage = "</br>Missing native file: ";
        internal const string MissingNativeFilesMessage = "Missing native file: ";
        internal const string MissingImageFileMessage = "</br>Missing image file: ";
        internal const string MissingImageForKey = "</br>Image is missing for";
        internal const string MissingContentFileMessage = "</br>Missing content file: ";
        internal const string FailedRecordMessage = "</br>DCN for failed record: {0}.";
        internal const string MisMatchedFieldMessage = "</br>DCN for mismatched field: {0}.";
        internal const string MisMatchedToWrongData = "</br>Mismatched field '{0}' does not match data type.";
        internal const int DateFieldTypeId = 61;
        internal const string Error_CreateContentFile = "Failed to create content file, Validate dataset shared path.";
        internal const string Error_DiskFullErrorMessage = "not enough space";
        internal const string DiskFullErrorMessage = "There is not enough space on the disk";
        internal const string MissingFiles = "Missing file(s).";

        internal const string MsgMissingFiles =
            "The import file is missing.  Please locate and select the import file for the dataset.";

        internal const string MsgMissingNativeFiles =
            "The native file is missing.  Please locate and select the native file for the dataset.";

        internal const string MsgMissingImage =
            "The image file is missing.  Please locate and select the image file for the dataset.";

        internal const string MsgMissingContentFile =
            "The text file is missing.  Please locate and select the text file for the dataset.";

        internal const string MsgMismatchedFile =
            "The data type does not match {0}. Please try again.";

        internal const string MsgFailedParsing =
            "Concordance Evolution could not parse the record. Please import file again.";

        internal const string MsgFailedRecord = "Concordance Evolution could not import the record.";

        internal const string MsgOverlayMisMatch =
            "More than one matching field is available for the record. The record was not updated.";

        internal const string MsgDocumentSearch = "No matching documents found. Please correct and try again.";

        internal const string MsgUnhandledException = "Unhandled Exception ({0})";

        #endregion

        #region Family Relation
        internal const string InputParameterJobRunIdInCreateRelationshipRecord = "@jobRunId";
        internal const string InputParameterParentDocumentIdInCreateRelationshipRecord = "@parentDocumentId";
        internal const string InputParameterChildDocumentIdInCreateRelationshipRecord = "@childDocumentId";
        internal const string InputParameterFamilyIdInCreateRelationshipRecord = "@familyId";
        internal const string InputParameterThreadingConstraintInCreateRelationshipRecord = "@threadingConstraint";
        internal const string InputParameterRelationshipTypeInCreateRelationshipRecrod = "@relationshipType";
        internal const string StoredProcedureCreateRelationshipRecord = "EV_TMP_JOB_CreateRelationshipRecord";
        #endregion
        #endregion

        internal const string ParserSuccessMessage = "Source file parsed successfully.";
        internal const string LoadFileParserWorkerRoleType = "7f88a6a2-a1be-4d41-be2e-e3d303a3137b";
        internal const string ParserFailureMessageOnInitialize = "Failed to initialize import job, validate source info.";
        internal const string StringZero = "0";
       
        #region Reviewset
        internal const string ReviewsetVaultUpdateRoleID = "DC1002F9-853D-4BE8-86F4-BE0E44DCF59F";
        internal const byte One = 1;
        internal const byte Three = 3;
        internal const string FalseZeroString = "false:0";

        internal const string ReviewsetLogicRoleID = "A9562A43-7A6F-4644-A157-84630A313783";
        internal const string AuditEventReviewSetName = "ReviewSet Name";
        internal const string AuditEventBinderName = "Binder Name";
        internal const string AuditEventChildReviewSetName = "Child Review set names";
        internal const string AuditEventDivisionLogic = "Division Logic";
        internal const string AuditEventSplittingLogic = "Splitting Logic";
        internal const string ReviewsetNameLog = "Review set name ";
        internal const string AlreadyExistsLog = " already exists.";
        internal const string ReviewsetLogicWorkerException = "ReviewsetLogicWorker : CreateReviewset : Exception details: {0}";

        internal const string DOCUMENTS_CHUNK_SIZE = "DocumentsChunkSize";
        internal const string ReviewsetStartupRoleID = "2F028C4D-5D38-421B-BEB9-B4D963798899";
        internal const string ReviewsetVaultTaggerRoleID = "A8CFA139-FE2A-4894-9CFD-539CDE68049D";
        internal const string SearchIndexTaggerRoleId = "3BB7D3C3-1E18-45C8-BE87-8C5BBDF25D9E";
        internal const string SplitActivity = "Split";
        internal const byte Tagged = 1;
        internal const byte Untagged = 3;

        #endregion

        #region Export
        internal const string FileSpecialCharactersRegex = @"[\\\/:\*\?""'<>|]";
        internal const string ExportCopyfailed = "Failed to copy source files.";
        internal const string ExportFileCopyWorkerRoleType = "4902c263-c3a3-43d5-8316-fe3f39ae3b8c";
        internal const string ExportFileCopyErrorMessage = "Image file is not exported since the selected image field contains no value";
        internal const string ExportNativeTextCopyErrorMessage = "Error occurred while copying file ";
        internal const string ExportBreakMessage = ".<br/>";
        internal const string ExportImageNoValueCopyErrorMessage = "Image file is not exported since the selected image field contains no value.<br/>";
        internal const string ExportImageCopyErrorMessage = "Error occurred while copying image ";
        internal const string ExportDCNMessage = "DCN:";
        internal const string ExportBreak = "<br/>";

        internal const string ConcordanceRecordSplitter = "\r\n";
        internal const string ConcordanceRowSplitter = "\r";
        internal const string DateType = "date";
        internal const string DateFormatType1 = "mmddyyyy";
        internal const string DateFormatType2 = "ddmmyyyy";
        internal const string DateFormatType3 = "yyyymmdd";
        internal const string USCulture = "en-US";
        internal const string RUCulture = "ru-RU";
        internal const string JACulture = "ja-JP";
        internal const string MonthFormatSmall = "mm";
        internal const int ContentFieldType = 2000;
        internal const string CommaSeparator = ",";
        internal const string DocumentBreak = "Y";
        internal const string LoadFileWriteFailed = "Failed to write in Load File.";
        internal const string ExportLaodFileWriterWorkerRoleType = "eacd0fc6-113e-4217-9863-ec58c3f7de89";
        internal const string AuditFailureReason = "Failure Reason";
        internal const string AuditExportJobId = "Export Job Id";
        internal const string AuditDocumentExpStatus = "Document Exported Status";
        internal const string AuditSuccess = "Success";
        internal const string AuditDcn = "DCN";
        internal const string Ansi = "windows-1252";

        internal const int DCNFieldType = 3000;
        internal const string ExportMetadataWorkerRoleType = "29f2843c-1c75-451e-8e78-6a34d5d2b000";
        internal const string TextFileTypeId = "1";
        internal const string NativeFileTypeId = "2";
        internal const string ScrubbedFileTypeId = "3";
        internal const string ScrubbedText = "Scrubbed Text";
        internal const string ExtractedText = "Extracted Text";

        internal const string Unicode = "unicode";
        internal const string HelperFileName = @"\HelperFile.opt";
        internal const string TextHelperFileName = @"\Text.opt";
        internal const string OptFileExtension = ".opt";
        internal const string SearchAndKey = " AND ";
        internal const string Search_OR_Key = " OR ";
        internal const string CreatedDate = "CreatedDate";
        internal const string Ascending = "Ascending";
        internal const Int32 BatchSize = 100;
        internal const string ExportStartupWorkerRoleType = "1a557ebb-88dd-4219-867d-783d4d233b7b";
        internal const string ExportPathInvalid = "Specified export path is invalid.";
        internal const string SearchProductionOr = " OR ";
        internal const string ExportSearchNoRecords = "There is zero records to export.";
        internal const string FailureInCreateLoadFile = "Failed to create Load File ,validate Export path,file name and credentials.";
        internal const string ExportLoadFileExists = "Specified export file already exists in export path.";
        internal const string FailureInSearch = "Failed to fetch documents for selected export options.";

        internal const string NearNativeViewerSection = "NearNativeViewer";
        internal const string RedactItTimeout = "RedactItTimeout";
        internal const string HeartbeatFileName = "_heartbeat";
        internal const string StepTimeout = "&StepTimeout=";
        internal const string HttpPostMethod = "POST";
        internal const string HttpUlrEncoded=  "application/x-www-form-urlencoded";
        internal const string MessageDCN = "DCN:";
        internal const string MessageSpace = " ";
        internal const string MessageLawDocumentId = " Law document Id:";
        internal const string MessageHeartbeatFile = "RedactIt heartbeat file:";
        internal const string MessageVolume = "Volume:";
        internal const string DocumentXdl = "document.xdl";
        internal const string RedactItDocumentExtension ="xdl";
        internal const string RedactItPageExtension = "zdl";
        internal const string LawImageArchiveFolderName= "$Image Archive";
        internal const string LawEVImagesFolderName = "EVImages";
        internal const string LawSyncMissingImageMessage = "Missing image. Document image did not convert. ";
        internal const string LawSyncImageSendFailureMessage = "Failure in send document for Conversion. ";
        internal const string RedactItPagingNameFormat = "_page";
        internal const string HeartBeateeErrorMessage = "Redact-It reported error converting document DCN:{0} Heartbeat error:{1} Refer heartbeat file:{2} for more info.";
        internal const string  DocumentconversionTimeoutMessage= "Document conversion timeout.";
        internal const string GlobalDocumentconversionTimeoutMessage = "Global document conversion timeout.";
        internal const string ExceptionFolderToCleanup = "FolderToCleanup";
        internal const string LawSyncStartupFailureMessage = "Failed in initial setup, verify job settings and image folder path.";
        internal const string LawSyncFailureinGetDcoumentsMessage = "Failed to get document(s) from search engine. ";
        internal const string LawSyncFailureinCreateMetadata = "Failed in create Metadata (Field/Tag) on Law.";
        internal const string LawSyncFailureinGetMetadataMessage = "Failure in get and assign dataset Field Values into Law Metadata (Field/Tag).";
        internal const string LawSyncFailureinSyncMetadataMessage = "Metadata not updated. ";
        internal const string LawSyncFailureinSyncImageMessage = "Images not updated";
        internal const string LawSyncFailureinConversionMessage = "Failure in image conversion. ";
        internal const string LawSyncFailureinConversionTimeOutMessage = "Document Conversion Timeout. ";
        internal const string LawSyncFailureinRenameImage = "Failed to rename image file based on Law image naming standard";
        internal const string LawSyncFailureinImagingMessage = "Failure in document imaging. ";
        internal const string LawSyncConversionCallBackMethod = "LawService.svc/conversion/notification";
        internal const string LawSyncDocumentNotAvailable = "Document not available in Law PreDiscovery Case. ";
        public enum LawSynProcessStateErrorCodes
        {
            ImageSyncFailure = 821,
            MetadataSyncFailure = 822,
            ImageConversionFailure = 823,
            Successful = 824
        }
        #endregion

        #region job type
        internal const string JobType_Import = "import";
        internal const string JobType_Production = "production";
        internal const string JobType_Reconversion = "Reconversion";
        #endregion

        #region conversion reprocess
        
        internal const string ReProcessConversionContext = "Reprocess";
        internal const string BulkReProcessConversionContext = "BulkReprocess";
        
        internal const string OutputPipeNameToValidation     = "ConversionReprocessValidation";
        internal const string OutputPipeNameToConversionReprocessImport = "ConversionReprocessImport";
        internal const string OutputPipeNameToProductionPreprocess = "ProductionPreprocess";


        internal const string DocId = "DOCID";
        internal const string DocumentSetName = "DOCUMENTSET";

        internal const string ColumnDocReferenceId = "docReferenceId";
        internal const string ColumnCollectionId = "collectionId";
        internal const string ColumnDocTitle = "docTitle";
        internal const string ColumnDocText = "docText";
        internal const string ColumnFieldId = "fieldId";
        internal const string ColumnFieldvalue = "fieldvalue";

        #endregion

        internal const string NearDuplicationStartupWorkerRoleType = "ND-2b4407dc-b0be-42c0-bd33-718d9b22c3f0";
        internal const string NearDuplicationPorcessingWorkerRoleType = "ND-f3e8650b-8ead-475b-a8c8-53d23c012350";
        internal const string NearDuplicationEvUpdateWorkerRoleType = "ND-3e0db506-7718-4abd-a79c-97b3a971e1ad";
        internal const string NearDuplicationPolarisLicenseKeyName = "LicenseServer";
        internal const string NearDuplicationDocumentFileMissingMessage = "</br>Text file not available for DCN : ";
        internal const string NearDuplicationDocumentFileAccessMessage = "</br>Texts file not accessible  or empty content for DCN : ";
        internal const string NearDuplicationEvUpdateWorkerFailureMessage = "</br>Failed to update near dupe fields for DCN : {0}.";
        internal const string NearDuplicationNoDocuments = "No documents found in the selected Dataset.";
        internal const string NearDuplicationNoTextFile = "Text file not available for document(s) in the selected dataset.";
        internal const string NearDuplicationEngineFailure = "Unable to ingest documents. License may be invalid. Please check";
        internal const string NearDuplicationIngestFailure = "Failed to ingest document(s)";
        internal const string NearDuplicationResultNoDocuments = "No documents return from near duplicate result";
        internal const int NearDuplicationJobBatchSize = 100;
        internal const byte NearDuplicationFamilyThresholdDefaultValue = 85;
        internal const byte NearDuplicationClusterThresholdDefaultValue = 60;


        internal const string LawSyncStartupWorkerRoleType = "LS-6e9c645b-1fcb-45ea-a6ce-5437055bc736";
        internal const string LawSyncImagingWorkerRoleType = "LS-35752cc1-c85d-4f20-9b64-8ec0f79dfac4";
        internal const string LawSyncVaultReaderWorkerRoleType = "LS-37936962-0c5e-4b74-9dd5-80066cd4eee2";
        internal const string LawSyncUpdateWorkerRoleType = "LS-928227a8-026d-4256-870d-0e051ca5df3d";
       // internal const string LawSyncImageValidationWorkerRoleType = "LS-e1e9e553-e506-4693-b555-7755a4159abf";
        internal const string LawSyncImageUpdateWorkerRoleType = "LS-bc8390a0-6d11-4b98-8863-72778c1c5769";
        internal const string LawSyncReProcessStartupWorkerRoleType = "LS-be9afee7-98ad-45f7-8560-263cbed31a50";

        public const string Spilt = "Spilt";

        public const string SplitReviewsetPipeLineType = "SplitReviewset";

        #region "Bulk Tag"
        internal const string TagAllDuplicates = "Tag All Duplicates";
        internal const string TagAllDuplicatesBehaviorName = "TAD";
        internal const string TagAllFamilyBehaviorName = "TAF";
        internal const string TagAllThreadBehaviorName = "TADF";
        /// <summary>
        /// specifies document relationship type: thread
        /// </summary>
        internal const string OutlookEmailThread = "OutlookEmailThread";
        internal const string LotusNotesEmailThread = "LotusNotesEmailThread";

        internal const string DcnField = "DCN";
        internal const string DcnFieldType = "3000";
        #endregion
    }
}
