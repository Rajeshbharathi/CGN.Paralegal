#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionReprocessStartupWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Henry</author>
//      <description>
//          This file contains all the  methods related to  ConversionReprocessStartupWorker
//      </description>
//      <changelog>
//          <date value="05-15-2013">Initial: Reconversion Processing</date>
//          <date value="05-21-2013">Bug # 142937,143536 and 143037 -ReConvers Buddy Defects</date>
//          <date value="05-22-2013">Task # 142101 - Production ReProcessing Blocking Issue Fix</date>
//          <date value="07-03-2013">Bug # 146561 and 145022 - Fix to show  all the documents are listing out in production manage conversion screen</date>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="10/07/2013">Dev Bug  # 154336 -ADM -ADMIN - 006 - Import /Production Reprocessing reprocess all documents even with filter and all and other migration fixes
//          <date value="10/28/2013">Dev Bug  # 155810 -ADM -ADMIN - 006 - Fix to reprocess only filtered documents for production
//          <date value="10/01/2015">Making sure that reprocessing passes right conversion priority to IGC</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespace
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.External.DataAccess;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using OverdriveWorkers.Data;

#endregion

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// This class represents the startup worker
    /// </summary>
    public class ConversionReprocessStartupWorker : WorkerBase
    {

        public ConversionReprocessJobBeo BootObject { get; set; }

        private int BatchSize = 100;
        private IDocumentVaultManager vaultManager = null;
        /// <summary>
        /// Begins the work.
        /// Absorb the boot parameters, deserialize and pass on the messages to the Search Worker
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();
            try
            {
                // Deserialize and determine the boot object
                BootObject = GetBootObject<ConversionReprocessJobBeo>(BootParameters);
                BootObject.ShouldNotBe(null);
                vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);       
            }
            catch (ApplicationException apEx)
            {
                LogMessage(false, apEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                LogMessage(false, "ReconversionStartup worker failed: "+ ex.Message);
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
                if (string.IsNullOrEmpty(PipelineId)) throw new Exception("PipelineId is null or empty");

                ConversionDocCollection documentCollection = GetReconversionDocCollection();

                //nothing to process, return
                if (documentCollection == null || documentCollection.Documents == null)
                    return true;

                SendMessage(documentCollection);

                IncreaseProcessedDocumentsCount(documentCollection.Documents.Count());
            }
            catch (Exception ex)
            {
                LogMessage(false, "Reconversion Startup: failed to get list of document and send to next processing: "+ex.Message);
                ex.Trace();
            }
            return true;
        }

        #region Private helper functions

        /// <summary>
        /// Get document collection for reconversion
        /// </summary>
        /// <returns></returns>
        /// 
        public ConversionDocCollection GetReconversionDocCollection( )
        {
            var docs = new ConversionDocCollection();

            //collectionid to be used in reconversion
            string collectionId = "";

            //populate job info
            docs.JobConfig = BootObject;
          
            BaseJobBEO baseConfig = ReconversionDAO.GetJobConfigInfo(Convert.ToInt32(BootObject.OrginialJobId));
            docs.BaseJobTypeId = baseConfig.JobTypeId;

            //different type of base job has different object to hold job config info
            if (baseConfig.JobTypeId == 9) // Base job is production job
            {
                docs.BaseJobConfig = GetBootObject<ProductionDetailsBEO>(baseConfig.BootParameters);
                //for production reconversion, the collection id is the production Set collectionId, which is the collectionId in job parameter
                collectionId = ((ProductionDetailsBEO) docs.BaseJobConfig).OriginalCollectionId;  //this is the native set collectionId
         

                //dataset associate with the document set
                docs.DataSet = DataSetBO.GetDataSetDetailForCollectionId(collectionId);

                //matterid associate with the document set
                long matterId = docs.DataSet.Matter.FolderID;

                //get the list of production document list to be reprocessed
                var helper = new ConversionReprocessStartupHelper();
                IEnumerable<ReconversionProductionDocumentBEO> pDocs = helper.GetProductionDocumentList(
                    BootObject.FilePath, BootObject.JobSelectionMode,matterId, 
                    docs.BaseJobConfig as ProductionDetailsBEO,docs.DataSet.RedactableDocumentSetId,
                    Convert.ToInt32(BootObject.OrginialJobId),BootObject.Filters);

                //cast back to parent list of parent class
                if(pDocs != null && pDocs.Any())
                    docs.Documents = pDocs.Cast<ReconversionDocumentBEO>().ToList();

            }else {
                if (baseConfig.JobTypeId == 14) //load file import
                {
                    docs.BaseJobConfig = GetBootObject<ImportBEO>(baseConfig.BootParameters);
                        //for import reconversion, the collection id is the native document set collectionId
                    collectionId = ((ImportBEO) docs.BaseJobConfig).CollectionId;
                 
                }
                else if (baseConfig.JobTypeId == 2 || baseConfig.JobTypeId == 8) //DCB import and  Edoc Import
                {
                    docs.BaseJobConfig = GetBootObject<ProfileBEO>(baseConfig.BootParameters);
                    //for import reconversion, the collection id is the native document set collectionId
                    collectionId = ((ProfileBEO) docs.BaseJobConfig).DatasetDetails.CollectionId;
                  
                }else if (baseConfig.JobTypeId == 35) //Law import
                {
                    docs.BaseJobConfig = GetBootObject<LawImportBEO>(baseConfig.BootParameters);
                    //for import reconversion, the collection id is the native document set collectionId
                    collectionId = ((LawImportBEO)docs.BaseJobConfig).CollectionId;
                  
                }

                //dataset associate with the document set
                docs.DataSet = DataSetBO.GetDataSetDetailForCollectionId(collectionId);

                //assign heartbeat file path, if directory not exists, create it.
                docs.HeartbeatFilePath = docs.DataSet.CompressedFileExtractionLocation + ApplicationConfigurationManager.GetValue("ReconversionHeartbeatFileFolder", "Imports") + PipelineId;
                if (!Directory.Exists(docs.HeartbeatFilePath))
                {
                    Directory.CreateDirectory(docs.HeartbeatFilePath);
                }

                //matterid associate with the document set
                long matterId = docs.DataSet.Matter.FolderID;

                docs.Documents = ConversionReprocessStartupHelper.GetImportDocumentList(
                    BootObject.FilePath, BootObject.JobSelectionMode, matterId, docs.DataSet.FolderID, BootObject.OrginialJobId,BootObject.Filters);
            }

           
            return docs;

        }



        /// <summary>
        /// This method deserializes and determine the Xml T typed job info object
        /// </summary>
        /// <param name="bootParameter"></param>

        private T GetBootObject<T>(string bootParameter)
        {
            if(bootParameter == null) throw new Exception("bootparamter can not be null");

            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParameter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(T));

            //Deserialization of bootparameter to get ImportBEO
            return (T)xmlStream.Deserialize(stream);
        }


        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="docCollection">The document batch.</param>
        private void SendMessage(ConversionDocCollection docCollection)
        {
            if (docCollection.BaseJobTypeId == 2 || docCollection.BaseJobTypeId == 8
                ||docCollection.BaseJobTypeId == 14 ||docCollection.BaseJobTypeId == 35 ) //import
            {
                var docBatch = new List<ReconversionDocumentBEO>();
                foreach (var doc in docCollection.Documents)
                {
                    docBatch.Add(doc);
                    if (docBatch.Count == BatchSize)
                    {
                        SendImportReconversionBatch(docBatch, docCollection);
                        docBatch = new List<ReconversionDocumentBEO>();
                    }
                }

                if (docBatch.Count != BatchSize) //the last batch is not a full batch and so not sent yet. need to send here
                    SendImportReconversionBatch(docBatch, docCollection);

            }
            else if (docCollection.BaseJobTypeId == 9) //production reconversion, jump to production preprocessing
            {
                var docList = ConversionReprocessStartupHelper.ConvertToProductionDocumentList(docCollection);

                if (docList != null && docList.Count > 0)
                {
                    var pBatch = new List<ProductionDocumentDetail>();
                    int count = 0;
                    foreach (var doc in docList)
                    {
                        count++;
                        pBatch.Add(doc);
                        if (count%BatchSize == 0 || count == docList.Count)
                        {
                            BulkUpdateProductionReprocessingState(docCollection, pBatch);
                            SendProductionReconversionBatch(pBatch);
                            pBatch = new List<ProductionDocumentDetail>();
                        }
                    }
                }
            }

        }
        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="docBatch">The document batch.</param>
        /// <param name="docCollection">The original document collection.</param>
        private void SendImportReconversionBatch(List<ReconversionDocumentBEO> docBatch, ConversionDocCollection docCollection)
        {
            //create new message for the batcha and send
            var newDocCollection = new ConversionDocCollection();
            newDocCollection.BaseJobConfig = docCollection.BaseJobConfig;
            newDocCollection.BaseJobTypeId = docCollection.BaseJobTypeId;
            newDocCollection.DataSet = docCollection.DataSet;
            newDocCollection.Documents = docBatch; //use the docBatch for this message
            newDocCollection.HeartbeatFilePath = docCollection.HeartbeatFilePath;
            newDocCollection.JobConfig = docCollection.JobConfig;

            var message = new PipeMessageEnvelope
            {
                Body = newDocCollection
            };

            //update processSet table's status
            BulkUpdateImportReprocessingState(newDocCollection);

            //send to specified output pipe
            Pipe outPipe = GetOutputDataPipe(Constants.OutputPipeNameToConversionReprocessImport);
            if (outPipe != null) outPipe.Send(message);

        }

        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="docBatch">The document batch.</param>
        private void SendProductionReconversionBatch(List<ProductionDocumentDetail> docBatch)
        {
            var message = new PipeMessageEnvelope
            {
                Body = docBatch
            };

            //send to specified output pipe
            Pipe outPipe = GetOutputDataPipe(Constants.OutputPipeNameToProductionPreprocess);
            if (outPipe != null) outPipe.Send(message);

        }


        private void LogMessage(bool success, string information)
        {
            try
            {

                var log = new List<JobWorkerLog<BaseWorkerProcessLogInfo>>();
                var logEntry = new JobWorkerLog<BaseWorkerProcessLogInfo>();
                logEntry.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
                logEntry.CorrelationId = 0;
                logEntry.WorkerRoleType = "8781617C-FE8A-4726-A8B6-D63D36120A0C";
                logEntry.WorkerInstanceId = WorkerId;
                logEntry.IsMessage = false;
                logEntry.Success = success;
                logEntry.CreatedBy = "N/A";
                logEntry.LogInfo = new BaseWorkerProcessLogInfo();
                logEntry.LogInfo.Information = information;
                if (!success)
                {
                    logEntry.LogInfo.Message = information;
                }
                log.Add(logEntry);
                SendLog(log);
            }
            catch (Exception ex)
            {
                Tracer.Error("Conversion Reprocess Startup Worker: " + ex.Message);
            }
            
        }

        //send the log info
        private void SendLog(List<JobWorkerLog<BaseWorkerProcessLogInfo>> log)
        {
            LogPipe.Open();
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// Bulks the state of the update import reprocessing.
        /// </summary>
        /// <param name="conversionDocCollection">The conversion doc collection.</param>
        private  void BulkUpdateImportReprocessingState(ConversionDocCollection conversionDocCollection)
        {
            var reconverionDocumentBeos = conversionDocCollection.Documents;
            if (reconverionDocumentBeos == null) return;
            var documentConversionLogBeos = reconverionDocumentBeos.Select(ConvertReocnversionDocumentBeo).ToList();
            BulkUpdateProcessedDocuments(conversionDocCollection.DataSet.Matter.FolderID, documentConversionLogBeos);
        }
        /// <summary>
        /// Bulks the state of the update production reprocessing.
        /// </summary>
        /// <param name="conversionDocCollection">The conversion doc collection.</param>
        /// <param name="productionDocumentDetails">The production document details.</param>
        private  void BulkUpdateProductionReprocessingState(ConversionDocCollection conversionDocCollection,IEnumerable<ProductionDocumentDetail> productionDocumentDetails)
    {
           if(productionDocumentDetails==null)return;
           var documentConversionLogBeos = productionDocumentDetails.Select(ConvertProductionDocumentDetails).ToList();
            BulkUpdateProcessedDocuments(conversionDocCollection.DataSet.Matter.FolderID, documentConversionLogBeos);
        }

        /// <summary>
        /// Converts the reocnversion document beo.
        /// </summary>
        /// <param name="reconversionDocumentBeo">The reconversion document beo.</param>
        /// <returns></returns>
        private DocumentConversionLogBeo ConvertReocnversionDocumentBeo(ReconversionDocumentBEO reconversionDocumentBeo)
        {
            return new DocumentConversionLogBeo()
            {
                ProcessJobId=WorkAssignment.JobId,
                JobRunId=BootObject.OrginialJobId,
                LastModifiedDate=DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                DocumentId = reconversionDocumentBeo.DocumentId,
                CollectionId = reconversionDocumentBeo.CollectionId,
                ReasonId = EVRedactItErrorCodes.Na,
                Status=EVRedactItErrorCodes.Submitted
            };
        }

        /// <summary>
        /// Converts the production document details.
        /// </summary>
        /// <param name="productionDocumentDetail">The production document detail.</param>
        /// <returns></returns>
        private DocumentConversionLogBeo ConvertProductionDocumentDetails(ProductionDocumentDetail productionDocumentDetail)
        {
            return new DocumentConversionLogBeo()
                       {
                           CollectionId = productionDocumentDetail.OriginalCollectionId,
                           DocumentId = productionDocumentDetail.DocumentId,
                           JobRunId = BootObject.OrginialJobId,
                           ProcessJobId = WorkAssignment.JobId,
                           Status = EVRedactItErrorCodes.Submitted,
                           ReasonId = EVRedactItErrorCodes.Na,
                           LastModifiedDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                       };
        }
       
        private void BulkUpdateProcessedDocuments(long matterId, IEnumerable<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {


                if (documentConversionLogBeos == null) return;
                
                  vaultManager.AddOrUpdateConversionLogs(matterId,documentConversionLogBeos.ToList(), true);
            }
            catch (Exception exception)
            {
                exception.Trace().Swallow();

            }
        }

           #endregion Private helper functions
    }
}
