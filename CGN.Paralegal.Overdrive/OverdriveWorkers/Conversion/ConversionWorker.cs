
#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Keerti/Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  ConversionWorker
//      </description>
//      <changelog>
//          <date value="01/17/2012">Bugs Fixed #95197-Made a fix for email redact issue by using overdrive non-linear pipeline </date>
//          <date value="01-Mar-2012">Fix for bug 96538</date>
//          <date value="18-04-2012">Task #98920</date>
//          <date value="04/06/2013">ADM -ADMIN-002 -  Near Native Conversion Priority</date>
//          <date value="05-12-2013">Task # 134432-ADM 03 -Re Convresion</date>
//          <date value="05-21-2013">Bug # 142937,143536 and 143037 -ReConvers Buddy Defects</date>
//          <date value="06-05-2013">Bug # 143924 -  Handled empty/ no file case for import conversion</date>
//          <date value="06-06-2013">Bug # 143682-Fix to reprocess the partially converted document</date>
//          <date value="06-26-2013">Bug # 146526 -Disposing WebResponse object and error handling while pushing the document</date>\
//          <date value="08-09-2013">Bug # 148380 -Fix to  avoid the call back process state update before bulk insert talk place in conversion worker </date>
//          <date value="02/28/2014">Included error handling </date>
//          <date value="03/24/2015">Bug Fix 184140 - Publish blank pages</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
#region All Namespaces
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.External;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.TraceServices;

#endregion All Namespaces

namespace LexisNexis.Evolution.Worker
{
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Pushes given list of documents for conversion.
    /// </summary>
    public class ConversionWorker : WorkerBase
    {
        private INearNativeConverter m_Converter = null;
        private IDocumentVaultManager vaultManager = null;
        private long _matterId = 0;
        private bool isNativeSetIncluded;

        internal class Constants
        {
            internal const string NATIVEFILETYPE = "Native";
            internal const string TEXTFILETYPE = "Text";
            internal const string IMAGEFILETYPE = "Image";
            internal const string EdocsImport = "ImportEdocs";
            internal const string ImportLoadFileAppendPipeLineTypeName = "ImportLoadFileAppend";
            internal const string ImportLoadFileOverlayPipeLineTypeName = "ImportLoadFileOverlay";
            internal const string ImportDcbPipeLineTypeName = "ImportDcb";
            internal const string ImportLoadFilePipeLineTypeName = "ImportLoadFile";
            internal const string ImportLawPipeLineTypeName = "ImportLaw";
            internal const string PublishBlankPages = "PublishBlankPages";

        }

        #region Job Framework functions

        /// <summary>
        /// Processes the work item. pushes give document files for conversion
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                DocumentCollection documentCollection = null;
                IEnumerable<DocumentDetail> documentDetails = null;

                message.ShouldNotBe(null);

                if (!message.IsPostback)
                {
                    Send(message);
                }

                #region Extract document details from incoming message object.

                documentCollection = message.Body as DocumentCollection;

                #region Assertion

                documentCollection.ShouldNotBe(null);

                #endregion

                    Debug.Assert(documentCollection != null, "documentCollection != null");
                _matterId = documentCollection.dataset.Matter.FolderID;
                documentDetails = documentCollection.documents;

                    if (documentDetails == null || !documentDetails.Any()) throw new EVException().AddErrorCode(ErrorCodes.NoDocumentsInConversionWorker); //?? resource file.

                var updateDocument = documentDetails.Where(p => (!p.IsNewDocument));
                if (updateDocument.Any())
                {
                        using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
                    {
                        // Incase of Overlay, for specific documents delete existing Document Binary                          
                        //1)Native Document- Delete files from nativeset collection(i.e dataset)
                            var nativeDoc = documentDetails.Where(p => (!p.IsNewDocument && p.docType == DocumentsetType.NativeSet));
                            if (nativeDoc.Any() && documentCollection.IsIncludeNativeFile) //If native file included then only need to delete existing native file documents.
                        {
                            DeleteNearNativeForOverlay(nativeDoc);
                        }
                        //2) Image Document -Delete files from imageset collection
                            var imageDoc = documentDetails.Where(p => (!p.IsNewDocument && p.docType == DocumentsetType.ImageSet));
                            if (imageDoc.Any())
                        {
                            DeleteNearNativeForOverlay(imageDoc);
                        }

                    }
                }

