# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="BulkTagDelete.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kokila Bai S L</author>
//      <description>
//          Actual backend Backend process which does deletion and untagging of tag to be
//          deleted
//      </description>
//      <changelog>
//          <date value="18/04/2011">File added</date>
//          <date value="01/09/2012">Fix for Bug# 85913.</date>
//          <date value="05/09/2012">Bug fix 100297</date>
//          <date value="04/June/2012">Task fix 101466 - Cr022</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/07/2012">Fixed the merge issue that changed tag display name back to tag name</date>
//          <date value="05/10/2013">BugFix 130823 - Tag delete performance issue fix</date>
//          <date value="09/25/2012">BugFix 142375,143043, 143590</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
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
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Business.NotificationManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Search;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.ServiceContracts;
using LexisNexis.Evolution.ServiceImplementation;
using Moq;

namespace LexisNexis.Evolution.BatchJobs.BulkTagDelete
{
    /// <summary>
    /// This class represents Bulk Tag Delete job for
    /// deleting and untagging documents with that tag
    /// </summary>
    [Serializable]
    public class BulkTagDelete : BaseJob<BulkTagJobBusinessEntity, BulkTagJobTaskBusinessEntity>
    {
        #region Variables

        private string _jobStartDate;
        private string _jobEndDate;
        private int _documentsUnTaggedCount; //Number of documents that have been untagged for this tag
        private int _numberOfTasks;
        private int _totalDocumentsFailedToUnTaggedCount; //Total Number of documents to be untagged
        private string _matterId;
        private StringBuilder _jobNameTitle;
        private RVWTagBEO _selectedTagDetails;
        private bool _isTagNotExists;
        private List<string> _tagDeleteStatusList; //tagId:statusOfTagDelete
        private int _totalTagDeleteTasks; //Task number up to which tag delete is to be done
        private string _createdBy;
        private const int MWindowSize = 1000; //determine number of documents that will be processed in a single task
        private const string Comma = ",";
        [NonSerialized] private HttpContextBase _userContext;
        private const string One = "1";

        #endregion Variables

        #region Initialize

        /// <summary>
        /// This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy">string</param>
        /// <returns>BulkTaskJobTaskBusinessEntity</returns>
        protected override BulkTagJobBusinessEntity Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            #region Initialize Job related variables

            _jobStartDate = string.Empty;
            _jobEndDate = string.Empty;
            _documentsUnTaggedCount = 0;
            _numberOfTasks = 0;
            _totalDocumentsFailedToUnTaggedCount = 0;
            _matterId = string.Empty;
            _isTagNotExists = false;
            _tagDeleteStatusList = new List<string>();
            _totalTagDeleteTasks = 1;
            _createdBy = createdBy;

            #endregion Initialize Job related variables

            BulkTagJobBusinessEntity jobBeo = null;
            try
            {
                //Initialize job start time
                _jobStartDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                // Initialize the JobBEO
                jobBeo = new BulkTagJobBusinessEntity {JobId = jobId, JobRunId = jobRunId};

                //filling properties of the job parameter
                var userBusinessEntity = UserBO.GetUserUsingGuid(createdBy); //NO SERVICE METHOD AVAILABLE
                jobBeo.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals(Constants.NotApplicable))
                    ? userBusinessEntity.UserId
                    : userBusinessEntity.DomainName + Constants.BackSlash + userBusinessEntity.UserId;
                jobBeo.JobTypeName = Constants.JobTypeName;
                jobBeo.BootParameters = bootParameters;
                //constructing BulkTagJobBusinessEntity from boot parameter by de serializing
                var bulkTaggingOperationDetails = GetBulkTagDeleteOperationDetails(bootParameters);
                jobBeo.JobName = Constants.JobName;
                jobBeo.JobTypeName = Constants.JobName;
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

                Tracer.Info("{0}{1}:{2}", jobId, Constants.AuditBootParameterKey, Constants.AuditBootParameterValue);

