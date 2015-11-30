//-----------------------------------------------------------------------------------------
// <copyright file="SendDocumentLinksToCaseMap" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Malarvizhi</author>
//      <description>
//          Batch Job Class For Sending documnet Links to CaseMap
//      </description>
//      <changelog>
//          <date value="25-Apr-2011">Created</date>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//          <date value="05/09/2012">Bug fix 100297</date>
//          <date value="12/04/2012">Fix for bug 111563</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>  
//          <date value="04/02/2014">Bug fix 166867 </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.External.DataAccess.CaseMap;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.MiddleTier.CaseMap;

#endregion

namespace LexisNexis.Evolution.BatchJobs.SendDocumentLinksToCaseMap
{
    ///<summary>
    ///Batch Job Class For Converting DCB Links to CaseMap
    ///</summary>
    [Serializable]
    public class SendDocumentLinksToCaseMap : BaseJob<BaseJobBEO, SendDocumentLinksToCaseMapTaskBusinessEntity>
    {
        #region Private Fields

        /// <summary>
        /// Guid of the current user
        /// </summary>
        private string createdByUserGuid = string.Empty;

        /// <summary>
        /// Conversion ID for DCBLinksToCaseMap
        /// </summary>
        private long conversionId;

        /// <summary>
        /// Holds status of a job
        /// </summary>
        private bool isJobFailed;

        /// <summary>
        /// Name of the job request
        /// </summary>
        private string requestDescription = string.Empty;

        /// <summary>
        /// Started Time
        /// </summary>
        private DateTime startedTime = DateTime.UtcNow;

        /// <summary>
        /// Document count
        /// </summary>
        private long documentCount;

        /// <summary>
        /// Type of File
        /// </summary>
        private string fileType = string.Empty;

        #endregion

        #region Constructor

        #endregion

        #region Job Framework Functions

        /// <summary>
        /// Initializes Job BEO 
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="jobRunId">Job Run Identifier</param>
        /// <param name="bootParameters">Boot parameters</param>
        /// <param name="createdBy">Job created by</param>
        /// <returns>Job Business Entity</returns>
        protected override BaseJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            var jobBEO = new BaseJobBEO();
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobId.ToString(CultureInfo.InvariantCulture),
                    Constants.Event_Job_Initialize_Start, EventLogEntryType.Information);
                //Initialize the JobBEO object
                jobBEO.JobId = jobId;
                jobBEO.JobRunId = jobRunId;
                jobBEO.JobName = Constants.JobTypeName + " " + DateTime.UtcNow;
                //fetch UserEntity
                var userBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                createdByUserGuid = createdBy;
                if (userBusinessEntity != null)
                {
                    jobBEO.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A"))
                        ? userBusinessEntity.UserId
                        : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
                }
                jobBEO.JobTypeName = Constants.JobTypeName;
                // Default settings
                jobBEO.StatusBrokerType = BrokerType.Database;
                jobBEO.CommitIntervalBrokerType = BrokerType.ConfigFile;
                jobBEO.CommitIntervalSettingType = SettingType.CommonSetting;

