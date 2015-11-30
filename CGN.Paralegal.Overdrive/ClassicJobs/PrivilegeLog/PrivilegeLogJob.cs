# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="PrivilegeLogJob" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Vikash Gupta</author>
//      <description>
//          Privilege Log job main class file.
//      </description>
//      <changelog>
//          <date value="7-Feb-2011"></date>
//          <date value="20/02/2012">97039- BVT issue fix</date>
//          <date value="21/Mar/2012">97991 bug fix</date>
//          <date value="02/June/2012">Task fix 101466 - Cr022</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="02/16/2013">BugFix#130820</date>
//          <date value="02/16/2013">BugFix#127175</date>
//          <date value="06/26/2013">BugFix#144515</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//          <date value="06/13/2014">BugFix#168887</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System.Web;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.ProductionManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Repository.MiddleTier.PrivilegeLog;
using LexisNexis.Evolution.ServiceImplementation.KnowledgeManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using LexisNexis.Evolution.TraceServices;
using Moq;
using DocumentFilterType = LexisNexis.Evolution.Repository.MiddleTier.PrivilegeLog.DocumentFilterType;


namespace LexisNexis.Evolution.BatchJobs.PrivilegeLog
{
    /// <summary>
    /// Privilege log job class
    /// </summary>
    [Serializable]
    public class PrivilegeLogJob : BaseJob<BaseJobBEO, PrivilegeLogJobTaskBEO>
    {
        #region Private Fields

        private readonly BaseJobBEO _job; // Job level data 
        private string _createdByGuid = string.Empty;
        private PrivilegeLogDetailsBEO _request;
        private string _matterId;
        private string _collectionId;
        private string _redactableDocumentsetId;
        private List<RVWTagBEO> _selectedTags = new List<RVWTagBEO>();
        private List<RedactionReasonBEO> _selectedReasonList = new List<RedactionReasonBEO>();
        private string _fullFilePath;
        private int _jobIdentifier;
        private int _reasonCodeFieldId;
        private int _documentDescriptionFieldId;
        private DatasetBEO _datsetBeo;
        private Dictionary<string, string> _logGeneratorSortOrderCollection;
        private Dictionary<string, string> _dcnCollection;
        internal const int MaxDocChunkSize = 500;
        private const int BatchSize = 1000;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public PrivilegeLogJob()
        {
            _job = new BaseJobBEO();
            _request = new PrivilegeLogDetailsBEO();
        }

        #endregion

        #region  " Job Management Function need to be overridden"

