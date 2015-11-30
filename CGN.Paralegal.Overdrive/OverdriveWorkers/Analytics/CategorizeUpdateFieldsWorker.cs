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
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.TraceServices;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class CategorizeUpdateFieldsWorker : SearchEngineWorkerBase
    {
        private CategorizeInfo _jobParameter;
        private DatasetBEO _dataset;
        private AnalyticsProjectInfo _projectInfo;
        

        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (CategorizeInfo) XmlUtility.DeserializeObject(BootParameters, typeof (CategorizeInfo));

            _dataset = DataSetBO.GetDataSetDetailForDataSetId(_jobParameter.DatasetId);
            _projectInfo = AnalyticsProject.Get(_jobParameter.MatterId.ToString(CultureInfo.InvariantCulture),
                _jobParameter.DatasetId.ToString(CultureInfo.InvariantCulture),
                _jobParameter.ProjectId.ToString(CultureInfo.InvariantCulture));
            SetCommiyIndexStatusToInitialized(_jobParameter.MatterId);

        }


        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                var analyticProject = new AnalyticsProject();
                var projectDocumentCollection = (ProjectDocumentCollection) message.Body;
                projectDocumentCollection.ShouldNotBe(null);
                projectDocumentCollection.Documents.ShouldNotBe(null);

                var documentIds = projectDocumentCollection.Documents.Select(p => p.DocId).ToList();
                var documentResult = analyticProject.BulkGetDocumentsByDocIds(_jobParameter.MatterId,
                    _dataset.CollectionId, documentIds);


                var projectDocuments = new List<AnalysisSetDocumentInfo>();
                foreach (var document in projectDocumentCollection.Documents)
                {
                    var projectDocument = new AnalysisSetDocumentInfo
                    {
                        DocumentId = (int) document.DocId,
                        PredictedCategory = document.PredictedCategory,
                        DocumentScore = (decimal) document.DocumentScore
                    };
                    var result = documentResult.FirstOrDefault(d => d.Id == document.DocId);
                    if (result == null) continue;
                    projectDocument.DocumentReferenceId = result.DocumentID;
                    projectDocuments.Add(projectDocument);
                }

                analyticProject.UpdateFieldsForCategorizeDocuments(_jobParameter.MatterId, _projectInfo,
                    _dataset.CollectionId, WorkAssignment.JobId, projectDocuments);

                IncreaseProcessedDocumentsCount(projectDocumentCollection.Documents.Count()); //Progress Status

            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        protected override void EndWork()
        {
            base.EndWork();
            SetCommitIndexStatusToCompleted(_jobParameter.MatterId);

        }
    }
}
