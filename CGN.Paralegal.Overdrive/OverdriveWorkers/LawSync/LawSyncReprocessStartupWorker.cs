using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.External.DataAccess;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.BusinessEntities.OptimizedSearch;
using System.Text;

namespace LexisNexis.Evolution.Worker
{
    public class LawSyncReprocessStartupWorker : WorkerBase
    {
        private ConversionReprocessJobBeo _reprocessJobParameter;
        private LawSyncBEO _lawSyncJobParameter;
        private string _dcnFieldName;
        private int _lawFieldId;
        private DatasetBEO _dataset;
        private string _jobEVImagesDirectory;
        private int _documentCorrelationId;
        private int _volumeDocumentCount;
        private int _volumeCount;
        private string _volumeFolderName;
        private const int VolumeMaximumDocumentCount = 50;
        private int _volumeDocumentPagesCount;
        private IDocumentVaultManager _vaultManager;
        private List<JobWorkerLog<LawSyncLogInfo>> _logInfoList;

        protected override void BeginWork()
        {
            base.BeginWork();

            _reprocessJobParameter =(ConversionReprocessJobBeo)XmlUtility.DeserializeObject(BootParameters, typeof (ConversionReprocessJobBeo));

            var baseConfig = ReconversionDAO.GetJobConfigInfo(Convert.ToInt32(_reprocessJobParameter.OrginialJobId));

            _lawSyncJobParameter =(LawSyncBEO) XmlUtility.DeserializeObject(baseConfig.BootParameters, typeof (LawSyncBEO));

            _dataset = DataSetBO.GetDataSetDetailForDataSetId(_lawSyncJobParameter.DatasetId);
            var field =_dataset.DatasetFieldList.FirstOrDefault(f => f.FieldType.DataTypeId == Constants.DCNFieldTypeId);
            if (field != null) _dcnFieldName = field.Name;
            
            var lawField=_dataset.DatasetFieldList.FirstOrDefault(f => f.Name == EVSystemFields.LawDocumentId);
            if (lawField != null) _lawFieldId = lawField.ID;

            if (_lawSyncJobParameter.IsProduceImage)
            {
                _jobEVImagesDirectory = LawVolumeHelper.GetJobImageFolder(WorkAssignment.JobId,
                    _lawSyncJobParameter.LawCaseId);
                _volumeCount++;
                _volumeFolderName = LawVolumeHelper.CreateVolumeFolder(_jobEVImagesDirectory, _volumeCount);
            }

            _vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
        }

        protected override bool GenerateMessage()
        {
            try
            {
                var reprocessDocumentList = GetDocumentsFromReprocessSelection(_reprocessJobParameter.FilePath,
                    _reprocessJobParameter.JobSelectionMode,
                    _lawSyncJobParameter.MatterId,
                    _lawSyncJobParameter.DatasetId,
                    _reprocessJobParameter.OrginialJobId,
                    _reprocessJobParameter.Filters);

                ConstructLawSyncDocument(reprocessDocumentList);

                return true;
            }
            catch (Exception ex)
            {
                ex.Trace();
                ReportToDirector(ex);
                ConstructLog(Constants.LawSyncFailureinGetDcoumentsMessage + ex.ToUserString());
                throw;
            }
        }


