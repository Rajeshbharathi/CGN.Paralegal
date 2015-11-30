using System;
using System.Collections.Generic;
using System.Linq;
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
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    public class UpdateStatusWorker : WorkerBase
    {
        //holds the reviewset id, number of batches to be processed, number of batches processed
        private readonly Dictionary<string, ProcessedCount> reviewsets = new Dictionary<string, ProcessedCount>();

        /// <summary>
        /// overrides base ProcessMessage method
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            //get the review set record and add the count of batches processed
            var reviewsetRecord = (ReviewsetRecord)envelope.Body;
            reviewsetRecord.ShouldNotBe(null);
            try
            {
                if (!reviewsets.ContainsKey(reviewsetRecord.ReviewSetId))
                {
                    reviewsets.Add(reviewsetRecord.ReviewSetId,
                        new ProcessedCount
                        {
                            TotalNumberofBatches = reviewsetRecord.NumberOfBatches,
                            NumberofBatchesProcessed = 1
                        });
                }
                else
                {
                    reviewsets[reviewsetRecord.ReviewSetId].NumberofBatchesProcessed += 1;
                }

                if (reviewsets.Count <= 0) return;
                foreach (
                    var reviewset in
                        reviewsets.Where(
                            reviewset =>
                                (reviewset.Value.NumberofBatchesProcessed.Equals(reviewset.Value.TotalNumberofBatches)) &&
                                reviewsetRecord.ReviewSetId.Equals(reviewset.Key)))
                {
                    UpdateReviewSetStatus(reviewsetRecord);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, "Reviewset status was not updated.", reviewsetRecord.CreatedBy,
                    reviewsetRecord.ReviewSetId, reviewsetRecord.ReviewSetName);
            }
        }

        /// <summary>
        /// Updates the reviewset status to Active in DB
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void UpdateReviewSetStatus(ReviewsetRecord reviewsetRecord)
        {
            var reviewsetEntity = ConvertToReviewsetBusinessEntity(reviewsetRecord);
            reviewsetEntity.IsAuditable = false;
            using (var transScope = new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                //if all the documents are added successfully then update the status of review set
                ReviewSetBO.UpdateReviewSet(reviewsetEntity, false);
            }
        }

        /// <summary>
        /// converts to review set business entity to update the status
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        /// <returns></returns>
        private ReviewsetDetailsBEO ConvertToReviewsetBusinessEntity(ReviewsetRecord reviewsetRecord)
        {
            var returnReviewset = new ReviewsetDetailsBEO
            {
                ReviewSetId = reviewsetRecord.ReviewSetId,
                ReviewSetName = reviewsetRecord.ReviewSetName,
                StatusId = Constants.Active,
                ModifiedBy = reviewsetRecord.CreatedBy,
                DatasetId = reviewsetRecord.DatasetId,
                Description = reviewsetRecord.ReviewSetDescription,
                DueDate = reviewsetRecord.DueDate,
                KeepDuplicates = reviewsetRecord.KeepDuplicatesTogether,
                KeepFamily = reviewsetRecord.KeepFamilyTogether,
                ReviewSetGroup = reviewsetRecord.ReviewSetGroup,
                ReviewSetLogic = reviewsetRecord.ReviewSetLogic,
                SearchQuery = reviewsetRecord.SearchQuery,
                SplittingOption = reviewsetRecord.SplittingOption,
                StartDate = reviewsetRecord.StartDate,
                NumberOfDocuments = reviewsetRecord.NumberOfDocuments,
                NumberOfReviewedDocs = reviewsetRecord.NumberOfReviewedDocs
            };

            return returnReviewset;
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information, string createdBy, string reviewsetId,
            string reviewsetName)
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
        private List<JobWorkerLog<ReviewsetLogInfo>> ConstructLogMessage(bool status, string information,
            string createdBy)
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
                LogInfo = new ReviewsetLogInfo {Information = information}
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
                var message = new PipeMessageEnvelope
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

    public class ProcessedCount
    {
        public int TotalNumberofBatches { get; set; }
        public int NumberofBatchesProcessed { get; set; }
        public string ReviewSetName { get; set; }
    }
}