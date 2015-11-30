using System.Globalization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.BusinessEntities.OptimizedSearch;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.DataAccess.ServerManagement;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Business.DatasetManagement;
using System.Text;

namespace LexisNexis.Evolution.Worker
{
    public class LawSyncStartupWorker : WorkerBase
    {
        private LawSyncBEO _jobParameter;
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
            _jobParameter = (LawSyncBEO) XmlUtility.DeserializeObject(BootParameters, typeof (LawSyncBEO));

            #region "Assertion"
            _jobParameter.DatasetId.ShouldBeGreaterThan(0);
            _jobParameter.MatterId.ShouldBeGreaterThan(0);
            #endregion
            try
            {
                //Get DCN field name
                _dataset = GetDatasetDetails(_jobParameter.DatasetId, _jobParameter.MatterId);
                var field =
                    _dataset.DatasetFieldList.FirstOrDefault(f => f.FieldType.DataTypeId == Constants.DCNFieldTypeId);
                if (field != null) _dcnFieldName = field.Name;

                var lawField = _dataset.DatasetFieldList.FirstOrDefault(f => f.Name == EVSystemFields.LawDocumentId);
                if (lawField != null) _lawFieldId = lawField.ID;

                //Create Volume for Images
                if (_jobParameter.IsProduceImage)
                {
                    _jobEVImagesDirectory = LawVolumeHelper.GetJobImageFolder(WorkAssignment.JobId,
                        _jobParameter.LawCaseId);
                    _volumeCount++;
                    _volumeFolderName = LawVolumeHelper.CreateVolumeFolder(_jobEVImagesDirectory, _volumeCount);
                }
                _vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
            }
            catch (Exception ex)
            {
                ex.Trace();
                ReportToDirector(ex);
                ConstructLog(Constants.LawSyncStartupFailureMessage);
                throw;
            }
        }

        protected override bool GenerateMessage()
        {
            var isErrorInCreateMetadata = false;
            try
            {
                //Create Fields & tags in LAW
                if (!CreateMetaDataFieldsInLawCase())
                {
                    isErrorInCreateMetadata = true;
                    throw new EVException().AddUsrMsg(Constants.LawSyncFailureinCreateMetadata);
                }
                //1) Get documents for LawSync
                var documentsSelection = new List<DocumentsSelectionBEO> { _jobParameter.DocumentsSelection };
                var resultDocuments = LawSyncSearchHelper.GetDocuments(documentsSelection, _dcnFieldName,_jobParameter.CreatedBy );
                if (!resultDocuments.Any())
                {
                    throw new EVException().AddUsrMsg(Constants.LawSyncFailureinGetDcoumentsMessage);
                }
                resultDocuments.ForEach(d => d.IsExclude = true); //Default Exclude all documents for imaging

                //2) Get subset documents for LawSync Imaging
                if (_jobParameter.IsProduceImage)
                {
                    documentsSelection.Add(_jobParameter.ImageDocumentsSelection);
                    var resultImagingDocuments = LawSyncSearchHelper.GetDocuments(documentsSelection, _dcnFieldName, _jobParameter.CreatedBy);
                    if (resultImagingDocuments.Any())
                    {
                        //Document not part of imaging subset excluded for imaging.
                        resultDocuments.FindAll(x => resultImagingDocuments.Exists(y => y.DCN.Equals(x.DCN))).SafeForEach(f => f.IsExclude = false);
                    }
                }
                resultDocuments = resultDocuments.DistinctBy(f => f.Id).ToList();
                //Construct LawSync Document
                ConstructLawSyncDocument(resultDocuments.OrderBy(d => d.DCN).ToList());
                return true;
            }
            catch (Exception ex)
            {
                ex.Trace();
                ReportToDirector(ex);
                ConstructLog(isErrorInCreateMetadata ? Constants.LawSyncFailureinCreateMetadata : Constants.LawSyncFailureinGetDcoumentsMessage);
                throw;
            }
        }
        