                #endregion Extract document details from incoming message object.

                #region Loop through documents and push for conversion

                if (documentDetails.Any())
                {
                    //send native set and image set documents seperatley
                    //this helps in tracking the correct processed documents
                    //For Eg:If native set is not selected then then native document treated as not processed
                        ProcessDocuments(documentDetails.Where(doc => doc.docType == DocumentsetType.NativeSet),
                            documentCollection, true);
                        ProcessDocuments(documentDetails.Where(doc => doc.docType == DocumentsetType.ImageSet),
                            documentCollection, false);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                //LogMessage(false, ex.ToUserString());
            }
            #endregion Loop through documents and push for conversion
        }

        /// <summary>
        /// Processes the documents.
        /// </summary>
        /// <param name="documentDetails">The document details.</param>
        /// <param name="documentCollection">The document collection.</param>
        /// <param name="isNativeDocuments">if set to <c>true</c> [is native documents].</param>
        private void ProcessDocuments(IEnumerable<DocumentDetail> documentDetails, DocumentCollection documentCollection, bool isNativeDocuments)
        {
            if (documentDetails == null || !documentDetails.Any()) return;

            // get list of documents from Document Details.
            var documents = documentDetails.Select(x => x.document);

            // Instantiation 
            var documentCollectionRetryList = new DocumentCollection()
                                                                 {
                                                                     documents = new List<DocumentDetail>(),
                                                                     dataset = documentCollection.dataset,
                                                                     Originator = Guid.NewGuid().ToString()
                                                                 };

            List<RVWDocumentBEO> queuedDocuments = new List<RVWDocumentBEO>();
            var docToPush = new Dictionary<string, IEnumerable<string>>();

            // loop through documents to push each document for conversion
            foreach (RVWDocumentBEO document in documents)
            {
                if (document.MatterId == 0 || String.IsNullOrEmpty(document.CollectionId))
                {
                    continue;
                }
                //Gets the List of documents for conversion
                GetDocumentForConversion(document, documentCollectionRetryList, m_Converter, queuedDocuments, docToPush,
                                         PipelineType.Moniker);
            }

            //Push documents for conversion
            PushDocumentsForConversion(queuedDocuments, docToPush, isNativeDocuments);
            IncreaseProcessedDocumentsCount(queuedDocuments.Count);

            #region If there are any documents in retry list - put back to Conversion worker. Those files are yet being created.

            if (documentCollectionRetryList.documents.Any())
            {
                InputDataPipe.Send(new PipeMessageEnvelope() { Body = documentCollectionRetryList, IsPostback = true });
            }

            #endregion If there are any documents in retry list - put back to Conversion worker. Those files are yet being created.
        }

        /// <summary>
        /// Method to Push the documents for conversion
        /// </summary>
        /// <param name="queuedDocuments">queuedDocuments</param>
        /// <param name="docToPush">docToPush</param>
        /// <param name="isNativeDocuments"> </param>
        private void PushDocumentsForConversion(List<RVWDocumentBEO> queuedDocuments, Dictionary<string, IEnumerable<string>> docToPush, bool isNativeDocuments)
        {
            if (queuedDocuments == null || !queuedDocuments.Any() || docToPush == null)
            {
                Tracer.Trace("No Documents Found To Push ");
                return;
            }
            DoPreConversionValidationAndMarkProcessStae(queuedDocuments, docToPush, isNativeDocuments);
            SendDocumentsForConversionAndMarkProcessState(queuedDocuments, docToPush, isNativeDocuments);
        }

