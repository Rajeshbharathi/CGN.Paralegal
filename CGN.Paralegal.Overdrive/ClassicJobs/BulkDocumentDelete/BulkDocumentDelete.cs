# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="BulkDocumentDelete.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kokila Bai S L</author>
//      <description>
//          Actual backend Backend process which does bulk delete of documents
//      </description>
//      <changelog>
//          <date value="26/5/2010">File added</date>
//          <date value="11-August-2011">changed a parameter in delete document method</date>
//          <date value="02/13/2013">Bug Fix #127361</date>
//          <date value="02/17/2013">Bug Fix #127361</date>
//          <date value="03/01/2013">Bug Fix #131265 - Deleting all the search retrieved documents using Select All option is deleting other non search resulted documents as well from reviewset : babugx</date>
//          <date value="30/4/2013">Fix for defect# 131265 : babugx</date>
//          <date value="08-23-2013">Bug # 150611  -Document is not getting deleted on deleting the document using the delete icon from Search Results view</date>//      </changelog>
//          <date value="3/30/2014">CNEV 3.0 - Requirement Bug #165088 - Document Delete NFR and functional fix : babugx</date>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.NotificationManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Search;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.TraceServices;

namespace LexisNexis.Evolution.BatchJobs.BulkDocumentDelete
{
    /// <summary>
    ///     This class represents Reviewer Bulk Tag job for
    ///     tagging more than 2 documents
    /// </summary>
    [Serializable]
    public class BulkDocumentDelete : BaseJob<BulkTagJobBusinessEntity, BulkDocumentDeleteJobTaskBusinessEntity>
    {
        #region Variables

        private int _totalDocs;
        private StringBuilder _jobNameTitle;
        private int _successfullyDeletedCount;
        private List<string> _documentsProcessedInJob; //list of Ids of documents directly processed in the job
        private int _batchSize;

        #endregion Variables

        private static IDocumentVaultManager _mDocumentVaultMngr;
        private BulkTagJobBusinessEntity _bulkDeleteOperationDetails;
        private double _taskPercent;
        private int taskNumber = 1;

        #region Initialize

        /// <summary>
        ///     This is the over ridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy">User who created the job</param>
        /// <returns>BulkTagJobBusinessEntity</returns>
        protected override BulkTagJobBusinessEntity Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            #region Initialize Job related variables

            _totalDocs = 0;
            _successfullyDeletedCount = 0;
            _documentsProcessedInJob = new List<string>();

            #endregion Initialize Job related variables

            BulkTagJobBusinessEntity jobBeo = null;
            try
            {
                // Initialize the JobBEO
                jobBeo = new BulkTagJobBusinessEntity {JobId = jobId, JobRunId = jobRunId};

                //filling properties of the job parameter
                UserBusinessEntity userBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                jobBeo.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A"))
                    ? userBusinessEntity.UserId
                    : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
                jobBeo.BootParameters = bootParameters;
                //constructing BulkTagJobBusinessEntity from boot parameter by de serializing
                _bulkDeleteOperationDetails = GetBulkDocumentDeleteOperationDetails(bootParameters);
                jobBeo.OperationMode = BulkTaskMode.Delete;
                jobBeo.JobName = Constants.JobName;
                jobBeo.JobTypeName = Constants.JobTypeName;
                _jobNameTitle = new StringBuilder();
                _jobNameTitle.Append(Constants.JobName);
                _jobNameTitle.Append(Constants.Space);
                _jobNameTitle.Append(Constants.Colon);
                _jobNameTitle.Append(Constants.Space);
                //Log
                Tracer.Info("{0}{1}:{2}", jobId, Constants.JobInitializationKey, Constants.JobInitializationValue);

                // Default settings
                jobBeo.StatusBrokerType = BrokerType.Database;
                jobBeo.CommitIntervalBrokerType = BrokerType.ConfigFile;
                jobBeo.CommitIntervalSettingType = SettingType.CommonSetting;
                InitJobSettings();
                _batchSize = 100;
                Tracer.Info("{0}{1}:{2}", jobId, Constants.AuditBootParameterKey, Constants.AuditBootParameterValue);

                if (_bulkDeleteOperationDetails != null)
                {
                    jobBeo.DocumentListDetails = _bulkDeleteOperationDetails.DocumentListDetails;
                }
                else
                {
                    Tracer.Error("{0}{1}:{2}", jobId, Constants.JobInitializationKey, Constants.EventXmlNotWellFormed);
                    throw new EVException().AddResMsg(ErrorCodes.ImpXmlFormatErrorId);
                }
            }
            catch (Exception exp)
            {
                exp.AddDbgMsg(jobId + Constants.EventInitializationExceptionValue).Trace();
                LogException(JobLogInfo, exp, LogCategory.Job, string.Empty, ErrorCodes.ProblemInJobInitialization);
            }
            return jobBeo;
        }

