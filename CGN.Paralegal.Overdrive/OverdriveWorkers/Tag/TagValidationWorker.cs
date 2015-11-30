#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="TagValidationWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>babugx</author>
//      <description>
//          This file contains all the  methods related to tag completion verification in Search Sub System
//      </description>
//      <changelog>
//           <date value="19-May-2014">Created</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
#region Namespaces
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.MatterManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#endregion

namespace LexisNexis.Evolution.Worker
{
    public class TagValidationWorker : WorkerBase
    {
        const string WorkerRoletype = "bulktag6-c696-41f2-a2eb-7c52e7abdaf9";
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            message.ShouldNotBe(null);
            var bulkTagRecord = (BulkTagRecord)message.Body;
            try
            {
                bulkTagRecord.ShouldNotBe(null);
                bulkTagRecord.CreatedByUserGuid.ShouldNotBeEmpty();
                MockSession(bulkTagRecord.CreatedByUserGuid);

                var failedDocs = new List<KeyValuePair<string, string>>();

                if (MatterBO.IsCrawlComplete(bulkTagRecord.MatterId.ToString(CultureInfo.InvariantCulture), bulkTagRecord.Originator, failedDocs))
                {
                    try
                    {
                        SendLog(CreateLogs(bulkTagRecord.Documents, failedDocs, bulkTagRecord));
                    }
                    catch (Exception excep)
                    {
                        var errMsg = string.Format("Unable to create / send logs to be sent to log pipe. Exception details: {0}",
                                excep.ToUserString());
                        excep.Trace().Swallow();
                    }
                    Send(bulkTagRecord);
                }
                else
                {
                    InputDataPipe.Send(new PipeMessageEnvelope() { Body = bulkTagRecord, IsPostback = true });
                }
            }
            catch (Exception ex)
            {
                if (bulkTagRecord != null)
                {
                    ex.Data["Originator"] = bulkTagRecord.Originator;
                }
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Creating logs to send logging worker. 
        /// </summary>
        /// <param name="originalDocuments"></param>
        /// <param name="failedDocuments"></param>
        /// <param name="bulkTagRecord"></param>
        private List<JobWorkerLog<TagLogInfo>> CreateLogs(List<BulkDocumentInfoBEO> originalDocuments, 
            IEnumerable<KeyValuePair<string, string>> failedDocuments, 
            BulkTagRecord bulkTagRecord)
        {
            var jobWorkerLogs = new List<JobWorkerLog<TagLogInfo>>();
            foreach (var failedDocument in failedDocuments)
            {
                var documentLog = originalDocuments.Find(d => d.DocumentId == failedDocument.Key);
                if (null == documentLog) continue;

                var logInfo = new TagLogInfo
                {
                    Information = string.Format("{0} : {1}",documentLog.DCN , failedDocument.Value),
                    IsFailureInDatabaseUpdate = false,
                    IsFailureInSearchUpdate = true,
                    DocumentControlNumber = documentLog.DCN,
                    DocumentId = failedDocument.Key,                    
                };

                var jobWorkerLog = new JobWorkerLog<TagLogInfo>
                {
                    JobRunId = Convert.ToInt32(PipelineId),
                    WorkerInstanceId = WorkerId,
                    WorkerRoleType = WorkerRoletype,
                    Success = false,
                    IsMessage = false,
                    LogInfo = logInfo
                };
                jobWorkerLogs.Add(jobWorkerLog);
                if (!bulkTagRecord.Notification.DocumentsFailed.Exists(d => d == failedDocument.Key))
                {
                    bulkTagRecord.Notification.DocumentsFailed.Add(failedDocument.Key);
                }
            }
            return jobWorkerLogs;
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<TagLogInfo>> log)
        {
                LogPipe.Open();
                var message = new PipeMessageEnvelope()
                                  {
                                      Body = log
                                  };
                LogPipe.Send(message);
        }

        private void Send(BulkTagRecord bulkTagRecord)
        {
            var message = new PipeMessageEnvelope()
            {
                Body = bulkTagRecord
            };
            if (null != OutputDataPipe)
            {
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(bulkTagRecord.Documents.Count);
            }
        }

        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="message"></param>
        private void Send(PipeMessageEnvelope message)
        {
            if (OutputDataPipe != null)
            {
                OutputDataPipe.Send(message);
            }
        }

        private void MockSession(string userGuid)
        {
            if (EVHttpContext.CurrentContext == null)
            {
                Utility.SetUserSession(userGuid);
            }
        }
    }
}
