#region Header

//-----------------------------------------------------------------------------------------
// <copyright file=""FindAndReplaceJob.cs"" company=""Lexis Nexis"">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Baranir</author>
//      <description>
//          This file contains the FindAndReplaceJob methods
//      </description>
//      <changelog>
//          <date value="11-04-2012">Bug Fix #98767</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//          <date value="09/23/2014">Task # 174614 - Refactor Velocity calls into one place External.Search.Dll : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.MatterManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.TraceServices;

#endregion

namespace LexisNexis.Evolution.BatchJobs.GlobalReplace
{
    /// <summary>
    ///     This class represents the global find and replace job
    /// </summary>
    [Serializable]
    public class FindandReplaceJob : BaseJob<GlobalReplaceJobBEO, GlobalReplaceTaskBEO>
    {
        #region Initialize

        /// <summary>
        ///     variable to hold the total occurrences of the word
        /// </summary>
        private int _mOccurrences;

        /// <summary>
        ///     Variable to hold the value if find text exists
        /// </summary>
        private bool _mIsFindTextExist;

        /// <summary>
        ///     Set the page size for no. of document handle as part of each task
        /// </summary>
        private int _mTaskBatchSize;

        /// <summary>
        ///     estimated time to index the documents
        /// </summary>
        private double _mEstimatedTimeToIndexInMinutes;

        /// <summary>
        ///     Orginator Id for crawling
        /// </summary>
        private RVWDocumentFieldBEO _mOrginatorField;

        /// <summary>
        ///     Matter Details
        /// </summary>
        private MatterBEO _mMatter;


        /// <summary>
        ///     Constructor - Initialize private objects.
        /// </summary>
        public FindandReplaceJob()
        {
            _mTaskBatchSize = 10;
        }

        /// <summary>
        ///     This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy">string</param>
        /// <returns>GlobalReplaceJobBEO</returns>
        protected override GlobalReplaceJobBEO Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            GlobalReplaceJobBEO jobBeo = null;
            try
            {
                LogMessage(Constants.InitializationStartMessage, false, LogCategory.Job, null);
                LogMessage(Constants.InitializationStartMessage, GetType(),
                    "LexisNexis.Evolution.BatchJobs.FindandReplaceJob.Initialize", EventLogEntryType.Information, jobId,
                    jobRunId);

                // Initialize the JobBEO
                jobBeo = new GlobalReplaceJobBEO
                {
                    JobId = jobId,
                    JobRunId = jobRunId,
                    JobScheduleCreatedBy = createdBy,
                    JobTypeName = Constants.Job_TYPE_NAME,
                    BootParameters = bootParameters,
                    JobName = Constants.JOB_NAME,
                    StatusBrokerType = BrokerType.Database,
                    CommitIntervalBrokerType = BrokerType.ConfigFile,
                    CommitIntervalSettingType = SettingType.CommonSetting
                };

                //filling properties of the job parameter

                // Default settings

                //constructing GlobalReplaceBEO from boot parameter by de serializing
                GlobalReplaceBEO globalReplaceContextBeo = GetGlobalReplaceBEO(bootParameters);

               
                globalReplaceContextBeo.CreatedBy = createdBy;

                // Set output batch size
                _mTaskBatchSize =
                    Convert.ToInt16(ApplicationConfigurationManager.GetValue(Constants.ResultsPageSize));

                EvLog.WriteEntry(jobId + Constants.AUDIT_BOOT_PARAMETER_KEY, Constants.AUDIT_BOOT_PARAMETER_VALUE,
                    EventLogEntryType.Information);
                jobBeo.SearchContext = globalReplaceContextBeo.SearchContext;
                jobBeo.ActualString = globalReplaceContextBeo.ActualString;
                jobBeo.ReplaceString = globalReplaceContextBeo.ReplaceString;
                _mOrginatorField = new RVWDocumentFieldBEO
                {
                    FieldName = Constants.OrginatorFieldName,
                    FieldValue = Guid.NewGuid().ToString(),
                    FieldId = -1
                };
                _mMatter = MatterDAO.GetMatterDetails(globalReplaceContextBeo.SearchContext.MatterId.ToString());
            }
            catch (EVException ex)
            {
                EvLog.WriteEntry(jobId + " - " + Constants.InitializationFailMessage, ex.ToUserString(),
                    EventLogEntryType.Error);
                LogException(jobId, ex, Constants.InitializationFailMessage, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInJobInitialization);
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(jobId + " - " + jobId.ToString(CultureInfo.InvariantCulture),
                    Constants.InitializationFailMessage + ":" + exp.Message, EventLogEntryType.Information);
                LogException(jobId, exp, Constants.InitializationFailMessage, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInJobInitialization);
            }
            return jobBeo;
        }