        /// <summary>
        /// Sends the state of the documents for conversion and mark process.
        /// </summary>
        /// <param name="queuedDocuments">The queued documents.</param>
        /// <param name="docToPush">The doc to push.</param>
        /// <param name="isNativeDocuments">if set to <c>true</c> [is native documents].</param>
        private void SendDocumentsForConversionAndMarkProcessState(IEnumerable<RVWDocumentBEO> queuedDocuments, Dictionary<string, IEnumerable<string>> docToPush, bool isNativeDocuments)
        {
            var documentConversionLogBeos = new List<DocumentConversionLogBeo>();
            foreach (var document in queuedDocuments)
            {
                byte processStatus = EVRedactItErrorCodes.Submitted;
                int reasonId = EVRedactItErrorCodes.Na;

                IEnumerable<string> fileListForConversion = null;
                string fileListKey = document.DocumentId + ":" + document.CollectionId;
                docToPush.TryGetValue(fileListKey, out fileListForConversion);
                if (fileListForConversion == null || !fileListForConversion.Any())
                {
                    //if native is not selected then there wont be entry for processed document
                    if (isNativeDocuments && !isNativeSetIncluded) continue;
                }
                try
                {
                    m_Converter.PushDocumentForConversion(
                        document.MatterId.ToString(CultureInfo.InvariantCulture),
                        document.CollectionId,
                        document.DocumentId,
                        docToPush[fileListKey], CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.PublishBlankPages));
                }
                catch (WebException webException)
                {
                    if (webException.Response != null)
                    {
                        webException.AddDbgMsg("ResponseURI = {0}", webException.Response.ResponseUri);
                    }
                    webException.Trace().Swallow();
                    processStatus = EVRedactItErrorCodes.Failed;
                    reasonId = EVRedactItErrorCodes.FailedToSendFile;
                }
                catch (Exception ex)
                {
                    processStatus = EVRedactItErrorCodes.Failed;
                    //if document is not supprot type then 1012 un support file type is the reason and
                    //other problem in submitting the document (1059) is the reason
                    reasonId = Utils.GetConversionErrorReason(ex.GetErrorCode());

                    // Prevent certain exceptions from polluting the log
                    if (ex.GetErrorCode().Equals("140") || // "No files associated with the document provided for conversion."
                        ex.GetErrorCode().Equals("138")) // "... extension not mapped to a queue"
                    {
                        ex.Swallow();
                    }
                    else
                    {
                        ex.Trace().Swallow();
                    }

                    LogMessage(false, ex.ToUserString());
                }

                //Update  post push validation state
                if(processStatus!=EVRedactItErrorCodes.Submitted)
                documentConversionLogBeos.Add(ConvertToConversionLogBeo(document, processStatus, (short) reasonId));
            }

