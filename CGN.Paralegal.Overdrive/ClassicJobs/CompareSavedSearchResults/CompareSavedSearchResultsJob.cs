#region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="CompareSavedSearchResultsJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish Kumar</author>
//      <description>
//        CompareSavedSearchResults  Batch Job Class
//      </description>
//      <changelog>
//          <date value="6/12/2011">Fix for bug 93337</date>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//          <date value="02/28/2012">Fix for Bug# 95162</date>
//          <date value="3/20/2012">Fix to change job to INPROC process type</date>
//          <date value="11\29\2012">Fix for bug 112025</date>
//          <date value="2/21/2013">Fix for bug 127359 - Saved Search Results Slowness Issue</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion File Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Repository.MiddleTier.SearchResults;
using LexisNexis.Evolution.Repository.Service.Contracts.SearchResults;
using LexisNexis.Evolution.Repository.Service.Implementation;
using LexisNexis.Evolution.Repository.Service.Implementation.SearchResults;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Infrastructure.Common;

#endregion Namespaces

namespace LexisNexis.Evolution.BatchJobs.CompareSavedSearchResultsJob
{
    ///<summary>
    ///Batch Job Class
    ///</summary>
    [Serializable]
    public class CompareSavedSearchResultsJob :
        BaseJob<CompareSavedSearchResultsJobBEO, CompareSavedSearchResultsJobTaskEntity>
    {
        private readonly CompareSavedSearchResultsJobBEO _jobBeo; // Job level data

        /// <summary>
        /// Encoding Required - > configurable item
        /// </summary>
        private Encoding _encoder;

        /// <summary>
        /// Type of file need to be generated
        /// </summary>
        private string _fileType;

        /// <summary>
        /// represents the type of encoding used to write report file
        /// </summary>
        private string _encodingType;

        /// <summary>
        /// represents the column separator
        /// </summary>
        private string _xslFilePathForComparisonReport;

        /// <summary>
        /// Reference to search results properties pass as boot parameter
        /// </summary>
        private SearchResultsPropertiesDataContract _searchResultsProperty;

        /// <summary>
        /// Store success/Failure status
        /// </summary>
        private bool _isJobFailed;

        /// <summary>
        /// Comparison detail
        /// </summary>
        private SavedSearchCompareReportBEO _savedSearchCompareReport;

        /// <summary>
        /// User information
        /// </summary>
        private UserBusinessEntity _userEntityOfJobOwner;

        /// <summary>
        /// Old Search result
        /// </summary>
        private SearchResultsDataContract _oldSearchResult;

        /// <summary>
        /// New search result
        /// </summary>
        private SearchResultsDataContract _newSearchResult;

        /// <summary>
        /// Report string
        /// </summary>
        private StringBuilder _reportString;

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

        /// <summary/>
        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public CompareSavedSearchResultsJob()
        {
            //Initialize the Private variables
            _jobBeo = new CompareSavedSearchResultsJobBEO();
            _fileType = Constants.FileTypeCsv; // Default file type
            _xslFilePathForComparisonReport = string.Empty;
            _isJobFailed = false;

            #region Initialize searchResultsService

            searchResultsService = new SearchResultsServiceImplementation();
            EVUnityContainer.RegisterInstance(Constants.SearchResultService, searchResultsService);

            #endregion
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
        protected override CompareSavedSearchResultsJobBEO Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            try
            {
                LogMessage(Constants.InitializationStartMessage, GetType(), Constants.InitializeMethodFullName,
                    EventLogEntryType.Information, jobId, jobRunId);
                //using job framework Logging
                LogMessage(Constants.InitializationStartMessage, false, LogCategory.Job, null);
                try
                {
                    // Set level of logging
                    _fileType = (ApplicationConfigurationManager.GetValue(Constants.RequiredFileType));
                    _encodingType = (ApplicationConfigurationManager.GetValue(Constants.RequiredEncoding));
                    _xslFilePathForComparisonReport =
                        (CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.XslFilePathForComparisonReport));
                    LogMessage(Constants.InitializationDoneForConfigurableItemMessage, false, LogCategory.Job, null);
                    LogMessage(Constants.InitializationDoneForConfigurableItemMessage, GetType(),
                        Constants.InitializeMethodFullName, EventLogEntryType.Information, jobId, jobRunId);
                }
                catch
                {
                    _isJobFailed = true;
                    LogMessage(Constants.InitializationDoneForConfigurableItemErrorMessage, true, LogCategory.Job, null);
                    LogMessage(Constants.InitializationDoneForConfigurableItemErrorMessage, GetType(),
                        Constants.InitializeMethodFullName, EventLogEntryType.Information, jobId, jobRunId);
                }

                //Constructing Search Results property Beo from passed boot paramter
                _searchResultsProperty =
                    (SearchResultsPropertiesDataContract)
                        XmlUtility.DeserializeObject(bootParameters, typeof (SearchResultsPropertiesDataContract));

                //Initialize the JobBEO object
                _jobBeo.JobName = string.Format("Compare Job - {0} at {1}", _searchResultsProperty.SearchResultsName,
                    DateTime.UtcNow.ConvertToUserTime());
                _jobBeo.JobDescription = _searchResultsProperty.SearchResultsDescription;
                _jobBeo.FileType = _fileType;

                _jobBeo.JobTypeName = Constants.JobTypeName;
                _jobBeo.JobId = jobId;
                _jobBeo.JobRunId = jobRunId;

                // Obtain User BEO of job owner -> will be used for audit log purpose
                _userEntityOfJobOwner = UserBO.GetUserUsingGuid(createdBy);
                _jobBeo.JobScheduleCreatedBy = (_userEntityOfJobOwner.DomainName.Equals("N/A"))
                    ? _userEntityOfJobOwner.UserId
                    : _userEntityOfJobOwner.DomainName + "\\" + _userEntityOfJobOwner.UserId;
            }
            catch (EVException ex)
            {
                _isJobFailed = true;
                WriteToEventViewer(ex, GetType(), MethodBase.GetCurrentMethod().Name, jobId, jobRunId);
                HandleJobException(null, ErrorCodes.ProblemInJobInitialization);
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                // Handle exception in initialize
                LogMessage(ex, GetType(), MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error, jobId, jobRunId);
                HandleJobException(ex, ErrorCodes.ProblemInJobInitialization);
            }
            return _jobBeo;
        }