                if (bulkTaggingOperationDetails != null)
                {
                    jobBeo.TagDetails = bulkTaggingOperationDetails.TagDetails;
                    jobBeo.DocumentListDetails = bulkTaggingOperationDetails.DocumentListDetails;
                }
                else
                {
                    Tracer.Error("{0}{1}:{2}", jobId, Constants.JobInitializationKey, Constants.EventXmlNotWellFormed);
                    throw new EVException().AddResMsg(ErrorCodes.ImpXmlFormatErrorId);
                }
            }
            catch (Exception exp)
            {
                Tracer.Error("{0}{1}:{2}", jobId, Constants.EventInitializationExceptionValue, exp.Message);
                LogException(jobId, exp, Constants.EventInitializationExceptionValue, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInJobInitialization);
            }
            return jobBeo;
        }

        #endregion Initialize

        /// <summary>
        /// This is the overriden GenerateTasks() method.
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount">int</param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<BulkTagJobTaskBusinessEntity> GenerateTasks(BulkTagJobBusinessEntity jobParameters,
            out int previouslyCommittedTaskCount)
        {
            previouslyCommittedTaskCount = 0;
            try
            {
                EvLog.WriteEntry(Constants.JobName + Constants.SpaceHiphenSpace + jobParameters.JobId,
                    Constants.AuditGenerateTaskValue,
                    EventLogEntryType.Information);
                var tasks = new Tasks<BulkTagJobTaskBusinessEntity>();
                var tagDetails = GetTagDetails(new BulkTagJobTaskBusinessEntity
                {
                    TagDetails = new RVWTagBEO {Id = jobParameters.TagDetails.Id},
                    DocumentDetails = new FilteredDocumentBusinessEntity
                    {
                        MatterId = jobParameters.TagDetails.MatterId.ToString(CultureInfo.InvariantCulture),
                        CollectionId = jobParameters.TagDetails.CollectionId
                    }
                });
                //Fetch the user information
                var objUser = UserService.GetUser(StringUtility.EncodeTo64(jobParameters.JobScheduleCreatedBy));
                if (tagDetails == null)
                {
                    BulkTaskCancelFromGenerateTasks(jobParameters,
                        string.IsNullOrEmpty(jobParameters.TagDetails.Name)
                            ? Constants.AuditTagNotFound
                            : jobParameters.TagDetails.Name);
                }
                else
                {
                    if (!tagDetails.Status)
                    {
                        BulkTaskCancelFromGenerateTasks(jobParameters, tagDetails.Name);
                    }
                }

                if (!_isTagNotExists)
                {
                    if (tagDetails != null)
                        _tagDeleteStatusList.Add(string.Format("{0}:{1}", tagDetails.Id, false));
                            //add tag delete status for actual tag selected by the user

                    #region Get other tags in the dataset

                    //Get related tags
                    if (tagDetails != null)
                    {
                        var otherTags = RVWTagService.GetAllTags(tagDetails.CollectionId,
                            tagDetails.MatterId.ToString(CultureInfo.InvariantCulture),
                            tagDetails.IsPrivateTag.ToString(CultureInfo.InvariantCulture).ToLower());

                        #endregion Get other tags in the dataset

                        var associatedTags = new List<RVWTagBEO>();
                        //Check if there are child tags for this tag
                        if (!(tagDetails.Type == TagType.Tag || tagDetails.Type == TagType.None))
                        {
                            #region Get other tags in the dataset which could be probable children

                            GetChildTags(tagDetails, otherTags, associatedTags);

                            #endregion Get other tags in the dataset which could be probable children
                        }
                        //Check if there are associated tags to be deleted

                        #region Get other associated tags in the dataset to be deleted

                        GetAssociatedTags(tagDetails, otherTags, associatedTags);

                        #endregion Get other associated tags in the dataset to be deleted

                        #region Get filtered list of documents

                        var documentsToBeUntagged = new List<RVWDocumentTagBEO>();
                        if (tagDetails.Type != TagType.TagFolder)
                        {
                            documentsToBeUntagged = GetDocumentsWithThisTag(jobParameters, jobParameters.TagDetails);
                        }
                        if (associatedTags.Count > 0)
                        {
                            associatedTags.ForEach(x => _tagDeleteStatusList.Add(string.Format("{0}:{1}", x.Id, false)));
                            _totalTagDeleteTasks += associatedTags.Count;
                            foreach (var associatedTag in associatedTags)
                            {
                                if (documentsToBeUntagged == null)
                                {
                                    documentsToBeUntagged = new List<RVWDocumentTagBEO>();
                                }
                                if (associatedTag.Type != TagType.TagFolder)
                                {
                                    documentsToBeUntagged.AddRange(GetDocumentsWithThisTag(jobParameters, associatedTag));
                                }
                            }
                        }

                        #endregion Get filtered list of documents

                        CreateTasks(jobParameters, tasks, associatedTags, documentsToBeUntagged);
                    }

                    //Send notification if no tasks found
                    if (_numberOfTasks == 0 && !_isTagNotExists)
                    {
                        AuditAndNotifyForNoTasksFound(jobParameters.JobScheduleCreatedBy,
                            jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                            jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture),
                            (GetBulkTagDeleteOperationDetails(jobParameters.BootParameters)).DocumentListDetails
                                .SearchContext, tagDetails.Name);
                    }
                    else
                    {
                        _selectedTagDetails =
                            RVWTagService.GetTag(jobParameters.TagDetails.Id.ToString(CultureInfo.InvariantCulture),
                                jobParameters.TagDetails.CollectionId,
                                jobParameters.TagDetails.MatterId.ToString(CultureInfo.InvariantCulture));
                    }
                }
                return tasks;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(_jobNameTitle.ToString() + jobParameters.JobId + Constants.AuditGenerateTaskValue,
                    exp.Message, EventLogEntryType.Error);
                LogException(jobParameters.JobId, exp, Constants.EventInitializationExceptionValue, LogCategory.Job,
                    string.Empty, ErrorCodes.ProblemInJobInitialization);
                return null;
            }
        }

        /// <summary>
        /// This is the overriden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <returns>Status of the operation.</returns>
        protected override bool DoAtomicWork(BulkTagJobTaskBusinessEntity task, BulkTagJobBusinessEntity jobParameters)
        {
            try
            {
                if (task.TaskNumber <= _totalTagDeleteTasks) //Delete the tag
                {
                    DeleteTag(task);
                }
                EvLog.WriteEntry(_jobNameTitle.ToString() + jobParameters.JobId,
                    Constants.TaskNumber + task.TaskNumber + Constants.JobEndMessage +
                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                    EventLogEntryType.Information);
                return true;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName + Constants.AuditInDoAtomicWorkValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(jobParameters.JobId, exp, Constants.AuditInDoAtomicWorkValue, LogCategory.Task,
                    "Collection Id: " + task.DocumentDetails.CollectionId, ErrorCodes.ProblemInDoAtomicWork);
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
                //Get job end date
                _jobEndDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                //Fetch the user information
                var objUser = UserService.GetUser(StringUtility.EncodeTo64(jobParameters.JobScheduleCreatedBy));

                //Fetch the Job details
                var jobDetail = JobService.GetJobDetails(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));
                jobDetail.CreatedById = objUser.UserGUID;

                if (jobDetail.NotificationId <= 0 || _isTagNotExists) return;
                var location = GetTaggingLocation(jobParameters.DocumentListDetails.SearchContext);
                SendNotificationToUser(jobParameters, jobDetail, location);
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName + Constants.AuditInShutdownValue, exp.Message,
                    EventLogEntryType.Error);
                LogException(jobParameters.JobId, exp, Constants.AuditInShutdownValue, LogCategory.Task, string.Empty,
                    ErrorCodes.ProblemInJobExecution);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Create tasks for the job
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="tasks">List of tasks</param>
        /// <param name="associatedTags">List<RVWTagBEO/></param>
        /// <param name="documentsToBeUntagged">List of documents to be untagged</param>
        private void CreateTasks(BulkTagJobBusinessEntity jobParameters, Tasks<BulkTagJobTaskBusinessEntity> tasks,
            List<RVWTagBEO> associatedTags, List<RVWDocumentTagBEO> documentsToBeUntagged)
        {
            var taskNumber = 0;

            _numberOfTasks = associatedTags != null ? associatedTags.Count : 0;

            _numberOfTasks = _numberOfTasks + 1; //to include the tag which suppose to be deleted

            #region Create Task for each document to be untagged

            if (documentsToBeUntagged != null && documentsToBeUntagged.Any())
            {
                _matterId = documentsToBeUntagged[0].MatterId.ToString(CultureInfo.InvariantCulture);
                GenerateDocumentTagTasks(jobParameters, documentsToBeUntagged, _numberOfTasks);
            }

            #endregion Create Task for each document to be untagged

            #region Create Task to Delete the tag

            var taskPercent = (100.0/_numberOfTasks);
            taskNumber++;
            var bulkTagDeleteTaskBusinessEntity = new BulkTagJobTaskBusinessEntity
            {
                JobId = jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                TaskNumber = taskNumber,
                TaskComplete = false,
                TaskPercent = taskPercent,
                TagDetails = jobParameters.TagDetails,
                TaskKey = jobParameters.TagDetails.Name
            };
            tasks.Add(bulkTagDeleteTaskBusinessEntity);

            if (associatedTags != null)
            {
                associatedTags.ForEach(associatedTag =>
                {
                    taskNumber++;
                    var associatedBulkTagDeleteTaskBusinessEntity = new BulkTagJobTaskBusinessEntity
                    {
                        JobId = jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                        TaskNumber = taskNumber,
                        TaskComplete = false,
                        TaskPercent = taskPercent,
                        TagDetails = associatedTag,
                        TaskKey = associatedTag.Name
                    };
                    tasks.Add(associatedBulkTagDeleteTaskBusinessEntity);
                });
            }

            #endregion Create Task to Delete the tag
        }


        /// <summary>
        /// Generate Tasks for simple tagging operation
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="filteredDocuments">List of filtered documents</param>
        /// <param name="tagTotalTask"></param>
        /// <returns>Number of tasks</returns>
        private void GenerateDocumentTagTasks(BulkTagJobBusinessEntity jobParameters,
            IEnumerable<RVWDocumentTagBEO> filteredDocuments, int tagTotalTask)
        {
            var tasks = new Tasks<BulkTagJobTaskBusinessEntity>();
            //Audit the bulk tagging started event information
            var filteredDocumentBusinessEntities = filteredDocuments as IList<RVWDocumentTagBEO> ??
                                                   filteredDocuments.ToList();
            if (filteredDocuments == null || !filteredDocumentBusinessEntities.Any()) return;
            var totalDocuments = filteredDocumentBusinessEntities.Count();
            var docTotalTask = (Int32) (Math.Ceiling((double) totalDocuments/MWindowSize));
            var sortedDocumentList = totalDocuments > MWindowSize
                ? filteredDocumentBusinessEntities.OrderBy(x => x.FamilyId).ThenBy(x => x.DuplicateId).ToList()
                : filteredDocumentBusinessEntities.ToList();
            _numberOfTasks = docTotalTask + tagTotalTask;
            var taskPercent = (100.0/_numberOfTasks);
            for (var taskNum = 0; taskNum < docTotalTask; taskNum++)
            {
                var taskNumber = taskNum + 1;
                var bulkTagTaskBusinessEntity = new BulkTagJobTaskBusinessEntity
                {
                    JobId = jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                    TaskNumber = taskNumber,
                    TaskComplete = false,
                    TaskPercent = taskPercent,
                    IsOperationTagging = jobParameters.IsOperationTagging,
                    IsTagAllDuplicates = false,
                    IsTagAllFamily = false,
                    OperationMode = jobParameters.OperationMode //Indicate if task is tagging or delete
                };

                bulkTagTaskBusinessEntity.BulkTagDeleteDocumentDetails.AddRange(
                    sortedDocumentList.GetRange(taskNum*MWindowSize,
                        Math.Min(totalDocuments - taskNum*MWindowSize, MWindowSize)));

                var taskKey = string.Empty;
                bulkTagTaskBusinessEntity.BulkTagDeleteDocumentDetails.ForEach(
                    document => taskKey = string.Concat(taskKey, document.DCN, Comma)
                    );
                taskKey = taskKey.TrimEnd(Comma.ToCharArray());
                bulkTagTaskBusinessEntity.TaskKey = taskKey;
                tasks.Add(bulkTagTaskBusinessEntity);
            }

            Parallel.ForEach(tasks, task => AssignTag(task,
                jobParameters.DocumentListDetails.SearchContext.DataSetId.ToString
                    (CultureInfo.InvariantCulture), _createdBy));
        }


        /// <summary>
        /// Bulk Tag the list of documents
        /// </summary>
        /// <param name="taskDetails">Details that are required to perform the task</param>
        /// <param name="datasetId"> </param>
        /// <param name="createdBy">User who has chosen to tag the document</param>
        private void AssignTag(BulkTagJobTaskBusinessEntity taskDetails, string datasetId, string createdBy)
        {
            if (taskDetails.BulkTagDeleteDocumentDetails == null || !taskDetails.BulkTagDeleteDocumentDetails.Any())
                return;
            var isIncludeDuplicates = taskDetails.IsTagAllDuplicates.ToString(CultureInfo.InvariantCulture) +
                                      Constants.Colon + One;
            var isIncludeFamilies = taskDetails.IsTagAllFamily.ToString(CultureInfo.InvariantCulture) + Constants.Colon +
                                    One;
            foreach (var tagId in taskDetails.BulkTagDeleteDocumentDetails.Select(f => f.TagId).Distinct())
            {
                var documentList = taskDetails.BulkTagDeleteDocumentDetails.Where(f => f.TagId == tagId).Select(
                    document =>
                        new BulkDocumentInfoBEO
                        {
                            DocumentId = document.DocumentId,
                            DCN = document.DCN,
                            FromOriginalQuery = true,
                            DuplicateId = document.DuplicateId,
                            FamilyId = document.FamilyId,
                            CreatedBy = createdBy
                        }).ToList();

                _userContext = CreateUserContext();
                EVHttpContext.CurrentContext = _userContext;

                var notification =
                    DocumentService.DoBulkTagging(
                        taskDetails.BulkTagDeleteDocumentDetails[0].MatterId.ToString(CultureInfo.InvariantCulture),
                        taskDetails.BulkTagDeleteDocumentDetails[0].CollectionId, documentList,
                        tagId.ToString(CultureInfo.InvariantCulture),
                        string.Empty, (taskDetails.IsOperationTagging) ? "1" : "3",
                        datasetId, isIncludeDuplicates, isIncludeFamilies);

                _totalDocumentsFailedToUnTaggedCount = _totalDocumentsFailedToUnTaggedCount +
                                                       notification.DocumentsFailed.Count();
                _documentsUnTaggedCount = _documentsUnTaggedCount + notification.DocumentsTagged.Count();
            }
        }

        private UserBusinessEntity _mUserDetails;

        private HttpContextBase CreateUserContext()
        {
            _mUserDetails = UserBO.GetUserUsingGuid(_createdBy);
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userProp = _mUserDetails;
            var userSession = new UserSessionBEO();
            SetUserSession(_createdBy, userProp, userSession);
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


        /// <summary>
        /// Get list of children for given tag
        /// </summary>
        /// <param name="tagDetails">Tag related details</param>
        /// <param name="otherTags">Other tags in the dataset</param>
        /// <param name="finalChildTagsList">List of child tags</param>
        private void GetChildTags(RVWTagBEO tagDetails, List<RVWTagBEO> otherTags, List<RVWTagBEO> finalChildTagsList)
        {
            #region Get child tags and create tasks

            if (otherTags != null && otherTags.Count > 0)
            {
                var childTags = otherTags.FindAll(tag => tag.ParentTagId == tagDetails.Id);
                if (childTags.Count > 0)
                {
                    finalChildTagsList.AddRange(childTags);
                    childTags.ForEach
                        (
                            childTag => GetChildTags(childTag, otherTags, finalChildTagsList)
                        );
                }
            }

            #endregion Get child tags and create tasks
        }

        /// <summary>
        /// Get list of associated tags for given tag
        /// </summary>
        /// <param name="tagDetails">Tag related details</param>
        /// <param name="otherTags">Other tags in the dataset</param>
        /// <param name="finalAssociatedTagsList">List of associated tags</param>
        private void GetAssociatedTags(RVWTagBEO tagDetails, List<RVWTagBEO> otherTags,
            List<RVWTagBEO> finalAssociatedTagsList)
        {
            #region Get associated tags and create tasks

            if (otherTags != null && otherTags.Count > 0)
            {
                RVWTagBEO associatedTag = null;
                if (tagDetails.IsSystemTag)
                {
                    if (tagDetails.SearchToken.ToLower().Equals(Constants.TagFamilyReviewed.ToLower()))
                    {
                        associatedTag =
                            otherTags.Find(
                                tag =>
                                    tag.IsSystemTag &&
                                    tag.SearchToken.ToLower().Equals(Constants.TagFamilyNotReviewed.ToLower()));
                    }
                    else if (tagDetails.SearchToken.ToLower().Equals(Constants.TagFamilyNotReviewed.ToLower()))
                    {
                        associatedTag =
                            otherTags.Find(
                                tag =>
                                    tag.IsSystemTag &&
                                    tag.SearchToken.ToLower().Equals(Constants.TagFamilyReviewed.ToLower()));
                    }
                }
                if (associatedTag != null)
                {
                    if (finalAssociatedTagsList == null)
                    {
                        finalAssociatedTagsList = new List<RVWTagBEO>();
                    }
                    finalAssociatedTagsList.Add(associatedTag);
                }
            }

            #endregion Get associated tags and create tasks
        }

        /// <summary>
        /// End bulk tag delete job if tag is deleted before the job starts
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="tagDetailToAudit">Tag Detail to be sent for auditing</param>
        private void BulkTaskCancelFromGenerateTasks(BulkTagJobBusinessEntity jobParameters, string tagDetailToAudit)
        {
            _isTagNotExists = true;
            //Get job end date
            _jobEndDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            //Cancel bulk tagging / untagging
            CancelBulkTask(jobParameters.JobId.ToString(CultureInfo.InvariantCulture));
            //Audit and send notification
            AuditAndNotifyForNoTasksFound(jobParameters.JobScheduleCreatedBy,
                jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture),
                (GetBulkTagDeleteOperationDetails(jobParameters.BootParameters)).DocumentListDetails.SearchContext,
                tagDetailToAudit);
        }

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="exp">exception received</param>
        /// <param name="msg">message to be logged</param>
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key to identify the Task, need for task log only</param>
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
            else
            {
                TaskLogInfo.AddParameters(jobId + msg);
                TaskLogInfo.TaskKey = taskKey;
                var jobException = new EVTaskException(errorCode, exp, TaskLogInfo);
                throw (jobException);
            }
        }

        /// <summary>
        /// Delete the tag
        /// </summary>
        /// <param name="taskDetails">Task related details</param>
        private void DeleteTag(BulkTagJobTaskBusinessEntity taskDetails)
        {
            RVWTagService.RemoveTag(taskDetails.TagDetails.Id.ToString(CultureInfo.InvariantCulture),
                taskDetails.TagDetails.CollectionId,
                taskDetails.TagDetails.MatterId.ToString(CultureInfo.InvariantCulture));
            if (_tagDeleteStatusList != null && taskDetails.TaskNumber <= _tagDeleteStatusList.Count)
            {
                _tagDeleteStatusList[taskDetails.TaskNumber - 1] = string.Format("{0}:{1}", taskDetails.TagDetails.Id,
                    true);
            }
        }

        /// <summary>
        /// Send notification message for each tag that was tagged
        /// </summary>
        /// <param name="jobParameters">Input parameters of job</param>
        /// <param name="jobDetail">Job related details</param>
        /// <param name="location">Location where tagging was done</param>
        private void SendNotificationToUser(BulkTagJobBusinessEntity jobParameters, JobBusinessEntity jobDetail,
            string location)
        {
            var tagDeleteDetails = _tagDeleteStatusList[0].Split(Constants.Colon.ToCharArray());
            var hasTagBeenDeleted = tagDeleteDetails.Length > 1 && bool.Parse(tagDeleteDetails[1]);
            //Build notification message
            var notificationMessage =
                BuildNotificationMessage(jobParameters.TagDetails.Id.ToString(CultureInfo.InvariantCulture), location,
                    (jobDetail.Status == 8 && !_isTagNotExists && !hasTagBeenDeleted), false);

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

            CustomNotificationMessage = notificationMessage;
            EvLog.WriteEntry(notificationLogEntry.ToString(), notificationValues.ToString(),
                EventLogEntryType.Information);
        }

        private static IRvwReviewerSearchService _mRvwSearchServiceInstance;

        #region Instancevariables

        /// <summary>
        /// Gets the RVW reviewer search service instance.
        /// </summary>
        private static IRvwReviewerSearchService ReviewerSearchServiceInstance
        {
            get { return _mRvwSearchServiceInstance ?? (_mRvwSearchServiceInstance = new RVWReviewerSearchService()); }
        }

        #endregion


        /// <summary>
        /// Get documents that have been applied with this tag
        /// </summary>
        /// <param name="jobParameters">Job related parameters</param>
        /// <param name="tagDetails">Tag related details</param>
        /// <returns>Resultant list of documents</returns>
        private List<RVWDocumentTagBEO> GetDocumentsWithThisTag(BulkTagJobBusinessEntity jobParameters,
            RVWTagBEO tagDetails)
        {
            var documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    ReviewsetId = jobParameters.DocumentListDetails.SearchContext.ReviewSetId,
                    DatasetId = jobParameters.DocumentListDetails.SearchContext.DataSetId,
                    MatterId = jobParameters.DocumentListDetails.SearchContext.MatterId,
                    IsConceptSearchEnabled = jobParameters.DocumentListDetails.SearchContext.IsConceptSearchEnabled
                }
            };


            var outputFields = new List<Field>();
            outputFields.AddRange(new List<Field>
            {
                new Field {FieldName = EVSystemFields.FamilyId},
                new Field {FieldName = EVSystemFields.ReviewSetId},
                new Field {FieldName = EVSystemFields.DuplicateId},
                new Field {FieldName = EVSystemFields.Duplicate},
                new Field {FieldName = EVSystemFields.DcnField}
            });
            documentQueryEntity.OutputFields.AddRange(outputFields); //Populate fetch duplicates fields
            documentQueryEntity.TotalRecallConfigEntity = new TotalRecallConfigEntity();
            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});
            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.DocumentStartIndex = 0;
            documentQueryEntity.DocumentCount = 999999;
       
            documentQueryEntity.TotalRecallConfigEntity.IsTotalRecall = true;

            string[] queryString = {Constants.DoubleQuote, tagDetails.TagDisplayName, Constants.DoubleQuote};
            documentQueryEntity.QueryObject.QueryList.Add(new Query(EVSearchSyntax.Tag.ConcatStrings(queryString)));
            documentQueryEntity.TransactionName = "BulkTagDelete - GetDocumentsWithThisTag";
            var searchResults = ReviewerSearchServiceInstance.GetSearchResults(documentQueryEntity);
            var filteredDocuments = searchResults.Documents.Select(document =>
                new RVWDocumentTagBEO
                {
                    CollectionId = document.DocumentId.CollectionId,
                    MatterId = Convert.ToInt64(document.DocumentId.MatterId),
                    DocumentId = document.DocumentId.DocumentId,
                    IsLocked = document.IsLocked,
                    DCN = GetFieldValueForFieldOfDocument(document.FieldValues, Constants.DcnField),
                    TagId = tagDetails.Id,
                    TagName = tagDetails.Name,
                    TagDisplayName = tagDetails.TagDisplayName
                }).ToList();
      
            return filteredDocuments;
        }

        /// <summary>
        /// Get field value for given field of document
        /// </summary>
        /// <param name="documentFieldValues">Field values of document</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        private static string GetFieldValueForFieldOfDocument(List<DocumentField> documentFieldValues, string fieldName)
        {
            var fieldValue = string.Empty;
            DocumentField field;
            if (string.Compare(fieldName, Constants.DcnField, true, CultureInfo.InvariantCulture) == 0)
            {
                field = documentFieldValues.Find(
                    x => string.Compare(x.Type, "3000", true, CultureInfo.InvariantCulture) == 0);
            }
            else
            {
                field = documentFieldValues.Find(
                    x => string.Compare(x.FieldName, fieldName, true, CultureInfo.InvariantCulture) == 0);
            }
            if (field != null)
            {
                fieldValue = field.Value;
            }
            return fieldValue;
        }

        /// <summary>
        /// This method determines and returns the content value for the specific field name
        /// </summary>
        /// <param name="fields">List of fields</param>
        /// <param name="fieldName">Field Name for which value is to be fetched</param>
        /// <returns>Field Value to be fetched</returns>
        public static string GetContent(List<FieldResult> fields, string fieldName)
        {
            var content = string.Empty;

            // -- determine the content value for the given field name
            var fieldContent =
                from field in fields
                where field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)
                select field.Value;

            if (fieldContent.Any())
            {
                content = fieldContent.First();
            }
            return HttpUtility.HtmlDecode(content);
        }


        /// <summary>
        /// This method will return BulkTagJobBusinessEntity out of the passed boot parameter
        /// </summary>
        /// <param name="bootParamter">String</param>
        /// <returns>BulkTagJobBusinessEntity</returns>
        private BulkTagJobBusinessEntity GetBulkTagDeleteOperationDetails(String bootParamter)
        {
            //Creating a stringReader stream for the boot parameter
            using (var stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                var xmlStream = new XmlSerializer(typeof (BulkTagJobBusinessEntity));

                //Deserialization of boot parameter to get BulkTagJobBusinessEntity
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
        /// <param name="tagName">Tag Name</param>
        private void AuditAndNotifyForNoTasksFound(string userGuid, string jobId, string jobRunId,
            RVWSearchBEO searchContext, string tagName)
        {
            //Fetch the user information
            var objUser = UserService.GetUser(StringUtility.EncodeTo64(userGuid));

            //Send notification message
            var jobDetail = JobService.GetJobDetails(jobId);
            jobDetail.CreatedById = objUser.UserGUID;

            if (jobDetail.NotificationId > 0)
            {
                var location = GetTaggingLocation(searchContext);

                var objNotificationMessage = new NotificationMessageBEO
                {
                    NotificationId = jobDetail.NotificationId,
                    SubscriptionTypeId = 1,
                    CreatedByUserGuid = jobDetail.CreatedById,
                    CreatedByUserName = jobDetail.CreatedBy,
                    NotificationSubject =
                        jobDetail.Name + Constants.Instance +
                        jobRunId
                };
                //Build notification message
                var notificationMessage = BuildNotificationMessage(tagName, location, false, true);

                objNotificationMessage.NotificationBody = notificationMessage;
                var notificationValues = objNotificationMessage.NotificationId + Constants.SpaceHiphenSpace +
                                         objNotificationMessage.CreatedByUserGuid + Constants.SpaceHiphenSpace +
                                         objNotificationMessage.NotificationSubject + Constants.SpaceHiphenSpace +
                                         objNotificationMessage.NotificationBody;

                //Send notification only if all the mandatory values for notification are populated
                EvLog.WriteEntry(jobDetail.Name + Constants.SpaceHiphenSpace + jobDetail.Id, notificationValues,
                    EventLogEntryType.Information);
                if ((objNotificationMessage.NotificationId != 0) &&
                    (objNotificationMessage.CreatedByUserGuid != null ||
                     objNotificationMessage.CreatedByUserGuid != string.Empty) &&
                    (objNotificationMessage.NotificationSubject != null ||
                     objNotificationMessage.NotificationSubject != string.Empty) &&
                    (objNotificationMessage.NotificationBody != null ||
                     objNotificationMessage.NotificationBody != string.Empty))
                {
                    NotificationBO.SendNotificationMessage(objNotificationMessage);
                }
            }
        }

        /// <summary>
        /// Build notification message to be sent on job success
        /// </summary>
        /// <returns>Notification message</returns>
        private string BuildNotificationMessage(string tagId, string location, bool isJobCancelled,
            bool isNoDocumentsFound)
        {
            var hasTagBeenDeleted = false;
            if (_tagDeleteStatusList.Count > 0)
            {
                var tagDeleteDetails = _tagDeleteStatusList[0].Split(Constants.Colon.ToCharArray());
                hasTagBeenDeleted = tagDeleteDetails.Length > 1 && bool.Parse(tagDeleteDetails[1]);
            }
            //Add notification message to be sent
            var notificationMessage = new StringBuilder();
            notificationMessage.Append(Constants.Table);
            if (!isNoDocumentsFound)
            {
                if (isJobCancelled)
                {
                    GenerateJobCancelledNotificationHeading(notificationMessage);
                }
                else
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Header);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(_isTagNotExists || !hasTagBeenDeleted
                            ? Constants.NotificationMessageHeadingForBulkTagDeleteSuspend
                            : Constants.NotificationMessageHeadingForBulkTagDelete));
                    notificationMessage.Append(Constants.CloseHeader);
                    notificationMessage.Append(Constants.CloseRow);
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(
                        HttpUtility.HtmlEncode(_isTagNotExists
                            ? Constants.NotificationMessageForBulkTagDeleteSuspend +
                              Constants.NotificationMessageForTagDeletedBeforeTaskStart
                            : !hasTagBeenDeleted
                                ? Constants.NotificationMessageHeadingForBulkTagDeleteSuspend +
                                  Constants.NotificationMessageForTagDeleteFailure
                                : Constants.NotificationMessageForBulkTagDelete));
                    notificationMessage.Append(Constants.CloseColumn);
                    notificationMessage.Append(Constants.CloseRow);
                }
                GenerateGeneralNotificationDetails(location, notificationMessage, tagId, false);
                notificationMessage.Append(Constants.Row);
                notificationMessage.Append(Constants.Column);
                notificationMessage.Append(Constants.HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageForDocumentsUnTagged));
                notificationMessage.Append(Constants.HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode((_documentsUnTaggedCount).ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(Constants.CloseColumn);
                notificationMessage.Append(Constants.CloseRow);
                notificationMessage.Append(Constants.Row);
                notificationMessage.Append(Constants.Column);
                notificationMessage.Append(Constants.HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageForDocumentsNotUntagged));
                notificationMessage.Append(Constants.HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(_totalDocumentsFailedToUnTaggedCount.ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(Constants.CloseColumn);
                notificationMessage.Append(Constants.CloseRow);
            }
            else
            {
                notificationMessage.Append(Constants.Row);
                notificationMessage.Append(Constants.Header);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(_isTagNotExists || !hasTagBeenDeleted
                        ? Constants.NotificationMessageHeadingForBulkTagDeleteSuspend
                        : Constants.NotificationMessageHeadingForBulkTagDelete));
                notificationMessage.Append(Constants.CloseHeader);
                notificationMessage.Append(Constants.CloseRow);
                notificationMessage.Append(Constants.Row);
                notificationMessage.Append(Constants.Column);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(_isTagNotExists
                        ? Constants.NotificationMessageForBulkTagDeleteSuspend +
                          Constants.NotificationMessageForTagDeletedBeforeTaskStart
                        : !hasTagBeenDeleted
                            ? Constants.NotificationMessageForBulkTagDeleteSuspend +
                              Constants.NotificationMessageForTagDeleteFailure
                            : Constants.NotificationMessageForBulkTagDelete));
                notificationMessage.Append(Constants.CloseColumn);
                notificationMessage.Append(Constants.CloseRow);
                GenerateGeneralNotificationDetails(location, notificationMessage, tagId, true);
                if (!_isTagNotExists)
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageForNoTagToDelete));
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
        private static void GenerateJobCancelledNotificationHeading(StringBuilder notificationMessage)
        {
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Header);
            notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageHeadingForJobCancel));
            notificationMessage.Append(Constants.CloseHeader);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageForBulkTagDeleteJobCancel));
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
            notificationMessage.Append(HttpUtility.HtmlEncode(DateTime.Parse(_jobStartDate).ConvertToUserTime()));
            notificationMessage.Append(Constants.CloseColumn);
            notificationMessage.Append(Constants.CloseRow);
            notificationMessage.Append(Constants.Row);
            notificationMessage.Append(Constants.Column);
            notificationMessage.Append(Constants.HtmlBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageEndTime));
            notificationMessage.Append(Constants.HtmlCloseBold);
            notificationMessage.Append(HttpUtility.HtmlEncode(DateTime.Parse(_jobEndDate).ConvertToUserTime()));
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
                if (_selectedTagDetails != null)
                {
                    notificationMessage.Append(Constants.Row);
                    notificationMessage.Append(Constants.Column);
                    notificationMessage.Append(Constants.HtmlBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(Constants.NotificationMessageTagName));
                    notificationMessage.Append(Constants.HtmlCloseBold);
                    notificationMessage.Append(HttpUtility.HtmlEncode(_selectedTagDetails.Name));
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
        }

        /// <summary>
        /// Get tag related details
        /// </summary>
        /// <param name="taskDetails">Current Task Details</param>
        /// <returns>Tag Details</returns>
        private RVWTagBEO GetTagDetails(BulkTagJobTaskBusinessEntity taskDetails)
        {
            var currentTagDetails =
                RVWTagService.GetTag(taskDetails.TagDetails.Id.ToString(CultureInfo.InvariantCulture),
                    taskDetails.DocumentDetails.CollectionId,
                    taskDetails.DocumentDetails.MatterId);
            return currentTagDetails;
        }

        #endregion Helper Methods
    }
}