                if (bootParameters != null)
                {
                    jobBEO.BootParameters = bootParameters;
                }
                else
                {
                    EvLog.WriteEntry(jobId + ":" + Constants.Event_Job_Initialize_Start, Constants.XmlNotWellFormed,
                        EventLogEntryType.Information);
                    throw new EVException().AddResMsg(ErrorCodes.XmlStringNotWellFormed);
                }
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobId.ToString(CultureInfo.InvariantCulture),
                    Constants.Event_Job_Initialize_Success, EventLogEntryType.Information);
            }
            catch (EVJobException ex)
            {
                isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInJobInitialization, string.Empty);
            }
            catch (Exception ex)
            {
                isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInJobInitialization, string.Empty);
            }
            return jobBEO;
        }

        /// <summary>
        /// Generates Concvert DCB Link tasks
        /// </summary>
        /// <param name="jobParameters">Job BEO</param>
        /// <returns>List of Job Tasks (BEOs)</returns>
        protected override Tasks<SendDocumentLinksToCaseMapTaskBusinessEntity> GenerateTasks(BaseJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            Tasks<SendDocumentLinksToCaseMapTaskBusinessEntity> tasks = null;
            previouslyCommittedTaskCount = 0;
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId,
                    Constants.Event_Job_GenerateTask_Start, EventLogEntryType.Information);
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobId, MethodBase.GetCurrentMethod().Name,
                    EventLogEntryType.Information);
                tasks = GetTaskList<BaseJobBEO, SendDocumentLinksToCaseMapTaskBusinessEntity>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;
                if (tasks.Count <= 0)
                {
                    var taskEntity =
                        (SendDocumentLinksToCaseMapTaskBusinessEntity)
                            XmlUtility.DeserializeObject(jobParameters.BootParameters,
                                typeof (SendDocumentLinksToCaseMapTaskBusinessEntity));
                    switch (taskEntity.CaseMapSource)
                    {
                        case SendDocumentLinksToCaseMapTaskBusinessEntity.Source.SearchResults:
                        {
                            if (taskEntity.DocumentLinkTasks.DocumentSelectionMode ==
                                DocumentLinksTaskEntity.SelectionMode.UseSelection)
                            {
                                documentCount = taskEntity.DocumentLinkTasks.DocumentIdList.Count;
                            }
                            else
                            {
                                documentCount = taskEntity.DocumentLinkTasks.SearchContext.TotalResultsCount -
                                                taskEntity.DocumentLinkTasks.DocumentIdList.Count;
                            }
                            break;
                        }
                        case SendDocumentLinksToCaseMapTaskBusinessEntity.Source.NearNative:
                        {
                            documentCount = 1;
                            break;
                        }
                        case SendDocumentLinksToCaseMapTaskBusinessEntity.Source.DocumentViewer:
                        {
                            documentCount = 1;
                            break;
                        }
                    }
                    var task = new SendDocumentLinksToCaseMapTaskBusinessEntity();
                    task.TaskNumber = 1;
                    task.TaskPercent = 100;
                    task.TaskComplete = false;
                    tasks.Add(task);
                }
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId,
                    Constants.Event_Job_GenerateTask_Success, EventLogEntryType.Information);
            }
            catch (EVJobException ex)
            {
                isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInGenerateTasks, string.Empty);
            }
            catch (Exception ex)
            {
                isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInGenerateTasks, string.Empty);
            }
            return tasks;
        }

        /// <summary>
        /// Does atomic 1)Gets document links2) Generate xml 3) Update xml file to database.
        /// </summary>
        /// <param name="task">ConvertDCBLinkTaskBusinessEntityObject</param>
        /// <param name="jobParameters">Job business entity</param>
        /// <returns></returns>
        protected override bool DoAtomicWork(SendDocumentLinksToCaseMapTaskBusinessEntity Task, BaseJobBEO jobParameters)
        {
            var StatusFlag = true; // Function return status.
            try
            {
                EvLog.WriteEntry(
                    Constants.JobTypeName + " - " + jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture),
                    Constants.Event_Job_DoAtomicWork_Start, EventLogEntryType.Information);
                var xml = string.Empty;
                var taskEntity =
                    (SendDocumentLinksToCaseMapTaskBusinessEntity)
                        XmlUtility.DeserializeObject(jobParameters.BootParameters,
                            typeof (SendDocumentLinksToCaseMapTaskBusinessEntity));
                switch (taskEntity.CaseMapSource)
                {
                    case SendDocumentLinksToCaseMapTaskBusinessEntity.Source.SearchResults:
                    {
                        var documentLinks = new List<DocumentLinkBEO>();
                        if (taskEntity.DocumentLinkTasks.DocumentSelectionMode ==
                            DocumentLinksTaskEntity.SelectionMode.UseSelection &&
                            taskEntity.DocumentLinkTasks.DocumentIdList != null)
                        {
                            documentLinks.AddRange(taskEntity.DocumentLinkTasks.DocumentIdList.Select(documentId =>
                                getDocumentLink(taskEntity.DocumentLinkTasks.SearchContext.MatterId,
                                    taskEntity.DocumentLinkTasks.SearchContext.CollectionId,
                                    documentId, taskEntity.DocumentLinkTasks.BaseLink,
                                    DocumentBO.GetDCNNumber(taskEntity.DocumentLinkTasks.SearchContext.MatterId,
                                        taskEntity.DocumentLinkTasks.SearchContext.CollectionId,
                                        documentId, jobParameters.JobScheduleCreatedBy
                                        )
                                    )));
                        }
                        else
                        {
                            var documentQueryEntity = new DocumentQueryEntity
                            {
                                QueryObject = new SearchQueryEntity
                                {
                                    ReviewsetId = taskEntity.DocumentLinkTasks.SearchContext.ReviewsetId,
                                    DatasetId = Convert.ToInt32(taskEntity.DocumentLinkTasks.SearchContext.DatasetId),
                                    MatterId = Convert.ToInt32(taskEntity.DocumentLinkTasks.SearchContext.MatterId),
                                    IsConceptSearchEnabled =
                                        taskEntity.DocumentLinkTasks.SearchContext.EnableConceptSearch
                                }
                            };
                            documentQueryEntity.QueryObject.QueryList.Add(
                                new Query(taskEntity.DocumentLinkTasks.SearchContext.Query));
                            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});
                            documentQueryEntity.IgnoreDocumentSnippet = true;
                            documentQueryEntity.TransactionName = "SendDocumentLinksToCaseMap - DoAtomicWork";
                            var searchResults = JobSearchHandler.GetAllDocuments(documentQueryEntity, false);
                            if (taskEntity.DocumentLinkTasks.DocumentIdList != null &&
                                taskEntity.DocumentLinkTasks.DocumentIdList.Count > 0)
                            {
                                documentLinks =
                                    searchResults.ResultDocuments.Where(
                                        x =>
                                            taskEntity.DocumentLinkTasks.DocumentIdList.Find(
                                                y => string.Compare(x.DocumentID, y, true) == 0) == null).
                                        Select(
                                            z =>
                                                getDocumentLink(z.MatterID.ToString(), z.CollectionID, z.DocumentID,
                                                    taskEntity.DocumentLinkTasks.BaseLink, z.DocumentControlNumber)
                                        ).ToList();
                            }
                            else
                            {
                                searchResults.ResultDocuments.SafeForEach(d => documentLinks.Add(
                                    getDocumentLink(d.MatterID.ToString(), d.CollectionID, d.DocumentID,
                                        taskEntity.DocumentLinkTasks.BaseLink, d.DocumentControlNumber)));
                            }
                        }


                        fileType = ApplicationConfigurationManager.GetValue(Constants.SearchResultsFileType);
                        xml = DocumentFactBusinessObject.GenerateDocumentLinksXml(documentLinks,
                            jobParameters.JobScheduleCreatedBy);

                        break;
                    }
                    case SendDocumentLinksToCaseMapTaskBusinessEntity.Source.DocumentViewer:
                    {
                        var documentFactLinks = taskEntity.DocumentFactLinks;
                        fileType = ApplicationConfigurationManager.GetValue(Constants.DocumentViewerFileType);
                        xml = DocumentFactBusinessObject.GenerateDocumentFactXml(documentFactLinks,
                            jobParameters.JobScheduleCreatedBy);

                        break;
                    }
                    case SendDocumentLinksToCaseMapTaskBusinessEntity.Source.NearNative:
                    {
                        var documentFactLinks = taskEntity.DocumentFactLinks;
                        fileType = ApplicationConfigurationManager.GetValue(Constants.NearNativeFileType);
                        xml = DocumentFactBusinessObject.GenerateDocumentFactXml(documentFactLinks,
                            jobParameters.JobScheduleCreatedBy);

                        break;
                    }
                }

                // Perform Atomic Task
                var encoding = new UTF8Encoding();
                var content = encoding.GetBytes(xml);
                var nameBuilder = new StringBuilder();
                nameBuilder.Append(Constants.TaskName);
                nameBuilder.Append(Constants.OnDate);
                nameBuilder.Append(startedTime.ConvertToUserTime());
                var requestName = nameBuilder.ToString();

                nameBuilder = new StringBuilder();
                nameBuilder.Append(Constants.TaskName);
                nameBuilder.Append(Constants.RequestByUser);
                nameBuilder.Append(jobParameters.JobScheduleCreatedBy);
                nameBuilder.Append(Constants.OnDate);
                nameBuilder.Append(startedTime.ConvertToUserTime());
                requestDescription = nameBuilder.ToString();


                conversionId = CaseMapDAO.SaveConversionResults(jobParameters.JobRunId, requestName, requestDescription,
                    content, fileType, createdByUserGuid);
                StatusFlag = true;
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId,
                    Constants.Event_Job_DoAtomicWork_Success, EventLogEntryType.Information);
            }
            catch (EVTaskException ex)
            {
                isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(TaskLogInfo, ex, LogCategory.Task, ErrorCodes.ProblemInDoAtomicWork, string.Empty);
            }
            catch (Exception ex)
            {
                isJobFailed = true;
                // Handle exception in Generate Tasks
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(TaskLogInfo, ex, LogCategory.Task, ErrorCodes.ProblemInDoAtomicWork, string.Empty);
            }
            return StatusFlag;
        }

        /// <summary>
        /// Before job shuts down, shall update job next run
        /// </summary>
        /// <param name="jobParameters">Job Business Object</param>
        protected override void Shutdown(BaseJobBEO jobParameters)
        {
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobId, Constants.Event_Job_ShutDown,
                    EventLogEntryType.Information);

                #region Notification section

                //get job details
                var jobDetails = JobMgmtBO.GetJobDetails(jobParameters.JobId.ToString());
                if (jobDetails != null && jobDetails.NotificationId > 0)
                {
                    var defaultMessage = string.Empty;
                    defaultMessage = isJobFailed
                        ? string.Format(Constants.NotificationErrorMessageFormat,
                            !string.IsNullOrEmpty(requestDescription) ? requestDescription : Constants.TaskName)
                        : documentCount > 0
                            ? string.Format(Constants.NotificationSuccessMessageFormat,
                                !string.IsNullOrEmpty(requestDescription)
                                    ? requestDescription
                                    : Constants.TaskName, documentCount,
                                ApplicationConfigurationManager.GetValue(Constants.CaseMapUrl), conversionId, fileType)
                            : string.Format(Constants.NotificationSuccessMessageFormatZeroDocs,
                                !string.IsNullOrEmpty(requestDescription) ? requestDescription : Constants.TaskName);
                    CustomNotificationMessage = defaultMessage;
                }
                JobLogInfo.AddParameters(Constants.CreatedBy, jobParameters.JobScheduleCreatedBy);
                JobLogInfo.AddParameters(Constants.DocumentIncludedInXml, Convert.ToString(documentCount));

                #endregion
            }
            catch (EVJobException ex)
            {
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInShutDown, string.Empty);
            }
            catch (Exception ex)
            {
                isJobFailed = true;
                // Handle exception in Generate Tasks
                EvLog.WriteEntry(Constants.JobTypeName + MethodBase.GetCurrentMethod().Name, ex.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInShutDown, string.Empty);
            }
        }

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="logInfo">Log information</param>
        /// <param name="exp">exception received</param>        
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="errocode">error code</param>      
        /// <param name="taskKey">taskKey</param> 
        private static void LogException(LogInfo logInfo, Exception exp, LogCategory category, string errorCode,
            string taskKey)
        {
            if (category == LogCategory.Job)
            {
                var jobException = new EVJobException(errorCode, exp, logInfo);
                throw (jobException);
            }
            logInfo.TaskKey = taskKey;
            var taskException = new EVTaskException(errorCode, exp, logInfo);
            throw (taskException);
        }

        /// <summary>
        /// Gets a DocumentLinkBEO
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="collectionId">Collection Id</param>
        /// <param name="documentId">Document Id</param>
        /// <param name="baseLink">Base URL to Link back</param>
        /// <returns>Document Link Business Entity</returns>
        private static DocumentLinkBEO getDocumentLink(string matterId, string collectionId, string documentId,
            string baseLink, string dcn)
        {
            var documentLink = new DocumentLinkBEO();
            var identifier = new DocumentIdentifierBEO(matterId, collectionId, documentId);
            identifier.DCN = dcn;
            documentLink.DocumentFact = identifier;
            documentLink.UrlApplicationLink = baseLink + identifier.UniqueIdentifier;
            return documentLink;
        }

        #endregion
    }
}