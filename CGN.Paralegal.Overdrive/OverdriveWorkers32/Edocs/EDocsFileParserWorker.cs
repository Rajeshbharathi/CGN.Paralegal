using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.FileUtilities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Business.MatterManagement;
using LexisNexis.Evolution.Worker.Data;

namespace LexisNexis.Evolution.Worker
{
    public class EDocsFileParserWorker : WorkerBase
    {
        private ProfileBEO profileBEO;
        private int fileParserBatchSize = 10;
        private FileIOHelper fileIoHelper;

        protected override void BeginWork()
        {
            base.BeginWork();

            try
            {
                var strFileParserBatchSize = ApplicationConfigurationManager.GetValue("FileParserBatchSize", "Imports",
                    false);
                if (!String.IsNullOrEmpty(strFileParserBatchSize))
                {
                    fileParserBatchSize = int.Parse(strFileParserBatchSize);
                }

                fileIoHelper = new FileIOHelper();

                // function that's called when a batch of documents are available.
                fileIoHelper.BatchOfDocumentsAvailable += Send;

                profileBEO = DocumentImportHelper.GetProfileBeo(BootParameters);

                #region Check for minimum required information.

                // Check if minimum required information, dataset details and matter details available.
                if (profileBEO.DatasetDetails == null)
                    throw new EVException().AddResMsg(
                        ErrorCodes.EDLoaderFileParserWorker_DatasetOrMatterDetailsUnavailable);
                if (profileBEO.DatasetDetails.Matter == null)
                    throw new EVException().AddResMsg(
                        ErrorCodes.EDLoaderFileParserWorker_DatasetOrMatterDetailsUnavailable);

                #endregion Check for minimum required information

                //TODO: Search Engine Replacement - Search Sub System - Create Seed Directory, if required
            }
            catch (Exception ex)
            {
                LogMessage(false, "Failed on initialize values. " + ex.ToUserString());
                ReportToDirector(ex);
                throw;
            }
        }

        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            try
            {
                if (profileBEO != null && profileBEO.FileLocations != null)
                {
                    fileIoHelper.GetSortedFileList(profileBEO.FileLocations, profileBEO.ExcludedFileTypes,
                        fileParserBatchSize);
                }
                LogMessage(true, "Successfully parsed source file.");
            }
            catch (Exception ex)
            {
                LogMessage(false, "Failed to parse source file. " + ex.ToUserString());
                ReportToDirector(ex);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Sends the specified file list to next worker in the pipeline.
        /// </summary>
        /// <param name="fileList">The file list.</param>
        /// <param name="isLastBatch">if set to <c>true</c> the current batch is the last available batch[is last batch].</param>
        private void Send(IEnumerable<string> fileList, bool isLastBatch)
        {
            if (fileList == null)
            {
                return;
            }

            var message = new PipeMessageEnvelope
            {
                Body =
                    new DocumentExtractionMessageEntity {FileCollection = fileList, IsLastMessageInBatch = isLastBatch}
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(fileList.Count());
        }

        #region Log

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information)
        {
            var log = new List<JobWorkerLog<EdLoaderParserLogInfo>>();
            var parserLog = new JobWorkerLog<EdLoaderParserLogInfo>();
            parserLog.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            parserLog.CorrelationId = 0; // TaskId
            parserLog.WorkerRoleType = "d279b1fd-dd38-4afd-a6fb-22527dfbc7aa";
            parserLog.WorkerInstanceId = WorkerId;
            parserLog.IsMessage = false;
            parserLog.Success = status;
            parserLog.CreatedBy = (!string.IsNullOrEmpty(profileBEO.CreatedBy) ? profileBEO.CreatedBy : "N/A");
            parserLog.LogInfo = new EdLoaderParserLogInfo();
            parserLog.LogInfo.Information = information;
            parserLog.LogInfo.Message = information;
            log.Add(parserLog);
            SendLog(log);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<EdLoaderParserLogInfo>> log)
        {
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        #endregion
    }
}