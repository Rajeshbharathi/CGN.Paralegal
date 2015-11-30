using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.Worker.Data;

namespace LexisNexis.Evolution.Worker
{
    public class NearDuplicationEVUpdate : WorkerBase
    {
        private NearDuplicationJobBEO _jobParameter;

        private DatasetBEO _dataset;
        private int _fieldIdNdIsMaster;
        private int _fieldIdNdSort;
        private int _fieldIdNdFamilyId;
        private int _fieldIdNdClusterId;
        private int _fieldIdNdSimilarity;

        private List<DocumentFieldsBEO> _documentsFields;
        private List<string> _datasetSearchFields;
        private Dictionary<string, List<KeyValuePair<string, string>>> _documentsFieldsForSearchEngineUpdate;

        private IndexManagerProxy _indexManagerProxy;

        #region Overdrive

        protected override void BeginWork()
        {
            base.BeginWork();
            _jobParameter =
                (NearDuplicationJobBEO) XmlUtility.DeserializeObject(BootParameters, typeof (NearDuplicationJobBEO));

            #region "Assertion"

            _jobParameter.DatasetId.ShouldBeGreaterThan(0);
            _jobParameter.MatterId.ShouldBeGreaterThan(0);
            _jobParameter.CollectionId.ShouldNotBeEmpty();

            #endregion

            _dataset = GetDatasetDetails(_jobParameter.DatasetId,
                _jobParameter.MatterId.ToString(CultureInfo.InvariantCulture));
            _dataset.ShouldNotBe(null);
            _indexManagerProxy = new IndexManagerProxy(_dataset.Matter.FolderID,_dataset.CollectionId);

            SetNearDuplicationFieldId();
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            var documentresultCollection = (NearDuplicationResultInfoCollection) message.Body;
            _documentsFields = new List<DocumentFieldsBEO>();
            _documentsFieldsForSearchEngineUpdate = new Dictionary<string, List<KeyValuePair<string, string>>>();
            foreach (var resultDocument in documentresultCollection.ResultDocuments)
            {
                ConstructDocumentFieldsForVault(resultDocument);
                ConstructDocumentFieldsForSearchEngineUpdate(resultDocument.DocumentId,
                    _documentsFields.Where(f => f.DocumentReferenceId == resultDocument.DocumentId).ToList());
            }
            //Add documents fields into Vault
            var databaseUpdateStaus = UpdateNearDuplicationFieldsInDatabase();
            //Add documents fields into search engine
            var searchEngineUpdateStaus = UpdateNearDuplicationFieldsInSearchEngine();


            if (!databaseUpdateStaus || !searchEngineUpdateStaus)
            {
                ConstructAndSendLog(documentresultCollection.ResultDocuments, databaseUpdateStaus,
                    true);
            }
            IncreaseProcessedDocumentsCount(documentresultCollection.ResultDocuments.Count());
        }

        #region Only search update call chanaged in the below code from 3.3 code base for search engine replacement work

        /// <summary>
        ///     Construct field for Document to Insert/Update in Search Engine
        /// </summary>
        private void ConstructDocumentFieldsForSearchEngineUpdate(string documentId,
            List<DocumentFieldsBEO> lstDocumentFields)
        {
            var docFields = new List<KeyValuePair<string, string>>();
            foreach (string searchField in _datasetSearchFields)
            {
                var field = lstDocumentFields.FirstOrDefault(f => f.FieldName == searchField);
                if (field != null)
                    docFields.Add(new KeyValuePair<string, string>(searchField, field.FieldValue));
            }
            _documentsFieldsForSearchEngineUpdate.Add(documentId, docFields);
        }

