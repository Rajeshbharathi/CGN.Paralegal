using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System.Text;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using Moq;


namespace LexisNexis.Evolution.Worker
{
    public class LawSyncUpdateWorker : WorkerBase
    {
        private LawSyncBEO _jobParameter;
        private List<JobWorkerLog<LawSyncLogInfo>> _logInfoList;
        private List<DocumentConversionLogBeo> _documentProcessStateList;
        private string _datasetCollectionId;
        private long _lawSyncJobId;
        private IDocumentVaultManager _vaultManager;
        protected override void BeginWork()
        {
            base.BeginWork();
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
                    {
                        _jobParameter = lawDocumentsList.OrginalJobParameter;
                    }
                    else
                    {
                        _jobParameter = (LawSyncBEO) XmlUtility.DeserializeObject(BootParameters, typeof (LawSyncBEO));
                    }
                }
                _datasetCollectionId = lawDocumentsList.DatasetCollectionId;
                _lawSyncJobId = lawDocumentsList.LawSynJobId;
                _logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
                _documentProcessStateList = new List<DocumentConversionLogBeo>();
                UpdateDocumentsMetadatasInLaw(lawDocumentsList);
                if (_documentProcessStateList.Any())
                {
                    UpdateDcoumentProcessState(_documentProcessStateList);
                }
                Send(lawDocumentsList);
                if (_logInfoList.Any())
                {
                    SendLogPipe(_logInfoList);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(Constants.LawSyncFailureinSyncMetadataMessage + ex.ToUserString());
            }
        }

        private void UpdateDocumentsMetadatasInLaw(LawSyncDocumentCollection lawDocumentsList)
        {
            try
            {
                var lawEvAdapter = LawBO.GetLawAdapter(_jobParameter.LawCaseId);
                var lawDocumentUpdateList = new List<LawDocumentBEO>();
                var metadatNameList = new List<string>();
                if (_jobParameter.MappingFields != null && _jobParameter.MappingFields.Any())
                {
                    metadatNameList.AddRange(_jobParameter.MappingFields.Select(fields => fields.Name).ToList());
                }
                if (_jobParameter.MappingTags != null && _jobParameter.MappingTags.Any())
                {
                    metadatNameList.AddRange(_jobParameter.MappingTags.Select(tags => tags.Name).ToList());
                }
                metadatNameList.Add(_jobParameter.LawSyncTagName);

                foreach (var document in lawDocumentsList.Documents.Where(d=>!d.IsErrorOnGetMetadata))
                {
                    var lawDocumentUpdate = new LawDocumentBEO
                                            {
                                                LawDocId = document.LawDocumentId,
                                                LawMetadatas = document.MetadataList
                                            };
                    lawDocumentUpdateList.Add(lawDocumentUpdate);

                }

                if (lawDocumentUpdateList.Any())
                {
                    lawEvAdapter.UpdateLawMetadata(lawDocumentUpdateList, metadatNameList);

                    foreach (var document in lawDocumentsList.Documents.Where(d => !d.IsErrorOnGetMetadata))
                    {
                        _documentProcessStateList.Add(GetDocumentProcessStateInformation(document, (int)LawSyncProcessState.Completed));
                    }
                }

            }
            catch (Exception ex)
            {
                //Construct Log
                foreach (var document in lawDocumentsList.Documents) 
                {
                    ConstructLog(document.LawDocumentId, document.CorrelationId, document.DocumentControlNumber,
                        Constants.LawSyncFailureinSyncMetadataMessage);
                    _documentProcessStateList.Add(GetDocumentProcessStateInformation(document, (int)LawSyncProcessState.Failed));
                    document.IsErrorOnSyncMetadata = true;
                }
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        private DocumentConversionLogBeo GetDocumentProcessStateInformation(LawSyncDocumentDetail lawDocument, int state)
        {
            var documentProcessState = new DocumentConversionLogBeo
                                       {
                                           JobRunId = _lawSyncJobId,
                                           ProcessJobId = WorkAssignment.JobId,

                                           DocumentId = lawDocument.DocumentReferenceId,
                                           CollectionId = _datasetCollectionId,

                                           MetadataSyncStatus = state,
                                           Status = EVRedactItErrorCodes.Failed, //default state

                                           ModifiedDate = DateTime.UtcNow
                                       };

            //Category Reason
            if (state == (int) LawSyncProcessState.Failed)
            {
                documentProcessState.ReasonId = (int) Constants.LawSynProcessStateErrorCodes.MetadataSyncFailure;
            }
            else if (lawDocument.IsImaging && !lawDocument.IsImagesXdlAvailable)
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.ImageConversionFailure;
            }
            else
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.Successful;
            }


            if (lawDocument.IsImaging) return documentProcessState;

            //Status
            switch (state)
            {
                case (int) LawSyncProcessState.Completed:
                    documentProcessState.Status = EVRedactItErrorCodes.Completed;
                    break;
                case (int) LawSyncProcessState.Failed:
                    documentProcessState.Status = EVRedactItErrorCodes.Failed;
                    break;
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
                WorkerRoleType = Constants.LawSyncUpdateWorkerRoleType,
                ErrorCode = (int)LawSyncErrorCode.MetadataSyncFail,
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


        public void LogMessage(string message)
        {
            var logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncUpdateWorkerRoleType,
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
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(LawSyncDocumentCollection documentCollection)
        {
            var message = new PipeMessageEnvelope { Body = documentCollection };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(documentCollection.Documents.Count);
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
    }
}