            BulkAddOrUpdateProcessedDocuments(_matterId, documentConversionLogBeos, true);
        }



        /// <summary>
        /// Does the pre conversion validation and mark process stae.
        /// </summary>
        /// <param name="queuedDocuments">The queued documents.</param>
        /// <param name="docToPush">The doc to push.</param>
        /// <param name="isNativeDocuments">if set to <c>true</c> [is native documents].</param>
        private void DoPreConversionValidationAndMarkProcessStae(List<RVWDocumentBEO> queuedDocuments, Dictionary<string, IEnumerable<string>> docToPush, bool isNativeDocuments)
        {
            //Pre Conversion Validation
            var documentConversionLogBeos = new List<DocumentConversionLogBeo>();
         

            foreach (var document in queuedDocuments)
            {
                var fileListKey = document.DocumentId + ":" + document.CollectionId;
                byte processStaus;
                short reasonId;
                try
                {
                    IEnumerable<string> fileListForConversion = null;
                    docToPush.TryGetValue(fileListKey, out fileListForConversion);
                    if (fileListForConversion == null || !fileListForConversion.Any())
                    {
                        //if native is not selected then there wont be entry for processed document
                        if ( (isNativeDocuments && !isNativeSetIncluded)
                              || (!isNativeDocuments && document.IsImageFilesNotAssociated)) continue;  //if image is not part of the document then there wont be entry for processed document
                      

                        //Reason Id 1007 : can not find file 
                        documentConversionLogBeos.Add(ConvertToConversionLogBeo(document, EVRedactItErrorCodes.Failed,
                                                                                EVRedactItErrorCodes.CanNotFindFile));
                        continue;
                    }

                    var filesWithReasonCodes = Utils.GetReasonCodes(fileListForConversion);
                    docToPush.Remove(fileListKey);
                    docToPush[fileListKey] = filesWithReasonCodes.Item1;
                    processStaus = filesWithReasonCodes.Item3;
                    reasonId = filesWithReasonCodes.Item2;
                    
                }
                catch (Exception ex)
                {
                    //if there is problem in figuring if files exists on disk , marking them as Can not find file 
                    ex.AddUsrMsg("Not able to find the exact status of the files for the document  {0}", fileListKey);
                    processStaus = EVRedactItErrorCodes.Failed;
                    reasonId = EVRedactItErrorCodes.CanNotFindFile;
                    ReportToDirector(ex);
                    ex.Trace().Swallow();
                    //LogMessage(false, ex.ToUserString());
                }
                documentConversionLogBeos.Add(ConvertToConversionLogBeo(document, processStaus, reasonId));
            }
            //Mark Documents with Process State
            BulkAddOrUpdateProcessedDocuments(_matterId, documentConversionLogBeos, false);
        }

        /// <summary>
        /// Converts to conversion log beo.
        /// </summary>
        /// <param name="rvwDocumentBeo">The RVW document beo.</param>
        /// <param name="processStatus">The process status.</param>
        /// <param name="reasonId">The reason id.</param>
        /// <returns></returns>
        private DocumentConversionLogBeo ConvertToConversionLogBeo(RVWDocumentBEO rvwDocumentBeo, byte processStatus = EVRedactItErrorCodes.Submitted, short reasonId = EVRedactItErrorCodes.Na)
        {
            return new DocumentConversionLogBeo()
                       {
                           DocumentId = rvwDocumentBeo.DocumentId,
                           CollectionId = rvwDocumentBeo.CollectionId,
                           JobRunId = WorkAssignment.JobId,
                           ProcessJobId = WorkAssignment.JobId,
                           DCN = rvwDocumentBeo.DocumentControlNumber,
                           CrossReferenceId =
                               !string.IsNullOrEmpty(rvwDocumentBeo.CrossReferenceFieldValue)
                                   ? rvwDocumentBeo.CrossReferenceFieldValue
                                   : "N/A",
                           Status = processStatus,
                           ReasonId = reasonId,
                           ModifiedDate = DateTime.UtcNow,
                           CreatedDate = DateTime.UtcNow

                       };
        }



        /// <summary>
        /// Bulks the add or update processed documents.
        /// </summary>
        /// <param name="matterId">The matter id.</param>
        /// <param name="conversionLogBeos">The conversion log beos.</param>
        /// <param name="isUpdate">if set to <c>true</c> [is update].</param>
        private void BulkAddOrUpdateProcessedDocuments(long matterId, IEnumerable<DocumentConversionLogBeo> conversionLogBeos, bool isUpdate)
        {
            try
            {
                if (conversionLogBeos == null || !conversionLogBeos.Any()) return;
                vaultManager.AddOrUpdateConversionLogs(matterId, conversionLogBeos.ToList(), isUpdate);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                //LogMessage(false, ex.ToUserString());
            }
        }

        protected override void EndWork()
        {
            base.EndWork();
        }
        /// <summary>
        /// Get Job Parameters
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bootParameters"></param>
        /// <returns></returns>
        private static Object GetJobParameters<T>(string bootParameters)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            var stringReader = new StringReader(bootParameters);
            object jobParametersBeo = xmlSerializer.Deserialize(stringReader);
            stringReader.Close();
            return jobParametersBeo;
        }
        /// <summary>
        /// Pushes the document for conversion.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="documentCollectionToReSend">The document collection to re send.</param>
        /// <param name="converter">The converter.</param>
        private void GetDocumentForConversion(RVWDocumentBEO document,
            DocumentCollection documentCollectionToReSend, INearNativeConverter converter, List<RVWDocumentBEO> nativeDocuments, Dictionary<string,
            IEnumerable<string>> filesToPush, string importType)
        {
            // get list of files to be converted from document object.
            IEnumerable<string> fileListForConversion = GetFilePathsForRedactItConversion(document);
            if (importType.Equals(Constants.EdocsImport))
            {
                if (fileListForConversion != null)// if the file list is available - attempt to push for conversion
                {
                    // Push all documents that exist and NOT zero byte files for conversion.
                    IEnumerable<string> existingFileList = fileListForConversion.Where(f => File.Exists(f)
                                                                                && (new FileInfo(f).Length > 0));

                    if (existingFileList.Count() == fileListForConversion.Count())
                    {
                        nativeDocuments.Add(document);
                        filesToPush.Add(document.DocumentId + ":" + document.CollectionId, fileListForConversion);
                    }
                    else
                    {
                        #region If files doesn't exist yet, add them to a list so that they are put back to the Conversion Worker input data pipe.

                        // Files that are not ready for conversion need to be put back to Conversion worker - next time around they might have been created.
                        // if there is a null file we shouldn't resend it to Conversion worker - hence no else clause.                        
                        if (fileListForConversion.Where(f => !File.Exists(f)).FirstOrDefault() != null)
                        {
                            documentCollectionToReSend.documents.Add(new DocumentDetail() { document = document, });
                        }
                        #endregion If files doesn't exist yet, add them to a list so that they are put back to the Conversion Worker input data pipe.
                    }
                }
            }
            else
            {
                nativeDocuments.Add(document);
                filesToPush.Add(document.DocumentId + ":" + document.CollectionId, fileListForConversion);
            }
        }

        /// <summary>
        /// Deletes the near native for overlay.
        /// </summary>
        /// <param name="overlayDocumentDetailObjects">The overlay document detail objects.</param>
        private void DeleteNearNativeForOverlay(IEnumerable<DocumentDetail> overlayDocumentDetailObjects)
        {
            try
            {
                if (overlayDocumentDetailObjects != null && overlayDocumentDetailObjects.Any())
                {
                    // extract RVWDocumentBEO objects out of overlay document detail objects.
                    IEnumerable<RVWDocumentBEO> overlayDocuments = overlayDocumentDetailObjects.Select(p => p.document);

                    // Call Document Vault Manager function delete document binary
                    if (overlayDocuments.Any())
                    {
                        var documentVaultManager = new DocumentVaultManager();
                        documentVaultManager.DeleteNearNativeDocumentBinary(overlayDocuments);
                        //Delete document Redaction
                        var firstOrDefault = overlayDocuments.FirstOrDefault();
                        if (firstOrDefault != null)
                        {
                            documentVaultManager.DeleteDocumentsMetaData(_matterId, firstOrDefault.CollectionId, overlayDocuments, 4);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                //LogMessage(false, ex.ToUserString());
            }
        }

        /// <summary>
        /// Begins the worker process.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();
            int nearNativePriority = -100;
            var edocsNDcbPipeLineTypeNames = new List<string>() { Constants.EdocsImport, Constants.ImportDcbPipeLineTypeName };
            var loadFilePipeLineTypeNames = new List<string>()
                                                {
                                                    Constants.ImportLoadFileAppendPipeLineTypeName,
                                                    Constants.ImportLoadFileOverlayPipeLineTypeName
                                                };

            try
            {
                if (!string.IsNullOrEmpty(PipelineType.Moniker))
                {
                    if (edocsNDcbPipeLineTypeNames.Contains(PipelineType.Moniker))
                    {
                        var importJobParameters = GetJobParameters<ProfileBEO>(BootParameters) as ProfileBEO;
                        importJobParameters.ShouldNotBe(null);
                        nearNativePriority = importJobParameters.NearNativeConversionPriority;
                        isNativeSetIncluded = true;
                    }
                    else if (PipelineType.Moniker.Contains(Constants.ImportLoadFilePipeLineTypeName) ||
                             loadFilePipeLineTypeNames.Contains(PipelineType.Moniker))
                    {
                        var importJobParameters = GetJobParameters<ImportBEO>(BootParameters) as ImportBEO;
                        importJobParameters.ShouldNotBe(null);
                        nearNativePriority = importJobParameters.NearNativeConversionPriority;
                        isNativeSetIncluded = importJobParameters.IsImportNativeFiles;
                    }
                    else if (PipelineType.Moniker.Equals(Constants.ImportLawPipeLineTypeName))
                    {
                        var importJobParameters = GetJobParameters<LawImportBEO>(BootParameters) as LawImportBEO;
                        importJobParameters.ShouldNotBe(null);
                        nearNativePriority = importJobParameters.NearNativeConversionPriority;
                        isNativeSetIncluded = importJobParameters.IsImportNative;
                    }
                }

                vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Worker.Constants.DocumentVaultManager);
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Problem in reading the near native conversion priority");
                ex.Trace().Swallow();
                ReportToDirector(ex);
                //LogMessage(false, ex.ToUserString());
            }
            m_Converter = new NearNativeConversionAdapter(true, WorkAssignment.JobId.ToString(CultureInfo.InvariantCulture), nearNativePriority);
        }



        #endregion Job Framework functions

        #region Private helper functions
        /// <summary>
        /// Gets the file paths for redact it conversion.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        private IEnumerable<string> GetFilePathsForRedactItConversion(RVWDocumentBEO document)
        {
            List<string> finalFileList = new List<string>();
            try
            {
                if (document != null &&
                    document.DocumentBinary != null &&
                        document.DocumentBinary.FileList != null &&
                            document.DocumentBinary.FileList.Count > 0)
                {
                    IEnumerable<RVWExternalFileBEO> imageAndNativeFiles = document.DocumentBinary.FileList
                        .Where(p => p.Type.Equals(Constants.IMAGEFILETYPE) || p.Type.Equals(Constants.NATIVEFILETYPE));

                    // images to be converted.
                    if (imageAndNativeFiles != null && imageAndNativeFiles.Any())
                    {
                        finalFileList = imageAndNativeFiles.Select(p => p.Path).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                //LogMessage(false, ex.ToUserString());
            }

            return finalFileList;
        }

        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="message">The document batch.</param>
        private void Send(PipeMessageEnvelope message)
        {
            if (OutputDataPipe != null)
            {
                OutputDataPipe.Send(message);
            }
        }

        private void LogMessage(bool success, string information)
        {
            List<JobWorkerLog<BaseWorkerProcessLogInfo>> log = new List<JobWorkerLog<BaseWorkerProcessLogInfo>>();
            JobWorkerLog<BaseWorkerProcessLogInfo> logEntry = new JobWorkerLog<BaseWorkerProcessLogInfo>();
            logEntry.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            logEntry.CorrelationId = 0;// TaskId
            logEntry.WorkerRoleType = "051c994f-a843-428f-92e9-1958b87f0015";
            logEntry.WorkerInstanceId = WorkerId;
            logEntry.IsMessage = false;
            logEntry.Success = success;
            logEntry.CreatedBy = "N/A";
            logEntry.LogInfo = new BaseWorkerProcessLogInfo();
            logEntry.LogInfo.Information = information;
            if (!success)
            {
                logEntry.LogInfo.Message = information;
            }
            log.Add(logEntry);
            SendLog(log);
        }

        private void SendLog(List<JobWorkerLog<BaseWorkerProcessLogInfo>> log)
        {
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        #endregion Private helper functions
    }
}
