using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.BusinessEntities;
using Moq;
using OverdriveWorkers.Data;
using DocumentIdentifier = LexisNexis.Evolution.Business.Analytics.DocumentIdentifier;
using System.Web;

namespace LexisNexis.Evolution.Worker
{
    public class IncludeDocumentsReaderWorker : WorkerBase
    {


        private AnalyticsProjectInfo _jobParameter;
        private AnalyticsProject _analyticProject;
        private DatasetBEO _dataset;
        private int _projectFieldId;
        private int _documentBachSize;
        private int _docStart;
        private int _docEnd;
        private string _jobIds;
        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (AnalyticsProjectInfo)XmlUtility.DeserializeObject(BootParameters, typeof(AnalyticsProjectInfo));
            _analyticProject = new AnalyticsProject();
            _dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(_jobParameter.DatasetId, CultureInfo.CurrentCulture));
            _projectFieldId = AnalyticsProject.GetProjectFieldId(_jobParameter.MatterId, _dataset.CollectionId);
            _documentBachSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("IncludeDocumentsIntoProjectJobBatchSize",
                "AnalyticsProject"));
            _jobParameter.DocumentSource.CollectionId = _dataset.CollectionId;

            if (!_jobParameter.IsAddAdditionalDocuments || !string.IsNullOrEmpty(_jobIds)) return;
            _jobIds = GetIncludeJobIds();
        }


      
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                var recordInfo = (ProjectDocumentRecordInfo)message.Body;
                recordInfo.ShouldNotBe(null);

                _docStart = recordInfo.StartNumber;
                _docEnd = recordInfo.EndNumber;

               

                var projectDocumentDataList = GetDocuments();

                if (projectDocumentDataList==null) return;

                SetDocumentIndexStatusAndContentSize(projectDocumentDataList);

                Send(projectDocumentDataList);


                IncreaseProcessedDocumentsCount(projectDocumentDataList.Count()); //Progress Status

            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Set Document index status and Content size
        /// </summary>
        private void SetDocumentIndexStatusAndContentSize(List<ProjectDocumentDetail> projectDocumentDataList)
        {
            var documents = projectDocumentDataList.Select(projectDocument => projectDocument.DocumentReferenceId).ToList();
            var resultDocuments = _analyticProject.BulkGetDocumentContentSizeAndIndexStatusInfo(_jobParameter.MatterId,
                _dataset.CollectionId, documents);

            foreach (var projectDocument in projectDocumentDataList)
            {
                var document =
                    resultDocuments.FirstOrDefault(d => d.DocumentId == projectDocument.DocumentReferenceId);
                if (document == null) continue;
                projectDocument.DocumentIndexStatus = document.DocumentIndexingStatus;
                projectDocument.DocumentTextSize = document.FileSize;
            }
        }


        /// <summary>
        /// Get Documents from source
        /// </summary>
        /// <returns></returns>
        private List<ProjectDocumentDetail> GetDocuments()
        {
            try
            {
                var documents = _analyticProject.GetDocumentIdentifiers(_jobParameter.MatterId, _jobParameter.DatasetId,
                    _jobParameter.DocumentSource, _docStart, _docEnd, _documentBachSize, _jobParameter.IsAddAdditionalDocuments, _jobParameter.ProjectCollectionId, _jobIds);

                var documentIdentifiers = documents as DocumentIdentifier[] ?? documents.ToArray();
                if (documents == null || !documentIdentifiers.Any()) return null;

                if (_jobParameter.IsRerunJob)  //---Rerun
                {
                   return GetDocumentsForRerunJob(documentIdentifiers);
                }

                var projectDocumentDataList = documentIdentifiers.Select(document => new ProjectDocumentDetail
                {
                    DocumentReferenceId = document.ReferenceId,
                    TextFilePath = document.Url
                }).ToList();

                return projectDocumentDataList;
            }
            catch (Exception ex)
            {
                //Status code : 20991 for Get documents
                AnalyticsProject.LogError(_jobParameter.MatterId, WorkAssignment.JobId, 20991, ex);
                throw;
            }
        }


        /// <summary>
        /// Get documents for Rerun job
        /// </summary>
        /// <param name="documents">Documents</param>
        private List<ProjectDocumentDetail> GetDocumentsForRerunJob(IEnumerable<DocumentIdentifier> documents)
        {
            var documentStatusList = _analyticProject.GetProjectDocumentsStausFromProcessSet(_jobParameter.MatterId,
                _dataset.CollectionId,
                _jobParameter.RerunOriginalJobId,
                WorkAssignment.JobId, documents.Select(d => d.ReferenceId).ToList());

            var projectDocumentDetailList = new List<ProjectDocumentDetail>();

            foreach (var document in documents)
            {
                var resultdocumentStatus = documentStatusList.FirstOrDefault(d => d.DocumentReferenceId == document.ReferenceId);

                if (resultdocumentStatus != null) //Document processed earlier
                {
                    if (resultdocumentStatus.PrimarySystemStatus) continue; //If already succeed, then no need to process again
                    var projectDocumentDetail = new ProjectDocumentDetail
                                                {
                                                    DocumentReferenceId =
                                                        document.ReferenceId,
                                                    TextFilePath = document.Url,
                                                    IsDocumentUpdate = true
                                                };
                    projectDocumentDetailList.Add(projectDocumentDetail);
                }
                else //Document not processed earlier
                {
                    var projectDocumentDetail = new ProjectDocumentDetail
                                                {
                                                    DocumentReferenceId =
                                                        document.ReferenceId,
                                                    TextFilePath = document.Url
                                                };
                    projectDocumentDetailList.Add(projectDocumentDetail);
                }
            }
            return projectDocumentDetailList;
        }


        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<ProjectDocumentDetail> documents)
        {
            if (documents == null || !documents.Any()) return;
            var documentCollection = new ProjectDocumentCollection
            {
                Documents = documents,
                ProjectFieldId = _projectFieldId
            };
            var message = new PipeMessageEnvelope
            {
                Body = documentCollection
            };
            if (OutputDataPipe != null)
                OutputDataPipe.Send(message);
        }

        /// <summary>
        /// Get include job ids
        /// </summary>
        /// <returns></returns>
        private string GetIncludeJobIds()
        {
            SetUserMockSessionForJob(_jobParameter.CreatedBy);
            var jobids = _analyticProject.GetIncludeJobList(_jobParameter.Id);
            if (!jobids.Any()) return string.Empty;
            var jobs = jobids.Where(jobid => jobid != WorkAssignment.JobId).ToList();
            return (jobs.Any()) ? string.Join(",", jobs.ToList()) : string.Empty;
        }

        /// <summary>
        /// Set User Mock Session.
        /// </summary>
        private void SetUserMockSessionForJob(string createdBy)
        {
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            var userProp = UserBO.GetUserByGuid(createdBy);
            var userSession = new UserSessionBEO
            {
                UserId = userProp.UserId,
                UserGUID = createdBy,
                DomainName = userProp.DomainName,
                IsSuperAdmin = userProp.IsSuperAdmin,
                LastPasswordChangedDate = userProp.LastPasswordChangedDate,
                PasswordExpiryDays = userProp.PasswordExpiryDays,
                Timezone = userProp.Timezone
            };
            if (userProp.Organizations.Any())
                userSession.Organizations.AddRange(userProp.Organizations);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
        }

    }
}

