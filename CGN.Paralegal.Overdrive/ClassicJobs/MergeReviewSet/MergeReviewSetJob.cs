#region Header
//-----------------------------------------------------------------------------------------
// <copyright file=""MergeReviewSetJob.cs"" company=""Lexis Nexis"">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Ram Sundar</author>
//      <description>
//          This file contains the MergeReviewSetJob class for Merging the reviewsets
//      </description>
//      <changelog>
//          <date value="02/05/2011">Review Set Service call</date>
//          <date value="25/05/2011">Archive count issue fix</date>
//          <date value="28/05/2011">Empty Review set Archive issue fix</date>
//          <date value="31/05/2011">Increased the chunk size while updating docs</date>
//          <date value="03/06/2011">Implemented page wise searching</date>
//          <date value="23/09/2011">Page index issue fix</date>
//          <date value="23/09/2011">Bug Fix#93485</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/07/2012">Task # 101474 (Merge ReviewSet System Fields Rename)</date>
//          <date value="06/07/2012">DevBugFix#102068</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.Common;

#endregion

namespace LexisNexis.Evolution.BatchJobs.MergeReviewSet
{
    /// <summary>
    /// Merge Job will Merges the selected Reviewsets into a single review set
    /// </summary>
    [Serializable]
    public class MergeReviewSetJob : BaseJob<MergeReviewSetJobBEO, MergeReviewSetTaskBEO>
    {
        string _userGuid = string.Empty;
        #region Job FrameWork Overiden Functions
        /// <summary>
        /// Initializes Job BEO 
        /// </summary>
        /// <param name="jobId">Create ReviewSet Job Identifier</param>
        /// <param name="jobRunId">Create ReviewSet Run Identifier</param>
        /// <param name="bootParameters">Boot parameters</param>
        /// <param name="createdBy">Create Reviewset created by</param>
        /// <returns>Create Reviewset Job Business Entity</returns>
        protected override MergeReviewSetJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            MergeReviewSetJobBEO mergeReviewSetJobBeo = null;
            try
            {
                //Message that job has been initialized.
                EvLog.WriteEntry(jobId + Constants.InitMessage, Constants.JobStartMessage, EventLogEntryType.Information);
                //Populate the the UpdateReviewSetJobBEO from the boot Parameters
                mergeReviewSetJobBeo = GetUpdateRsJobBeo(bootParameters);

                if (mergeReviewSetJobBeo != null)
                {
                    mergeReviewSetJobBeo.JobId = jobId;
                    mergeReviewSetJobBeo.JobRunId = jobRunId;
                    _userGuid = createdBy;
                    UserBusinessEntity userBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                    mergeReviewSetJobBeo.JobScheduleCreatedBy = userBusinessEntity.UserId;
                    mergeReviewSetJobBeo.JobTypeName = Constants.JobName;
                    mergeReviewSetJobBeo.JobName = Constants.JobName + DateTime.Now.ToString(CultureInfo.InvariantCulture);
                }
                EvLog.WriteEntry(jobId + Constants.InitMessage, Constants.JobEndMessage, EventLogEntryType.Information);

            }
            catch (EVException ex)
            {
                HandleEVException();
                LogException(JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInJobInitialization);
            }
            catch (Exception ex)
            {
                //Handle exception in intialize
                EvLog.WriteEntry(jobId + Constants.JobError, ex.Message, EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInJobInitialization);
            }

            //return merge reviewset Job Business Entity
            return mergeReviewSetJobBeo;
        }

