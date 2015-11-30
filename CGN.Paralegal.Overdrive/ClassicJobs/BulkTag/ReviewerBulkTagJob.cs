# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="ReviewerBulkTagJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kokila Bai S L</author>
//      <description>
//          Actual backend Backend process which does the bulk tagging
//      </description>
//      <changelog>
//          <date value="26/11/2010">File added</date>
//          <date value="20/12/2010">Code Review Changes</date>
//          <date value="10/3/2011">Bug Fix for 77748</date>
//          <date value="04 May 2011">Update for bulk tagging performance</date>
//	        <date value="12-19-2011">Bug Fix #81330</date>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//	        <date value="02-14-2012">Fix for bugs 96511, 96517</date>
//	        <date value="02-14-2012">Fix for bug 94180</date>
//	        <date value="02/24/2012">Fix for bugs 96992, 96996, 96998</date>
//          <date value="2-Mar-2012">Fix for bugs 97469, 97496 </date>
//          <date value="13-Mar-2012">Fix for bug 97880</date>
//          <date value="30-Apr-2012">Fix for bug 99913</date>
//          <date value="05/09/2012">Bug fix 100297</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//         <date value="04/09/2012">Applied new policy changes - 108300</date>
//         <date value="17/09/2012">Removed CanManageTagging, CanManageFields policy - 108865</date>
//          <date value="10/09/2012">Task 109236 - Tag all families and Threads in results view</date>
//         <date value="30/10/2012">Removed CanManageTagging, CanManageFields, CanManageFieldSettings policy - 108865</date>
//         <date value="11/09/2012">Fix for bug 112426</date>
//         <date value="11/21/2012">Fix for bug 112426 as per Jeremy's clarification</date>
//         <date value="11/28/2012">Fix for bug 114033 - Fix for tag families and threads case</date>
//         <date value="12/12/2012">Fix for bug 126848 - Initialize reviewset Id in business entity conversion for locked doc check</date>
//         <date value="12/12/2012">Fix for bug 114553 - Fix to tag all docs in thread when both TAF and TADF behaviors exists for the tag</date>
//         <date value="12/14/2012">Fix for bug 126455 - Fix to handle group by and cluster filters</date>
//         <date value="12/20/2012">Fix for bug 111449 - Bulk tagging job termination fix</date>
//         <date value="01/16/2013">BugFix#127946 - System tagging performance fix : babugx</date>
//         <date value="01/16/2013">BugFix#111948 - Tag duplicate issue fix</date>
//         <date value="02/26/2013">Fix for bulk tagging issue reported during BVT on Prod2</date>
//         <date value="05/28/2013">BugFix 131020</date>
//         <date value="06/13/2013">BugFix 131020</date>
//          <date value="07-17-2013">Bug # 147760 - Fix to tag documents in the manage conversion
//          <date value="09/27/2013">Task # 150468 -ADM -REVIEWSET - 002 -  Review Status Tagging Per Binder Part2
//          <date value="10/07/2013">Dev Bug # 154032 -:Review Status Tagging doesnt work in bulk (Intermittent) on on review set complete and regular bulk tagging
//          <date value="10/07/2013">Dev Bug  # 154336 -ADM -ADMIN - 006 - Import /Production Reprocessing reprocess all documents even with filter and all and other migration fixes
//          <date value="10/23/2013">Bug # 155287 -ADM -ADMIN - 006 -  Fix for tagging  the filtered documents  only when select all used 
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVPolicy;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.TraceServices;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.IR;

namespace LexisNexis.Evolution.BatchJobs.ReviewerBulkTag
{
    /// <summary>
    /// This class represents Reviewer Bulk Tag job for
    /// tagging more than 1 document
    /// </summary>
    [Serializable]
    public class ReviewerBulkTagJob : BaseJob<BulkTagJobBusinessEntity, BulkTagJobTaskBusinessEntity>
    {
        #region Variables

        private string m_JobStartDate;
        private string m_JobEndDate;
        private int m_DocumentsTaggedCount; //Number of documents that have been tagged for this tag
        private int m_DocumentsAlreadyTaggedCount; //Number of documents that have already been tagged with this tag
        private int m_NumberOfTasks;
        private int m_DocumentsFailedcount;
        private StringBuilder m_JobNameTitle;
        private bool m_IsTagNotExists; //indicate if any task in the job was cancelled due to tag deletion midway
        private int m_WindowSize; //determine number of documents that will be processed in a single task
        private BulkTaggingNotificationBEO m_Notification;
        private List<string> m_DeletedTagIdsList;
        private bool m_IsAuditAndNotificationDone;
        private int m_JobId;
    
        private UserBusinessEntity m_UserDetails;
        private RVWTagBEO m_TagDetails;
        private bool m_IsAnyDocsLocked;
        private string createdByGuid = string.Empty;

        private string _mBinderId;

        [NonSerialized] private HttpContextBase userContext;

        /// <summary>
        /// Document Vault Manager Properties
        /// </summary>
        private static IDocumentVaultManager m_DocumentVaultMngr;

        public static IDocumentVaultManager DocumentVaultMngr
        {
            get { return m_DocumentVaultMngr ?? (m_DocumentVaultMngr = new DocumentVaultManager()); }
            set { m_DocumentVaultMngr = value; }
            }

        #endregion

        #region Initialize

        /// <summary>
        /// This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy"> </param>
        /// <returns>BulkTaskJobTaskBusinessEntity</returns>
        protected override BulkTagJobBusinessEntity Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            #region Initialize Job related variables

            m_JobStartDate = string.Empty;
            m_JobEndDate = string.Empty;
            m_DocumentsTaggedCount = 0;
            m_DocumentsAlreadyTaggedCount = 0;
            m_NumberOfTasks = 0;
            m_DeletedTagIdsList = new List<string>();
            m_IsTagNotExists = false;
            m_JobId = jobId;
            m_IsAnyDocsLocked = false;
            if (
                !Int32.TryParse(ApplicationConfigurationManager.GetValue(Constants.BulkTaggingWindowSize),
                    out m_WindowSize))
            {
                m_WindowSize = Constants.DefaultWindowSize;
            }

                     
            m_Notification = new BulkTaggingNotificationBEO();
            m_IsAuditAndNotificationDone = false;

            #endregion

