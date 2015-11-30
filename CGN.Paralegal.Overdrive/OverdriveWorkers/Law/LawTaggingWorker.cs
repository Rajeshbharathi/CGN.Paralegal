using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.TraceServices;
using Moq;
using LexisNexis.Evolution.Overdrive;

namespace LexisNexis.Evolution.Worker
{   

    /// <summary>
    /// Tagging worker uses for tagging the documnets batch wise 
    /// </summary>
    public class LawTaggingWorker : WorkerBase
    {
        private LawImportBEO _jobParams;
        private const string Status = "1";
        private const string TagSuccesMessage = "Tag document successful";
        private const string TagFailureMessage = "Tag document failed";
        private const string TaggingWorkerRoleType = "be9379e0-731a-4fcf-8728-b87d3bd24525";


        /// <summary>
        /// Processes the work item. pushes give document files for tagging
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            Send(message);
            var documentCollection = message.Body as DocumentCollection;
            documentCollection.ShouldNotBe(null);
            documentCollection.documents.ShouldNotBe(null);
            documentCollection.documents.ShouldNotBeEmpty();
            var nativeSet =
                    documentCollection.documents.FindAll(
                        d => (d.docType == DocumentsetType.NativeSet));
            
            try
            {
                IDictionary<int, List<BulkDocumentInfoBEO>> tags = new Dictionary<int, List<BulkDocumentInfoBEO>>();
                IDictionary<int, string> tagKeys = new Dictionary<int, string>();
                //Converting to RVWDocument from Document detail object
                var documents = ToDocumentBeoList(nativeSet);
                foreach (var document in documents)
                {
                    var bulkDocumentInfoBEO = new BulkDocumentInfoBEO
                    {
                        DocumentId = document.DocumentId,
                        DuplicateId = document.DuplicateId,
                        FromOriginalQuery = true,
                        CreatedBy = _jobParams.CreatedBy,
                        FamilyId = document.FamilyId
                    };

                    foreach (var tag in document.Tags)
                    {
                        if (tags.ContainsKey(tag.TagId))
                        {
                            var bulkDocumentInfoBeOs = tags.FirstOrDefault(t => t.Key == tag.TagId).Value;
                            bulkDocumentInfoBeOs.Add(bulkDocumentInfoBEO);
                        }
                        else
                        {
                            var bulkDocumentInfoBeOs = new List<BulkDocumentInfoBEO> {bulkDocumentInfoBEO};
                            tags.Add(tag.TagId, bulkDocumentInfoBeOs);
                            tagKeys.Add(tag.TagId, tag.TagName);
                        }
                    }
                }

                BulkTagging(tags, tagKeys);
                LogTaggingMessage(nativeSet, true, TagSuccesMessage);
            }
            catch (Exception ex)
            {
                LogTaggingMessage(nativeSet, false, TagFailureMessage);
                ReportToDirector(ex.ToUserString());
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Bulk tagging the documents that are selected for import
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="tagKeys"></param>
        private void BulkTagging(IEnumerable<KeyValuePair<int, List<BulkDocumentInfoBEO>>> tags, IDictionary<int, string> tagKeys)
        {
            foreach (var tag in tags)
            {
                var includeDuplicates = "False" + Constants.Colon + Constants.Zero;
                var includeFamilies = "False" + Constants.Colon + Constants.Zero;

                //Sending for bulk tagging
                BulkTagBO.DoBulkOperation(tag.Key, tagKeys[tag.Key], tag.Value, byte.Parse(Status),
                                          _jobParams.MatterId.ToString(CultureInfo.InvariantCulture),
                                          _jobParams.CollectionId,
                                          _jobParams.DatasetId.ToString(CultureInfo.InvariantCulture),
                                          includeDuplicates, includeFamilies);
            }
        }

        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <returns></returns>
        private LawImportBEO GetJobParams()
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(BootParameters);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof (LawImportBEO));

            //Deserialization of bootparameter to get ImportBEO
            return (LawImportBEO) xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="message">The document batch.</param>
        private void Send(PipeMessageEnvelope message)
        {
            if (OutputDataPipe != null)
            {
                OutputDataPipe.Send(message);
            }
        }


        /// <summary>
        /// Logs the tagging message.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="isSuccess">if set to <c>true</c> [is success].</param>
        /// <param name="message">The message.</param>
        private void LogTaggingMessage(IEnumerable<DocumentDetail> documents, bool isSuccess, string message)
        {
            try
            {
                var logs = documents.Select(document => new JobWorkerLog<LawImportTaggingLogInfo>
                {
                    JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                    CorrelationId =
                        (!string.IsNullOrEmpty(document.CorrelationId)) ? Convert.ToInt64(document.CorrelationId) : 0,
                    WorkerInstanceId = WorkerId,
                    WorkerRoleType = TaggingWorkerRoleType,
                    Success = isSuccess,
                    LogInfo = new LawImportTaggingLogInfo()
                    {
                        DCN = document.document.DocumentControlNumber,
                        CrossReferenceField = document.document.CrossReferenceFieldValue,
                        DocumentId = document.document.DocumentId,
                        Message = message,
                        Information = !isSuccess ? string.Format("{0} for DCN:{1}", message, document.document.DocumentControlNumber) : message
                    }
                }).ToList();
                LogPipe.ShouldNotBe(null);
                var logMessage = new PipeMessageEnvelope
                {
                    Body = logs
                };
                LogPipe.Send(logMessage);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex.ToUserString());
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Begins the worker process.
        /// </summary>
        protected override void BeginWork()
        {
            try
            {

                base.BeginWork();
                _jobParams = GetJobParams();
                if (EVHttpContext.CurrentContext == null)
                {
                    // Moq the session
                    MockSession(_jobParams.CreatedBy);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Mock Session : Windows job doesn't 
        /// </summary>
        private void MockSession(string createdBy)
        {
            #region Mock
            //MockWebOperationContext webContext = new MockWebOperationContext(createdBy);

            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            UserBusinessEntity userProp = UserBO.AuthenticateUsingUserGuid(createdBy);
            userProp.UserGUID = createdBy;
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


        private static List<RVWDocumentBEO> ToDocumentBeoList(List<DocumentDetail> documents)
        {
            var documentBeoList = new List<RVWDocumentBEO>();
            RVWDocumentBEO document;
            documents.ForEach(d =>
            {
                document = new RVWDocumentBEO { MatterId = d.document.MatterId };
                document.FieldList.AddRange(d.document.FieldList);
                document.CollectionId = d.document.CollectionId;
                document.NativeFilePath = d.document.NativeFilePath;
                document.DocumentRelationShip = d.document.DocumentRelationShip;
                document.DocumentId = d.document.DocumentId;
                document.Id = d.document.Id;
                document.DocumentBinary = d.document.DocumentBinary;
                document.CustomFieldToPopulateText = d.document.CustomFieldToPopulateText;
                if (d.document.Tags != null && d.document.Tags.Any())
                {
                    d.document.Tags.ForEach(x => document.Tags.Add(x));
                }
                documentBeoList.Add(document);
            });
            return documentBeoList;
        }
    }
}
