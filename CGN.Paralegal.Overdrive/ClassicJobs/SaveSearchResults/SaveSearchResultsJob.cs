#region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="SaveSearchResultsJob" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish Kumar</author>
//      <description>
//          SaveSearchResultsJob Class File
//      </description>
//      <changelog>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//          <date value="2/2/2012">Fix for bug 95162</date>
//          <date value="3/20/2012">Fix to change job to INPROC process type</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="11\29\2012">Fix for bug 112025</date>
//          <date value="2/21/2013">Fix for bug 127359 - Saved Search Results Slowness Issue</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
// </header>
//----------------------------------------------------------------------------------------- 

#endregion

#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Web;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Repository.Service.Contracts.SearchResults;
using LexisNexis.Evolution.Repository.Service.Implementation.SearchResults;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Infrastructure.Common;
using System.Configuration;

#endregion

namespace LexisNexis.Evolution.BatchJobs.SaveSearchResults
{
    ///<summary>
    ///SaveSearchResultsJob Class File
    ///</summary>
    [Serializable]
    public class SaveSearchResultsJob : BaseJob<SaveSearchResultsJobBEO, SaveSearchResultsJobTaskBEO>
    {
        private readonly SaveSearchResultsJobBEO _jobBeo; // Job level data

        /// <summary>
        /// Reference to resource file on the project
        /// </summary>
        /// <summary>
        /// Set the page size for no. of document handle as part of each
        /// task
        /// </summary>
        private int _taskBatchSize;

        ///Constants
        internal const int MaxDocChunkSize = 500;

        /// <summary>
        /// Reference to search results properties pass as boot parameter
        /// </summary>
        private SearchResultsPropertiesBEO _searchResultsProperty;

        /// <summary>
        /// User Business Entity oBject
        /// </summary>
        private UserBusinessEntity _userEntityOfJobOwner;

        /// <summary>
        /// Dcn list
        /// </summary>
        private readonly List<string> _dcnList;

        /// <summary>
        /// Search results service object
        /// </summary>
        [NonSerialized] private readonly SearchResultsServiceImplementation searchResultsService;

        /// <summary>
        /// Search results service property
        /// </summary>
        public SearchResultsServiceImplementation SearchResultsService
        {
            get { return searchResultsService; }
        }

        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>        
        public SaveSearchResultsJob()
        {
            //Initialize the Private variables
            _jobBeo = new SaveSearchResultsJobBEO();
            _taskBatchSize = 10; // Default Value
            _dcnList = new List<string>();

            #region Initialize searchResultsService

            searchResultsService = new SearchResultsServiceImplementation();
            EVUnityContainer.RegisterInstance(Constants.SearchResultService, searchResultsService);

            #endregion
        }

