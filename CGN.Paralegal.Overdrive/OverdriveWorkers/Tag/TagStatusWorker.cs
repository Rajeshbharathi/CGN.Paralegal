#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="IndexingValidationWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>babugx</author>
//      <description>
//          This file contains all the  methods related to  TagStatusWorker
//      </description>
//      <changelog>
//           <date value="05/19/2014">Draft Created : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
#region Namespaces

using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
#endregion

namespace LexisNexis.Evolution.Worker
{
    public class TagStatusWorker : WorkerBase
    {
        #region Notification Table constants

        private int _processedBatchCount;
        private int _totalBatchCount;
        private int _documentsTagged;
        private int _documentsFailed;
        private int _documentsAlreadyTagged;

        #endregion


        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            var bulkTagRecord = (BulkTagRecord)message.Body;
            try
            {
                bulkTagRecord.ShouldNotBe(null);
                // Mock http context
                bulkTagRecord.CreatedByUserGuid.ShouldNotBeEmpty();
                MockSession(bulkTagRecord.CreatedByUserGuid);

                if (_processedBatchCount == 0)
                {
                    bulkTagRecord.NumberOfBatches.ShouldBeGreaterThan(0);
                    _totalBatchCount = bulkTagRecord.NumberOfBatches;
                }

                _processedBatchCount += 1;
                Tracer.Info("Total # of batches processed so far : {0}", _processedBatchCount);

                if (bulkTagRecord.Notification.DocumentsTagged != null)
                {
                    _documentsTagged += bulkTagRecord.Notification.DocumentsTagged.Count;
                }

                if (bulkTagRecord.Notification.DocumentsFailed != null)
                {
                    _documentsFailed += bulkTagRecord.Notification.DocumentsFailed.Count;
                }

                if (bulkTagRecord.Notification.DocumentsAlreadyTagged != null)
                {
                    _documentsAlreadyTagged += bulkTagRecord.Notification.DocumentsAlreadyTagged.Count;
                }

                //Send out the completed notification, only all the batches are processed
                if (_processedBatchCount != _totalBatchCount) return;
                Tracer.Info("Total # of batches processed : {0}", _processedBatchCount);
                Tracer.Info("Total # of documents tagged : {0}", _documentsTagged);
                Tracer.Info("Total # of documents failed : {0}", _documentsFailed);
                Tracer.Info("Total # of documents already tagged : {0}", _documentsAlreadyTagged);

                UpdateTagStatistics(bulkTagRecord);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        private static void MockSession(string userGuid)
        {
            if (EVHttpContext.CurrentContext == null)
            {
                Utility.SetUserSession(userGuid);
            }
        }

        /// <summary>
        /// Helper method to update the tag log in database
        /// </summary>
        /// <param name="bulkTagRecord"></param>
        private void UpdateTagStatistics(BulkTagRecord bulkTagRecord)
        {
            var taglog = new TagLogBEO
                                       {
                                           DatasetId = Convert.ToInt32(bulkTagRecord.DatasetId),
                                           JobId = WorkAssignment.JobId,
                                           AlreadyTag = _documentsAlreadyTagged,
                                           FailedTag = _documentsFailed,
                                           DocumentTag = _documentsTagged
                                       };

            RVWTagBO.UpdateTagLog(taglog);
        }
    }
}