            BulkTagJobBusinessEntity jobBeo = null;
            BulkTagJobBusinessEntity bulkTaggingOperationDetails = null;
            try
            {
                // Initialize the JobBEO
                jobBeo = new BulkTagJobBusinessEntity {JobId = jobId, JobRunId = jobRunId};

                //filling properties of the job parameter
                //Fetch the user information
                m_UserDetails = UserBO.GetUserUsingGuid(createdBy);
                createdByGuid = createdBy;

                userContext = CreateUserContext();
                EVHttpContext.CurrentContext = userContext;

                jobBeo.JobScheduleCreatedBy = (m_UserDetails.DomainName.Equals(Constants.LocalDomain))
                    ? m_UserDetails.UserId
                    : m_UserDetails.DomainName + Constants.BackSlash + m_UserDetails.UserId;
                jobBeo.JobTypeName = Constants.JobTypeName;
                jobBeo.BootParameters = bootParameters;
                //constructing BulkTagJobBusinessEntity from boot parameter by de serializing
                bulkTaggingOperationDetails = GetBulkTaggingOperationDetails(bootParameters);
                jobBeo.OperationMode = BulkTaskMode.Tagging;
                jobBeo.JobName = Constants.JobName;
                jobBeo.JobTypeName = Constants.JobName;
                m_JobNameTitle = new StringBuilder();
                m_JobNameTitle.Append(Constants.JobName);
                m_JobNameTitle.Append(Constants.Space);
                m_JobNameTitle.Append(Constants.Colon);
                m_JobNameTitle.Append(Constants.Space);
                //Log
                EvLog.WriteEntry(jobId + Constants.JobInitializationKey, Constants.JobInitializationValue,
                    EventLogEntryType.Information);

                // Default settings
                jobBeo.StatusBrokerType = BrokerType.Database;
                jobBeo.CommitIntervalBrokerType = BrokerType.ConfigFile;
                jobBeo.CommitIntervalSettingType = SettingType.CommonSetting;

                EvLog.WriteEntry(jobId + Constants.AuditBootParameterKey, Constants.AuditBootParameterValue,
                    EventLogEntryType.Information);

                if (bulkTaggingOperationDetails != null)
                {
                    jobBeo.IsOperationTagging = bulkTaggingOperationDetails.IsOperationTagging;
                    jobBeo.IsTagAllDuplicates = bulkTaggingOperationDetails.IsTagAllDuplicates;
                    jobBeo.IsTagAllFamily = bulkTaggingOperationDetails.IsTagAllFamily;
                    jobBeo.TagDetails = bulkTaggingOperationDetails.TagDetails;
                    jobBeo.IsSelectAll = bulkTaggingOperationDetails.IsSelectAll;
                    jobBeo.DocumentListDetails = bulkTaggingOperationDetails.DocumentListDetails;
                    _mBinderId = jobBeo.BinderId = bulkTaggingOperationDetails.BinderId;
                    jobBeo.Filters = bulkTaggingOperationDetails.Filters;
                    jobBeo.ReprocessJobId = bulkTaggingOperationDetails.ReprocessJobId;
                    jobBeo.JobSource = bulkTaggingOperationDetails.JobSource;
                }
                else
                {
                    EvLog.WriteEntry(jobId + Constants.JobInitializationKey, Constants.EventXmlNotWellFormed,
                        EventLogEntryType.Error);
                    throw new EVException().AddResMsg(ErrorCodes.ImpXmlFormatErrorId);
                }
                new List<string> {Constants.ConversionReprocess};
                //this is to not to check the review set rules if the tagging job is scheduled from other than reviewer screen.
                //For example :If tagging is scheduled from reprocessing screen then bulk tag should not validate the review set rules 
                //as the job is scheduled by Admin

                IndexWrapper.InitBulkIndex(jobBeo.TagDetails.MatterId);
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(jobId + Constants.EventInitializationExceptionValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, exp, LogCategory.Job, string.Empty, ErrorCodes.ProblemInJobInitialization);
            }
            return jobBeo;
        }

