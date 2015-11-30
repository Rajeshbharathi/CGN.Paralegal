using System.Web;
using LexisNexis.Evolution.Business.ReviewerSearch;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities.OptimizedSearch;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.Jobs;
using Moq;
using LexisNexis.Evolution.ServiceImplementation;


namespace LexisNexis.Evolution.Worker
{
    public  class LawSyncSearchHelper
    {
        #region "Search"
        /// <summary>
        /// Get search result documents
        /// </summary>
        public static List<FilteredDocumentBusinessEntity> GetDocuments(List<DocumentsSelectionBEO> documentSelection, string dncFieldName, string userId)
        {
            MockUserSession(userId);
            var filteredDocuments = new List<FilteredDocumentBusinessEntity>();

            //Get default Fields part of search result.
            var outputFields = new List<Field>
                               {
                                   new Field {FieldName = EVSystemFields.DocumentKey},
                                   new Field {FieldName = dncFieldName},
                                   new Field {FieldName = EVSystemFields.LawDocumentId}
                               };
          
            //Call Search BO
            var searchResults =(RvwReviewerSearchResults) RVWSearchBO.GetSelectedDocuments(documentSelection, outputFields);

            if (searchResults == null || !searchResults.Documents.Any()) return filteredDocuments;
            filteredDocuments.AddRange(searchResults.Documents.Select(ConvertToFilteredDocumentBusinessEntity));
            return filteredDocuments;
        }


        /// <summary>
        /// Converts the search result document to FilteredDocumentBusinessEntity.
        /// </summary>
        private static FilteredDocumentBusinessEntity ConvertToFilteredDocumentBusinessEntity(ResultDocument resultDocument)
        {
            var filteredDocument = new FilteredDocumentBusinessEntity
            {
                Id = resultDocument.DocumentId.DocumentId,
                MatterId = resultDocument.DocumentId.MatterId,
                CollectionId = resultDocument.DocumentId.CollectionId,
                IsLocked = resultDocument.IsLocked,
                FamilyId = resultDocument.DocumentId.FamilyId
            };

            if (resultDocument.FieldValues == null || !resultDocument.FieldValues.Any()) return filteredDocument;

            foreach (var fieldResult in resultDocument.FieldValues.Select(ConvertDocumentFieldToFieldResult))
            {
                filteredDocument.OutPutFields.Add(fieldResult);
                if (fieldResult.DataTypeId == 3000) filteredDocument.DCN = fieldResult.Value;
            }
            return filteredDocument;
        }


        /// <summary>
        /// Converts the document field to field result.
        /// </summary>
        /// <param name="docField">The doc field.</param>
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


        /// <summary>
        /// Mock Session.
        /// </summary>
        private static void MockUserSession(string createdBy)
        {
            var webContext = new MockWebOperationContext();
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            var userProp = UserBO.GetUserByGuid(createdBy);
            var userSession = new UserSessionBEO();
            SetUserSession(createdBy, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
            var mockSearchService=new RVWReviewerSearchService(webContext.Object);
        }

        /// <summary>
        /// Sets the user session object using the UserBusinessEntity details.
        /// </summary>
        /// <param name="createdByGuid">Created by User Guid.</param>
        /// <param name="userProp">User Properties.</param>
        /// <param name="userSession">User Session.</param>
        private static void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            if (userProp.Organizations.Any())
                userSession.Organizations.AddRange(userProp.Organizations);
        }

        #endregion
    }
}