        /// <summary>
        /// Generates Alerts tasks
        /// </summary>
        /// <param name="jobParameters">Job BEO</param>
        /// <param name="previouslyCommittedTaskCount">int</param>
        /// <returns>List of Job Tasks (BEOs)</returns>
        protected override Tasks<CompareSavedSearchResultsJobTaskEntity> GenerateTasks(
            CompareSavedSearchResultsJobBEO jobParameters, out int previouslyCommittedTaskCount)
        {
            var tasks = new Tasks<CompareSavedSearchResultsJobTaskEntity>();
            previouslyCommittedTaskCount = 0;

            try
            {
                LogMessage(Constants.TaskGenerationStartedMessage, GetType(), Constants.GenerateTaskMethodFullName,
                    EventLogEntryType.Information, jobParameters.JobId, jobParameters.JobRunId);
                var counter = 1;

                foreach (var task in from int type in Enum.GetValues(typeof (TypeOfTask))
                    select new CompareSavedSearchResultsJobTaskEntity
                    {
                        TaskType = (TypeOfTask) type,
                        TaskNumber = counter++,
                        TaskPercent = 25.0
                    })
                {
                    tasks.Add(task);
                }
                LogMessage(string.Format(Constants.TaskGenerationCompletedMessage, tasks.Count), GetType(),
                    Constants.GenerateTaskMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                    jobParameters.JobRunId);
            }
            catch (EVException ex)
            {
                _isJobFailed = true;
                WriteToEventViewer(ex, GetType(), MethodBase.GetCurrentMethod().Name, jobParameters.JobId,
                    jobParameters.JobRunId);
                HandleJobException(null, ErrorCodes.ProblemInGenerateTasks);
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                // Handle exception in Generate Tasks
                LogMessage(ex, GetType(), MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error,
                    jobParameters.JobId, jobParameters.JobRunId);
                HandleJobException(ex, ErrorCodes.ProblemInGenerateTasks);
            }
            return tasks;
        }

