//---------------------------------------------------------------------------------------------------
// <copyright file="PrintValidationWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Madhavan Murrali</author>
//      <description>
//          This file contains the PrintValidationWorker.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//          <date value="22/4/2013">ADM – PRINTING – buddy defect fixes</date>
//          <date value="07/24/2013">BugFix # 146819 - Discrepancy in document count isssue fix </date>
//          <date value="07/24/2013">BugFix # 149102 - NLog Error Fixes -INSERT statement conflicted with the FOREIGN KEY constraint "FK_EV_JOB_JobLogs_EV_JOB_JobMaster </date>
//          <date value="07/24/2013">Bug # 142090 - [Bulk Printing]: Bulk print job is getting failed when a field with blank value is chosen for file name </date>
//          <date value="08/30/2013">Bug # 146858 </date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//          <date value="09/25/2014">Bug # 175765 - When document printing is timeout please display the warning. </date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#region Namespace
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.PrinterManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;



#endregion

namespace LexisNexis.Evolution.Worker
{
    public class PrintValidationWorker : WorkerBase
    {
        #region Private Variables
        private BulkPrintServiceRequestBEO bulkPrintServiceRequestBEO;
        private string sharedLocation;
        private MappedPrinterBEO mappedPrinter;
        private int totalDocumentCount;
        private string sourceLocation; 
        private int jobRunId = 0;
        DatasetBEO _mDataSet;
        const string DocumentFailedDuetoTimeOutError = "The document has timed-out ";
        #endregion

        protected override void BeginWork()
        {
            base.BeginWork();
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(BootParameters);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(BulkPrintServiceRequestBEO));

            //Deserialization of bootparameter to get BulkPrintServiceRequestBEO
            bulkPrintServiceRequestBEO = (BulkPrintServiceRequestBEO)xmlStream.Deserialize(stream);
            sharedLocation = bulkPrintServiceRequestBEO.FolderPath;
            mappedPrinter = PrinterManagementBusiness.GetMappedPrinter(new MappedPrinterIdentifierBEO(bulkPrintServiceRequestBEO.Printer.UniqueIdentifier.Split(Constants.Split).Last(), true));
            sourceLocation = Path.Combine(Path.Combine(sharedLocation, bulkPrintServiceRequestBEO.Name), Constants.SourceDirectoryPath);
            jobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToInt32(PipelineId) : 0;
            //Get Dataset details for a given Collection Id
            _mDataSet = DataSetBO.GetDataSetDetailForCollectionId(bulkPrintServiceRequestBEO.DataSet.CollectionId);
        }

        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var printCollection = (PrintDocumentCollection)envelope.Body;
            totalDocumentCount = printCollection.TotalDocumentCount;
            ValidateDocuments(printCollection.Documents);
        }

        /// <summary>
        /// Processes the data.
        /// </summary>
        public void ValidateDocuments(List<DocumentResult> printDocuments)
        {
            if (bulkPrintServiceRequestBEO == null) return;
            var targetDirectoryPath = Path.Combine(sharedLocation, bulkPrintServiceRequestBEO.Name);
            var notReady = new List<DocumentResult>();
            var sourcedir = new DirectoryInfo(targetDirectoryPath);
            var separatorFileList = sourcedir.GetFiles();
            try
            {
                foreach (DocumentResult documentResult in printDocuments)
                {
                    string fieldValue;
                    fieldValue = documentResult.DocumentControlNumber;
                    if (documentResult.Fields != null && documentResult.Fields.Any())
                    {
                        foreach (var field in documentResult.Fields)
                        {
                            if (field == null) continue;
                            if (String.IsNullOrEmpty(field.Name)) continue;
                            if (field.Name.Equals(bulkPrintServiceRequestBEO.FieldName)) fieldValue = !string.IsNullOrEmpty(field.Value) ? field.Value.Trim() : fieldValue;
                    }
                    }

                    var document = separatorFileList.Where(x => x.Name.Equals(string.Format("{0}.pdf", fieldValue)));
                    if (!document.Any())
                    {
                        if (documentResult.CreatedDate > DateTime.Now.AddMinutes(-5))
                        {
                            notReady.Add(documentResult);
                        }
                        else
                        {
                            Tracer.Info("Print Validation Worker - Failed Document: {0}", documentResult.DocumentControlNumber);
                            LogMessage(documentResult, false, DocumentFailedDuetoTimeOutError);
                        }
                    }
                    else
                    {
                        LogMessage(documentResult, true, "Document queued to printer");
                    }
                }

                if (notReady.Any())
                {
                    var documentCollection = new PrintDocumentCollection { Documents = notReady, TotalDocumentCount = totalDocumentCount };
                    var message = new PipeMessageEnvelope { Body = documentCollection, IsPostback = true };
                    InputDataPipe.Send(message);
                }
                else
                {

                    UpdateAuditLog();
                }
            }
            catch (IOException ex)
            {
                ex.AddDbgMsg("Directory = {0}", sourceLocation);
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                UpdateAuditLog();
            }
        }

        /// <summary>
        /// Update Audit Log
        /// </summary>
        private void UpdateAuditLog()
        {
            var jobId = WorkAssignment.JobId;
            var userid = bulkPrintServiceRequestBEO.RequestedBy.UserId;
            Helper.JobLog(jobId, jobRunId, string.Empty, Helper.SerializeObject(new LogInfo()), userid, false, false);
            
            
        }

        
        

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(DocumentResult document, bool success, string message)
        {
            var log = new List<JobWorkerLog<LogInfo>>();
            var taskId = Convert.ToInt32(document.DocumentControlNumber.Replace(_mDataSet.DCNPrefix, string.Empty));
            // form the paser lof entity
            var parserLog = new JobWorkerLog<LogInfo>
            {
                JobRunId =

                    (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = taskId,
                WorkerRoleType = "PrintValidationWorker",
                WorkerInstanceId = WorkerId,
                IsMessage = false,
                Success = success, //!string.IsNullOrEmpty(documentDetail.DocumentProductionNumber) && success,
                CreatedBy = bulkPrintServiceRequestBEO.RequestedBy.UserId,
                LogInfo =
                    new LogInfo
                    {
                        TaskKey = document.DocumentControlNumber,
                        IsError = !success

                    }

            };
            parserLog.LogInfo.AddParameters(Constants.DCN, document.DocumentControlNumber);
            parserLog.LogInfo.AddParameters(message, mappedPrinter.PrinterDetails.Name);
            log.Add(parserLog);
            SendLog(log);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<LogInfo>> log)
        {
            LogPipe.Open();
            var message = new PipeMessageEnvelope
                {
                    Body = log
                };
            LogPipe.Send(message);
        }
    }
}