        private void ConstructLawSyncDocument(IEnumerable<ReconversionDocumentBEO> reprocessDocumentList)
        {
            #region Produce Image
            List<FilteredDocumentBusinessEntity> imagingDocuments = null;
            if (_lawSyncJobParameter.IsProduceImage)
            {
                var documentsSelection = new List<DocumentsSelectionBEO>
                                         {
                                             _lawSyncJobParameter.DocumentsSelection,
                                             _lawSyncJobParameter.ImageDocumentsSelection
                                         };
                imagingDocuments = LawSyncSearchHelper.GetDocuments(documentsSelection, _dcnFieldName, _lawSyncJobParameter.CreatedBy);
            }
            #endregion

            var runningDocCount = 0;
            var lawDocumentsList = new List<LawSyncDocumentDetail>();
            var documentProcessStateList = new List<DocumentConversionLogBeo>();
            _logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            foreach (var reprocessDocument in reprocessDocumentList)
            {
                _documentCorrelationId++;
                runningDocCount++;
                if (runningDocCount > Constants.BatchSize)
                {
                    runningDocCount = 0;
                    UpdateDcoumentProcessState(documentProcessStateList);
                    Send(lawDocumentsList);
                    lawDocumentsList = new List<LawSyncDocumentDetail>(); //Clear document List
                      documentProcessStateList = new List<DocumentConversionLogBeo>(); //Clear document process state List
                }
                var lawDocument = new LawSyncDocumentDetail
                                  {
                                      DocumentReferenceId = reprocessDocument.DocumentId,
                                      DocumentControlNumber = reprocessDocument.DCNNumber,
                                      CorrelationId = _documentCorrelationId

                                  };

                if (_lawSyncJobParameter.IsProduceImage && imagingDocuments != null && imagingDocuments.Any())
                {
                    lawDocument.IsImaging = imagingDocuments.Exists(y => y.DCN.Equals(lawDocument.DocumentControlNumber));
                }
                else
                {
                    //Document not part of imaging subset excluded for imaging.
                    lawDocument.IsImaging = false;
                }


                var field =
                    DocumentBO.GetDocumentFieldById(
                        _lawSyncJobParameter.MatterId.ToString(CultureInfo.InvariantCulture), _dataset.CollectionId,
                        lawDocument.DocumentReferenceId, _lawFieldId);
                if (field != null && !string.IsNullOrEmpty(field.FieldValue))
                    lawDocument.LawDocumentId = Convert.ToInt32(field.FieldValue);

                if (lawDocument.IsImaging)
                {
                    _volumeDocumentCount++;
                    var documentPagesCount = GetPageCountForImages(_lawSyncJobParameter.MatterId,
                        _dataset.RedactableDocumentSetId, lawDocument.DocumentReferenceId);
                    if (_volumeDocumentCount > VolumeMaximumDocumentCount)
                    {
                        _volumeDocumentCount = 0;
                        _volumeCount++;
                        _volumeFolderName = LawVolumeHelper.CreateVolumeFolder(_jobEVImagesDirectory, _volumeCount);
                        _volumeDocumentPagesCount = 0;
                    }
                    lawDocument.ImagesFolderPath = _volumeFolderName;
                    lawDocument.ImageStartingNumber = _volumeDocumentPagesCount;
                    _volumeDocumentPagesCount = _volumeDocumentPagesCount + documentPagesCount;
                }
                if (lawDocument.LawDocumentId > 0)
                {
                    lawDocumentsList.Add(lawDocument);
                }
                else
                {
                    ConstructLog(lawDocument.LawDocumentId, lawDocument.CorrelationId, lawDocument.DocumentControlNumber, Constants.LawSyncDocumentNotAvailable);
                }
                documentProcessStateList.Add(GetDocumentProcessStateInformation(lawDocument));
            }

            if (documentProcessStateList.Any())
            {
                UpdateDcoumentProcessState(documentProcessStateList);
            }

            if (lawDocumentsList.Any())
            {
                Send(lawDocumentsList);
            }
            if (_logInfoList.Any())
            {
                SendLogPipe(_logInfoList);
            }
        }




        private DocumentConversionLogBeo GetDocumentProcessStateInformation(LawSyncDocumentDetail lawDocument)
        {
            var documentProcessState = new DocumentConversionLogBeo
                                       {
                ProcessJobId = WorkAssignment.JobId,
                JobRunId = _reprocessJobParameter.OrginialJobId,

                DocumentId = lawDocument.DocumentReferenceId,
                CollectionId = _dataset.CollectionId,
                DCN = lawDocument.DocumentControlNumber,
                CrossReferenceId = lawDocument.LawDocumentId.ToString(CultureInfo.InvariantCulture),

                MetadataSyncStatus = (int)LawSyncProcessState.NotStarted,
                ReasonId = (int)Constants.LawSynProcessStateErrorCodes.MetadataSyncFailure,
                Status = EVRedactItErrorCodes.Failed, //Default state

                ModifiedDate = DateTime.UtcNow
            };
            if (!lawDocument.IsImaging) return documentProcessState;
            documentProcessState.ImageSyncStatus = (int)LawSyncProcessState.NotStarted;
            documentProcessState.ConversionStatus = (int)LawSyncProcessState.NotStarted;
            return documentProcessState;
        }

