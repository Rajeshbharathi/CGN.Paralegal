#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetLogicWorker" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Sivasankari Partheeban</author>
//      <description>
//          This file has methods to read data from Search worker and Manipulate documents to go into respective reviewset and
//          create reviewset in DB
//      </description>
//      <changelog>
//          <date value="01-Feb-2012">Created</date>
//          <date value="03-Feb-2012">Modified LogMessage</date>
//          <date value="19-Mar-2012">Modified code for splitting options for bugs: 93874, 97712, 97714, 97762, 97947 </date>
//          <date value="28-Mar-2012">Bugs Fixed #98349</date>
//          <date value="28-Mar-2012">Bugs Fixed #98601</date>
//          <date value="10-May-2012">changed logic, When there is family/duplicate documents and splitting into 2 review sets containing more than 5000 documents per reviewset.
//          Incase of splitting options, then collect all the documents else send in batches.
//          <date value="14-May-2012">Bug Fix #100793 - during splitting options removed code for sorting as the result is already sorted with DCN in Search worker</date>
//          <date value="11-28-2012">Bug Fix # 111296 - Split reviewsets are not getting created in QA CPM - 2.85.12 (Pre-Build)</date>
//          <date value="09/06/2013">CNEV 2.2.2 - Split Reviewset NFR fix - babugx</date>
//          <date value="10/07/2013">Bug Fix # 154039 - CNEV 2.2.2 - Create reviewset job fix - babugx</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//         <date value="03/26/2014">CNEV 3.0 - Task #165090 - 250K dataset with families batch-set creation fix : babugx</date>
//          </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespace
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;
using System.Web;
using LexisNexis.Evolution.Business.ReviewSet;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;


#endregion

namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Business.Binder;

    public class ReviewsetLogicWorker : WorkerBase
    {
        private int m_TotalDocumentCount;
        //to do:this needs to be configurable value
        private int m_BatchSize = 0;

        // Split Job Variables
        private int m_TotalDocumentsInSplitReviewSet = 0;
        private string m_splitReviewsetNames = string.Empty;
        string splitReviewsetId = string.Empty;
        string splitReviewsetName = String.Empty;

        //to hold the documents received from Search worker
        private List<DocumentIdentityRecord> m_ReceivedDocuments = new List<DocumentIdentityRecord>();

        //to hold all the documents sent to Vault worker
        private Dictionary<string, DocumentDetails> m_SentDocuments = new Dictionary<string, DocumentDetails>();

        //to store the last created reviewset id for single reviewset
        private string m_LastUsedReviewSetId = string.Empty;
        //to accumulate the Reviewset IDs
        private List<string> m_ReviewsetIds = new List<string>();

        private bool m_KeepDuplicates = false;
        private bool m_KeepFamilies = false;

        //holds list of all reviewset and its documents count details to process further
        private Dictionary<string, ReviewsetDetails> m_AllReviewsets = new Dictionary<string, ReviewsetDetails>();

        /* Get All Reviewset Details for dataset id */
        private List<ReviewSetPropertiesBEO> m_AllReviewSetinBinder;
        private int iSetStartNo = 0;
        private MockWebOperationContext _webContext;

        protected override void BeginWork()
        {
            base.BeginWork();
            m_BatchSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("TagBatchSize", "Reviewset"));
        }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var reviewsetRecord = (DocumentRecordCollection)envelope.Body;
            splitReviewsetId = reviewsetRecord.ReviewsetDetails.ReviewSetId;
            splitReviewsetName = reviewsetRecord.ReviewsetDetails.ReviewSetName;

            try
            {

                if (EVHttpContext.CurrentContext == null)
                {
                    MockSession();
                }
                //get all the documents from all the instances
                m_TotalDocumentCount = reviewsetRecord.TotalDocumentCount;
                m_TotalDocumentCount.ShouldBeGreaterThan(0);
                reviewsetRecord.Documents.ShouldNotBe(null);
                //get all the documents from all the search worker instances
                m_ReceivedDocuments.AddRange(reviewsetRecord.Documents);


                //Review set(s) will be created only during first times and documents will be associated to created review sets
                //once review sets are created, do not create again
                if (m_AllReviewsets.Count.Equals(0))
                {
                    using (new EVTransactionScope(TransactionScopeOption.Suppress))
                    {
                        // Babugx : To do - Replace ReviewsetBO call with BinderBO call, once current VaultID empty issue resolved
                        // m_AllReviewSetinBinder = BinderBO.GetAllReviewSetsForBinder(reviewsetRecord.ReviewsetDetails.BinderFolderId.ToString());
                        //this.m_AllReviewSetinBinder = ReviewSetBO.GetAllReviewSetForDataSet(reviewsetRecord.ReviewsetDetails.DatasetId.ToString());
                        this.m_AllReviewSetinBinder = BinderBO.GetAllReviewSetsForBinder(reviewsetRecord.ReviewsetDetails.BinderFolderId.ToString(),
                            reviewsetRecord.ReviewsetDetails.BinderId,
                            reviewsetRecord.ReviewsetDetails.DatasetId.ToString(),
                            reviewsetRecord.ReviewsetDetails.MatterId.ToString()).ToList();
                    }

                    if (m_AllReviewSetinBinder.Any() && reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity)
                    {
                        if (m_AllReviewSetinBinder.Exists(rs => rs.ReviewSetId.Equals(splitReviewsetId)))
                        {
                            m_TotalDocumentsInSplitReviewSet =
                                Convert.ToInt32(
                                    m_AllReviewSetinBinder.FirstOrDefault(rs => rs.ReviewSetId.Equals(splitReviewsetId))
                                                           .NumberOfDocuments);

                        }
                        iSetStartNo = GetReviewSetStartingNumber(m_AllReviewSetinBinder, splitReviewsetName);
                    }

                    //check for reviewset splitting logic
                    //1. Single Review set
                    //If the splitting option is "Single" reviewset then no property need to be changed
                    //Keep Family and Keep duplicates also need not be considered

                    if (reviewsetRecord.ReviewsetDetails.Activity != Constants.SplitActivity && reviewsetRecord.ReviewsetDetails.SplittingOption.ToLower().Equals("single"))
                    {
                        //only one reviewset will be created                            
                        CreateSingleReviewset(reviewsetRecord);
                    }
                    //2. Distribute evenly
                    //if splitting option is "Distribute", based on the options reviewset name must be changed
                    //Keep family and Keep duplicates together need to be considered
                    //Considers documents in a family and duplicates are sent in one batch from search worker
                    /*
                        * Conditions to check
                        * ===================
                        * 1. If total documents is greater than or equal to number of documents per set, then only one reviewset will
                        *    be created and the reviewset name will not have any number 
                        * 2. If number of review set is one, then only one reviewset will be created and the reviewset name will not have any number
                        * 3. If number of documents per set is specified and if number of documents is odd and have remaining documents then add one more
                        *    reviewset and add remaining documents to that. But this will not have exact number of documents that was set originally
                        * 4. If number of reviewset is specified and if number of documents cannot be divided equally, then reminder document will be
                        *    added to the last one. But this will not have exact number of documents that was set originally
                        * 5. Say, if there are 30 documents and every 3 documents are duplicates. Say user inputs to create reviewset with 2 docs per set, originally
                        *    it will create 15 reviewsets but documents will be assigned to only 10 reviewsets and rest of the other 5 reviewsets will be having
                        *    document count as "0". 
                        * 6. Say, if there are 30 documents and every 3 documents are in a family. Say user inputs to create reviewset with 2 docs per set, originally
                        *    it will create 15 reviewsets but documents will be assigned to only 10 reviewsets and rest of the other 5 reviewsets will be having
                        *    document count as "0".  
                        * 7. Do not remove any reviewset that needs to hold "0"    documents // todo: confirm with Paul
                        */

                    //calculate total number of documents in a reviewset based on splitting options
                    if (reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity || reviewsetRecord.ReviewsetDetails.SplittingOption.ToLower().Equals("distribute"))
                    {
                        //get the user values from reviewset details
                        m_KeepDuplicates = reviewsetRecord.ReviewsetDetails.KeepDuplicatesTogether;
                        m_KeepFamilies = reviewsetRecord.ReviewsetDetails.KeepFamilyTogether;

                        //check what is the condition
                        //  a. documents per set                        
                        if (!reviewsetRecord.ReviewsetDetails.NumberOfDocumentsPerSet.Equals(0))
                        {
                            //Condition 1 from above list
                            if (m_TotalDocumentCount <= reviewsetRecord.ReviewsetDetails.NumberOfDocumentsPerSet)
                            {
                                //only one reviewset will be created                            
                                CreateSingleReviewset(reviewsetRecord);
                            }
                            else
                            {
                                //do not process till all the documents are collected
                                if (m_ReceivedDocuments.Count.Equals(m_TotalDocumentCount) ||
                                    (m_ReceivedDocuments.Count > m_TotalDocumentCount && reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity))
                                {
                                    //split the documents and create reviewsets based on the number of documents per set
                                    SplitReviewsetOnNumberofDocs(reviewsetRecord);
                                }
                            }
                        }

                        //  b. total number of review sets
                        if (!reviewsetRecord.ReviewsetDetails.NumberOfReviewSets.Equals(0))
                        {
                            //Condition 2 from above list
                            if (reviewsetRecord.ReviewsetDetails.NumberOfReviewSets.Equals(1))
                            {
                                //only one reviewset will be created                            
                                CreateSingleReviewset(reviewsetRecord);
                            }
                            else
                            {
                                //do not process till all the documents are collected
                                if (m_ReceivedDocuments.Count.Equals(m_TotalDocumentCount) ||
                                    (m_ReceivedDocuments.Count > m_TotalDocumentCount && reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity))
                                {
                                    //split documents and create reviewset based on number of reviewset
                                    SplitDocumentsOnNumberofReviewsets(reviewsetRecord);
                                }
                            }
                        }
                    }
                }

                //send to vault worker after creating reviewset
                foreach (KeyValuePair<string, ReviewsetDetails> reviewset in m_AllReviewsets)
                {
                    if (!string.IsNullOrEmpty(reviewset.Key))
                    {
                        if (m_ReceivedDocuments.Count > 0)
                        {
                            //if calculated document count is already filled then move on to next reviewset in the list
                            if (reviewset.Value.FilledDocs.Equals(reviewset.Value.CalculatedNoOfDocs))
                                continue;


                            if (reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity)
                            {
                                reviewsetRecord.ReviewsetDetails.SplitReviewSetId = splitReviewsetId;
                                reviewsetRecord.ReviewsetDetails.SplitReviewSetName = splitReviewsetName;
                                reviewsetRecord.ReviewsetDetails.SplitPreDocumentCount = m_TotalDocumentsInSplitReviewSet;
                            }

                            reviewsetRecord.ReviewsetDetails.ReviewSetName = reviewset.Value.ReviewsetName;

                            //if there are any documents to send in last batch then add 1
                            int batchCount = ((reviewset.Value.CalculatedNoOfDocs % m_BatchSize) > 0) ? (reviewset.Value.CalculatedNoOfDocs / m_BatchSize) + 1 : (reviewset.Value.CalculatedNoOfDocs / m_BatchSize);
                            reviewsetRecord.ReviewsetDetails.NumberOfBatches = batchCount;

                            reviewsetRecord.ReviewsetDetails.NumberOfDocuments = reviewset.Value.CalculatedNoOfDocs;

                            reviewsetRecord.ReviewsetDetails.ReviewSetId = reviewset.Key;
                            SendtoVaultWorker(reviewsetRecord);
                            if (string.IsNullOrEmpty(m_splitReviewsetNames))
                            {
                                m_splitReviewsetNames = reviewset.Value.ReviewsetName;
                            }
                            else
                            {
                                m_splitReviewsetNames = string.Format("{0},{1}", m_splitReviewsetNames, reviewset.Value.ReviewsetName);
                            }


                            //if this reviewset itself is not complete then do not execute for other reviewsets
                            if (!reviewset.Value.FilledDocs.Equals(reviewset.Value.CalculatedNoOfDocs))
                                break;
                        }
                    }
                }

                
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, string.Format("Error in ReviewsetLogicWorker - Exception: {0}", ex.ToUserString()),
                    reviewsetRecord.ReviewsetDetails.CreatedBy, reviewsetRecord.ReviewsetDetails.ReviewSetName);
            }
        }

        private void MockSession()
        {
            #region Mock

            _webContext = new MockWebOperationContext();
            if (PipelineType == null) return;
            if (string.IsNullOrEmpty(PipelineType.Moniker)) return;

            string createdByGuid = null;
            if (PipelineType.Moniker.Equals(Constants.SplitReviewsetPipeLineType,
                StringComparison.InvariantCultureIgnoreCase))
            {
                var updateReviewSetJobBeo =
                    Utils.Deserialize<UpdateReviewSetJobBEO>(BootParameters);
                updateReviewSetJobBeo.ShouldNotBe(null);
                createdByGuid = updateReviewSetJobBeo.CreatedByGUID;
            }
            else
            {
                var createReviewSetJobBeo = Utils.Deserialize<CreateReviewSetJobBEO>(BootParameters);
                createReviewSetJobBeo.ShouldNotBe(null);
                createdByGuid = createReviewSetJobBeo.JobScheduleCreatedBy;
            }
            Utility.SetUserSession(createdByGuid);

            #endregion
        }

        /// <summary>
        /// Gets the review set starting number.
        /// </summary>
        /// <param name="lstReviewSet">The LST review set.</param>
        /// <param name="splittedReviewSet">The split review set.</param>
        /// <returns></returns>
        private int GetReviewSetStartingNumber(List<ReviewSetPropertiesBEO> lstReviewSet,string reviewSetName)
        {
            int iStartNo = 0;
            lstReviewSet = lstReviewSet.Where(o => (o.ReviewSetName.StartsWith(reviewSetName)) && (!o.ReviewSetName.Equals(reviewSetName))).ToList();
            //lstReviewSet = lstReviewSet.Where(rs => !rs.ReviewSetName.Equals(reviewSetName)).ToList();
            if (lstReviewSet.Any())
            {
                iStartNo = lstReviewSet.Max(o => int.Parse(o.ReviewSetName.Substring(reviewSetName.Length, o.ReviewSetName.Length - reviewSetName.Length)));
            }
            return iStartNo;
        }

        /// <summary>
        /// Creates single reviewset and generates the reviewset id
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void CreateSingleReviewset(DocumentRecordCollection reviewsetRecord)
        {
            reviewsetRecord.ShouldNotBe(null);

            //create reviewset in vault and get reviewset id
            if (String.IsNullOrWhiteSpace(m_LastUsedReviewSetId))
            {
                if (reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity)
                {
                    reviewsetRecord.ReviewsetDetails.ReviewSetName = string.Format("{0}{1}",
                        reviewsetRecord.ReviewsetDetails.ReviewSetName, (iSetStartNo + 1).ToString(CultureInfo.InvariantCulture));
                }
                m_LastUsedReviewSetId = CreateReviewset(reviewsetRecord);
            }
            //add review set details to the dictionary
            if (!m_AllReviewsets.ContainsKey(m_LastUsedReviewSetId) && (!string.IsNullOrEmpty(m_LastUsedReviewSetId)))
            {
                m_AllReviewsets.Add(m_LastUsedReviewSetId, new ReviewsetDetails { ReviewsetName = reviewsetRecord.ReviewsetDetails.ReviewSetName, CalculatedNoOfDocs = m_TotalDocumentCount });
            }
        }

        /// <summary>
        /// splits into different batches to send to vault worker
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void SendtoVaultWorker(DocumentRecordCollection reviewsetRecord)
        {
            reviewsetRecord.ShouldNotBe(null);
            int batchSize = m_BatchSize;

            //when the batch size is reached send to next worker
            if (m_ReceivedDocuments.Count >= batchSize)
            {
                //find the pending documents to update to reviewset
                int pendingDocsBeforeUpdate = m_AllReviewsets[reviewsetRecord.ReviewsetDetails.ReviewSetId].CalculatedNoOfDocs - m_AllReviewsets[reviewsetRecord.ReviewsetDetails.ReviewSetId].FilledDocs;
                int groupSize = batchSize;
                if (m_ReceivedDocuments.Count >= pendingDocsBeforeUpdate)
                {
                    groupSize = pendingDocsBeforeUpdate;
                }
                SendBatches(groupSize, reviewsetRecord.ReviewsetDetails.ReviewSetId, reviewsetRecord);
            }

            //find the pending documents to update to reviewset
            int pendingDocsAfterUpdate = m_AllReviewsets[reviewsetRecord.ReviewsetDetails.ReviewSetId].CalculatedNoOfDocs - m_AllReviewsets[reviewsetRecord.ReviewsetDetails.ReviewSetId].FilledDocs;

            //if there are any documents still pending
            if (m_ReceivedDocuments.Count > 0)
            {
                //if pending document is equal to excess document after batching then add all
                //the remaining documents and send as last instance to vault worker
                // or if the review set if filled with calculated number of documents even if it is less than batch count
                // still send the review set               
                if (m_ReceivedDocuments.Count.Equals(pendingDocsAfterUpdate) || pendingDocsAfterUpdate.Equals(m_AllReviewsets[reviewsetRecord.ReviewsetDetails.ReviewSetId].CalculatedNoOfDocs))
                {
                    SendBatches(((m_ReceivedDocuments.Count >= pendingDocsAfterUpdate) ? pendingDocsAfterUpdate : m_ReceivedDocuments.Count),
                        reviewsetRecord.ReviewsetDetails.ReviewSetId, reviewsetRecord);
                }
            }
        }

        private void SendBatches(int size, string id, DocumentRecordCollection reviewsetRecord)
        {
            int numberOfFullBatches = size / m_BatchSize;
            int lastBatchSize = size % m_BatchSize;

            for (int fullBatchNumber = 0; fullBatchNumber < numberOfFullBatches; fullBatchNumber++)
            {
                SendBatch(m_BatchSize, id, reviewsetRecord);
            }

            if (lastBatchSize > 0)
            {
                SendBatch(lastBatchSize, id, reviewsetRecord);
            }
        }

        private void SendBatch(int size, string id, DocumentRecordCollection reviewsetRecord)
        {
            size.ShouldBeGreaterThan(0);
            id.ShouldNotBe(null);
            reviewsetRecord.ShouldNotBe(null);

            if (m_ReceivedDocuments.Count > 0)
            {
                //temporary list to hold the documents
                var tempList = new List<DocumentIdentityRecord>();

                //add the batch size to temporary list
                tempList.AddRange(m_ReceivedDocuments.GetRange(0, size));

                //remove all the associated documents from the received documents list
                var remainingList = m_ReceivedDocuments.Except(tempList).ToList();
                m_ReceivedDocuments.Clear();
                m_ReceivedDocuments.AddRange(remainingList);

                //assign the current filling review set id to all the documents in the temp list
                tempList.ForEach(x => x.ReviewsetId = id);

                //do not want to maintain this when there is no logic for Keep Families & Keep Duplicates
                if (m_KeepFamilies || m_KeepDuplicates)
                {
                    tempList.ForEach(
                        x =>
                            m_SentDocuments.Add(x.DocumentId,
                                new DocumentDetails { DuplicateID = x.DuplicateId, FamilyID = x.FamilyId }));
                }

                //clear the documents and add the temp list with all the details
                reviewsetRecord.Documents.Clear();
                reviewsetRecord.Documents.AddRange(tempList);

                //update the documents filled count for the review set
                m_AllReviewsets[id].FilledDocs += tempList.Count;

                Send(reviewsetRecord);


            }
        }

        /// <summary>
        /// creates reviewset with given details
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        /// <returns></returns>
        private string CreateReviewset(DocumentRecordCollection reviewsetRecord)
        {
            reviewsetRecord.ShouldNotBe(null);
            string reviewsetName = reviewsetRecord.ReviewsetDetails.ReviewSetName;

            if (m_AllReviewSetinBinder.Exists(o => o.ReviewSetName.ToLower() == reviewsetName.ToLower()))
            {
                throw new Exception(string.Format("{0}{1}{2}", Constants.ReviewsetNameLog, reviewsetRecord.ReviewsetDetails.ReviewSetName, Constants.AlreadyExistsLog));
            }

            //create the review set with the details sent
            using (EVTransactionScope transScope = new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                CreateReviewSetTaskBEO reviewSetBusinesssEntity = ConverttoReviewsetBusinessEntity(reviewsetRecord.ReviewsetDetails);
                //Creates the reviewset
                string reviewsetId = ReviewSetBO.CreateReviewSetJob(reviewSetBusinesssEntity);
                
                return reviewsetId;
            };
        }

        /// <summary>
        /// Splits the document based on number of review sets and creates the master data in DB
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void SplitDocumentsOnNumberofReviewsets(DocumentRecordCollection reviewsetRecord)
        {
            reviewsetRecord.ShouldNotBe(null);
            reviewsetRecord.ReviewsetDetails.NumberOfReviewSets.ShouldBeGreaterThan(0);

            //temp holder for documents grouped
            List<DocumentGroupById> duplicateFamilyDocuments = new List<DocumentGroupById>();

            //holds review set names generated and number of documents to be assigned for each reviewset
            List<ReviewsetDetails> reviewsetList = new List<ReviewsetDetails>();

            //find if there is any reminder document after calculated documents per set
            int remainderDocuments = m_TotalDocumentCount % reviewsetRecord.ReviewsetDetails.NumberOfReviewSets;

            //find number of documents in a reviewset
            int docsPerReviewset = Convert.ToInt32(m_TotalDocumentCount / reviewsetRecord.ReviewsetDetails.NumberOfReviewSets);

            //get all the reviewset names and number of documents to be associated            
            for (int i = 1; i <= reviewsetRecord.ReviewsetDetails.NumberOfReviewSets; i++)
            {
                reviewsetList.Add(new ReviewsetDetails
                {
                    ReviewsetName = (reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity) ?
                    (reviewsetRecord.ReviewsetDetails.ReviewSetName + (iSetStartNo + i)).ToString(CultureInfo.InvariantCulture) :
                    (reviewsetRecord.ReviewsetDetails.ReviewSetGroup + i).ToString(CultureInfo.InvariantCulture),
                    OriginalNoOfDocs = docsPerReviewset
                });
            }

            //add the remaining documents equally in the reviewsets
            for (int i = 0; i < remainderDocuments; i++)
            {
                reviewsetList[i].OriginalNoOfDocs++;
            }

            //save the manipulated reviewset details in DB
            SaveReviewsetMasterData(reviewsetList, reviewsetRecord);
        }

        /// <summary>
        /// Creates n reviewsets distributing on documents
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void SplitReviewsetOnNumberofDocs(DocumentRecordCollection reviewsetRecord)
        {
            reviewsetRecord.ShouldNotBe(null);
            reviewsetRecord.ReviewsetDetails.NumberOfDocumentsPerSet.ShouldBeGreaterThan(0);

            //find if there is any reminder document after calculated documents per set
            int remainderDocuments = m_TotalDocumentCount % reviewsetRecord.ReviewsetDetails.NumberOfDocumentsPerSet;

            //find the total number of reviewsets
            int totalReviewsets = Convert.ToInt32(m_TotalDocumentCount / reviewsetRecord.ReviewsetDetails.NumberOfDocumentsPerSet);

            //if there is any reminder document then increase the total reviewset count by 1
            totalReviewsets = remainderDocuments > 0 ? totalReviewsets + 1 : totalReviewsets;

            //get all the reviewset names
            List<ReviewsetDetails> reviewsetList = new List<ReviewsetDetails>();
            for (int i = 1; i <= totalReviewsets; i++)
            {
                int numberOfDocs = reviewsetRecord.ReviewsetDetails.NumberOfDocumentsPerSet;

                //if there are any remaining documents then add to last reviewset
                if (remainderDocuments > 0 && i == totalReviewsets)
                {
                    numberOfDocs = remainderDocuments;
                }

                reviewsetList.Add(new ReviewsetDetails
                {
                    ReviewsetName = (reviewsetRecord.ReviewsetDetails.Activity == Constants.SplitActivity) ?
                    (reviewsetRecord.ReviewsetDetails.ReviewSetName + (iSetStartNo + i)).ToString(CultureInfo.InvariantCulture) :
                    (reviewsetRecord.ReviewsetDetails.ReviewSetGroup + i).ToString(CultureInfo.InvariantCulture),
                    OriginalNoOfDocs = numberOfDocs
                });
            }

            SaveReviewsetMasterData(reviewsetList, reviewsetRecord);
        }

        /// <summary>
        /// Re-calculates the number of documents to go into a reviewset based in families & duplicates
        /// </summary>
        /// <param name="reviewsetList"></param>
        private void SaveReviewsetMasterData(List<ReviewsetDetails> reviewsetList, DocumentRecordCollection reviewsetRecord)
        {
            List<DocumentGroupById> duplicateFamilyDocuments = new List<DocumentGroupById>();
            //if splitting logic is distributed and if either keep families or keep duplicates is set


            //if both "Keep Families" & "Keep Duplicates" are set then group by both Family ID and Duplicate ID
            if (m_KeepFamilies && m_KeepDuplicates)
            {
                var familyDuplicateGroup = m_ReceivedDocuments.GroupBy(x => x.GroupId).Where(x=> !string.IsNullOrEmpty(x.Key)).ToList();
                familyDuplicateGroup.ForEach(x => duplicateFamilyDocuments.Add(new DocumentGroupById { GroupID = x.Key.ToString(), DocumentCount = x.Count() }));

                var familyGroup = m_ReceivedDocuments.Where(d=> String.IsNullOrEmpty(d.GroupId)).GroupBy(x => x.FamilyId).ToList();
                familyGroup.ForEach(x => duplicateFamilyDocuments.Add(new DocumentGroupById
                {
                    GroupID = x.Key,
                    DocumentCount = x.Count()
                }));
            }

            //if only "Keep Families" is set then group by Family ID
            if (m_KeepFamilies && !m_KeepDuplicates)
            {
                var familyGroup = m_ReceivedDocuments.GroupBy(x => x.FamilyId).ToList();
                familyGroup.ForEach(x => duplicateFamilyDocuments.Add(new DocumentGroupById
                {
                    GroupID = x.Key,
                    DocumentCount = x.Count()
                }));
            }

            //if only "Keep Duplicates" is set then group by Duplicate Id
            if (m_KeepDuplicates && !m_KeepFamilies)
            {
                var duplicateGroup = m_ReceivedDocuments.GroupBy(x => x.DuplicateId).ToList();
                duplicateGroup.ForEach(x => duplicateFamilyDocuments.Add(new DocumentGroupById { GroupID = x.Key, DocumentCount = x.Count() }));
            }            

            //for every family and duplicate group
            foreach (DocumentGroupById documentGroup in duplicateFamilyDocuments)
            {
                if (String.IsNullOrEmpty(documentGroup.GroupID))
                {
                    CalculateNumberOfDocuments(documentGroup, reviewsetList, false);
                }
                else
                {
                    CalculateNumberOfDocuments(documentGroup, reviewsetList, true);
                }
            }

            //for every reviewset in the list add the reviewset details in Vault
            foreach (ReviewsetDetails reviewset in reviewsetList)
            {
                //set reviewset name
                reviewsetRecord.ReviewsetDetails.ReviewSetName = reviewset.ReviewsetName;

                //if there is no grouping, then keep the original count as calculated count
                if (!m_KeepDuplicates && !m_KeepFamilies)
                {
                    reviewset.CalculatedNoOfDocs = reviewset.OriginalNoOfDocs;
                }

                //set number of records in a reviewset
                reviewsetRecord.ReviewsetDetails.NumberOfDocuments = reviewset.CalculatedNoOfDocs;

                string tempReviewsetId = CreateReviewset(reviewsetRecord);

                //add review set details to the dictionary
                if (!m_AllReviewsets.ContainsKey(tempReviewsetId) && (!string.IsNullOrEmpty(tempReviewsetId)))
                {
                    m_AllReviewsets.Add(tempReviewsetId, new ReviewsetDetails
                    {
                        ReviewsetName = reviewset.ReviewsetName,
                        CalculatedNoOfDocs = reviewset.CalculatedNoOfDocs,
                        OriginalNoOfDocs = reviewset.OriginalNoOfDocs
                    });
                }
            }
        }

        /// <summary>
        /// Calculates the number of documents to be set for every review set
        /// For every review set in the list
        /// 1. Check if original document count for the review set is greater than calculated number of documents
        /// 2. If so, check if the document count need to be obtained from family duplicate grouping
        /// 3. If yes, then set family/ duplicate grouping count to the review set
        /// 4. If no, and it belongs to orphan group then also add the orphan group to the same review set
        /// 5. If no, and there is no orphan group then add the original count to the review set
        /// </summary>
        /// <param name="documentGroup"></param>
        /// <param name="reviewsetList"></param>
        /// <param name="useFamilyCount"></param>
        private void CalculateNumberOfDocuments(DocumentGroupById documentGroup, List<ReviewsetDetails> reviewsetList, bool useFamilyCount)
        {
            reviewsetList.ShouldNotBe(null);
            for (int i = 0; i < reviewsetList.Count; i++)
            {
                if (reviewsetList[i].OriginalNoOfDocs > reviewsetList[i].CalculatedNoOfDocs)
                {
                    if (useFamilyCount)
                    {
                        reviewsetList[i].CalculatedNoOfDocs += documentGroup.DocumentCount;
                    }
                    else
                    {
                        if (documentGroup != null && documentGroup.DocumentCount > 0)
                        {
                            //if there is orphan family, that can be splitted between review sets, calculated number of docs will increase by the difference between
                            //original docs and calculated number. Document group count has to be reduced by the same difference
                            if ((reviewsetList[i].CalculatedNoOfDocs + documentGroup.DocumentCount) > reviewsetList[i].OriginalNoOfDocs)
                            {
                                reviewsetList[i].CalculatedNoOfDocs += (reviewsetList[i].OriginalNoOfDocs - reviewsetList[i].CalculatedNoOfDocs);
                                documentGroup.DocumentCount -= (reviewsetList[i].OriginalNoOfDocs - reviewsetList[i].CalculatedNoOfDocs);
                                continue;
                            }
                            else
                            {
                                reviewsetList[i].CalculatedNoOfDocs += documentGroup.DocumentCount;
                                break;
                            }
                        }
                        else
                        {
                            reviewsetList[i].CalculatedNoOfDocs += reviewsetList[i].OriginalNoOfDocs;
                        }
                    }
                    if ((reviewsetList[i].CalculatedNoOfDocs > reviewsetList[i].OriginalNoOfDocs))
                    {
                        if (i + 1 < reviewsetList.Count)
                        {
                            reviewsetList[i + 1].OriginalNoOfDocs -= (reviewsetList[i].CalculatedNoOfDocs - reviewsetList[i].OriginalNoOfDocs);

                            //if the next set count becomes zero then pull from next set in the list
                            if ((reviewsetList[i + 1].OriginalNoOfDocs <= 0) && (i + 2 < reviewsetList.Count))
                            {
                                reviewsetList[i + 1].OriginalNoOfDocs = reviewsetList[i + 2].OriginalNoOfDocs;
                            }
                        }
                    }
                    if (useFamilyCount)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// converts review set record to reviewset business entity to save to reviewset master details in vault DB
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        /// <returns></returns>
        private CreateReviewSetTaskBEO ConverttoReviewsetBusinessEntity(ReviewsetRecord reviewsetRecord)
        {
            var reviewSetBusinesssEntity = new CreateReviewSetTaskBEO();

            reviewSetBusinesssEntity.ReviewSetName = reviewsetRecord.ReviewSetName;
            reviewSetBusinesssEntity.ReviewsetDescription = reviewsetRecord.ReviewSetDescription;
            reviewSetBusinesssEntity.DatasetId = reviewsetRecord.DatasetId;
            reviewSetBusinesssEntity.ReviewSetId = reviewsetRecord.ReviewSetId;
            reviewSetBusinesssEntity.BinderId = reviewsetRecord.BinderId;
            reviewSetBusinesssEntity.BinderName = reviewsetRecord.BinderName;
            reviewSetBusinesssEntity.DueDate = reviewsetRecord.DueDate;
            reviewSetBusinesssEntity.KeepDuplicates = reviewsetRecord.KeepDuplicatesTogether;
            reviewSetBusinesssEntity.KeepFamily = reviewsetRecord.KeepFamilyTogether;
            reviewSetBusinesssEntity.ReviewSetGroup = reviewsetRecord.ReviewSetGroup;
            reviewSetBusinesssEntity.ReviewSetLogic = reviewsetRecord.ReviewSetLogic;
            reviewSetBusinesssEntity.SearchQuery = reviewsetRecord.SearchQuery;
            reviewSetBusinesssEntity.SplittingOption = reviewsetRecord.SplittingOption;
            reviewSetBusinesssEntity.StartDate = reviewsetRecord.StartDate;
            reviewSetBusinesssEntity.NumberOfDocuments = reviewsetRecord.NumberOfDocuments;
            reviewSetBusinesssEntity.NumberOfReviewedDocs = reviewsetRecord.NumberOfReviewedDocs;
            reviewSetBusinesssEntity.ReviewSetUserList.AddRange(reviewsetRecord.ReviewSetUserList);
            if (PipelineType==null
                ||PipelineType.Moniker==null
                ||String.IsNullOrEmpty(PipelineType.Moniker))
                
            return reviewSetBusinesssEntity;

            if (PipelineType.Moniker.Equals(Constants.SplitReviewsetPipeLineType))
                reviewSetBusinesssEntity.Action = Constants.Spilt;
            return reviewSetBusinesssEntity;
        }

        private void Send(DocumentRecordCollection reviewsetRecord)
        {
            if (String.IsNullOrEmpty(reviewsetRecord.ReviewsetDetails.ReviewSetId))
            {
                LogMessage(false, "ReviewsetLogicWorker - reviewsetRecord.ReviewsetDetails.ReviewSetId is empty string", 
                    reviewsetRecord.ReviewsetDetails.CreatedBy, reviewsetRecord.ReviewsetDetails.ReviewSetName);
                throw new EVException().AddDbgMsg("reviewsetRecord.ReviewsetDetails.ReviewSetId is empty string");
            }

            var message = new PipeMessageEnvelope()
            {
                Body = reviewsetRecord
            };
            if (null != OutputDataPipe)
            {
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(reviewsetRecord.Documents.Count);
            }
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information, string createdBy, string reviewsetName)
        {
            try
            {
                var log = new List<JobWorkerLog<ReviewsetLogInfo>>();
                var parserLog = new JobWorkerLog<ReviewsetLogInfo>
                {
                    JobRunId =
                        (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                    CorrelationId = 0,
                    WorkerRoleType = Constants.ReviewsetLogicRoleID,
                    WorkerInstanceId = WorkerId,
                    IsMessage = false,
                    Success = status,
                    CreatedBy = createdBy,
                    LogInfo = new ReviewsetLogInfo { Information = information, ReviewsetName = reviewsetName }
                };
                // TaskId
                log.Add(parserLog);
                SendLog(log);
            }
            catch (Exception exception)
            {
                Tracer.Info("ReviewsetLogicWorker : LogMessage : Exception details: {0}", exception);
            }
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ReviewsetLogInfo>> log)
        {
            try
            {
                LogPipe.Open();
                var message = new PipeMessageEnvelope()
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
            catch (Exception exception)
            {
                Tracer.Info("ReviewsetLogicWorker : SendLog : Exception details: {0}", exception);
                throw;
            }
        }
    }

    /// <summary>
    /// holds the temporary review set details
    /// </summary>
    public class ReviewsetDetails
    {
        //review set name
        public string ReviewsetName { get; set; }

        //number of documents originally calculated based on boot parameters
        public int OriginalNoOfDocs { get; set; }

        //number of documents calculated based on review set rules "Keep Families" & "Keep duplicates" together
        public int CalculatedNoOfDocs { get; set; }

        //number of documents filled so far
        public int FilledDocs { get; set; }
    }

    /// <summary>
    /// holds temporary details of Family and Duplicate ID of a document
    /// </summary>
    public class DocumentDetails
    {
        public string FamilyID { get; set; }
        public string DuplicateID { get; set; }
    }

    /// <summary>
    /// holds temporary details of group by key and count after grouping documents based on "Keep Families" & "Keep duplicates"
    /// </summary>
    public class DocumentGroupById
    {
        public string GroupID { get; set; }
        public int DocumentCount { get; set; }
    }
}

