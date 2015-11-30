using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System.Text;
using LexisNexis.Evolution.External.Law;


namespace LexisNexis.Evolution.Worker
{
    public class LawSyncImageUpdateWorker : WorkerBase
    {
        private LawSyncBEO _jobParameter;
        private TimeSpan _documentConversionTimeout;
        private TimeSpan _documentConverisonGlobalTimeout;
       
        private List<DocumentConversionLogBeo> _documentProcessStateList;
        private string _datasetCollectionId;
        private long _lawSyncJobId;
        private IDocumentVaultManager _vaultManager;
        private List<JobWorkerLog<LawSyncLogInfo>> _logInfoList;
        private IEVLawAdapter _lawEvAdapter;
        private string _imageArchiveDirectory;

        /// <summary>
        /// Represent one page (tif) of converted image
        /// </summary>
        private class ConvertedImage
        {
            public int PageNo;
            public string RelativePath;
        }
        protected override void BeginWork()
        {
             base.BeginWork();
            _documentConversionTimeout = ConversionHelper.GetDocumentConversionTimeout();
            _documentConverisonGlobalTimeout = ConversionHelper.GetDocumentGlobalConversionTimeout();
            _vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                if (message.Body == null)
                {
                    return;
                }
                var lawDocumentsList = (LawSyncDocumentCollection) message.Body;
                if (_jobParameter == null)
                {
                    if (lawDocumentsList.IsLawSyncReprocessJob)
                        _jobParameter = lawDocumentsList.OrginalJobParameter;
                    else
                        _jobParameter = (LawSyncBEO) XmlUtility.DeserializeObject(BootParameters, typeof (LawSyncBEO));
                }
                _datasetCollectionId = lawDocumentsList.DatasetCollectionId;
                _lawSyncJobId = lawDocumentsList.LawSynJobId;
                _logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
                _documentProcessStateList = new List<DocumentConversionLogBeo>();

                _lawEvAdapter = LawBO.GetLawAdapter(_jobParameter.LawCaseId);
                _imageArchiveDirectory = _lawEvAdapter.GetImageArchiveDirectory();

                var lawImagingDocuments = lawDocumentsList.Documents.Where(d => d.IsImaging).ToList();
                if (_jobParameter.IsProduceImage && lawImagingDocuments.Any())
                {
                    _logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
                    ProcessDocumentImages(lawDocumentsList);
                }

                if (_logInfoList.Any())
                {
                    SendLogPipe(_logInfoList);
                }
                if (_documentProcessStateList.Any())
                {
                    UpdateDcoumentProcessState(_documentProcessStateList);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(Constants.LawSyncFailureinSyncImageMessage + ex.ToUserString());
            }
        }

        private void ProcessDocumentImages(LawSyncDocumentCollection lawDocumentsList)
        {
            var documentReady = new List<LawSyncDocumentDetail>();
            var documentNotReady = new List<LawSyncDocumentDetail>();

            foreach (var lawImagingDocument in lawDocumentsList.Documents)
            {
                CheckConversionStateAndSyncImages(lawImagingDocument, documentNotReady, documentReady);
            }

            if (documentNotReady.Any())
            {
                lawDocumentsList.Documents.Clear();
                lawDocumentsList.Documents = documentNotReady;
                InputDataPipe.Send(new PipeMessageEnvelope { Body = lawDocumentsList, IsPostback = true });
            }
            IncreaseProcessedDocumentsCount(documentReady.Count);
        }
        