        private void InitJobSettings()
        {
            _mDocumentVaultMngr = EVUnityContainer.Resolve<IDocumentVaultManager>("DocumentVaultManager");
        }

        #endregion Initialize

        #region Generate Tasks

        /// <summary>
        ///     This is the overridden GenerateTasks() method.
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount"></param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<BulkDocumentDeleteJobTaskBusinessEntity> GenerateTasks(
            BulkTagJobBusinessEntity jobParameters, out int previouslyCommittedTaskCount)
        {
            previouslyCommittedTaskCount = 0;
            try
            {
                EvLog.WriteEntry(Constants.JobName + Constants.SpaceHiphenSpace + jobParameters.JobId,
                    Constants.AuditGenerateTaskValue,
                    EventLogEntryType.Information);
                var tasks = new Tasks<BulkDocumentDeleteJobTaskBusinessEntity>();

                #region Get filtered list of documents

                List<DocumentResult> filteredDocuments = GetFilteredListOfDocuments(jobParameters);

                #endregion Get filtered list of documents

                #region Create Task for each document to be deleted

                if (filteredDocuments.Count > 0)
                {
                    GenerateTasksForDocumentDelete(jobParameters, tasks, filteredDocuments);
                }

                #endregion Create Task for each document to be deleted

                if (_documentsProcessedInJob != null && _documentsProcessedInJob.Count > 0)
                {
                    _documentsProcessedInJob =
                        _documentsProcessedInJob.GroupBy(i => i, (key, group) => group.First()).ToList();
                }
                return tasks;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(_jobNameTitle.ToString() + jobParameters.JobId + Constants.AuditGenerateTaskValue,
                    exp.Message, EventLogEntryType.Error);
                LogException(JobLogInfo, exp, LogCategory.Job, string.Empty, ErrorCodes.ProblemInGenerateTasks);
                return null;
            }
        }

        #endregion Generate Tasks

        #region DoAtomicWork

        /// <summary>
        ///     This is the overridden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters"></param>
        /// <returns>Status of the operation.</returns>
        protected override bool DoAtomicWork(BulkDocumentDeleteJobTaskBusinessEntity task,
            BulkTagJobBusinessEntity jobParameters)
        {
            try
            {
                if (_mDocumentVaultMngr != null)
                {
                    if (task.Type.Equals("Delete"))
                    {
                        //Delete document
                        DeleteDocument(task);
                    }
                }
                EvLog.WriteEntry(_jobNameTitle.ToString() + jobParameters.JobId,
                    "Task Type:" + task.Type + " " + Constants.TaskNumber + task.TaskNumber +
                    Constants.JobEndMessage + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                    EventLogEntryType.Information);
                return true;
            }
            catch (Exception exp)
            {
                exp.Data["Message"] = "Problem in DoAtomicWork :" + task.TaskKey;
                exp.Trace();
                EvLog.WriteEntry(Constants.JobName + Constants.AuditInDoAtomicWorkValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(TaskLogInfo, exp, LogCategory.Task, task.TaskKey, ErrorCodes.ProblemInDoAtomicWork);
                return false;
            }
        }

        #endregion DoAtomicWork

        #region ShutDown

        /// <summary>
        ///     This is the overridden ShutDown() method
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job</param>
        protected override void Shutdown(BulkTagJobBusinessEntity jobParameters)
        {
            try
            {
                //Fetch the user information
                UserBusinessEntity objUser =
                    UserService.GetUser(StringUtility.EncodeTo64(jobParameters.JobScheduleCreatedBy));

                //Fetch the Job details
                var jobDetail =
                    JobService.GetJobDetails(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));
                jobDetail.CreatedById = objUser.UserGUID;

                if (jobDetail.NotificationId > 0)
                {
                    var location = GetOperationLocation(jobParameters.DocumentListDetails.SearchContext);
                    //Send notification message for bulk delete
                    SendNotificationForDelete(jobParameters, jobDetail, location);
                }
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName + Constants.AuditInShutdownValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, exp, LogCategory.Task, string.Empty, ErrorCodes.ProblemInShutDown);
            }
        }

        #endregion ShutDown

        #region Helper Methods

        /// <summary>
        ///     Logs the exception message into database..
        /// </summary>
        /// <param name="logInfo"></param>
        /// <param name="exp">exception received</param>
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key to identify the Task, need for task log only</param>
        /// <param name="errorCode"></param>
        private void LogException(LogInfo logInfo, Exception exp, LogCategory category, string taskKey, string errorCode)
        {
            switch (category)
            {
                case LogCategory.Job:
                {
                    var jobException = new EVJobException(errorCode, exp, logInfo);
                    throw (jobException);
                }
                default:
                {
                    logInfo.TaskKey = taskKey;
                    var jobException = new EVTaskException(errorCode, exp, logInfo);
                    throw (jobException);
                }
            }
        }
        #region Only search update call chanaged in the below code from 3.3 code base for search engine replacement work
        /// <summary>
        ///     Get filtered list of documents from the search context
        /// </summary>
        /// <param name="jobParameters">Input parameters of the job</param>
        /// <returns>List of filtered documents</returns>
        private List<DocumentResult> GetFilteredListOfDocuments(BulkTagJobBusinessEntity jobParameters)
        {
            var documentResults = new List<DocumentResult>();
            //No need to include family documents when search to figure out actual documents to delete
            DocumentQueryEntity documentQueryEntity = ConstructDocumentQueryEntity(jobParameters);

            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});

            if (jobParameters.DocumentListDetails.GenerateDocumentMode == DocumentSelectMode.UseSelectedDocuments)
            {
                jobParameters.DocumentListDetails.SelectedDocuments.SafeForEach(
                    selDoc => documentResults.Add(new DocumentResult {DocumentID = selDoc}));
            }

            else
            {
                documentQueryEntity.QueryObject.QueryList.Clear();

                documentQueryEntity.QueryObject.QueryList.Add(
                    new Query(jobParameters.DocumentListDetails.SearchContext.Query));
                var batchResults = JobSearchHandler.GetSearchResults(documentQueryEntity);
                var searchReults = batchResults.ResultDocuments;
                if (searchReults == null) return documentResults;
                if (_bulkDeleteOperationDetails.DocumentListDetails.DocumentsToExclude.Any())
                {
                    searchReults = searchReults.Where(
                        doc =>
                            !_bulkDeleteOperationDetails.DocumentListDetails.DocumentsToExclude.Contains(
                                doc.DocumentID)).
                        ToList();
                }

                documentResults.AddRange(searchReults);
                var selectedDocs = searchReults.Select(sr => sr.DocumentID).ToList();
                _bulkDeleteOperationDetails.DocumentListDetails.SelectedDocuments.AddRange(selectedDocs);
            }

            return documentResults;
        }

        /// <summary>
        ///     Construct the document query entity to search
        /// </summary>
        /// <param name="jobParameters">BulkTagJobBusinessEntity</param>
        /// <returns></returns>
        private DocumentQueryEntity ConstructDocumentQueryEntity(BulkTagJobBusinessEntity jobParameters)
        {
            var documentQueryEntity = new DocumentQueryEntity
            {
                DocumentCount = jobParameters.DocumentListDetails.TotalRecordCount,
                DocumentStartIndex = 0,
                QueryObject = new SearchQueryEntity
                {
                    DatasetId =
                        jobParameters.DocumentListDetails.
                            SearchContext.DataSetId,
                    ReviewsetId =
                        jobParameters.DocumentListDetails.
                            SearchContext.ReviewSetId,
                    MatterId =
                        jobParameters.DocumentListDetails.
                            SearchContext.MatterId,
                    IsConceptSearchEnabled =
                        jobParameters.DocumentListDetails.
                            SearchContext.IsConceptSearchEnabled
                }
            };

            documentQueryEntity.TotalRecallConfigEntity = new TotalRecallConfigEntity {IsTotalRecall = true};

            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});

            documentQueryEntity.IgnoreDocumentSnippet = true;
            return documentQueryEntity;
        }
        #endregion

        /// <summary>
        ///     DeleteDocument
        /// </summary>
        /// <param name="taskDetails"></param>
        private void DeleteDocument(BulkDocumentDeleteJobTaskBusinessEntity taskDetails)
        {
            List<string> documentIds = taskDetails.DocumentDetails.Select(doc => doc.DocumentID).ToList();
            if (!documentIds.Any()) return;
            DocumentBO.BatchDelete(
                _bulkDeleteOperationDetails.DocumentListDetails.SearchContext.MatterId.ToString(
                    CultureInfo.InvariantCulture),
                _bulkDeleteOperationDetails.DocumentListDetails.SearchContext.CollectionId, documentIds);
            _successfullyDeletedCount += documentIds.Count;
        }

        /// <summary>
        ///     Send notification for bulk document delete
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="jobDetail">Job details</param>
        /// <param name="location">Location where operation is being carried out</param>
        private void SendNotificationForDelete(BulkTagJobBusinessEntity jobParameters, JobBusinessEntity jobDetail,
            string location)
        {
            var objNotificationMessage = new NotificationMessageBEO
            {
                NotificationId = jobDetail.NotificationId,
                SubscriptionTypeId = 1,
                CreatedByUserGuid = jobDetail.CreatedById,
                CreatedByUserName = jobDetail.CreatedBy,
                NotificationSubject =
                    jobDetail.Name + Constants.Instance +
                    jobParameters.JobRunId
            };
            //Build notification message
            var notificationMessage = new StringBuilder();
            notificationMessage.Append(Constants.Table);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Header);
            notificationMessage.Append(Constants.BulkDeleteTitle);
            notificationMessage.Append(Constants.CloseHeader);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.BulkDeleteMessage);
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.NotificationMessageLocation + location);
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.BulkDeleteSuccessCount + _successfullyDeletedCount);
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.BulkDeleteFailCount + (_totalDocs - _successfullyDeletedCount));
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.CloseTable);

            //Build notification body
            var notificationBody = new StringBuilder();
            notificationBody.Append(notificationMessage);
            objNotificationMessage.NotificationBody = notificationBody.ToString();

            //Build Notification value to write to event log
            var notificationValues = new StringBuilder();
            notificationValues.Append(objNotificationMessage.NotificationId);
            notificationValues.Append(Constants.SpaceHiphenSpace);
            notificationValues.Append(objNotificationMessage.CreatedByUserGuid);
            notificationValues.Append(Constants.SpaceHiphenSpace);
            notificationValues.Append(objNotificationMessage.NotificationSubject);
            notificationValues.Append(Constants.SpaceHiphenSpace);
            notificationValues.Append(objNotificationMessage.NotificationBody);

            //Send notification only if all the mandatory values for notification are populated
            var notificationLogEntry = new StringBuilder();
            notificationLogEntry.Append(jobDetail.Name);
            notificationLogEntry.Append(Constants.SpaceHiphenSpace);
            notificationLogEntry.Append(jobDetail.Id);

            EvLog.WriteEntry(notificationLogEntry.ToString(), notificationValues.ToString(),
                EventLogEntryType.Information);
            if ((objNotificationMessage.NotificationId != 0) &&
                (!string.IsNullOrEmpty(objNotificationMessage.CreatedByUserGuid)) &&
                (!string.IsNullOrEmpty(objNotificationMessage.NotificationSubject)) &&
                (!string.IsNullOrEmpty(objNotificationMessage.NotificationBody)))
            {
                NotificationBO.SendNotificationMessage(objNotificationMessage);
            }
        }


        /// Generate Tasks for bulk document delete operation
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="tasks">List of tasks</param>
        /// <param name="filteredDocuments">List of filtered documents</param>
        /// <returns>Number of tasks</returns>
        private void GenerateTasksForDocumentDelete(BulkTagJobBusinessEntity jobParameters,
            Tasks<BulkDocumentDeleteJobTaskBusinessEntity> tasks, List<DocumentResult> filteredDocuments)
        {
            filteredDocuments.SafeForEach(x => _documentsProcessedInJob.Add(x.DocumentID));
            _batchSize.ShouldBeGreaterThan(0);
            _totalDocs = filteredDocuments.Count;
            int noOfBatches = _totalDocs%_batchSize == 0 ? _totalDocs/_batchSize : (_totalDocs/_batchSize) + 1;

            _taskPercent = (100.0/(noOfBatches + 2));

            int processedDocumentsCount = 0;

            for (int i = 0; i < noOfBatches; i++)
            {
                var bulkDocumentDeleteJobTaskBusinessEntity = new BulkDocumentDeleteJobTaskBusinessEntity
                {
                    JobId = jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                    TaskNumber = taskNumber++,
                    TaskComplete = false,
                    TaskPercent = _taskPercent,
                    TagDetails = jobParameters.TagDetails,
                    IsOperationTagging = jobParameters.IsOperationTagging,
                    IsTagAllDuplicates = jobParameters.IsTagAllDuplicates,
                    IsTagAllFamily = jobParameters.IsTagAllFamily,
                    Type = "Delete"
                };

                bulkDocumentDeleteJobTaskBusinessEntity.DocumentDetails.AddRange(
                    filteredDocuments.Skip(processedDocumentsCount).Take(_batchSize));
                processedDocumentsCount += _batchSize;
                tasks.Add(bulkDocumentDeleteJobTaskBusinessEntity);
            }
        }

        /// <summary>
        ///     This method will return BulkTagJobBusinessEntity out of the passed boot parameter
        /// </summary>
        /// <param name="bootParamter">String</param>
        /// <returns>BulkTagJobBusinessEntity</returns>
        private BulkTagJobBusinessEntity GetBulkDocumentDeleteOperationDetails(String bootParamter)
        {
            //Creating a stringReader stream for the boot parameter
            using (var stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                var xmlStream = new XmlSerializer(typeof (BulkTagJobBusinessEntity));

                //De serialization of boot parameter to get BulkTagJobBusinessEntity
                return (BulkTagJobBusinessEntity) xmlStream.Deserialize(stream);
            }
        }

        /// <summary>
        ///     Get location where document delete is being done
        /// </summary>
        /// <param name="searchContext">Search context</param>
        /// <returns>Location where document delete is being done</returns>
        private string GetOperationLocation(RVWSearchBEO searchContext)
        {
            string location = string.Empty;
            if (!string.IsNullOrEmpty(searchContext.ReviewSetId))
            {
                ReviewsetDetailsBEO reviewSetDetails =
                    ReviewSetService.GetReviewSetDetails(searchContext.MatterId.ToString(CultureInfo.InvariantCulture),
                        searchContext.ReviewSetId);
                if (reviewSetDetails != null)
                {
                    location = reviewSetDetails.ReviewSetName;
                }
            }
            else
            {
                DatasetBEO dataSetDetails =
                    DataSetService.GetDataSet(searchContext.DataSetId.ToString(CultureInfo.InvariantCulture));
                if (dataSetDetails != null)
                {
                    location = dataSetDetails.FolderName;
                }
            }
            return location;
        }

        #endregion Helper Methods
    }
}