        /// <summary>
        /// Generates No.of Reviewsets to be created tasks
        /// </summary>
        /// <param name="jobParameters">Create Reviewset BEO</param>
        /// <param name="lastCommitedTaskCount">int</param>
        /// <returns>List of Create ReviewsetJob Tasks (BEOs)</returns>
        protected override Tasks<MergeReviewSetTaskBEO> GenerateTasks(MergeReviewSetJobBEO jobParameters, out int lastCommitedTaskCount)
        {
            Tasks<MergeReviewSetTaskBEO> tasks = new Tasks<MergeReviewSetTaskBEO>();
            lastCommitedTaskCount = 0;
            try
            {
                if (jobParameters != null)
                {
                    string datasetId = jobParameters.DatasetId.ToString(CultureInfo.InvariantCulture);
                    /* Get Dataset Details for dataset id to get know about the Collection id and the Matter ID*/
                    DatasetBEO datasetEntity = DataSetService.GetDataSet(datasetId);
                    string sMatterId = datasetEntity.Matter.FolderID.ToString(CultureInfo.InvariantCulture);
                    jobParameters.Activity = Constants.Create;

                    List<RVWDocumentBEO> docList = new List<RVWDocumentBEO>();
                    List<MergeReviewSetTaskBEO> mergedRsList = new List<MergeReviewSetTaskBEO>();

                    foreach (string reviewsetId in jobParameters.MergedReviewSetIds)
                    {
                        ReviewsetDetailsBEO reviewsetDetailsBeo = ReviewSetService.GetReviewSetDetails(sMatterId, reviewsetId);

                        ReviewerSearchResults qualifiedDocuments = new ReviewerSearchResults();
                        jobParameters.ReviewSetId = reviewsetId;
                        var queryContext = ConstructDocQueryEntity(jobParameters, datasetEntity);
                        queryContext.TransactionName = "MergeReviewSetJob - DoAtomicWork";
                        ReviewerSearchResults searchDocs = JobSearchHandler.GetAllDocuments(queryContext, false);

                        if (searchDocs != null)
                        {
                            searchDocs.ResultDocuments.SafeForEach(x => qualifiedDocuments.ResultDocuments.Add(x));
                        }

                        List<RVWDocumentBEO> iterationDocuments = qualifiedDocuments.ResultDocuments.
                                        Select(d => new RVWDocumentBEO
                                        {
                                            DocumentId = d.DocumentID,
                                            MatterId = d.MatterID,
                                            CollectionId = d.CollectionID,
                                            FamilyId = d.FamilyID
                                        }).ToList();

                        reviewsetDetailsBeo.Documents.Clear();
                        reviewsetDetailsBeo.Documents.AddRange(iterationDocuments);
                        reviewsetDetailsBeo.StatusId = 2;
                        MergeReviewSetTaskBEO mReviewsetDetailsBeo = ConvertToTaskBeo(reviewsetDetailsBeo);
                        docList.AddRange(reviewsetDetailsBeo.Documents);
                        mergedRsList.Add(mReviewsetDetailsBeo);
                    }
                    jobParameters.Documents.AddRange(docList);
                    tasks.Add(mergedRsList);
                    MergeReviewSetTaskBEO lstUpdateReviewSetTaskBeo = ConvertToTaskBeo(jobParameters);
                    tasks.Add(lstUpdateReviewSetTaskBeo);

                    for (int i = 1; i <= tasks.Count; i++)
                    {
                        tasks[i - 1].TaskNumber = i;
                    }
                }
                else
                {
                    lastCommitedTaskCount = 0;
                    EvLog.WriteEntry(Constants.JobLogName + Constants.GenerateTasks, Constants.JobParamND, EventLogEntryType.Error);
                    JobLogInfo.AddParameters(Constants.JobParamND);
                    JobLogInfo.IsError = true;
                }
            }
            catch (Exception ex)
            {
                LogException(JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInGenerateTasks);
            }
            return tasks;
        }
        /// <summary>
        /// Atomic work 1) Delete search sub-system Data 2) Delete Vault Data 3) Delete EVMaster Data 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(MergeReviewSetTaskBEO task, MergeReviewSetJobBEO jobParameters)
        {
            bool output = false;
            try
            {
                string datasetId = task.DatasetId.ToString(CultureInfo.InvariantCulture);

                switch (task.Activity)
                {
                    case Constants.Create:
                        {
                            /*since documents not needed for create reviewset, removing the docouments from the ReviewsetTaskBeo*/
                            List<RVWDocumentBEO> taskDocs = new List<RVWDocumentBEO>();
                            task.Documents.SafeForEach(taskDocs.Add);
                            task.Documents.Clear();

                            CreateReviewSetTaskBEO createReviewSetTaskBeo = ConvertToCTaskBeo(task);
                            createReviewSetTaskBeo.StatusId = 1;
                            createReviewSetTaskBeo.Activity = Constants.Merge;
                            createReviewSetTaskBeo.SplittingOption = Constants.Single;

                            /*Create Reviewset*/
                            task.reviewSetId = ReviewSetService.CreateReviewSetJob(createReviewSetTaskBeo);
                            taskDocs.SafeForEach(o => task.Documents.Add(o));

                            UpdateReviewSetTaskBEO updateReviewSetTaskBeo = ConvertToUTaskBeo(task);
                            updateReviewSetTaskBeo.StatusId = 1;
                            updateReviewSetTaskBeo.Activity = Constants.Merge;
                            updateReviewSetTaskBeo.SplittingOption = Constants.Single;
                            ReviewSetService.UpdateReviewSetJob(updateReviewSetTaskBeo);

                            //Assigning the reviewset id to the documents
                            IEnumerable<ReviewsetDocumentBEO> rsDocBeoList = PopulateReviewSetDocumentEntites(task.ReviewSetId, task.Documents);
                            output = AddDocumentsBychunk(datasetId, task.reviewSetId, rsDocBeoList);
                            output = true;
                            break;
                        }
                    default:
                        {
                            if (task.Documents.Count > 0)
                            {
                                //Assigning the reviewset id to the documents
                                IEnumerable<ReviewsetDocumentBEO> rsDocBeoList = PopulateReviewSetDocumentEntites(task.ReviewSetId, task.Documents);

                                //Delete documents from vault and  from search sub-system
                                output = RemoveDocumentsBychunk(datasetId, task.reviewSetId, rsDocBeoList);
                            }
                            //Update Review set service - Archiving
                            UpdateReviewSetTaskBEO updateReviewSetTaskBeo = ConvertToUTaskBeo(task);
                            ReviewSetService.UpdateReviewSetJob(updateReviewSetTaskBeo);
                            output = true;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                ex.AddResMsg(ErrorCodes.ProblemInDoAtomicWork).Trace().Swallow();
            }
            return output;
        }

        protected override void Shutdown(MergeReviewSetJobBEO jobParameters)
        {
            
        }

        #endregion

        #region PrivateMethods

        /// <summary>
        /// To Add docs to Review set or Remove docs from Review set
        /// </summary>
        /// <param name="datasetId"></param>
        /// <param name="reviewsetId"></param>
        /// <param name="rsDocBeoList"></param>
        /// <returns></returns>
        private bool AddDocumentsBychunk(string datasetId, string reviewsetId, IEnumerable<ReviewsetDocumentBEO> rsDocBeoList)
        {
            const bool bFlag = true;
            int iChunkSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get(Constants.DocumentsChunkSize));
            if (rsDocBeoList != null)
            {
                int iRsDocCount = rsDocBeoList.Count();
                int iNoOfChunks = iRsDocCount / iChunkSize;
                int remValue = iRsDocCount % iChunkSize;

                for (int i = 0; i < iNoOfChunks; i++)
                {
                    List<ReviewsetDocumentBEO> tempList = rsDocBeoList.ToList().GetRange(iChunkSize * i, iChunkSize);
                    ReviewSetService.AddDocumentToReviewSet(datasetId, reviewsetId, tempList);
                }

                if (remValue > 0)
                {
                    List<ReviewsetDocumentBEO> remainderList = rsDocBeoList.ToList().GetRange(iChunkSize * iNoOfChunks, remValue);
                    ReviewSetService.AddDocumentToReviewSet(datasetId, reviewsetId, remainderList);
                }
            }
            return bFlag;
        }

        /// <summary>
        /// To Update docs to Review set or Remove docs from Review set
        /// </summary>
        /// <param name="reviewsetId">string</param>
        /// <param name="rsDocBeoList">IEnumerable<ReviewsetDocumentBEO/></param>
        /// <param name="datasetId">string</param>
        /// <returns>boolean</returns>
        private bool RemoveDocumentsBychunk(string datasetId, string reviewsetId, IEnumerable<ReviewsetDocumentBEO> rsDocBeoList)
        {
            const bool bFlag = true;
            int iChunkSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get(Constants.DocumentsChunkSize));
            if (rsDocBeoList != null)
            {
                int iRsDocCount = rsDocBeoList.Count();
                int iNoOfChunks = iRsDocCount / iChunkSize;
                int remValue = iRsDocCount % iChunkSize;

                for (int i = 0; i < iNoOfChunks; i++)
                {
                    List<ReviewsetDocumentBEO> tempList = rsDocBeoList.ToList().GetRange(iChunkSize * i, iChunkSize);
                    ReviewSetService.DeleteDocumentFromReviewSet(datasetId, reviewsetId, tempList);
                }

                if (remValue > 0)
                {
                    List<ReviewsetDocumentBEO> remainderList = rsDocBeoList.ToList().GetRange(iChunkSize * iNoOfChunks, remValue);
                    ReviewSetService.DeleteDocumentFromReviewSet(datasetId, reviewsetId, remainderList);
                }
            }
            return bFlag;
        }

