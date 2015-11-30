using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.NearDuplication;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Business.DatasetManagement;


namespace LexisNexis.Evolution.Worker
{

    public class NearDuplicationStartup : WorkerBase
    {
        private NearDuplicationJobBEO _jobParameter;  
        private string _ingestionId = string.Empty;
        INearDuplicationAdapter _nearDuplicationAdapter;
        private int _documentCount;
        private int _ingestDocumentCount; 
        private List<JobWorkerLog<NearDuplicationLogInfo>> _logInfoList=new List<JobWorkerLog<NearDuplicationLogInfo>>();
        private string _connectionString;
        private DatasetBEO _dataset;

        #region "Overdrive"
        protected override void BeginWork()
        {
            base.BeginWork();

            _jobParameter =
                (NearDuplicationJobBEO) XmlUtility.DeserializeObject(BootParameters, typeof (NearDuplicationJobBEO));

            #region "Assertion"

            _jobParameter.DatasetId.ShouldBeGreaterThan(0);
            _jobParameter.MatterId.ShouldBeGreaterThan(0);
            _jobParameter.CollectionId.ShouldNotBeEmpty();
           // _jobParameter.JobName.ShouldNotBeEmpty();

            #endregion
                       
            _ingestionId = string.Format("{0}_Dataset-{1}_RunId-{2}", _jobParameter.JobName, _jobParameter.DatasetId, PipelineId);
            var familyThreshHold = ((_jobParameter.FamilyThreshold > 0)
                                        ? (byte) _jobParameter.FamilyThreshold
                                        : Constants.NearDuplicationFamilyThresholdDefaultValue);
            var clusterThreshHold = ((_jobParameter.ClusterThreshold > 0)
                                         ? (byte) _jobParameter.ClusterThreshold
                                         : Constants.NearDuplicationClusterThresholdDefaultValue);

            //Get Vault Database Connection string
            var documentVaultManager = new DocumentVaultManager();
            _connectionString = documentVaultManager.GetConnectionStringForMatter(_jobParameter.MatterId);

            //Get Dataset details
            _dataset= DataSetBO.GetDataSetDetailForDataSetId(_jobParameter.DatasetId);
            _dataset.ShouldNotBe(null);

            //Get Polaris license server IP address
            var licenseServer =
                CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.NearDuplicationPolarisLicenseKeyName);

            try
            {
                //Initialize Near Dupe Adapter
                _nearDuplicationAdapter = new EVPolarisNearDuplicationAdapter();
                _nearDuplicationAdapter.InitializeAdapter(_connectionString, _ingestionId, licenseServer,
                                                          clusterThreshHold,
                                                          familyThreshHold);
            }
            catch (Exception ex)
            {
                Tracer.Error(
                    "Near Duplication Start Up Worker: Failed to initialize Near duplication engine for job run id:{0}, exception:{1}",
                    PipelineId, ex);
                throw;
            }
           // _nearDuplicationAdapter.DeleteAllData();
        }

        protected override bool GenerateMessage()
        {
           
             IngestDocuments();

            if (_documentCount > 0)
            {
                SendDataPipe(); //Send message to Pipe
            }
            else
            {
                Tracer.Error("Near Duplication Start Up Worker: No documents found in selected Dataset for job run id:{0}", PipelineId);
                ConstructLog(string.Empty, Constants.NearDuplicationNoDocuments);
                SendLogDocuments();
                throw new EVException().AddUsrMsg(Constants.NearDuplicationNoDocuments);
            }

            //Send log
            SendLogDocuments();
            return true;
        }

        protected override void EndWork()
        {
            base.EndWork();
            _nearDuplicationAdapter = null;
            _jobParameter = null;
            _logInfoList = null;
        }
        #endregion

        /// <summary>
        /// Ingest documents to Near Dupe Engine
        /// </summary>
        private void IngestDocuments()
        {
            var isTextFileNotAvailable = false;
            try
            {
                var documents = GetDocumentTextFile();
                Tracer.Info("Near Duplication Start Up Worker: Ingestion started time {0} for job run id :{1}", DateTime.Now, PipelineId);
                //Ingest documents to Near Duplication Engine
                _nearDuplicationAdapter.IngestDocuments(GetNearDuplicateDocuments(documents));
                Tracer.Info("Near Duplication Start Up Worker: Ingestion completed time {0} for job run id:{1}",
                                DateTime.Now, PipelineId);
                if (_ingestDocumentCount > 0) return;
                isTextFileNotAvailable = true;
                throw new EVException().AddDbgMsg(
                    "Near Duplication Start Up Worker: Text file not available for documents in the selected Dataset for job run id:{0}",
                    PipelineId);             
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg(
                    "Near Duplication Start Up Worker: Failed to Ingest Document(s) to Near dupe engine for job run id:{0}, exception:{1}",
                    PipelineId, ex);
                var userMessage = isTextFileNotAvailable ? Constants.NearDuplicationNoTextFile : Constants.NearDuplicationEngineFailure;
                ConstructLog(string.Empty, userMessage);
                SendLogDocuments();
                throw;
            }
        }