        private HttpContextBase CreateUserContext()
        {
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userProp = m_UserDetails;
            var userSession = new UserSessionBEO();
            SetUserSession(createdByGuid, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
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
        }

        #endregion

        /// <summary>
        /// This is the overriden GenerateTasks() method. 
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount"> </param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<BulkTagJobTaskBusinessEntity> GenerateTasks(BulkTagJobBusinessEntity jobParameters,
            out int previouslyCommittedTaskCount)
        {
            jobParameters.ShouldNotBe(null);
            jobParameters.TagDetails.ShouldNotBe(null);
            previouslyCommittedTaskCount = 0;
            try
            {
                EvLog.WriteEntry(Constants.JobName + Constants.SpaceHiphenSpace + jobParameters.JobId,
                                 Constants.AuditGenerateTaskValue,
                                 EventLogEntryType.Information);
                //Initialize job start time
                m_JobStartDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                var tasks = new Tasks<BulkTagJobTaskBusinessEntity>();
                m_TagDetails = RVWTagService.GetTag(jobParameters.TagDetails.Id.ToString(CultureInfo.InvariantCulture),
                                                    jobParameters.TagDetails.CollectionId,
                                                    jobParameters.TagDetails.MatterId.ToString(
                                                        CultureInfo.InvariantCulture));
                if (m_TagDetails == null)
                {
                    BulkTaskCancelFromGenerateTasks(jobParameters, Constants.AuditTagNotFound, true);
                }
                else
                {
                    if (!m_TagDetails.Status)
                    {
                        BulkTaskCancelFromGenerateTasks(jobParameters, m_TagDetails.Name, true);
                    }
                }

                if (!m_IsTagNotExists)
                {
                    #region Get filtered list of documents

                    //List<FilteredDocumentBusinessEntity> filteredDocuments;
                    //filteredDocuments = GetFilteredListOfDocuments(jobParameters);

                    var totalDocuments = GetDocuments(jobParameters);

                    #endregion

                    var taskNumber = 0;
                    var isAnyReviewSetLocked = false;
                    taskNumber = GenerateDocumentTagTasks(jobParameters, tasks, totalDocuments);
                    m_IsAnyDocsLocked = isAnyReviewSetLocked;
                    if (totalDocuments != null && totalDocuments.Count == 0)
                    {
                        if (m_TagDetails != null)
                            BulkTaskCancelFromGenerateTasks(jobParameters, m_TagDetails.Name, false);
                        m_IsTagNotExists = false; //Reset this variable's value back to correct value
                    }
                    //Send notification if no tasks found
                    if (m_NumberOfTasks == 0 && !isAnyReviewSetLocked)
                    {
                        AuditAndNotifyForNoTasksFound(jobParameters.JobScheduleCreatedBy,
                                                      jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                                                      jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture),
                                                      (GetBulkTaggingOperationDetails(jobParameters.BootParameters)).
                                                          DocumentListDetails.SearchContext,
                                                      jobParameters.IsTagAllFamily,
                                                      jobParameters.IsTagAllDuplicates,
                                                      jobParameters.IsOperationTagging,
                                                      m_TagDetails != null
                                                          ? m_TagDetails.Name
                                                          : Constants.AuditTagNotFound);
                    }
                }
                return tasks;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(m_JobNameTitle.ToString() + jobParameters.JobId + Constants.AuditGenerateTaskValue,
                                 exp.Message, EventLogEntryType.Error);
                LogException(JobLogInfo, exp, LogCategory.Job, string.Empty, ErrorCodes.ProblemInGenerateTasks);
                return null;
            }
        }

        /// <summary>
        /// This is the overriden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters"> </param>
        /// <returns>Status of the operation.</returns>
        protected override bool DoAtomicWork(BulkTagJobTaskBusinessEntity task, BulkTagJobBusinessEntity jobParameters)
        {
            try
            {
                return true;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName + Constants.AuditInDoAtomicWorkValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(TaskLogInfo, exp, LogCategory.Task, task.TaskKey, ErrorCodes.ProblemInDoAtomicWork);
                return false;
            }
        }

        /// <summary>
        /// This is the overridden ShutDown() method
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job</param>
        protected override void Shutdown(BulkTagJobBusinessEntity jobParameters)
        {
            try
            {
                if (!m_IsAuditAndNotificationDone)
                {
                    var tagLog =
                        RVWTagBO.GetTagLog(Convert.ToInt32(jobParameters.DocumentListDetails.SearchContext.DataSetId),
                                      Convert.ToInt32(m_JobId));

                    m_DocumentsAlreadyTaggedCount = tagLog.AlreadyTag;
                    m_DocumentsTaggedCount = tagLog.DocumentTag;
                    m_DocumentsFailedcount = tagLog.FailedTag;

                    //Get job end date
                    m_JobEndDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

                    //Fetch the Job details
                    var jobDetail = JobService.GetJobDetails(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));

                    jobDetail.CreatedById = m_UserDetails.UserGUID;

                    if (jobDetail.NotificationId > 0)
                    {
                        var location = GetTaggingLocation(jobParameters.DocumentListDetails.SearchContext);
                        CreateNotificationMessageForTag(jobParameters, jobDetail, location);
                    }
                }
              
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName + Constants.AuditInShutdownValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(JobLogInfo, exp, LogCategory.Task, string.Empty, ErrorCodes.ProblemInShutDown);
            }
            finally
            {
                IndexWrapper.CommitBulkIndex(jobParameters.TagDetails.MatterId);

            }
        }

        #region Helper Methods

        /// <summary>
        /// End bulk tagging job if tag is deleted before the job starts
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="tagDetailToAudit">Tag Detail to be sent for auditing</param>
        /// <param name="doAudit"> </param>
        private void BulkTaskCancelFromGenerateTasks(BulkTagJobBusinessEntity jobParameters, string tagDetailToAudit,
            bool doAudit)
        {
            //Get job end date
            m_JobEndDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            //Cancel bulk tagging / untagging
            CancelBulkTask(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));

            if (doAudit)
            {
                //Audit and send notification
                AuditAndNotifyForNoTasksFound(jobParameters.JobScheduleCreatedBy,
                    jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                    jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture),
                    (GetBulkTaggingOperationDetails(jobParameters.BootParameters)).DocumentListDetails.SearchContext,
                    jobParameters.IsTagAllFamily, jobParameters.IsTagAllDuplicates, jobParameters.IsOperationTagging,
                    tagDetailToAudit);
            }
        }

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="logInfo"> </param>
        /// <param name="exp">exception received</param>
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key to identify the Task, need for task log only</param>
        /// <param name="errorCode"> </param>
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

        #region "Search - To fetch originally qualifying documents"

        private List<BulkDocumentInfoBEO> GetDocuments(BulkTagJobBusinessEntity jobParameters)
        {
            var mSearchContext = GetSearchContext(jobParameters);
            var documents = GetDocuments(mSearchContext);

            if (documents.Any())
            {
                documents = GatherFamiliesAndDuplicates(documents, jobParameters);
            }
            return documents;
        }

        private DocumentQueryEntity GetSearchContext(BulkTagJobBusinessEntity jobParameters)
        {
            if (!string.IsNullOrEmpty(jobParameters.JobSource) &&
                jobParameters.JobSource.Equals("reprocessfilterdocuments", StringComparison.InvariantCultureIgnoreCase))
            {
                SetSelectedDocumentsForReprocessTagging(jobParameters);
            }

            var documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    ReviewsetId = jobParameters.DocumentListDetails.SearchContext.ReviewSetId,
                    DatasetId = jobParameters.DocumentListDetails.SearchContext.DataSetId,
                    MatterId = jobParameters.DocumentListDetails.SearchContext.MatterId,
                    IsConceptSearchEnabled = jobParameters.DocumentListDetails.SearchContext.IsConceptSearchEnabled,
                },
            };

            #region Initialize Bin filters

            if (!string.IsNullOrEmpty(jobParameters.DocumentListDetails.SearchContext.BinningState))
            {
                var binquerys = new List<BinFilter>();
                string[] separator = {"AND"};
                var selectedList = jobParameters.DocumentListDetails.SearchContext.BinningState.Trim()
                    .Split(separator, StringSplitOptions.None);
                foreach (var query in selectedList)
                {
                    var bins = query.Split(':');
                    if (bins.Length > 0)
                    {
                        var binvalue = string.Empty;
                        for (var i = 1; i < bins.Length; i++)
                        {
                            if (binvalue != string.Empty)
                            {
                                binvalue = binvalue + ":";
                            }
                            binvalue = binvalue + bins[i];
                        }

                        binquerys.Add(new BinFilter {BinField = bins[0], BinValue = binvalue});
                        documentQueryEntity.QueryObject.BinFilters.Clear();
                        documentQueryEntity.QueryObject.BinFilters.AddRange(binquerys);
                    }
                }
            }

            #endregion

            // Max-num
            documentQueryEntity.DocumentCount = 999999;


            var outputFields = new List<Field>();
            outputFields.AddRange(new List<Field>
            {
                new Field {FieldName = Constants.DcnField},
                new Field {FieldName = EVSystemFields.FamilyId},
                new Field {FieldName = EVSystemFields.DuplicateId}
            });
            documentQueryEntity.OutputFields.AddRange(outputFields); //Populate fetch duplicates fields
           
            documentQueryEntity.TotalRecallConfigEntity.IsTotalRecall = true;
            documentQueryEntity.IncludeFamilies = jobParameters.IsTagAllFamily;
            documentQueryEntity.IncludeDuplicates = jobParameters.IsTagAllDuplicates;

            documentQueryEntity.QueryObject.QueryList.Clear();
            documentQueryEntity.QueryObject.QueryList.Add(new Query {SearchQuery = ConstructSearchQuery(jobParameters)});
            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});
            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.DocumentStartIndex = 0;
            return documentQueryEntity;
        }

        /// <summary>
        /// Construct the query to retrieve selected documents along with duplicates if required
        /// </summary>
        /// <param name="jobParameters">Job parameters</param>
        /// <returns>Search query string</returns>
        private static string ConstructSearchQuery(BulkTagJobBusinessEntity jobParameters)
        {
            var tmpQuery = string.Empty;
            var selectionQuery = string.Empty;
            if (!string.IsNullOrEmpty(jobParameters.DocumentListDetails.SearchContext.Query))
            {
                tmpQuery = jobParameters.DocumentListDetails.SearchContext.Query;
            }
            switch (jobParameters.DocumentListDetails.GenerateDocumentMode)
            {
                case DocumentSelectMode.UseSelectedDocuments:
                    {
                        //Resetting the tmpQuery to empty string since it is not required when selected documents are sent - 
                        //to handle the issue when there are OR operators in the query or the search is done using concept search 
                        //and the search term has relevant synonyms
                        tmpQuery = string.Empty;
                        jobParameters.DocumentListDetails.SelectedDocuments.ForEach(d =>
                                                                                    selectionQuery +=
                                                                                    string.Format("{0}:\"{1}\" OR ",
                                                                                                  EVSystemFields.DocumentKey, d));
                       

                        if (!string.IsNullOrEmpty(selectionQuery))
                        {
                            selectionQuery = selectionQuery.Substring(0,
                                                                      selectionQuery.LastIndexOf(" OR ",
                                                                                                 StringComparison.Ordinal));
                            tmpQuery = string.IsNullOrEmpty(tmpQuery)
                                           ? selectionQuery
                                           : string.Format("({0} AND {1})", tmpQuery, selectionQuery);
                        }

                        break;
                    }
                case DocumentSelectMode.QueryAndExclude:
                    {
                        jobParameters.DocumentListDetails.DocumentsToExclude.ForEach(d =>
                                                                                     selectionQuery +=
                                                                                     string.Format("(NOT {0}:\"{1}\") AND ",
                                                                                                   EVSystemFields.DocumentKey, d));
                        if (!string.IsNullOrEmpty(selectionQuery))
                        {
                        selectionQuery = selectionQuery.Substring(0,
                            selectionQuery.LastIndexOf(" AND ", StringComparison.Ordinal));
                            tmpQuery = string.IsNullOrEmpty(tmpQuery)
                                           ? selectionQuery
                                           : string.Format("({0} AND {1})", tmpQuery, selectionQuery);
                        }
                        break;
                    }
            }
            return tmpQuery;
        }

        /// <summary>
        /// This method is the actual search to search engine to determine actually qualifying document(s)
        /// </summary>
        /// <param name="searchContext"></param>
        /// <returns></returns>
        public List<BulkDocumentInfoBEO> GetDocuments(DocumentQueryEntity searchContext)
        {
            var tagDocuments = new List<BulkDocumentInfoBEO>();
            searchContext.TransactionName = "ReviewerBulkTagJob - GetDocuments";
             
            var searchResults = SearchBo.Search(searchContext, true);

            if (searchResults==null||searchResults.Documents==null) return tagDocuments;
            searchResults.Documents.SafeForEach(r => tagDocuments.Add(ConvertToDocumentIdentityRecord(r)));
            return tagDocuments;
        }

        # endregion

        #region "Families & duplicates retrieval - From Vault DB"

        /// <summary>
        /// Gathers the families and duplicates.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="jobParameters">The job parameters.</param>
        /// <returns>List&lt;BulkDocumentInfoBEO&gt;.</returns>
        public List<BulkDocumentInfoBEO> GatherFamiliesAndDuplicates(List<BulkDocumentInfoBEO> documents,
            BulkTagJobBusinessEntity jobParameters)
        {
            //TODO: This method has to refactor
            var originalDocuments = documents;
            //dictionary to hold list of documents to update
            var documentList = originalDocuments.ToDictionary(a => a.DocumentId, a => a);

            if (jobParameters.IsTagAllDuplicates ||
                jobParameters.TagDetails.TagBehaviors.Exists(
                            x => string.Compare(x.BehaviorName, Constants.TagAllDuplicatesBehaviorName, true,
                                                CultureInfo.InvariantCulture) == 0))
            {
                GetDuplicateDocumentsInParallel(jobParameters, originalDocuments, documentList);
            }

            // Refine family documents based on the tag behavior (families / threads/ both)
            if (PolicyManager.IsAllowFolderPolicy(EVPolicies.CanChangeTagState,
                                                  jobParameters.TagDetails.DatasetId.ToString(CultureInfo.InvariantCulture)))
            {
                if (jobParameters.IsTagAllFamily || (
                         jobParameters.TagDetails.TagBehaviors.Exists(
                             x =>
                             string.Compare(x.BehaviorName, Constants.TagAllFamilyBehaviorName, true,
                                            CultureInfo.InvariantCulture) == 0) &&
                         jobParameters.TagDetails.TagBehaviors.Exists(
                             x =>
                             string.Compare(x.BehaviorName, Constants.TagAllThreadBehaviorName, true,
                                            CultureInfo.InvariantCulture) == 0)
                         ))
                {
                    var distinctFamilies = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.FamilyId)).
                  DistinctBy(d => d.FamilyId).ToList();

                    if (distinctFamilies.Any())
                    {
                        var familyBatches = distinctFamilies.Batch(m_WindowSize);
                        Parallel.ForEach(familyBatches, familyBatch =>
                        {
                            EVHttpContext.CurrentContext = userContext;
                            var famDocs = FillEntireFamilies(jobParameters.TagDetails.MatterId.ToString(),
                                jobParameters.TagDetails.CollectionId, familyBatch.ToList());

                            if (famDocs != null && famDocs.Any())
                            {
                                lock (documentList)
                                {
                                    foreach (var doc in famDocs)
                                    {
                                        if (!documentList.ContainsKey(doc.DocumentId))
                                        {
                                            documentList.Add(doc.DocumentId, doc);
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
                else
                {
                    if (jobParameters.TagDetails.TagBehaviors.Exists(
                        x =>
                        string.Compare(x.BehaviorName, Constants.TagAllFamilyBehaviorName, true,
                                       CultureInfo.InvariantCulture) == 0))
                    {
                        var familyDocuments = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.FamilyId));
                        if (familyDocuments.Any())
                        {
                            var familyBatches = familyDocuments.Batch(m_WindowSize);
                            Parallel.ForEach(familyBatches, familyBatch =>
                            {
                                EVHttpContext.CurrentContext = userContext;
                                var famDocs = GetDocumentFamilySubsetBulk(jobParameters.TagDetails.MatterId.ToString(),
                                    jobParameters.TagDetails.CollectionId, familyBatch.ToList(), false);

                                if (famDocs != null && famDocs.Any())
                                {
                                    lock (documentList)
                                    {
                                        foreach (var doc in famDocs)
                                        {
                                            if (!documentList.ContainsKey(doc.DocumentId))
                                            {
                                                documentList.Add(doc.DocumentId, doc);
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    }
                    //if tag behavior is tag all threads
                    if (jobParameters.TagDetails.TagBehaviors.Exists(
                        x =>
                        string.Compare(x.BehaviorName, Constants.TagAllThreadBehaviorName, true,
                                       CultureInfo.InvariantCulture) == 0))
                    {
                        var distinctFamilies = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.FamilyId)).
                        DistinctBy(d => d.FamilyId).ToList();
                        if (distinctFamilies.Any())
                        {
                            var familyBatches = distinctFamilies.Batch(m_WindowSize);
                            Parallel.ForEach(familyBatches, familyBatch =>
                            {
                                EVHttpContext.CurrentContext = userContext;
                                var famDocs = GetDocumentFamilySubsetBulk(jobParameters.TagDetails.MatterId.ToString(),
                                    jobParameters.TagDetails.CollectionId, familyBatch.ToList(), true);

                                if (famDocs != null && famDocs.Any())
                                {
                                    lock (documentList)
                                    {
                                        foreach (var doc in famDocs)
                                        {
                                            if (!documentList.ContainsKey(doc.DocumentId))
                                            {
                                                documentList.Add(doc.DocumentId, doc);
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    }
                }
            }
            return documentList.Select(d => d.Value).ToList();
        }

        private void GetDuplicateDocumentsInParallel(BulkTagJobBusinessEntity jobParameters, List<BulkDocumentInfoBEO> originalDocuments, Dictionary<string, BulkDocumentInfoBEO> documentList)
        {
            var distinctDuplicates = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.DuplicateId)).
               DistinctBy(d => d.DuplicateId).ToList();

            if (distinctDuplicates.Any())
            {
                var dupBatches = distinctDuplicates.Batch(m_WindowSize);
                Parallel.ForEach(dupBatches, dupBatch =>
                {
                    EVHttpContext.CurrentContext = userContext;
                    var dupDocs = GetDuplicateDocumentList(jobParameters.TagDetails.MatterId.ToString(),
                        jobParameters.TagDetails.CollectionId, dupBatch.ToList());

                    if (dupDocs != null && dupDocs.Any())
                    {
                        lock (documentList)
                        {
                            foreach (var duplicateDocument in dupDocs)
                            {
                                if (!documentList.ContainsKey(duplicateDocument.DocumentId))
                                {
                                    documentList.Add(duplicateDocument.DocumentId, duplicateDocument);
                                }
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Gets the duplicate document list.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="duplicateMaster">The duplicate master.</param>
        /// <returns>List&lt;BulkDocumentInfoBEO&gt;.</returns>
        private List<BulkDocumentInfoBEO> GetDuplicateDocumentList(string matterId, string collectionId,
            List<BulkDocumentInfoBEO> duplicateMaster)
        {
            var duplicateDocuments = DocumentBO.GetDocumentDuplicatesExpressWithFamilyId
                (matterId, collectionId, duplicateMaster);

            var duplicateDocumentsConverted = new List<BulkDocumentInfoBEO>();
            if (duplicateDocuments != null && duplicateDocuments.Any())
            {
                duplicateDocuments.SafeForEach(o => duplicateDocumentsConverted.Add(new BulkDocumentInfoBEO
                {
                    DocumentId = o.DocumentReferenceId,
                    FromOriginalQuery = true,
                    DuplicateId = o.DuplicateId,
                    FamilyId = o.Relationship,
                    DCN = o.DCN,
                    CreatedBy = createdByGuid
                }));
            }
            return duplicateDocumentsConverted;
        }

        /// <summary>
        /// Fills the entire families.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="documentList">The document list.</param>
        /// <returns>List&lt;BulkDocumentInfoBEO&gt;.</returns>
        private List<BulkDocumentInfoBEO> FillEntireFamilies(string matterId,
           string collectionId, List<BulkDocumentInfoBEO> documentList)
        {
            var familyDocuments = DocumentVaultMngr.GetEntireFamilyDocuments(matterId, collectionId,
                documentList);

            var familyDocumentsConverted = new List<BulkDocumentInfoBEO>();

            if (familyDocuments != null && familyDocuments.Count > 0)
            {
                familyDocuments.SafeForEach(o => familyDocumentsConverted.Add(new BulkDocumentInfoBEO
                {
                    DocumentId = o.DocumentReferenceId,
                    FromOriginalQuery = true,
                    DCN = o.DocTitle,
                    CreatedBy = createdByGuid
                }));
            }
            return familyDocumentsConverted;
        }

        /// <summary>
        /// Gets the document family subset bulk.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="documentList">The document list.</param>
        /// <param name="isTagAllThread">if set to <c>true</c> [is tag all thread].</param>
        /// <returns>List&lt;BulkDocumentInfoBEO&gt;.</returns>
        private List<BulkDocumentInfoBEO> GetDocumentFamilySubsetBulk(string matterId,
           string collectionId, List<BulkDocumentInfoBEO> documentList, bool isTagAllThread)
        {
            var familyDocuments = DocumentVaultMngr.GetDocumentFamilySubsetBulk(matterId, collectionId,
                documentList, !isTagAllThread);
            var familyDocumentsConverted = new List<BulkDocumentInfoBEO>();

            if (familyDocuments != null && familyDocuments.Any())
            {
                familyDocuments.SafeForEach(o => familyDocumentsConverted.Add(new BulkDocumentInfoBEO
                {
                    DocumentId = o.DependentDocumentReferenceId,
                    FromOriginalQuery = true,
                    DCN = o.DependentDCN,
                    CreatedBy = createdByGuid
                }));
            }
            return familyDocumentsConverted;
        }

        #endregion

        /// <summary>
        /// converts result document to document identity record
        /// </summary>
        /// <param name="resultDocument">ResultDocument</param>
        /// <returns>DocumentIdentityRecord</returns>
        private BulkDocumentInfoBEO ConvertToDocumentIdentityRecord(ResultDocument resultDocument)
        {
            return new BulkDocumentInfoBEO
            {
                DocumentId = resultDocument.DocumentId.DocumentId,
                FamilyId = resultDocument.DocumentId.FamilyId,
                DuplicateId = resultDocument.DocumentId.DuplicateId,
                FromOriginalQuery = true,
                DCN = GetDCN(resultDocument.FieldValues),
                CreatedBy = createdByGuid
            };
        }


        /// <summary>
        /// Gets the DCN.
        /// </summary>
        /// <param name="metaDataList">The meta data list.</param>
        /// <returns></returns>
        private static string GetDCN(List<DocumentField> metaDataList)
        {
            if (metaDataList == null || !metaDataList.Any())
                return string.Empty;
            var dcnField = metaDataList.Find(
                    x => string.Compare(x.FieldName, Constants.DcnField, true, CultureInfo.InvariantCulture) == 0);

            if (dcnField == null)
            {
                return string.Empty;
            }
            return dcnField.Value;
        }

        

        /// <summary>
        /// Sets the selected documents for reprocess tagging.
        /// </summary>
        /// <param name="jobParameters">The job parameters.</param>
        private static void SetSelectedDocumentsForReprocessTagging(BulkTagJobBusinessEntity jobParameters)
        {
            if (!string.IsNullOrEmpty(jobParameters.JobSource) &&
                jobParameters.JobSource.Equals("reprocessfilterdocuments", StringComparison.InvariantCultureIgnoreCase))
            {
                var documentVaultManager = new DocumentVaultManager();
                documentVaultManager.Init(jobParameters.DocumentListDetails.SearchContext.MatterId);
                var documentConversionLogBeos =
                    documentVaultManager.GetConversionResultsWithFilters(
                        jobParameters.DocumentListDetails.SearchContext.MatterId, jobParameters.ReprocessJobId, null,
                        null,
                        jobParameters.Filters);
                foreach (var document in documentConversionLogBeos)
                {
                    jobParameters.DocumentListDetails.SelectedDocuments.Add(document.DocumentId);
                }
            }
        }

        /// <summary>
        /// Generate Tasks for simple tagging operation
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="tasks">List of tasks</param>
        /// <param name="filteredDocuments">List of filtered documents</param>
        /// <returns>Number of tasks</returns>
        private int GenerateDocumentTagTasks(BulkTagJobBusinessEntity jobParameters,
            Tasks<BulkTagJobTaskBusinessEntity> tasks,
            IEnumerable<BulkDocumentInfoBEO> filteredDocuments)
        {
            var taskNumber = 0;
            if (filteredDocuments.ToList() != null && filteredDocuments.ToList().Count > 0)
            {
                var totalDocuments = filteredDocuments.Count();

                m_NumberOfTasks = (Int32) (Math.Ceiling((double) totalDocuments/m_WindowSize));
                var sortedDocumentList = totalDocuments > m_WindowSize
                    ? filteredDocuments.OrderBy
                        (x => x.FamilyId).ThenBy(x => x.DuplicateId).ToList()
                    : filteredDocuments.ToList();

                var taskPercent = (100.0/m_NumberOfTasks);
                for (taskNumber = 0; taskNumber < m_NumberOfTasks; taskNumber++)
                {
                    var bulkTagTaskBusinessEntity = new BulkTagJobTaskBusinessEntity
                    {
                        JobId = jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                        TaskNumber = taskNumber + 1,
                        TaskComplete = false,
                        TaskPercent = taskPercent,
                        TagDetails = jobParameters.TagDetails,
                        IsOperationTagging = jobParameters.IsOperationTagging,
                        IsTagAllDuplicates = jobParameters.IsTagAllDuplicates,
                        IsTagAllFamily = jobParameters.IsTagAllFamily,
                        OperationMode = jobParameters.OperationMode //Indicate if task is tagging or delete
                    };
                    bulkTagTaskBusinessEntity.BulkTagDocumentDetails.AddRange
                        (sortedDocumentList.GetRange(taskNumber*m_WindowSize,
                            Math.Min(totalDocuments - taskNumber*m_WindowSize, m_WindowSize)));
                    var taskKey = string.Empty;
                    bulkTagTaskBusinessEntity.BulkTagDocumentDetails.ForEach(
                        document => taskKey = string.Concat(taskKey, document.DCN, Constants.Comma)
                        );
                    taskKey = taskKey.TrimEnd(Constants.Comma.ToCharArray());
                    bulkTagTaskBusinessEntity.TaskKey = taskKey;

                    tasks.Add(bulkTagTaskBusinessEntity);
                }

                Parallel.ForEach(tasks, task =>
                                              {
                                                  AssignTag(task,
                                                            jobParameters.DocumentListDetails.SearchContext.DataSetId.ToString
                                                                (CultureInfo.InvariantCulture), m_UserDetails.UserGUID);
                                                  EvLog.WriteEntry(m_JobNameTitle.ToString() + jobParameters.JobId,
                                                                   Constants.TaskNumber + task.TaskNumber +
                                                                   Constants.JobEndMessage +
                                                                   DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                                                                   EventLogEntryType.Information);
                                              });
            }
            return taskNumber;
        }


        /// <summary>
        /// This method will return BulkTagJobBusinessEntity out of the passed bootparamter
        /// </summary>
        /// <param name="bootParamter">String</param>
        /// <returns>BulkTagJobBusinessEntity</returns>
        private BulkTagJobBusinessEntity GetBulkTaggingOperationDetails(String bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            using (var stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                var xmlStream = new XmlSerializer(typeof (BulkTagJobBusinessEntity));
                //De serialization of boot parameter to get BulkTagJobBusinessEntity
                return (BulkTagJobBusinessEntity) xmlStream.Deserialize(stream);
            }
        }

        /// <summary>
        /// Get location where tagging is being done
        /// </summary>
        /// <param name="searchContext">Search context</param>
        /// <returns>Location where tagging is being done</returns>
        private string GetTaggingLocation(RVWSearchBEO searchContext)
        {
            var location = string.Empty;
            if (!string.IsNullOrEmpty(searchContext.ReviewSetId))
            {
                var reviewSetDetails =
                    ReviewSetService.GetReviewSetDetails(searchContext.MatterId.ToString(CultureInfo.InvariantCulture),
                        searchContext.ReviewSetId);
                if (reviewSetDetails != null)
                {
                    location = reviewSetDetails.ReviewSetName;
                }
            }
            else
            {
                var dataSetDetails =
                    DataSetService.GetDataSet(searchContext.DataSetId.ToString(CultureInfo.InvariantCulture));
                if (dataSetDetails != null)
                {
                    location = dataSetDetails.FolderName;
                }
            }
            return location;
        }

        /// <summary>
        /// Perform auditing and notification when no tasks could be generated for the job
        /// </summary>
        /// <param name="userGuid">User GUID</param>
        /// <param name="jobId">Current job id</param>
        /// <param name="jobRunId">Current job run id</param>
        /// <param name="searchContext">Search context</param>
        /// <param name="isTagAllFamily">true if tag all family operation, false otherwise</param>
        /// <param name="isTagAllDuplicates">true if tag all duplicates operation, false otherwise</param>
        /// <param name="isOperationTagging">true if tagging operation, false otherwise</param>
        /// <param name="tagName">Tag Name</param>
        private void AuditAndNotifyForNoTasksFound(string userGuid, string jobId, string jobRunId,
            RVWSearchBEO searchContext, bool isTagAllFamily,
            bool isTagAllDuplicates, bool isOperationTagging, string tagName)
        {
            searchContext.ShouldNotBe(null);
            jobId.ShouldNotBe(null);
            jobId.ShouldNotBeEmpty();
            jobRunId.ShouldNotBe(null);
            jobRunId.ShouldNotBeEmpty();

            //Send notification message
            var jobDetail = JobService.GetJobDetails(jobId);
            jobDetail.CreatedById = m_UserDetails.UserGUID;

            if (jobDetail != null && jobDetail.NotificationId > 0)
            {
                var location = GetTaggingLocation(searchContext);
                //Build notification message
                var notificationMessage = string.Empty;
                notificationMessage = isTagAllDuplicates
                    ? BuildNotificationMessage(tagName, true, location, false, true)
                                                           : BuildNotificationMessage(tagName, isOperationTagging, location, false, true);

                //Send notification only if all the mandatory values for notification are populated
                EvLog.WriteEntry(jobDetail.Name + Constants.SpaceHiphenSpace + jobDetail.Id, notificationMessage,
                    EventLogEntryType.Information);
                if (!string.IsNullOrEmpty(notificationMessage))
                {
                    CustomNotificationMessage = notificationMessage;
                    m_IsAuditAndNotificationDone = true;
                }
            }
        }

        /// <summary>
        /// Bulk Tag the list of documents
        /// </summary>
        /// <param name="taskDetails">Details that are required to perform the task</param>
        /// <param name="datasetId"> </param>
        /// <param name="createdBy">User who has chosen to tag the document</param>
        private void AssignTag(BulkTagJobTaskBusinessEntity taskDetails, string datasetId, string createdBy)
        {
            var isIncludeDuplicates = taskDetails.IsTagAllDuplicates.ToString(CultureInfo.InvariantCulture) +
                                      Constants.Colon + Constants.One;
            var isIncludeFamilies = taskDetails.IsTagAllFamily.ToString(CultureInfo.InvariantCulture) + Constants.Colon +
                                    Constants.One;

            var documentList = taskDetails.BulkTagDocumentDetails;

            EVHttpContext.CurrentContext = userContext;

            var originator = Guid.NewGuid().ToString();
            var currentRunNotification = BulkTagBO.DoBulkOperation(taskDetails.TagDetails.Id,
                taskDetails.TagDetails.Name,
                documentList, byte.Parse((taskDetails.IsOperationTagging) ? "1" : "3"),
                taskDetails.TagDetails.MatterId.ToString(CultureInfo.InvariantCulture),
                taskDetails.TagDetails.CollectionId,
                datasetId, isIncludeDuplicates, isIncludeFamilies, _mBinderId, originator);


            var taglog = new TagLogBEO
                                   {
                                       DatasetId = Convert.ToInt32(datasetId),
                                       JobId = m_JobId,
                                       AlreadyTag =
                                           Convert.ToInt32(currentRunNotification.DocumentsAlreadyTagged.Count()),
                                       FailedTag = Convert.ToInt32(currentRunNotification.DocumentsFailed.Count()),
                                       DocumentTag = Convert.ToInt32(currentRunNotification.DocumentsTagged.Count())
                                   };
            // Wait for the # of minutes and keep polling and get the confirmation of index. Move ahead if exceeds the quota time
            //TODO:Search Engine Replacement - Sub System - How to make sure tag is updated especially at search sub-system??

            RVWTagBO.UpdateTagLog(taglog);
        }

        /// <summary>
        /// cancel currently running bulk task
        /// </summary>
        /// <param name="jobId">Id of Job to cancel</param>
        /// <returns>Success status of operation</returns>
        public void CancelBulkTask(string jobId)
        {
            var bulkJobBusinessEntity = JobService.GetJobDetails(jobId);
            bulkJobBusinessEntity.Status = Constants.JobStatusCancelled; //Constants.CancelJob; //Cancelled
            //Set job status to cancel
            JobService.UpdateJob(bulkJobBusinessEntity, Constants.EvolutionJobUpdateStatus);
            m_IsTagNotExists = true;
        }

        #endregion

        #region All Notifications

        /// <summary>
        /// Create notification message for each tag that was tagged
        /// </summary>
        /// <param name="jobParameters">Input parameters of job</param>
        /// <param name="jobDetail">Job related details</param>
        /// <param name="location">Location where tagging was done</param>
        private void CreateNotificationMessageForTag(BulkTagJobBusinessEntity jobParameters, JobBusinessEntity jobDetail,
            string location)
        {
            var notificationMessage =
                BuildNotificationMessage(jobParameters.TagDetails.Id.ToString(CultureInfo.InvariantCulture),
                    (jobParameters.IsTagAllDuplicates && jobParameters.IsTagAllFamily
                        ? true
                        : jobParameters.IsTagAllDuplicates
                            ? true
                            : jobParameters.IsTagAllFamily
                                ? jobParameters.IsTagAllFamily && jobParameters.IsOperationTagging
                                : jobParameters.IsOperationTagging), location,
                (jobDetail.Status == 8 && (!m_IsTagNotExists)), false);
            CustomNotificationMessage = notificationMessage;

            //Build Notification value to write to event log
            var notificationValues = new StringBuilder();
            notificationValues.Append(jobDetail.NotificationId);
            notificationValues.Append(Constants.SpaceHiphenSpace);
            notificationValues.Append(jobDetail.CreatedById);
            notificationValues.Append(Constants.SpaceHiphenSpace);
            notificationValues.Append(jobDetail.Name).Append(Constants.Instance).Append(jobParameters.JobRunId);
            notificationValues.Append(Constants.SpaceHiphenSpace);
            notificationValues.Append(notificationMessage);

            //Send notification only if all the mandatory values for notification are populated
            var notificationLogEntry = new StringBuilder();
            notificationLogEntry.Append(jobDetail.Name);
            notificationLogEntry.Append(Constants.SpaceHiphenSpace);
            notificationLogEntry.Append(jobDetail.Id);
            EvLog.WriteEntry(notificationLogEntry.ToString(), notificationValues.ToString(),
                EventLogEntryType.Information);
        }

        /// <summary>
        /// Build notification message to be sent on job success
        /// </summary>
        /// <param name="tagId"> </param>
        /// <param name="isOperationTagging">True if tagging, false if untagging</param>
        /// <param name="location"> </param>
        /// <param name="isJobCancelled"> </param>
        /// <param name="isNoDocumentsFound"> </param>
        /// <returns>Notification message</returns>
        private string BuildNotificationMessage(string tagId, bool isOperationTagging, string location,
            bool isJobCancelled, bool isNoDocumentsFound)
        {
            //Add notification message to be sent
            var notificationMessage = new StringBuilder();
            notificationMessage.Append(Constants.Table);
            if (!isNoDocumentsFound)
            {
                var isTagDeleted =
                    m_DeletedTagIdsList.Find(
                        x => System.String.Compare(x, tagId, System.StringComparison.OrdinalIgnoreCase) == 0) != null;
                if (isOperationTagging)
                {
                    if (isJobCancelled)
                    {
                        GenerateJobCancelledNotificationHeading(notificationMessage, isOperationTagging);
                    }
                    else
                    {
                        notificationMessage.Append(Constants.Row);
                        notificationMessage.Append(Constants.Header);
                        notificationMessage.Append(
                            HttpUtility.HtmlEncode(isTagDeleted
                                ? Constants.NotificationMessageHeadingForTaggingSuspend
                                : Constants.NotificationMessageHeadingForTagging));
                        notificationMessage.Append(Constants.CloseHeader);
                        notificationMessage.Append(Constants.CloseRow);
                        notificationMessage.Append(Constants.Row);
                        notificationMessage.Append(Constants.Column);
                        notificationMessage.Append(
                            HttpUtility.HtmlEncode(isTagDeleted
                                ? Constants.NotificationMessageForTaggingSuspend +
                                  Constants.NotificationMessageForTagDeleted
                                : Constants.NotificationMessageForTagging));
                        notificationMessage.Append(Constants.CloseColumn);
                        notificationMessage.Append(Constants.CloseRow);
                    }
                    GenerateGeneralNotificationDetails(location, notificationMessage, tagId, isNoDocumentsFound);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(Constants.NotificationMessageForTaggingDocumentsTagged));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_DocumentsTaggedCount.ToString(CultureInfo.InvariantCulture)));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(Constants.NotificationMessageForTaggingDocumentsAlreadyTagged));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_DocumentsAlreadyTaggedCount.ToString(CultureInfo.InvariantCulture)));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(Constants.NotificationMessageForTaggingDocumentsFailed));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(m_IsAnyDocsLocked
                        ? (m_Notification.DocumentsFailed.Count + m_DocumentsFailedcount).ToString(
                            CultureInfo.InvariantCulture)
                        : m_DocumentsFailedcount.ToString(CultureInfo.InvariantCulture)));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
                else
                {
                    if (isJobCancelled)
                    {
                        GenerateJobCancelledNotificationHeading(notificationMessage, isOperationTagging);
                    }
                    else
                    {
                        notificationMessage.Append(Constants.Row);
                        notificationMessage.Append(Constants.Header);
                        notificationMessage.Append(
                            HttpUtility.HtmlEncode(isTagDeleted
                                ? Constants.NotificationMessageHeadingForUntaggingSuspend
                                : Constants.NotificationMessageHeadingForUntagging));
                        notificationMessage.Append(Constants.CloseHeader);
                        notificationMessage.Append(Constants.CloseRow);
                        notificationMessage.Append(Constants.Row);
                        notificationMessage.Append(Constants.Column);
                        notificationMessage.Append(
                            HttpUtility.HtmlEncode(isTagDeleted
                                ? Constants.NotificationMessageForUntaggingSuspend +
                                  Constants.NotificationMessageForTagDeleted
                                : Constants.NotificationMessageForUntagging));
                        notificationMessage.Append(Constants.CloseColumn);
                        notificationMessage.Append(Constants.CloseRow);
                    }
                    GenerateGeneralNotificationDetails(location, notificationMessage, tagId, isNoDocumentsFound);

                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(Constants.NotificationMessageForUntaggingDocumentsUnTagged));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_DocumentsTaggedCount.ToString(CultureInfo.InvariantCulture)));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(Constants.NotificationMessageForUntaggingDocumentsNotUntagged));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(
                        m_IsAnyDocsLocked
                            ? (m_Notification.DocumentsFailed.Count + m_DocumentsFailedcount +
                               m_DocumentsAlreadyTaggedCount).ToString(CultureInfo.InvariantCulture)
                            : (m_DocumentsFailedcount + m_DocumentsAlreadyTaggedCount).ToString(
                                CultureInfo.InvariantCulture)));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
            }
            else
            {
                if (isOperationTagging)
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Header);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_IsTagNotExists
                            ? Constants.NotificationMessageHeadingForTaggingSuspend
                            : Constants.NotificationMessageHeadingForTagging));
                    notificationMessage.Append(Constants.CloseHeader);
                    notificationMessage.Append(Constants.CloseRow);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_IsTagNotExists
                            ? Constants.NotificationMessageForTaggingSuspend +
                              Constants.NotificationMessageForTagDeletedBeforeTaskStart
                            : Constants.NotificationMessageForTagging));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
                else
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Header);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_IsTagNotExists
                            ? Constants.NotificationMessageHeadingForUntaggingSuspend
                            : Constants.NotificationMessageHeadingForUntagging));
                    notificationMessage.Append(Constants.CloseHeader);
                    notificationMessage.Append(Constants.CloseRow);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(m_IsTagNotExists
                            ? Constants.NotificationMessageForUntaggingSuspend +
                              Constants.NotificationMessageForTagDeletedBeforeTaskStart
                            : Constants.NotificationMessageForUntagging));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
                GenerateGeneralNotificationDetails(location, notificationMessage, tagId, isNoDocumentsFound);
                if (!m_IsTagNotExists)
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageForNoDocumentsToTag));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
            }

            notificationMessage.Append(Constants.CloseTable);
            return notificationMessage.ToString();
        }

        /// <summary>
        /// Generate part of notification message for bulk tagging cancelled event
        /// </summary>
        /// <param name="notificationMessage">Notification message container</param>
        /// <param name="isOperationTagging"> </param>
        private static void GenerateJobCancelledNotificationHeading(StringBuilder notificationMessage,
            bool isOperationTagging)
        {
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Header);
            notificationMessage.Append(
                HttpUtility.HtmlEncode(isOperationTagging
                    ? Constants.NotificationMessageHeadingForJobCancel
                    : Constants.NotificationMessageHeadingForUntagJobCancel));
            notificationMessage.Append(Constants.CloseHeader);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(
                HttpUtility.HtmlEncode(isOperationTagging
                    ? Constants.NotificationMessageForTaggingJobCancel
                    : Constants.NotificationMessageForUntaggingJobCancel));
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
        }

        /// <summary>
        /// Generate part of notification message common to all events during bulk tagging
        /// </summary>
        /// <param name="location">Location of bulk tagging</param>
        /// <param name="notificationMessage">Notification message container</param>
        /// <param name="tagId">Tag Id</param>
        /// <param name="isNoDocumentsFound">true if no tasks were generated, false otherwise</param>
        private void GenerateGeneralNotificationDetails(string location, StringBuilder notificationMessage, string tagId,
            bool isNoDocumentsFound)
        {
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.HtmlBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageStartTime));
            notificationMessage.Append(Constants.HtmlCloseBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(DateTime.Parse(m_JobStartDate).ConvertToUserTime()));
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.HtmlBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageEndTime));
            notificationMessage.Append(Constants.HtmlCloseBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(DateTime.Parse(m_JobEndDate).ConvertToUserTime()));
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.HtmlBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageLocation));
            notificationMessage.Append(Constants.HtmlCloseBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(location));
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);

            if (!isNoDocumentsFound)
            {
                if (m_TagDetails != null)
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageTagName));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(m_TagDetails.Name));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
                else
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageTagName));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(string.Empty));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
            }
            else
            {
                notificationMessage.Append(Constants.Row);
                notificationMessage.Append(Constants.Column);
                notificationMessage.Append(Constants.HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageTagName));
                notificationMessage.Append(Constants.HtmlCloseBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(tagId));
                notificationMessage.Append(Constants.CloseColumn);
                notificationMessage.Append(Constants.CloseRow);
            }
        }

        #endregion
    }
}
