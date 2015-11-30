using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Linq;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.LTN.Analytics.ServiceContract;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class IncludeSubSystemsAddDocumentsWorker : WorkerBase
    {

        private AnalyticsProjectInfo _jobParameter;
        private AnalyticsProject _analyticProject;
        private DatasetBEO _dataset;
        private IIndexService _indexService;
        private string _indexId;

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
            _jobParameter.DocumentSource.CollectionId = _dataset.CollectionId;
        }



        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                if (string.IsNullOrEmpty(_indexId))
                {
                    _indexId = AnalyticsProject.GetIndexIdForProject(_jobParameter.MatterId, WorkAssignment.JobId,
                        _dataset.CollectionId, _jobParameter.ProjectCollectionId);
                }

                if (_indexService == null)
                {
                    //Get Indexservice
                    _indexService = AnalyticsProject.GetAnalyticalEngineIndexService(_jobParameter.MatterId,
                        WorkAssignment.JobId, _indexId);
                }


                var projectDocumentCollection = (ProjectDocumentCollection)message.Body;
                projectDocumentCollection.ShouldNotBe(null);
                projectDocumentCollection.Documents.ShouldNotBe(null);
                var documents = projectDocumentCollection.Documents.Select(projectDocument => new Business.Analytics.DocumentIdentifier
                {
                    ReferenceId = projectDocument.DocumentReferenceId,
                    DocId = projectDocument.DocId,
                    Url = projectDocument.TextFilePath
                }).ToList();

                _analyticProject = new AnalyticsProject();
                _analyticProject.AddDocumentsInAnalyticalEngine(_jobParameter.MatterId, _dataset.CollectionId,_jobParameter, documents, _indexService,
                     _indexId, WorkAssignment.JobId);

                Send(projectDocumentCollection);

            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(ProjectDocumentCollection data)
        {
            var message = new PipeMessageEnvelope
            {
                Body = data
            };
            if (OutputDataPipe != null)
                OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(data.Documents.Count);
        }

    }
}
