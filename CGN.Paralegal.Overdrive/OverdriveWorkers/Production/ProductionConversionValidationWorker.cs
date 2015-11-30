
#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="ProductionConversionValiation.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Prabhu/Konstatin</author>
//      <description>
//          This file contains all unit test cases related to production conversion validation worker 
//      </description>
//      <changelog>
//      <date value="05/03/2012">Task #100232 </date>
//      <date value="04/17/2013">Task #135044 - Bates And Dpn in DDV and table view for conversion failed documenets </date>
//      <date value="05-12-2013">Task # 134432-ADM 03 -Re Convresion</date>
//      <date value="05-22-2013">Task # 142101 - Production ReProcessing Blocking Issue Fix</date>
//      <date value="07-03-2013">Bug # 146561 and 145022 - Fix to show  all the documents are listing out in production manage conversion screen</date>
//      <date value="08/06/2013">Bug Fix # 147838 </date>
//      <date value="10/17/2013">Bug # 155220 &155337  - Fix to get the xdl for single page production with more than 16 pages and capture the xdl file missing and unknown conversion errors 
//          <date value="10/23/2013">Bug  # 154585 -ADM -ADMIN - 006 - Fix to avoid forever  conversion validation and blocking behaviour for document conversion time out
//          <date value="10/25/2013">Dev Bug # 155931 -Fix to mark not found documents as not ready documents in production conversion validation
//          <date value="10/23/2013">Bug # 156607 - Fix to avoid infinite conversion validation for documents without heartbeat files by having absolute timeout
//          <date value="06/1/2013">Bug # 156607 - Making timeout as a warning 
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion Header
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Overdrive;
using System.IO;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Business.AuditManagement;

namespace LexisNexis.Evolution.Worker
{

    public class ProductionConversionValidationWorker : WorkerBase
    {
        private const string TimeoutError = "document conversion validation timeout warning";
        private IDocumentVaultManager documentVaultMngr;
        private long _matterId;
        private const string ConErrorOnRenameImage = "Failed to rename produced image file based on Bates number for document DCN: ";
        private TimeSpan documentConversionTimeout;
        private TimeSpan documentConverisonGlobalTimeout;
        private ProductionDetailsBEO m_BootParameters;
        private int collectionId = 0;
        private IDataSetVaultManager m_DatasetVaultManager = null;
        private string datasetCollectionGuid = null;


