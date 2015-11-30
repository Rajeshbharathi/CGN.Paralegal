using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using OverdriveWorkers.Data;
using OverdriveWorkers.Law;

namespace LexisNexis.Evolution.Worker
{
    using Business.Relationships;

    using Overdrive;

    /// <summary>
    /// This worker uses for overlay documents before sending to Vault worker.
    /// </summary>
    public class LawProcessingWorker : WorkerBase
    {
        private LawImportBEO _jobParams;
        private DatasetBEO _dataset;
        private UserBusinessEntity _userInfo;
        private int _batchSize = 100;
        private bool _isIncludeNativeFile;
        private const string ProcessFailedMessage = "Document process failed";

        protected override void BeginWork()
        {
            try
            {
                base.BeginWork();
                _jobParams = GetImportBEO(BootParameters);

                if (_jobParams != null && !string.IsNullOrEmpty(_jobParams.CreatedBy))
                {
                    //Get User Information , Its needed for search
                    _userInfo = UserBO.AuthenticateUsingUserGuid(_jobParams.CreatedBy);
                    _userInfo.CreatedBy = _jobParams.CreatedBy;
                    _isIncludeNativeFile = (_jobParams.IsImportNative);
                }
                //Law import batch size for documents
                _batchSize = GetMessageBatchSize();
            }
            catch (Exception ex)
            {
                ReportToDirector(ex.ToUserString());
                ex.Trace().Swallow();
            }
        }


        /// <summary>
        /// Processes the work item.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            var documentCollection = (LawDocumentCollection)message.Body;
            documentCollection.ShouldNotBe(null);
            documentCollection.Documents.ShouldNotBe(null);
            _dataset = documentCollection.Dataset;
            try
            {
                var documentDetailList = new List<DocumentDetail>();
                var logs = new List<JobWorkerLog<LawImportLogInfo>>();
                var docManager = new LawDocumentManager(_jobParams, PipelineId, WorkerId, _dataset);

                foreach (var doc in documentCollection.Documents)
                {
                    JobWorkerLog<LawImportLogInfo> log;
                    var docs = docManager.GetDocuments(doc.LawDocumentId.ToString(CultureInfo.InvariantCulture),
                        doc.DocumentControlNumber, doc, out log);

                    if (docs != null) documentDetailList.AddRange(docs);
                    if (log != null) logs.Add(log);
                }

                //Log messages for missing native, missing images and missing text
                if (logs.Any()) SendLog(logs);
                
                if (_jobParams.ImportOptions == ImportOptionsBEO.AppendNew)
                {
                    Send(documentDetailList);
                    SendThreads(documentDetailList);
                    SendFamilies(documentDetailList);
                    return;
                }

                //Process documents for overlay scenario
                if (documentDetailList.Any())
                {
                    ProcessDocuments(documentDetailList);
                    SendThreads(documentDetailList);
                    SendFamilies(documentDetailList);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex.ToUserString());
                ex.Trace().Swallow();
                LogErrorMessage(documentCollection.Documents, false, ProcessFailedMessage);
            }
        }