        /// <summary>
        /// This property retrieves the search chunk size for fetching document details from search engine
        /// </summary>
        /// <returns></returns>
        private static int GetMaximumDocumentChunkSize()
        {
            try
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("SEARCH_MAX_CHUNKSIZE"));
            }
            catch (Exception)
            {
                return MaxDocChunkSize;
            }
        }

        #region Job Framework Functions

        /// <summary>
        /// Initializes Job BEO 
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="jobRunId">Job Run Identifier</param>
        /// <param name="bootParameters">Boot parameters</param>
        /// <param name="createdBy">Job created by</param>
        /// <returns>Job Business Entity</returns>
        protected override SaveSearchResultsJobBEO Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            LogMessage(Constants.InitializationStartMessage, false, LogCategory.Job, null);
            LogMessage(Constants.InitializationStartMessage, GetType(), Constants.InitializeMethodFullName,
                EventLogEntryType.Information, jobId, jobRunId);
            _taskBatchSize = GetMaximumDocumentChunkSize();


            //Constructing Search Results property Beo from passed boot paramter
            _searchResultsProperty =
                (SearchResultsPropertiesBEO)
                    XmlUtility.DeserializeObject(bootParameters, typeof (SearchResultsPropertiesBEO));

            //Initialize the JobBEO object
            _jobBeo.JobName = _searchResultsProperty.SearchResultsName;
            _jobBeo.JobDescription = _searchResultsProperty.SearchResultsDescription;
            _jobBeo.SearchQueryId = _searchResultsProperty.SearchResultsId;
            _userEntityOfJobOwner = UserBO.GetUserUsingGuid(createdBy);
            _jobBeo.JobScheduleCreatedBy = (_userEntityOfJobOwner.DomainName.Equals("N/A"))
                ? _userEntityOfJobOwner.UserId
                : _userEntityOfJobOwner.DomainName + "\\" + _userEntityOfJobOwner.UserId;
            _jobBeo.JobTypeName = Constants.JobTypeName;
            _jobBeo.JobId = jobId;
            _jobBeo.JobRunId = jobRunId;
            _jobBeo.DocumentQuery = _searchResultsProperty.DocumentQuery;
            return _jobBeo;
        }


        /// <summary>
        /// Generates Save Search Results Job tasks
        /// </summary>
        /// <param name="jobParameters">Job BEO</param>
        /// <param name="previouslyCommittedTaskCount">The previously committed task count.</param>
        /// <returns>
        /// List of Job Tasks (BEOs)
        /// </returns>
        protected override Tasks<SaveSearchResultsJobTaskBEO> GenerateTasks(SaveSearchResultsJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            var tasks = new Tasks<SaveSearchResultsJobTaskBEO>();
            previouslyCommittedTaskCount = 0;
            LogMessage(Constants.TaskGenerationStartedMessage, false, LogCategory.Job, null);
            LogMessage(Constants.TaskGenerationStartedMessage, GetType(), Constants.GenerateTaskMethodFullName,
                EventLogEntryType.Information, jobParameters.JobId, jobParameters.JobRunId);

            jobParameters.DocumentQuery.IgnoreDocumentSnippet = true;
            jobParameters.DocumentQuery.OutputFields.Clear();
            jobParameters.DocumentQuery.OutputFields.Add(new Field { FieldName = EVSystemFields.DcnField });
            jobParameters.DocumentQuery.QueryObject.TransactionName = "SaveSearchResultsJob - GenerateTasks (GetCount)";
            var totalResultCount = JobSearchHandler.GetSearchResultsCount(jobParameters.DocumentQuery.QueryObject);
            // getting total no. of documents present in current search List


            for (var pageno = 1;; pageno++)
            {
                var task = new SaveSearchResultsJobTaskBEO
                {
                    PageNo = pageno - 1,
                    PageSize = _taskBatchSize,
                    TaskNumber = pageno,
                    TaskPercent =
                        100/
                        Math.Ceiling((float) totalResultCount/
                                     _taskBatchSize)
                };
                tasks.Add(task);
                //pageno * _taskBatchSize exhaust
                if (pageno*_taskBatchSize >= totalResultCount)
                    break;
            }

            if (tasks.Count == 0)
            {
                LogMessage(Constants.NoTaskToExecuteError, GetType(), Constants.GenerateTaskMethodFullName,
                    EventLogEntryType.Information, jobParameters.JobId, jobParameters.JobRunId);
            }
            LogMessage(string.Format(Constants.TaskGenerationCompletedMessage, tasks.Count), false, LogCategory.Job,
                null);
            LogMessage(string.Format(Constants.TaskGenerationCompletedMessage, tasks.Count), GetType(),
                Constants.GenerateTaskMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                jobParameters.JobRunId);
            return tasks;
        }

        /// <summary>
        /// Atomic work 1) Search 2) Insert to DB 3) Audit Log.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(SaveSearchResultsJobTaskBEO task, SaveSearchResultsJobBEO jobParameters)
        {
            #region Pre-condition asserts

            task.ShouldNotBe(null);
            jobParameters.ShouldNotBe(null);

            #endregion

            var searchResultsDataEntityObject = new SearchResultsDataContract();
            var searchContext = jobParameters.DocumentQuery;
            searchContext.DocumentStartIndex = (task.PageNo*task.PageSize);
            searchContext.DocumentCount = task.PageSize;
            searchContext.IgnoreDocumentSnippet = true;
            searchContext.OutputFields.Clear();
            searchContext.OutputFields.Add(new Field { FieldName = EVSystemFields.DcnField});
            LogMessage(string.Format(Constants.DoAtomicWorkStartMessage, task.TaskNumber), false, LogCategory.Job, null);
            LogMessage(string.Format(Constants.DoAtomicWorkStartMessage, task.TaskNumber), GetType(),
                Constants.DoAtomicMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                jobParameters.JobRunId);
            try
            {
                // Perform Atomic Task
                //ReviewerSearchResults rvwSearchBeo = JobSearchHandler.GetSearchResultsWithMatchContext(searchContext);
                // we get the search results from   search sub-system in batches of 1000
                searchContext.TransactionName = "SaveSearchResultJob - DoAtomicWork";
                var rvwSearchBeo = JobSearchHandler.GetSearchResults(searchContext);

                LogMessage(string.Format(Constants.SearchDoneForTask, task.TaskNumber), false, LogCategory.Job, null);
                LogMessage(string.Format(Constants.SearchDoneForTask, task.TaskNumber), GetType(),
                    Constants.DoAtomicMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                    jobParameters.JobRunId);
                if (rvwSearchBeo != null)
                {
                    if (rvwSearchBeo.ResultDocuments != null && rvwSearchBeo.ResultDocuments.Any())
                    {
                        searchResultsDataEntityObject = new SearchResultsDataContract();

                        foreach (var searchDocument in rvwSearchBeo.ResultDocuments)
                        {
                            var documentDetail =
                                new SearchResultDocumentDetailsDataContract
                                {
                                    DocumentId = searchDocument.DocumentID,
                                    DocumentControlNumber = searchDocument.DocumentControlNumber,
                                    NativeFilePath = GetNativeFilePath(searchDocument),
                                    DatasetId = _jobBeo.DocumentQuery.QueryObject.DatasetId
                                };
                            searchResultsDataEntityObject.Information = new SearchResultsInformationDataContract
                            {
                                Properties = new SearchResultsPropertiesDataContract
                                {
                                    SearchResultId = _jobBeo.SearchQueryId,
                                    DocumentQuery = _jobBeo.DocumentQuery
                                },
                                CreatedBy = _userEntityOfJobOwner.UserGUID,
                                CreatedDate = DateTime.UtcNow
                            };
                            documentDetail.SearchHitCount = searchDocument.SearchHitCount;
                            searchResultsDataEntityObject.Details.Add(documentDetail);
                        }
                        // perform db insertion
                        LogMessage(Constants.InsertDocumentDetailMessage, false, LogCategory.Job, null);
                        LogMessage(Constants.InsertDocumentDetailMessage, GetType(), Constants.DoAtomicMethodFullName,
                            EventLogEntryType.Information, jobParameters.JobId, jobParameters.JobRunId);

                        #region Audit for each document

                        if (rvwSearchBeo.ResultDocuments != null && rvwSearchBeo.ResultDocuments.Count > 0)
                        {
                            foreach (var searchDocument in rvwSearchBeo.ResultDocuments)
                            {
                                var additionalDetails = new List<KeyValuePair<string, string>>();

                                var additionalDetail = new KeyValuePair<string, string>(Constants.AuditFor,
                                    jobParameters.JobName);
                                additionalDetails.Add(additionalDetail);
                                additionalDetail = new KeyValuePair<string, string>(Constants.DocumentControlNumber,
                                    searchDocument.DocumentControlNumber);
                                additionalDetails.Add(additionalDetail);
                                additionalDetail = new KeyValuePair<string, string>(Constants.DocumentGuid,
                                    searchDocument.DocumentID);
                                additionalDetails.Add(additionalDetail);
                            }
                        }

                        #endregion

                        searchResultsDataEntityObject =
                            SearchResultsService.SaveSearchResultsDocumentDetails(searchResultsDataEntityObject);

                        LogMessage(string.Format(Constants.DoAtomicWorkCompletedMessage, task.TaskNumber), false,
                            LogCategory.Job, null);
                        LogMessage(string.Format(Constants.DoAtomicWorkCompletedMessage, task.TaskNumber), GetType(),
                            Constants.DoAtomicMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                            jobParameters.JobRunId);
                    }
                }
            }
            finally
            {
                if (searchResultsDataEntityObject.Details != null && searchResultsDataEntityObject.Details.Count > 0)
                {
                    searchResultsDataEntityObject.Details.SafeForEach(x => _dcnList.Add(x.DocumentControlNumber));
                }
            }

            #region Post-condition and class invariant asserts

            task.ShouldNotBe(null);
            jobParameters.ShouldNotBe(null);

            #endregion

            return true;
        }

        /// <summary>
        /// Before job shuts down, shall update job next run
        /// </summary>
        /// <param name="jobParameters">Job Business Object</param>
        protected override void Shutdown(SaveSearchResultsJobBEO jobParameters)
        {
            jobParameters.ShouldNotBe(null);
            LogMessage(Constants.ShutdownLogMessage, false, LogCategory.Job, null);
            var additionalDetails = new List<KeyValuePair<string, string>>();

            var aditionalDetail = new KeyValuePair<string, string>(Constants.SearchDescription,
                _searchResultsProperty.SearchResultsDescription);
            additionalDetails.Add(aditionalDetail);

            aditionalDetail = new KeyValuePair<string, string>(Constants.SearchQuery,
                jobParameters.DocumentQuery.QueryObject.DisplayQuery);
            additionalDetails.Add(aditionalDetail);

            aditionalDetail = new KeyValuePair<string, string>(Constants.NoOfDocuments,
                _dcnList.Count.ToString(CultureInfo.InvariantCulture));
            additionalDetails.Add(aditionalDetail);

            aditionalDetail = new KeyValuePair<string, string>(Constants.DocumentControlNumberList,
                string.Join(",", _dcnList.ToArray()));
            additionalDetails.Add(aditionalDetail);

            #region Notification section

            //Getting Job detail detail
            var jobBeo = JobMgmtBO.GetJobDetails(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));
            if (jobBeo.NotificationId > 0)
            {
                string defaultMessage;
                var nameEndIndex = jobBeo.Name.LastIndexOf(Constants.JobDateSeparator, StringComparison.Ordinal);
                var savedSearchResultName = nameEndIndex >= 0 ? jobBeo.Name.Substring(0, nameEndIndex) : jobBeo.Name;
                defaultMessage = string.Format(Constants.SuccessNotificationMessage,
                    HttpUtility.HtmlEncode(savedSearchResultName),
                    ApplicationConfigurationManager.GetValue(Constants.SaveSearchResultUrl));
                CustomNotificationMessage = defaultMessage;
            }

            #endregion
        }

        #endregion

        #region Job framework logging and Exception handling

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="isError">if set to <c>true</c> [is error].</param>
        /// <param name="category">The category.</param>
        /// <param name="additionalDetail">The additional detail.</param>
        private void LogMessage(string customMessage, bool isError, LogCategory category,
            List<KeyValuePair<string, string>> additionalDetail)
        {
            if (category == LogCategory.Job)
            {
                JobLogInfo.AddParameters(customMessage);
                JobLogInfo.IsError = isError;
                if (additionalDetail != null && additionalDetail.Count > 0)
                {
                    foreach (var keyValue in additionalDetail)
                    {
                        JobLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                    }
                }
            }
            else if (category == LogCategory.Task)
            {
                TaskLogInfo.AddParameters(customMessage);
                TaskLogInfo.IsError = isError;
                if (additionalDetail != null && additionalDetail.Count > 0)
                {
                    foreach (var keyValue in additionalDetail)
                    {
                        TaskLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                    }
                }
            }
        }

        #endregion

        #region Utility Method --> need to move in common File

        public enum AuditLogPurpose
        {
            CreateSaveSearchResult,
            SaveResultList,
        };

        /// <summary>
        /// Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of logging messages.
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
            EvLog.WriteEntry(consumerClass.ToString(),
                "Job ID: " + jobId
                + Constants.NextLineCharacter + "Job Run ID: " + jobRunId
                + Constants.NextLineCharacter + "Location: " + messageLocation
                + Constants.NextLineCharacter + ((message.Equals(string.Empty)) ? string.Empty : "Details: " + message),
                eventLogEntryType);
        }

        /// <summary>
        /// Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of logging messages.
        /// </summary>
        /// <param name="exception"> Exception details to be logged </param>
        /// <param name="consumerClass"> Import job class type using this function </param>
        /// <param name="messageLocation"> Location from which message is being logged - normally it's function name </param>
        /// <param name="eventLogEntryType"> Error or Message or Audit entry </param>
        /// <param name="jobId"> Job Identifier </param>
        /// <param name="jobRunId"> Job instance identifier </param>
        public static void LogMessage(Exception exception, Type consumerClass, string messageLocation,
            EventLogEntryType eventLogEntryType, int jobId, int jobRunId)
        {
            // Create Message from the exception
            var message = new StringBuilder();
            message.Append((exception != null)
                ? " Error Message: " + exception.Message + Constants.NextLineCharacter
                : string.Empty);

            var levelOfInnerException = 1;
            if (exception != null && exception.InnerException != null)
                GetMessagesFromInnerException(exception, message, levelOfInnerException);

            message.Append(exception != null && (exception.StackTrace != null)
                ? " Stack Trace: " + exception.StackTrace + Constants.NextLineCharacter
                : string.Empty);

            // Log message.
            LogMessage(message.ToString(), consumerClass, messageLocation, eventLogEntryType, jobId, jobRunId);
        }

        /// <summary>
        /// Obtains exception detail and stack trace
        /// </summary>
        /// <param name="exception"> Exception object for which inner exception details need to be obtained </param>
        /// <param name="message"> message object to which details are appended </param>
        /// <param name="levelOfException"> auto incremented number that depicts level of inner exception </param>
        /// <returns> recursive function - hence the bool return type </returns>
        public static bool GetMessagesFromInnerException(Exception exception, StringBuilder message,
            int levelOfException)
        {
            checked
            {
                levelOfException += 1;
            }

            message.Append(
                string.Format(
                    " Inner Exception (level {0}): " + exception.InnerException + Constants.NextLineCharacter,
                    levelOfException));

            if (exception.InnerException.StackTrace != null)
            {
                message.Append(" Inner Exception Stack Trace: " + exception.InnerException.StackTrace +
                               ">>> END INNER EXCEPTION STACK TRACE <<< " + Constants.NextLineCharacter);
            }

            message.Append(string.Format(" >>> END INNER EXCEPTION level {0}", levelOfException));

            return exception.InnerException.InnerException != null &&
                   GetMessagesFromInnerException(exception.InnerException, message, levelOfException);
        }


        /// <summary>
        /// Encapsulates resource manager creation and retrieval of resource values
        /// </summary>
        public class ResourceManagerHelper
        {
            private readonly ResourceManager _resourceManager;

            /// <summary>
            /// When resource is unavailable, getResourceValue function shall return this value.
            /// </summary>
            public string ResourceUnavailableString { get; set; }


            /// <summary>
            /// Creates file based resource manager object.
            /// </summary>
            /// <param name="resouceBaseName"></param>
            /// <param name="resourceDirectory"></param>
            public ResourceManagerHelper(string resouceBaseName, string resourceDirectory)
            {
                _resourceManager = ResourceManager.CreateFileBasedResourceManager(resouceBaseName, resourceDirectory,
                    null);
                ResourceUnavailableString = string.Empty;
            }

            /// <summary>
            /// Return resource value if available, if not returns empty string
            /// </summary>
            /// <param name="resourceName">Resource for which value need to be obtained</param>
            /// <returns> Resource value or empty string </returns>
            public string GetResourceValue(string resourceName)
            {
                try
                {
                    return _resourceManager.GetString(resourceName);
                }
                catch
                {
                    return ResourceUnavailableString;
                }
            }
        }

        /// <summary>
        /// Get Native file path of a document
        /// </summary>
        /// <param name="searchDocument">Document to get native file path of</param>
        /// <returns>Native file path</returns>
        private string GetNativeFilePath(DocumentResult searchDocument)
        {
            #region Pre-condition asserts

            searchDocument.ShouldNotBe(null);
            searchDocument.Fields.ShouldNotBe(null);

            #endregion

            var nativeFilePath = string.Empty;
            var nativeFilePathField =
                searchDocument.Fields.Find(
                    x => String.CompareOrdinal(x.Name.ToLowerInvariant(), EVSystemFields.NativeFilePath.ToLower()) == 0);
            if (nativeFilePathField != null)
            {
                nativeFilePath = nativeFilePathField.Value;
            }

            #region Post-condition asserts

            searchDocument.ShouldNotBe(null);
            searchDocument.Fields.ShouldNotBe(null);

            #endregion

            return nativeFilePath;
        }

        #endregion
    }
}