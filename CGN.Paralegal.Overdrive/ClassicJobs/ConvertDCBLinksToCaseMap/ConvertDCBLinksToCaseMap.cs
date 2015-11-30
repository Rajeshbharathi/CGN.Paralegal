//-----------------------------------------------------------------------------------------
// <copyright file="ConvertDCBLinksToCaseMap" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Malarvizhi</author>
//      <description>
//          Batch Job Class For Converting DCB Links to CaseMap
//      </description>
//      <changelog>
//          <date value="25-Mar-2011"></date>
//          <date value="29-Mar-2011">Modified to include dataset name, parameter changes</date>
//          <date value="30-Mar-2011">Namespace changed, Audit log added</date>
//          <date value="31-Mar-2011">Property added, Audit log modified</date>
//          <date value="12-Apr-2011">Request name, Linkback uri changed</date>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//          <date value="05/09/2012">Bug fix 100297</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//         <date value="03/14/2014">ADM-REPORTS-003  - Included code changes for New Audit Log</date>
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
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.External.DataAccess.CaseMap;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.MiddleTier.CaseMap;
using LexisNexis.Evolution.Infrastructure.Common;
#endregion

namespace LexisNexis.Evolution.BatchJobs.ConvertDCBLinksToCaseMap
{
    ///<summary>
    ///Batch Job Class For Converting DCB Links to CaseMap
    ///</summary>
    [Serializable]
    public class ConvertDCBLinksToCaseMap : BaseJob<BaseJobBEO, ConvertDCBLinkTaskBusinessEntityObject>
    {
        #region Private Fields
        /// <summary>
        /// Guid of the current user
        /// </summary>
        private string _createdByUserGuid = string.Empty;

        /// <summary>
        /// Conversion ID for DCBLinksToCaseMap
        /// </summary>
        private long _conversionId = 0;

        /// <summary>
        /// Holds status of a job
        /// </summary>
        private bool _isJobFailed = false;

        /// <summary>
        /// Name of the job request
        /// </summary>
        private string _requestDescription = string.Empty;

        /// <summary>
        /// Query to search documents of type DCB
        /// </summary>
        private string _query = string.Empty;

        /// <summary>
        /// Started Time
        /// </summary>
        private readonly DateTime startedTime = DateTime.UtcNow;

        /// <summary>
        /// Document count
        /// </summary>
        private long _documentCount = 0;


        #endregion

        #region Constructor
        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public ConvertDCBLinksToCaseMap()
        {

        }
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
            BaseJobBEO jobBeo = new BaseJobBEO();
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_Initialize_Start, EventLogEntryType.Information);
                //Initialize the JobBEO object
                jobBeo.JobId = jobId;
                jobBeo.JobRunId = jobRunId;
                jobBeo.JobName = Constants.JobTypeName + " " + DateTime.UtcNow;
                //fetch UserEntity
                UserBusinessEntity userBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                jobBeo.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A")) ? userBusinessEntity.UserId : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
                userBusinessEntity = null;
                jobBeo.JobTypeName = Constants.JobTypeName;
                // Default settings
                jobBeo.StatusBrokerType = BrokerType.Database;
                jobBeo.CommitIntervalBrokerType = BrokerType.ConfigFile;
                jobBeo.CommitIntervalSettingType = SettingType.CommonSetting;

