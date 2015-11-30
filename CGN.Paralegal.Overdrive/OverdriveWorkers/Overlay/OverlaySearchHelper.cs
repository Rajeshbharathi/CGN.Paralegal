# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="OverlaySearchHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Srini</author>
//      <description>
//          This file contains all overlay search related helper methods
//      </description>
//      <changelog>
//          <date value="08/17/2012">Fix for Bug 101537</date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using Moq;

namespace LexisNexis.Evolution.Worker
{
    public class OverlaySearchHelper
    {
        public ReviewerSearchResults Search(string searchQuery, string collectionId, long datasetId, long matterId,
            string matterDbName, string createdBy, UserBusinessEntity userInfo, List<Field> outputFields = null)
        {
            #region User-Session

            var userProp = userInfo;
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            var userSession = new UserSessionBEO();
            SetUserSession(createdBy, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;

            #endregion

            var queryEntity = new DocumentQueryEntity
            {
                DocumentStartIndex = 0,
                DocumentCount = 1000,
                QueryObject = new SearchQueryEntity()
            };
            //Default page 0
            //Default maximum 1000 document can fetch from Search Sub System
            var query = new Query {SearchQuery = searchQuery, Precedence = 1};
            queryEntity.QueryObject.QueryList.Add(query);
            queryEntity.QueryObject.MatterId = matterId;
            queryEntity.QueryObject.DatasetId = datasetId;
            queryEntity.QueryObject.IsConceptSearchEnabled = false;
            queryEntity.TransactionName = "OverlaySearchHelper - Search";
            //Explicitly set - to not to return snippet from search engine..Will be scrapped as part of search engine replacement
            queryEntity.IgnoreDocumentSnippet = true;

            if (outputFields != null && outputFields.Any())
            {
                queryEntity.OutputFields.AddRange(outputFields);
            }

            #region BO Call

            var results = (RvwReviewerSearchResults) SearchBo.Search(queryEntity);
            return ConvertRvwReviewerSearchResultsToReviewerSearchResults(results);

            #endregion
        }

        /// <summary>
        /// Sets the usersession object using the UserBusinessEntity details
        /// </summary>
        /// <param name="createdByGuid"></param>
        /// <param name="userProp"></param>
        /// <param name="userSession"></param>
        private void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            // userSession.EntityTypeId = userProp.AuthorizedBEO.EntityTypeId;
        }

        #region Search Convert Result

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rvwReviewerSearchResults"></param>
        /// <returns></returns>
        private static ReviewerSearchResults ConvertRvwReviewerSearchResultsToReviewerSearchResults(
            RvwReviewerSearchResults rvwReviewerSearchResults)
        {
            var reviewerSearchResults = new ReviewerSearchResults
            {
                TotalRecordCount = rvwReviewerSearchResults.Documents.Count,
                TotalHitCount = rvwReviewerSearchResults.TotalHitResultCount
            };

            foreach (var resultDocument in rvwReviewerSearchResults.Documents)
            {
                reviewerSearchResults.ResultDocuments.Add(ConvertResultDocumentToDocumentResult(resultDocument));
            }
            return reviewerSearchResults;
        }

        /// <summary>
        /// Converts ResultDocument object into Document result object
        /// </summary>
        /// <param name="rvwReviewerSearchResults">ResultDocument</param>
        /// <returns>DocumentResult</returns>
        private static DocumentResult ConvertResultDocumentToDocumentResult(ResultDocument resultDocument)
        {
            var docResult = new DocumentResult
            {
                Id = resultDocument.DocumentId.Id,
                DocumentID = resultDocument.DocumentId.DocumentId,
                IsLocked = resultDocument.IsLocked,
                RedactableDocumentSetId = resultDocument.RedactableDocumentSetId
            };

            foreach (var docField in resultDocument.FieldValues)
            {
                docResult.Fields.Add(ConvertDocumentFieldToFieldResult(docField));
            }

            //Add parent Id as a field for overlay
            var parentField = new FieldResult();
            parentField.Name = EVSystemFields.ParentDocId;
            parentField.Value = string.IsNullOrWhiteSpace(resultDocument.DocumentId.Parent.DocumentId)
                ? string.Empty
                : resultDocument.DocumentId.Parent.DocumentId;
            docResult.Fields.Add(parentField);

            //Add family Id as a field for overlay
            var familyField = new FieldResult();
            familyField.Name = EVSystemFields.FamilyId;
            familyField.Value = string.IsNullOrWhiteSpace(resultDocument.DocumentId.FamilyId)
                ? string.Empty
                : resultDocument.DocumentId.FamilyId;
            docResult.Fields.Add(familyField);

            return docResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rvwReviewerSearchResults"></param>
        /// <returns></returns>
        private static FieldResult ConvertDocumentFieldToFieldResult(DocumentField docField)
        {
            var newField = new FieldResult
            {
                Name = docField.FieldName,
                Value = docField.Value,
                ID = Convert.ToInt32(docField.Id),
                DataTypeId = Convert.ToInt32(docField.Type)
            };
            return newField;
        }

        #endregion
    }
}