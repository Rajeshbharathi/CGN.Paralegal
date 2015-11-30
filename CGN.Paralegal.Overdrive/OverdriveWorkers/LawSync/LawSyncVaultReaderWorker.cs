using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.BusinessEntities;
using System.Text;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.External.VaultManager;

namespace LexisNexis.Evolution.Worker
{
    public class LawSyncVaultReaderWorker : WorkerBase
    {
        private LawSyncBEO _jobParameter;
        private string _datasetCollectionId;
        private List<JobWorkerLog<LawSyncLogInfo>> _logInfoList;
        private DatasetBEO _dataset;
        private List<DocumentConversionLogBeo> _documentProcessStateList;
        private IDocumentVaultManager _vaultManager;
        private long _lawSyncJobId;

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
                _dataset = DataSetBO.GetDataSetDetailForDataSetId(_jobParameter.DatasetId);
                if (lawDocumentsList.Documents.Any())
                {
                    foreach (var lawDocument in lawDocumentsList.Documents)
                    {
                        SetMetadataForDcoument(lawDocument);
                    }

                }

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
                LogMessage(Constants.LawSyncFailureinGetMetadataMessage + ex.ToUserString());
            }
        }

            private
            void SetMetadataForDcoument(LawSyncDocumentDetail lawDocument)
        {
            try
            {
                var metaDataList = new List<LawMetadataBEO>();
                //1) Get Fields
                bool isErrorInGetFields;
                metaDataList = GetFields(lawDocument, metaDataList, out isErrorInGetFields);
                //2) Get Tag
                bool isErrorInGetTags;
                GetTags(lawDocument, metaDataList, out isErrorInGetTags);
                //3) Law sync Tag 
                var lawSyncTag = new LawMetadataBEO
                                 {
                                     Name = _jobParameter.LawSyncTagName,
                                     Value = true,
                                     IsTag = true
                                 };
                metaDataList.Add(lawSyncTag);
                lawDocument.MetadataList = metaDataList;

                if (isErrorInGetFields || isErrorInGetTags)
                {
                    ConstructLog(lawDocument.LawDocumentId, lawDocument.CorrelationId, lawDocument.DocumentControlNumber,
                        Constants.LawSyncFailureinGetMetadataMessage);
                    _documentProcessStateList.Add(GetDocumentProcessStateInformation(lawDocument, (int)LawSyncProcessState.Failed));
                    lawDocument.IsErrorOnGetMetadata = true;
                }

            }
            catch (Exception ex)
            {
                ConstructLog(lawDocument.LawDocumentId, lawDocument.CorrelationId, lawDocument.DocumentControlNumber,
                    Constants.LawSyncFailureinGetMetadataMessage);
                _documentProcessStateList.Add(GetDocumentProcessStateInformation(lawDocument, (int)LawSyncProcessState.Failed));
                lawDocument.IsErrorOnGetMetadata = true;
                ex.AddDbgMsg("Law Document Id:{0}", lawDocument.LawDocumentId);
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        private void GetTags(LawSyncDocumentDetail lawDocument, List<LawMetadataBEO> metaDataList, out bool isError)
        {
            isError = false;
            if (_jobParameter.MappingTags == null || !_jobParameter.MappingTags.Any()) return;
            var documentTag = GetDocumentTags(lawDocument.DocumentReferenceId, _datasetCollectionId, out isError);
            foreach (var lawMappingTag in _jobParameter.MappingTags)
            {
                var docTag =
                    documentTag.FirstOrDefault(
                        f =>
                            f.TagId.ToString(CultureInfo.InvariantCulture)
                                .Equals(lawMappingTag.MappingTagId));

                var metaData = new LawMetadataBEO
                               {
                                   Name = lawMappingTag.Name,
                                   Id = lawMappingTag.Id, //Not Required
                                   Value = (docTag != null),
                                   IsTag = true
                               };

                metaDataList.Add(metaData);
            }
        }

        private List<LawMetadataBEO> GetFields(LawSyncDocumentDetail lawDocument, List<LawMetadataBEO> metaDataList, out bool isError)
        {
            isError = false;
            if (_jobParameter.MappingFields == null || !_jobParameter.MappingFields.Any()) return metaDataList;

            var document = GetDocumentDetails(lawDocument.DocumentReferenceId, _datasetCollectionId, out isError);

            foreach (var lawMappingField in _jobParameter.MappingFields)
            {
                var docField =
                    document.FieldList.FirstOrDefault(
                        f =>
                            f.FieldId.ToString(CultureInfo.InvariantCulture)
                                .Equals(lawMappingField.MappingFieldId));
                if (docField == null) continue;

               var datasetField=  _dataset.DatasetFieldList.FirstOrDefault(f => f.ID.ToString(CultureInfo.InvariantCulture)
                   .Equals(lawMappingField.MappingFieldId));
                var format=string.Empty;
                if (datasetField != null)
                {
                    format = datasetField.FieldType.DataFormat;
                }

                var metaData = new LawMetadataBEO
                               {
                                   Name = lawMappingField.Name,
                                   Id = lawMappingField.Id, //Not Required
                                   Type = lawMappingField.FieldType,
                                   Value = ConvertFieldValueIntoLawType(docField.FieldValue, lawMappingField.FieldType, format)
                               };

                metaDataList.Add(metaData);
            }
            return metaDataList;
        }

        /// <summary>
        /// Convert Field Value to Law Type
        /// </summary>
        private object ConvertFieldValueIntoLawType(string value, LawFieldTypeBEO fieldType,string format)
        {
            var isErrorOnConversion = false;
            const string defaultDateFormat = "ddMMyyyy"; //Currently no format used when data imported through Law Import, So using default format.
            if (string.IsNullOrEmpty(value)) return null;
            switch (fieldType)
            {
                case LawFieldTypeBEO.Numeric:
                    Int32 numericValue;
                    if (Int32.TryParse(value, out numericValue))
                    {
                        return numericValue;
                    }
                    isErrorOnConversion = true;
                    break;
                case LawFieldTypeBEO.DateTime:
                    format = (!string.IsNullOrEmpty(format)
                        ? DateTimeUtility.GetLegitimateDateFormat(format)
                        : defaultDateFormat);
                    DateTime date;
                    DateTime.TryParseExact(value.Replace("/", string.Empty), format.Replace("/", string.Empty), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                    return date;
                case LawFieldTypeBEO.String:
                    return value;
            }
            if (isErrorOnConversion)
            {
                throw new EVException().AddUsrMsg("Invalid data, field value not match with Law Field Type.");
            }
            return null;
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
                WorkerRoleType = Constants.LawSyncVaultReaderWorkerRoleType,
                ErrorCode = (int)LawSyncErrorCode.MetadataSyncFail,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new LawSyncLogInfo
                {
                    LawDocumentId = lawDocumentId,
                    DocumentControlNumber = documentControlNumber,
                    Information = sbErrorMessage.ToString()
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
                WorkerRoleType = Constants.LawSyncVaultReaderWorkerRoleType,
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


        private DocumentConversionLogBeo GetDocumentProcessStateInformation(LawSyncDocumentDetail lawDocument, int state)
        {
            var documentProcessState = new DocumentConversionLogBeo
            {
                JobRunId = _lawSyncJobId,
                ProcessJobId = WorkAssignment.JobId,

                DocumentId = lawDocument.DocumentReferenceId,
                CollectionId = _datasetCollectionId,

                MetadataSyncStatus = state,
                Status = EVRedactItErrorCodes.Failed, 
                ReasonId =  (int)Constants.LawSynProcessStateErrorCodes.MetadataSyncFailure,

                ModifiedDate = DateTime.UtcNow
            };
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

        private RVWDocumentBEO GetDocumentDetails(string documentId, string collectionId, out bool isError)
        {
            RVWDocumentBEO document = null;
            isError = false;
            try
            {
                using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
                {
                    document = DocumentBO.GetDocumentDataViewFromVaultWithOutContent(_jobParameter.MatterId.ToString(CultureInfo.InvariantCulture), collectionId, documentId, string.Empty, false);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
                isError = true;
            }
            return document;
        }


        private List<RVWDocumentTagBEO> GetDocumentTags(string documentId,string collectionId, out bool isError)
        {
            List<RVWDocumentTagBEO> tags = null;
            isError = false;
            try
            {
                var tagdetails = new List<RVWTagBEO>();
                using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
                {
                    tags = DocumentBO.GetDocumentTags(ref tagdetails, _jobParameter.MatterId.ToString(CultureInfo.InvariantCulture), collectionId, documentId, false, false);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
                isError = true;
            }
            return tags;
        }
    }
}

