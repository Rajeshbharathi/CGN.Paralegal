using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.NearDuplication;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using NearDuplicateResultInfo = LexisNexis.Evolution.External.NearDuplicateResultInfo;

namespace LexisNexis.Evolution.Worker
{

    public class NearDuplicationProcessing : WorkerBase
    {
        private NearDuplicationJobBEO _jobParameter;
        private string _ingestionId = string.Empty;
        private INearDuplicationAdapter _nearDuplicationAdapter;
        private byte _familyThreshHold;
        private byte _clusterThreshHold;
        private string _connectionString;
        private int _processedDocumentResult;
        private string _licenseServer=string.Empty;
        #region Overdrive
        protected override void BeginWork()
        {
            base.BeginWork();
            _jobParameter =
                (NearDuplicationJobBEO) XmlUtility.DeserializeObject(BootParameters, typeof (NearDuplicationJobBEO));

            #region Assertion

            _jobParameter.DatasetId.ShouldBeGreaterThan(0);
            _jobParameter.MatterId.ShouldBeGreaterThan(0);
            _jobParameter.CollectionId.ShouldNotBeEmpty();
          //  _jobParameter.JobName.ShouldNotBeEmpty();

            #endregion

            _ingestionId = string.Format("{0}_Dataset-{1}_RunId-{2}", _jobParameter.JobName, _jobParameter.DatasetId, PipelineId);
            _familyThreshHold = ((_jobParameter.FamilyThreshold > 0)
                                     ? (byte) _jobParameter.FamilyThreshold
                                     : Constants.NearDuplicationFamilyThresholdDefaultValue);
            _clusterThreshHold = ((_jobParameter.ClusterThreshold > 0)
                                      ? (byte) _jobParameter.ClusterThreshold
                                      : Constants.NearDuplicationClusterThresholdDefaultValue);
            //Get Vault Database Connection string
            var documentVaultManager = new DocumentVaultManager();
            _connectionString = documentVaultManager.GetConnectionStringForMatter(_jobParameter.MatterId);

            //Get Polaris license server IP address
            _licenseServer = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.NearDuplicationPolarisLicenseKeyName);
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                if (!((bool)message.Body)) return;

                //1) Index & Group documents
                PerformNearDuplicateComputation();
                //2) Get Result
                var nearDuplicationResultDocuments = GetNearDuplicationResult();

                //3) Send to Data pipe
                if (nearDuplicationResultDocuments.Any())
                    SendDataPipe(nearDuplicationResultDocuments);
                nearDuplicationResultDocuments.Clear();

                IncreaseProcessedDocumentsCount(_processedDocumentResult);

                if (_processedDocumentResult >= 1) return;

