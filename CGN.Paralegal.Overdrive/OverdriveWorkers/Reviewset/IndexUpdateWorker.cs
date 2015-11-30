#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="IndexUpdateWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Giri</author>
//      <description>
//          This file does the tagging operation in Search Sub System
//      </description>
//      <changelog>
//          <date value="28-Jan-2012">created</date>
//          <date value="03-Feb-2012">Modified LogMessage</date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//          <date value="03/09/2011">Bug Fix 97742</date>
//	        <date value="04/05/2012">Bug Fix # 98763</date>
//          <date value="6/5/2012">Fix for bug 100692 & 100624 - babugx</date>
//          <date value="09/06/2013">CNEV 2.2.2 - Split Reviewset NFR fix - babugx</date>
//          <date value="03/30/2015">CNEV 4.0 - Renamed VelocityUpdateWorker as IndexUpdateWorker : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespaces

using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.Common;
using System.Diagnostics;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

#endregion

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// This class owns the activity to perform tagging the metadata into the searchengine
    /// </summary>
    public class IndexUpdateWorker : WorkerBase
    {
        /// <summary>
        /// This method processes the pipe message and updates the document in Search Sub System
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var documentRecords = envelope.Body as DocumentRecordCollection;
            Debug.Assert(documentRecords != null, "documentRecords != null");
            documentRecords.Documents.Count.ShouldBeGreaterThan(0);
            documentRecords.ReviewsetDetails.ShouldNotBe(null);
            documentRecords.ReviewsetDetails.ReviewSetId.ShouldNotBeEmpty();
            documentRecords.ReviewsetDetails.BinderId.ShouldNotBeEmpty();

            try
            {
                UpdateDocuments(documentRecords);

                if (documentRecords.ReviewsetDetails.Activity == "Split")
                {
                    Send(documentRecords);
                }
                else
                {
                    Send(documentRecords.ReviewsetDetails);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, string.Format("Error in IndexUpdateWorker - Exception: {0}", ex.ToUserString()),
                    documentRecords.ReviewsetDetails.CreatedBy, documentRecords.ReviewsetDetails.ReviewSetName);
            }
            IncreaseProcessedDocumentsCount(documentRecords.Documents.Count);
        }

        /// <summary>
        /// Helper method to update the reviewset identifiers in search index
        /// </summary>
        /// <param name="documentRecords"></param>
        private void UpdateDocuments(DocumentRecordCollection documentRecords)
        {
            documentRecords.ReviewsetDetails.ReviewSetId.ShouldNotBeEmpty();
            var indexManagerProxy = new IndexManagerProxy
                (documentRecords.ReviewsetDetails.MatterId, documentRecords.ReviewsetDetails.CollectionId);

            var fields = new Dictionary<string, string>
            {
                {EVSystemFields.ReviewSetId, documentRecords.ReviewsetDetails.ReviewSetId},
                {EVSystemFields.BinderId, documentRecords.ReviewsetDetails.BinderId}
            };

            const int batchSize = 1000;
            var processedCount = 0;
            while (processedCount != documentRecords.Documents.Count)
            {
                List<DocumentIdentityRecord> documents;
                if ((documentRecords.Documents.Count - processedCount) < batchSize)
                {
                    documents =
                        documentRecords.Documents.Skip(processedCount)
                            .Take(documentRecords.Documents.Count - processedCount)
                            .ToList();
                    processedCount += documentRecords.Documents.Count - processedCount;
                }
                else
                {
                    documents = documentRecords.Documents.Skip(processedCount).Take(batchSize).ToList();
                    processedCount += batchSize;
                }

                var docs = documents.Select(doc => new DocumentBeo() { Id = doc.DocumentId, Fields = fields }).ToList();
                indexManagerProxy.BulkAppendFields(docs);

            }
        }

        private void Send(ReviewsetRecord reviewsetRecord)
        {
            var message = new PipeMessageEnvelope
            {
                Body = reviewsetRecord
            };

            if (null != OutputDataPipe)
            {
                OutputDataPipe.Send(message);
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
                    WorkerRoleType = Constants.SearchIndexUpdateWorkerRoleId,
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
                Tracer.Info("IndexUpdateWorker : LogMessage : Exception details: {0}", exception);
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
                Tracer.Info("IndexUpdateWorker : SendLog : Exception details: {0}", exception);
                throw;
            }
        }
    }
}