        /// <summary>
        /// Deserializing the bootparameters to form the list of update review set BEO
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private MergeReviewSetJobBEO GetUpdateRsJobBeo(String bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            using (StringReader stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                XmlSerializer xmlStream = new XmlSerializer(typeof(MergeReviewSetJobBEO));

                //De serialization of boot parameter to get profileBEO
                return (MergeReviewSetJobBEO)xmlStream.Deserialize(stream);
            }
        }

        /// <summary>
        /// EV Exception if thrown use error code for locating message from resource file.
        /// This function logs the message as well...
        /// </summary>
        /// <returns>Success Status.</returns>
        private void HandleEVException()
        {
        }

        /// <summary>
        /// Converts Merge Review Set Job BEO to Merge Review Set Task BEO 
        /// </summary>
        /// <param name="reviewSetBeo"></param>
        /// <returns></returns>
        private static MergeReviewSetTaskBEO ConvertToTaskBeo(MergeReviewSetJobBEO reviewSetBeo)
        {
            MergeReviewSetTaskBEO reviewSet = new MergeReviewSetTaskBEO
                                                  {
                                                      ReviewSetId = reviewSetBeo.ReviewSetId,
                                                      ReviewSetName = reviewSetBeo.ReviewSetName,
                                                      DatasetId = reviewSetBeo.DatasetId,
                                                      NumberOfDocumentsPerSet = reviewSetBeo.NumberOfDocumentsPerSet,
                                                      NumberOfReviewSets = reviewSetBeo.NumberOfReviewSets,
                                                      StatusId = reviewSetBeo.StatusId,
                                                      ReviewsetDescription = reviewSetBeo.ReviewSetDescription,
                                                      StartDate = reviewSetBeo.StartDate,
                                                      DueDate = reviewSetBeo.DueDate,
                                                      ReviewSetLogic = reviewSetBeo.ReviewSetLogic,
                                                      SplittingOption = reviewSetBeo.SplittingOption,
                                                      SearchQuery = reviewSetBeo.SearchQuery,
                                                      KeepDuplicates = reviewSetBeo.KeepDuplicates,
                                                      KeepFamily = reviewSetBeo.KeepFamily,
                                                      ReviewSetGroup = reviewSetBeo.ReviewSetGroup,
                                                      AssignTo = reviewSetBeo.AssignTo,
                                                      Activity = reviewSetBeo.Activity
                                                  };


            reviewSet.NumberOfDocuments = reviewSet.Documents != null ? reviewSetBeo.Documents.Count : 0;
            if (reviewSet.Documents != null) reviewSet.Documents.AddRange(reviewSetBeo.Documents);
            reviewSet.ReviewSetUserList.AddRange(reviewSetBeo.ReviewSetUserList);
            return reviewSet;
        }