        /// <summary>
        /// Atomic work 1)Get old search result 2)New Search 3) Compare 4) generate report to database.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(CompareSavedSearchResultsJobTaskEntity task,
            CompareSavedSearchResultsJobBEO jobParameters)
        {
            task.ShouldNotBe(null);
            jobParameters.ShouldNotBe(null);
            var queryContext = _searchResultsProperty.DocumentQuery;
            queryContext.DocumentStartIndex = 0;
            queryContext.DocumentCount = 1;
            queryContext.IgnoreDocumentSnippet = true;
            queryContext.OutputFields.Clear();
            queryContext.OutputFields.Add(new Field { FieldName = EVSystemFields.DcnField });
            try
            {
                LogMessage(string.Format(Constants.DoAtomicWorkStartMessage, task.TaskNumber), GetType(),
                    Constants.DoAtomicMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                    jobParameters.JobRunId);
                switch (task.TaskType)
                {
                    case TypeOfTask.FetchRecordFromDb:
                    {
                        var searchResultuuid = string.Format(SaveSearchResultIdentifierBEO.UniqueIdentifierFormat,
                            _searchResultsProperty.DocumentQuery.QueryObject.MatterId,
                            _searchResultsProperty.DocumentQuery.QueryObject.DatasetId,
                            _searchResultsProperty.SearchResultId);
                        _oldSearchResult = SearchResultsService.GetSavedSearchResultsWithDocument(searchResultuuid);
                    }
                        break;
                    case TypeOfTask.FetchRecordFromSearchSubSystem:
                    {
                        LogMessage(string.Format("New Search start at : {0}",
                            DateTime.UtcNow.ConvertToUserTime()), GetType(), Constants.DoAtomicMethodFullName,
                            EventLogEntryType.Information, jobParameters.JobId, jobParameters.JobRunId);

                        //SearchContextObject searchContextObject = ConvertToSearchContextObject(queryContext);
                        ReviewerSearchResults rvwSearchBeo;
                        queryContext.QueryObject.TransactionName =
                            "CompareSavedSearchResultsJob - DoAtomicWork (GetCount)";
                        var totalResultCount = JobSearchHandler.GetSearchResultsCount(queryContext.QueryObject);
                        //searchContextObject.ItemsPerPage = totalResultCount;
                        queryContext.DocumentCount = (int) totalResultCount;
                        queryContext.TransactionName = "CompareSavedSearchResultsJob - DoAtomicWork (GetAll)";
                        rvwSearchBeo = JobSearchHandler.GetAllDocuments(queryContext, false);
                            // Getting search result list by page index
                        LogMessage(string.Format("New Search End at : {0}", DateTime.UtcNow.ConvertToUserTime()),
                            GetType(), Constants.DoAtomicMethodFullName, EventLogEntryType.Information,
                            jobParameters.JobId, jobParameters.JobRunId);
                        //Constructing New search Result Beo
                        _newSearchResult = new SearchResultsDataContract
                        {
                            Information = new SearchResultsInformationDataContract
                            {
                                Properties =
                                    new SearchResultsPropertiesDataContract
                                    {
                                        SearchResultId = _searchResultsProperty.SearchResultId,
                                        SearchResultsName = _searchResultsProperty.SearchResultsName,
                                        SearchResultsDescription = _searchResultsProperty.SearchResultsDescription,
                                        DocumentQuery = _searchResultsProperty.DocumentQuery
                                    },
                                CreatedBy = _userEntityOfJobOwner.UserId,
                                CreatedDate = DateTime.UtcNow.ConvertToUserTime()
                            }
                        };
                        if (rvwSearchBeo != null)
                        {
                            if (rvwSearchBeo.ResultDocuments != null && rvwSearchBeo.ResultDocuments.Any())
                            {
                                //int totalHitCount = 0;
                                foreach (var document in rvwSearchBeo.ResultDocuments)
                                {
                                    var documentDeatail = new SearchResultDocumentDetailsDataContract();
                                    documentDeatail.DocumentId = document.DocumentID;
                                    documentDeatail.DocumentControlNumber = document.DocumentControlNumber;
                                    documentDeatail.NativeFilePath = GetNativeFilePath(document);
                                    _newSearchResult.Details.Add(documentDeatail);
                                }
                                _newSearchResult.Information.NumberOfDocuments = rvwSearchBeo.ResultDocuments.Count;
                            }
                        }
                        LogMessage(string.Format("New Search Results Construction done at : {0}",
                            DateTime.UtcNow.ConvertToUserTime()), GetType(), Constants.DoAtomicMethodFullName,
                            EventLogEntryType.Information, jobParameters.JobId, jobParameters.JobRunId);
                    }
                        break;
                    case TypeOfTask.CompareRecords:
                    {
                        var comparisonStart = DateTime.Now;
                        _savedSearchCompareReport = new SavedSearchCompareReportBEO();
                        LogMessage(string.Format("Comparison Start at: {0}", DateTime.UtcNow.ConvertToUserTime()),
                            GetType(), Constants.DoAtomicMethodFullName, EventLogEntryType.Information,
                            jobParameters.JobId, jobParameters.JobRunId);
                        var oldResultSet = new List<SearchResultsDetailBEO>();
                        var newResultSet = new List<SearchResultsDetailBEO>();
                        _oldSearchResult.Details.SafeForEach(x => oldResultSet.Add(x.ToBusinessEntity()));
                        _newSearchResult.Details.SafeForEach(x => newResultSet.Add(x.ToBusinessEntity()));

                        // Extracting Common document details
                        (newResultSet.Intersect(oldResultSet).ToList()).SafeForEach(x =>
                            _savedSearchCompareReport.CommonDocumentSet.Add(x));
                        //Extracting Unique document details from new Search result
                        (newResultSet.Except(_savedSearchCompareReport.CommonDocumentSet).ToList()).
                            SafeForEach(x => _savedSearchCompareReport.DocumentsOnlyInNewResultSet.Add(x));
                        //Extracting Unique document details from old(Saved) Search result
                        (oldResultSet.Except(_savedSearchCompareReport.CommonDocumentSet).ToList()).
                            SafeForEach(x => _savedSearchCompareReport.DocumentOnlyInOldResultSet.Add(x));
                        var comparisonStop = DateTime.Now;
                        LogMessage(string.Format("Total Time taken for comparison: {0}",
                            (comparisonStop - comparisonStart).Milliseconds), GetType(),
                            Constants.DoAtomicMethodFullName, EventLogEntryType.Information, jobParameters.JobId,
                            jobParameters.JobRunId);
                    }
                        break;
                    case TypeOfTask.ConstructFileContent:
                    {
                        LogMessage(
                            string.Format("File Construction start at: {0}", DateTime.UtcNow.ConvertToUserTime()),
                            GetType(), Constants.DoAtomicMethodFullName, EventLogEntryType.Information,
                            jobParameters.JobId, jobParameters.JobRunId);

                        //Saving Old search results information
                        _savedSearchCompareReport.OldResultDetails = new SearchResultsInformationBEO
                        {
                            CreatedBy = _oldSearchResult.Information.CreatedBy,
                            CreatedDate = _oldSearchResult.Information.CreatedDate,
                            NumberOfDocuments = _oldSearchResult.Information.NumberOfDocuments,
                            NumberOfSearchHits = _oldSearchResult.Information.NumberOfSearchHits
                        };
                        //Saving Old search results information
                        _savedSearchCompareReport.NewResultDetails = new SearchResultsInformationBEO
                        {
                            CreatedBy = _newSearchResult.Information.CreatedBy,
                            CreatedDate = _newSearchResult.Information.CreatedDate,
                            NumberOfDocuments = _newSearchResult.Information.NumberOfDocuments,
                            NumberOfSearchHits = _newSearchResult.Information.NumberOfSearchHits
                        };
                        _savedSearchCompareReport.CreatedBy = _userEntityOfJobOwner.UserId;
                        _savedSearchCompareReport.SearchQueryTerm = queryContext.QueryObject.DisplayQuery;
                        _savedSearchCompareReport.CreatedDate = DateTime.UtcNow.ConvertToUserTime();
                        //File Creation Logic
                        CreateReportFile(out _reportString);
                        LogMessage(string.Format("File Construction End at: {0}", DateTime.UtcNow.ConvertToUserTime()),
                            GetType(), Constants.DoAtomicMethodFullName, EventLogEntryType.Information,
                            jobParameters.JobId, jobParameters.JobRunId);
                        //Clean up all class level variable
                        _oldSearchResult = null;
                        _newSearchResult = null;
                    }
                        break;
                }
            }
            catch (EVException ex)
            {
                _isJobFailed = true;
                WriteToEventViewer(ex, GetType(), MethodBase.GetCurrentMethod().Name, jobParameters.JobId,
                    jobParameters.JobRunId);
                HandleTaskException(null, string.Format(Constants.TaskKeyStringFormat, task.TaskType),
                    ErrorCodes.ProblemInDoAtomicWork);
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                // Handle exception in DoAutomic
                LogMessage(ex, GetType(), MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error,
                    jobParameters.JobId, jobParameters.JobRunId);
                HandleTaskException(ex, string.Format(Constants.TaskKeyStringFormat, task.TaskType),
                    ErrorCodes.ProblemInDoAtomicWork);
            }
            return true;
        }

        /// <summary>
        /// Shutdown method will be called at end of the job
        /// </summary>
        /// <param name="jobParameters">Job Business Object</param>
        protected override void Shutdown(CompareSavedSearchResultsJobBEO jobParameters)
        {
            try
            {
                long reportId = 0;
                _encoder = _encodingType.Equals(Constants.UniCode, StringComparison.CurrentCultureIgnoreCase)
                    ? (Encoding) new UnicodeEncoding()
                    : new UTF8Encoding();
                if (_fileType.Equals(Constants.FileTypeCsv, StringComparison.CurrentCultureIgnoreCase))
                {
                    //Writing generated report to Db
                    reportId = SearchResultsBO.SaveExportResults(_searchResultsProperty.SearchResultId,
                        _encoder.GetBytes(_reportString.ToString()), true, _fileType, _userEntityOfJobOwner.UserGUID);
                    _reportString = null;
                }

                #region Notification section

                //Getting Job detail detail
                var jobBeo = JobMgmtBO.GetJobDetails(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));
                if (jobBeo.NotificationId > 0)
                {
                    string defaultMessage;
                    defaultMessage = _isJobFailed
                        ? string.Format(Constants.FailureNotificationMessage, _searchResultsProperty.SearchResultsName)
                        : string.Format(Constants.CompareReportMessage, _searchResultsProperty.SearchResultsName,
                            ApplicationConfigurationManager.GetValue(Constants.ReportHandlerUrl)
                            , Constants.CompareReport, _searchResultsProperty.SearchResultId, _fileType, reportId,
                            _searchResultsProperty.DocumentQuery.QueryObject.DatasetId);
                    CustomNotificationMessage = defaultMessage;
                }

                #endregion Notification section
            }
            catch (EVException ex)
            {
                WriteToEventViewer(ex, GetType(), MethodBase.GetCurrentMethod().Name, jobParameters.JobId,
                    jobParameters.JobRunId);
                HandleJobException(null, ErrorCodes.ProblemInShutDown);
            }
            catch (Exception ex)
            {
                // Handle exception
                LogMessage(ex, GetType(), MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error,
                    jobParameters.JobId, jobParameters.JobRunId);
                HandleJobException(ex, ErrorCodes.ProblemInShutDown);
            }
        }

        #endregion Job Framework Functions

        #region Helper Method

        /// <summary>
        /// Transforms the specified s XML data.
        /// </summary>
        /// <param name="sXmlData">The s XML data.</param>
        /// <param name="sXslPath">The s XSL path.</param>
        /// <returns></returns>
        private static string Transform(string sXmlData, string sXslPath)
        {
            var reportString = string.Empty;
            TextReader xr = null;
            try
            {
                xr = new StringReader(sXmlData);
                var myXPathDoc = new XPathDocument(xr);
                //load the Xml doc
                var myXslTrans = new XslCompiledTransform();
                //load the Xsl
                myXslTrans.Load(sXslPath);
                //create the output stream
                using (var sw = new StringWriter())
                {
                    //do the actual transform of Xml
                    myXslTrans.Transform(myXPathDoc, null, sw);
                    //getting report String from stringWrite object
                    reportString = sw.ToString();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (xr != null)
                {
                    xr.Dispose();
                }
            }
            return reportString;
        }

        /// <summary>
        /// Creates the report file.
        /// </summary>
        /// <param name="csvReport">The CSV report.</param>
        private void CreateReportFile(out StringBuilder csvReport)
        {
            csvReport = new StringBuilder();
            _savedSearchCompareReport.ShouldNotBe(null);
            _savedSearchCompareReport.CommonDocumentSet.ForEach(
                x =>
                {
                    if (!string.IsNullOrEmpty(x.NativeFilePath))
                    {
                        x.NativeFilePath = x.NativeFilePath.Substring(
                            x.NativeFilePath.LastIndexOf(Constants.BackSlash, StringComparison.Ordinal) + 1);
                    }
                });
            _savedSearchCompareReport.DocumentOnlyInOldResultSet.ForEach(
                x =>
                {
                    if (!string.IsNullOrEmpty(x.NativeFilePath))
                    {
                        x.NativeFilePath = x.NativeFilePath.Substring(
                            x.NativeFilePath.LastIndexOf(Constants.BackSlash, StringComparison.Ordinal) + 1);
                    }
                });
            _savedSearchCompareReport.DocumentsOnlyInNewResultSet.ForEach(
                x =>
                {
                    if (!string.IsNullOrEmpty(x.NativeFilePath))
                    {
                        x.NativeFilePath = x.NativeFilePath.Substring(
                            x.NativeFilePath.LastIndexOf(Constants.BackSlash, StringComparison.Ordinal) + 1);
                    }
                });
            //getting report string after applying XSL on the sterilized object
            var reportStr = Transform(XmlUtility.SerializeObject(_savedSearchCompareReport),
                _xslFilePathForComparisonReport);
            reportStr.ShouldNotBe(null);
            csvReport.Append(reportStr);
        }

        #endregion Helper Method

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
                if (additionalDetail != null && additionalDetail.Count > 0)
                {
                    foreach (var keyValue in additionalDetail)
                    {
                        JobLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                    }
                }
                JobLogInfo.IsError = isError;
            }
            else if (category == LogCategory.Task)
            {
                TaskLogInfo.AddParameters(customMessage);
                if (additionalDetail != null && additionalDetail.Count > 0)
                {
                    foreach (var keyValue in additionalDetail)
                    {
                        TaskLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                    }
                }
                TaskLogInfo.IsError = isError;
            }
        }

        /// <summary>
        /// Handles the job exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="erroCode">string</param>
        private void HandleJobException(Exception ex, string erroCode)
        {
            HandleException(LogCategory.Job, ex, null, erroCode);
        }

        /// <summary>
        /// Handles the task exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="taskKey">string</param>
        /// <param name="errorCode">Error Code</param>
        private void HandleTaskException(Exception ex, string taskKey, string errorCode)
        {
            HandleException(LogCategory.Task, ex, taskKey, errorCode);
        }

        /// <summary>
        /// Handles the exception.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="taskKey">string</param>
        /// <param name="errorCode">Error Code</param>
        private void HandleException(LogCategory category, Exception ex, string taskKey, string errorCode)
        {
            if (category == LogCategory.Job)
            {
                JobLogInfo.AddParameters(ex.Message);
                var jobException = new EVJobException(errorCode, ex, JobLogInfo);
                throw jobException;
            }
            TaskLogInfo.AddParameters(ex.Message);
            TaskLogInfo.TaskKey = taskKey;
            var taskException = new EVTaskException(errorCode, ex, TaskLogInfo);
            throw taskException;
        }

        #endregion Job framework logging and Exception handling

        #region Utility Method --> need to move in common File

        /// <summary>
        /// EV Exception if thrown use error code for locating message from resource file.
        /// This function logs the message as well...
        /// </summary>
        /// <param name="evException">EV specific application error.</param>
        /// <param name="consumerClass">Type</param>
        /// <param name="location">Location from which message is being logged - normally it's function name</param>
        /// <param name="jobId">int</param>
        /// <param name="jobRunId">int</param>
        /// <returns>Success Status.</returns>
        private void WriteToEventViewer(EVException evException, Type consumerClass, string location, int jobId,
            int jobRunId)
        {
            var message = Msg.FromRes(evException.GetErrorCode()); // Get user friendly message by the error code.
            LogMessage(message, true, LogCategory.Job, null);
            // Log message
            LogMessage(message + Constants.NextLineCharacter + evException.ToUserString(), consumerClass, location,
                EventLogEntryType.Error, jobId, jobRunId);
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

        /// <summary>
        /// Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of logging messages.
        /// </summary>
        /// <param name="message"> Message to be logged</param>
        /// <param name="consumerClass"> Import job class type using this function </param>
        /// <param name="messageLocation"> Location from which message is being logged - normally it's function name </param>
        /// <param name="eventLogEntryType"> Error or Message or Audit entry </param>
        /// <param name="jobID"> Job Identifier </param>
        /// <param name="jobRunId"> Job instance identifier </param>
        public static void LogMessage(string message, Type consumerClass, string messageLocation,
            EventLogEntryType eventLogEntryType, int jobID, int jobRunId)
        {
            try
            {
                EvLog.WriteEntry(consumerClass.ToString(),
                    "Job ID: " + jobID
                    + Constants.NextLineCharacter + "Job Run ID: " + jobRunId
                    + Constants.NextLineCharacter + "Location: " + messageLocation
                    + Constants.NextLineCharacter +
                    ((message.Equals(string.Empty)) ? string.Empty : "Details: " + message), eventLogEntryType);
            }
            catch
            {
            } // No error logging a message can be captured and handled
        }

        /// <summary>
        /// Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of logging messages.
        /// </summary>
        /// <param name="exception"> Exception details to be logged </param>
        /// <param name="consumerClass"> Import job class type using this function </param>
        /// <param name="messageLocation">string</param>
        /// <param name="eventLogEntryType"> Error or Message or Audit entry </param>
        /// <param name="jobID"> Job Identifier </param>
        /// <param name="jobRunId"> Job instance identifier </param>
        public static void LogMessage(Exception exception, Type consumerClass, string messageLocation,
            EventLogEntryType eventLogEntryType, int jobID, int jobRunId)
        {
            try
            {
                // Create Message from the exception
                var message = new StringBuilder();
                message.Append((exception != null)
                    ? " Error Message: " + exception.Message.ToString(CultureInfo.InvariantCulture) +
                      Constants.NextLineCharacter
                    : string.Empty);

                const int levelOfInnerException = 1;
                if (exception != null && exception.InnerException != null)
                    GetMessagesFromInnerException(exception, message, levelOfInnerException);

                message.Append(exception != null && (exception.StackTrace != null)
                    ? " Stack Trace: " + exception.StackTrace.ToString(CultureInfo.InvariantCulture) +
                      Constants.NextLineCharacter
                    : string.Empty);

                // Log message.
                LogMessage(message.ToString(), consumerClass, messageLocation, eventLogEntryType, jobID, jobRunId);
            }
            catch
            {
            } // No error logging a message can be captured and handled
        }

        /// <summary>
        /// Obtains exception detail and stack trace
        /// </summary>
        /// <param name="exception"> Exception object for which inner exception details need to be obtained </param>
        /// <param name="message"> message object to which details are appened </param>
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
                message.Append(" Inner Exception Stack Trace: " +
                               exception.InnerException.StackTrace.ToString(CultureInfo.InvariantCulture) +
                               ">>> END INNER EXCEPTION STACK TRACE <<< " + Constants.NextLineCharacter);
            }

            message.Append(string.Format(" >>> END INNER EXCEPTION level {0}", levelOfException));

            return exception.InnerException.InnerException != null &&
                   GetMessagesFromInnerException(exception.InnerException, message, levelOfException);
        }

        /// <summary>
        /// Encapsulates resource manager creation and retrieval of resource values
        /// </summary>
        [Serializable]
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

        #endregion Utility Method --> need to move in common File
    }
}