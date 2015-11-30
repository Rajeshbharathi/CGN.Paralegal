#region Header
//-----------------------------------------------------------------------------------------
// <copyright file=""UpdateReviewSetJob.cs"" company=""Lexis Nexis"">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <author>Ram Sundar</author>
//      <description>
//          This file contains the UpdateReviewSetJob class for updating the reviewset
//      </description>
//      <changelog>
//          <date value="16 March 2011">Intial Version</date>
//          <date value="29 April 2011">Make this job as Inproc</date>
//          <date value="25/05/2011">Empty Review set archive issue fix</date>
//          <date value="31/05/2011">Increased the chunk size while updating docs</date>
//          <date value="01/06/2011">Document Level auditing for create reviewset</date>
//          <date value="07/09/2011">Bug Fix #90071</date>
//          <date value="12/06/2011">Bug Fix #93366</date>
//          <date value="11/06/2012">Dev Bug Fix # 102159 And 102160</date>
//          <date value="06/07/2013">Dev Bug Fix # 142068 </date>
//          <date value="02/13/2015">CNEV 4.0 - Search Engine Replacement - ESLite Integration : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces

using System;
using System.Collections;
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
using LexisNexis.Evolution.Business.ReviewSet;

#endregion

namespace LexisNexis.Evolution.BatchJobs.UpdateReviewSet
{
    /// <summary>
    /// Update Job will Update the Review sets
    /// </summary>
    [Serializable]
    public class UpdateReviewSetJob : BaseJob<UpdateReviewSetJobBEO, UpdateReviewSetTaskBEO>
    {