        /// <summary>
        /// Converts Review Set Details BEO to Merge Review Set Task BEO 
        /// </summary>
        /// <param name="reviewSetBeo"></param>
        /// <returns></returns>
        private static MergeReviewSetTaskBEO ConvertToTaskBeo(ReviewsetDetailsBEO reviewSetBeo)
        {
            MergeReviewSetTaskBEO reviewSet = new MergeReviewSetTaskBEO
                                                  {
                                                      ReviewSetId = reviewSetBeo.ReviewSetId,
                                                      ReviewSetName = reviewSetBeo.ReviewSetName,
                                                      DatasetId = reviewSetBeo.DatasetId,
                                                      NumberOfDocumentsPerSet = reviewSetBeo.NumberOfDocumentsPerSet,
                                                      NumberOfReviewSets = reviewSetBeo.NumberOfReviewSets,
                                                      StatusId = reviewSetBeo.StatusId,
                                                      ReviewsetDescription = reviewSetBeo.Description,
                                                      StartDate = reviewSetBeo.StartDate,
                                                      DueDate = reviewSetBeo.DueDate,
                                                      ReviewSetLogic = reviewSetBeo.ReviewSetLogic,
                                                      SplittingOption = reviewSetBeo.SplittingOption,
                                                      SearchQuery = reviewSetBeo.SearchQuery,
                                                      KeepDuplicates = reviewSetBeo.KeepDuplicates,
                                                      KeepFamily = reviewSetBeo.KeepFamily,
                                                      ReviewSetGroup = reviewSetBeo.ReviewSetGroup,
                                                      AssignTo = reviewSetBeo.AssignTo,
                                                      NumberOfDocuments = reviewSetBeo.NumberOfDocuments
                                                  };



            reviewSet.Documents.AddRange(reviewSetBeo.Documents);
            reviewSet.ReviewSetUserList.AddRange(reviewSetBeo.ReviewSetUserList);

            return reviewSet;
        }

