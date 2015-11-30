#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionReprocessImportWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Henry</author>
//      <description>
//          This file contains all the  methods related to  ConversionReprocessImportWorker
//      </description>
//      <changelog>
//          <date value="05-15-2013">Initial: Reconversion Processing</date>
//          <date value="05-21-2013">Bug # 142937,143536 and 143037 -ReConvers Buddy Defects</date>
//          <date value="06-06-2013">Bug # 143682-Fix to reprocess the partially converted document</date>
//          <date value="06-26-2013">Bug # 146526 -Disposing WebResponse object and error handling while pushing the document</date>
//          <date value="06-26-2013">Bug # 145018 -[CHEV 2.2.1][NNV Reprocess] The file size is not updating for the natives reprocessed through NNV reprocess job</date>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="10/07/2013">Dev Bug  # 154336 -ADM -ADMIN - 006 - Import /Production Reprocessing reprocess all documents even with filter and all and other migration fixes
//          <date value="10/23/2013">Bug  # 154585 -ADM -ADMIN - 006 - Fix to avoid forever  conversion validation and blocking behaviour for document conversion time out
//          <date value="10/23/2013">Bug  # 156607 - Fix to avoid forever  conversion validation for documents without hearbeat files by having absolute timeout
//          <date value="03/24/2015">Bug Fix 184140 - Publish blank pages</date>
//          <date value="10/01/2015">Making sure that reprocessing passes right conversion priority to IGC</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region All Namespaces
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.External;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using OverdriveWorkers.Data;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.TraceServices;

