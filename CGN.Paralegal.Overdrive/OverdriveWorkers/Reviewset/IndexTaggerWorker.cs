#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="IndexTaggerWorker" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>babugx</author>
//      <description>
//          This worker file owns the responsibility to write the tag information in the search-engine against the given documents
//      </description>
//      <changelog>
//          <date value="07-Nov-2013">Created</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//          <date value="03/30/2015">CNEV 4.0 - Renamed VelocityTaggerWorker as IndexTaggerWorker : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespace

using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Linq;


#endregion

namespace LexisNexis.Evolution.Worker
{
    using Infrastructure.ExceptionManagement;

    public class IndexTaggerWorker : SearchEngineWorkerBase
    {
        private CreateReviewSetJobBEO _jobParameter;
        #region Private Variables

        private IVaultManager m_VaultManager;

        #endregion

        /// <summary>
        /// Document Vault Manager Properties
        /// </summary>        
        public IVaultManager VaultMngr
        {
            get { return m_VaultManager ?? (m_VaultManager = new VaultManager()); }
            set { m_VaultManager = value; }
        }


        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (CreateReviewSetJobBEO)XmlUtility.DeserializeObject(BootParameters, typeof(CreateReviewSetJobBEO));
            _jobParameter.ShouldNotBe(null);          
            SetCommiyIndexStatusToInitialized(_jobParameter.MatterId);

        }
        /// <summary>
        /// overrides base ProcessMessage method
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var reviewsetRecord = (DocumentRecordCollection) envelope.Body;
            reviewsetRecord.ShouldNotBe(null);
            reviewsetRecord.ReviewsetDetails.CreatedBy.ShouldNotBeEmpty();
            reviewsetRecord.Documents.ShouldNotBe(null);

            try
            {
                if (reviewsetRecord.DocumentTags == null || !reviewsetRecord.DocumentTags.Any())
                {
                    Send(reviewsetRecord);
                }
                else
                {
                    UpdateTag(reviewsetRecord);
                    Send(reviewsetRecord);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, string.Format("Error in IndexTaggerWorker - Exception: {0}", ex.ToUserString()),
                    reviewsetRecord.ReviewsetDetails.CreatedBy, reviewsetRecord.ReviewsetDetails.ReviewSetName);
            }
        }
        /// <summary>
        /// Ends the work.
        /// </summary>
        protected override void EndWork()
        {
            base.EndWork();
            SetCommitIndexStatusToCompleted(_jobParameter.MatterId);

        }

        #region Helper Methods

        /// <summary>
        /// Update the binder >> not_reviewed tag for the documents
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void UpdateTag(DocumentRecordCollection reviewsetRecord)
        {
            var indexManagerProxy = new IndexManagerProxy(reviewsetRecord.ReviewsetDetails.MatterId, reviewsetRecord.ReviewsetDetails.CollectionId);
            var documentList = reviewsetRecord.DocumentTags.
                GroupBy(item => item.DocumentId).ToDictionary(group => group.Key, group => group.ToList());
            var tagsList = new Dictionary<string, KeyValuePair<string, string>>();

            // create key value pair for every document to be updated in Search Sub System
            foreach (var document in documentList)
            {
                var strTags = string.Join(",", document.Value.Where(x => x.Status == 1).
                    Select(t => String.Format(EVSearchSyntax.TagValueFormat + "{0}", t.TagId)).ToArray());
                tagsList.Add(document.Key,
                             string.IsNullOrEmpty(strTags)
                                 ? new KeyValuePair<string, string>(EVSystemFields.Tag, string.Empty)
                                 : new KeyValuePair<string, string>(EVSystemFields.Tag, strTags));
            }

            const int batchSize = 1000;
            var processedCount = 0;
            while (processedCount != tagsList.Count)
            {
                Dictionary<string, KeyValuePair<string, string>> batchTags;
                if ((tagsList.Count - processedCount) < batchSize)
                {
                    batchTags = tagsList.Skip(processedCount).Take(tagsList.Count - processedCount).ToDictionary(x => x.Key, x => x.Value);
                    processedCount += tagsList.Count - processedCount;
                }
                else
                {
                    batchTags = tagsList.Skip(processedCount).Take(batchSize).ToDictionary(x => x.Key, x => x.Value);
                    processedCount += batchSize;
                }

                if (!batchTags.Any()) continue;

                var docs = batchTags.Select(doc => new DocumentBeo()
                {
                    Id = doc.Key,
                    Fields = new Dictionary<string, string> { { doc.Value.Key, doc.Value.Value } }
                }).ToList();

                indexManagerProxy.BulkUpdateDocumentsAsync(docs);
            }
        }


        private void Send(DocumentRecordCollection reviewsetRecord)
        {
            var message = new PipeMessageEnvelope
            {
                Body = reviewsetRecord
            };
            if (null != OutputDataPipe)
            {
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(reviewsetRecord.Documents.Count);
            }
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information, string createdBy, string reviewsetName)
        {
            try
            {
                var log = new List<JobWorkerLog<ReviewsetLogInfo>>();
                var parserLog = new JobWorkerLog<ReviewsetLogInfo>
                {
                    JobRunId =
                        (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                    CorrelationId = 0,
                    WorkerRoleType = Constants.SearchIndexTaggerRoleId,
                    WorkerInstanceId = WorkerId,
                    IsMessage = false,
                    Success = status,
                    CreatedBy = createdBy,
                    LogInfo = new ReviewsetLogInfo {Information = information, ReviewsetName = reviewsetName}
                };
                // TaskId
                log.Add(parserLog);
                SendLog(log);
            }
            catch (Exception exception)
            {
                Tracer.Info("IndexTaggerWorker : LogMessage : Exception details: {0}", exception);
            }
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ReviewsetLogInfo>> log)
        {
            try
            {
                LogPipe.Open();
                var message = new PipeMessageEnvelope
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
            catch (Exception exception)
            {
                Tracer.Info("IndexTaggerWorker : SendLog : Exception details: {0}", exception);
                throw;
            }
        }

        #endregion
    }
}