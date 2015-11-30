#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="LogWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>CNEV</author>
//      <description>
//         Log worker class
//      </description>
//      <changelog>
//  <date value="04/03/2012">Bug fix for 98615</date>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//          <date value="02/28/2014">Included error handling </date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.JobManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;

namespace LexisNexis.Evolution.Worker
{
    public class LogWorker : WorkerBase
    {
        protected override void ProcessMessage(PipeMessageEnvelope pipeMessage)
        {
            try
            {
                this.LogData(pipeMessage.Body);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Log message
        /// </summary>
        /// <param name="data"></param>
        public void LogData(object data)
        {
            var list1 = data as List<JobWorkerLog<LoadFileDocumentParserLogInfo>>;
            if (null != list1)//Load File Record Parser
            {
                if (list1.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list1.Select(ConstructWorkerLog<LoadFileDocumentParserLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list1.Count);
                }
                return;
            }

            var list2 = data as List<JobWorkerLog<LoadFileParserLogInfo>>;
            if (null != list2)//Load File Parser
            {
                if (list2.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list2.Select(ConstructWorkerLog<LoadFileParserLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list2.Count);
                }
                return;
            }

            var list3 = data as List<JobWorkerLog<OverlaySearchLogInfo>>;
            if (null != list3)//Load File Parser
            {
                if (list3.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list3.Select(ConstructWorkerLog<OverlaySearchLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list3.Count);
                }
                return;
            }

            var list4 = data as List<JobWorkerLog<VaultLogInfo>>;
            if (null != list4)//Load File Parser
            {
                if (list4.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list4.Select(ConstructWorkerLog<VaultLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list4.Count);
                }
                return;
            }

            var list5 = data as List<JobWorkerLog<SearchIndexLogInfo>>;
            if (null != list5)//Load File Parser
            {
                if (list5.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list5.Select(ConstructWorkerLog<SearchIndexLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list5.Count);
                }
                return;
            }

            var list6 = data as List<JobWorkerLog<NearNativeLogInfo>>;
            if (null != list6)//Load File Parser
            {
                if (list6.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list6.Select(ConstructWorkerLog<NearNativeLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list6.Count);
                }
                return;
            }

            var list7 = data as List<JobWorkerLog<EdLoaderParserLogInfo>>;
            if (null != list7)//Load File Parser
            {
                if (list7.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list7.Select(ConstructWorkerLog<EdLoaderParserLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list7.Count);
                }
                return;
            }

            var list8 = data as List<JobWorkerLog<DcbParserLogInfo>>;
            if (null != list8)//Load File Parser
            {
                if (list8.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list8.Select(ConstructWorkerLog<DcbParserLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list8.Count);
                }
                return;
            }

            var list9 = data as List<JobWorkerLog<EDocsExtractionLogInfo>>;
            if (null != list9)//Load File Parser
            {
                if (list9.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list9.Select(ConstructWorkerLog<EDocsExtractionLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list9.Count);
                }
                return;
            }

            var list10 = data as List<JobWorkerLog<ProductionParserLogInfo>>;
            if (null != list10)//Load File Parser
            {
                if (list10.Any())
                {
                    List<ProductionWorkerLogBEO> workerLogList =
                        list10.Select(ConstructProductionWorkerLog<ProductionParserLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertProductionJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list10.Count);
                }
                return;
            }

            var list11 = data as List<JobWorkerLog<ReviewsetLogInfo>>;
            if (null != list11)//Load File Parser
            {
                if (list11.Any())
                {
                    List<ReviewsetLogBEO> workerLogList =
                        list11.Select(ConstructReviewsetWorkerLog<ReviewsetLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertReviewsetJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list11.Count);
                }
                return;
            }

            #region Export
            var list12 = data as List<JobWorkerLog<ExportMetadataLogInfo>>;
            if (null != list12)//Load File Parser
            {
                if (list12.Any())
                {
                    List<ExportWorkerLogBEO> workerLogList =
                        list12.Select(ConstructExportWorkerLog<ExportMetadataLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertExportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list12.Count);
                }
                return;
            }

            var list13 = data as List<JobWorkerLog<ExportStartupLogInfo>>;
            if (null != list13)//Load File Parser
            {
                if (list13.Any())
                {
                    List<ExportWorkerLogBEO> workerLogList =
                        list13.Select(ConstructExportWorkerLog<ExportStartupLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertExportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list13.Count);
                }
                return;
            }

            var list14 = data as List<JobWorkerLog<ExportFileCopyLogInfo>>;
            if (null != list14)//Load File Parser
            {
                if (list14.Any())
                {
                    List<ExportWorkerLogBEO> workerLogList =
                        list14.Select(ConstructExportWorkerLog<ExportFileCopyLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertExportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list14.Count);
                }
                return;
            }

            var list15 = data as List<JobWorkerLog<ExportLoadFileWritterLogInfo>>;
            if (null != list15)//Load File Parser
            {
                if (list15.Any())
                {
                    List<ExportWorkerLogBEO> workerLogList =
                        list15.Select(ConstructExportWorkerLog<ExportLoadFileWritterLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertExportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list15.Count);
                }
                return;
            }
            
            var list16 = data as List<JobWorkerLog<LawImportLogInfo>>;
            if (null != list16)//Law Import
            {
                if (list16.Any())
                {
                    List<ImportWorkerLogBEO> workerLogList =
                        list16.Select(ConstructWorkerLog<LawImportLogInfo>).ToList();
                    JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(workerLogList);
                    IncreaseProcessedDocumentsCount(list16.Count);
                }
                return;
            }
            #endregion

            var list17 = data as List<JobWorkerLog<LogInfo>>;
            if (null != list17)
            {
                if (list17.Any())
                {
                    int JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToInt32(PipelineId) : 0;
                    int jobID = JobMgmtDAO.GetJobIdFromJobRunId(JobRunId);
                    foreach (JobWorkerLog<LogInfo> log in list17)
                    {
                        if (log.WorkerRoleType != "PrintValidationWorker") // Don't do Insert for "PrintValidationWorker"
                        {
                            byte[] binaryData = DatabaseBroker.SerializeObjectBinary<LogInfo>(log.LogInfo);
                            DatabaseBroker.InsertTaskDetails((int) log.JobRunId, (int) log.CorrelationId, binaryData);
                        }
                        Helper.UpdateTaskLog(jobID, (int)log.JobRunId, (int)log.CorrelationId, string.IsNullOrEmpty(log.LogInfo.TaskKey) ? string.Empty : log.LogInfo.TaskKey, Helper.SerializeObject<LogInfo>(log.LogInfo), log.LogInfo.IsError, System.DateTime.UtcNow.AddMinutes(-5), System.DateTime.UtcNow, string.Empty);
                    }

                    IncreaseProcessedDocumentsCount(list17.Count);
                }
                return;
            }

            var listBulkTagLog = data as List<JobWorkerLog<TagLogInfo>>;
            if (null != listBulkTagLog && listBulkTagLog.Any())
            {
                var workerLogList = listBulkTagLog.Select(ConstructBulkTagWorkerLog<TagLogInfo>).ToList();
                JobMgmtDAO.BulkInsertJobTaskWorkerLog(workerLogList);
                IncreaseProcessedDocumentsCount(workerLogList.Count);
                return;
            }

            var listNearDupeLog = data as List<JobWorkerLog<NearDuplicationLogInfo>>;
            if (null != listNearDupeLog && listNearDupeLog.Any()) //Near Duplication Job
            {
                var workerLogList =
                    listNearDupeLog.Select(ConstructNearDuplicationWorkerLog<NearDuplicationLogInfo>).ToList();
                JobMgmtDAO.BulkInsertJobTaskWorkerLog(workerLogList);
                IncreaseProcessedDocumentsCount(listNearDupeLog.Count);
                return;
            }

            var listTaggingLog = data as List<JobWorkerLog<LawImportTaggingLogInfo>>;
            if (null != listTaggingLog && listTaggingLog.Any())
            {
                List<ImportWorkerLogBEO> logList =
                        listTaggingLog.Select(ConstructWorkerLog<LawImportTaggingLogInfo>).ToList();
                JobMgmtDAO.BulkInsertImportJobTaskWorkerLog(logList);
                IncreaseProcessedDocumentsCount(listTaggingLog.Count);
                return;
            }

            var listLawSyncLog = data as List<JobWorkerLog<LawSyncLogInfo>>;
            if (null != listLawSyncLog && listLawSyncLog.Any()) //Law Sync Job
            {
                var workerLogList =
                    listLawSyncLog.Select(ConstructLawSyncWorkerLog).ToList();
                JobMgmtDAO.BulkInsertJobTaskWorkerLog(workerLogList);
                IncreaseProcessedDocumentsCount(listLawSyncLog.Count);
                return;
            }

        } // LogData

        public ImportWorkerLogBEO ConstructWorkerLog<T>(JobWorkerLog<T> log)
        {
            var workerLog = new ImportWorkerLogBEO
                {
                    JobRunId = log.JobRunId,
                    CorrelationId = log.CorrelationId,
                    WorkerInstanceId = log.WorkerInstanceId,
                    WorkerRoleType = log.WorkerRoleType,
                    IsError = !log.Success,
                    IsWarning = log.IsMessage,
                    CreatedBy = log.CreatedBy
                };

            if (log is JobWorkerLog<LoadFileDocumentParserLogInfo>)
            {
                var documentParserLog = log as JobWorkerLog<LoadFileDocumentParserLogInfo>;
                workerLog.IsMissingImage = documentParserLog.LogInfo.IsMissingImage;
                workerLog.IsMissingNative = documentParserLog.LogInfo.IsMissingNative;
                workerLog.IsMissingText = documentParserLog.LogInfo.IsMissingText;
                workerLog.DocumentsAdded = documentParserLog.LogInfo.AddedDocument;
                workerLog.ImagesAdded = documentParserLog.LogInfo.AddedImages;
                workerLog.LogDescription = documentParserLog.LogInfo.Information;
                workerLog.DCN = documentParserLog.LogInfo.DCN;
                workerLog.CrossReferenceField = documentParserLog.LogInfo.CrossReferenceField;
                workerLog.Message = documentParserLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<LoadFileParserLogInfo>)
            {
                var loadFileParserLog = log as JobWorkerLog<LoadFileParserLogInfo>;
                workerLog.LogDescription = loadFileParserLog.LogInfo.Information;
                workerLog.CrossReferenceField = loadFileParserLog.LogInfo.CrossReferenceField;
                workerLog.Message = loadFileParserLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<DcbParserLogInfo>)
            {
                var dcbParserLog = log as JobWorkerLog<DcbParserLogInfo>;
                workerLog.LogDescription = dcbParserLog.LogInfo.Information;
                workerLog.CrossReferenceField = dcbParserLog.LogInfo.CrossReferenceField;
                workerLog.Message = dcbParserLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<EdLoaderParserLogInfo>)
            {
                var edloaderParserLog = log as JobWorkerLog<EdLoaderParserLogInfo>;
                workerLog.LogDescription = edloaderParserLog.LogInfo.Information;
                workerLog.CrossReferenceField = edloaderParserLog.LogInfo.CrossReferenceField;
                workerLog.Message = edloaderParserLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<NearNativeLogInfo>)
            {
                var nearNativeLog = log as JobWorkerLog<NearNativeLogInfo>;
                workerLog.LogDescription = nearNativeLog.LogInfo.Information;
                workerLog.CrossReferenceField = nearNativeLog.LogInfo.CrossReferenceField;
                workerLog.Message = nearNativeLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<VaultLogInfo>)
            {
                var vaultLog = log as JobWorkerLog<VaultLogInfo>;
                //DCN need to set
                workerLog.DCN = vaultLog.LogInfo.DCN;
                workerLog.LogDescription = vaultLog.LogInfo.Information;
                workerLog.CrossReferenceField = vaultLog.LogInfo.CrossReferenceField;
                workerLog.Message = vaultLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<SearchIndexLogInfo>)
            {
                var searchIndexLogInfo = log as JobWorkerLog<SearchIndexLogInfo>;
                if (searchIndexLogInfo.LogInfo == null) return workerLog;
                workerLog.DCN = searchIndexLogInfo.LogInfo.DCNNumber;
                workerLog.LogDescription = searchIndexLogInfo.LogInfo.Information;
                workerLog.CrossReferenceField = searchIndexLogInfo.LogInfo.CrossReferenceField;
                workerLog.Message = searchIndexLogInfo.LogInfo.Message;
            }
            else if (log is JobWorkerLog<OverlaySearchLogInfo>)
            {
                var overlayLog = log as JobWorkerLog<OverlaySearchLogInfo>;
                //workerLog.DCN = overlayLog.LogInfo.OverlayDocumentId;
                workerLog.IsOverLay = overlayLog.LogInfo.IsDocumentUpdated;
                workerLog.IsUnableToOverLay = overlayLog.LogInfo.IsNoMatch;
                workerLog.LogDescription = overlayLog.LogInfo.Information;
                workerLog.DCN = overlayLog.LogInfo.DCN;
                workerLog.DocumentsAdded = (overlayLog.LogInfo.IsDocumentAdded ? 1 : 0);
                workerLog.CrossReferenceField = overlayLog.LogInfo.CrossReferenceField;
                workerLog.Message = overlayLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<EDocsExtractionLogInfo>)
            {
                var edocsExtractionLog = log as JobWorkerLog<EDocsExtractionLogInfo>;
                workerLog.LogDescription = edocsExtractionLog.LogInfo.Information;
                workerLog.CreatedBy = edocsExtractionLog.CreatedBy;
                workerLog.JobRunId = edocsExtractionLog.JobRunId;
                workerLog.WorkerRoleType = edocsExtractionLog.WorkerRoleType;
                workerLog.CrossReferenceField = edocsExtractionLog.LogInfo.CrossReferenceField;
                workerLog.Message = edocsExtractionLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<LawImportLogInfo>)
            {
                var documentParserLog = log as JobWorkerLog<LawImportLogInfo>;
                workerLog.IsMissingImage = documentParserLog.LogInfo.IsMissingImage;
                workerLog.IsMissingNative = documentParserLog.LogInfo.IsMissingNative;
                workerLog.IsMissingText = documentParserLog.LogInfo.IsMissingText;
                workerLog.DocumentsAdded = documentParserLog.LogInfo.AddedDocument;
                workerLog.ImagesAdded = documentParserLog.LogInfo.AddedImages;
                workerLog.LogDescription = documentParserLog.LogInfo.Information;
                workerLog.DCN = documentParserLog.LogInfo.DCN;
                workerLog.CrossReferenceField = documentParserLog.LogInfo.CrossReferenceField;
                workerLog.Message = documentParserLog.LogInfo.Message;
            }
            else if (log is JobWorkerLog<LawImportTaggingLogInfo>)
            {
                var taggingLog = log as JobWorkerLog<LawImportTaggingLogInfo>;
                workerLog.DCN = taggingLog.LogInfo.DCN;
                workerLog.LogDescription = taggingLog.LogInfo.Information;
                workerLog.CrossReferenceField = taggingLog.LogInfo.CrossReferenceField;
                workerLog.Message = taggingLog.LogInfo.Message;
            }

            return workerLog;
        }

        public ProductionWorkerLogBEO ConstructProductionWorkerLog<T>(JobWorkerLog<T> log)
        {
            ProductionWorkerLogBEO workerLog = new ProductionWorkerLogBEO();
            workerLog.JobRunId = Convert.ToInt32(log.JobRunId);
            workerLog.CorrelationId = log.CorrelationId;
            workerLog.WorkerInstanceId = log.WorkerInstanceId;
            workerLog.WorkerRoleType = log.WorkerRoleType;
            workerLog.IsError = !log.Success;
            workerLog.CreatedBy = log.CreatedBy;
            if (log is JobWorkerLog<ProductionParserLogInfo>)
            {
                var documentParserLog = log as JobWorkerLog<ProductionParserLogInfo>;
                workerLog.DCN = documentParserLog.LogInfo.DCN;
                workerLog.DatasetName = documentParserLog.LogInfo.DatasetName;
                workerLog.BatesNumber = documentParserLog.LogInfo.BatesNumber;
                workerLog.ProductionDocumentNumber = documentParserLog.LogInfo.ProductionDocumentNumber;
                workerLog.ProductionName = documentParserLog.LogInfo.ProductionName;
                workerLog.LogDescription = documentParserLog.LogInfo.Information;
            }
            else if (log is JobWorkerLog<ProductionParserLogInfo>)
            {
                var productionParserLog = log as JobWorkerLog<ProductionParserLogInfo>;
                workerLog.LogDescription = productionParserLog.LogInfo.Information;
            }

            return workerLog;
        }


        public PrintWorkerLogBusinessEntity ConstructPrintWorkerLog<T>(JobWorkerLog<T> log)
        {
            PrintWorkerLogBusinessEntity workerLog = new PrintWorkerLogBusinessEntity();
            workerLog.JobRunId = Convert.ToInt32(log.JobRunId);
            workerLog.CorrelationId = log.CorrelationId;
            workerLog.WorkerInstanceId = log.WorkerInstanceId;
            workerLog.WorkerRoleType = log.WorkerRoleType;
            workerLog.IsError = !log.Success;
            workerLog.CreatedBy = log.CreatedBy;
            if (log is JobWorkerLog<PrintLogInfo>)
            {
                var documentParserLog = log as JobWorkerLog<PrintLogInfo>;
                workerLog.Dcn = documentParserLog.LogInfo.DCN;
                workerLog.DatasetName = documentParserLog.LogInfo.DatasetName;
                workerLog.PrintJobName = documentParserLog.LogInfo.PrintJobName;
                workerLog.LogDescription = documentParserLog.LogInfo.Information;
            }
            return workerLog;
        }

        public ReviewsetLogBEO ConstructReviewsetWorkerLog<T>(JobWorkerLog<T> log)
        {
            ReviewsetLogBEO workerLog = new ReviewsetLogBEO();
            workerLog.JobRunId = Convert.ToInt32(log.JobRunId);
            workerLog.TaskId = log.CorrelationId;
            workerLog.WorkerInstanceId = log.WorkerInstanceId;
            workerLog.WorkerRoleType = log.WorkerRoleType;
            workerLog.IsError = !log.Success;
            workerLog.CreatedBy = log.CreatedBy;
            if (log is JobWorkerLog<ReviewsetLogInfo>)
            {
                var documentParserLog = log as JobWorkerLog<ReviewsetLogInfo>;
                workerLog.ReviewsetId = documentParserLog.LogInfo.ReviewsetID;
                workerLog.ReviewsetName = documentParserLog.LogInfo.ReviewsetName;
                workerLog.LogDescription = documentParserLog.LogInfo.Information;
            }

            return workerLog;
        }

        public ExportWorkerLogBEO ConstructExportWorkerLog<T>(JobWorkerLog<T> log)
        {
            ExportWorkerLogBEO workerLog = new ExportWorkerLogBEO();
            workerLog.JobRunId = log.JobRunId;
            workerLog.CorrelationId = log.CorrelationId;
            workerLog.WorkerInstanceId = log.WorkerInstanceId;
            workerLog.WorkerRoleType = log.WorkerRoleType;
            workerLog.IsError = !log.Success;
            workerLog.IsWarning = log.IsMessage;
            workerLog.CreatedBy = log.CreatedBy;
            if (log is JobWorkerLog<ExportMetadataLogInfo>)
            {
                var documentLog = log as JobWorkerLog<ExportMetadataLogInfo>;
                workerLog.IsErrorInField = documentLog.LogInfo.IsErrorInField;
                workerLog.IsErrorInNativeFile = documentLog.LogInfo.IsErrorInNativeFile;
                workerLog.IsErrorInImageFile = documentLog.LogInfo.IsErrorInImageFile;
                workerLog.IsErrorInTextFile = documentLog.LogInfo.IsErrorInTextFile;
                workerLog.IsErrorInTag = documentLog.LogInfo.IsErrorInTag;
                workerLog.IsErrorInComments = documentLog.LogInfo.IsErrorInComments;
                workerLog.LogDescription = documentLog.LogInfo.Information;
            }
            else if (log is JobWorkerLog<ExportFileCopyLogInfo>)
            {
                var documentLog = log as JobWorkerLog<ExportFileCopyLogInfo>;
                workerLog.IsErrorInNativeFile = documentLog.LogInfo.IsErrorInNativeFile;
                workerLog.IsErrorInImageFile = documentLog.LogInfo.IsErrorInImageFile;
                workerLog.IsErrorInTextFile = documentLog.LogInfo.IsErrorInTextFile;
                workerLog.LogDescription = documentLog.LogInfo.Information;
            }
            else if (log is JobWorkerLog<ExportLoadFileWritterLogInfo>)
            {
                var documentLog = log as JobWorkerLog<ExportLoadFileWritterLogInfo>;
                workerLog.IsErrorInNativeFile = documentLog.LogInfo.IsErrorInNativeFile;
                workerLog.IsErrorInImageFile = documentLog.LogInfo.IsErrorInImageFile;
                workerLog.IsErrorInTextFile = documentLog.LogInfo.IsErrorInTextFile;
                workerLog.LogDescription = documentLog.LogInfo.Information;
            }
            else if (log is JobWorkerLog<ExportStartupLogInfo>)
            {
                var documentLog = log as JobWorkerLog<ExportStartupLogInfo>;
                workerLog.LogDescription = documentLog.LogInfo.Information;
            }
            return workerLog;
        }

        public JobWorkerLogBEO ConstructNearDuplicationWorkerLog<T>(JobWorkerLog<T> log)
        {
            var workerLog = new JobWorkerLogBEO
            {
                JobRunId = log.JobRunId,
                CorrelationId = log.CorrelationId,
                WorkerInstanceId = log.WorkerInstanceId,
                WorkerRoleType = log.WorkerRoleType,
                IsError = !log.Success,
                IsWarning = log.IsMessage,
                CreatedBy = log.CreatedBy
            };
            if (log is JobWorkerLog<NearDuplicationLogInfo>)
            {
                var documentLog = log as JobWorkerLog<NearDuplicationLogInfo>;
                workerLog.IsMissingText = documentLog.LogInfo.IsMissingText;
                workerLog.IsFailureInDatabaseUpdate = documentLog.LogInfo.IsFailureInDatabaseUpdate;
                workerLog.IsFailureInSearchUpdate = documentLog.LogInfo.IsFailureInSearchUpdate;
                workerLog.LogDescription = documentLog.LogInfo.Information;
            }

            return workerLog;
        }

        public JobWorkerLogBEO ConstructBulkTagWorkerLog<T>(JobWorkerLog<T> log)
        {
            var workerLog = new JobWorkerLogBEO
            {
                JobRunId = log.JobRunId,
                CorrelationId = log.CorrelationId,
                WorkerInstanceId = log.WorkerInstanceId,
                WorkerRoleType = log.WorkerRoleType,
                IsError = !log.Success,
                IsWarning = log.IsMessage,
                CreatedBy = log.CreatedBy
            };
            if (log is JobWorkerLog<TagLogInfo>)
            {
                var documentLog = log as JobWorkerLog<TagLogInfo>;
                workerLog.IsFailureInDatabaseUpdate = documentLog.LogInfo.IsFailureInDatabaseUpdate;
                workerLog.IsFailureInSearchUpdate = documentLog.LogInfo.IsFailureInSearchUpdate;
                workerLog.LogDescription = documentLog.LogInfo.Information;
            }

            return workerLog;
        }

        public JobWorkerLogBEO ConstructLawSyncWorkerLog(JobWorkerLog<LawSyncLogInfo> log)
        {
            var workerLog = new JobWorkerLogBEO
                            {
                                JobRunId = log.JobRunId,
                                CorrelationId = log.CorrelationId,
                                WorkerInstanceId = log.WorkerInstanceId,
                                WorkerRoleType = log.WorkerRoleType,
                                IsError = !log.Success,
                                ErrorCode = log.ErrorCode,
                                IsWarning = log.IsMessage,
                                CreatedBy = log.CreatedBy,
                                CrossReferenceField = log.LogInfo.LawDocumentId.ToString(CultureInfo.InvariantCulture),
                                LogDescription = log.LogInfo.Information
                            };
            return workerLog;
        }
    }

    public enum LawSyncErrorCode
    {
        ImageConversionFailure=501,
        ImageSyncFail=502,
        MetadataSyncFail=503,
        DocumentNotAvailble=504
    }
}
