#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="SearchWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Giri</author>
//      <description>
//          This file does the common document search activity for the given query enity
//      </description>
//      <changelog>
//          <date value="28-Jan-2012">created</date>
//          <date value="03-Feb-2012">added logging code</date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//	        <date value="12/13/2012">Fix for bug 126298 - Family grouping for reviewset distribution : babugx</date>
//	        <date value="12/17/2012">Fix for bug 127167 - [R2.1]:[BVT]:Unable to create the reviewset for test data having families and threads : babugx</date>
//          <date value="09/06/2013">CNEV 2.2.2 - Split Reviewset NFR fix - babugx</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using System.Globalization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Business.DatasetManagement;

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// This class performs the search and returns the document results information
    /// </summary>
    public class SearchWorker : WorkerBase
    {
        #region Private Variables
        private CreateReviewSetJobBEO _jobParameter;
        private string _createdBy;
        private int _batchSize = 1000;
        private MockWebOperationContext _webContext;
        private DatasetBEO _dataset;
        
        #endregion

        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (CreateReviewSetJobBEO)XmlUtility.DeserializeObject(BootParameters, typeof(CreateReviewSetJobBEO));
           
        }

        /// <summary>
        /// This method processes the pipe message
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var searchRecord = (ReviewsetSearchRecord)envelope.Body;

            // assert checks
            searchRecord.ShouldNotBe(null);
            searchRecord.QueryEntity.ShouldNotBe(null);
            searchRecord.ReviewsetDetails.ShouldNotBe(null);

            try
            {
                // Initialize config values
                GetConfigurationValues();

                searchRecord.ReviewsetDetails.CreatedBy.ShouldNotBeEmpty();
                _createdBy = searchRecord.ReviewsetDetails.CreatedBy;
          

                DocumentRecordCollection reviewsetDetail;
                // Convert the ReviewsetSearchRecord to DocumentRecordCollection type
                ConvertReviewsetSearchRecordToDocumentRecordCollection(searchRecord, out reviewsetDetail);

                _dataset = DataSetBO.GetDataSetDetailForDataSetId(searchRecord.ReviewsetDetails.DatasetId);

                

                var documents=new List<DocumentIdentityRecord>();
                var reviewsetLogic = searchRecord.ReviewsetDetails.ReviewSetLogic.ToLower();
                if (reviewsetLogic == "all" || reviewsetLogic == "tag")
                {
                    var searchQuery = !string.IsNullOrEmpty(_jobParameter.SearchQuery)? _jobParameter.SearchQuery.Replace("\"", ""): string.Empty;
                    Tracer.Info("Get documents from database to create reviewset is started for All/Tag options - job run id : {0}", PipelineId);
                    var resultDocuments= DocumentBO.GetDocumentsForCreateReviewsetJob(searchRecord.QueryEntity.QueryObject.MatterId,
                                                                 _dataset.CollectionId,searchRecord.TotalDocumentCount,
                                                                 reviewsetLogic, searchQuery.ToLower(), _batchSize);

                    documents.AddRange(resultDocuments.Select(resultDocument => new DocumentIdentityRecord
                                                                                {
                                                                                    Id = resultDocument.Id,
                                                                                    DocumentId = resultDocument.DocumentID,
                                                                                    FamilyId = resultDocument.FamilyID, 
                                                                                    DuplicateId = resultDocument.DuplicateId
                                                                                }));
                    Tracer.Info("Documents retrieved from database to create review set for All/Tag options - job run id : {0}", PipelineId);
                }
                else
                {
                    documents = GetDocuments(searchRecord);
                }
                

                if (documents == null || !documents.Any())
                {
                    Tracer.Error("No documents found for the job run id : {0}", PipelineId);
                    LogMessage(false, string.Format("No documents found for the job run id : {0}", PipelineId),
                        _createdBy, searchRecord.ReviewsetDetails.ReviewSetName);
                    return;
                }

                Tracer.Info("Total of {0} documents found for the job run id : {1}", documents.Count.ToString(),
                    PipelineId);
                LogMessage(true,
                    string.Format("Total of {0} documents found for the job run id : {1}", documents.Count, PipelineId),
                    _createdBy, searchRecord.ReviewsetDetails.ReviewSetName);

                // Group the results and send it in batches
                GroupDocumentsAndSend(documents, reviewsetDetail);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, ex.ToUserString(), searchRecord.ReviewsetDetails.CreatedBy,
                    searchRecord.ReviewsetDetails.ReviewSetName);
            }
        }


        /// <summary>
        /// Processes the data.
        /// </summary>
        /// <param name="searchRecord"></param>
        public List<DocumentIdentityRecord> GetDocuments(ReviewsetSearchRecord searchRecord)
        {
            var documents = new List<DocumentIdentityRecord>();
            try
            {
                var searchContext = searchRecord.QueryEntity;

                searchContext.DocumentCount = searchRecord.TotalDocumentCount;
                searchContext.DocumentStartIndex = 0;
                searchRecord.QueryEntity.TransactionName =
                    searchContext.TransactionName = "SearchWorker - GetDocuments";
                searchContext.TotalRecallConfigEntity.IsTotalRecall = true;
                MockSession();

                var searchResults = SearchBo.Search(searchContext, true);

                if (searchResults != null && searchResults.Documents != null &&
                    searchResults.Documents.Any())
                {
                    searchResults.Documents.ForEach(r => documents.Add(ConvertToDocumentIdentityRecord(r)));
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                Tracer.Error("SearchWorker - GetDocuments : {0}", ex);
                throw;
            }
            return documents;
        }


        /// <summary>
        /// converts result document to document identity record
        /// </summary>
        /// <param name="resultDocument">ResultDocument</param>
        /// <returns>DocumentIdentityRecord</returns>
        private static DocumentIdentityRecord ConvertToDocumentIdentityRecord(ResultDocument resultDocument)
        {
            var documentIdentityRecord = new DocumentIdentityRecord
            {
                Id = resultDocument.DocumentId.Id,
                DocumentId = resultDocument.DocumentId.DocumentId,
                FamilyId = resultDocument.DocumentId.FamilyId,
                DuplicateId = resultDocument.DocumentId.DuplicateId
            };
            documentIdentityRecord.Fields.AddRange(resultDocument.FieldValues.ToDataAccessEntity());
            return documentIdentityRecord;
        }

        /// <summary>
        /// This method converts the ReviewsetSearchRecord type to DocumentRecordCollection
        /// </summary>
        /// <param name="searchRecord">ReviewsetSearchRecord</param>
        /// <param name="documentRecordCollection">ReviewsetSearchRecord</param>
        /// <returns></returns>
        private void ConvertReviewsetSearchRecordToDocumentRecordCollection
            (ReviewsetSearchRecord searchRecord, out DocumentRecordCollection documentRecordCollection)
        {
            documentRecordCollection = new DocumentRecordCollection();
            var reviewsetRecord = new ReviewsetRecord
            {
                Activity = searchRecord.ReviewsetDetails.Activity,
                CreatedBy = searchRecord.ReviewsetDetails.CreatedBy,
                DatasetId = searchRecord.ReviewsetDetails.DatasetId,
                MatterId = searchRecord.QueryEntity.QueryObject.MatterId,
                ReviewSetId = searchRecord.ReviewsetDetails.ReviewSetId,
                BinderFolderId = searchRecord.ReviewsetDetails.BinderFolderId,
                BinderId = searchRecord.ReviewsetDetails.BinderId,
                BinderName = searchRecord.ReviewsetDetails.BinderName,
                ReviewSetName = searchRecord.ReviewsetDetails.ReviewSetName,
                ReviewSetDescription = searchRecord.ReviewsetDetails.ReviewSetDescription,
                DueDate = searchRecord.ReviewsetDetails.DueDate,
                KeepDuplicatesTogether = searchRecord.ReviewsetDetails.KeepDuplicatesTogether,
                KeepFamilyTogether = searchRecord.ReviewsetDetails.KeepFamilyTogether,
                ReviewSetGroup = searchRecord.ReviewsetDetails.ReviewSetName,
                ReviewSetLogic = searchRecord.ReviewsetDetails.ReviewSetLogic,
                SearchQuery = searchRecord.ReviewsetDetails.SearchQuery,
                SplittingOption = searchRecord.ReviewsetDetails.SplittingOption,
                StartDate = searchRecord.ReviewsetDetails.StartDate,
                NumberOfDocuments = searchRecord.ReviewsetDetails.NumberOfDocuments,
                NumberOfReviewedDocs = searchRecord.ReviewsetDetails.NumberOfReviewedDocs,
                NumberOfDocumentsPerSet = searchRecord.ReviewsetDetails.NumberOfDocumentsPerSet,
                NumberOfReviewSets = searchRecord.ReviewsetDetails.NumberOfReviewSets,
                CollectionId = searchRecord.ReviewsetDetails.CollectionId,
                AssignTo = searchRecord.ReviewsetDetails.AssignTo
            };

            reviewsetRecord.ReviewSetUserList.AddRange(searchRecord.ReviewsetDetails.ReviewSetUserList);
            reviewsetRecord.DsTags.AddRange(searchRecord.ReviewsetDetails.DsTags);
            documentRecordCollection.ReviewsetDetails = reviewsetRecord;
            documentRecordCollection.TotalDocumentCount = searchRecord.TotalDocumentCount;

        }

        /// <summary> 
        /// Send Worker response to Pipe.
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void Send(DocumentRecordCollection reviewsetRecord)
        {
            try
            {
                var message = new PipeMessageEnvelope
                {
                    Body = reviewsetRecord
                };
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(reviewsetRecord.Documents.Count);
            }
            catch (Exception ex)
            {
                Tracer.Error("SearchWorker: Send: {0}", ex);
            }
        }


        /// <summary>
        /// Mock Session : Windows job doesn't 
        /// </summary>
        private void MockSession()
        {
            #region Mock

            _webContext = new MockWebOperationContext();
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            var userProp = UserBO.AuthenticateUsingUserGuid(_createdBy);
            userProp.UserGUID = _createdBy;
            var userSession = new UserSessionBEO();
            SetUserSession(userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;

            #endregion
        }


        /// <summary>
        /// Sets the usersession object using the UserBusinessEntity details
        /// </summary>
        /// <param name="userProp"></param>
        /// <param name="userSession"></param>
        private static void SetUserSession(UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = userProp.UserGUID;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
        }


        /// <summary>
        /// This method groups the results into families and send it in batches
        /// </summary>
        /// <param name="results">DocumentIdentityRecord List</param>
        /// <param name="reviewSetRecord">DocumentRecordCollection</param>
        private bool GroupDocumentsAndSend(List<DocumentIdentityRecord> results,
            DocumentRecordCollection reviewSetRecord)
        {
            var documentsCopy = new List<DocumentIdentityRecord>(results);
            var outbox = new List<DocumentIdentityRecord>();

            // Process the duplicates first
            if (reviewSetRecord.ReviewsetDetails.KeepDuplicatesTogether && reviewSetRecord.ReviewsetDetails.KeepFamilyTogether)
            {
                var duplicates = documentsCopy.GroupBy(d => d.DuplicateId);
                GroupCombinationsSend(ref results, reviewSetRecord, duplicates, ref outbox);
            }

            // Process the duplicates first
            if (reviewSetRecord.ReviewsetDetails.KeepDuplicatesTogether)
            {
                var duplicates = documentsCopy.GroupBy(d => d.DuplicateId);
                GroupSend(ref results, reviewSetRecord, duplicates, ref outbox);
            }

            // Process the families next
            if (reviewSetRecord.ReviewsetDetails.KeepFamilyTogether)
            {
                documentsCopy = new List<DocumentIdentityRecord>(results);
                var families = documentsCopy.GroupBy(d => d.FamilyId);
                GroupSend(ref results, reviewSetRecord, families, ref outbox);
            }

            // Process the orphan documents finally
            var proceesedCount = 0;

            documentsCopy = new List<DocumentIdentityRecord>(results);
            foreach (var document in documentsCopy)
            {
                outbox.Add(document);
                if (outbox.Count() >= _batchSize)
                {
                    reviewSetRecord.Documents = outbox;
                    // Send
                    Send(reviewSetRecord);
                    results = results.Except(outbox).ToList();
                    proceesedCount = outbox.Count;
                    outbox.Clear();
                }
            }


            // if any, left over documents
            if (outbox != null && outbox.Any())
            {
                reviewSetRecord.Documents = outbox;
                // Send
                Send(reviewSetRecord);
                results = results.Except(outbox).ToList();
                proceesedCount = outbox.Count;
                outbox.Clear();
            }
            return true;
        }

        /// <summary>
        /// This method processes the grouped documents and send it in message
        /// </summary>
        /// <param name="results"></param>
        /// <param name="reviewSetRecord"></param>
        /// <param name="groupDocuments"></param>
        /// <param name="outbox"></param>
        private void GroupSend(ref List<DocumentIdentityRecord> results,
            DocumentRecordCollection reviewSetRecord,
            IEnumerable<IGrouping<string, DocumentIdentityRecord>> groupDocuments,
            ref List<DocumentIdentityRecord> outbox)
        {
            //var outbox = new List<DocumentIdentityRecord>();
            foreach (var documentGroup in groupDocuments)
            {
                if (string.IsNullOrEmpty(documentGroup.Key)) continue;

                AddResultsToOutBox(outbox, documentGroup.ToList());

                if (outbox.Count() >= _batchSize)
                {
                    reviewSetRecord.Documents = outbox;
                    // Send
                    Send(reviewSetRecord);
                    results = results.Except(outbox).ToList();
                    outbox.Clear();
                }
            }

            if (outbox.Any())
            {
                results = results.Except(outbox).ToList();
            }
        }


        /// <summary>
        /// This method Groups the documents by duplicates + families together, and send it in message
        /// </summary>
        /// <param name="results"></param>
        /// <param name="reviewSetRecord"></param>
        /// <param name="groupDocuments"></param>
        /// <param name="outbox"></param>
        private void GroupCombinationsSend(ref List<DocumentIdentityRecord> results,
            DocumentRecordCollection reviewSetRecord,
            IEnumerable<IGrouping<string, DocumentIdentityRecord>> groupDocuments, ref List<DocumentIdentityRecord> outbox)
        {
            foreach (var dupGroup in groupDocuments)
            {
                if (string.IsNullOrEmpty(dupGroup.Key)) continue;

                var dupGroupDocs = dupGroup.ToList();
                var groupDocs = new List<DocumentIdentityRecord>();                
                foreach (var dupDoc in dupGroupDocs)
                {                    
                    var resultDoc = results.FirstOrDefault(r => r.DocumentId == dupDoc.DocumentId);
                    if (resultDoc != null)
                    {
                        //Assign the duplicate-id as the "Group Identifier" for the actual duplicate doc
                        resultDoc.GroupId = dupGroup.Key;

                        // Add the actual duplicate document, first.
                        groupDocs.Add(resultDoc);
                        
                        if (!string.IsNullOrEmpty(dupDoc.FamilyId))
                        {
                            // Identify all the family documents for the duplicte doc
                            var familyDocs = results.Where(r => r.FamilyId == resultDoc.FamilyId);
                            if (familyDocs != null && familyDocs.Any())
                            {
                                // Assign the duplicate-id of the actual duplicate doc as "Group Identifier", for all the family documents
                                familyDocs.SafeForEach(f => f.GroupId = dupGroup.Key);
                                //Include the family documents too, in the group
                                groupDocs.AddRange(familyDocs);
                            }
                        }
                        // Pass on the grouped documents it to outbox
                        AddResultsToOutBox(outbox, groupDocs);
                        groupDocs.Clear();
                    }
                }
                
                
                if (outbox.Count() >= _batchSize)
                {
                    reviewSetRecord.Documents = outbox;
                    // Send
                    Send(reviewSetRecord);
                    results = results.Except(outbox).ToList();
                    outbox.Clear();
                }
            }

            if (outbox.Any())
            {
                results = results.Except(outbox).ToList();
            }
        }

        /// <summary>
        ///  Add results to OutBox
        /// </summary>      
        private void AddResultsToOutBox(List<DocumentIdentityRecord> outbox, List<DocumentIdentityRecord> duplicateDocs)
        {
            foreach (var doc in duplicateDocs)
            {
                if (outbox != null && !outbox.Exists(d => d.DocumentId == doc.DocumentId))
                //To avoid same documents added to the list
                {
                    outbox.Add(doc);
                }
            }
        }

        /// <summary>
        /// Gets the configuration values.
        /// </summary>
        /// <returns></returns>
        private void GetConfigurationValues()
        {
            _batchSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("QueryBatchSize", "Reviewset"));
            Tracer.Info("Configuration values are initialized successfully.");
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
                    WorkerRoleType = Constants.ReviewsetSearchRoleId,
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
                Tracer.Info("SearchWorker : LogMessage : Exception details: {0}", exception);
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
                var message = new PipeMessageEnvelope
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
            catch (Exception exception)
            {
                Tracer.Info("SearchWorker : SendLog : Exception details: {0}", exception);
                throw;
            }
        }
    }
}