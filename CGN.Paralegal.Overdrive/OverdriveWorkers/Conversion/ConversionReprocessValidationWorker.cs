
#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="ConversionReprocessValidationWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Henry</author>
//      <description>
//          This file contains all the  methods related to  ConversionReprocessValidationWorker
//      </description>
//      <changelog>
//          <date value="05-15-2013">Initial: Reconversion Processing</date>
//          <date value="05-22-2013">Task # 142101 - Production ReProcessing Blocking Issue Fix</date>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="10/17/2013">Bug # 155220 &155337  - Fix to get the xdl for single page production with more than 16 pages and capture the xdl file missing and unknown conversion errors 
//          <date value="10/23/2013">Bug # 156607 - Fix to avoid infinite conversion validation for documents without heartbeat files by having absolute timeout
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.External.DataAccess;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.DBManagement;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Overdrive;
using System.IO;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using OverdriveWorkers.Data;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.EVContainer;

namespace LexisNexis.Evolution.Worker
{
    class ConversionReprocessValidationWorker : WorkerBase
    {
        private IDocumentVaultManager documentVaultMngr;
        private long matterId;
        private const string ConErrorOnRenameImage = "Failed to rename produced image file based on Bates number for document DCN: ";
        private TimeSpan documentConversionTimeout;
        private TimeSpan documentGlobalConversionTimeout;

        protected override void BeginWork()
        {
                base.BeginWork();
                documentConversionTimeout = ConversionHelper.GetDocumentConversionTimeout();
                documentGlobalConversionTimeout = ConversionHelper.GetDocumentGlobalConversionTimeout();
                documentVaultMngr = EVUnityContainer.Resolve<IDocumentVaultManager>("DocumentVaultManager");
            }
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
                if (message.Body is ConversionDocCollection)
                {
                    ValidateImportReprocessing(message);
                }
                else if(message.Body is List<ProductionDocumentDetail>)
                {
                    ValidateProdctionReprocessing(message);
                }
            }


