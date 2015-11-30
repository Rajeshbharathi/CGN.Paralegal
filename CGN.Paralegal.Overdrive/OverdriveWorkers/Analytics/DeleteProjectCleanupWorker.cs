using System.Globalization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Business.Analytics;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class DeleteProjectCleanupWorker : WorkerBase
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
            //Indicates the final message to do the final step
            if (message.Label == "PleaseCleanup")
            {
                DeleteProject();
                return;
            }
            var projectDocumentCollection = (ProjectDocumentCollection)message.Body;
            IncreaseProcessedDocumentsCount(projectDocumentCollection.Documents.Count);
        }

        /// <summary>
        /// Delete project
        /// </summary>
        private void DeleteProject()
        {
            _analyticProject = new AnalyticsProject();
            _analyticProject.DeleteProject(_jobParameter.MatterId, _dataset,  _jobParameter, WorkAssignment.JobId);
        }
    }
}
