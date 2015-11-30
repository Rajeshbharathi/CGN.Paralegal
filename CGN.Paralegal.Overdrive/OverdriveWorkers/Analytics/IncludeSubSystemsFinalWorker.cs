using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
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
using LexisNexis.LTN.Analytics.ServiceContract;
using OverdriveWorkers.Data;
using System.Diagnostics;

namespace LexisNexis.Evolution.Worker
{
    public class IncludeSubSystemsFinalWorker : WorkerBase
    {
        private AnalyticsProjectInfo _jobParameter;
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
            if (message.Label == "PleaseBuildProject")
            {
                BuildProject();
                return;
            }
            var projectDocumentCollection = (ProjectDocumentCollection)message.Body;
            IncreaseProcessedDocumentsCount(projectDocumentCollection.Documents.Count);
        }

        /// <summary>
        /// Delete project
        /// </summary>
        private void BuildProject()
        {
            var stopWatch = Stopwatch.StartNew();
            var indexId = AnalyticsProject.GetIndexIdForProject(_jobParameter.MatterId, WorkAssignment.JobId, _dataset.CollectionId,
               _jobParameter.ProjectCollectionId);
            var indexService = AnalyticsProject.GetAnalyticalEngineIndexService(_jobParameter.MatterId, WorkAssignment.JobId, indexId);
            AnalyticsProject.BuildProjectInAnalyticalEngine(_jobParameter.MatterId, WorkAssignment.JobId, indexService, indexId);
            stopWatch.Stop();
            Tracer.Info("Job {0} : Time taken for build project in Analytical Engine  {1} m.s ", WorkAssignment.JobId, stopWatch.ElapsedMilliseconds);
        }
    }
}