        /// <summary>
        ///     Update Fields in Search Engine
        /// </summary>
        /// <returns></returns>
        private bool UpdateNearDuplicationFieldsInSearchEngine()
        {
            bool status = false;
            try
            {
                Tracer.Info(
                    "Near Duplication EV Update Worker: Update fields in search engine, started time {0} for job run id :{1}",
                    DateTime.Now, PipelineId);

                var documentBeos = new List<DocumentBeo>();
                foreach (var document in _documentsFieldsForSearchEngineUpdate)
                {
                    documentBeos.Add(DocumentBO.ToDocumentBeo(document.Key, document.Value));
                }
                _indexManagerProxy.BulkUpdateDocumentsAsync(documentBeos);
                status = true;
                Tracer.Info(
                    "Near Duplication EV Update Worker: Update fields in search engine, completed time {0} for job run id :{1}",
                    DateTime.Now, PipelineId);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
            return status;
        }
        #endregion
        protected override void EndWork()
        {
            base.EndWork();
            _jobParameter = null;
            _dataset = null;
            _documentsFields = null;
            _documentsFieldsForSearchEngineUpdate = null;
        }

        #endregion

        /// <summary>
        ///     Set Near Duplication Field Id
        /// </summary>
        private void SetNearDuplicationFieldId()
        {
            //TODO: Search Engine Replacement - Eliminate EVSystemFields in up-side layers


            _fieldIdNdIsMaster = GetFieldId(EVSystemFields.ND_IsMaster);
            _fieldIdNdSort = GetFieldId(EVSystemFields.ND_Sort);
            _fieldIdNdFamilyId = GetFieldId(EVSystemFields.ND_FamilyID);
            _fieldIdNdClusterId = GetFieldId(EVSystemFields.ND_ClusterID);
            _fieldIdNdSimilarity = GetFieldId(EVSystemFields.ND_Similarity);
            _datasetSearchFields = _dataset.DatasetFieldList.Where(f => f.ID == _fieldIdNdIsMaster ||
                                                                          f.ID == _fieldIdNdSort ||
                                                                          f.ID == _fieldIdNdFamilyId ||
                                                                          f.ID == _fieldIdNdClusterId ||
                                                                          f.ID == _fieldIdNdSimilarity).Select(
                                                                              f => f.Name).ToList();
        }


        /// <summary>
        ///     Update Fields in Database
        /// </summary>
        private bool UpdateNearDuplicationFieldsInDatabase()
        {
            bool status = false;
            try
            {
                Tracer.Info(
                    "Near Duplication EV Update Worker: Update fields in database, started time {0} for job run id :{1}",
                    DateTime.Now, PipelineId);
                var documentVaultManager = new DocumentVaultManager();
                status = documentVaultManager.BulkUpsertDocumentsFields(
                    _jobParameter.MatterId.ToString(CultureInfo.InvariantCulture),
                    _jobParameter.CollectionId, _documentsFields);
                Tracer.Info(
                    "Near Duplication EV Update Worker: Update fields in database, completed time {0} for job run id :{1}",
                    DateTime.Now, PipelineId);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
            return status;
        }

        /// <summary>
        ///     Get Document Control Number for Documents
        /// </summary>
        /// <param name="nearDuplicationResultDocuments"></param>
        /// <returns></returns>
        private List<DocumentMasterEntity> GetDocumentControlNumberForDocuments(
            IEnumerable<NearDuplicationResultInfo> nearDuplicationResultDocuments)
        {
            try
            {
                List<string> documentReferenceIds =
                    nearDuplicationResultDocuments.Select(resultDocument => resultDocument.DocumentId).ToList();
                var documentVaultManager = new DocumentVaultManager();
                IEnumerable<DocumentMasterEntity> documentsDetailInfo =
                    documentVaultManager.BulkGetDcnForDocuments(_jobParameter.MatterId,
                        _jobParameter.CollectionId, documentReferenceIds);
                return documentsDetailInfo.ToList();
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
            return null;
        }

        /// <summary>
        ///     Construct Log for failure documents
        /// </summary>
        public void ConstructAndSendLog(List<NearDuplicationResultInfo> nearDuplicationResults, bool isErrorInDatabase,
            bool isErrorInSearchEngine)
        {
            var logInfoList = new List<JobWorkerLog<NearDuplicationLogInfo>>();
            var documentsDetailInfo = GetDocumentControlNumberForDocuments(nearDuplicationResults);
            foreach (NearDuplicationResultInfo resultDocument in nearDuplicationResults)
            {
                var documentInfo =
                    documentsDetailInfo.FirstOrDefault(d => d.DocumentReferenceId == resultDocument.DocumentId);
                if (documentInfo == null) continue;
                var nearDuplicationLog =
                    ConstructNearDuplicationLogInfoForDocument(isErrorInDatabase,
                        isErrorInSearchEngine, documentInfo);
                logInfoList.Add(nearDuplicationLog);
            }
            if (logInfoList.Any())
            {
                SendLogPipe(logInfoList);
            }
        }

        /// <summary>
        ///     Construct Near Duplication Log Info for Document
        /// </summary>
        private JobWorkerLog<NearDuplicationLogInfo> ConstructNearDuplicationLogInfoForDocument(bool isErrorInDatabase,
            bool isErrorInSearchEngine,
            DocumentMasterEntity documentInfo)
        {
            string documentControlNumber = documentInfo.DocumentTitle;
            var nearDuplicationLog = new JobWorkerLog<NearDuplicationLogInfo>
            {
                JobRunId =
                    (!string.IsNullOrEmpty(PipelineId))
                        ? Convert.ToInt64(PipelineId)
                        : 0,
                CorrelationId = Convert.ToInt32(documentControlNumber.Replace(_dataset.DCNPrefix, string.Empty)),
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.NearDuplicationEvUpdateWorkerRoleType,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new NearDuplicationLogInfo
                {
                    DocumentControlNumber = documentControlNumber,
                    IsFailureInDatabaseUpdate = isErrorInDatabase,
                    IsFailureInSearchUpdate = isErrorInSearchEngine,
                    Information =
                        string.Format(
                            Constants.
                                NearDuplicationEvUpdateWorkerFailureMessage,
                            documentControlNumber)
                }
            };
            return nearDuplicationLog;
        }

        /// <summary>
        ///     Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLogPipe(List<JobWorkerLog<NearDuplicationLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }


        /// <summary>
        ///     Construct field for Document to Insert/Update in Database
        /// </summary>
        /// <param name="resultDocument"></param>
        private void ConstructDocumentFieldsForVault(NearDuplicationResultInfo resultDocument)
        {
            //Master
            AddNearDuplicationFields(resultDocument.DocumentId, EVSystemFields.ND_IsMaster,
                (resultDocument.IsMaster ? "Y" : "N"), _fieldIdNdIsMaster);
            //Sort
            AddNearDuplicationFields(resultDocument.DocumentId, EVSystemFields.ND_Sort,
                resultDocument.DocumentSort.ToString(CultureInfo.InvariantCulture), _fieldIdNdSort);
            //FamilyId
            string documentNearDuplicationFamilyId = resultDocument.ClusterSort.ToString(CultureInfo.InvariantCulture) +
                                                     "_" +
                                                     resultDocument.FamilySort.ToString(CultureInfo.InvariantCulture);
            AddNearDuplicationFields(resultDocument.DocumentId, EVSystemFields.ND_FamilyID,
                documentNearDuplicationFamilyId, _fieldIdNdFamilyId);
            //ClusterId
            AddNearDuplicationFields(resultDocument.DocumentId, EVSystemFields.ND_ClusterID,
                resultDocument.ClusterSort.ToString(CultureInfo.InvariantCulture),
                _fieldIdNdClusterId);
            //Similarity
            AddNearDuplicationFields(resultDocument.DocumentId, EVSystemFields.ND_Similarity,
                resultDocument.Similarity, _fieldIdNdSimilarity);
        }

        /// <summary>
        ///     Add Near Duplication Fields
        /// </summary>
        private void AddNearDuplicationFields(string documentId, string fieldName, string fieldValue, int fieldId)
        {
            var field = new DocumentFieldsBEO
            {
                FieldId = fieldId,
                FieldName = fieldName,
                FieldValue = fieldValue,
                DocumentReferenceId = documentId,
                CreatedBy = _jobParameter.CreatedBy,
                CreateOREditDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
            };
            _documentsFields.Add(field);
        }

        /// <summary>
        ///     Get dataset details.
        /// </summary>
        private DatasetBEO GetDatasetDetails(long datasetId, string matterId)
        {
            var dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(datasetId));
            var matterDetails = MatterDAO.GetMatterDetails(matterId);
            if (matterDetails == null) return dataset;
            dataset.Matter = matterDetails;
            return dataset;
        }

        /// <summary>
        ///     Get field Id from Dataset
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private int GetFieldId(string fieldName)
        {
            var fieldId = 0;
            var fieldNdMaster =
                _dataset.DatasetFieldList.FirstOrDefault(f => String.Equals(f.Name, fieldName, StringComparison.CurrentCultureIgnoreCase));
            if (fieldNdMaster == null) return fieldId;
            fieldId = fieldNdMaster.ID;
            return fieldId;
        }
    }
}