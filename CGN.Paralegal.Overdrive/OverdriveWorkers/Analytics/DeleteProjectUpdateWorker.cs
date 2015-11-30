using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexisNexis.Evolution.TraceServices;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class DeleteProjectUpdateWorker : WorkerBase
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
            _dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(_jobParameter.DatasetId, CultureInfo.CurrentCulture));
            
        }


        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                var projectDocumentCollection = (ProjectDocumentCollection)message.Body;
                projectDocumentCollection.ShouldNotBe(null);
                projectDocumentCollection.Documents.ShouldNotBe(null);
                var documents = projectDocumentCollection.Documents.Select(projectDocument => new Business.Analytics.DocumentIdentifier
                {
                    ReferenceId = projectDocument.DocumentReferenceId
                }).ToList();
                _analyticProject = new AnalyticsProject();
                _analyticProject.UpdateProjectFieldForDeleteDocuments(_jobParameter.MatterId, _dataset.CollectionId, _jobParameter, documents, 
                  projectDocumentCollection.ProjectFieldId, WorkAssignment.JobId);

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
