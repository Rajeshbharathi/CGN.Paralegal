using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Linq;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.TraceServices;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class IncludeDocumentsUpdateWorker : SearchEngineWorkerBase
    {

        private AnalyticsProjectInfo _jobParameter;
        private AnalyticsProject _analyticProject;
        private DatasetBEO _dataset;
        
      

        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (AnalyticsProjectInfo)XmlUtility.DeserializeObject(BootParameters, typeof(AnalyticsProjectInfo));
            _analyticProject = new AnalyticsProject();
            _dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(_jobParameter.DatasetId, CultureInfo.CurrentCulture));
            _jobParameter.DocumentSource.CollectionId = _dataset.CollectionId;
            SetCommiyIndexStatusToInitialized(_jobParameter.MatterId);
        }


        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                var projectDocumentCollection = (ProjectDocumentCollection) message.Body;
                projectDocumentCollection.ShouldNotBe(null);
                projectDocumentCollection.Documents.ShouldNotBe(null);

                _analyticProject = new AnalyticsProject();
                if (_jobParameter.IsRerunJob)  //---Rerun
                {
                    ProcessDocumentsForRerunJob(projectDocumentCollection);
                }
                else
                {

                    var documents =
                        projectDocumentCollection.Documents.Select(
                            projectDocument => new Business.Analytics.DocumentIdentifier
                                               {
                                                   ReferenceId = projectDocument.DocumentReferenceId,
                                                   Url = projectDocument.TextFilePath,
                                                   DocumentIndexingStatus = projectDocument.DocumentIndexStatus,
                                                   DocumentTextSize = projectDocument.DocumentTextSize
                                               }).ToList();

                    _analyticProject.AddDocumentsIntoProject(_jobParameter.MatterId, _dataset, documents, _jobParameter,
                        projectDocumentCollection.ProjectFieldId, WorkAssignment.JobId);
                }

                IncreaseProcessedDocumentsCount(projectDocumentCollection.Documents.Count()); //Progress Status

            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Process documents for Rerun job
        /// </summary>
        /// <param name="projectDocumentCollection">ProjectDocumentCollection</param>
        private void ProcessDocumentsForRerunJob(ProjectDocumentCollection projectDocumentCollection)
        {
             //Fix Existing documents
            var documentsUpdate =
                projectDocumentCollection.Documents.Where(d => d.IsDocumentUpdate)
                    .Select(projectDocument => new Business.Analytics.DocumentIdentifier
                                               {
                                                   ReferenceId = projectDocument.DocumentReferenceId,
                                                   Url = projectDocument.TextFilePath,
                                                   DocumentIndexingStatus = projectDocument.DocumentIndexStatus,
                                                   DocumentTextSize = projectDocument.DocumentTextSize
                                               }).ToList();

            if (documentsUpdate.Any())
            {
                _analyticProject.AddDocumentsIntoProject(_jobParameter.MatterId, _dataset, documentsUpdate,
                    _jobParameter,
                    projectDocumentCollection.ProjectFieldId, WorkAssignment.JobId, true);
            }


            //Add unprocessed documents
            var documentsAdd =
                projectDocumentCollection.Documents.Where(d => !d.IsDocumentUpdate)
                    .Select(projectDocument => new Business.Analytics.DocumentIdentifier
                                               {
                                                   ReferenceId = projectDocument.DocumentReferenceId,
                                                   Url = projectDocument.TextFilePath,
                                                   DocumentIndexingStatus = projectDocument.DocumentIndexStatus,
                                                   DocumentTextSize = projectDocument.DocumentTextSize
                                               }).ToList();

            if (documentsAdd.Any())
            {
                _analyticProject.AddDocumentsIntoProject(_jobParameter.MatterId, _dataset, documentsAdd,
                    _jobParameter,
                    projectDocumentCollection.ProjectFieldId, WorkAssignment.JobId);
            }
        }

        protected override void EndWork()
        {
            base.EndWork();
            SetCommitIndexStatusToCompleted(_jobParameter.MatterId);

        }
       
       
    }
}