        #endregion

        #region GenerateTasks

        /// <summary>
        ///     This is the overridden GenerateTasks() method.
        /// </summary>
        /// <param name="jobParameters">GlobalReplaceJobBEO</param>
        /// <param name="previouslyCommittedTaskCount">int</param>
        /// <returns>list of GlobalReplaceTaskBEO object</returns>
        protected override Tasks<GlobalReplaceTaskBEO> GenerateTasks(GlobalReplaceJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            var tasks = new Tasks<GlobalReplaceTaskBEO>();
            previouslyCommittedTaskCount = 0;
            try
            {
                LogMessage(Constants.TaskGenerationStartedMessage, false, LogCategory.Job, null);
                LogMessage(Constants.TaskGenerationStartedMessage, GetType(),
                    "LexisNexis.Evolution.BatchJobs.FindandReplaceJob.GenerateTasks", EventLogEntryType.Information,
                    jobParameters.JobId, jobParameters.JobRunId);

                var searchQueryEntity = new SearchQueryEntity
                {
                    MatterId = jobParameters.SearchContext.MatterId,
                    ReviewsetId = jobParameters.SearchContext.ReviewSetId,
                    DatasetId = jobParameters.SearchContext.DataSetId,
                    IsConceptSearchEnabled = jobParameters.SearchContext.IsConceptSearchEnabled
                };
                searchQueryEntity.TransactionName = "FindandReplaceJob - GenerateTasks (GetCount)";
                searchQueryEntity.QueryList.Add(new Query(jobParameters.SearchContext.Query));
                long totalResultCount = JobSearchHandler.GetSearchResultsCount(searchQueryEntity);

                if (totalResultCount > 0)
                {
                    //estimated enqueue time for a document is 30 seconds
                    _mEstimatedTimeToIndexInMinutes = (totalResultCount*30)/60;
                    for (int pageno = 1;; pageno++)
                    {
                        var task = new GlobalReplaceTaskBEO
                        {
                            PageNumber = pageno - 1,
                            PageSize = _mTaskBatchSize,
                            TaskNumber = pageno,
                            SearchQueryObject = searchQueryEntity,
                            TaskPercent = 100/Math.Ceiling((float) totalResultCount/_mTaskBatchSize),
                            TaskComplete = false,
                            ActualString = jobParameters.ActualString,
                            ReplaceString = jobParameters.ReplaceString
                        };
                        tasks.Add(task);
                        //pageno * _taskBatchSize exhaust
                        if (pageno*_mTaskBatchSize >= totalResultCount)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    CustomNotificationMessage = string.Format(Constants.SearchTextNotFound,
                        jobParameters.ActualString);
                }
                if (tasks.Count == 0)
                {
                    LogMessage(Constants.NoTaskToExecuteError, GetType(),
                        "LexisNexis.Evolution.BatchJobs.FindandReplaceJob.GenerateTasks", EventLogEntryType.Information,
                        jobParameters.JobId, jobParameters.JobRunId);
                }
                LogMessage(string.Format(Constants.TaskGenerationCompletedMessage, tasks.Count), false,
                    LogCategory.Job, null);
                LogMessage(string.Format(Constants.TaskGenerationCompletedMessage, tasks.Count), GetType(),
                    "LexisNexis.Evolution.BatchJobs.FindandReplaceJob.GenerateTasks", EventLogEntryType.Information,
                    jobParameters.JobId, jobParameters.JobRunId);
            }
            catch (EVException ex)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + " : " + jobParameters.JobId + Constants.TaskGenerationFails,
                    ex.ToUserString(), EventLogEntryType.Error);
                LogException(jobParameters.JobId, ex, string.Empty, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + " : " + jobParameters.JobId + Constants.TaskGenerationFails,
                    exp.Message, EventLogEntryType.Error);
                LogException(jobParameters.JobId, exp, string.Empty, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
            return tasks;
        }

        #endregion

        #region Do Atomic Work

        /// <summary>
        ///     This is the overridden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <returns>Status of the operation.</returns>
        protected override bool DoAtomicWork(GlobalReplaceTaskBEO task, GlobalReplaceJobBEO jobParameters)
        {
            bool isSuccess = false;
            string taskKey = string.Empty;
            try
            {
                LogMessage(string.Format(Constants.DoAtomicWorkStartMessage, task.TaskNumber), false,
                    LogCategory.Job, null);
                LogMessage(string.Format(Constants.DoAtomicWorkStartMessage, task.TaskNumber), GetType(),
                    Constants.DoAtomicWorkNamespace, EventLogEntryType.Information, jobParameters.JobId,
                    jobParameters.JobRunId);

                var documentQueryEntity = new DocumentQueryEntity
                {
                    DocumentCount = task.PageSize,
                    DocumentStartIndex = task.PageNumber*task.PageSize,
                    QueryObject = task.SearchQueryObject
                };
                documentQueryEntity.IgnoreDocumentSnippet = true;
                documentQueryEntity.TransactionName = "FindAndReplaceJob - DoAtomicWork";
                ReviewerSearchResults reviewerSearchResults = JobSearchHandler.GetSearchResults(documentQueryEntity);

                LogMessage(string.Format(Constants.SearchDoneForTask, task.TaskNumber), false, LogCategory.Job,
                    null);
                LogMessage(string.Format(Constants.SearchDoneForTask, task.TaskNumber), GetType(),
                    Constants.DoAtomicWorkNamespace, EventLogEntryType.Information, jobParameters.JobId,
                    jobParameters.JobRunId);

                if (reviewerSearchResults.ResultDocuments != null && reviewerSearchResults.ResultDocuments.Count > 0)
                {
                    foreach (DocumentResult document in reviewerSearchResults.ResultDocuments)
                    {
                        List<RVWDocumentFieldBEO> documentFieldBeoList = GetFieldValuesToUpdate
                            (document, task.ActualString, task.ReplaceString, jobParameters.JobScheduleCreatedBy);
                        if (documentFieldBeoList != null && documentFieldBeoList.Count > 0)
                        {
                            var documentData = new RVWDocumentBEO
                            {
                                MatterId = document.MatterID,
                                CollectionId = document.CollectionID
                            };
                            taskKey = Constants.CollectionId + document.CollectionID;
                            documentData.DocumentId = document.DocumentID;
                            taskKey += Constants.DocumentId + document.DocumentID;
                            documentData.ModifiedBy = jobParameters.JobScheduleCreatedBy;
                            documentData.ModifiedDate = DateTime.UtcNow;
                            documentFieldBeoList.SafeForEach(x => documentData.FieldList.Add(x));

                            _mIsFindTextExist = true;
                            //Update the field value information in vault for the appropriate fields matching                              
                            isSuccess =
                                DocumentService.UpdateDocumentFields(
                                    document.MatterID.ToString(CultureInfo.InvariantCulture), document.CollectionID,
                                    document.DocumentID, documentData);
                            if (!isSuccess)
                            {
                                break;
                            }
                        }
                    }
                }

                CustomNotificationMessage = !_mIsFindTextExist
                    ? string.Format(Constants.SearchTextNotFound, task.ActualString)
                    : string.Format(Constants.SearchTextFound, _mOccurrences);

                LogMessage(string.Format(Constants.DoAtomicWorkCompletedMessage, task.TaskNumber), false,
                    LogCategory.Job, null);
                LogMessage(string.Format(Constants.DoAtomicWorkCompletedMessage, task.TaskNumber), GetType(),
                    Constants.DoAtomicWorkNamespace, EventLogEntryType.Information, jobParameters.JobId,
                    jobParameters.JobRunId);
            }
            catch (EVException ex)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + Constants.DoAtomicWorkFailMessage,
                    ex.ToUserString() + Constants.TaskNumber + task.TaskNumber, EventLogEntryType.Error);
                LogException(jobParameters.JobId, ex, string.Empty, LogCategory.Task, taskKey,
                    ErrorCodes.ProblemInDoAtomicWork);
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + Constants.DoAtomicWorkFailMessage,
                    exp.Message + Constants.TaskNumber + task.TaskNumber, EventLogEntryType.Error);
                LogException(jobParameters.JobId, exp, string.Empty, LogCategory.Task, taskKey,
                    ErrorCodes.ProblemInDoAtomicWork);
            }
            return isSuccess;
        }

        #endregion

        #region Shout Down

        /// <summary>
        ///     This is the overriden Shutdown() method.
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        protected override void Shutdown(GlobalReplaceJobBEO jobParameters)
        {
            try
            {
                
                LogMessage(Constants.ShutdownLogMessage, false, LogCategory.Job, null);
                GetGlobalReplaceBEO(jobParameters.BootParameters);

                JobLogInfo.CustomMessage = Constants.JobSummary;
                JobLogInfo.AddParameters(Constants.JobName, Constants.JOB_NAME);
                JobLogInfo.AddParameters(Constants.EV_AUDIT_ACTUAL_STRING, jobParameters.ActualString);
                JobLogInfo.AddParameters(Constants.EV_AUDIT_REPLACE_STRING, jobParameters.ReplaceString);
            }
            catch (EVException ex)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + Constants.ShutdownErrorMessage, ex.ToUserString(),
                    EventLogEntryType.Error);
                LogException(jobParameters.JobId, ex, Constants.ShutdownErrorMessage, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInJobExecution);
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + Constants.ShutdownErrorMessage, ex.Message,
                    EventLogEntryType.Error);
                LogException(jobParameters.JobId, ex, Constants.ShutdownErrorMessage, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInJobExecution);
            }
            finally
            {
                _mMatter = null;
                _mOrginatorField = null;
            }
        }

        #endregion

        #region Helper Methods

       
        /// <summary>
        ///     This method will return GetGlobalReplaceBEO out of the passed bootparamter
        /// </summary>
        /// <param name="bootParamter">String</param>
        /// <returns>GlobalReplaceBEO</returns>
        private static GlobalReplaceBEO GetGlobalReplaceBEO(String bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            using (var stream = new StringReader(bootParamter))
            {
                //Ceating xmlStream for xmlserialization
                var xmlStream = new XmlSerializer(typeof (GlobalReplaceBEO));

                //Deserialization of bootparameter to get profileBEO
                return (GlobalReplaceBEO) xmlStream.Deserialize(stream);
            }
        }

        /// <summary>
        ///     Methos to identify the field's to which values are to be updated
        /// </summary>
        /// <param name="document">DocumentResult</param>
        /// <param name="actualString">string</param>
        /// <param name="replaceString">string</param>
        /// <param name="userGuid">string</param>
        /// <returns>List<RVWDocumentFieldBEO /></returns>
        private List<RVWDocumentFieldBEO> GetFieldValuesToUpdate(DocumentResult document,
            string actualString, string replaceString, string userGuid)
        {
            var documentFields = new List<RVWDocumentFieldBEO>();
            RVWDocumentBEO documentData =
                DocumentService.GetDocumentData(document.MatterID.ToString(CultureInfo.InvariantCulture),
                    document.CollectionID, document.DocumentID, userGuid, "false");

            if (documentData != null && !documentData.IsLocked && documentData.FieldList.Count > 0)
            {
                //Filters the system and read only fields
                IEnumerable<RVWDocumentFieldBEO> editableFields = documentData.FieldList.Where(x => !x.IsReadable);
                if (editableFields.Any())
                {
                    List<RVWDocumentFieldBEO> fields = editableFields.ToList();
                    documentData.FieldList.Clear();
                    fields.SafeForEach(y => documentData.FieldList.Add(y));
                }

                //Loop through the fields and update the field value accordingly 
                foreach (RVWDocumentFieldBEO documentFieldValue in documentData.FieldList)
                {
                    if (documentFieldValue.FieldType.DataTypeId == Constants.ContentFieldType)
                    {
                        documentFieldValue.FieldValue = DecodeContentField(documentFieldValue.FieldValue);
                    }
                    if (!string.IsNullOrEmpty(documentFieldValue.FieldValue))
                    {
                        if (documentFieldValue.FieldValue.ToLowerInvariant().Contains(actualString.ToLowerInvariant()))
                        {
                            var reg = new Regex(actualString.ToLowerInvariant(), RegexOptions.Multiline);
                            MatchCollection theMatches = reg.Matches(documentFieldValue.FieldValue.ToLowerInvariant());
                            _mOccurrences += theMatches.Count;
                            documentFields.Add(new RVWDocumentFieldBEO
                            {
                                FieldId = documentFieldValue.FieldId,
                                FieldName = documentFieldValue.FieldName,
                                FieldValue = documentFieldValue.FieldValue.ToLowerInvariant().
                                    Replace(actualString.ToLowerInvariant(), replaceString),
                                FieldTypeId = documentFieldValue.FieldType.DataTypeId
                            });
                        }
                    }
                }
            }
            return documentFields;
        }

        /// <summary>
        ///     Decodes the content field
        /// </summary>
        /// <param name="content">string</param>
        /// <returns>string</returns>
        private string DecodeContentField(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                byte[] documentBytes = Convert.FromBase64String(content);
                if (documentBytes.Length > 0)
                {
                    Decoder utfDecode = new UTF8Encoding().GetDecoder();
                    var decodedChar = new char[utfDecode.GetCharCount(documentBytes, 0, documentBytes.Length)];
                    utfDecode.GetChars(documentBytes, 0, documentBytes.Length, decodedChar, 0);
                    content = new string(decodedChar);
                }
            }
            return content;
        }

        #endregion

        #region Handle Exception

        /// <summary>
        ///     Logs the message.
        /// </summary>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="isError">if set to <c>true</c> [is error].</param>
        /// <param name="category">The category.</param>
        /// <param name="additinalDetail">The additinal detail.</param>
        private void LogMessage(string customMessage, bool isError, LogCategory category,
            List<KeyValuePair<string, string>> additinalDetail)
        {
            if (additinalDetail != null && additinalDetail.Count > 0)
            {
                foreach (var keyValue in additinalDetail)
                {
                    switch (category)
                    {
                        case LogCategory.Job:
                            JobLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                            break;
                        default:
                            TaskLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                            break;
                    }
                }
            }
            switch (category)
            {
                case LogCategory.Job:
                    JobLogInfo.AddParameters(customMessage);
                    JobLogInfo.IsError = isError;
                    break;
                case LogCategory.Task:
                    TaskLogInfo.AddParameters(customMessage);
                    TaskLogInfo.IsError = isError;
                    break;
            }
        }

        /// <summary>
        ///     Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of
        ///     logging messages.
        /// </summary>
        /// <param name="message"> Message to be logged</param>
        /// <param name="consumerClass"> Import job class type using this function </param>
        /// <param name="messageLocation"> Location from which message is being logged - normally it's function name </param>
        /// <param name="eventLogEntryType"> Error or Message or Audit entry </param>
        /// <param name="jobId"> Job Identifier </param>
        /// <param name="jobRunId"> Job instance identifier </param>
        public static void LogMessage(string message, Type consumerClass, string messageLocation,
            EventLogEntryType eventLogEntryType, int jobId, int jobRunId)
        {
            //// Errors are always logged, if levels of logging is set to true, events are always logged.          
            EvLog.WriteEntry(consumerClass.ToString(),
                "Job ID: " + jobId
                + Constants.NextLineCharacter + "Job Run ID: " + jobRunId
                + Constants.NextLineCharacter + "Location: " + messageLocation
                + Constants.NextLineCharacter + ((message.Equals(string.Empty)) ? string.Empty : "Details: " + message),
                eventLogEntryType);
        }

        /// <summary>
        ///     Logs the exception message.
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="exp">exception received</param>
        /// <param name="msg">message to be logged</param>
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key value pair to identify the Task, need for task log only</param>
        /// <param name="errorCode">string</param>
        private void LogException(int jobId, Exception exp, string msg, LogCategory category, string taskKey,
            string errorCode)
        {
            if (category == LogCategory.Job)
            {
                JobLogInfo.AddParameters(jobId + msg);
                var jobException = new EVJobException(errorCode, exp, JobLogInfo);
                throw (jobException);
            }
            TaskLogInfo.AddParameters(jobId + msg);
            TaskLogInfo.TaskKey = taskKey;
            var taskException = new EVTaskException(errorCode, exp, TaskLogInfo);
            throw (taskException);
        }

        #endregion
    }
}