#endregion All Namespaces

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// Pushes given list of documents for conversion.
    /// </summary>
    public class ConversionReprocessImportWorker : WorkerBase
    {
        public INearNativeConverter ConverterAdapter { get; set; }

        private IDocumentVaultManager _vaultManager = null;
        public ConversionReprocessJobBeo BootObject { get; set; }

        #region Job Framework functions
        protected override void BeginWork()
        {
            base.BeginWork();
            try
            {
              
                _vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
                BootObject = GetBootObject<ConversionReprocessJobBeo>(BootParameters);
                BootObject.ShouldNotBe(null);
                Tracer.Info("Conversion priority {0}", BootObject.NearNativeConversionPriority);
            }
           catch (Exception ex)
            {
                LogMessage(false, "ReconversionStartup worker failed: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Processes the work item. pushes give document files for conversion
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            if (message == null || message.Body == null) return;

            var docCollection = message.Body as ConversionDocCollection;
            if (docCollection == null || docCollection.Documents == null || !docCollection.Documents.Any())
                return; //nothing to process

            //initial converterAdapter. Near native conversion priority assume to be the same for all documents.
            //need to pass job id to adapter
            if (ConverterAdapter == null )
                ConverterAdapter = new NearNativeConversionAdapter(true, WorkAssignment.JobId.ToString(CultureInfo.InvariantCulture), BootObject.NearNativeConversionPriority, Constants.BulkReProcessConversionContext);

            ImportReconvert(docCollection);

            SendMessage(message);

            #endregion Loop through documents and push for conversion

        }

        /// <summary>
        /// Reconvert document from import jobs
        /// </summary>
        /// <param name="docCollection">The documents</param>
        public void ImportReconvert(ConversionDocCollection docCollection)
        {
            // loop through documents to push each document for conversion
            var documentConversionLogBeo = new List<DocumentConversionLogBeo>();
            var validationList = new List<ReconversionDocumentBEO>();
            foreach (var document in docCollection.Documents)
            {
                short reasonId;
                byte status;
                int fileSize = 0;
                try
                {

                    //if FileList does not exists, do not send for conversion
                    if (document.FileList == null)
                    {
                        //can not find file
                        LogMessage(false, "File paths null or Files does not exist");
                    }
                    else
                    {
                        //calculate file size; multiple images file will add them together
                        fileSize = 0;
                        foreach (var file in document.FileList)
                        {
                            if (String.IsNullOrEmpty(file)) continue;
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Exists)
                                fileSize += (int) Math.Ceiling(fileInfo.Length/Constants.KBConversionConstant);
                        }
                    }
                    var filesWithReasonCodes = Utils.GetReasonCodes(document.FileList);
                    var heartbeatFilePath = docCollection.GetDefaultHeartbeatFileFullPath(document);
                    var matterId = docCollection.DataSet.Matter.FolderID;
                    
                    ConverterAdapter.PushDocumentForConversionWithHearbeat(
                            matterId.ToString(CultureInfo.InvariantCulture), document.CollectionId,
                            document.DocumentId, filesWithReasonCodes.Item1,
                            heartbeatFilePath, CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.PublishBlankPages));

                    reasonId = filesWithReasonCodes.Item2;
                    status = filesWithReasonCodes.Item3;

                    document.ConversionCheckCounter = 0;
                    document.SubmittedTime = DateTime.UtcNow;
                    document.ConversionEnqueueTime = DateTime.UtcNow;

                    //only add submitted documents for validation, otherwise we might wait for heartbeat files that never exists.
                    validationList.Add(document);

                    IncreaseProcessedDocumentsCount(1);
                }
                catch (Exception ex)
                {
                    ReportToDirector(ex);
                    ex.Trace().Swallow();
                    if (ex is WebException)
                {
                    reasonId = EVRedactItErrorCodes.FailedToSendFile;
                }
                    else
                {
                    reasonId = Utils.GetConversionErrorReason(ex.GetErrorCode());
                    }
                    status = EVRedactItErrorCodes.Failed;
                    LogMessage(false, ex.ToUserString());
                }

                var logBeo = ConvertToReoconversionDocumentBeo(document, status, reasonId);
                logBeo.Size = fileSize;
                documentConversionLogBeo.Add(logBeo);

            }

            docCollection.Documents = validationList;
            BulkUpdateProcessedDocuments(docCollection.DataSet.Matter.FolderID, documentConversionLogBeo);
        }

        /// <summary>
        /// Bulks the update processed documents.
        /// </summary>
        /// <param name="matterId">The matter id.</param>
        /// <param name="documentConversionLogBeos">The document conversion log beos.</param>
        private void BulkUpdateProcessedDocuments(long matterId, IEnumerable<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {


                if (documentConversionLogBeos == null) return;

                _vaultManager.AddOrUpdateConversionLogs(matterId, documentConversionLogBeos.ToList(), true);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();

            }
        }
        /// <summary>
        /// This method deserializes and determine the Xml T typed job info object
        /// </summary>
        /// <param name="bootParameter"></param>

        private T GetBootObject<T>(string bootParameter)
        {
            if (bootParameter == null) throw new EVException().AddUsrMsg("bootparamter can not be null");

            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParameter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(T));

            //Deserialization of bootparameter to get ImportBEO
            return (T)xmlStream.Deserialize(stream);
        }
        private DocumentConversionLogBeo ConvertToReoconversionDocumentBeo(ReconversionDocumentBEO reconversionDocumentBeo,byte processStatus,
            short reasonId)
        {
            if (WorkAssignment == null||BootObject==null) return null;
            return new DocumentConversionLogBeo()
            {
                ProcessJobId = WorkAssignment.JobId,
                JobRunId = BootObject.OrginialJobId,
                LastModifiedDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                DocumentId = reconversionDocumentBeo.DocumentId,
                CollectionId = reconversionDocumentBeo.CollectionId,
                Status = processStatus,
                ReasonId = reasonId
            };
        }
        
        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="message">The document batch.</param>
        public void SendMessage(PipeMessageEnvelope message)
        {
            Pipe outPipe = GetOutputDataPipe(Constants.OutputPipeNameToValidation);
            if (outPipe != null)
            {
                outPipe.Send(message);
            }

        }

        //To be used by converters
        public string GetPipelineId()
        {
            return PipelineId;
        }

        //To be used by converters
        public string GetPipelineType()
        {
            return PipelineType.Moniker;
        }


        public void LogMessage(bool success, string information)
        {
            var log = new List<JobWorkerLog<BaseWorkerProcessLogInfo>>();
            var logEntry = new JobWorkerLog<BaseWorkerProcessLogInfo>();
            logEntry.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            logEntry.CorrelationId = 0;
            logEntry.WorkerRoleType = "78FD2345-85E1-4D16-93FF-679F0D8B94A7"; 
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

        private void SendLog(List<JobWorkerLog<BaseWorkerProcessLogInfo>> log)
        {
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

  }



}