                if (bootParameters != null)
                {
                    jobBeo.BootParameters = bootParameters;
                }
                else
                {
                    throw new EVException().AddDbgMsg("{0}:{1}:{2}", jobId, Constants.Event_Job_Initialize_Start, Constants.XmlNotWellFormed).
                        AddResMsg(ErrorCodes.XmlStringNotWellFormed);
                }
                Tracer.Info("{0} - {1}:{2}", Constants.JobTypeName, jobId, Constants.Event_Job_Initialize_Success);
            }
            catch (EVJobException ex)
            {
                _isJobFailed = true;
                ex.AddDbgMsg("{0}:{1}", Constants.JobTypeName, MethodInfo.GetCurrentMethod().Name);
                throw;
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                ex.AddDbgMsg("{0}:{1}", Constants.JobTypeName, MethodInfo.GetCurrentMethod().Name);
                JobLogInfo.AddParameters("Problem in" + Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name);
                EVJobException jobException = new EVJobException(ErrorCodes.InitializeError, ex, JobLogInfo);
                throw (jobException);
            }
            return jobBeo;
        }

        /// <summary>
        /// Generates Concvert DCB Link tasks
        /// </summary>
        /// <param name="jobParameters">Job BEO</param>
        /// <param name="previouslyCommittedTaskCount"> </param>
        /// <returns>List of Job Tasks (BEOs)</returns>
        protected override Tasks<ConvertDCBLinkTaskBusinessEntityObject> GenerateTasks(BaseJobBEO jobParameters, out int previouslyCommittedTaskCount)
        {
            Tasks<ConvertDCBLinkTaskBusinessEntityObject> tasks = null;
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_GenerateTask_Start, EventLogEntryType.Information);
                previouslyCommittedTaskCount = 0;
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobId, MethodInfo.GetCurrentMethod().Name, EventLogEntryType.Information);
                tasks = GetTaskList<BaseJobBEO, ConvertDCBLinkTaskBusinessEntityObject>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;
                if (tasks.Count <= 0)
                {
                    ConvertDCBLinkTaskBusinessEntityObject convertDcbLinkTask = new ConvertDCBLinkTaskBusinessEntityObject();
                    convertDcbLinkTask = (ConvertDCBLinkTaskBusinessEntityObject)XmlUtility.DeserializeObject(jobParameters.BootParameters, typeof(ConvertDCBLinkTaskBusinessEntityObject));
                    _query = ApplicationConfigurationManager.GetValue(Constants.SearchQuery);
                    _createdByUserGuid = convertDcbLinkTask.CreateByUserGuid;
                    string pageSize = "1";
                    DocumentQueryEntity docQueryEntity = GetDocumentQueryEntity(convertDcbLinkTask.DatasetId.ToString(CultureInfo.InvariantCulture), _query, pageSize);
                    docQueryEntity.TransactionName = "ConvertDCBLinksToCaseMap - GenerateTasks";
                    ReviewerSearchResults searchResults = JobSearchHandler.GetSearchResults(docQueryEntity);
                    if (searchResults.TotalRecordCount > 0)
                    {
                        convertDcbLinkTask.TaskNumber = 1;
                        convertDcbLinkTask.TaskPercent = 100;
                        convertDcbLinkTask.TaskComplete = false;
                    }
                    _documentCount = searchResults.TotalHitCount;
                    tasks.Add(convertDcbLinkTask);
                }
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_GenerateTask_Success, EventLogEntryType.Information);

            }
            catch (EVJobException ex)
            {
                _isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                throw;
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                EVJobException jobException = new EVJobException(ErrorCodes.GenerateTasksError, ex, JobLogInfo);
                throw (jobException);
            }
            return tasks;
        }

        /// <summary>
        /// Does atomic 1)Gets DCB document 2) Generate xml 3) Update xml file to database.
        /// </summary>
        /// <param name="task">ConvertDCBLinkTaskBusinessEntityObject</param>
        /// <param name="jobParameters">Job business entity</param>
        /// <returns></returns>
        protected override bool DoAtomicWork(ConvertDCBLinkTaskBusinessEntityObject task, BaseJobBEO jobParameters)
        {
            bool StatusFlag = true;// Function return status.
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_DoAtomicWork_Start, EventLogEntryType.Information);
                // Perform Atomic Task
                DCBLinkCollectionBEO dcbLinks = new DCBLinkCollectionBEO();
                DCBLinkBEO dcbLink;
                ReviewerSearchResults searchResults = null;
                if (_documentCount > 0)
                {
                    DocumentQueryEntity documentQueryEntity = GetDocumentQueryEntity(task.DatasetId.ToString(CultureInfo.InvariantCulture), _query, _documentCount.ToString(CultureInfo.InvariantCulture));
                    documentQueryEntity.TransactionName = "ConvertDCBLinksToCaseMap - DoAtomicWork";
                    searchResults = JobSearchHandler.GetSearchResults(documentQueryEntity);
                    
                }
                List<DCBLinkBEO> linkList = new List<DCBLinkBEO>();
                string uuid = string.Empty;
                int count = 0;
                foreach (DocumentResult document in searchResults.ResultDocuments)
                {
                    dcbLink = new DCBLinkBEO();
                    DocumentIdentifierBEO docIdentifier = new DocumentIdentifierBEO(document.MatterID.ToString(CultureInfo.InvariantCulture), document.CollectionID, document.DocumentID);
                    if (count == 0)
                    {
                        uuid = docIdentifier.UniqueIdentifier.Replace(docIdentifier.DocumentId, string.Empty);
                    }
                    dcbLink.NewDocumentId = docIdentifier.DocumentId;
                    
                    List<FieldResult> fieldValues = document.Fields.Where(f => System.String.Compare(f.Name, EVSystemFields.DcbId, System.StringComparison.OrdinalIgnoreCase) == 0).ToList();
                    dcbLink.OldDocumentId = fieldValues.Any() ? fieldValues.First().Value.Replace("[", "(").Replace("]", ")") : string.Empty;
                    linkList.Add(dcbLink);
                    dcbLink.CollectionId = docIdentifier.CollectionId;
                    dcbLink.DCN = docIdentifier.DCN;
                    if (docIdentifier.MatterId !=null)
                        dcbLink.MatterId = long.Parse(docIdentifier.MatterId);

                    count++;
                }
                linkList.SafeForEach(l => dcbLinks.Links.Add(l));
                dcbLinks.UrlApplicationLink = task.LinkBackUrl;

                string xml = DocumentFactBusinessObject.GenerateDcbLinksXml(dcbLinks, uuid);
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] content = encoding.GetBytes(xml);
                StringBuilder nameBuilder = new StringBuilder();
                nameBuilder.Append(Constants.TaskName);
                nameBuilder.Append(Constants.OnDate);
                nameBuilder.Append(startedTime.ConvertToUserTime());
                string requestName = nameBuilder.ToString();

                nameBuilder = new StringBuilder();
                nameBuilder.Append(Constants.TaskName);
                _requestDescription = nameBuilder.ToString();

                string fileType = ApplicationConfigurationManager.GetValue(Constants.FileType);
                _conversionId = CaseMapDAO.SaveConversionResults(jobParameters.JobRunId, requestName, _requestDescription, content, fileType, _createdByUserGuid);
                StatusFlag = true;
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_DoAtomicWork_Success, EventLogEntryType.Information);
                TaskLogInfo.AddParameters(Constants.DataSetName, task.DatasetName);
            }
            catch (EVTaskException ex)
            {
                _isJobFailed = true;
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                throw;
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                // Handle exception in Generate Tasks
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                EVTaskException jobException = new EVTaskException(ErrorCodes.DoAtomicError, ex);
                TaskLogInfo.StackTrace = ex.Message + Constants.LineBreak + ex.StackTrace;
                TaskLogInfo.AddParameters(Constants.DataSetId, task.DatasetId.ToString(CultureInfo.InvariantCulture));
                TaskLogInfo.AddParameters(Constants.DataSetName, task.DatasetName);
                TaskLogInfo.TaskKey = Constants.DataSetName + ":" + task.DatasetName;
                jobException.LogMessge = TaskLogInfo;
                throw (jobException);
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
                EvLog.WriteEntry(Constants.JobTypeName + " - " + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_ShutDown, EventLogEntryType.Information);
                #region Notification section
                //get job details
                JobBusinessEntity jobDetails = JobMgmtBO.GetJobDetails(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));
                if (jobDetails != null && jobDetails.NotificationId > 0)
                {
                    string defaultMessage = string.Empty;
                    defaultMessage = _isJobFailed ? string.Format(Constants.NotificationErrorMessageFormat, !string.IsNullOrEmpty(_requestDescription) ? _requestDescription : Constants.TaskName) : string.Format(Constants.NotificationSuccessMessageFormat, _requestDescription, _documentCount, ApplicationConfigurationManager.GetValue(Constants.CaseMapUrl), _conversionId.ToString());
                    CustomNotificationMessage = defaultMessage;
                }
                #endregion
                JobLogInfo.AddParameters(Constants.CreatedBy, jobParameters.JobScheduleCreatedBy);
                JobLogInfo.AddParameters(Constants.DocumentIncludedInXml, Convert.ToString(_documentCount));
                JobLogInfo.AddParameters(Constants.TaskStartTime, Convert.ToString(startedTime));
                JobLogInfo.AddParameters(Constants.TaskEndTime, Convert.ToString(DateTime.UtcNow));
            }
            catch (EVJobException ex)
            {
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                throw;
            }
            catch (Exception ex)
            {
                _isJobFailed = true;
                // Handle exception in Generate Tasks
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                EVJobException jobException = new EVJobException(ErrorCodes.DoAtomicError, ex);
                JobLogInfo.AddParameters(Constants.JobRunId, jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture));
                jobException.LogMessge = JobLogInfo;
                throw (jobException);
            }
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Get service uri
        /// </summary>
        /// <param name="datasetId">Datase Id</param>
        /// <param name="searchQuery">Search Query</param>
        /// <param name="pageSize">Size of the page results</param>
        /// <returns>Uri for Search results</returns>
        private DocumentQueryEntity GetDocumentQueryEntity(string datasetId, string searchQuery, string pageSize)
        {
            DocumentQueryEntity documentQueryEntity = new DocumentQueryEntity
            {
                DocumentStartIndex = 0,
                QueryObject = new SearchQueryEntity
                {
                    DatasetId = Convert.ToInt32(datasetId),
                    IsConceptSearchEnabled = false
                }
            };
            documentQueryEntity.QueryObject.QueryList.Add(new Query(searchQuery));
            documentQueryEntity.IgnoreDocumentSnippet = true;

            int docCount = 0;
            Int32.TryParse(pageSize, out docCount);
            if (docCount <= 0)
            {
                docCount = 1;
            }
            documentQueryEntity.DocumentCount = docCount;
            return documentQueryEntity;
        }


        #endregion
    }
}