        protected override void BeginWork()
        {

            base.BeginWork();
            documentConversionTimeout = ConversionHelper.GetDocumentConversionTimeout();
            documentConverisonGlobalTimeout = ConversionHelper.GetDocumentGlobalConversionTimeout();
            documentVaultMngr = EVUnityContainer.Resolve<IDocumentVaultManager>("DocumentVaultManager"); 
            m_DatasetVaultManager = new DataSetVaultManager();
            m_BootParameters = Utils.Deserialize<ProductionDetailsBEO>(BootParameters);
       }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            try
            {
                var documentIdentifierEntityBeos =
                    new List<DocumentIdentifierEntityBEO>();
                var productionDocuments = envelope.Body as List<ProductionDocumentDetail>;
                if (productionDocuments == null || productionDocuments.Count == 0)
                {
                    return;
                }
                ProductionDocumentDetail productionDocument = productionDocuments.First();
                if (productionDocument == null) return;
                productionDocument.MatterId.ShouldNotBeEmpty();
                long.TryParse(productionDocument.MatterId, out _matterId);
                _matterId.ShouldBeGreaterThan(0);

                productionDocument.dataSetBeo.ShouldNotBe(null);
                productionDocument.dataSetBeo.CollectionId.ShouldNotBeEmpty();
                datasetCollectionGuid = productionDocument.dataSetBeo.CollectionId;


                var ready = new List<ProductionDocumentDetail>();
                var notReady = new List<ProductionDocumentDetail>();
                var documentConversionLogBeos = new List<DocumentConversionLogBeo>();

                foreach (var productionDocumentDetail in productionDocuments)
                {
                    var documentStatus =
                        RedactItHeartbeatWatcher.CheckDocumentState(productionDocumentDetail.HeartBeatFile, 2);

                    switch (documentStatus.DocumentState)
                    {
                        case RedactItHeartbeatWatcher.DocumentStateEnum.NotFound:

                            if (!productionDocumentDetail.ConversionEnqueueTime.HasValue)
                            {
                                productionDocumentDetail.ConversionEnqueueTime = DateTime.UtcNow;
                            }
                            //the document conversion global timeout is the maximum waiting  time for  a document to be  converted
                            //Below are the possible reasons for this kind of timeout 
                            //1)heart beat file is not generated for this documents
                            //2)document is waiting in queue then the expected global or waiting timeout configured in the CCMS
                            if (DateTime.UtcNow - productionDocumentDetail.ConversionEnqueueTime >
                                documentConverisonGlobalTimeout)
                            {
                                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                                documentStatus.ErrorReason = "RedactIt heartbeat file creation timeout warning";
                               
                                Tracer.Trace(
                                    "Global Conversion Timeout For Document with document id - {0} ,dcn {1},collection id - {2} , matter id - {3} , HeartBeatFile {4} and Timeout Value {5}",
                                    productionDocumentDetail.DocumentId, productionDocumentDetail.DCNNumber,
                                    productionDocumentDetail.OriginalCollectionId, _matterId,
                                    productionDocumentDetail.HeartBeatFile, documentConverisonGlobalTimeout);
                                goto case RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                            }
                            notReady.Add(productionDocumentDetail);
                            break;

                        case RedactItHeartbeatWatcher.DocumentStateEnum.NotReady:
                            if (!productionDocumentDetail.ConversionStartTime.HasValue)
                            {
                                productionDocumentDetail.ConversionStartTime = DateTime.UtcNow;
                            }
                            if (DateTime.UtcNow - productionDocumentDetail.ConversionStartTime >
                                documentConversionTimeout)
                            {
                                documentStatus.DocumentState = RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                                documentStatus.ErrorMessage=documentStatus.ErrorReason = "Document conversion validation timeout warning"; 
                                Tracer.Trace(
                                    " Conversion Timeout For Document with document id - {0} ,dcn {1},collection id - {2} , matter id - {3} , HeartBeatFile {4} and Timeout Value {5}",
                                    productionDocumentDetail.DocumentId, productionDocumentDetail.DCNNumber,
                                    productionDocumentDetail.OriginalCollectionId, _matterId,
                                    productionDocumentDetail.HeartBeatFile, documentConversionTimeout);
                                goto case RedactItHeartbeatWatcher.DocumentStateEnum.Failure;
                            }
                            notReady.Add(productionDocumentDetail);
                            break;
                        case RedactItHeartbeatWatcher.DocumentStateEnum.Success:
                            ready.Add(productionDocumentDetail);
                            SafeDeleteFolder(productionDocumentDetail.SourceDestinationPath);
                            SafeDeleteFile(productionDocumentDetail.HeartBeatFile);
                            documentConversionLogBeos.Add(ConvertToDocumentConversionLogBeo(productionDocumentDetail,
                                EVRedactItErrorCodes.
                                    Completed));
                            var documentIdentifierEntity = ConstructAuditLog(productionDocumentDetail);
                            documentIdentifierEntityBeos.Add(documentIdentifierEntity);
                            RenameImagesBasedOnBatesNumber(productionDocumentDetail);
                            //Delete all the source file for the document
                            // Heart beat file and source file are deleted by callback in Production Notify service
                            break;
                        case RedactItHeartbeatWatcher.DocumentStateEnum.Failure:
                            SafeDeleteFolder(productionDocumentDetail.SourceDestinationPath);
                            //Delete all the source file for the document
                            string message = "Redact-It reported error converting document DCN: " +
                                             productionDocumentDetail.DCNNumber + ". Heartbeat error: " +
                                             documentStatus.ErrorMessage + ". Refer heartbeat file " +
                                             productionDocumentDetail.HeartBeatFile + " for more info.";
                            if (string.IsNullOrEmpty(documentStatus.ErrorReason))
                            {
                                documentStatus.ErrorReason = EVRedactItErrorCodes.UnKnownConversionFailure;
                            }
                            documentConversionLogBeos.Add(ConvertToDocumentConversionLogBeo(productionDocumentDetail,
                                EVRedactItErrorCodes.Failed,
                                documentStatus.ErrorReason,
                                documentStatus.ErrorMessage));

                            if (message.ToLower().Contains(TimeoutError))
                                LogMessage(productionDocumentDetail, true, message, true);
                            else
                                LogMessage(productionDocumentDetail, false, message, false);
                            RenameImagesBasedOnBatesNumber(productionDocumentDetail);
                            //Handle partially converted documents
                            IncreaseProcessedDocumentsCount(1); // Failed document counted as processed
                            break;
                    }
                }

                if (notReady.Any())
                {
                    InputDataPipe.Send(new PipeMessageEnvelope() {Body = notReady, IsPostback = true});
                }

                BulkUpdateProcessSetStatus(documentConversionLogBeos);
                if (m_BootParameters != null &&
                    m_BootParameters.Profile != null &&
                    !string.IsNullOrEmpty(m_BootParameters.Profile.ProductionSetName) &&
                    documentIdentifierEntityBeos.Count > 0)
                    DoAuditLog(documentIdentifierEntityBeos, m_BootParameters.Profile.ProductionSetName);
                IncreaseProcessedDocumentsCount(ready.Count);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Re name produced image(s) based on Bates Number
        /// </summary>
        private void RenameImagesBasedOnBatesNumber(ProductionDocumentDetail productionDocumentDetail)
        {
            try
            {
                if (!productionDocumentDetail.Profile.IsOneImagePerPage ||
                    String.IsNullOrEmpty(productionDocumentDetail.StartingBatesNumber)) return;
                if (string.IsNullOrEmpty(productionDocumentDetail.Profile.ProductionPrefix) ||
                    string.IsNullOrEmpty(productionDocumentDetail.Profile.ProductionStartingNumber))
                    return;
                var productionConversionHelper = new ProductionConversionHelper();
                var producedImages =
                    productionConversionHelper.RenameProducedImages(productionDocumentDetail);
                if (!producedImages.Any()) return;
                productionConversionHelper.UpdateProducedImageFilePath(
                    productionDocumentDetail.DocumentId, productionDocumentDetail.ProductionCollectionId,
                    _matterId, producedImages, productionDocumentDetail.CreatedBy);
            }
            catch (Exception exception)
            {
                //Log the error message for failed documents
                var message = ConErrorOnRenameImage + productionDocumentDetail.DCNNumber;
                LogMessage(productionDocumentDetail, false, message, false);
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
                using (var transScope = new EVTransactionScope(TransactionScopeOption.Required))
                {
                    if (!documentConversionLogBeos.Any()) return;
                    documentVaultMngr.BulkUpdateConversionLogs(_matterId, documentConversionLogBeos);
                    transScope.Complete();
                }
            }
            catch (Exception exception)
            {
                //continue the production process with out updating the conversion /process status
                exception.Trace().Swallow();
            }
        }

        /// <summary>
        /// Constructs the audit log.
        /// </summary>
        /// <param name="productionDocumentDetail">The production document detail.</param>
        /// <returns>DocumentIdentifierEntityBEO</returns>
        private DocumentIdentifierEntityBEO ConstructAuditLog(ProductionDocumentDetail productionDocumentDetail)
        {

            if (collectionId == 0)
            {
                datasetCollectionGuid.ShouldNotBeEmpty();
                collectionId = m_DatasetVaultManager.GetCollectionId(_matterId, new Guid(datasetCollectionGuid));
            }

            var documentIdentifierEntityBeo = new DocumentIdentifierEntityBEO();

            if (!string.IsNullOrEmpty(productionDocumentDetail.OriginalCollectionId))
            {
                documentIdentifierEntityBeo.CollectionId = productionDocumentDetail.OriginalCollectionId;
            }
            if (!string.IsNullOrEmpty(productionDocumentDetail.DCNNumber))
            {
                documentIdentifierEntityBeo.Dcn = productionDocumentDetail.DCNNumber;
            }
            if (!string.IsNullOrEmpty(productionDocumentDetail.OriginalDatasetName))
            {
                documentIdentifierEntityBeo.CollectionName = productionDocumentDetail.OriginalDatasetName;
            }

            documentIdentifierEntityBeo.ParentId = collectionId;
            documentIdentifierEntityBeo.DocumentReferenceId =
                productionDocumentDetail.OriginalDocumentReferenceId;
            return documentIdentifierEntityBeo;


        }
        /// <summary>
        /// Does the audit log.
        /// </summary>
        /// <param name="documentIdentifierEntityBeos">The document identifier entity beos.</param>
        /// <param name="productionName">Name of the production.</param>
        private void DoAuditLog(List<DocumentIdentifierEntityBEO> documentIdentifierEntityBeos, string productionName)
        {
            try
            {
                if(documentIdentifierEntityBeos==null ||!documentIdentifierEntityBeos.Any()) return;
                productionName.ShouldNotBeEmpty();
                Utility.SetUserSession(m_BootParameters.CreatedBy);
                AuditBO.LogDocumentProduced(_matterId, documentIdentifierEntityBeos, productionName);
            }
            catch (Exception ex)
            {
                //putting try catch to avoid the case of not sending documents to validation worker where required operations for reprocessing is done 
                ex.AddDbgMsg("Unable to update audit logs");
                ex.Trace().Swallow();
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
        /// <param name="strHearbeatFile">The STR hearbeat file.</param>
        private static void SafeDeleteFile(string strHearbeatFile)
        {
            try
            {
                if (File.Exists(strHearbeatFile)) //Delete the hearbeat file for the document
                {
                    File.Delete(strHearbeatFile);
                }
            }
            catch (Exception ex)
            {
                ex.Data["FileToCleanup"] = strHearbeatFile;
                Tracer.Info(ex.ToDebugString());
            }
        }

        private void LogMessage(ProductionDocumentDetail documentDetail, bool success, string message, bool isMessage)
        {
            const string roleId = "prod0fc6-113e-4217-9863-ec58c3f7yz89";
            var log = new List<JobWorkerLog<ProductionParserLogInfo>>();
            var parserLog = new JobWorkerLog<ProductionParserLogInfo>
            {
                JobRunId =
                    (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = documentDetail.CorrelationId,
                WorkerRoleType = roleId,
                WorkerInstanceId = WorkerId,
                IsMessage = isMessage,
                Success = success,
                CreatedBy = documentDetail.CreatedBy,
                LogInfo =
                    new ProductionParserLogInfo
                    {
                        Information = message,
                        BatesNumber = documentDetail.AllBates,
                        DatasetName = documentDetail.OriginalDatasetName,
                        DCN = documentDetail.DCNNumber,
                        ProductionDocumentNumber = documentDetail.DocumentProductionNumber,
                        ProductionName = documentDetail.Profile.ProfileName
                    }
            };

            log.Add(parserLog);
            SendLog(log);
        }

        private void SendLog(List<JobWorkerLog<ProductionParserLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }
    }
}