        /// <summary>
        /// Converts Merge Review Set Job BEO to Create Review Set Task BEO 
        /// </summary>
        /// <param name="reviewSetBeo"></param>
        /// <returns></returns>
        private static CreateReviewSetTaskBEO ConvertToCTaskBeo(MergeReviewSetTaskBEO reviewSetBeo)
        {
            CreateReviewSetTaskBEO reviewSet = new CreateReviewSetTaskBEO
                                                   {
                                                       ReviewSetId = reviewSetBeo.ReviewSetId,
                                                       ReviewSetName = reviewSetBeo.ReviewSetName,
                                                       DatasetId = reviewSetBeo.DatasetId,
                                                       NumberOfDocumentsPerSet = reviewSetBeo.NumberOfDocumentsPerSet,
                                                       NumberOfReviewSets = reviewSetBeo.NumberOfReviewSets,
                                                       StatusId = reviewSetBeo.StatusId,
                                                       ReviewsetDescription = reviewSetBeo.ReviewsetDescription,
                                                       StartDate = reviewSetBeo.StartDate,
                                                       DueDate = reviewSetBeo.DueDate,
                                                       ReviewSetLogic = reviewSetBeo.ReviewSetLogic,
                                                       SplittingOption = reviewSetBeo.SplittingOption,
                                                       SearchQuery = reviewSetBeo.SearchQuery,
                                                       KeepDuplicates = reviewSetBeo.KeepDuplicates,
                                                       KeepFamily = reviewSetBeo.KeepFamily,
                                                       ReviewSetGroup = reviewSetBeo.ReviewSetGroup
                                                   };



            reviewSet.Documents.AddRange(reviewSetBeo.Documents);
            reviewSet.ReviewSetUserList.AddRange(reviewSetBeo.ReviewSetUserList);

            return reviewSet;
        }