        /// <summary>
        /// To set the conversation index
        /// </summary>
        /// <param name="documents"></param>
        private void SendThreads(ICollection<DocumentDetail> documents)
        {
            if (documents == null || documents.Count <= 0) return;
            if (_jobParams.CreateThreads)
            {
                var conversationInfoList =
                    documents.Where(d => d.docType == DocumentsetType.NativeSet)
                        .Select(
                            doc =>
                                new ConversationInfo
                                {
                                    JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                                    ConversationIndex = doc.ConversationIndex,
                                    ParentId = doc.document.EVLoadFileParentId,
                                    DocId = doc.document.DocumentId
                                });
                SendThreads(conversationInfoList);
            }
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<LawImportLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// Send data to Data pipe
        /// </summary>
        /// <param name="docDetails"></param>
        private void Send(List<DocumentDetail> docDetails)
        {
            var documentList = new DocumentCollection
                {
                    documents = docDetails,
                    dataset = _dataset,
                    IsDeleteTagsForOverlay = false,
                    IsIncludeNativeFile = _isIncludeNativeFile
                };
            var message = new PipeMessageEnvelope
                {
                    Body = documentList
                };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(docDetails.Count);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<OverlaySearchLogInfo>> log)
        {
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// Process each document 
        /// </summary>
        private void ProcessDocuments(List<DocumentDetail> documentCollection)
        {
            var documentDetailList = new List<DocumentDetail>();
            var overlayLogList = new List<JobWorkerLog<OverlaySearchLogInfo>>();
            var docManager = new LawOverlayDocumentManager(_jobParams, _dataset, PipelineId, WorkerId);

            try
            {
                //Bulk Search for entire batch
                Dictionary<string, string> overlayDocumentIdPair;
                documentDetailList = docManager.BulkSearch(documentCollection, _userInfo,
                                                           out overlayLogList, 
                                                           out overlayDocumentIdPair);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex.ToUserString());
                ex.Trace().Swallow();
            }

            #region Send Message
            //Send response   
            if (documentDetailList != null && documentDetailList.Any())
            {
                if (documentDetailList.Count > _batchSize)
                {
                    Send(documentDetailList.Take(_batchSize).ToList());
                    var remainDocumentList = documentDetailList.Skip(_batchSize).ToList();
                    Send(remainDocumentList); //send remaining list
                    documentDetailList.Clear();
                }
                else
                {
                    Send(documentDetailList);
                    documentDetailList.Clear();
                }
            }
            //Send Log   
            if (overlayLogList == null || overlayLogList.Count <= 0) return;
            SendLog(overlayLogList);
            overlayLogList.Clear();

            #endregion
        }

        private void SendThreads(IEnumerable<ConversationInfo> conversationInfoList)
        {
            if (conversationInfoList == null || !conversationInfoList.Any())
            {
                return;
            }

            ThreadsInfo threadsInfo = new ThreadsInfo();

            foreach (ConversationInfo conversationInfo in conversationInfoList)
            {
                // Debug 
                //Tracer.Warning("conversationInfo: DocId = {0}, convIndex = {1}", conversationInfo.DocId, conversationInfo.ConversationIndex);

                string docReferenceId = conversationInfo.DocId;
                if (String.IsNullOrEmpty(docReferenceId))
                {
                    continue;
                }

                // Sanitize the value
                conversationInfo.ConversationIndex = String.IsNullOrEmpty(conversationInfo.ConversationIndex) ? null : conversationInfo.ConversationIndex;

                // On Append we only calculate relationships between new documents, therefore we don't even send standalone documents to Linker
                if (_jobParams.ImportOptions == ImportOptionsBEO.AppendNew && conversationInfo.ConversationIndex == null)
                {
                    continue;
                }

                var threadInfo = new ThreadInfo(docReferenceId, conversationInfo.ConversationIndex);
                threadsInfo.ThreadInfoList.Add(threadInfo);
            }
            SendThreads(threadsInfo);
        }

        private void SendThreads(ThreadsInfo threadsInfo)
        {
            if (threadsInfo == null || threadsInfo.ThreadInfoList == null || !threadsInfo.ThreadInfoList.Any())
            {
                return;
            }
            Pipe familiesAndThreadsPipe = GetOutputDataPipe("ThreadsLinker");
            familiesAndThreadsPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = threadsInfo
            };
            familiesAndThreadsPipe.Send(message);
        }

