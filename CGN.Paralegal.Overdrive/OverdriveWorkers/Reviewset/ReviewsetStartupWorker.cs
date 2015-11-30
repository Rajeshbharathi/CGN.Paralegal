#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetStartupWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Giri</author>
//      <description>
//          This file does the start-up activity for reviewset creation
//      </description>
//      <changelog>
//          <date value="28-Jan-2012">created</date>
//          <date value="03-Feb-2012">Modified LogMessage</date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="10/18/2013">Bug # 156997 - Split reviewset fix to avert family documents inclusion : babugx</date>
//          <date value="02/13/2015">CNEV 4.0 - Search Engine Replacement - ESLite Integration : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Search;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.ServiceContracts;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Business.Binder;
using LexisNexis.Evolution.BusinessEntities.Binder;
#endregion

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// This class represents the startup worker
    /// </summary>
    public class ReviewsetStartupWorker : WorkerBase
    {

        #region Private Variables
        private CreateReviewSetJobBEO _bootObject;
        private DatasetBEO _datasetEntity;
        private BinderBEO _binderEntity;
        private long _totalDocumentCount;
        private DocumentQueryEntity _docQueryEntity;
        private ReviewsetRecord _reviewSetRecord;
        private MockWebOperationContext _webContext;
        #endregion

        #region Instance variables
        private static IRvwReviewerSearchService _rvwSearchServiceInstance = null;
        /// <summary>
        /// Gets the RVW reviewer search service instance.
        /// </summary>
        private static IRvwReviewerSearchService ReviewerSearchInstance
        {
            get { return _rvwSearchServiceInstance ?? (_rvwSearchServiceInstance = new RVWReviewerSearchService()); }
        }
        #endregion

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();

            #region Assert conditions
            BootParameters.ShouldNotBe(null);
            BootParameters.ShouldBeTypeOf<string>();
            PipelineId.ShouldNotBeEmpty();
            #endregion

            try
            {
                DoBeginWork(BootParameters);
            }
            catch (ApplicationException apEx)
            {
                LogMessage(false, apEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                if (_reviewSetRecord != null && _reviewSetRecord.ReviewSetName != null)
                    LogMessage(false, string.Format("Reviewset {0} creation failed. Exception: {1}", _reviewSetRecord.ReviewSetName, ex.Message));
                throw;
            }
        }


        /// <summary>
        /// Absorb the boot parameters, deserialize and pass on the messages to the Search Worker
        /// </summary>
        public void DoBeginWork(string bootParameter)
        {
            bootParameter.ShouldNotBeEmpty();
            // Deserialize and determine the boot object
            _bootObject = GetBootObject(bootParameter);

            // Assert condition to check for jobscheduled by
            _bootObject.JobScheduleCreatedBy.ShouldNotBeEmpty();
            _bootObject.BinderFolderId.ShouldNotBe(0);

            // Get Dataset Details to know about the Collection id and the Matter ID details
            _datasetEntity = DataSetBO.GetDataSetDetailForDataSetId(_bootObject.datasetId);
            //Assert condition to check for dataset details
            _datasetEntity.ShouldNotBe(null);

            _binderEntity = BinderBO.GetBinderDetails(_bootObject.BinderFolderId.ToString());
            _binderEntity.ShouldNotBe(null);

            _reviewSetRecord = ConvertToReviewSetRecord(_bootObject);

            // Construct the document query entity to determine the total documents
            _docQueryEntity = GetQueryEntity(_bootObject, _datasetEntity, 0, 1, null);

            // Mock the user session
            MockSession();

            _docQueryEntity.TransactionName = _docQueryEntity.QueryObject.TransactionName = "ReviewsetStartupWorker - DoBeginWork (GetCount)";

            var reviewsetLogic = _reviewSetRecord.ReviewSetLogic.ToLower();
            if (reviewsetLogic == "all" || reviewsetLogic == "tag")
            {
                var searchQuery = !string.IsNullOrEmpty(_bootObject.SearchQuery)? _bootObject.SearchQuery.Replace("\"", ""): string.Empty;
                _totalDocumentCount = DocumentBO.GetDocumentCountForCreateReviewsetJob(_datasetEntity.Matter.FolderID, _datasetEntity.CollectionId,
                    reviewsetLogic, searchQuery);
            }
            else
            {
                // Retrieve the total documents qualified
                _totalDocumentCount = ReviewerSearchInstance.GetDocumentCount(_docQueryEntity.QueryObject);
            }

            Tracer.Info("Reviewset Startup Worker : {0} matching documents determined for the requested query", _totalDocumentCount);
            if (_totalDocumentCount < 1)
            {
                var message = String.Format("Search server does not return any documents for the reviewset '{0}'", _reviewSetRecord.ReviewSetName);
                throw new ApplicationException(message);
            }

            LogMessage(true, string.Format("{0} documents are qualified", _totalDocumentCount));

            // Construct the document query entity to write the resultant documents in xml file
            var outputFields = new List<Field>();
            outputFields.AddRange(new List<Field>()
            {
                    new Field { FieldName = EVSystemFields.DcnField},
                    new Field { FieldName = EVSystemFields.FamilyId},
                    new Field { FieldName = EVSystemFields.DuplicateId}
            });
            _docQueryEntity = GetQueryEntity(_bootObject, _datasetEntity, 0, Convert.ToInt32(_totalDocumentCount), outputFields);
        }


        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            var searchRecord = new ReviewsetSearchRecord();
            try
            {
                ////Send the document to the pipe to process once it reach the batchsize
                searchRecord.ReviewsetDetails = _reviewSetRecord;
                _docQueryEntity.ShouldNotBe(null);

                searchRecord.QueryEntity = _docQueryEntity;
                searchRecord.TotalDocumentCount = (int)_totalDocumentCount;
                Send(searchRecord);
                LogMessage(true, "Reviewset: Message successfully sent to acquire all the qualified documents");
            }
            catch (Exception ex)
            {
                LogMessage(false, "Reviewset: Failed to determine the qualifying documents");
                ex.Trace();
                return false;
            }
            return true;
        }



        /// <summary>
        /// Sends the specified search record.
        /// </summary>
        /// <param name="searchRecord">The search record.</param>
        private void Send(ReviewsetSearchRecord searchRecord)
        {
            try
            {
                var message = new PipeMessageEnvelope()
                {
                    Body = searchRecord
                };
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(searchRecord.TotalDocumentCount);
            }
            catch (Exception ex)
            {
                Tracer.Error("ReviewsetStartupWorker: Send: {0}", ex);
            }
        }

        /// <summary>
        /// Mock Session : Windows job doesn't 
        /// </summary>
        private void MockSession()
        {
            #region Mock
            _webContext = new MockWebOperationContext();
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userProp = UserBO.GetUserUsingGuid(_bootObject.JobScheduleCreatedBy);
            var userSession = new UserSessionBEO();
            SetUserSession(_bootObject.JobScheduleCreatedBy, userProp, userSession);
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
        /// <param name="createdUserGuid"></param>
        /// <param name="userProp"></param>
        /// <param name="userSession"></param>
        private static void SetUserSession(string createdUserGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdUserGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
        }

        /// <summary>
        /// This method deserializes and determine the Xml CreateReviewSetJobBEO object
        /// </summary>
        /// <param name="bootParameter"></param>
        private CreateReviewSetJobBEO GetBootObject(string bootParameter)
        {
            CreateReviewSetJobBEO bootObject = null;
            if (!string.IsNullOrEmpty(bootParameter))
            {
                //Creating a stringReader stream for the bootparameter
                StringReader stream = new StringReader(bootParameter);

                //Ceating xmlStream for xmlserialization
                XmlSerializer xmlStream = new XmlSerializer(typeof(CreateReviewSetJobBEO));

                //Deserialization of bootparameter to get ProductionDetailsBEO
                bootObject = (CreateReviewSetJobBEO)xmlStream.Deserialize(stream);
            }
            return bootObject;
        }


        /// <summary>
        /// Constructs and returns the document search query entity
        /// </summary>
        /// <param name="jobParameters">The job parameters.</param>
        /// <param name="datasetEntity">The dataset entity.</param>
        /// <returns></returns>
        private DocumentQueryEntity GetQueryEntity(CreateReviewSetJobBEO jobParameters,
            DatasetBEO datasetEntity, int startIndex, int documentCount, List<Field> outputFields)
        {
            DocumentQueryEntity documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    MatterId = datasetEntity.Matter.FolderID,
                    IsConceptSearchEnabled = jobParameters.DocumentSelectionContext.SearchContext.IsConceptSearchEnabled,
                    DatasetId = datasetEntity.FolderID
                }
            };

            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.DocumentStartIndex = startIndex;
            documentQueryEntity.DocumentCount = documentCount;
            documentQueryEntity.SortFields.Add(new Sort { SortBy = Constants.Relevance });

            if (outputFields != null && outputFields.Any())
            {
                documentQueryEntity.OutputFields.AddRange(outputFields);
            }

            var tmpQuery = string.Empty;
            var selectionQuery = string.Empty;

            if (!string.IsNullOrEmpty(jobParameters.DocumentSelectionContext.SearchContext.Query))
            {
                tmpQuery = string.Format("({0} )", jobParameters.DocumentSelectionContext.SearchContext.Query);
            }
            else
            {
                tmpQuery = string.Format("(NOT ({0}:\"{1}\"))", EVSystemFields.BinderId.ToLowerInvariant(), _binderEntity.BinderId);
            }

            switch (jobParameters.DocumentSelectionContext.GenerateDocumentMode)
            {
                case DocumentSelectMode.UseSelectedDocuments:
                    {
                        jobParameters.DocumentSelectionContext.SelectedDocuments.ForEach(d =>
                            selectionQuery += string.Format("{0}:\"{1}\" OR ", EVSystemFields.DocumentKey, d));
                        if (!string.IsNullOrEmpty(selectionQuery))
                        {
                            selectionQuery = selectionQuery.Substring(0, selectionQuery.LastIndexOf(" OR "));
                            tmpQuery = string.Format("({0} AND ({1}))", tmpQuery, selectionQuery);
                        }

                        break;
                    }
                case DocumentSelectMode.QueryAndExclude:
                    {
                        jobParameters.DocumentSelectionContext.DocumentsToExclude.ForEach(d =>
                            selectionQuery += string.Format("(NOT {0}:\"{1}\") AND ", EVSystemFields.DocumentKey, d));
                        if (!string.IsNullOrEmpty(selectionQuery))
                        {
                            selectionQuery = selectionQuery.Substring(0, selectionQuery.LastIndexOf(" AND "));
                            tmpQuery = string.Format("({0} AND ({1}))", tmpQuery, selectionQuery);
                        }
                        break;
                    }
            }

            documentQueryEntity.QueryObject.QueryList.Clear();
            documentQueryEntity.QueryObject.QueryList.Add(new Query { SearchQuery = tmpQuery });
            return documentQueryEntity;
        }

        /// <summary>
        /// This method converts the CreateReviewSetJobBEO to ReviewsetRecord
        /// </summary>
        /// <param name="reviewSetJobBEO">CreateReviewSetJobBEO/param>
        /// <returns>ReviewsetRecord</returns>
        private ReviewsetRecord ConvertToReviewSetRecord(CreateReviewSetJobBEO reviewSetJobBEO)
        {
            ReviewsetRecord rSetRecord = new ReviewsetRecord
            {
                Activity = reviewSetJobBEO.Activity,
                DatasetId = reviewSetJobBEO.DatasetId,
                DueDate = reviewSetJobBEO.DueDate,
                KeepDuplicatesTogether = reviewSetJobBEO.KeepDuplicates,
                KeepFamilyTogether = reviewSetJobBEO.KeepFamily,
                NumberOfDocuments = reviewSetJobBEO.NumberOfDocuments,
                NumberOfDocumentsPerSet = reviewSetJobBEO.NumberOfDocumentsPerSet,
                NumberOfReviewedDocs = reviewSetJobBEO.NumberOfReviewedDocs,
                NumberOfReviewSets = reviewSetJobBEO.NumberOfReviewSets,
                ReviewSetDescription = reviewSetJobBEO.ReviewSetDescription,
                ReviewSetGroup = reviewSetJobBEO.ReviewSetGroup,
                ReviewSetId = reviewSetJobBEO.ReviewSetId,
                BinderFolderId = reviewSetJobBEO.BinderFolderId,
                BinderId = _binderEntity.BinderId,
                BinderName = _binderEntity.BinderName,
                ReviewSetLogic = reviewSetJobBEO.ReviewSetLogic,
                ReviewSetName = reviewSetJobBEO.ReviewSetName,
                SplittingOption = reviewSetJobBEO.SplittingOption,
                StartDate = reviewSetJobBEO.StartDate,
                StatusId = reviewSetJobBEO.StatusId,
                CreatedBy = reviewSetJobBEO.JobScheduleCreatedBy,
                CollectionId = reviewSetJobBEO.CollectionId
            };
            rSetRecord.DsTags.AddRange(reviewSetJobBEO.DsTags);
            return rSetRecord;
        }

        private void LogMessage(bool status, string information)
        {
            var parserLog = new JobWorkerLog<ReviewsetLogInfo>
            {
                JobRunId =
                    (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = 0,
                WorkerRoleType = Constants.ReviewsetStartupRoleID,
                WorkerInstanceId = WorkerId,
                IsMessage = false,
                Success = status,
                CreatedBy = _bootObject.JobScheduleCreatedBy,
                LogInfo = new ReviewsetLogInfo { Information = information }
            };

            var log = new List<JobWorkerLog<ReviewsetLogInfo>>();
            log.Add(parserLog);
            SendLog(log);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ReviewsetLogInfo>> log)
        {
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            if (null != LogPipe)
            {
                LogPipe.Send(message);
            }
        }
    }
}