        private void CheckConversionStateAndSyncImages(LawSyncDocumentDetail lawImagingDocument, List<LawSyncDocumentDetail> documentNotReady,
            List<LawSyncDocumentDetail> documentReady)
        {
            try
            {

                if (!lawImagingDocument.IsImagesXdlAvailable) return;
                var documentStatus =
                    RedactItHeartbeatWatcher.CheckDocumentState(lawImagingDocument.RedactItHeartBeatFilePath, 1);
                switch (documentStatus.DocumentState)
                {
                    case RedactItHeartbeatWatcher.DocumentStateEnum.NotFound:
                        if (!HandleConversionImageNotFoundState(lawImagingDocument, documentStatus, documentNotReady))
                        {
                            // timeout
                            // Bug 169138: When conversion time out, Job log table display the error message "Failure in image conversion". It should display "Conversion timeout".
                            HandleConversionImageFailureState(lawImagingDocument, documentStatus, true); // timeout
                        }
                        break;
                    case RedactItHeartbeatWatcher.DocumentStateEnum.NotReady:
                        if (!HandleConversionImageNotReadyState(lawImagingDocument, documentStatus, documentNotReady))
                        {
                            // timeout
                            // Bug 169138: When conversion time out, Job log table display the error message "Failure in image conversion". It should display "Conversion timeout".
                            HandleConversionImageFailureState(lawImagingDocument, documentStatus, true); // timeout
                        }
                        break;
                    case RedactItHeartbeatWatcher.DocumentStateEnum.Success:
                        HandleConversionImageSucessState(documentReady, lawImagingDocument);
                        break;
                    case RedactItHeartbeatWatcher.DocumentStateEnum.Failure:
                        HandleConversionImageFailureState(lawImagingDocument, documentStatus, false); // not timeout
                        break;
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Law Document Id:{0}", lawImagingDocument.LawDocumentId);
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        /// <summary>
        /// Handle Image Conversion Failure
        /// </summary>
        private void HandleConversionImageFailureState(LawSyncDocumentDetail lawImagingDocument, RedactItHeartbeatWatcher.DocumentStatus documentStatus, bool isTimeOut)
        {
            SafeDeleteFolder(lawImagingDocument.DocumentExtractionPath);

            // Bug 169138: When conversion time out, Job log table display the error message "Failure in image conversion". It should display "Conversion timeout".
            string errorMessage = Constants.LawSyncFailureinConversionMessage;
            if (isTimeOut)
            {
                errorMessage = Constants.LawSyncFailureinConversionTimeOutMessage;
            }
            Tracer.Error("Image conversion error: {0}", errorMessage);
            ConstructLog(lawImagingDocument.LawDocumentId, lawImagingDocument.CorrelationId,
                lawImagingDocument.DocumentControlNumber, errorMessage, lawImagingDocument.RedactItHeartBeatFilePath, lawImagingDocument.ImagesFolderPath);
             

            IncreaseProcessedDocumentsCount(1); // Failed document counted as processed
           
            if (string.IsNullOrEmpty(documentStatus.ErrorReason))
            {
                documentStatus.ErrorReason = EVRedactItErrorCodes.UnKnownConversionFailure;
            }
            //Filling error details in ProcessSet table for future use
            var documentProcessState = GetDocumentProcessStateInformationForImageConversion(lawImagingDocument,
                documentStatus.ErrorReason);
            documentProcessState.ErrorDetails = documentStatus.ErrorMessage;
            _documentProcessStateList.Add(documentProcessState);
            //Handle partially converted documents
            RenameAndSetImagesInDocument(lawImagingDocument);
        }

        /// <summary>
        /// Handle Image Conversion Sucesss
        /// </summary>
        private void HandleConversionImageSucessState(List<LawSyncDocumentDetail> documentReady, LawSyncDocumentDetail lawImagingDocument)
        {
            try
            {
                documentReady.Add(lawImagingDocument);
                SafeDeleteFolder(lawImagingDocument.DocumentExtractionPath);
                SafeDeleteFile(lawImagingDocument.RedactItHeartBeatFilePath);            
                RenameAndSetImagesInDocument(lawImagingDocument);
                UpdateDocumentImageInLaw(lawImagingDocument);
            }
            catch (Exception ex)
            {
                ConstructLog(lawImagingDocument.LawDocumentId, lawImagingDocument.CorrelationId, lawImagingDocument.DocumentControlNumber,
                    Constants.LawSyncFailureinSyncImageMessage);
                _documentProcessStateList.Add(GetDocumentProcessStateInformationForImageSync(lawImagingDocument,
                    (int) LawSyncProcessState.Failed));

                ex.AddDbgMsg("Law Document Id:{0}", lawImagingDocument.LawDocumentId);
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        /// <summary>
        /// Handle Image Conversion Not Ready
        /// </summary>
        private bool HandleConversionImageNotReadyState(LawSyncDocumentDetail lawImagingDocument, RedactItHeartbeatWatcher.DocumentStatus documentStatus,
            List<LawSyncDocumentDetail> documentNotReady)
        {
            if (!lawImagingDocument.ConversionStartTime.HasValue)
            {
                lawImagingDocument.ConversionStartTime = DateTime.UtcNow;
            }            
            if (DateTime.UtcNow - lawImagingDocument.ConversionStartTime > _documentConversionTimeout)
            {
                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                documentStatus.ErrorMessage = Constants.DocumentconversionTimeoutMessage;

                Tracer.Error("Conversion Timeout for Law document id - {0} ,dcn {1} , HeartBeatFile {2}, timeoutValue: {3}",
                   lawImagingDocument.LawDocumentId, lawImagingDocument.DocumentControlNumber,
                    lawImagingDocument.RedactItHeartBeatFilePath, _documentConversionTimeout.TotalSeconds);
                return false;
            }
            documentNotReady.Add(lawImagingDocument);
            return true;
        }

        /// <summary>
        /// Handle Image Not Found
        /// </summary>
        private bool HandleConversionImageNotFoundState(LawSyncDocumentDetail lawImagingDocument, RedactItHeartbeatWatcher.DocumentStatus documentStatus,
            List<LawSyncDocumentDetail> documentNotReady)
        {
            if (!lawImagingDocument.ConversionEnqueueTime.HasValue)
            {
                lawImagingDocument.ConversionEnqueueTime = DateTime.UtcNow;
            }
            if (DateTime.UtcNow - lawImagingDocument.ConversionEnqueueTime >
                _documentConverisonGlobalTimeout)
            {
                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                documentStatus.ErrorMessage = Constants.GlobalDocumentconversionTimeoutMessage;

                Tracer.Trace("Global Conversion Timeout for Law document id - {0} ,dcn {1},collection id - {2} , HeartBeatFile {3} and Job Run Id {4}",
                    lawImagingDocument.LawDocumentId, lawImagingDocument.DocumentControlNumber,
                    lawImagingDocument.RedactItHeartBeatFilePath,
                    PipelineId);
                return false;
            }
            documentNotReady.Add(lawImagingDocument);
            return true;
        }

        /// <summary>
        /// Update Images in Law
        /// </summary>
        /// <param name="document"></param>
        private void UpdateDocumentImageInLaw(LawSyncDocumentDetail document)
        {
            var lawDocumentUpdate = new LawDocumentBEO
                                    {
                                        LawDocId = document.LawDocumentId,
                                        ImagePaths = document.ProducedImages
                                    };
            _lawEvAdapter.UpdateLawImagePaths(lawDocumentUpdate);
            _documentProcessStateList.Add(GetDocumentProcessStateInformationForImageSync(document,
                (int) LawSyncProcessState.Completed));
        }

        private DocumentConversionLogBeo GetDocumentProcessStateInformationForImageSync(
            LawSyncDocumentDetail lawDocument, int state)
        {
            var documentProcessState = new DocumentConversionLogBeo
                                       {
                                           JobRunId = _lawSyncJobId,
                                           ProcessJobId = WorkAssignment.JobId,

                                           DocumentId = lawDocument.DocumentReferenceId,
                                           CollectionId = _datasetCollectionId,

                                           ConversionStatus = (int) LawSyncProcessState.Completed,
                                           ImageSyncStatus = state,

                                           ModifiedDate = DateTime.UtcNow
                                       };
            //Category Reason
            if (state == (int)LawSyncProcessState.Completed && !lawDocument.IsErrorOnSyncMetadata)
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.Successful;
            }
            else if (state == (int)LawSyncProcessState.Failed)
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.ImageSyncFailure;
            }
            else
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.MetadataSyncFailure;
            }

            //Status
            if (state == (int)LawSyncProcessState.Completed && !lawDocument.IsErrorOnSyncMetadata)
            {
                documentProcessState.Status = EVRedactItErrorCodes.Completed; 
            }
            else
            {
                documentProcessState.Status = EVRedactItErrorCodes.Failed;
            }
           
            return documentProcessState;
        }

        private DocumentConversionLogBeo GetDocumentProcessStateInformationForImageConversion(
            LawSyncDocumentDetail lawDocument, string errorReason = null)
        {
            var documentProcessState = new DocumentConversionLogBeo
                                       {
                                           JobRunId = _lawSyncJobId,
                                           ProcessJobId = WorkAssignment.JobId,

                                           DocumentId = lawDocument.DocumentReferenceId,
                                           CollectionId = _datasetCollectionId,

                                           Status = EVRedactItErrorCodes.Failed,

                                           ErrorReason = errorReason,

                                           ConversionStatus = (int) LawSyncProcessState.Failed,
                                           ImageSyncStatus = (int) LawSyncProcessState.NotStarted,
                                           ReasonId =(int) Constants.LawSynProcessStateErrorCodes.ImageConversionFailure,

                                           ModifiedDate = DateTime.UtcNow
                                       };

            if (!string.IsNullOrEmpty(errorReason) && errorReason.Length > 60)  //To set common conversion error when conversion reason was not generic
            {
                documentProcessState.ErrorReason = EVRedactItErrorCodes.UnKnownConversionFailure;
            }
            return documentProcessState;
        }

        private void UpdateDcoumentProcessState(List<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {
                _vaultManager.AddOrUpdateConversionLogs(Convert.ToInt64(_jobParameter.MatterId),
                                                            documentConversionLogBeos, true);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        public void ConstructLog(int lawDocumentId, int documentCorrelationId, string documentControlNumber,string message,string heartBeatFilePath=null,string imageFolderPath=null)
        {
            var sbErrorMessage = new StringBuilder();
            sbErrorMessage.Append(message);
            sbErrorMessage.Append(Constants.MessageSpace);
            sbErrorMessage.Append(Constants.MessageDCN);
            sbErrorMessage.Append(documentControlNumber);
            sbErrorMessage.Append(Constants.MessageLawDocumentId);
            sbErrorMessage.Append(lawDocumentId);
            if (!string.IsNullOrEmpty(heartBeatFilePath))
            {
                sbErrorMessage.Append(Constants.MessageSpace);
                sbErrorMessage.Append(Constants.MessageHeartbeatFile);
                sbErrorMessage.Append(heartBeatFilePath);
            }
            if (!string.IsNullOrEmpty(imageFolderPath))
            {
                sbErrorMessage.Append(Constants.MessageSpace);
                sbErrorMessage.Append(Constants.MessageVolume);
                sbErrorMessage.Append(imageFolderPath);
            }
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = Convert.ToInt64(PipelineId),
                CorrelationId = documentCorrelationId,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncImageUpdateWorkerRoleType,
                ErrorCode = (int)LawSyncErrorCode.ImageConversionFailure,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                LogInfo = new LawSyncLogInfo
                {
                    LawDocumentId = lawDocumentId,
                    DocumentControlNumber = documentControlNumber,
                    Information = sbErrorMessage.ToString(),
                    IsFailureInImageConversion = true
                }
            };
            _logInfoList.Add(lawSyncLog);
        }


        public void LogMessage(string message)
        {
            var logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncImageUpdateWorkerRoleType,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new LawSyncLogInfo
                {
                    Information = message
                }
            };
            logInfoList.Add(lawSyncLog);
            SendLogPipe(logInfoList);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLogPipe(List<JobWorkerLog<LawSyncLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope { Body = log };
            LogPipe.Send(message);
        }

        private void RenameAndSetImagesInDocument(LawSyncDocumentDetail lawDocument)
        {
            try
            {
                var lawDocumentId = lawDocument.DocumentControlNumber;
                var files =
                    new DirectoryInfo(lawDocument.ImagesFolderPath).GetFiles(
                        string.Format("{0}{1}*", lawDocumentId,Constants.RedactItPagingNameFormat));
                if (!files.Any()) return;
                var convertedImages = new List<ConvertedImage>(); // hold the list of converted iamges. 
                foreach (var file in files)
                {
                    if (String.IsNullOrEmpty(file.Name)) continue;
                    var fileExtenstion = Path.GetExtension(file.Name);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                    var currentPage = fileNameWithoutExtension.Replace(lawDocumentId, "").Replace(Constants.RedactItPagingNameFormat, "");
                    int pageNumber;
                    if (!int.TryParse(currentPage,
                        out pageNumber))
                    {
                        continue;
                    }
                    var imageStartingNumber = (lawDocument.ImageStartingNumber + 1) + pageNumber;
                    var newFileName = string.Format("{0:D3}", imageStartingNumber) + fileExtenstion;
                    var image = Path.Combine(lawDocument.ImagesFolderPath, newFileName);
                    File.Move(file.FullName, image);
                    var evImageRelativePath = image.Replace(_imageArchiveDirectory, string.Empty);
                    evImageRelativePath = evImageRelativePath.StartsWith(@"\") ? evImageRelativePath.Remove(0, 1) : evImageRelativePath;
                    convertedImages.Add(new ConvertedImage { PageNo = pageNumber, RelativePath = evImageRelativePath });
                }
                // DEVBug 169433: Page order mess up for LAW Sync reprocess. For example the first page in LAW is not the first page in EV NNV viewer,
                // e.g. the first page is 701.tif instead of 001.tif. 
                // To solve this, we need to sort the image by page Number
                var imagePaths = convertedImages.OrderBy(o => o.PageNo).Select(o => o.RelativePath).ToList();
                if (lawDocument.ProducedImages == null)
                {
                    lawDocument.ProducedImages = new List<string>();
                }
                lawDocument.ProducedImages.AddRange( imagePaths );
            }
            catch (Exception ex)
            {
                //continue the image process with out rename images
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
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
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Safes the delete file.
        /// </summary>
        /// <param name="strHearbeatFile">The STR hearbeat file.</param>
        private void SafeDeleteFile(string strHearbeatFile)
        {
            try
            {
                if (File.Exists(strHearbeatFile)) //Delete the hearbeat file for the document
                {
                    File.Delete(strHearbeatFile);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }
    }
}