                Tracer.Error(
                    "Near Duplication Processing Worker: No documents return from near duplicate result after perform computation for job run id :{0}",
                    PipelineId);
                ConstructAndSendLog(Constants.NearDuplicationResultNoDocuments);
                throw new EVException().AddUsrMsg(Constants.NearDuplicationResultNoDocuments);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }

        }

        protected override void EndWork()
        {
            base.EndWork();
            if (_nearDuplicationAdapter != null)
            {
                _nearDuplicationAdapter.DeleteDataInCurrentBatch();
                _nearDuplicationAdapter = null;
            }
            _jobParameter = null;
        }
        #endregion

        /// <summary>
        /// Call Near Duplicate Computation
        /// </summary>
        private void PerformNearDuplicateComputation()
        {
            //Initialize Near Dupe Adapter
            _nearDuplicationAdapter = new EVPolarisNearDuplicationAdapter();
            _nearDuplicationAdapter.InitializeAdapter(_connectionString, _ingestionId, _licenseServer,
                                                        _clusterThreshHold, _familyThreshHold);
            Tracer.Info("Near Duplication Processing Worker: Computation started time {0} for job run id :{1}", DateTime.Now, PipelineId);
            //Index & Group documents
            _nearDuplicationAdapter.PerformNearDuplicateComputation();
            Tracer.Info("Near Duplication Processing Worker: Computation completed time {0} for job run id :{1}", DateTime.Now, PipelineId);
        }

        /// <summary>
        /// Get Near Duplicate Results from DB
        /// </summary>
        private List<Data.NearDuplicationResultInfo> GetNearDuplicationResult()
        {
            //Send result documents to next worker
            var nearDuplicationResultDocumentList = new List<Data.NearDuplicationResultInfo>();
            //Get Near duplication document results
            var nearDuplicationDocumentResult = _nearDuplicationAdapter.GetNearDuplicationResult(_jobParameter.MatterId);

            var currentBatchDocumentCount = 0;
            var nearDupeFamilyId = string.Empty; //ClusterSort_FamilySort
            foreach (var document in nearDuplicationDocumentResult) //Iterate document result and send as batch
            {
                currentBatchDocumentCount++;
                _processedDocumentResult++;
                var isMasterDocumentInNearDuplicationGroup = IsMasterDocumentInNearDuplicationGroup(document, document.IsMaster, ref nearDupeFamilyId);
                var data = new Data.NearDuplicationResultInfo
                {
                    DocumentId = GetDocumentRefernceIdFromNearDuplicationResultDocumentId(document.DocumentId),
                    DocumentSort = document.DocumentSort,
                    ClusterSort = document.ClusterSort,
                    FamilySort = document.FamilySort,
                    IsMaster = isMasterDocumentInNearDuplicationGroup,
                    Source = document.Source,
                    Similarity = document.Similarity
                };
                nearDuplicationResultDocumentList.Add(data);
                if (currentBatchDocumentCount < Constants.NearDuplicationJobBatchSize) continue;
                //Send to Data pipe
                SendDataPipe(nearDuplicationResultDocumentList);
                currentBatchDocumentCount = 0;
                nearDuplicationResultDocumentList.Clear();
            }
            return nearDuplicationResultDocumentList;
        }

        /// <summary>
        /// Get document reference id from near duplication result document id
        /// </summary>
        private string GetDocumentRefernceIdFromNearDuplicationResultDocumentId(string nearDuplicationResultDocumentId)
        {
            return nearDuplicationResultDocumentId.Replace(string.Format("{0}:", PipelineId), string.Empty);
        }

        /// <summary>
        /// Check the Near Dupe result document and find Master document in Family group
        /// In same cluster Group, first document treated as Master (Base) document, 
        /// remaining documents in same cluster group are treated as non-Master (Base) document 
        /// </summary>
        private static bool IsMasterDocumentInNearDuplicationGroup(NearDuplicateResultInfo document, bool isMasterDocument,
                                                     ref string nearDupeFamilyId)
        {
            var documentNearDupeFamilyId = document.ClusterSort.ToString(CultureInfo.InvariantCulture) + "_" +
                                           document.FamilySort.ToString(CultureInfo.InvariantCulture);
            if (nearDupeFamilyId == string.Empty || nearDupeFamilyId != documentNearDupeFamilyId)
            {
                nearDupeFamilyId = documentNearDupeFamilyId;
            }
            else if (nearDupeFamilyId == documentNearDupeFamilyId)
            {
                /*In same cluster Group, first document treated as Master (Base) document, 
                    remaining documents in same cluster group are treated as non-Master (Base) document */
                isMasterDocument = false;
            }
            return isMasterDocument;
        }

        /// <summary>
        /// Construct and Send Log
        /// </summary>
        /// <param name="message"></param>
        public void ConstructAndSendLog(string message)
        {
             var logInfoList=new List<JobWorkerLog<NearDuplicationLogInfo>>();
            var nearDuplicationLog = new JobWorkerLog<NearDuplicationLogInfo>
            {
                JobRunId =
                    (!string.IsNullOrEmpty(PipelineId))
                        ? Convert.ToInt64(PipelineId)
                        : 0,
                CorrelationId = 1,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.NearDuplicationPorcessingWorkerRoleType,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new NearDuplicationLogInfo
                {
                    Information = message
                }
            };
            logInfoList.Add(nearDuplicationLog);
            SendLogPipe(logInfoList);
        }

        #region "DataPipe"
        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void SendDataPipe(List<Data.NearDuplicationResultInfo> data)
        {
            OutputDataPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
                              {
                                  Body = new Data.NearDuplicationResultInfoCollection {ResultDocuments = data}
                              };
            OutputDataPipe.Send(message);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLogPipe(List<JobWorkerLog<NearDuplicationLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope { Body = log };
            LogPipe.Send(message);
        }
        #endregion

    }
}