        private void UpdateDcoumentProcessState(List<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {
                _vaultManager.AddOrUpdateConversionLogs(Convert.ToInt64(_lawSyncJobParameter.MatterId),
                                                            documentConversionLogBeos, true);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        private int GetPageCountForImages(long matterId, string collectionId, string docReferenceId)
        {
            var pageCount = 0;
            //Get all the brava file names for the document in a list (.xdl, .zdl..)
            var fileNames = DocumentBO.GetBinaryReferenceIdFromPropertiesOnly(matterId.ToString(CultureInfo.InvariantCulture), collectionId, docReferenceId, "4");

            if (fileNames == null || !fileNames.Any()) return pageCount;
            var xdlCount = fileNames.Distinct().Count(s => s.EndsWith(Constants.RedactItDocumentExtension));
            if (xdlCount > 0)
            {
                pageCount = fileNames.Distinct().Count(s => s.EndsWith(Constants.RedactItPageExtension));
            }
            return pageCount;
        }



        public void ConstructLog(string message)
        {
            var logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncReProcessStartupWorkerRoleType,
                Success = false,
                CreatedBy = _lawSyncJobParameter.CreatedBy,
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
        /// Construct log
        /// </summary>
        public void ConstructLog(int lawDocumentId, int documentCorrelationId, string documentControlNumber, string message)
        {
            var sbErrorMessage = new StringBuilder();
            sbErrorMessage.Append(message);
            sbErrorMessage.Append(Constants.MessageDCN);
            sbErrorMessage.Append(documentControlNumber);
            sbErrorMessage.Append(Constants.MessageLawDocumentId);
            sbErrorMessage.Append(lawDocumentId);
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = Convert.ToInt64(PipelineId),
                CorrelationId = documentCorrelationId,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncReProcessStartupWorkerRoleType,
                ErrorCode = (int)LawSyncErrorCode.DocumentNotAvailble,
                Success = false,
                CreatedBy = _lawSyncJobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new LawSyncLogInfo
                {
                    LawDocumentId = lawDocumentId,
                    DocumentControlNumber = documentControlNumber,
                    Information = sbErrorMessage.ToString(),
                    IsFailureInSyncMetadata = true
                }
            };
            _logInfoList.Add(lawSyncLog);
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

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<LawSyncDocumentDetail> lawDocumentsList)
        {
            var documentCollection = new LawSyncDocumentCollection
            {
                Documents = lawDocumentsList,
                DatasetId = _lawSyncJobParameter.DatasetId,
                DatasetCollectionId = _dataset.CollectionId,
                MatterId = _lawSyncJobParameter.MatterId,
                RedactableSetCollectionId = _dataset.RedactableDocumentSetId,
                DatasetExtractionPath = _dataset.CompressedFileExtractionLocation,
                LawSynJobId = _reprocessJobParameter.OrginialJobId,
                IsLawSyncReprocessJob = true,
                OrginalJobParameter=_lawSyncJobParameter
            };
            var message = new PipeMessageEnvelope { Body = documentCollection };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(lawDocumentsList.Count);
        }

        public List<ReconversionDocumentBEO> GetDocumentsFromReprocessSelection(
            string inputFilePath, ReProcessJobSelectionMode selectionMode, long matterId, long datasetId, long jobId,
            string filters = null)
        {
            var reprocessDicumentList = new List<ReconversionDocumentBEO>();
            switch (selectionMode)
            {
                case ReProcessJobSelectionMode.Selected:
                {
                    var docidList = ConversionReprocessStartupHelper.GetDocumentIdListFromFile(inputFilePath,
                        Constants.DocId);
                    reprocessDicumentList.AddRange(ConversionReprocessStartupHelper.GetImportDocumentListForIDList(docidList, Constants.DocId, null, matterId));
                    break;
                }
                case ReProcessJobSelectionMode.CrossReference:
                {
                    var docidList = ConversionReprocessStartupHelper.GetDocumentIdListFromFile(inputFilePath,
                        Constants.DCN);
                    reprocessDicumentList.AddRange(ConversionReprocessStartupHelper.GetImportDocumentListForIDList(docidList, Constants.DCN, _dataset.CollectionId, matterId));
                    break;
                }
                case ReProcessJobSelectionMode.Csv:
                    var dictIds = ConversionReprocessStartupHelper.GetDocumentIdListFromFile(inputFilePath,
                        Constants.DCN, Constants.DocumentSetName);
                    var lstDocumentSet = DataSetBO.GetAllDocumentSet(datasetId.ToString(CultureInfo.InvariantCulture));
                    foreach (var key in dictIds.Keys)
                    {
                        var firstOrDefault = lstDocumentSet.FirstOrDefault(d => d.DocumentSetName.Equals(key));
                        if (firstOrDefault == null) continue;
                        var collectionId = firstOrDefault.DocumentSetId;
                        reprocessDicumentList.AddRange(ConversionReprocessStartupHelper.GetImportDocumentListForIDList(dictIds[key], Constants.DCN, collectionId, matterId));
                    }
                    break;
                case ReProcessJobSelectionMode.All:
                    reprocessDicumentList.AddRange(ConversionReprocessStartupHelper.GetReconversionDocumentBeosForJobId(matterId, jobId, filters));
                    break;
            }
            return reprocessDicumentList;
        }
    }
}

