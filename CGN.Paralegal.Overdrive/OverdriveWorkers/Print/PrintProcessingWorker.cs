//---------------------------------------------------------------------------------------------------
// <copyright file="PrintProcessingWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Madhavan Murrali</author>
//      <description>
//          This file contains the PrintProcessingWorker.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//          <date value="22/4/2013">ADM – PRINTING – buddy defect fixes</date>
//          <date value="06/11/2013">CNEV2.2.1 - Defect# 143791, 143918, 142091,143787 - Licensing and Bulk printing Fix : babugx</date>
//          <date value="06-26-2013">Bug # 146526 -Disposing WebResponse object and error handling while pushing the document</date>
//          <date value="07/24/2013">Bug # 146819 - Discrepancy in document count isssue fix </date>
//          <date value="07/24/2013">Bug # 142090 - [Bulk Printing]: Bulk print job is getting failed when a field with blank value is chosen for file name </date>
//          <date value="08/30/2013">Bug # 146858 </date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//         <date value="03/14/2014">ADM-REPORTS-003  - Included code changes for New Audit Log</date>
//          <date value="04/02/2014">Bug fix 166874 </date>
//          <date value="04/16/2014">Fix for the 168375 </date>
//          <date value="03/24/2015">Bug Fix 184140 - Publish blank pages</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#region Namespace

using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.MatterManagement;
using LexisNexis.Evolution.Business.PrinterManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.JobManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Vault.AuditLog;
using LexisNexis.Evolution.Vault.Entities;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;

#endregion

namespace LexisNexis.Evolution.Worker
{
    public class PrintProcessingWorker : WorkerBase
    {
        #region Private Variables

        private BulkPrintServiceRequestBEO _mBootParameters;
        private string _mSharedLocation;
        private const int NativeSetBinaryType = 2;
        private const int ProductionSetBinaryType = 3;
        private const int ImageSetBinaryType = 2;
        private int _mTotalDocumentCount;
        private MappedPrinterBEO _mMappedPrinterToNetwork;
        private DatasetBEO _mDataSet;
        private string _mDatasetName;
        private UserBusinessEntity m_UserDetails;

        [NonSerialized] private HttpContextBase userContext;

        #endregion

        /// <summary>
        /// Process the message
        /// </summary>
        /// <param name="envelope">PipeMessageEnvelope</param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var printCollection = (PrintDocumentCollection) envelope.Body;
            InitializeForProcessing(BootParameters);
            _mTotalDocumentCount = printCollection.TotalDocumentCount;
            ProcessTheDocument(printCollection.Documents);
        }

        /// <summary>
        /// InitializeForProcessing
        /// </summary>
        /// <param name="printBootParameter">Print Boot Parameter</param>
        private void InitializeForProcessing(string printBootParameter)
        {
            if (!string.IsNullOrEmpty(printBootParameter))
            {
                //Creating a stringReader stream for the bootparameter
                var stream = new StringReader(printBootParameter);

                //Ceating xmlStream for xmlserialization
                var xmlStream = new XmlSerializer(typeof (BulkPrintServiceRequestBEO));

                //De serialization of boot parameter to get BulkPrintServiceRequestBEO
                _mBootParameters = (BulkPrintServiceRequestBEO) xmlStream.Deserialize(stream);
                _mSharedLocation = _mBootParameters.FolderPath;
            }
        }