        /// <summary>
        /// This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="jobRunId"></param>
        /// <param name="bootParameters"></param>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        protected override BaseJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            EVJobException exception;
            try
            {
                _logGeneratorSortOrderCollection = new Dictionary<string, string>();
                //filling properties of the job parameter
                _job.JobId = jobId;
                _job.JobRunId = jobRunId;
                UserBusinessEntity userBusinessEntity;

                GetUserBusinessEntity(createdBy, out userBusinessEntity);

                _job.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A"))
                    ? userBusinessEntity.UserId
                    : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
                _job.JobTypeName = Constants.JobTypeName;
                _job.BootParameters = bootParameters;
                _job.JobName = Constants.JobName;
                _jobIdentifier = jobId;
                //currentJobRunId = jobRunId;
                EvLog.WriteEntry(jobId + ":" + Constants.Event_Job_Initialize_Start,
                    Constants.EventJobInitializationValue, EventLogEntryType.Information);

                LogJobException(ErrorCodes.InitializeHelperMessage, Constants.JobStartedMessage, false, string.Empty,
                    LogCategory.Job);

                // Default settings
                _job.StatusBrokerType = BrokerType.Database;
                _job.CommitIntervalBrokerType = BrokerType.ConfigFile;
                _job.CommitIntervalSettingType = SettingType.CommonSetting;
                _createdByGuid = createdBy;
                //De serialize the Boot parameters
                _request =
                    (PrivilegeLogDetailsBEO)
                        XmlUtility.DeserializeObject(bootParameters, typeof (PrivilegeLogDetailsBEO));

                _request.ShouldNotBe(null);

                //Bug # 130820 -Privilege Log with saved query option -generates log for all the documents in dataset
                //Mock the user session with organization id also otherwise the search API which gets 
                //and called in this job will filter all the saved searches which results in  performing the document search with empty search query 
                //that is getting all the documents in the dataset

                EVHttpContext.CurrentContext = CreateUserContext(_createdByGuid, userBusinessEntity);

                //Bug # 130820 -Privilege Log with saved query option -generates log for all the documents in dataset
                _datsetBeo = DataSetBO.GetDataSetDetailForDataSetId(_request.DatasetId);
                if (_request == null)
                {
                    exception = CreateJobExceptionObject(ErrorCodes.XmlStringNotWellFormed,
                        Constants.EventXmlNotWellFormedValue,
                        jobId + ":" + Constants.EventJobInitializationKey + "  " + Constants.EventXmlNotWellFormedValue,
                        null);
                    throw exception;
                }
                VerifyPrivilegeLogFolderPathAndFileName(_request);

                GetCollectionMatter();

                #region Order the privilege log fields in the expected order

                //Order the Privilege log fields list such that
                //-Reason code is the first field
                //-Document description is the second field
                //-Other fields follow
                if (_request.PrivilegeLogFieldMapping != null && _request.PrivilegeLogFieldMapping.Count > 0)
                {
                    //Add document description field - This should be the second field
                    var privilegeLogDocumentDescriptionField =
                        _request.PrivilegeLogFieldMapping.Find(x => x.FieldId == _documentDescriptionFieldId);
                    if (privilegeLogDocumentDescriptionField != null)
                    {
                        _request.PrivilegeLogFieldMapping.Remove(privilegeLogDocumentDescriptionField);
                        _request.PrivilegeLogFieldMapping.Insert(0, privilegeLogDocumentDescriptionField);
                    }

                    //Add reason code field - This should be the first field
                    var privilegeLogReasonCodeField =
                        _request.PrivilegeLogFieldMapping.Find(x => x.FieldId == _reasonCodeFieldId);
                    if (privilegeLogReasonCodeField != null)
                    {
                        _request.PrivilegeLogFieldMapping.Remove(privilegeLogReasonCodeField);
                        _request.PrivilegeLogFieldMapping.Insert(0, privilegeLogReasonCodeField);
                    }
                }

                #endregion

                #region Create csv file with header

                _fullFilePath = _request.PrivilegeLogFolder + @"\" + _request.PrivilegeLogName;

                //Add csv extension if needed
                _fullFilePath = (_fullFilePath.ToLower().EndsWith(".csv") ? _fullFilePath : _fullFilePath + ".csv");

                #region Log the file path

                LogJobException(string.Empty, Constants.LogDocumentLabel + _fullFilePath, false, string.Empty,
                    LogCategory.Job);

                #endregion

                CreateCsvFile(_fullFilePath);

                #endregion

                EvLog.WriteEntry(jobId + ":" + Constants.Event_Job_Initialize_Success,
                    Constants.EventJobInitializationValue, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                exception = CreateJobExceptionObject(ErrorCodes.ErrorInInitialize, Constants.ErrorInInitialize,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                EvLog.WriteEntry(jobId + ":" + Constants.EventJobInitializationKey,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Error);
                throw exception;
            }
            return _job;
        }


        /// <summary>
        /// This is the overridden GenerateTasks() method. 
        /// </summary>
        /// <param name="jobParameters"></param>
        /// <param name="previouslyCommittedTaskCount"></param>
        /// <returns></returns>
        protected override Tasks<PrivilegeLogJobTaskBEO> GenerateTasks(BaseJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            var tasks = new Tasks<PrivilegeLogJobTaskBEO>();
            previouslyCommittedTaskCount = 0;
            var taskNumber = 0;

            EvLog.WriteEntry(jobParameters.JobId + ":" + Constants.GenerateTask, Constants.EventJobGenerateTaskValue,
                EventLogEntryType.Information);

            try
            {
                //Getting task list if some of the tasks is already being committed
                tasks = GetTaskList<BaseJobBEO, PrivilegeLogJobTaskBEO>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;
                //// NOTE: Although GetTaskList() might get the committed tasks if the job had run earlier, it is highly recommended
                //// to manually generate the tasks and check if there are any new tasks that may have come up due to some changes in main
                //// parameters.                
                //// If job is running for first time i.e., there were no last committed tasks for this job then manually create tasks.
                if (tasks.Count <= 0)
                {
                    var searchContext = GetDocumentQueryEntity().QueryObject;
                    searchContext.TransactionName = "PrivilegeLogJob - GenerateTasks (GetCount)";
                    var totalSearchResultscount = JobSearchHandler.GetSearchResultsCount(searchContext);
                    var totalTaskCount = totalSearchResultscount < BatchSize
                        ? 1
                        : (Int32) (Math.Ceiling((double) totalSearchResultscount/BatchSize));
                    //Generate Tasks
                    if (totalSearchResultscount > 0)
                    {
                        for (var i = 0; i < totalTaskCount; i++)
                        {
                            var task = new PrivilegeLogJobTaskBEO();
                            taskNumber++;
                            task.TaskNumber = taskNumber;
                            task.TaskComplete = false;
                            task.TaskPercent = 100.0/totalTaskCount;
                            task.TaskKey = _matterId + '$' + _collectionId + '$' + taskNumber + '$' + _createdByGuid;
                            tasks.Add(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGenerateTask, Constants.ErrorGenerateTask,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                EvLog.WriteEntry(jobParameters.JobId + ":" + Constants.EventGenerateTasksAddDocumentTaskValue,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Error);
            }

            return tasks;
        }

        /// <summary>
        ///  This is the overridden DoAtomicWork method.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(PrivilegeLogJobTaskBEO task, BaseJobBEO jobParameters)
        {
            var isFailed = false;

            try
            {
                var documentCsvProductionFieldList = new List<string>();
                var startingBates = string.Empty;
                var endinggBates = string.Empty;
                List<string> documents; //list of document reference id's
                GetDocuments(task.TaskNumber, out documents);
                /*
                  For a given document make entries in log for below 2 scenarios
                     Scenario 1. The document itself is privileged - make one entry only in log
                     Scenario 2. Document is not privileged & If at least one redaction exists for 
                        the document - One entry is made for each redaction in the log
                   
                    For both scenario 1 & 2 the field and tag data would be the same
                 
                    For reason for redaction data
                      Reason Scenario 1.1 : In scenario 1 - All respective reasons for redaction will be marked 
                                     ============================== 
                                     Reason1,Reason2,Reason3
                                      X,X,X                      
                                     ============================== 
                      Reason Scenario 1.2 :In scenario 2 - Only the respective reason for redaction will be marked
                                     ============================== 
                                      Reason1,Reason2,Reason3
                                       X,,
                                      ,X,
                                      ,,X
                                     ============================== 

                   */

                foreach (var documentIdentifier in documents)
                {
                    bool isDocumentPrivileged;
                    List<string> documentCsvFieldList;
                    if (
                        !GetFieldValueForDocument(documentIdentifier, out isDocumentPrivileged, out documentCsvFieldList))
                    {
                        return false;
                    }
                    List<string> documentCsvTagList;
                    if (!GetTagValueForDocument(documentIdentifier, out documentCsvTagList))
                    {
                        return false;
                    }
                    List<List<string>> documentCsvCommentsList;
                    if (
                        !GetRedactionReasonValueForDocument(documentIdentifier,
                            out documentCsvCommentsList))
                    {
                        return false;
                    }

                    #region Get production document vault field details

                    if (_request.ProductionNumbering != null)
                    {
                        if (_request.ProductionNumbering.IsProductionIncluded)
                        {
                            if (string.IsNullOrEmpty(_request.ProductionNumbering.ProductionCollectionIncluded))
                                //Production collection not specified
                            {
                                LogJobException(ErrorCodes.ProblemInDoAtomicWork,
                                    Constants.CollectionNotSpecifiedMessage, false, string.Empty,
                                    LogCategory.Task);
                            }
                            else
                            {
                                var lstProductionSetFeilds = DataSetBO.GetDataSetFields(_request.DatasetId,
                                    _request
                                        .ProductionNumbering
                                        .ProductionCollectionIncluded);
                                if (lstProductionSetFeilds != null && lstProductionSetFeilds.Any() && _datsetBeo != null)
                                {
                                    if (_request.ProductionNumbering.IsBeginBatesIncluded ||
                                        _request.ProductionNumbering.IsEndBatesIncluded ||
                                        _request.ProductionNumbering.IsBatesRangeIncluded)
                                    {
                                        var batesBeginField =
                                            lstProductionSetFeilds.First(
                                                productionField =>
                                                    productionField.FieldType.DataTypeId == Constants.BatesBeginTypeId);
                                        var batesEndField =
                                            lstProductionSetFeilds.First(
                                                productionField =>
                                                    productionField.FieldType.DataTypeId == Constants.BatesEndTypeId);
                                        if (batesBeginField != null)
                                        {
                                            startingBates = GetProductionDocumentVaultField(_datsetBeo.CollectionId,
                                                documentIdentifier,
                                                batesBeginField.Name);
                                        }
                                        if (batesEndField != null)
                                        {
                                            endinggBates = GetProductionDocumentVaultField(_datsetBeo.CollectionId,
                                                documentIdentifier,
                                                batesEndField.Name);
                                        }
                                    }

                                    //DPN
                                    if (_request.ProductionNumbering.IsDPNIncluded)
                                    {
                                        var dpn =
                                            lstProductionSetFeilds.First(
                                                productionField =>
                                                    productionField.FieldType.DataTypeId == Constants.DPNTypeId);
                                        if (dpn != null)
                                        {
                                            documentCsvProductionFieldList.Add(
                                                GetProductionDocumentVaultField(_datsetBeo.CollectionId,
                                                    documentIdentifier,
                                                    dpn.Name));
                                        }
                                    }
                                    //Begin bates number
                                    if (_request.ProductionNumbering.IsBeginBatesIncluded)
                                    {
                                        documentCsvProductionFieldList.Add(startingBates);
                                    }
                                    //End bates number
                                    if (_request.ProductionNumbering.IsEndBatesIncluded)
                                    {
                                        documentCsvProductionFieldList.Add(endinggBates);
                                    }
                                    //Bates range
                                    if (_request.ProductionNumbering.IsBatesRangeIncluded)
                                    {
                                        var batesRange =
                                            lstProductionSetFeilds.First(
                                                productionField =>
                                                    productionField.FieldType.DataTypeId == Constants.BatesRangeTypeId);
                                        if (batesRange != null)
                                        {
                                            documentCsvProductionFieldList.Add(
                                                GetProductionDocumentVaultField(_datsetBeo.CollectionId,
                                                    documentIdentifier,
                                                    batesRange.Name));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    //Write document data to string builder
                    var tempRow = new StringBuilder("");
                    foreach (var documentCsvCommentsData in documentCsvCommentsList)
                    {
                        var tempRowCombinedList = new List<string>();

                        //Concat the Fields + Tags + Reason for redaction(comments)
                        tempRowCombinedList.AddRange(documentCsvProductionFieldList);
                        tempRowCombinedList.AddRange(documentCsvFieldList);
                        tempRowCombinedList.AddRange(documentCsvTagList);
                        tempRowCombinedList.AddRange(documentCsvCommentsData);

                        foreach (var csvColumnValue in tempRowCombinedList)
                        {
                            tempRow.Append(csvColumnValue);
                            tempRow.Append(",");
                        }
                        //Remove the last comma
                        if (tempRow.Length > 0)
                        {
                            tempRow.Remove(tempRow.Length - 1, 1);
                        }
                    }

                    if (string.IsNullOrEmpty(tempRow.ToString())) continue;
                    if (!_logGeneratorSortOrderCollection.Keys.Contains(documentIdentifier))
                    {
                        _logGeneratorSortOrderCollection.Add(documentIdentifier, tempRow.ToString());
                    }
                }
            }
            catch (EVJobException ex)
            {
                LogJobException(ErrorCodes.ErrorDoAtomicWork, Constants.ErrorDoAtomicWork, true, ex.ErrorCode,
                    LogCategory.Task);
                isFailed = true;
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorDoAtomicWork, Constants.ErrorDoAtomicWork, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                isFailed = true;
            }


            return (!isFailed);
        }


        /// <summary>
        /// Perform shutdown activities for a job if any.
        /// </summary>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected override void Shutdown(BaseJobBEO jobParameters)
        {
            try
            {
                var failedDcn = string.Empty;
                var fileStream = new FileStream(_fullFilePath, FileMode.Append);
                var taskNumber = 0;
                using (TextWriter textWriter = new StreamWriter(fileStream))
                {
                    foreach (var document in _logGeneratorSortOrderCollection)
                    {
                        taskNumber++;
                        try
                        {
                            textWriter.WriteLine(document.Value);
                        }
                        catch (Exception)
                        {
                            string dcnDefaultValue;
                            _dcnCollection.TryGetValue(document.Key, out dcnDefaultValue);
                            failedDcn = string.Format("{0};{1}", failedDcn, dcnDefaultValue).Trim(';');
                        }
                        LogJobException(string.Empty,
                            Constants.TaskCompletedMessage +
                            taskNumber.ToString(CultureInfo.InvariantCulture), false, string.Empty,
                            LogCategory.Task, taskNumber);
                    }
                }


                LogJobException(ErrorCodes.InitializeHelperMessage, Constants.JobEndedMessage, false, string.Empty,
                    LogCategory.Job);
            }
            catch (Exception ex)
            {
                var exception = CreateJobExceptionObject(ErrorCodes.ErrorShutdown, Constants.ErrorShutdown,
                    ex.Message + ":" + ex.InnerException + ":" +
                    ex.StackTrace, ex);
                throw exception;
            }
        }

        #endregion

        #region Create Job Exception Object

        ///  <summary>
        ///  Log Job Exception
        ///  </summary>
        ///  <param name="errorCode">errorCode</param>
        ///  <param name="userFriendlyError">userFriendlyError</param>
        /// <param name="isError">isJobError</param>
        ///  <param name="customMessage">customMessage</param>
        ///  <param name="category">category</param>
        /// <param name="taskNumber"></param>
        /// <returns>success or failure</returns>
        public void LogJobException(string errorCode, string userFriendlyError, bool isError, string customMessage,
            LogCategory category, int taskNumber = 0)
        {
            if (isError)
            {
                EvLog.WriteEntry(_jobIdentifier + ":" + userFriendlyError, customMessage, EventLogEntryType.Error);
            }
            if (!isError)
            {
                if (category == LogCategory.Job) //Log a job level message
                {
                    JobLogInfo.AddParameters(userFriendlyError);
                }
                else //Log a task level message
                {
                    var rand = new Random();
                    long taskId = rand.Next(100000, 500000);
                    TaskLogInfo.AddParameters(userFriendlyError);
                    TaskLogInfo.TaskKey = taskNumber == 0
                        ? taskId.ToString(CultureInfo.InvariantCulture)
                        : taskNumber.ToString(CultureInfo.InvariantCulture); //As task key is not null able
                }
            }
            else
            {
                if (category == LogCategory.Task)
                {
                    //Log a task level error message
                    var rand = new Random();
                    long taskId = rand.Next(100000, 500000);
                    TaskLogInfo.AddParameters(errorCode + ":" + userFriendlyError + ":" + customMessage);
                    TaskLogInfo.TaskKey = taskNumber == 0
                        ? taskId.ToString(CultureInfo.InvariantCulture)
                        : taskNumber.ToString(CultureInfo.InvariantCulture); //As task key is not null able
                    TaskLogInfo.IsError = true;
                }
            }
        }

        /// <summary>
        /// Creates a job object to be thrown
        /// </summary>
        /// <param name="errorCode">Error Code</param>
        /// <param name="userFriendlyError">User friendly message</param>
        /// <param name="customMessage">Message</param>
        /// <param name="ex">Exception Object</param>
        /// <returns></returns>
        private EVJobException CreateJobExceptionObject(string errorCode, string userFriendlyError, string customMessage,
            Exception ex)
        {
            JobLogInfo.AddParameters(errorCode + ":" + userFriendlyError + ":" + customMessage);
            var exception = ex != null
                ? new EVJobException(errorCode, ex, JobLogInfo)
                : new EVJobException(errorCode) {LogMessge = JobLogInfo};
            return exception;
        }

        #endregion

        #region Get Documents

        /// <summary>
        /// Get the documents that are to be included in the privilege log
        /// </summary>
        /// <param name="taskNumber"></param>
        /// <param name="documentsSelected"></param>
        /// <returns></returns>
        private void GetDocuments(int taskNumber, out List<string> documentsSelected)
        {
            documentsSelected = new List<string>();
            const int numberOfDocuments = 1000;
            try
            {
                if (_request.FilterOption != null)
                {
                    var documentQueryEntity = GetDocumentQueryEntity();
                    documentQueryEntity.DocumentStartIndex = (taskNumber - 1)*numberOfDocuments;
                    documentQueryEntity.DocumentCount = numberOfDocuments;
                    _dcnCollection = new Dictionary<string, string>();
                    documentQueryEntity.TransactionName = "PrivilegeLogJob - GetDocuments";
                    var searchResult = JobSearchHandler.GetDocuments(documentQueryEntity);
                    if (searchResult != null &&
                        (true & searchResult.ResultDocuments != null && searchResult.ResultDocuments.Count > 0))
                    {
                        documentsSelected.AddRange(searchResult.ResultDocuments.Select(document => document.DocumentID));
                        searchResult.ResultDocuments.ForEach(
                            document => _dcnCollection.Add(document.DocumentID, document.DocumentControlNumber));
                    }
                }
                else
                {
                    CreateJobExceptionObject(ErrorCodes.ErrorGettingDocumentFilterOptionNotSet,
                        Constants.ErrorGettingDocumentFilterOptionNotSet,
                        Constants.ErrorGettingDocumentFilterOptionNotSet, null);
                }
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingDocuments, Constants.ErrorGettingDocuments,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
            }
        }


        private DocumentQueryEntity GetDocumentQueryEntity()
        {
            var reviewSetId = string.Empty;
            var isIncludeConceptSearch = false;
            var searchQuery = "";
            switch (_request.FilterOption.FilterType)
            {
                case DocumentFilterType.SavedSearch:
                    List<SavedSearchEntity> allSavedSearch;
                    //Service call to get all saved searches
                    if (
                        !GetAllSavedSearch("1", int.MaxValue.ToString(CultureInfo.InvariantCulture), "CreatedDate",
                            "Ascending",
                            out allSavedSearch))
                    {
                        return null;
                    }

                    if (allSavedSearch != null && allSavedSearch.Count > 0 && _request.FilterOption.FilterQuery != null)
                    {
                        var matchingSavedSearch = allSavedSearch.FirstOrDefault
                            (x =>
                                x.SavedSearchId.ToString(CultureInfo.InvariantCulture) ==
                                _request.FilterOption.FilterQuery);
                        if (matchingSavedSearch != null)
                        {
                            searchQuery = matchingSavedSearch.DocumentQuery.QueryObject.DisplayQuery;
                            reviewSetId = matchingSavedSearch.DocumentQuery.QueryObject.ReviewsetId;
                            isIncludeConceptSearch =
                                matchingSavedSearch.DocumentQuery.QueryObject.IsConceptSearchEnabled;
                        }
                    }
                    break;
                case DocumentFilterType.Tag:

                    List<RVWTagBEO> allTags;
                    //Service call to get all tags
                    if (!GetTagDefinitions(out allTags))
                    {
                        return null;
                    }

                    if (allTags != null && allTags.Count > 0 && _request.FilterOption.FilterQuery != null)
                    {
                        var selectedTag =
                            allTags.Where(
                                x =>
                                    x.TagDisplayName.ToString(CultureInfo.InvariantCulture) ==
                                    _request.FilterOption.FilterQuery)
                                .ToList();

                        if (selectedTag.Count > 0)
                        {
                            var tagQuery = new StringBuilder();
                            tagQuery.Append(EVSearchSyntax.Tag);
                            tagQuery.Append(Constants.Quote);
                            tagQuery.Append(selectedTag[0].TagDisplayName);
                            tagQuery.Append(Constants.Quote);
                            searchQuery = tagQuery.ToString();
                        }
                    }
                    break;
                default:
                    //This condition will not occur in normal conditions.Either tag or saved search will be selected.
                    searchQuery = _request.FilterOption.FilterQuery;
                    break;
            }

            var documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    ReviewsetId = reviewSetId,
                    MatterId = Convert.ToInt32(_matterId),
                    IsConceptSearchEnabled = isIncludeConceptSearch,
                    DatasetId = Convert.ToInt32(_request.DatasetId)
                }
            };
            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.QueryObject.QueryList.Add(new Query(searchQuery));
            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});
            var outputFields = new List<Field>();
            outputFields.AddRange(new List<Field>
            {
                new Field {FieldName = "DCN"}
            });
            documentQueryEntity.OutputFields.AddRange(outputFields); //Populate fetch duplicates fields
            return documentQueryEntity;
        }

        #endregion GetDocuments

        #region Get Matter and Collection from dataset

        /// <summary>
        /// Gets the collection and matter identifier
        /// </summary>
        /// <returns>Success or Failure</returns>
        private void GetCollectionMatter()
        {
            try
            {
                DatasetBEO dataset;

                //Service call to get dataset details
                if (!GetDataSetDetailForDataSetId(_request.DatasetId, out dataset))
                {
                    return;
                }

                if (dataset != null)
                {
                    _matterId = dataset.Matter.FolderID.ToString(CultureInfo.InvariantCulture);
                    _collectionId = dataset.CollectionId;
                    _redactableDocumentsetId = dataset.RedactableDocumentSetId;
                    var reasonCodeField =
                        dataset.DatasetFieldList.Find(
                            x => x.FieldType.DataTypeId == Constants.FieldTypeIdForPrivilegeField1);
                    if (reasonCodeField != null)
                    {
                        _reasonCodeFieldId = reasonCodeField.ID;
                    }

                    var documentDescriptionField =
                        dataset.DatasetFieldList.Find(
                            x => x.FieldType.DataTypeId == Constants.FieldTypeIdForPrivilegeField2);
                    if (documentDescriptionField != null)
                    {
                        _documentDescriptionFieldId = documentDescriptionField.ID;
                    }
                }
                else
                {
                    CreateJobExceptionObject(ErrorCodes.ErrorGettingDatasetDetailsNullReturned,
                        Constants.ErrorGettingDatasetDetailsNullReturned,
                        Constants.ErrorGettingDatasetDetailsNullReturned + "  " + _request.DatasetId, null);
                }
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingCollectionMatterDetails,
                    Constants.ErrorGettingCollectionMatterDetails,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
            }
        }

        #endregion

        #region Create csv file

        /// <summary>
        /// Create the csv file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Success or failure</returns>
        private void CreateCsvFile(string filePath)
        {
            var headerRow = new StringBuilder("");
            FileStream fileStream = null;
            try
            {
                if (!FetchHeaderDetails())
                {
                    return;
                }

                #region Form the header row

                #region Production headers

                if (_request.ProductionNumbering != null)
                {
                    if (_request.ProductionNumbering.IsProductionIncluded)
                    {
                        //Document production number
                        if (_request.ProductionNumbering.IsDPNIncluded)
                        {
                            headerRow.Append(_request.ProductionNumbering.DpnHeader);
                            headerRow.Append(",");
                        }
                        //Begin bates number
                        if (_request.ProductionNumbering.IsBeginBatesIncluded)
                        {
                            headerRow.Append(_request.ProductionNumbering.BeginBatesHeader);
                            headerRow.Append(",");
                        }
                        //End bates number
                        if (_request.ProductionNumbering.IsEndBatesIncluded)
                        {
                            headerRow.Append(_request.ProductionNumbering.EndBatesHeader);
                            headerRow.Append(",");
                        }
                        //Bates range
                        if (_request.ProductionNumbering.IsBatesRangeIncluded)
                        {
                            headerRow.Append(_request.ProductionNumbering.BatesRangeHeader);
                            headerRow.Append(",");
                        }
                    }
                }

                #endregion

                //Add fields if any
                if (_request.PrivilegeLogFieldMapping != null)
                {
                    if (_request.PrivilegeLogFieldMapping.Count > 0)
                    {
                        foreach (var field in _request.PrivilegeLogFieldMapping)
                        {
                            headerRow.Append(field.PrivilegeLogFieldName + ",");
                        }
                    }
                }

                //Add tags if any
                if (_selectedTags.Count > 0)
                {
                    foreach (var tag in _selectedTags)
                    {
                        headerRow.Append(tag.TagDisplayName + ",");
                    }
                }

                //Add reasons if any
                if (_selectedReasonList.Count > 0)
                {
                    foreach (var reason in _selectedReasonList)
                    {
                        headerRow.Append(reason.ReasonName + ",");
                    }
                }

                //Remove the last comma
                if (headerRow.Length > 0)
                {
                    headerRow.Remove(headerRow.Length - 1, 1);
                }

                #endregion

                #region Create new file with header

                // Delete the file if it exists.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                fileStream = new FileStream(filePath, FileMode.Create);
                using (TextWriter textWriter = new StreamWriter(fileStream))
                {
                    fileStream = null;
                    textWriter.WriteLine(headerRow.ToString());
                }

                #endregion
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorCreatingCsvFile, Constants.ErrorCreatingCsvFile,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }


        /// <summary>
        /// Fetch the header details for the csv file privilege log
        /// </summary>
        /// <returns>success or failure</returns>
        private bool FetchHeaderDetails()
        {
            try
            {
                #region Fetch header details

                if (_request.SelectedTags != null)
                {
                    if (_request.SelectedTags.Count > 0)
                    {
                        //Get the tags selected
                        List<RVWTagBEO> tags;
                        //Service call to get all tags
                        if (!GetTagDefinitions(out tags))
                        {
                            return false;
                        }

                        if (tags != null && tags.Count > 0 && _request.SelectedTags != null &&
                            _request.SelectedTags.Count > 0)
                        {
                            _selectedTags =
                                tags.Where(
                                    x => _request.SelectedTags.Contains(x.Id.ToString(CultureInfo.InvariantCulture)))
                                    .ToList();
                        }
                    }
                }

                //Get the reason for redaction selected
                //Service call to get all reasons for redaction
                List<RedactionReasonBEO> allReasonList;
                if (
                    !GetReasonsForRedaction(_request.DatasetId.ToString(CultureInfo.InvariantCulture), out allReasonList))
                {
                    return false;
                }

                if (allReasonList != null && allReasonList.Count > 0 && _request.SelectedReasonForRedaction != null &&
                    _request.SelectedReasonForRedaction.Count > 0)
                {
                    _selectedReasonList =
                        allReasonList.Where(
                            x =>
                                _request.SelectedReasonForRedaction.Contains(x.Id.ToString(CultureInfo.InvariantCulture)))
                            .ToList();
                }

                #endregion
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorFetchingHeaderDetails, Constants.ErrorFetchingHeaderDetails,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                return false;
            }

            return true;
        }

        #endregion

        #region Get redaction Comments in xml

        /// <summary>
        /// Get redaction Comments in xml
        /// </summary>
        /// <param name="nav">Navigator</param>
        /// <param name="xpath">Xpath</param>
        /// <returns>iterator</returns>
        private XPathNodeIterator GetRedactionCommentsInDocument(XPathNavigator nav, string xpath)
        {
            if (nav != null)
            {
                var xmlNamespaceManger = new XmlNamespaceManager(nav.NameTable);

                xmlNamespaceManger.AddNamespace(Constants.XmlNamespacePrefix, Constants.XmlNamespace);
                var xpathExpression = nav.Compile(xpath);
                xpathExpression.SetContext(xmlNamespaceManger);
                return nav.Select(xpathExpression);
            }
            return null;
        }

        #endregion

        #region Methods to get document data

        /// <summary>
        /// Get field values for a document
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="isDocumentPrivileged"></param>
        /// <param name="documentCsvFieldList"></param>
        /// <returns>success or failure</returns>
        private bool GetFieldValueForDocument(string documentId, out bool isDocumentPrivileged,
            out List<string> documentCsvFieldList)
        {
            isDocumentPrivileged = false;
            documentCsvFieldList = new List<string>();

            try
            {
                //Get field values for a document 
                //Service call to get fields for a document
                RVWDocumentBEO docData;
                if (!GetDocumentDataViewFromVault(documentId, out docData))
                {
                    return false;
                }
                documentCsvFieldList = new List<string>();

                # region Check if the document itself is privileged. If document has any of the privileged fields set then it is privileged

                //Check if the document itself is privileged. If document has any of the privileged fields set then it is privileged
                isDocumentPrivileged = CheckIfDocumentHasPrivilegedFieldValues(docData);

                #endregion

                if (_request != null && _request.PrivilegeLogFieldMapping != null &&
                    _request.PrivilegeLogFieldMapping.Count > 0)
                {
                    foreach (var userSelectedFields in _request.PrivilegeLogFieldMapping)
                    {
                        if (docData != null && docData.FieldList != null && docData.FieldList.Count > 0)
                        {
                            //Check if the document has the user selected field
                            var fields = userSelectedFields;
                            var field = docData.FieldList.Where(x => x.FieldId == fields.FieldId).ToList();

                            if (field.Count > 0)
                            {
                                documentCsvFieldList.Add("\"" + field[0].FieldValue.Replace("\"", "\"\"") + "\"");
                            }
                            else
                            {
                                documentCsvFieldList.Add(string.Empty);
                            }
                        }
                        else
                        {
                            documentCsvFieldList.Add(string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentFieldValues, Constants.ErrorGettingDocumentFieldValues,
                    true, ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determine if document has a value for the privileged fields
        /// </summary>
        /// <param name="docData"></param>
        /// <returns></returns>
        private bool CheckIfDocumentHasPrivilegedFieldValues(RVWDocumentBEO docData)
        {
            if (docData == null || docData.FieldList == null || docData.FieldList.Count <= 0) return false;
            var privilegedField1 =
                docData.FieldList.Where(x => x.FieldType.DataTypeId == Constants.FieldTypeIdForPrivilegeField1).ToList();
            var privilegedField2 =
                docData.FieldList.Where(x => x.FieldType.DataTypeId == Constants.FieldTypeIdForPrivilegeField2).ToList();
            if (privilegedField1.Exists(x => !string.IsNullOrEmpty(x.FieldValue)))
            {
                return true;
            }

            if (privilegedField2.Exists(x => !string.IsNullOrEmpty(x.FieldValue)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets tags for a document
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="documentCsvTagList"></param>
        /// <returns>success or failure</returns>
        private bool GetTagValueForDocument(string documentId, out List<string> documentCsvTagList)
        {
            documentCsvTagList = new List<string>();

            try
            {
                //Get tag values for a document
                //Service call to get tags for a document
                List<RVWDocumentTagBEO> documentTags;
                if (!GetDocumentTags(documentId, out documentTags))
                {
                    return false;
                }

                //Filter only the tag which was tag to that document
                documentTags = documentTags.Where(o => o.Status).ToList();

                documentCsvTagList = new List<string>();

                if (_selectedTags.Count > 0)
                {
                    foreach (var userSelectedTag in _selectedTags)
                    {
                        if (documentTags.Count > 0)
                        {
                            //Check if the document has the user selected tag
                            var tag = userSelectedTag;
                            documentCsvTagList.Add(documentTags.Exists(x => x.TagDisplayName.Equals(tag.TagDisplayName))
                                ? "X"
                                : string.Empty);
                        }
                        else
                        {
                            documentCsvTagList.Add(string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentTagValues, Constants.ErrorGettingDocumentTagValues, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get redaction reason values for a document
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="documentCsvCommentsList"></param>
        /// <returns>success or failure</returns>
        private bool GetRedactionReasonValueForDocument(string documentId,
            out List<List<string>> documentCsvCommentsList)
        {
            documentCsvCommentsList = new List<List<string>>();

            /*
                For a given document make entries in log for below 2 scenarios
                   Scenario 1. The document itself is privileged - make one entry only in log
                   Scenario 2. Document is not privileged & If atleast one redaction exists for 
                      the document - One entry is made for each redaction in the log
                   
                  For both scenario 1 & 2 the field and tag data would be the same
                 
                  For reason for redaction data
                    Reason Scenario 1.1 : In scenario 1 - All respective reasons for redaction will be marked 
                                   ============================== 
                                   Reason1,Reason2,Reason3
                                    X,X,X                      
                                   ============================== 
                    Reason Scenario 1.2 :In scenario 2 - Only the respective reason for redaction will be marked
                                   ============================== 
                                    Reason1,Reason2,Reason3
                                     X,,
                                    ,X,
                                    ,,X
                                   ============================== 

                 */
            try
            {
                //Get reason values for a document
                //Service call to get the redaction xml for the document
                RVWMarkupBEO markup;
                if (!GetRedactionXml(documentId, out markup))
                {
                    return false;
                }

                var tempReason = new List<string>();
                var commentsInDocument = new List<string>();
                documentCsvCommentsList = new List<List<string>>();

                #region Get all redaction comments in document

                if (markup != null && markup.MarkupXml != null && markup.MarkupXml.Length != 0)
                {
                    var markupXml = new XmlDocument();

                    //Add the version string
                    var markupXmlText = Constants.xmlVersionString.Replace(Constants.BackwardSlash, string.Empty) +
                                        markup.MarkupXml;
                    markupXml.LoadXml(markupXmlText);

                    var nodeIterator = GetRedactionCommentsInDocument(markupXml.CreateNavigator(),
                        Constants.XpathPrevilegePage);

                    if (nodeIterator.Count > 0)
                    {
                        commentsInDocument.AddRange(from XPathNavigator curNav in nodeIterator select curNav.Value);
                    }
                }

                #endregion

                tempReason.AddRange(
                    _selectedReasonList.Select(
                        selectedReason => commentsInDocument.Contains(selectedReason.ReasonName) ? "X" : string.Empty));
                documentCsvCommentsList.Add(tempReason);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentRedactionReasonValues,
                    Constants.ErrorGettingDocumentRedactionReasonValues, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return false;
            }
            return true;
        }

        #endregion

        #region Check if file name is valid

        /// <summary>
        /// To validate the file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>True if filename is valid else false</returns>
        private bool IsFileNameValid(string fileName)
        {
            var invalidFilenameChars = Path.GetInvalidFileNameChars();
            return fileName.All(characterToTest => Array.BinarySearch(invalidFilenameChars, characterToTest) < 0);
        }

        #endregion

        #region SERVICE CALL CODE TO FETCH DATA

        /// <summary>
        /// Gets all saved searches
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="numberOfRecords"></param>
        /// <param name="sortColumn"></param>
        /// <param name="sortOrder"></param>
        /// <param name="allSavedSearch"></param>
        /// <returns>success or failure</returns>
        private bool GetAllSavedSearch(string pageNumber, string numberOfRecords, string sortColumn, string sortOrder,
            out List<SavedSearchEntity> allSavedSearch)
        {
            allSavedSearch = new List<SavedSearchEntity>();
            var uri = string.Empty;


            try
            {
                allSavedSearch = ReviewerSearchService.GetAllSavedSearch(pageNumber, numberOfRecords, sortColumn,
                    sortOrder);
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingAllSavedSearch, Constants.ErrorGettingAllSavedSearch,
                    uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Gets all tags
        /// </summary>
        /// <param name="allTags"></param>
        /// <returns>success or failure</returns>
        private bool GetTagDefinitions(out List<RVWTagBEO> allTags)
        {
            allTags = new List<RVWTagBEO>();
            var uri = string.Empty;
            const string scope = "all";

            try
            {
                allTags = RVWTagService.GetTagsDefinitions(_matterId, _collectionId, scope, "True");
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingAllTags, Constants.ErrorGettingAllTags,
                    uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get dataset details for a dataset identifier
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="dataset"></param>
        /// <returns>success or failure</returns>
        private bool GetDataSetDetailForDataSetId(long datasetId, out DatasetBEO dataset)
        {
            dataset = new DatasetBEO();
            var uri = string.Empty;

            try
            {
                dataset = DataSetService.GetDataSet(datasetId.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingDatasetDetails, Constants.ErrorGettingDatasetDetails,
                    Constants.ErrorGettingDatasetDetails + ":" + uri + ":" + ex.Message + ":" + ex.InnerException + ":" +
                    ex.StackTrace, ex);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Get all reasons for redaction
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="allReasonList"></param>
        /// <returns>success or failure</returns>
        private bool GetReasonsForRedaction(string datasetId, out List<RedactionReasonBEO> allReasonList)
        {
            allReasonList = new List<RedactionReasonBEO>();
            var uri = string.Empty;

            try
            {
                var knowledgeService = new KnowledgeService();
                allReasonList = knowledgeService.GetReasonsForRedaction(datasetId, Constants.DefaultLanguageId);
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingAllReasonForRedaction,
                    Constants.ErrorGettingAllReasonForRedaction,
                    uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Get document data/fields data for a document
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="docData"></param>
        /// <returns>success or failure</returns>
        private bool GetDocumentDataViewFromVault(string documentId, out RVWDocumentBEO docData)
        {
            docData = new RVWDocumentBEO();
            var uri = string.Empty;

            try
            {
                docData = DocumentService.GetDocumentData(_matterId, _collectionId, documentId, _createdByGuid,
                    false.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentFieldData, Constants.ErrorGettingDocumentFieldData, true,
                    uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Get document tags
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="documentTags"></param>
        /// <returns>success or failure</returns>
        private bool GetDocumentTags(string documentId, out List<RVWDocumentTagBEO> documentTags)
        {
            documentTags = new List<RVWDocumentTagBEO>();
            var uri = string.Empty;

            try
            {
                documentTags = DocumentService.GetDocumentTags(_matterId, _collectionId, documentId);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentTags, Constants.ErrorGettingDocumentTags, true,
                    uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Get the markup for a document
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="markup"></param>
        /// <returns>success or failure</returns>
        private bool GetRedactionXml(string documentId, out RVWMarkupBEO markup)
        {
            markup = new RVWMarkupBEO();
            var uri = string.Empty;

            try
            {
                markup = DocumentService.GetMarkupXML(_matterId, _redactableDocumentsetId, documentId);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentRedactionXml, Constants.ErrorGettingDocumentRedactionXml,
                    true, uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return false;
            }
            return true;
        }

        /// <summary>
        /// To get the User Business Entity
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="returnObject"></param>
        /// <returns></returns>
        private void GetUserBusinessEntity(string userGuid, out UserBusinessEntity returnObject)
        {
            returnObject = new UserBusinessEntity();

            try
            {
                userGuid.ShouldNotBe(null);
                //Bug # 130820 -Privilege Log with saved query option -generates log for all the documents in dataset
                //The below method gets user details for the given user guid along with organization information 
                returnObject = UserBO.GetUserByGuid(userGuid);
                //Bug # 130820 -Privilege Log with saved query option -generates log for all the documents in dataset
            }
            catch (Exception ex)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingUserDetails, Constants.ErrorGettingUserDetails,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, ex);
                return;
            }

            //Check if the user identifier exists
            if (returnObject.UserId == null)
            {
                CreateJobExceptionObject(ErrorCodes.ErrorGettingUserDetails, Constants.ErrorGettingUserDetails,
                    string.Empty, null);
            }
        }

        #endregion

        /// <summary>
        /// Verify if privilege log folder path and file name are valid.
        /// </summary>
        /// <param name="privilegeLogDetails">PrivilegeLogDetailsBEO  objects</param>
        /// <returns></returns>
        private void VerifyPrivilegeLogFolderPathAndFileName(PrivilegeLogDetailsBEO privilegeLogDetails)
        {
            if (!string.IsNullOrEmpty(privilegeLogDetails.PrivilegeLogFolder))
            {
                if (!Directory.Exists(privilegeLogDetails.PrivilegeLogFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(privilegeLogDetails.PrivilegeLogFolder);
                            //Create the directory if not exists
                    }
                    catch
                    {
                        CreateJobExceptionObject(ErrorCodes.LogFoldePathMissing, Constants.EventLogFolderPathMissing,
                            privilegeLogDetails.PrivilegeLogFolder, null);
                        return;
                    }
                }
            }
            else
            {
                CreateJobExceptionObject(ErrorCodes.LogFoldePathNotSpecified, Constants.EventLogFolderPathNotSpecified,
                    Constants.EventJobInitializationValue + ":" + Constants.EventLogFolderPathNotSpecified, null);
                return;
            }
            if (string.IsNullOrEmpty(privilegeLogDetails.PrivilegeLogName))
            {
                CreateJobExceptionObject(ErrorCodes.LogFileNameMissing, Constants.LogFileNameMissing,
                    Constants.LogFileNameMissing, null);
                return;
            }
            if (!IsFileNameValid(privilegeLogDetails.PrivilegeLogName))
            {
                CreateJobExceptionObject(ErrorCodes.LogFilehasInvalidCharacters, Constants.LogFilehasInvalidCharacters,
                    Constants.LogFilehasInvalidCharacters + ":" + privilegeLogDetails.PrivilegeLogName, null);
            }
        }

        private string GetProductionDocumentVaultField(string productionCollectionId, string docReferenceId,
            string fieldName)
        {
            try
            {
                return ProductionBO.GetProductionDocumentVaultField(_matterId, productionCollectionId, docReferenceId,
                    fieldName);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentFieldValues,
                    Constants.ErrorGettingDocumentProductionFieldValues, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, LogCategory.Task);
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates user current context
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="userBusinessEntity"></param>
        /// <returns></returns>
        private HttpContextBase CreateUserContext(string userGuid, UserBusinessEntity userBusinessEntity)
        {
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userSession = new UserSessionBEO();
            SetUserSession(userGuid, userBusinessEntity, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userBusinessEntity);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            return mockContext.Object;
        }

        /// <summary>
        /// This method sets the user details in session
        /// </summary>
        /// <param name="createdUserGuid">string</param>
        /// <param name="userProp">UserBusinessEntity</param>
        /// <param name="userSession">UserSessionBEO</param>
        private static void SetUserSession(string createdUserGuid, UserBusinessEntity userProp,
            UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdUserGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
            if (userProp.Organizations.Any())
                userSession.Organizations.AddRange(userProp.Organizations);
        }
    }
}