        #region Job FrameWork Overiden Functions
        /// <summary>
        /// Initializes Job BEO 
        /// </summary>
        /// <param name="jobId">Update ReviewSet Job Identifier</param>
        /// <param name="jobRunId">Update ReviewSet Run Identifier</param>
        /// <param name="bootParameters">Boot parameters</param>
        /// <param name="createdBy">Update Reviewset created by</param>
        /// <returns>Update Reviewset Job Business Entity</returns>
        protected override UpdateReviewSetJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            UpdateReviewSetJobBEO updateReviewSetJobBeo = null;
            try
            {
                //Message that job has been initialized.
                EvLog.WriteEntry(jobId + Constants.InitMessage, Constants.JobStartMessage, EventLogEntryType.Information);

                //Populate the the UpdateReviewSetJobBEO from the boot Parameters
                updateReviewSetJobBeo = GetUpdateRsJobBeo(bootParameters);
                if (updateReviewSetJobBeo != null)
                {
                    updateReviewSetJobBeo.JobId = jobId;
                    updateReviewSetJobBeo.JobRunId = jobRunId;
                    UserBusinessEntity userBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                    updateReviewSetJobBeo.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A")) ? userBusinessEntity.UserId : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
                    userBusinessEntity = null;
                    updateReviewSetJobBeo.JobTypeName = Constants.JobName;
                    updateReviewSetJobBeo.JobName = Constants.JobName + DateTime.Now.ToString(CultureInfo.InvariantCulture);
                }
                EvLog.WriteEntry(jobId + Constants.InitMessage, Constants.JobEndMessage, EventLogEntryType.Information);
            }
            catch (EVException ex)
            {
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInJobInitialization, string.Empty);
            }
            catch (Exception ex)
            {
                //Handle exception in initialize
                EvLog.WriteEntry(jobId + Constants.JobError, ex.Message, EventLogEntryType.Error);
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInJobInitialization, string.Empty);
            }

            //return create reviewset Job Business Entity
            return updateReviewSetJobBeo;
        }

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="logInfo">Log information</param>
        /// <param name="exp">exception received</param>        
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="errorCode"> </param>
        /// <param name="taskKey">taskKey</param> 
        private static void LogException(LogInfo logInfo, Exception exp, LogCategory category, string errorCode, string taskKey)
        {
            switch (category)
            {
                case LogCategory.Job:
                    {
                        EVJobException jobException = new EVJobException(errorCode, exp, logInfo);
                        throw (jobException);
                    }
                default:
                    {
                        logInfo.TaskKey = taskKey;
                        EVTaskException taskException = new EVTaskException(errorCode, exp, logInfo);
                        throw (taskException);
                    }
            }
        }

        /// <summary>
        /// Generates No.of Reviewsets to be created tasks
        /// </summary>
        /// <param name="jobParameters">Create Reviewset BEO</param>
        /// <param name="lastCommitedTaskCount"> </param>
        /// <returns>List of Create ReviewsetJob Tasks (BEOs)</returns>
        protected override Tasks<UpdateReviewSetTaskBEO> GenerateTasks(UpdateReviewSetJobBEO jobParameters, out int lastCommitedTaskCount)
        {
            Tasks<UpdateReviewSetTaskBEO> tasks = new Tasks<UpdateReviewSetTaskBEO>();
            lastCommitedTaskCount = 0;
            try
            {
                if (jobParameters != null)
                {
                    /* Get Dataset Details for dataset id to get know about the Collection id and the Matter ID*/
                    DatasetBEO datasetEntity = DataSetService.GetDataSet(jobParameters.datasetId.ToString(CultureInfo.InvariantCulture));
                    string sMatterId = datasetEntity.Matter.FolderID.ToString(CultureInfo.InvariantCulture);
                    var _reviewSetEntity = ReviewSetBO.GetReviewSetDetails(sMatterId, jobParameters.ReviewSetId);

                    List<FilteredDocumentBusinessEntity> qualifiedDocuments = null;
                    if (jobParameters.Activity.Equals(Constants.Add))
                    {
                        qualifiedDocuments = GetQualifiedDocuments(jobParameters, jobParameters.datasetId.ToString(CultureInfo.InvariantCulture),
                            sMatterId, _reviewSetEntity.BinderId, Constants.Add);
                    }
                    else if (jobParameters.Activity.Equals(Constants.Remove) || jobParameters.Activity.Equals(Constants.Archive))
                    {
                        qualifiedDocuments = GetQualifiedDocuments(jobParameters, jobParameters.datasetId.ToString(CultureInfo.InvariantCulture),
                            sMatterId, _reviewSetEntity.BinderId, Constants.Remove);
                    }
                    jobParameters.Documents.Clear();

                    if (qualifiedDocuments != null && qualifiedDocuments.Count > 0)
                    {
                        List<RVWDocumentBEO> iterationDocuments = qualifiedDocuments.
                                        Select(d => new RVWDocumentBEO
                                        {
                                            DocumentId = d.Id,
                                            MatterId = Convert.ToInt64(d.MatterId),
                                            CollectionId = d.CollectionId,
                                            FamilyId = d.FamilyId,
                                            DocumentControlNumber = d.DCN,
                                            DuplicateId = d.DuplicateId
                                        }).ToList();

                        jobParameters.Documents.AddRange(iterationDocuments);
                    }
                    UpdateReviewSetTaskBEO updateReviewSetTaskBeo = ConvertToTaskBeo(jobParameters, _reviewSetEntity);
                    tasks.Add(updateReviewSetTaskBeo);
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
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInGenerateTasks, string.Empty);
            }
            return tasks;
        }

        /// <summary>
        /// Atomic work 1) Delete search sub-system Data 2) Delete Vault Data 3) Delete EVMaster Data 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(UpdateReviewSetTaskBEO task, UpdateReviewSetJobBEO jobParameters)
        {
            bool output = false;
            try
            {
                //Fetching the dataset details
                DataSetService.GetDataSet(task.DatasetId.ToString(CultureInfo.InvariantCulture));

                
                string activity = task.Activity;

                switch (activity)
                {
                    case Constants.Add:
                        {
                            //Assigning the reviewset id to the documents
                            IEnumerable<ReviewsetDocumentBEO> rsDocBeoList = PopulateReviewSetDocumentEntites(task, task.Documents, jobParameters.CreatedByGUID);
                            AddDocumentsBychunk(task.DatasetId.ToString(CultureInfo.InvariantCulture), task.reviewSetId, rsDocBeoList);
                            //Update Review set service                            
                            task.NumberOfDocuments += rsDocBeoList.Count();
                            task.StatusId = 1;
                            task.Action = "Add";
                            task.IsAuditable = true;
                            ReviewSetService.UpdateReviewSetJob(task);
                            break;
                        }
                    case Constants.Remove:
                        {
                            //Assigning the reviewset id to the documents
                            IEnumerable<ReviewsetDocumentBEO> rsDocBeoList = PopulateReviewSetDocumentEntites(task, task.Documents, jobParameters.CreatedByGUID);

                            //Remove documents from vault and search sub-system
                            RemoveDocumentsBychunk(task.DatasetId.ToString(CultureInfo.InvariantCulture), task.reviewSetId, rsDocBeoList);

                            //Update Review set service                            
                            task.NumberOfDocuments -= rsDocBeoList.Count();
                            task.StatusId = 1;
                            task.Action = "Remove";
                            task.IsAuditable = true;
                            ReviewSetService.UpdateReviewSetJob(task);
                            break;
                        }
                    case Constants.Archive:
                        {
                            if (task.Documents.Count > 0)
                            {
                                //Assigning the reviewset id to the documents
                                IEnumerable<ReviewsetDocumentBEO> rsDocBeoList = PopulateReviewSetDocumentEntites(task, task.Documents, jobParameters.CreatedByGUID);

                                //Remove documents from vault and search sub-system
                                RemoveDocumentsBychunk(task.DatasetId.ToString(CultureInfo.InvariantCulture), task.reviewSetId, rsDocBeoList);
                            }
                            //Update Review set service - Archiving                            
                            ReviewSetService.UpdateReviewSetJob(task);
                            break;
                        }
                    default:
                        {
                            //If the reviewset is getting archived
                            if (task.StatusId.Equals(2))
                            {
                                //Assigning the reviewset id to the documents
                                IEnumerable<ReviewsetDocumentBEO> rsDocBeoList = PopulateReviewSetDocumentEntites(task, task.Documents, jobParameters.CreatedByGUID);

                                //Remove documents from vault and search sub-system
                                RemoveDocumentsBychunk(task.DatasetId.ToString(CultureInfo.InvariantCulture), task.reviewSetId, rsDocBeoList);

                                //Update Review set service - Archiving                            
                                ReviewSetService.UpdateReviewSetJob(task);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Problem in DoAtomicWork of the UpdateReviewset");
                ex.Trace().Swallow();

            }
            return output;
        }

        #endregion

        #region PrivateMethods

        /// <summary>
        /// To Add docs to Review set or Remove docs from Review set
        /// </summary>
        /// <param name="reviewsetId"> </param>
        /// <param name="rsDocBeoList"></param>
        /// <param name="datasetId"> </param>
        /// <returns>boolean</returns>
        private bool AddDocumentsBychunk(string datasetId, string reviewsetId, IEnumerable<ReviewsetDocumentBEO> rsDocBeoList)
        {
            bool bFlag = true;
            int iChunkSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get(Constants.DocumentsChunkSize));
            int iRsDocCount = rsDocBeoList.Count();
            int iNoOfChunks = iRsDocCount / iChunkSize;
            int remValue = iRsDocCount % iChunkSize;

            for (int i = 0; i < iNoOfChunks; i++)
            {
                List<ReviewsetDocumentBEO> tempList = new List<ReviewsetDocumentBEO>();
                tempList = rsDocBeoList.ToList().GetRange(iChunkSize * i, iChunkSize);
                ReviewSetService.AddDocumentToReviewSet(datasetId, reviewsetId, tempList);
            }
            if (remValue > 0)
            {
                List<ReviewsetDocumentBEO> remainderList = new List<ReviewsetDocumentBEO>();
                remainderList = rsDocBeoList.ToList().GetRange(iChunkSize * iNoOfChunks, remValue);
                ReviewSetService.AddDocumentToReviewSet(datasetId, reviewsetId, remainderList);
            }
            return bFlag;
        }

        /// <summary>
        /// To Update docs to Review set or Remove docs from Review set
        /// </summary>
        /// <param name="reviewsetId"> </param>
        /// <param name="rsDocBeoList"></param>
        /// <param name="datasetId"> </param>
        /// <returns>boolean</returns>
        private bool RemoveDocumentsBychunk(string datasetId, string reviewsetId, IEnumerable<ReviewsetDocumentBEO> rsDocBeoList)
        {
            bool bFlag = true;
            int iChunkSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get(Constants.DocumentsChunkSize));
            int iRsDocCount = rsDocBeoList.Count();
            int iNoOfChunks = iRsDocCount / iChunkSize;
            int remValue = iRsDocCount % iChunkSize;

            for (int i = 0; i < iNoOfChunks; i++)
            {
                List<ReviewsetDocumentBEO> tempList = new List<ReviewsetDocumentBEO>();
                tempList = rsDocBeoList.ToList().GetRange(iChunkSize * i, iChunkSize);
                ReviewSetService.DeleteDocumentFromReviewSet(datasetId, reviewsetId, tempList);
            }

            if (remValue > 0)
            {
                List<ReviewsetDocumentBEO> remainderList = new List<ReviewsetDocumentBEO>();
                remainderList = rsDocBeoList.ToList().GetRange(iChunkSize * iNoOfChunks, remValue);
                ReviewSetService.DeleteDocumentFromReviewSet(datasetId, reviewsetId, remainderList);
            }
            return bFlag;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private UpdateReviewSetJobBEO GetUpdateRsJobBeo(String bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            using (StringReader stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                XmlSerializer xmlStream = new XmlSerializer(typeof(UpdateReviewSetJobBEO));
                //De serialization of boot parameter to get profileBEO
                return (UpdateReviewSetJobBEO)xmlStream.Deserialize(stream);
            }
        }

        /// <summary>
        /// Method to convert the entity
        /// </summary>
        /// <param name="reviewSetBeo"></param>
        /// <returns></returns>
        private static UpdateReviewSetTaskBEO ConvertToTaskBeo(UpdateReviewSetJobBEO reviewSetBeo, ReviewsetDetailsBEO reviewset)
        {
            UpdateReviewSetTaskBEO reviewSet = new UpdateReviewSetTaskBEO
                                                   {
                                                       ReviewSetId = reviewSetBeo.ReviewSetId,
                                                       ReviewSetName = reviewSetBeo.ReviewSetName,
                                                       DatasetId = reviewSetBeo.DatasetId,
                                                       BinderId = reviewset.BinderId,
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
        private static List<ReviewsetDocumentBEO> PopulateReviewSetDocumentEntites(UpdateReviewSetTaskBEO reviewSet, List<RVWDocumentBEO> lstDocument, string createdBy)
        {
            return lstDocument.Select(document => PolulateReviewSetDcoumentEntity(reviewSet.ReviewSetId, reviewSet.BinderId, document, createdBy)).ToList();
        }

        /// <summary>
        /// Populates the review set dcoument entity.
        /// </summary>
        /// <param name="reviewSetId">The review set id.</param>
        /// <param name="binderId">The binder ID.</param>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        private static ReviewsetDocumentBEO PolulateReviewSetDcoumentEntity(string reviewSetId, string binderId, RVWDocumentBEO document, string createdBy)
        {
            ReviewsetDocumentBEO reviewsetDocument = new ReviewsetDocumentBEO
                                                         {
                                                             DocumentId = document.DocumentId,
                                                             CollectionViewId = reviewSetId,
                                                             BinderId = binderId,
                                                             TotalRecordCount = document.TotalRecordCount,
                                                             FamilyId = document.FamilyId,
                                                             DCN = document.DocumentControlNumber,
                                                             CreatedBy = createdBy
                                                         };
            return reviewsetDocument;
        }

        /// <summary>
        /// Get the Qualified Documents
        /// </summary>
        /// <param name="jobParameters"></param>
        /// <param name="datasetId"> </param>
        /// <param name="sMatterId"></param>
        /// <param name="operation"> </param>
        /// <returns></returns>        
        private static List<FilteredDocumentBusinessEntity> GetQualifiedDocuments(UpdateReviewSetJobBEO jobParameters,
            string datasetId, string sMatterId, string sBinderId, string operation)
        {
            DocumentQueryEntity documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    DatasetId = Convert.ToInt64(datasetId),
                    MatterId = Convert.ToInt64(sMatterId),
                    IsConceptSearchEnabled = jobParameters.DocumentSelectionContext.SearchContext.IsConceptSearchEnabled,
                    ReviewsetId = operation.Equals(Constants.Add) ? string.Empty : jobParameters.ReviewSetId
                }
            };
            

            if (operation.Equals(Constants.Add))
            {
                if (!String.IsNullOrEmpty(jobParameters.DocumentSelectionContext.SearchContext.Query))
                {
                    jobParameters.DocumentSelectionContext.SearchContext.Query = string.Format("({0}) AND (NOT ({1}:\"{2}\"))", 
                        jobParameters.DocumentSelectionContext.SearchContext.Query, EVSystemFields.BinderId.ToLowerInvariant(), sBinderId);
                }
                else
                {
                    jobParameters.DocumentSelectionContext.SearchContext.Query = string.Format("(NOT ({0}:\"{1}\"))", 
                        EVSystemFields.BinderId.ToLowerInvariant(), sBinderId);
                }
            }

            documentQueryEntity.QueryObject.QueryList.Add(new Query(jobParameters.DocumentSelectionContext.SearchContext.Query));
            documentQueryEntity.SortFields.Add(new Sort { SortBy = Constants.Relevance });
            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.TransactionName = "UpdateReviewSetJob - GetQualifiedDocuments";
            // -- get the qualified documents to beassigned for the reviewset
            List<FilteredDocumentBusinessEntity> qualifiedDocuments = JobSearchHandler.GetFilteredListOfDocuments(documentQueryEntity, jobParameters.DocumentSelectionContext);
            return qualifiedDocuments;
        }

        #endregion
    }
}