        /// <summary>
        /// Processes the data.
        /// </summary>
        /// <param name="printDocuments"></param>
        public void ProcessTheDocument(List<DocumentResult> printDocuments)
        {
            if (_mBootParameters == null) return;
            if (string.IsNullOrEmpty(_mBootParameters.DataSet.CollectionId)) return;
            // Get mapped printer
            _mMappedPrinterToNetwork =
                PrinterManagementBusiness.GetMappedPrinter(
                    new MappedPrinterIdentifierBEO(
                        _mBootParameters.Printer.UniqueIdentifier.Split(Constants.Split).Last(), true));
            // Create folder
            CreateFoldersForTemporaryStorage();


            //Get Dataset and Matter information for a given Collection Id
            _mDataSet = DataSetBO.GetDataSetDetailForCollectionId(_mBootParameters.DataSet.CollectionId);

            //Get DataSet Fields 
            _mDataSet.DatasetFieldList.AddRange(
                DataSetBO.GetDataSetDetailForDataSetId(_mDataSet.FolderID).DatasetFieldList);
            // Get Matter information
            _mDataSet.Matter = MatterBO.GetMatterInformation(_mDataSet.FolderID);
            _mDatasetName = _mDataSet.FolderName;


            var documents = new List<DocumentResult>();
            var documentIdentifierEntities = new List<DocumentIdentifierEntity>();

            foreach (var document in printDocuments)
            {
                try
                {
                    string errorCode;
                    var separatorSheetFolder = Guid.NewGuid();
                    var separatorSheet = Path.Combine(Path.Combine(_mSharedLocation, _mBootParameters.Name),
                        Constants.SourceDirectoryPath, separatorSheetFolder.ToString(),
                        Constants.separatorHtml);
                    CreateseparatorSheet(separatorSheet, _mBootParameters.DataSet.MatterId,
                        _mBootParameters.DataSet.CollectionId, document.DocumentID);
                    //Print the document set
                    var jobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToInt32(PipelineId) : 0;
                    var jobId = JobMgmtDAO.GetJobIdFromJobRunId(jobRunId);
                    var status = PrintDocumentSet(jobId.ToString(CultureInfo.InvariantCulture), _mBootParameters, document,
                        separatorSheet, out errorCode);
                    if (status)
                    {
                        document.CreatedDate = DateTime.Now;
                        documents.Add(document);
                        // Log the message using Log worker...
                        LogMessage(document, true, string.Empty);
                    }
                    else
                    {
                        // Log the message using Log worker...
                        LogMessage(document, false, errorCode);
                    }
                    if (_mDataSet != null &&
                        _mDataSet.Matter != null)
                    {
                        var documentIdentifierEntity = new DocumentIdentifierEntity();
                        documentIdentifierEntity.CollectionId = document.CollectionID;
                        documentIdentifierEntity.Dcn = document.DocumentControlNumber;
                        documentIdentifierEntity.DocumentReferenceId = document.DocumentID;
                        documentIdentifierEntity.CollectionName = _mDataSet.FolderName;
                        documentIdentifierEntities.Add(documentIdentifierEntity);
                    }
                }
                catch (Exception ex)
                {
                    //report to director and continue with other documents if there is error in printing a documents
                    ex.Trace().Swallow();
                    ReportToDirector(ex);
                }
            }
            if (documents.Count > 0)
            {
                Tracer.Info("Print Processing worker - Document Count: {0}",
                    documents.Count.ToString(CultureInfo.InvariantCulture));
                Send(documents);
                if (_mDataSet != null &&
                    _mDataSet.Matter != null
                    )
                {
                    AuditLogFacade.LogDocumentsPrinted(_mDataSet.Matter.FolderID, documentIdentifierEntities);
                }
            }
        }

