#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetVaultUpdateWorker" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Sivasankari Partheeban</author>
//      <description>
//          This file has methods to read data from Review set Logic worker and write document review set relation in Vault DB.
//          Also sends review set, document relation to Search sub System
//      </description>
//      <changelog>
//          <date value="01-Feb-2012">Created</date>
//          <date value="03-Feb-2012">Modified LogMessage</date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//	        <date value="03/01/2012">Unit test fixes for bug 86129</date>
//          <date value="03/09/2011">Bug Fix 97742</date>
//	        <date value="04/05/2012">Bug Fix # 98763</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespace

using LexisNexis.Evolution.Business.ReviewSet;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Web;

#endregion

namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    public class ReviewsetVaultUpdateWorker : WorkerBase
    {
        #region Private Variables

        private int _totalDocumentCount;
        private MockWebOperationContext _webContext;
        private string _createdBy;
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
            try 
            {
                var reviewsetRecord = (DocumentRecordCollection) envelope.Body;
                WriteDocumentstoVault(reviewsetRecord);
                Send(reviewsetRecord);            
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        #region Helper Methods

        /// <summary>
        /// writes the list of documents into Vault
        /// </summary>
        /// <param name="reviewsetRecord"></param>
        private void WriteDocumentstoVault(DocumentRecordCollection reviewsetRecord)
        {
            reviewsetRecord.ShouldNotBe(null);
            var tempList = new List<ReviewsetDocumentBEO>();
            reviewsetRecord.ReviewsetDetails.CreatedBy.ShouldNotBeEmpty();
            reviewsetRecord.Documents.ShouldNotBe(null);
            _createdBy = reviewsetRecord.ReviewsetDetails.CreatedBy;
            _totalDocumentCount = reviewsetRecord.TotalDocumentCount;

            //stores converted document list
            tempList = ConvertDocumentRecordtoReviewsetDocument(reviewsetRecord.Documents,
                reviewsetRecord.ReviewsetDetails);

            if (EVHttpContext.CurrentContext == null)
            {
                // Moq the session
                MockSession();
            }

            ReviewSetBO.AddDocumentsToReviewSetForOverDrive(reviewsetRecord.ReviewsetDetails.MatterId.ToString(),
                reviewsetRecord.ReviewsetDetails.CollectionId, tempList);
        }

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
                    // Assign document numeric identifier
                    Id = document.Id,
                    DocumentId = document.DocumentId,
                    CollectionViewId = document.ReviewsetId,
                    BinderId = reviewset.BinderId,
                    TotalRecordCount = _totalDocumentCount,
                    FamilyId = document.FamilyId,
                    DCN = document.DocumentControlNumber,
                    CreatedBy = reviewset.CreatedBy
                }
                );
            }
            return convertedDocuments;
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
                    WorkerRoleType = Constants.ReviewsetVaultUpdateRoleID,
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
                Tracer.Info("ReviewsetVaultUpdateWorker : LogMessage : Exception details: {0}", exception);
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
                Tracer.Info("ReviewsetVaultUpdateWorker : SendLog : Exception details: {0}", exception);
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