        /// <summary>
        /// Validate Import Conversion Reprocessing
        /// </summary>
        /// <param name="message">Input message</param>
        private void ValidateImportReprocessing(PipeMessageEnvelope message)
        {
            var docCollection = message.Body as ConversionDocCollection;
            if (docCollection == null || docCollection.Documents == null || !docCollection.Documents.Any())
                //no document to validate
            {
                return;
            }

            var succeededDocs = new List<ReconversionDocumentBEO>();
            var notReadyDocs = new List<ReconversionDocumentBEO>();
            var failedDocs = new List<ReconversionDocumentBEO>();

            foreach (var document in docCollection.Documents)
            {
                try
                {
                    //number of calls to redactIt with the same DCNNumber, which all share the same heartbeat file
                    int callCount = docCollection.Documents.Count(x => x.DCNNumber == document.DCNNumber);
                    string docHbFilename = docCollection.GetDefaultHeartbeatFileFullPath(document);
                    RedactItHeartbeatWatcher.DocumentStatus documentStatus =
                        RedactItHeartbeatWatcher.CheckDocumentState(docHbFilename, callCount);

                    switch (documentStatus.DocumentState)
                    {
                        case RedactItHeartbeatWatcher.DocumentStateEnum.NotFound:
                             if (!document.ConversionEnqueueTime.HasValue)
                            {
                                document.ConversionEnqueueTime = DateTime.UtcNow;
                            }
                             if (DateTime.UtcNow - document.ConversionEnqueueTime > documentGlobalConversionTimeout)
                            {
                                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                                documentStatus.ErrorMessage = "Global document conversion timeout.";
                                Tracer.Trace(
                                    "Global Conversion Timeout For Document with document id - {0} ,dcn -{1},collection id - {2} , matter id - {3} and Timeout value {4} ",
                                    document.DocumentId, document.DCNNumber, document.CollectionId, matterId,documentGlobalConversionTimeout);
                                goto case RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                            }
                           
                            notReadyDocs.Add(document);
                            break;
                        case RedactItHeartbeatWatcher.DocumentStateEnum.NotReady:
                            if (!document.ConversionStartTime.HasValue)
                            {
                                document.ConversionStartTime = DateTime.UtcNow;
                            }
                            if (DateTime.UtcNow - document.ConversionStartTime > documentConversionTimeout)
                            {
                                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                                documentStatus.ErrorMessage = "Document conversion timeout.";
                                Tracer.Trace(
                                    "Conversion Timeout For Document with document id - {0} ,dcn -{1},collection id - {2}  matter id - {3} and Timeout value {4} ",
                                    document.DocumentId, document.DCNNumber, document.CollectionId, matterId,documentConversionTimeout);
                                goto case RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                            }
                            notReadyDocs.Add(document);
                            break;
                        case RedactItHeartbeatWatcher.DocumentStateEnum.Success:
                            succeededDocs.Add(document);
                            break;
                        case RedactItHeartbeatWatcher.DocumentStateEnum.Failure:
                            //ToDo:For now we don't have job log for conversion reprocess job
                            string error = "Redact-It reported error converting document DCN: " +
                                           document.DCNNumber + ". Heartbeat error: " + documentStatus.ErrorMessage +
                                           ". Refer heartbeat file " +
                                           docHbFilename + " for more info.";
                            document.ConversionError = error;
                            failedDocs.Add(document);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ex.AddResMsg(
                        "Problem in validating the conversion for the document document id - {0} ,dcn -{1},collection id - {2} and matter id - {3}",
                        document.DocumentId, document.DCNNumber, document.CollectionId, matterId);
                    ReportToDirector(ex);
                    ex.Trace().Swallow();
                    failedDocs.Add(document);
                }
            }

            if (notReadyDocs.Any())
            {
                docCollection.Documents = notReadyDocs;
                InputDataPipe.Send(new PipeMessageEnvelope {Body = docCollection, IsPostback = true});
            }

            if (failedDocs.Any())
                IncreaseProcessedDocumentsCount(failedDocs.Count);
            //ProcessConversionResults(failedDocs, docCollection, false);

            if (succeededDocs.Any())
            {
                IncreaseProcessedDocumentsCount(succeededDocs.Count);
                //ProcessConversionResults(succeededDocs, docCollection, true);
            }
        }

        /// <summary>
        /// Validate production Conversion Reprocessing
        /// </summary>
        /// <param name="message">Input message</param>
        private void ValidateProdctionReprocessing(PipeMessageEnvelope message)
        {
            var docList = message.Body as List<ProductionDocumentDetail>;
            if (docList == null || docList.Count == 0)
            //no document to validate
            {
                return;
            }

            var succeededDocs = new List<ProductionDocumentDetail>();
            var notReadyDocs = new List<ProductionDocumentDetail>();
            var failedDocs = new List<ProductionDocumentDetail>();
            matterId = Convert.ToInt64(docList[0].MatterId);
            matterId.ShouldBeGreaterThan(0);
            var documentConversionLogBeos = new List<DocumentConversionLogBeo>();
            foreach (var document in docList)
            {
                try
                {
                document.ConversionCheckCounter++;

                //production heartbeat file expect 2 calls for each conversion
                RedactItHeartbeatWatcher.DocumentStatus documentStatus = RedactItHeartbeatWatcher.CheckDocumentState(document.HeartBeatFile, 2);

                switch (documentStatus.DocumentState)
                {
                    case RedactItHeartbeatWatcher.DocumentStateEnum.NotFound:
                         if (!document.ConversionEnqueueTime.HasValue)
                            {
                                document.ConversionEnqueueTime = DateTime.UtcNow;
                            }
                             if (DateTime.UtcNow - document.ConversionEnqueueTime > documentGlobalConversionTimeout)
                            {
                                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                                documentStatus.ErrorMessage = "Document conversion timeout.";
                                Tracer.Trace(
                                    "Global Conversion Timeout For Document with document id - {0} ,dcn -{1},collection id - {2} and matter id - {3} ",
                                    document.DocumentId, document.DCNNumber, document.OriginalCollectionId, matterId);
                                goto case RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                            }
                           
                        notReadyDocs.Add(document);
                        break;
                    case RedactItHeartbeatWatcher.DocumentStateEnum.NotReady:
                        if (!document.ConversionStartTime.HasValue)
                        {
                            document.ConversionStartTime = DateTime.UtcNow;
                        }
                        if (DateTime.UtcNow - document.ConversionStartTime > documentConversionTimeout)
                        {
                            documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                            documentStatus.ErrorMessage = "Document conversion timeout. Heartbeat file path: " + document.HeartBeatFile;
                            Tracer.Trace(
                                "Conversion Timeout For Document with  document id - {0} ,dcn-{1},collection id - {2} and matter id - {3} ",
                                document.DocumentId,document.DCNNumber, document.OriginalCollectionId, matterId);
                            goto case RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                        }
                           notReadyDocs.Add(document);
                        break;
                    case RedactItHeartbeatWatcher.DocumentStateEnum.Success:
                        succeededDocs.Add(document);
                        SafeDeleteFolder(document.SourceDestinationPath); //Delete all the source file for the document
                        documentConversionLogBeos.Add(ConvertToDocumentConversionLogBeo(document, 2));
                        SafeDeleteFile(document.HeartBeatFile);
                        RenameImagesBasedOnBatesNumber(document);
                        break;
                    case RedactItHeartbeatWatcher.DocumentStateEnum.Failure:
                        failedDocs.Add(document);
                        if (string.IsNullOrEmpty(documentStatus.ErrorReason))
                        {
                            documentStatus.ErrorReason = EVRedactItErrorCodes.UnKnownConversionFailure;
                        }
                        documentConversionLogBeos.Add(ConvertToDocumentConversionLogBeo(document, 3, documentStatus.ErrorReason, documentStatus.ErrorMessage));
                        SafeDeleteFolder(document.SourceDestinationPath); //Delete all the source file for the document

                        string error = "Redact-It reported error converting document DCN: " +
                            document.DCNNumber + ". Heartbeat error: " + documentStatus.ErrorMessage + ". Refer heartbeat file " +
                            document.HeartBeatFile + " for more info.";
                        RenameImagesBasedOnBatesNumber(document); //Handle partially converted documents
                        LogMessage(document, false, error);
                        break;
                }
            }
                catch (Exception ex)
                {
                    ReportToDirector(ex);
                    ex.Trace().Swallow();
                    //LogMessage(document, false, ex.ToUserString());
                }
            }

            if (notReadyDocs.Any())
            {
                InputDataPipe.Send(new PipeMessageEnvelope { Body = notReadyDocs, IsPostback = true });
            }

            if (failedDocs.Any())
            {
                IncreaseProcessedDocumentsCount(failedDocs.Count);
            }

            if (succeededDocs.Any())
            {
                IncreaseProcessedDocumentsCount(succeededDocs.Count);
            }
            BulkUpdateProcessSetStatus(documentConversionLogBeos);
        }

        /// <summary>
        /// Re name produced image(s) based on Bates Number
        /// </summary>
        private void RenameImagesBasedOnBatesNumber(ProductionDocumentDetail document)
        {
            try
            {
                if (!document.Profile.IsOneImagePerPage || String.IsNullOrEmpty(document.StartingBatesNumber)) return;
                if (string.IsNullOrEmpty(document.Profile.ProductionPrefix) ||
                    string.IsNullOrEmpty(document.Profile.ProductionStartingNumber))
                    return;
                var productionConversionHelper = new ProductionConversionHelper();
                var producedImages =
                    productionConversionHelper.RenameProducedImages(document);
                productionConversionHelper.UpdateProducedImageFilePath(
                    document.DocumentId, document.ProductionCollectionId,
                    matterId, producedImages, document.CreatedBy);
            }
            catch (Exception exception)
            {
                //Log the error message for failed documents
                var message = ConErrorOnRenameImage + document.DCNNumber;
                LogMessage(document, false, message);
                //continue the production process with out rename images
                exception.Trace().Swallow();
            }
        }

        /// <summary>
        /// Bulks the update process set status.
        /// </summary>
        /// <param name="documentConversionLogBeos">The document conversion log beos.</param>
        private void BulkUpdateProcessSetStatus(IList<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {
                if (!documentConversionLogBeos.Any()) return;
                documentVaultMngr.BulkUpdateConversionLogs(matterId, documentConversionLogBeos);
            }
            catch (Exception exception)
            {
                //continue the production process with out updating the conversion /process status
                exception.Trace().Swallow();
            }
        }
        /// <summary>
        /// Converts the specified production document detail.
        /// </summary>
        /// <param name="productionDocumentDetail">The production document detail.</param>
        /// <param name="processStatus">The process status.</param>
        /// <param name="errorReason">The error reason.</param>
        /// <param name="errorDetails">The error details.</param>
        /// <returns></returns>
        private DocumentConversionLogBeo ConvertToDocumentConversionLogBeo(ProductionDocumentDetail productionDocumentDetail, byte processStatus, string errorReason = null, string errorDetails = null)
        {
            var documentConversionLogBeo = new DocumentConversionLogBeo
            {
                JobRunId = WorkAssignment.JobId,
                ProcessJobId = WorkAssignment.JobId,
                Status = processStatus,
                ErrorReason = errorReason,
                CollectionId = productionDocumentDetail.OriginalCollectionId,
                DocumentId = productionDocumentDetail.DocumentId,
                ModifiedDate = DateTime.UtcNow,
                ErrorDetails = errorDetails
            };
            return documentConversionLogBeo;

        }

        /// <summary>
        /// Delete the folder.
        /// </summary>
        /// <param name="folderToCleanUp">Folder to clean up</param>
        /// <returns>Success or failure</returns>
        private void SafeDeleteFolder(string folderToCleanUp)
        {
            //Clean up the folder once atomic work is completed
            try
            {
                if (Directory.Exists(folderToCleanUp))
                {
                    Directory.Delete(folderToCleanUp, true);
                }
            }
            catch (Exception ex)
            {
                ex.Data["FolderToCleanup"] = folderToCleanUp;
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Safes the delete file.
        /// </summary>
        /// <param name="filePath">The STR hearbeat file.</param>
        private void SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath)) //delete given file if exists
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                ex.Data["FileToCleanup"] = filePath;
                ReportToDirector(ex);
            }
        }


        private void LogMessage(Object documentDetail, bool success, string message)
        {
            const string roleId = "CE73BFB1-94FB-4953-8AF8-B53BE828AD11";
            if (documentDetail is ProductionDocumentDetail)
            {
                var doc = (ProductionDocumentDetail)documentDetail;
                var log = new List<JobWorkerLog<ProductionParserLogInfo>>();
                var parserLog = new JobWorkerLog<ProductionParserLogInfo>
                {
                    JobRunId =
                        (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                    CorrelationId = doc.CorrelationId,
                    WorkerRoleType = roleId,
                    WorkerInstanceId = WorkerId,
                    IsMessage = false,
                    Success = success,
                    CreatedBy = doc.CreatedBy,
                    LogInfo =
                        new ProductionParserLogInfo
                        {
                            Information = message,
                            BatesNumber = doc.AllBates,
                            DatasetName = doc.OriginalDatasetName,
                            DCN = doc.DCNNumber,
                            ProductionDocumentNumber = doc.DocumentProductionNumber,
                            ProductionName = doc.Profile.ProfileName
                        }
                };

                log.Add(parserLog);
                SendLog(log);
            }
        }

        private void SendLog(List<JobWorkerLog<ProductionParserLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// End of the work. process
        /// Update the job's boot parameter with total number of document processed and number of failed documents
        /// </summary>
        protected override void EndWork()
        {
            var bootParam = GetBootObject<ConversionReprocessJobBeo>(BootParameters);

            //the corresponding job id
            int jobId = WorkAssignment.JobId;

            //get various count
            DatasetBEO ds = DataSetBO.GetDataSetDetailForDataSetId(bootParam.DatasetId);
            long matterId = ds.Matter.FolderID;

			var vault = VaultRepository.CreateRepository(matterId);

            int totalDocCount ;
            int failedDocCount ;
            int succeedDocCount ;

            vault.GetReconversionDocStatusCount(jobId, out totalDocCount, out succeedDocCount,
                                                          out failedDocCount);

            bootParam.TotalDocCount = totalDocCount;
            bootParam.FailedDocCount = failedDocCount;
            //bootParam.SucceedDocCount = succeedDocCount;


            //re serialize the boot param
            string newBootParam;

            var serializer = new XmlSerializer(typeof(ConversionReprocessJobBeo));

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, bootParam);

                newBootParam = writer.ToString();
            }

            //update the boot parameters for the job
            ReconversionDAO.UpdateReconversionBootParamter(jobId,newBootParam);

            //clean up reconversion input file that contain the list of document to convert
            SafeDeleteFile(bootParam.FilePath);

        }

        /// <summary>
        /// This method deserializes and determine the Xml T typed job info object
        /// </summary>
        /// <param name="bootParameter"></param>

        private T GetBootObject<T>(string bootParameter)
        {
            if (bootParameter == null) throw new Exception("bootparamter can not be null");

            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParameter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(T));

            //Deserialization of bootparameter to get ImportBEO
            return (T)xmlStream.Deserialize(stream);
        }

    }
}