        /// <summary>
        /// Get Document Text File for colletionId
        /// </summary>
        private IEnumerable<RVWDocumentBEO> GetDocumentTextFile()
        {
            try
            {
                //Get document text file
                var documentVaultManager = new DocumentVaultManager();
                var documents = documentVaultManager.GetDocumentTextFileForCollection(_jobParameter.MatterId,
                                                                                      _jobParameter.CollectionId,
                                                                                      1);
                return documents;
            }
            catch (Exception ex)
            {
                Tracer.Error(
                    "Near Duplication Start Up Worker: Failed in get document text file for job run id:{0}, exception:{1}",
                    PipelineId, ex);
                throw;
            }
        }

        /// <summary>
        ///  Get Near Duplicate Document to Ingest in Near Dupe Engine
        /// </summary>
        private IEnumerable<NearDuplicateDocumentInfo> GetNearDuplicateDocuments(IEnumerable<RVWDocumentBEO> documents)
        {
            foreach (var document in documents)
            {
                _documentCount++;
                var textFile = string.Empty;
                var rvwExternalFileBeo = document.DocumentBinary.FileList;
                if (rvwExternalFileBeo != null)
                {
                    var externalFileBeo = document.DocumentBinary.FileList.FirstOrDefault();
                    if (externalFileBeo != null)
                    {
                        textFile = externalFileBeo.Path; //Text File..
                    }
                }


                if (String.IsNullOrEmpty(textFile))
                {
                    ConstructLog(document.DocumentControlNumber, Constants.NearDuplicationDocumentFileMissingMessage);
                    continue;
                }

                if (textFile.Contains("?"))
                {
                    textFile = textFile.Substring(0,
                                                  textFile.IndexOf("?",
                                                                   StringComparison.
                                                                       CurrentCulture));
                }

                var textFieInfo = new FileInfo(textFile);
                if (!textFieInfo.Exists || !(textFieInfo.Length > 0))
                {
                    ConstructLog(document.DocumentControlNumber, Constants.NearDuplicationDocumentFileAccessMessage);
                    continue;
                }
                _ingestDocumentCount++;
                yield return
                    new NearDuplicateDocumentInfo { DocumentId = GetNearDuplicationDocumentIdFromDocumentRefernceId(document.DocumentId), DocumentText = textFile, ContainsFullText = false };
            }
        }

        /// <summary>
        /// Construct near duplication document id with combination of Run id & document reference id
        /// </summary>
        private  string GetNearDuplicationDocumentIdFromDocumentRefernceId(string documentId)
        {
            return string.Format("{0}:{1}", PipelineId, documentId);
        }

        #region Log
        public void ConstructLog(string documentControlNumber, string message)
        {
            var nearDuplicationLog = new JobWorkerLog<NearDuplicationLogInfo>
                                         {
                                             JobRunId =
                                                 (!string.IsNullOrEmpty(PipelineId))
                                                     ? Convert.ToInt64(PipelineId)
                                                     : 0,
                                             CorrelationId = (!string.IsNullOrEmpty(documentControlNumber)
                                                                     ? Convert.ToInt32(
                                                                         documentControlNumber.Replace(_dataset.DCNPrefix, string.Empty)) //To get dcn prefix number to log document related failures in Job
                                                                     : new Random().Next(1000000, 10000000)  //To get random number to log common failures in Job
                                                             ),
                                             WorkerInstanceId = WorkerId,
                                             WorkerRoleType = Constants.NearDuplicationStartupWorkerRoleType,
                                             Success = string.IsNullOrEmpty(documentControlNumber),
                                             CreatedBy = _jobParameter.CreatedBy,
                                             IsMessage = string.IsNullOrEmpty(documentControlNumber),
                                             LogInfo = new NearDuplicationLogInfo
                                                           {
                                                               DocumentControlNumber = documentControlNumber,
                                                               IsMissingText =!string.IsNullOrEmpty(documentControlNumber),
                                                               Information = (message + documentControlNumber)
                                                           }
                                         };
            _logInfoList.Add(nearDuplicationLog);
        }


        private void SendLogDocuments()
        {
            if (!_logInfoList.Any()) return;
            var logCount = _logInfoList.Count();
            if (logCount <= Constants.NearDuplicationJobBatchSize)
            {
                SendLogPipe(_logInfoList);
            }
            else
            {
                var batchCount = logCount / Constants.NearDuplicationJobBatchSize;
                for (var currentBatch = 1; currentBatch <= batchCount; currentBatch++)
                {
                    if (currentBatch == 1)
                    {
                        SendLogPipe(_logInfoList.Take(Constants.NearDuplicationJobBatchSize).ToList());
                    }
                    else if (currentBatch == batchCount)
                    {
                        SendLogPipe(_logInfoList.Skip((currentBatch - 1) * Constants.NearDuplicationJobBatchSize).Take(logCount).ToList());
                    }
                    else
                    {
                        SendLogPipe(_logInfoList.Skip((currentBatch - 1) * Constants.NearDuplicationJobBatchSize).Take(((currentBatch + 1) * Constants.NearDuplicationJobBatchSize)).ToList());
                    }
                }
            }
        }

        #endregion
        
        #region "DataPipe"
        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void SendDataPipe()
        {
            OutputDataPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope {Body = true};
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(_documentCount);
        }


        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLogPipe(List<JobWorkerLog<NearDuplicationLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope {Body = log};
            LogPipe.Send(message);
        }
        #endregion

    }
}
