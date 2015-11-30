#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="VaultTaggerWorker" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>babugx</author>
//      <description>
//          This worker file owns the responsibility to write the tag information in the Vault database against the given documents
//      </description>
//      <changelog>
//          <date value="07-Nov-2013">Created</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespace

using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.SessionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

#endregion

namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    public class VaultTaggerWorker : WorkerBase
    {
        #region Private Variables

        private string _createdBy;
        private MockWebOperationContext _webContext;
        private IDocumentVaultManager _documentVaultManager;
        private IBinderVaultManager _binderVaultManager;

        #endregion

        /// <summary>
        /// Gets the DocumentVaultManager instance
        /// </summary>
        public IDocumentVaultManager DocumentVaultManager
        {
            get { return _documentVaultManager ?? (_documentVaultManager = new DocumentVaultManager()); }
            set { _documentVaultManager = value; }
        }

        /// <summary>
        /// Gets the TagsVaultManager instance
        /// </summary>
        public IBinderVaultManager BinderVaultManagerInstance
        {
            get { return _binderVaultManager ?? (_binderVaultManager = new BinderVaultManager()); }
            set { _binderVaultManager = value; }
        }

        /// <summary>
        /// overrides base ProcessMessage method
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var reviewsetRecord = (DocumentRecordCollection) envelope.Body;
            reviewsetRecord.ShouldNotBe(null);
            reviewsetRecord.ReviewsetDetails.CreatedBy.ShouldNotBeEmpty();
            reviewsetRecord.Documents.ShouldNotBe(null);
            _createdBy = reviewsetRecord.ReviewsetDetails.CreatedBy;

            if (EVHttpContext.CurrentContext == null)
            {
                // Moq the session
                MockSession();
            }

            try
            {
                //stores converted document list
                var tempList = ConvertDocumentRecordtoReviewsetDocument(reviewsetRecord.Documents,
                    reviewsetRecord.ReviewsetDetails);

                TagDocumentsNotReviewed(reviewsetRecord, tempList);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, string.Format("Error in VaultTaggerWorker - Exception: {0}", ex.ToUserString()),
                    reviewsetRecord.ReviewsetDetails.CreatedBy, reviewsetRecord.ReviewsetDetails.ReviewSetName);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts document record to review set document BEO
        /// </summary>
        /// <param name="documentList"></param>
        /// <param name="createdBy"> </param>
        /// <returns></returns>
        private List<ReviewsetDocumentBEO> ConvertDocumentRecordtoReviewsetDocument(
            List<DocumentIdentityRecord> documentList, ReviewsetRecord reviewset)
        {
            var convertedDocuments = new List<ReviewsetDocumentBEO>();
            foreach (var document in documentList)
            {
                convertedDocuments.Add(new ReviewsetDocumentBEO
                {
                    DocumentId = document.DocumentId,
                    CollectionViewId = document.ReviewsetId,
                    BinderId = reviewset.BinderId,
                    DCN = document.DocumentControlNumber,
                    CreatedBy = reviewset.CreatedBy
                }
                    );
            }
            return convertedDocuments;
        }


        /// <summary>
        /// Tag the documents with not reviewed tag
        /// </summary>
        /// <param name="reviewsetRecord">Reviewset Record</param>
        /// <param name="tempList">List of documents added to reviewset</param>
        private void TagDocumentsNotReviewed(DocumentRecordCollection reviewsetRecord,
            List<ReviewsetDocumentBEO> tempList)
        {
            reviewsetRecord.ShouldNotBe(null);
            tempList.ShouldNotBe(null);
            var matterId = reviewsetRecord.ReviewsetDetails.MatterId.ToString(CultureInfo.InvariantCulture);
            var collectionId = reviewsetRecord.ReviewsetDetails.CollectionId;

            //var currentUser = EVSessionManager.Get<UserSessionBEO>(Constants.UserSessionInfo);
            var documentTagObjects = new List<DocumentTagBEO>();

            var _binderDetail = BinderVaultManagerInstance.GetBinderSpecificDetails(matterId,
                reviewsetRecord.ReviewsetDetails.BinderId);
            _binderDetail.ShouldNotBe(null);
            _binderDetail.NotReviewedTagId.ShouldBeGreaterThan(0);
            _binderDetail.ReviewedTagId.ShouldBeGreaterThan(0);
            reviewsetRecord.ReviewsetDetails.NotReviewedTagId = _binderDetail.NotReviewedTagId;
            reviewsetRecord.ReviewsetDetails.ReviewedTagId = _binderDetail.ReviewedTagId;


            var docsInfo = ConvertToBulkDocumentInfoBEO(tempList);

            //dictionary to hold list of documents to update
            var documentList = docsInfo.ToDictionary(a => a.DocumentId, a => a);

            //get Effective Tags for the give tag
            var effectiveTagList =
                BulkTagBO.GetEfectiveTags(matterId, new Guid(collectionId), _binderDetail.BinderId,
                    _binderDetail.NotReviewedTagId, Constants.One).ToList();

            var currentState = new List<DocumentTagBEO>();
            var newState = new List<DocumentTagBEO>();

            //get the list of tags with current status for the documents to be tagged
            currentState.AddRange(DocumentVaultManager.GetDocumentTags(matterId, new Guid(collectionId),
                documentList.Keys.ToList()));
            //for every tag from the effective list of tags to be updated
            foreach (var tag in effectiveTagList)
            {
                newState.AddRange(
                    documentList.Values.Select(
                        x =>
                            new DocumentTagBEO
                            {
                                TagId = tag.TagId,
                                Status = tag.TagState,
                                DocumentId = x.DocumentId,
                                DCN = x.DCN,
                                TagTypeSpecifier = tag.type,
                                TagName = tag.TagName
                            }));
            }

            //get all the documents that is not part of current documents list and update to vault
            var vaultChanges = newState.Except(currentState, new DocumentTagComparer()).Distinct().ToList();
            //Remove the entries that are already untagged but are marked for untagging
            vaultChanges.RemoveAll(document => document.Status == Constants.Untagged &&
                                               currentState.Exists(
                                                   y =>
                                                       y.TagId == document.TagId &&
                                                       String.Compare(y.DocumentId, document.DocumentId,
                                                           StringComparison.OrdinalIgnoreCase) == 0) == false);

            /* The following statement will take the untagged documents in the new state and 
             * make it as list of tagged documents so that except operator in the next statement 
             * remove all the tagged documents in the current state that have 
             * been untagged now.*/
            var newstateUntaggedDocuments = newState.Where(t => t.Status != Constants.Tagged).Select(t =>
                new DocumentTagBEO {TagId = t.TagId, DocumentId = t.DocumentId, Status = Constants.Tagged}).ToList();

            //Determine Tag changes to update in search index..This has to be supplied for IndexTaggerWorker to take care in search-engine
            var indexChanges = reviewsetRecord.DocumentTags = currentState.FindAll(currentStateOfDocument =>
                (newstateUntaggedDocuments.Find(newStateOfDocument =>
                    currentStateOfDocument.TagId == newStateOfDocument.TagId &&
                    currentStateOfDocument.DocumentId == newStateOfDocument.DocumentId
                    && currentStateOfDocument.Status == newStateOfDocument.Status) == null)).Union(newState).
                Distinct(new DocumentTagBEO()).ToList();

            DocumentVaultManager.UpdateDocumentTags(matterId, new Guid(collectionId), _binderDetail.BinderId,
                vaultChanges, indexChanges, _createdBy);
            Send(reviewsetRecord);
        }


        /// <summary>
        /// Convert ReviewsetDocumentBEOs to BulkDocumentInfoBEOs
        /// </summary>
        /// <param name="reviewsetDocuments">List of ReviewsetDocumentBEOs</param>
        /// <returns>List of BulkDocumentInfoBEOs</returns>
        private List<BulkDocumentInfoBEO> ConvertToBulkDocumentInfoBEO(List<ReviewsetDocumentBEO> reviewsetDocuments)
        {
            reviewsetDocuments.ShouldNotBe(null);
            var currentUser = EVSessionManager.Get<UserSessionBEO>(Constants.UserSessionInfo);
            var bulkTagDocuments = new List<BulkDocumentInfoBEO>();
            reviewsetDocuments.ForEach(document => bulkTagDocuments.Add(new BulkDocumentInfoBEO
            {
                CreatedBy = currentUser != null ? currentUser.UserGUID : string.Empty,
                DocumentId = document.DocumentId,
                DCN = document.DCN,
                FamilyId = document.FamilyId,
                FromOriginalQuery = true
            }));
            return bulkTagDocuments;
        }

        private void Send(DocumentRecordCollection reviewsetRecord)
        {
            var message = new PipeMessageEnvelope
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
                    WorkerRoleType = Constants.ReviewsetVaultTaggerRoleID,
                    WorkerInstanceId = WorkerId,
                    IsMessage = false,
                    Success = status,
                    CreatedBy = createdBy,
                    LogInfo = new ReviewsetLogInfo {Information = information, ReviewsetName = reviewsetName}
                };
                // TaskId
                log.Add(parserLog);
                SendLog(log);
            }
            catch (Exception exception)
            {
                Tracer.Info("VaultTaggerWorker : LogMessage : Exception details: {0}", exception);
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
                Tracer.Info("VaultTaggerWorker : SendLog : Exception details: {0}", exception);
                throw;
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
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
        }

        #endregion
    }
}