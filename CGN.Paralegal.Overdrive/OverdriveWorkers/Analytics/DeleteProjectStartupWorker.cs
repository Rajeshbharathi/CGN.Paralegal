using System.Globalization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Infrastructure;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class DeleteProjectStartupWorker : WorkerBase
    {

       
        private AnalyticsProjectInfo _jobParameter;
        private AnalyticsProject _analyticProject;
        private int _totalDocumentCount;
        private DatasetBEO _dataset;
        private int _batchSize;
        private int _docStart;
        private int _docEnd;
        private int _projectFieldId;

        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (AnalyticsProjectInfo)XmlUtility.DeserializeObject(BootParameters, typeof(AnalyticsProjectInfo));
           
            _dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(_jobParameter.DatasetId, CultureInfo.CurrentCulture));
            _analyticProject = new AnalyticsProject();
            _totalDocumentCount = _analyticProject.GetProjectDocumentsCount( Convert.ToInt64(_jobParameter.MatterId, CultureInfo.CurrentCulture),
                _jobParameter.ProjectCollectionId);
            _batchSize =
            Convert.ToInt32(ApplicationConfigurationManager.GetValue("UpdateFieldsBatchSize", "AnalyticsProject"));
            _projectFieldId = AnalyticsProject.GetProjectFieldId(_jobParameter.MatterId, _dataset.CollectionId);
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            _docStart = _docEnd + 1;
            _docEnd = _docEnd + _batchSize;

            var projectDocumentDataList = GetDocuments();

            Send(projectDocumentDataList);

            return _docEnd >= _totalDocumentCount;
        }


        /// <summary>
        /// Get Documents from source
        /// </summary>
        /// <returns></returns>
        private List<ProjectDocumentDetail> GetDocuments()
        {
            try
            {
                var documents = _analyticProject.GetProjectDocuments(_jobParameter.MatterId, _dataset.CollectionId,
                    _jobParameter.ProjectCollectionId, _docStart, _docEnd);

                var projectDocumentDataList = documents.Select(document => new ProjectDocumentDetail
                {
                    DocumentReferenceId = document.ReferenceId
                }).ToList();

                return projectDocumentDataList;
            }
            catch (Exception ex)
            {
                AnalyticsProject.LogError(_jobParameter.MatterId, WorkAssignment.JobId, 20991, ex);
                throw;
            }
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<ProjectDocumentDetail> documents)
        {
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
            IncreaseProcessedDocumentsCount(documents.Count);
        }

    }
}