        /// <summary>
        /// Create metadata in Law case
        /// </summary>
        private bool CreateMetaDataFieldsInLawCase()
        {
            try
            {
                //1) Fields
                var lawEvAdapter = LawBO.GetLawAdapter(_jobParameter.LawCaseId);
                if (_jobParameter.MappingFields != null && _jobParameter.MappingFields.Any())
                {
                    foreach (var field in _jobParameter.MappingFields.Where(field => !string.IsNullOrEmpty(field.Name)))
                    {
                        lawEvAdapter.CreateField(field.Name, field.FieldType);
                    }
                }

                //2) Tag
                if (_jobParameter.MappingTags != null && _jobParameter.MappingTags.Any())
                {
                    foreach (var tag in _jobParameter.MappingTags.Where(tag => !string.IsNullOrEmpty(tag.Name)))
                    {
                        lawEvAdapter.CreateTag(tag.Name);
                    }
                }
                //3) create special Tag
                if (!string.IsNullOrEmpty(_jobParameter.LawSyncTagName))
                {
                    lawEvAdapter.CreateTag(_jobParameter.LawSyncTagName);
                }
                return true;
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
            return false;
        }

        /// <summary>
        /// Construct LawSync document
        /// </summary>
        private void ConstructLawSyncDocument(IEnumerable<FilteredDocumentBusinessEntity> resultDocuments)
        {
            var runningDocCount = 0;
            var lawDocumentsList=new List<LawSyncDocumentDetail>();
            var documentProcessStateList = new List<DocumentConversionLogBeo>();
            _logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            foreach (var doc in resultDocuments)
            {
                _documentCorrelationId++;
                runningDocCount++;
                if (runningDocCount > Constants.BatchSize)
                {
                    runningDocCount = 0;
                    InsertDcoumentProcessState(documentProcessStateList);
                    Send(lawDocumentsList);
                    lawDocumentsList = new List<LawSyncDocumentDetail>(); //Clear document List
                    documentProcessStateList = new List<DocumentConversionLogBeo>(); //Clear document process state List
                }
               
                var lawDocument=new LawSyncDocumentDetail
                                {
                                    DocumentReferenceId = doc.Id,
                                    DocumentControlNumber = doc.DCN,
                                    IsImaging = !doc.IsExclude,
                                    CorrelationId = _documentCorrelationId

                                };
                var field = doc.OutPutFields.FirstOrDefault(f => f.Name == EVSystemFields.LawDocumentId);
                if (field != null && !string.IsNullOrEmpty(field.Value))
                      lawDocument.LawDocumentId = Convert.ToInt32(field.Value);
              
                //To fix upgrade issue from earlier version to 3.0 version. Law document id(LawDocumentId) not coming part of search result.
                if(lawDocument.LawDocumentId<=0) 
                {
                    var lawField = DocumentBO.GetDocumentFieldById(
                       _jobParameter.MatterId.ToString(CultureInfo.InvariantCulture), _dataset.CollectionId,
                       lawDocument.DocumentReferenceId, _lawFieldId);
                    if (lawField != null && !string.IsNullOrEmpty(lawField.FieldValue))
                    {
                        lawDocument.LawDocumentId = Convert.ToInt32(lawField.FieldValue);
                    }
                }

                if (lawDocument.IsImaging)
                {
                    _volumeDocumentCount++;
                    var documentPagesCount = GetPageCountForImages(_jobParameter.MatterId,
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

                if (lawDocument.LawDocumentId>0)
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
                InsertDcoumentProcessState(documentProcessStateList);
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

        /// <summary>
        /// Get document process state information
        /// </summary>
        private DocumentConversionLogBeo GetDocumentProcessStateInformation(LawSyncDocumentDetail lawDocument)
        {
            var documentProcessState = new DocumentConversionLogBeo
                                       {
                                           ProcessJobId = WorkAssignment.JobId,
                                           JobRunId = WorkAssignment.JobId,

                                           DocumentId = lawDocument.DocumentReferenceId,
                                           CollectionId = _dataset.CollectionId,
                                           DCN = lawDocument.DocumentControlNumber,
                                           CrossReferenceId =
                                               lawDocument.LawDocumentId.ToString(CultureInfo.InvariantCulture),
                                         
                                           MetadataSyncStatus = (int) LawSyncProcessState.NotStarted,
                                           ReasonId = (int) Constants.LawSynProcessStateErrorCodes.MetadataSyncFailure,
                                           Status =  EVRedactItErrorCodes.Failed, //Default state

                                           ModifiedDate = DateTime.UtcNow,
                                           CreatedDate = DateTime.UtcNow
                                       };

            if (!lawDocument.IsImaging) return documentProcessState;
            documentProcessState.ImageSyncStatus = (int) LawSyncProcessState.NotStarted;
            documentProcessState.ConversionStatus = (int) LawSyncProcessState.NotStarted;
            return documentProcessState;
        }


        /// <summary>
        /// Insert document process state
        /// </summary>
        private void InsertDcoumentProcessState(List<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {
                _vaultManager.AddOrUpdateConversionLogs(Convert.ToInt64(_jobParameter.MatterId),
                                                            documentConversionLogBeos, false);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Get Page Count for document
        /// </summary>
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


        /// <summary>
        /// Get Dataset details
        /// </summary>
        private DatasetBEO GetDatasetDetails(long datasetId, long matterId)
        {
            var dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(datasetId));
            dataset.ShouldNotBe(null);
            var matterDetails = MatterDAO.GetMatterDetails(matterId.ToString(CultureInfo.InvariantCulture));
            if (matterDetails == null) return dataset;
            dataset.Matter = matterDetails;
            var searchServerDetails = ServerDAO.GetSearchServer(matterDetails.SearchServer.Id);
            if (searchServerDetails != null)
            {
                dataset.Matter.SearchServer = searchServerDetails;
            }
            return dataset;
        }


        /// <summary>
        /// Construct log
        /// </summary>
        public void ConstructLog(string message)
        {
            var logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncStartupWorkerRoleType,
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
                WorkerRoleType = Constants.LawSyncStartupWorkerRoleType,
                ErrorCode = (int)LawSyncErrorCode.DocumentNotAvailble,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
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

        #region Send Message

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<LawSyncDocumentDetail> lawDocumentsList)
        {
            var documentCollection = new LawSyncDocumentCollection
                                     {
                                         Documents = lawDocumentsList,
                                         DatasetId = _jobParameter.DatasetId,
                                         DatasetCollectionId = _dataset.CollectionId,
                                         MatterId = _jobParameter.MatterId,
                                         RedactableSetCollectionId = _dataset.RedactableDocumentSetId,
                                         DatasetExtractionPath = _dataset.CompressedFileExtractionLocation,
                                         LawSynJobId=WorkAssignment.JobId
                                     };
            var message = new PipeMessageEnvelope {Body = documentCollection};
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(lawDocumentsList.Count);
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

        #endregion



    }
}
