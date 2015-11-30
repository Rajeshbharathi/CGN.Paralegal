using System;
using System.Collections.Generic;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Business.ReviewSet;
using System.Transactions;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Worker
{
    using Infrastructure.ExceptionManagement;

    public class SplitUpdateStatusWorker : WorkerBase
    {
        //holds the reviewset id, number of batches to be processed, number of batches processed
        private Dictionary<string, ProcessedCount> reviewsets = new Dictionary<string, ProcessedCount>();
        private int processedReviewsetCount = 0;

        /// <summary>
        /// overrides base ProcessMessage method
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            //get the review set record and add the count of batches processed

            var documentRecords = envelope.Body as DocumentRecordCollection;
            documentRecords.ShouldNotBe(null);
            documentRecords.ReviewsetDetails.ShouldNotBe(null);
            
            try
            {
                if (!reviewsets.ContainsKey(documentRecords.ReviewsetDetails.ReviewSetId))
                {
                    reviewsets.Add(documentRecords.ReviewsetDetails.ReviewSetId, new ProcessedCount
                        {
                            TotalNumberofBatches = documentRecords.ReviewsetDetails.NumberOfBatches,
                            NumberofBatchesProcessed = 1,
                            ReviewSetName = documentRecords.ReviewsetDetails.ReviewSetName
                        });
                }
                else
                {
                    reviewsets[documentRecords.ReviewsetDetails.ReviewSetId].NumberofBatchesProcessed += 1;
                }

                if (reviewsets.Count > 0)
                {
                    foreach (KeyValuePair<string, ProcessedCount> reviewset in reviewsets)
                    {
                        // If all the batches are processed for all the reviewsets, write audit log here
                        if ((reviewset.Value.NumberofBatchesProcessed.Equals(reviewset.Value.TotalNumberofBatches)) &&
                            documentRecords.ReviewsetDetails.ReviewSetId.Equals(reviewset.Key))
                        {
                            UpdateReviewSetStatus(documentRecords.ReviewsetDetails, Constants.Active,false);
                            processedReviewsetCount += 1;
                        }
                    }

                    if (reviewsets.Count == processedReviewsetCount)
                    {
                        documentRecords.ReviewsetDetails.ReviewSetId = documentRecords.ReviewsetDetails.SplitReviewSetId;
                        documentRecords.ReviewsetDetails.ReviewSetName = documentRecords.ReviewsetDetails.SplitReviewSetName;
                        documentRecords.ReviewsetDetails.NumberOfDocuments =
                            documentRecords.ReviewsetDetails.SplitPreDocumentCount - documentRecords.TotalDocumentCount;
                        if (documentRecords.ReviewsetDetails.SplitPreDocumentCount - documentRecords.TotalDocumentCount == 0)
                        {
                            UpdateReviewSetStatus(documentRecords.ReviewsetDetails, Constants.Archive,false);
                        }
                        else
                        {
                            UpdateReviewSetStatus(documentRecords.ReviewsetDetails, Constants.Active,false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, "Split Reviewset status was not updated.", documentRecords.ReviewsetDetails.CreatedBy,
                    documentRecords.ReviewsetDetails.SplitReviewSetId, documentRecords.ReviewsetDetails.SplitReviewSetName);
            }
        }

        /// <summary>
        /// Updates the reviewset status to Active in DB
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        /// <param name="status"></param>
        /// <param name="isAuditable"></param>
        private void UpdateReviewSetStatus(ReviewsetRecord reviewsetRecord, int status,bool isAuditable=true)
        {
            ReviewsetDetailsBEO reviewsetEntity = ConvertToReviewsetBusinessEntity(reviewsetRecord, status);
            reviewsetEntity.IsAuditable = isAuditable;
            using (EVTransactionScope transScope = new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                //if all the documents are added successfully then update the status of review set
                ReviewSetBO.UpdateReviewSet(reviewsetEntity, false);
            }
        }

        /// <summary>
        /// converts to review set business entity to update the status
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private ReviewsetDetailsBEO ConvertToReviewsetBusinessEntity(ReviewsetRecord reviewsetRecord, int status)
        {
            ReviewsetDetailsBEO returnReviewset = new ReviewsetDetailsBEO();
            returnReviewset.ReviewSetId = reviewsetRecord.ReviewSetId;
            returnReviewset.ReviewSetName = reviewsetRecord.ReviewSetName;
            returnReviewset.StatusId = status;
            returnReviewset.ModifiedBy = reviewsetRecord.CreatedBy;
            returnReviewset.DatasetId = reviewsetRecord.DatasetId;

            returnReviewset.Description = reviewsetRecord.ReviewSetDescription;
            returnReviewset.DueDate = reviewsetRecord.DueDate;
            returnReviewset.KeepDuplicates = reviewsetRecord.KeepDuplicatesTogether;
            returnReviewset.KeepFamily = reviewsetRecord.KeepFamilyTogether;
            returnReviewset.ReviewSetGroup = reviewsetRecord.ReviewSetGroup;
            returnReviewset.ReviewSetLogic = reviewsetRecord.ReviewSetLogic;
            returnReviewset.SearchQuery = reviewsetRecord.SearchQuery;
            returnReviewset.SplittingOption = reviewsetRecord.SplittingOption;
            returnReviewset.StartDate = reviewsetRecord.StartDate;

            returnReviewset.NumberOfDocuments = reviewsetRecord.NumberOfDocuments;
            returnReviewset.NumberOfReviewedDocs = reviewsetRecord.NumberOfReviewedDocs;

            return returnReviewset;

        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information, string createdBy, string reviewsetId, string reviewsetName)
        {
            var log = ConstructLogMessage(status, information, createdBy);
            SendLog(log);
        }

        /// <summary>
        /// This method constructs the log message to send
        /// </summary>
        /// <param name="status">bool</param>
        /// <param name="information">string</param>
        /// <param name="createdBy">string</param>
        /// <returns>List<JobWorkerLog<ReviewsetLogInfo>></returns>
        private List<JobWorkerLog<ReviewsetLogInfo>> ConstructLogMessage(bool status, string information, string createdBy)
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
                LogInfo = new ReviewsetLogInfo { Information = information }
            };
            // TaskId
            log.Add(parserLog);
            return log;
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
                var message = new PipeMessageEnvelope()
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
            catch (Exception exception)
            {
                Tracer.Info("UpdateStatusWorker : SendLog : Exception details: {0}", exception);
            }
        }
    }    
}
