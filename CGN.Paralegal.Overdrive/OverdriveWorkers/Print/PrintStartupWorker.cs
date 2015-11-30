//---------------------------------------------------------------------------------------------------
// <copyright file="PrintProcessingWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Madhavan Murrali</author>
//      <description>
//          This file contains the PrintStartupWorker.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//          <date value="22/4/2013">ADM – PRINTING – buddy defect fixes</date>
//          <date value="07/19/2013">BugFix 146819, 144007, 147272, 146007</date>
//          <date value="07/24/2013">BugFix # 146819 - Discrepancy in document count isssue fix </date>
//          <date value="08/30/2013">BugFix 151089</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#region Namespace

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using Moq;

#endregion

namespace LexisNexis.Evolution.Worker
{
    public class PrintStartupWorker : WorkerBase
    {
        #region Private Variables

        private BulkPrintServiceRequestBEO _mBootParameters;
        private MockWebOperationContext _webContext;
        private string _mCreatedBy = string.Empty;
        private UserBusinessEntity _mUserProp;
        private string _mCollectionId = string.Empty;
        private ReviewerSearchResults _mSearchResults; //Search results Documents
        private const int BatchSize = 100;

        #endregion

        #region Properties

        #endregion

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();

            #region Assertion

            //Pre Condition
            BootParameters.ShouldNotBe(null);
            BootParameters.ShouldBeTypeOf<string>();
            PipelineId.ShouldNotBeEmpty();

            #endregion

            // Get values from boot parameters....
            _mBootParameters = GetPrintDetailsBusinessEntity(BootParameters);
            _mCollectionId = _mBootParameters.DataSet.CollectionId;
            _mCreatedBy = _mBootParameters.RequestedBy.UserId;
            // Mock session
            MockSession();
        }


        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            try
            {
                // gets the search results.....
                GetSearchResults();
                // Send Documents to next worker
                var runningDocCount = 0;
                var documents = new List<DocumentResult>();
                foreach (var document in _mSearchResults.ResultDocuments)
                {
                    if (runningDocCount == BatchSize)
                    {
                        Send(documents);
                        Tracer.Info("Print Startup Worker- Document Count: {0}",
                            documents.Count.ToString(CultureInfo.InvariantCulture));
                        documents = new List<DocumentResult> {document};
                        runningDocCount = 1;
                    }
                    else
                    {
                        documents.Add(document);
                        runningDocCount++;
                    }
                }

                if (documents.Count <= 0)
                {
                    return false;
                }
                Tracer.Info("Print Startup Worker- Document Count: {0}",
                    documents.Count.ToString(CultureInfo.InvariantCulture));
                Send(documents);
            }
            catch (Exception ex)
            {
                ex.Swallow();
                Tracer.Error(Constants.ProductionsetGenerateError, ex);
                throw;
            }
            return true;
        }

        /// <summary>
        /// Get search results...
        /// </summary>
        public void GetSearchResults()
        {
            if (string.IsNullOrEmpty(_mCollectionId)) return;
            //Get the dataset id
           
            var dataSet = DataSetBO.GetDataSetDetailForCollectionId(_mCollectionId);
            dataSet.ShouldNotBe(null);
            var dataSetId = dataSet.FolderID.ToString(CultureInfo.InvariantCulture);
            //Construct query
            var query = _mBootParameters.BulkPrintOptions.FilterOption.FilterQuery;
            var documentFilterType = _mBootParameters.BulkPrintOptions.FilterOption.DocumentFilterType;
            var index = documentFilterType == DocumentFilterTypeBEO.SavedSearch
                ? query.LastIndexOf(Constants.IsConceptSearchingEnabled, StringComparison.Ordinal)
                : 0;
            var enableConceptSearch = documentFilterType == DocumentFilterTypeBEO.SavedSearch &&
                                      bool.Parse(
                                          query.Substring(
                                              query.LastIndexOf(Constants.IsConceptSearchingEnabled,
                                                  StringComparison.Ordinal) + 30, 5).Trim());
            var includeFamilyThread = documentFilterType == DocumentFilterTypeBEO.SavedSearch &&
                                      (query.Substring(
                                          query.LastIndexOf(Constants.IncludeFamily, StringComparison.Ordinal) + 18, 4)
                                          .Contains(Constants.True));
            query = documentFilterType == DocumentFilterTypeBEO.SavedSearch ? query.Substring(0, index - 1) : query;

            var documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    ReviewsetId = string.Empty,
                    DatasetId = Convert.ToInt32(dataSetId),
                    MatterId = Convert.ToInt32(_mBootParameters.DataSet.MatterId),
                    IsConceptSearchEnabled = enableConceptSearch
                },
                IgnoreDocumentSnippet = true
            };
            documentQueryEntity.QueryObject.QueryList.Add(new Query(query));
            documentQueryEntity.OutputFields.Add(new Field { FieldName = EVSystemFields.DcnField});
            documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.SearchResultsSortByRelevance});
            documentQueryEntity.TransactionName = "PrintStartupWorker - GetSearchResults";

            //Fetch search results
            _mSearchResults = JobSearchHandler.GetAllDocuments(documentQueryEntity, includeFamilyThread);
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<DocumentResult> documentList)
        {
            // get the collection
            var documentCollection = new PrintDocumentCollection
            {
                Documents = documentList,
                TotalDocumentCount = documentList.Count
            };
            var message = new PipeMessageEnvelope
            {
                Body = documentCollection
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(documentList.Count);
        }

        /// <summary>
        /// This method will return print details BEO out of the passed bootparamter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns>print details entity object</returns>
        private BulkPrintServiceRequestBEO GetPrintDetailsBusinessEntity(string bootParamter)
        {
            BulkPrintServiceRequestBEO toReturn = null;
            if (!string.IsNullOrEmpty(bootParamter))
            {
                //Creating a stringReader stream for the bootparameter
                var stream = new StringReader(bootParamter);

                //Ceating xmlStream for xmlserialization
                var xmlStream = new XmlSerializer(typeof (BulkPrintServiceRequestBEO));

                //Deserialization of bootparameter to get BulkPrintServiceRequestBEO
                toReturn = (BulkPrintServiceRequestBEO) xmlStream.Deserialize(stream);
            }
            return toReturn;
        }

        /// <summary>
        /// Mock Session : Windows job doesn't 
        /// </summary>
        private void MockSession()
        {
            #region Mock

            _webContext = new MockWebOperationContext();
            var userProp = new UserBusinessEntity {UserGUID = _mCreatedBy};
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            if (_mUserProp != null)
                userProp = _mUserProp;
            else
            {
                _mUserProp = UserBO.AuthenticateUsingUserGuid(_mCreatedBy);
                userProp = _mUserProp;
            }
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
        /// <param name="userProp">userProp</param>
        /// <param name="userSession">userSession</param>
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
    }
}