        private void SendFamilies(IEnumerable<DocumentDetail> documentDetailList)
        {
            if (!_jobParams.CreateFamilyGroups)
            {
                return;
            }

            FamiliesInfo familiesInfo = new FamiliesInfo();

            foreach (DocumentDetail doc in documentDetailList)
            {
                if (doc.docType != DocumentsetType.NativeSet)
                {
                    continue; // Only original documents may participate in relationships
                }

                string docReferenceId = doc.document.DocumentId;
                if (String.IsNullOrEmpty(docReferenceId))
                {
                    continue;
                }

                if (!String.IsNullOrEmpty(doc.document.EVLoadFileDocumentId))
                {
                    // We don't skip standalone documents for Families, because they always can appear to be topmost parents
                    FamilyInfo familyInfoRecord = new FamilyInfo(docReferenceId);
                    familyInfoRecord.OriginalDocumentId = doc.document.EVLoadFileDocumentId;
                    familyInfoRecord.OriginalParentId = (String.IsNullOrEmpty(doc.document.EVLoadFileParentId) || doc.document.EVLoadFileParentId == "0") ? null : doc.document.EVLoadFileParentId;

                    // Debug 
                    //Tracer.Warning("LawProcessingWorker.SendFamilies: DocId = {0}, OriginalDocumentId = {1}, OriginalParentId = {2}",
                    //    docReferenceId, familyInfoRecord.OriginalDocumentId, familyInfoRecord.OriginalParentId);

                    if (String.Equals(familyInfoRecord.OriginalDocumentId, familyInfoRecord.OriginalParentId, StringComparison.InvariantCulture))
                    {
                        //Tracer.Warning("SendRelationshipsInfo: OriginalDocumentId = {0}, OriginalParentId reset to null", familyInfoRecord.OriginalDocumentId);
                        familyInfoRecord.OriginalParentId = null; // Document must not be its own parent
                    }

                    familiesInfo.FamilyInfoList.Add(familyInfoRecord);
                }
            }

            SendFamilies(familiesInfo);
        }

        private void SendFamilies(FamiliesInfo familiesInfo)
        {
            if (familiesInfo == null || familiesInfo.FamilyInfoList == null || !familiesInfo.FamilyInfoList.Any())
            {
                return;
            }
            Pipe familiesAndThreadsPipe = GetOutputDataPipe("FamiliesLinker");
            familiesAndThreadsPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = familiesInfo
            };
            familiesAndThreadsPipe.Send(message);
        }

        /// <summary>
        /// Logs the error message.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="isSuccess">if set to <c>true</c> [is success].</param>
        /// <param name="message">The message.</param>
        private void LogErrorMessage(IEnumerable<RVWDocumentBEO> documents, bool isSuccess, string message)
        {
            try
            {
                var logs = documents.Select(document => new JobWorkerLog<LawImportLogInfo>
                {
                    JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                    WorkerInstanceId = WorkerId,
                    WorkerRoleType = Constants.LawImportStartupWorkerRoleType,
                    Success = isSuccess,
                    LogInfo = new LawImportLogInfo()
                    {
                        DCN = document.DocumentControlNumber,
                        CrossReferenceField = document.CrossReferenceFieldValue,
                        DocumentId = document.DocumentId,
                        Message = message,
                        Information = !isSuccess ? string.Format("{0} for DCN:{1}", message, document.DocumentControlNumber) : message
                    }
                }).ToList();
                LogPipe.ShouldNotBe(null);
                var logMessage = new PipeMessageEnvelope
                {
                    Body = logs
                };
                LogPipe.Send(logMessage);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex.ToUserString());
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private static LawImportBEO GetImportBEO(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(LawImportBEO));

            //Deserialization of bootparameter to get LawImportBEO
            return (LawImportBEO)xmlStream.Deserialize(stream);
        }

        #region Common

        /// <summary>
        /// Add key value pair into list
        /// </summary>
        /// <param name="source"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public Dictionary<string, string> AddKeyValues(Dictionary<string, string> source, Dictionary<string, string> collection)
        {
            foreach (var item in collection.Where(item => !source.ContainsKey(item.Key)))
            {
                source.Add(item.Key, item.Value);
            }
            return source;
        }

        /// <summary>
        /// To get the batch size for law import 
        /// </summary>
        private static int GetMessageBatchSize()
        {
            try
            {
                return Convert.ToInt32(ApplicationConfigurationManager.GetValue("LawPipelineBatchSize", "Imports"));
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                return 100;
            }
        }

        #endregion
    }
}
