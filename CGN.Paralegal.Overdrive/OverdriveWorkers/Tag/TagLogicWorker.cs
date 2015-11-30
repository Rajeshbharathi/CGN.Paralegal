#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="TagLogicWorker" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>babugx</author>
//      <description>
//          This file contains the methods for tag logic calculations and grouping
//      </description>
//      <changelog>
//          <date value="19-May-2014">Created</date>
//          </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespace

using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace LexisNexis.Evolution.Worker
{
    public class TagLogicWorker : SearchEngineWorkerBase
    {
        private const string WorkerRoletype = "bulktag3-91fb-46fb-8d78-88d22b7c1c51";

        /// <summary>
        /// The _matter identifier
        /// </summary>
        private long _matterId;

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            ReadMatterId();
            SetCommiyIndexStatusToInitialized(_matterId);
          
        }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var bulkTagRecord = (BulkTagRecord) envelope.Body;

            try
            {
                // Mock http context
                bulkTagRecord.CreatedByUserGuid.ShouldNotBeEmpty();
                MockSession(bulkTagRecord.CreatedByUserGuid);

                bulkTagRecord.Documents.ShouldNotBe(null);
                bulkTagRecord.TagDetails.ShouldNotBe(null);

                if (!bulkTagRecord.Documents.Any())
                    return;

                bulkTagRecord.Originator = Guid.NewGuid().ToString();

                //var bootObject = GetBootObject<BulkTagJobBusinessEntity>(BootParameters);

                // Dictionary to hold list of documents to update
                var documentList = bulkTagRecord.Documents.
                    ToDictionary(a => a.DocumentId, a => a);

                //get Effective Tags for the give tag
                var effectiveTagList =
                    BulkTagBO.GetEfectiveTags(bulkTagRecord.MatterId.ToString(CultureInfo.InvariantCulture),
                        new Guid(bulkTagRecord.CollectionId), bulkTagRecord.BinderId, bulkTagRecord.TagDetails.Id,
                        byte.Parse((bulkTagRecord.TagDetails.IsOperationTagging ? "1" : "3"))).ToList();

                List<DocumentTagBEO> currentState;
                var newState =
                    BulkTagBO.ArriveNewTagState(bulkTagRecord.MatterId.ToString(CultureInfo.InvariantCulture),
                        new Guid(bulkTagRecord.CollectionId), documentList, effectiveTagList,
                        (bulkTagRecord.TagDetails.IsTagAllDuplicates ||
                         bulkTagRecord.TagDetails.TagBehaviors.Exists(
                             x => string.Compare(x.BehaviorName, Constants.TagAllDuplicatesBehaviorName, true,
                                 CultureInfo.InvariantCulture) == 0)),
                        (bulkTagRecord.TagDetails.IsTagAllFamily || (
                            bulkTagRecord.TagDetails.TagBehaviors.Exists(
                                x =>
                                    string.Compare(x.BehaviorName, Constants.TagAllFamilyBehaviorName, true,
                                        CultureInfo.InvariantCulture) == 0) ||
                            bulkTagRecord.TagDetails.TagBehaviors.Exists(
                                x =>
                                    string.Compare(x.BehaviorName, Constants.TagAllThreadBehaviorName, true,
                                        CultureInfo.InvariantCulture) == 0))
                            ), out currentState);

                bulkTagRecord.CurrentState = currentState;
                bulkTagRecord.NewState = newState;
               

                var watch = new Stopwatch();
                watch.Start();
                Parallel.Invoke(
                    () =>
                    {
                        MockSession(bulkTagRecord.CreatedByUserGuid);
                        List<DocumentTagBEO> vaultState;
                        var notification =
                            BulkTagBO.VaultTagUpdate(bulkTagRecord.MatterId.ToString(CultureInfo.InvariantCulture),
                                new Guid(bulkTagRecord.CollectionId), bulkTagRecord.BinderId,
                                bulkTagRecord.CurrentState, bulkTagRecord.NewState, bulkTagRecord.CreatedByUserGuid,
                                out vaultState);

                        BulkTagBO.ArriveNotificationStatistics(
                            bulkTagRecord.MatterId.ToString(CultureInfo.InvariantCulture),
                            bulkTagRecord.TagDetails.Id, bulkTagRecord.CurrentState, bulkTagRecord.NewState, vaultState,
                            notification);

                        notification.ShouldNotBe(null);
                        bulkTagRecord.Notification = notification;
                        bulkTagRecord.VaultState = vaultState;
                        bulkTagRecord.TagTimeStamp = DateTime.UtcNow;
                    },
                    
                    () =>
                     {
                         MockSession(bulkTagRecord.CreatedByUserGuid);
                         BulkTagBO.IndexTagUpdate
                (bulkTagRecord.MatterId.ToString(CultureInfo.InvariantCulture),
                bulkTagRecord.CollectionId,
                bulkTagRecord.CurrentState, bulkTagRecord.NewState);
                         Tracer.Info("{0} Documents queueued for indexing for JobRunId : {1} against the tag : {2}",
                             bulkTagRecord.Documents.Count, PipelineId, bulkTagRecord.TagDetails.Name);
                     }

                    //TODO: Search Engine Replacement - Search Sub System - Update Tag details in search index
                    );
                watch.Stop();
                Tracer.Info("Total time in parallel update in vault & search subsystem is {0} milli seconds",
                    watch.Elapsed.TotalMilliseconds);
                Send(bulkTagRecord);
            }
            catch (Exception ex)
            {
                LogMessage(true, string.Format("Error in TagLogicWorker - Exception: {0}", ex.ToUserString()),
                    bulkTagRecord.CreatedByUserGuid);
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }
        /// <summary>
        /// Ends the work.
        /// </summary>
        protected override void EndWork()
        {
            SetCommitIndexStatusToCompleted(_matterId);

        }

        private static void MockSession(string userGuid)
        {
            if (EVHttpContext.CurrentContext == null)
            {
                Utility.SetUserSession(userGuid);
            }
        }
       

        private void Send(BulkTagRecord bulkTagRecord)
        {
            var message = new PipeMessageEnvelope
            {
                Body = bulkTagRecord
            };
            if (null == OutputDataPipe) return;
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(bulkTagRecord.Documents.Count);
        }

        /// <summary>
        /// Construct the log and send it to log worker
        /// </summary>
        public void LogMessage(bool isError, string msg, string userGuid)
        {
            try
            {
                if (isError)
                {
                    Tracer.Error(msg);
                }
                else
                {
                    Tracer.Info(msg);
                    return;
                }

                var logInfoList = new List<JobWorkerLog<TagLogInfo>>
                {
                    ConstructTagLog(true, true, false, string.Empty, msg, WorkerRoletype, userGuid)
                };

                LogPipe.ShouldNotBe(null);
                var message = new PipeMessageEnvelope
                {
                    Body = logInfoList
                };
                LogPipe.Send(message);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Construct Near Duplication Log Info for Document
        /// </summary>
        private JobWorkerLog<TagLogInfo> ConstructTagLog(bool isError, bool isErrorInDatabase,
            bool isErrorInSearchEngine,
            string docTitle, string information, string workerRoleType, string userGuid)
        {
            var tagLogInfo = new JobWorkerLog<TagLogInfo>
            {
                JobRunId = !string.IsNullOrEmpty(PipelineId) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = workerRoleType,
                Success = !isError,
                CreatedBy = userGuid,
                IsMessage = !isError,
                LogInfo = new TagLogInfo
                {
                    DocumentControlNumber = docTitle,
                    IsFailureInDatabaseUpdate = isErrorInDatabase,
                    IsFailureInSearchUpdate = isErrorInSearchEngine,
                    Information = information
                }
            };
            return tagLogInfo;
        }

        /// <summary>
        /// Reads the matter identifier.
        /// </summary>
        private void ReadMatterId()
        {
            var bulkTagJobTaskBusinessEntity = XmlUtility.DeserializeObject(BootParameters, typeof(BulkTagJobBusinessEntity)) as BulkTagJobBusinessEntity;
            bulkTagJobTaskBusinessEntity.ShouldNotBe(null);
            bulkTagJobTaskBusinessEntity.TagDetails.ShouldNotBe(null);
            _matterId = bulkTagJobTaskBusinessEntity.TagDetails.MatterId;
        }
    }
}