        /// <summary>
        /// Converts Merge Review Set Job BEO to Update Review Set Task BEO 
        /// </summary>
        /// <param name="reviewSetBeo"></param>
        /// <returns></returns>
        private static UpdateReviewSetTaskBEO ConvertToUTaskBeo(MergeReviewSetTaskBEO reviewSetBeo)
        {
            UpdateReviewSetTaskBEO reviewSet = new UpdateReviewSetTaskBEO
                                                   {
                                                       ReviewSetId = reviewSetBeo.ReviewSetId,
                                                       ReviewSetName = reviewSetBeo.ReviewSetName,
                                                       DatasetId = reviewSetBeo.DatasetId,
                                                       NumberOfDocumentsPerSet = reviewSetBeo.NumberOfDocumentsPerSet,
                                                       NumberOfReviewSets = reviewSetBeo.NumberOfReviewSets,
                                                       StatusId = reviewSetBeo.StatusId,
                                                       ReviewsetDescription = reviewSetBeo.ReviewsetDescription,
                                                       StartDate = reviewSetBeo.StartDate,
                                                       DueDate = reviewSetBeo.DueDate,
                                                       ReviewSetLogic = reviewSetBeo.ReviewSetLogic,
                                                       SplittingOption = reviewSetBeo.SplittingOption,
                                                       SearchQuery = reviewSetBeo.SearchQuery,
                                                       KeepDuplicates = reviewSetBeo.KeepDuplicates,
                                                       KeepFamily = reviewSetBeo.KeepFamily,
                                                       ReviewSetGroup = reviewSetBeo.ReviewSetGroup,
                                                       AssignTo = reviewSetBeo.AssignTo,
                                                       NumberOfDocuments = reviewSetBeo.NumberOfDocuments
                                                   };
            reviewSet.Documents.AddRange(reviewSetBeo.Documents);
            reviewSet.ReviewSetUserList.AddRange(reviewSetBeo.ReviewSetUserList);

            return reviewSet;
        }

        /// <summary>
        /// Populates the review set document entites.
        /// </summary>
        /// <param name="reviewSetId">The review set ID.</param>
        /// <param name="lstDocument">The document list.</param>
        /// <returns></returns>
        private static IEnumerable<ReviewsetDocumentBEO> PopulateReviewSetDocumentEntites(string reviewSetId, IEnumerable<RVWDocumentBEO> lstDocument)
        {
            return lstDocument.Select(document => PolulateReviewSetDcoumentEntity(reviewSetId, document)).ToList();
        }

        /// <summary>
        /// Populates the review set dcoument entity.
        /// </summary>
        /// <param name="reviewSetId">The review set id.</param>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        private static ReviewsetDocumentBEO PolulateReviewSetDcoumentEntity(string reviewSetId, RVWDocumentBEO document)
        {
            ReviewsetDocumentBEO reviewsetDocument = new ReviewsetDocumentBEO
                                                         {
                                                             DocumentId = document.DocumentId,
                                                             CollectionViewId = reviewSetId,
                                                             TotalRecordCount = document.TotalRecordCount,
                                                             FamilyId = document.FamilyId
                                                         };
            return reviewsetDocument;
        }

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="logMsg">Log Info</param>
        /// <param name="exp">exception received</param>        
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key to identify the Task, need for task log only</param>
        /// <param name="errorCode">errorCode</param>
        private static void LogException(LogInfo logMsg, Exception exp, LogCategory category, string taskKey, string errorCode)
        {
            switch (category)
            {
                case LogCategory.Job:
                    {
                        EVJobException jobException = new EVJobException(errorCode, exp, logMsg);
                        throw (jobException);
                    }
                default:
                    {
                        logMsg.TaskKey = taskKey;
                        EVTaskException jobException = new EVTaskException(errorCode, exp, logMsg);
                        throw (jobException);
                    }
            }
        }

        /// <summary>
        /// Constructs the doc query entity.
        /// </summary>
        /// <param name="jobParameters">The job parameters.</param>
        /// <param name="datasetEntity">The dataset entity.</param>
        /// <returns></returns>
        private static DocumentQueryEntity ConstructDocQueryEntity(CreateReviewSetJobBEO jobParameters, DatasetBEO datasetEntity)
        {
            DocumentQueryEntity documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    ReviewsetId = jobParameters.ReviewSetId,
                    MatterId = datasetEntity.Matter.FolderID,
                    IsConceptSearchEnabled = false,
                    DatasetId = datasetEntity.FolderID
                }
            };
            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.SortFields.Add(new Sort { SortBy = Constants.Relevance });
            documentQueryEntity.OutputFields.AddRange(new List<Field>
                                                          {
                                                            new Field { FieldName = EVSystemFields.FamilyId},
                                                            new Field { FieldName = EVSystemFields.ReviewSetId},
                                                            new Field { FieldName = datasetEntity.DocumentControlNumberName},
                                                            new Field { FieldName = EVSystemFields.DuplicateId},
                                                        });
            return documentQueryEntity;
        }

        #endregion
    }
}