        private HttpContextBase CreateUserContext(string createdByGuid)
        {
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userProp = m_UserDetails;
            var userSession = new UserSessionBEO();
            SetUserSession(createdByGuid, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            return mockContext.Object;
        }

        /// <summary>
        /// This method sets the user details in session
        /// </summary>
        /// <param name="createdUserGuid">string</param>
        /// <param name="userProp">UserBusinessEntity</param>
        /// <param name="userSession">UserSessionBEO</param>
        private static void SetUserSession(string createdUserGuid, UserBusinessEntity userProp,
            UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdUserGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(DocumentResult document, bool success, string message)
        {
            var log = new List<JobWorkerLog<LogInfo>>();
            var taskId = Convert.ToInt32(document.DocumentControlNumber.Replace(_mDataSet.DCNPrefix, string.Empty));
            // form the parser log entity
            var parserLog = new JobWorkerLog<LogInfo>
            {
                JobRunId =
                    (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = taskId,
                WorkerRoleType = Constants.PrintProcessRoleId,
                WorkerInstanceId = WorkerId,
                IsMessage = false,
                Success = success, //!string.IsNullOrEmpty(documentDetail.DocumentProductionNumber) && success,
                CreatedBy = _mBootParameters.RequestedBy.UserId,
                LogInfo =
                    new LogInfo
                    {
                        TaskKey = document.DocumentControlNumber,
                        IsError = !success
                    }
            };
            parserLog.LogInfo.AddParameters(Constants.DCN, document.DocumentControlNumber);
            parserLog.LogInfo.AddParameters(message, _mMappedPrinterToNetwork.PrinterDetails.Name);
            log.Add(parserLog);
            SendLog(log);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<LogInfo>> log)
        {
            LogPipe.Open();
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }


        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<DocumentResult> documentList)
        {
            var documentCollection = new PrintDocumentCollection
            {
                Documents = documentList,
                DatasetName = _mDatasetName,
                TotalDocumentCount = _mTotalDocumentCount
            };
            var message = new PipeMessageEnvelope
            {
                Body = documentCollection
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(documentList.Count);
        }

        /// <summary>
        /// Print the document
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="request"></param>
        /// <param name="document"></param>       
        /// <param name="separatorSheet"></param>
        /// <param name="errorCode"></param>
        /// <returns></returns>
        private bool PrintDocumentSet(string jobId, BulkPrintServiceRequestBEO request, DocumentResult document,
            string separatorSheet, out string errorCode)
        {
            var toReturn = false;
            var title = string.Empty;
            var fieldValue = string.Empty;
            var binaryType = NativeSetBinaryType;
            var sb = new StringBuilder(string.Empty);
            //Get the binary types for the images
            switch (request.BulkPrintOptions.ImageType.ImageType)
            {
                case DocumentImageTypeBEO.Native:
                    binaryType = NativeSetBinaryType;
                    break;
                case DocumentImageTypeBEO.ImageSet:
                    binaryType = ImageSetBinaryType;
                    break;
                case DocumentImageTypeBEO.ProductionSet:
                    binaryType = ProductionSetBinaryType;
                    break;
            }
            //Get the document binary            
            var documentData = DocumentBO.GetDocumentBinaryData(long.Parse(request.DataSet.MatterId),
                request.BulkPrintOptions.ImageType.
                    ImageIdentifier.CollectionId,
                document.DocumentID, binaryType,
                string.Empty);

            #region Assertion

            documentData.ShouldNotBe(null);
            documentData.DocumentBinary.ShouldNotBe(null);

            #endregion

            var isFileExists = documentData.DocumentBinary.FileList.Count > 0;
            errorCode = string.Empty;
            foreach (var fileInfo in documentData.DocumentBinary.FileList)
            {
                if (!File.Exists(fileInfo.Path))
                {
                    isFileExists = false;
                    break;
                }
            }
            m_UserDetails = UserBO.GetUserUsingGuid(request.RequestedBy.UserId);
            userContext = CreateUserContext(request.RequestedBy.UserId);
            EVHttpContext.CurrentContext = userContext;
            //Construct Query string                       
            if (!isFileExists)
            {
                errorCode = "Bulk print Invalid file";
                toReturn = true;
            }
            else
            {
                fieldValue = document.DocumentControlNumber;
                if (document.Fields != null && document.Fields.Any())
                {
                    fieldValue = document.DocumentControlNumber;
                    title = Constants.TitleNotAvailable;
                    foreach (var field in document.Fields)
                    {
                        if (field == null) continue;
                        if (field.DataTypeId == Constants.TitleFieldType)
                            title = !string.IsNullOrEmpty(field.Value) ? fieldValue.Trim() : title;
                        if (String.IsNullOrEmpty(field.Name)) continue;
                        if (field.Name.Equals(_mBootParameters.FieldName))
                            fieldValue = !string.IsNullOrEmpty(field.Value) ? field.Value.Trim() : fieldValue;
                    }
                }
                var specialChars = new List<string> {"<", ">", ":", "\"", "\\", "|", "?", "*", "."};
                if (specialChars.Exists(x => fieldValue.Contains(x)))
                {
                    // Log documents with special characters or empty values...
                    toReturn = false;
                    errorCode = "Special characters in the field value and hence cannot be printed";
                }
                else
                {
                    ConstructQueryString(jobId, document, title, separatorSheet, sb, documentData, fieldValue);
                    try
                    {
                        toReturn = CreatePostWebRequestForCreatingPdf(jobId, sb.ToString());
                    }
                    catch (WebException webException)
                    {
                        webException.Trace().Swallow();
                    }
                    catch (Exception ex)
                    {
                        ex.Trace().Swallow();
                        errorCode = "Error in Conversion and hence cannot be printed";
                    }
                }
            }
            return toReturn;
        }


        /// <summary>
        /// Delete the folder.
        /// </summary>
        /// <param name="folderToCleanUp">Folder to clean up</param>
        /// <returns>Success or failure</returns>
        private void SafeDeleteFolder(string folderToCleanUp)
        {
            //Clean up the folder once atomic work is completed
            try
            {
                if (Directory.Exists(folderToCleanUp))
                {
                    Directory.Delete(folderToCleanUp, true);
                }
            }
            catch (Exception ex)
            {
                ex.Data["FolderToCleanup"] = folderToCleanUp;
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// This is the method that sends the web request to redact it.
        /// </summary>
        /// <param name="requestId">request id</param>
        /// <param name="queryString">Query String</param>
        /// <returns>Status of operation</returns>
        public bool CreatePostWebRequestForCreatingPdf(string requestId, string queryString)
        {
            requestId.ShouldNotBeEmpty();
            queryString.ShouldNotBeEmpty();
            string status;
            var success = true;

            var request =
                WebRequest.Create(CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.QueueServerUrl));
            var byteArray = Encoding.UTF8.GetBytes(queryString);
            request.Method = Constants.Post;
            request.ContentType = Constants.UrlEncodedContentType;
            request.ContentLength = byteArray.Length;
            using (var dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            using (var response = request.GetResponse())
            {
                status = ((HttpWebResponse) response).StatusDescription.ToUpper().Trim();
            }
            if (status.Equals(Constants.Ok))
            {
                EvLog.WriteEntry(requestId + Constants.Colon + Constants.CallPushMethodCalled, status,
                    EventLogEntryType.Information);
            }
            else
            {
                success = false;
                EvLog.WriteEntry(
                    requestId + Constants.Colon + Constants.CallPushMethodCalled +
                    Constants.RedactitServerResponseWithStatus, status, EventLogEntryType.Error);
            }
            return success;
        }

        /// <summary>
        /// Construct Query string for Redact-it
        /// </summary>
        /// <param name="jobId">Job Id</param>
        /// <param name="document">Document:Contains the document details (DocumentID) and field information's(filed type)</param>
        /// <param name="title">Title</param>
        /// <param name="separatorSheet">separator sheet path</param>
        /// <param name="sb">String builder</param>
        /// <param name="documentInfo">RVWDocumentBEO</param>
        /// <param name="fieldValue"></param>
        public void ConstructQueryString(string jobId, DocumentResult document, string title, string separatorSheet,
            StringBuilder sb, RVWDocumentBEO documentInfo, string fieldValue)
        {
            var targetDirectoryPath = Path.Combine(_mSharedLocation, _mBootParameters.Name);
            //Url Encode for target file path
            targetDirectoryPath = HttpUtility.UrlEncode(targetDirectoryPath);
            separatorSheet = HttpUtility.UrlEncode(separatorSheet);
            var sourceCount = 0;
            if (documentInfo.DocumentBinary.FileList.Count > 0)
            {
                //Adds the separator sheet for each document
                sb.Append(Constants.Source.ConcatStrings(new List<string>
                {
                    sourceCount.ToString(CultureInfo.InvariantCulture),
                    Constants.EqualTo,
                    separatorSheet,
                    Constants.Ampersand
                }));
                foreach (var t in documentInfo.DocumentBinary.FileList)
                {
                    sourceCount++;
                    //Adds the native/image/production files for each document
                    sb.Append(Constants.Source.ConcatStrings(new List<string>
                    {
                        sourceCount.ToString(CultureInfo.InvariantCulture),
                        Constants.EqualTo,
                        HttpUtility.UrlEncode(t.Path),
                        Constants.Ampersand
                    }));
                }
            }
            else
            {
                sb.Append(Constants.Source0.ConcatStrings(new List<string>
                {
                    separatorSheet,
                    Constants.Source1,
                    HttpUtility.UrlEncode(documentInfo.NativeFilePath)
                }));
            }
            sb.Append(Constants.Target.ConcatStrings(new List<string>
            {
                targetDirectoryPath,
                Constants.CSFName,
                fieldValue,
                Constants.PdfAndNotificationUrl,
                CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.CallBackUri),
                Constants.BulkPrintRequestId,
                jobId
            }));
            sb.Append(Constants.MatterId.ConcatStrings(new List<string>
            {
                document.MatterID.ToString(),
                Constants.CollectionId,
                document.CollectionID,
                Constants.DocumentId,
                document.DocumentID
            }));
            sb.Append(Constants.Title + title);
            sb.Append(Constants.NotificationVerb);
            sb.Append(Constants.PublishBlankPagesQueryString);
            sb.Append(CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.PublishBlankPages));

        }


        /// <summary>
        /// Create separator between the documents
        /// </summary>
        /// <param name="path">path</param>
        /// <param name="matterId">matter id</param>
        /// <param name="collectionId">collection id</param>
        /// <param name="documentId">document id</param>
        public void CreateseparatorSheet(string path, string matterId, string collectionId, string documentId)
        {
            path.ShouldNotBeEmpty();
            matterId.ShouldNotBeEmpty();
            collectionId.ShouldNotBeEmpty();
            documentId.ShouldNotBeEmpty();

            var title = string.Empty;
            var author = string.Empty;
            var documentCtlNo = string.Empty;
            //Get title and Author of the document
            GetDocumentMetaData(matterId, collectionId, documentId, ref title, ref author, ref documentCtlNo);
            //Get Document level tag
            var tagList = new List<RVWTagBEO>();
            DocumentBO.GetDocumentTags(ref tagList, matterId, collectionId, documentId, false);

            //Construct Separator sheet in the shared
            var sb = new StringBuilder(string.Empty);
            sb.Append(Constants.HtmlHeadForseparatorSheet);
            sb.Append(Constants.HtmlContentForDCN);
            sb.Append(documentCtlNo);
            sb.Append(Constants.HtmlContentForTitle);
            sb.Append(title);
            sb.Append(Constants.HtmlContentForAuthor);
            sb.Append(author);
            sb.Append(Constants.HtmlContentForTags);
            //Add tags to Html Content
            var alt = 0;
            tagList.ForEach(o => AddTags(sb, ref alt, o));
            sb.Append(Constants.HtmlContentForEndOfseparatorSheet);
            var folderName = path.Substring(0, path.LastIndexOf("\\"));
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Add Tag value to the html
        /// </summary>
        /// <param name="sb">StringBuilder</param>
        /// <param name="alt">row count</param>
        /// <param name="tag">Tag Information Object</param>
        /// <returns>String Builder</returns>
        private static StringBuilder AddTags(StringBuilder sb, ref int alt, RVWTagBEO tag)
        {
            if ((++alt)%2 > 0)
            {
                // Append tag related information to string builder
                sb.Append(String.Format("{0}{1}{2}", Constants.HtmlContentForTagNameStart, tag.TagDisplayName,
                    Constants.HtmlContentForTagNameEnd));
            }
            else
            {
                // Append tag related information to string builder but with difference for odd and even rows we add extra css calss to alternate row
                sb.Append(String.Format("{0}{1}{2}", Constants.HtmlContentForTagNameStartAlternate, tag.TagDisplayName,
                    Constants.HtmlContentForTagNameEnd));
            }
            return sb;
        }

        /// <summary>
        /// Get Title ,author and  document control number
        /// </summary>
        /// <param name="matterId">matter id</param>
        /// <param name="collectionId">collection id</param>
        /// <param name="documentId">document id</param>
        /// <param name="title">title</param>
        /// <param name="author">author</param>
        /// <param name="docCtlNumber">Document control number</param>
        private void GetDocumentMetaData(string matterId, string collectionId, string documentId, ref string title,
            ref string author, ref string docCtlNumber)
        {
            author = Constants.NotApplicable;
            title = Constants.NotApplicable;
            docCtlNumber = Constants.NotApplicable;
            string fileSize = null;
            string datasetName = null;
            DocumentBO.GetDocumentMetaDataForPrintAndEmail(matterId,collectionId,documentId, ref title, ref author, ref docCtlNumber,ref fileSize,ref datasetName);
        }


        /// <summary>
        /// Get the DCN value for a document
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="collectionId">Collection Id</param>
        /// <param name="documentId">Document Id</param>
        /// <returns>DCN Value</returns>
        public static
            string GetDcnFieldValue
            (string matterId, string collectionId, string documentId)
        {
            matterId.ShouldNotBeEmpty();
            collectionId.ShouldNotBeEmpty();
            documentId.ShouldNotBeEmpty();
            var fields = DataSetBO.GetDatasetFieldsOfType(matterId, new Guid(collectionId), Constants.DCNFieldTypeId);
            var dcnFieldValue = string.Empty;
            if (fields.Count > 0)
            {
                var dcnField = DocumentBO.GetDocumentFieldById(matterId, collectionId, documentId, fields[0].ID);
                dcnFieldValue = dcnField.FieldValue;
            }
            return string.IsNullOrEmpty(dcnFieldValue) ? string.Empty : dcnFieldValue;
        }

        /// <summary>
        /// Create Folders for Temporary storage of files for delivery options
        /// </summary>
        protected
            void CreateFoldersForTemporaryStorage
            ()
        {
            if (!Directory.Exists(_mSharedLocation))
            {
                throw new EVException().AddResMsg(ErrorCodes.SourceDirectoryNotExists);
            }
            var deliveryOptionsPath = Path.Combine(_mSharedLocation, _mBootParameters.Name);
            if (!Directory.Exists(deliveryOptionsPath))
            {
                try
                {
                    Directory.CreateDirectory(deliveryOptionsPath);
                }
                catch (Exception ex)
                {
                    ex.AddResMsg(ErrorCodes.WriteAccessForSharePathIsDenied);
                    throw;
                }
            }
            var sourceDirectoryPath = Path.Combine(Path.Combine(_mSharedLocation, _mBootParameters.Name),
                Constants.SourceDirectoryPath);
            if (!Directory.Exists(sourceDirectoryPath))
            {
                try
                {
                    Directory.CreateDirectory(sourceDirectoryPath);
                }
                catch (Exception ex)
                {
                    ex.AddResMsg(ErrorCodes.WriteAccessForSharePathIsDenied);
                    throw;
                }
            }
        }
    }
}