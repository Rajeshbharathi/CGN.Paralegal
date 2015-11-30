#region Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.WebOperationContextManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.ServiceImplementation.Document;
using LexisNexis.Evolution.Worker.Data;
using Moq;

#endregion

namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    public class TagCommentWorker : WorkerBase
    {
        private Hashtable htTagnameToTagidMapping = new Hashtable();
        private ProfileBEO _bootParameters;
        private string _createdBy;
        private string _modifiedBy;
        private IWebOperationContext _session;

        #region Overrides of WorkerBase

        protected override void BeginWork()
        {
            base.BeginWork();

            _bootParameters = Utils.SmartXmlDeserializer(BootParameters) as ProfileBEO;

            _createdBy = string.Empty;
            if (_bootParameters.CreatedBy != null)
            {
                _createdBy = _bootParameters.CreatedBy;
            }
            _modifiedBy = string.Empty;
            if (_bootParameters.ModifiedBy != null)
            {
                _modifiedBy = _bootParameters.ModifiedBy;
            }

            _session = EstablishSession(_createdBy);
        }

        #region Helpers

        private IWebOperationContext EstablishSession(string createdBy)
        {
            var userProp = new UserBusinessEntity { UserGUID = createdBy };
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            userProp = UserBO.AuthenticateUsingUserGuid(createdBy);
            var userSession = new UserSessionBEO();
            SetUserSession(createdBy, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
            return new MockWebOperationContext().Object;
        }

        private void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
        }

        #endregion

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            try
            {
                if (null != envelope && null != envelope.Body)
                {
                    var documentCollection = envelope.Body as DocumentCollection;
                    if (null != documentCollection)
                    {
                        var documentDetails = documentCollection.documents;

                        if (documentDetails == null || !documentDetails.Any())
                        {
                            Tracer.Warning("Tag Comment Worker: No documents in the document collection.");
                            return;
                        }

                        ProcessTagsAndComments(documentDetails);
                        Send(envelope);
                        IncreaseProcessedDocumentsCount(documentDetails.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        #endregion

        #region Private methods

        private void Send(PipeMessageEnvelope message)
        {
            if (OutputDataPipe != null)
            {
                OutputDataPipe.Send(message);
            }
        }

        #region Process Tags And Comments

        private void ProcessTagsAndComments(List<DocumentDetail> documentDetails)
        {
            try
            {
                documentDetails.ForEach((documentDetail) =>
                                            {
                                                var documentId = documentDetail.document.DocumentId;

                                                var dcbTags = documentDetail.DcbTags;
                                                if (null != dcbTags)
                                                {
                                                    ProcessDcbTags(documentId, dcbTags);
                                                }

                                                var dcbComments = documentDetail.DcbComments;
                                                if (null != dcbComments)
                                                {
                                                    ProcessDcbComments(dcbComments);
                                                }
                                            });
            }
            catch
            {
                Tracer.Error("Tag Comment Worker: Unable to process tags and comments for documents.");
            }

        }

        #endregion

        #region Process Dcb Tags

        private void ProcessDcbTags(string documentId, List<DcbTags> dcbTags)
        {
            try
            {
                var databaseTags = dcbTags.GetType() == typeof(DcbDatabaseTags);
                dcbTags.ForEach((dcbTag) =>
                                    {
                                        var datasetId = dcbTag.DatasetId;
                                        var matterId = dcbTag.MatterId;

                                        // For each composite tag
                                        dcbTag.compositeTagNames.ForEach(
                                            (compositeTagName) =>
                                            {
                                                if (databaseTags)
                                                {
                                                    GetWellFormedTags
                                                        (compositeTagName,
                                                         datasetId,
                                                         matterId);
                                                }
                                                else
                                                {
                                                    var documentTags =
                                                        ProcessTag
                                                            (documentId,
                                                             compositeTagName,
                                                             datasetId,
                                                             matterId);
                                                    if (null != documentTags)
                                                    {
                                                        documentTags.ForEach(
                                                            (documentTag) =>
                                                            SetDocumentTag(documentTag));
                                                    }
                                                }
                                            });
                                    });
            }
            catch
            {
                Tracer.Error("Tag Comment Worker: Unable to process dcb tags.");
            }
        }

        #endregion

        #region Process Dcb Comments

        private void ProcessDcbComments(List<DocumentCommentBEO> dcbComments)
        {
            try
            {
                dcbComments.ForEach((dcbComment) => SetDocumentComments(dcbComment));
            }
            catch
            {
                Tracer.Error("Tag Comment Worker: Unable to process dcb comments.");
            }
        }

        #endregion

        #region Process Tag

        private List<RVWDocumentTagBEO> ProcessTag(string documentId, string tag, string collectionId, long matterId)
        {
            try
            {
                var rvwTagBeos = GetWellFormedTags(tag, collectionId, matterId);
                if (null != rvwTagBeos)
                {
                    return (from tagBEO in rvwTagBeos
                            where tagBEO.Type == TagType.Tag
                            select new RVWDocumentTagBEO
                                       {
                                           DocumentId = documentId,
                                           TagId = tagBEO.Id,
                                           CollectionId = collectionId,
                                           MatterId = Convert.ToInt64(matterId),
                                           Scope = TagScope.Document,
                                           Status = true,
                                           CreatedBy = _createdBy, // TODO
                                           ModifiedBy = _modifiedBy // TODO
                                       }).ToList();
                }
            }
            catch
            {
                Tracer.Error("Tag Comment Worker: Unable to process tag: {0}, collection id: {1}, matter id: {2}", tag);
            }
            return null;
        }

        #endregion

        #region Get Well Formed Tags

        private IEnumerable<RVWTagBEO> GetWellFormedTags(string tagname, string collectionid, long matterId)
        {
            var tagBEOs = new List<RVWTagBEO>();
            var newTagBEOs = new List<RVWTagBEO>();
            var parentTagId = 0;
            if (String.IsNullOrEmpty(tagname))
            {
                return tagBEOs;
            }
            var tagnamelist = tagname.Split('»');
            var actualTagIndex = tagnamelist.Length - 1;
            for (var i = 0; i < tagnamelist.Length; i++)
            {
                var rvwTag = new RVWTagBEO
                                 {
                                     CollectionId = collectionid,
                                     MatterId = matterId,
                                     CreatedBy = _createdBy,
                                     ModifiedBy = _modifiedBy,
                                     Type = i == actualTagIndex ? TagType.Tag : TagType.TagFolder,
                                     ParentTagId = parentTagId,
                                     Scope = TagScope.Document,
                                     IsHiddenTag = false,
                                     IsPrivateTag = false,
                                     IsSystemTag = false,
                                     Name = tagnamelist[i],
                                     SearchToken = tagnamelist[i]
                                 };

                var tagBEO = IsTagAlreadyExists(tagnamelist, i);
                if (tagBEO != null)
                {
                    parentTagId = tagBEO.Id;
                    tagBEOs.Add(tagBEO);
                    continue;
                }

                var nTagId = GetDocumentTag(rvwTag.Name, rvwTag.ParentTagId.ToString(), "Document",
                                            matterId: matterId.ToString(), collectionId: collectionid);
                switch (nTagId)
                {
                    case Constants.TagNotAvailable:
                        nTagId = CreateDocumentTag(rvwTag);
                        break;
                    case Constants.InternalTagReturnErrorCode:
                        return tagBEOs;
                }

                if (Constants.InternalTagReturnErrorCode == nTagId)
                {
                    PersistInternalTagKeys(tagnamelist, newTagBEOs);
                    return tagBEOs;
                }
                rvwTag.Id = nTagId;
                parentTagId = rvwTag.Id;
                newTagBEOs.Add(rvwTag);
                tagBEOs.Add(rvwTag);
            }
            if (newTagBEOs.Count > 0)
                PersistInternalTagKeys(tagnamelist, newTagBEOs);
            return tagBEOs;
        }

        #endregion

        #region Is Tag Already Exists

        private RVWTagBEO IsTagAlreadyExists(string[] tagnamelist, int index)
        {
            var tagkey = GetInternalTagKey(tagnamelist, index);
            if (!String.IsNullOrEmpty(tagkey) && htTagnameToTagidMapping.Contains(tagkey))
                return (RVWTagBEO)htTagnameToTagidMapping[tagkey];
            return null;
        }

        #endregion

        #region Persist Internal Tag Keys

        private void PersistInternalTagKeys(string[] tagnamelist, List<RVWTagBEO> rvwTagList)
        {
            for (var i = 0; i < tagnamelist.Length; i++)
            {
                var tagkey = GetInternalTagKey(tagnamelist, i);
                if (!String.IsNullOrEmpty(tagkey) && (i < rvwTagList.Count - 1))
                    htTagnameToTagidMapping[tagkey] = rvwTagList[i];
            }
        }

        #endregion

        #region Get Internal Tag Key

        private string GetInternalTagKey(string[] tagnamelist, int index)
        {
            var tagkey = string.Empty;
            TagType tagType;
            if (index == 0)
            {
                tagType = tagnamelist.Length > 1 ? TagType.TagFolder : TagType.Tag;
                return String.Format("{0}-{1}", tagnamelist[0], tagType);
            }
            for (var i = 0; i < tagnamelist.Length; i++)
            {
                tagkey = String.Concat(tagkey, tagnamelist[i]);
                tagType = i == (tagnamelist.Length - 1) ? TagType.Tag : TagType.TagFolder;
                tagkey = String.Format("{0}-{1}", tagkey, tagType);
                if (index != i)
                    tagkey = String.Concat(tagkey, "$$");
            }
            return tagkey;
        }

        #endregion

        #region Get Document Tag

        private int GetDocumentTag(string tagName, string parentTagId, string scope, string matterId,
                                   string collectionId)
        {
            try
            {
                var tagService = new RVWTagService(_session);
                var tagId = tagService.GetTagId(tagName, collectionId, matterId, parentTagId, scope);
                return tagId;
            }
            catch (Exception)
            {
                //LogJobException(ErrorCodes.ErrorDcbImportGetEVDocumentTagId, String.Format(Constants.ErrorGetDocumentTagId, tagName),
                //                                                                                                    false,
                //                                                                                                    exp.Message);

                Tracer.Error("Unable to get document tag.");
                return Constants.InternalTagReturnErrorCode;
            }

        }

        #endregion

        #region Create Document Tag

        private int CreateDocumentTag(RVWTagBEO tag)
        {
            try
            {
                var tagService = new RVWTagService(_session);
                var tagId = tagService.AddTag(tag);
                return tagId;
            }
            catch (Exception)
            {
                //LogJobException(ErrorCodes.ErrorDcbImportCreateEVDocumentTag, String.Format(Constants.ErrorCreateDocumentTag, tag.Name),
                //                                                                                                    false,
                //                                                                                                    exp.Message);
                Tracer.Error("Unable to create document tag.");
                return Constants.InternalTagReturnErrorCode;
            }
        }

        #endregion

        #region Set Document Tag

        private int SetDocumentTag(RVWDocumentTagBEO documentTag)
        {
            try
            {
                var documentService = new DocumentService(_session);
                var documentsTagged = documentService.AssignTag(documentTag);
                return documentsTagged;
            }
            catch (Exception)
            {
                //LogJobException(ErrorCodes.ErrorDcbImportSetEVDocumentTag, String.Format(Constants.ErrorSetDocumentTag, documentTag.TagId, currentDocumentDetails.DocumentId),
                //                                                                                                    false,
                //                                                                                                    exp.Message);
                Tracer.Error("Tag Comment Worker: Unable to set tag: {0} to the document id: {1}.", documentTag.TagId, documentTag.DocumentId);
                return Constants.InternalTagReturnErrorCode;
            }
        }

        #endregion

        #region Set Document Comments

        private bool SetDocumentComments(DocumentCommentBEO documentComment)
        {
            try
            {
                var collectionId = documentComment.CollectionId.ToString();
                var documentId = documentComment.DocumentId;
                var matterId = documentComment.MatterId.ToString();
                var documentService = new DocumentService(_session);
                return documentService.AddComment(documentComment, true);
            }
            catch (Exception)
            {
                //LogJobException(ErrorCodes.ErrorDcbImportCreateDocumentComments, String.Format(Constants.ErrorCreateDocumentComments, currentDocumentId),
                //                                                                                                    false,
                //                                                                                                    exp.Message);
                Tracer.Error("Tag Comment Worker: Unable to set the comment to document id: {0}",
                             documentComment.DocumentId);
                return false;
            }
        }

        #endregion

        #region Constants

        internal static class Constants
        {
            internal const int InternalTagReturnErrorCode = -2000;
            internal const int TagNotAvailable = 0;
        }

        #endregion

        #endregion
    }
}
