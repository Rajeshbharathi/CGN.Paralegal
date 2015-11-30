using System;
using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.LTN.Analytics.ServiceContract;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    public class CategorizeProjectDocumentsWorker : WorkerBase
    {

        private CategorizeInfo _jobParameter;
        const string NotRelevantCategory = "Not_Relevant";
        const string RelevantCategory = "Relevant";
        private int _batchSize;
        private AnalyticsProjectInfo _projectInfo;
        private AnalyticsProject _analyticProject;

        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (CategorizeInfo)XmlUtility.DeserializeObject(BootParameters, typeof(CategorizeInfo));
            _batchSize =
               Convert.ToInt32(ApplicationConfigurationManager.GetValue("UpdateFieldsBatchSize", "AnalyticsProject"));
            _projectInfo = AnalyticsProject.Get(_jobParameter.MatterId.ToString(CultureInfo.InvariantCulture),
              _jobParameter.DatasetId.ToString(CultureInfo.InvariantCulture), _jobParameter.ProjectId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            _analyticProject = new AnalyticsProject();
            var totalDocument = 0;
            try
            {
                var projectDocument = _analyticProject.GetProjectDocumentsCount(_jobParameter.MatterId,
               _projectInfo.ProjectCollectionId);

                totalDocument = projectDocument; 

                //Step 1 : Categorize Document
                var categorizedResultDocuments = _analyticProject.CategorizeProjectDocuments(_jobParameter.MatterId,
                    _jobParameter.DatasetId, _jobParameter.ProjectId,
                    WorkAssignment.JobId, _jobParameter.CreatedBy);
              

                //Step 2: Send documents to next worker for Update Fields
                if (categorizedResultDocuments != null)
                {
                    SendDocumentsForUpdate(categorizedResultDocuments);
                }
                return true;
            }
            catch (Exception ex)
            {
                //Update Job Status
                _analyticProject.UpdateJobResult(WorkAssignment.JobId, 0, totalDocument);
                ex.Trace().Swallow();
                throw;
            }
           
        }


        /// <summary>
        /// Send documents to next worker for Update Fields
        /// </summary>
        /// <param name="categorizedResultDocuments"></param>
        private void SendDocumentsForUpdate(IEnumerable<CategorizedDoc> categorizedResultDocuments)
        {
            var resultDocuments = new List<ProjectDocumentDetail>();
            foreach (var categorizedResultDocument in categorizedResultDocuments)
            {
                var resultDocument = new ProjectDocumentDetail {DocId = categorizedResultDocument.DocId };
                var predictedCategory = string.Empty;
                double predictedScore = 0;
                if (categorizedResultDocument.CategoryResults != null && categorizedResultDocument.CategoryResults.Any())
                {
                    if (!string.IsNullOrEmpty(categorizedResultDocument.CategoryResults[0].CategoryName))
                    {
                        predictedCategory = categorizedResultDocument.CategoryResults[0].CategoryName.Equals("R")
                            ? RelevantCategory
                            : NotRelevantCategory;
                    }

                    predictedScore = categorizedResultDocument.CategoryResults[0].Score;
                }
                resultDocument.PredictedCategory = predictedCategory;
                resultDocument.DocumentScore = predictedScore;
                resultDocuments.Add(resultDocument);
                if (resultDocuments.Count < _batchSize) continue;
                //Send documents to pipeline
                Send(resultDocuments);
                resultDocuments.Clear();
            }

            if (resultDocuments.Any())
            {
                //Send documents to pipeline
                Send(resultDocuments);
            }
        }


        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<ProjectDocumentDetail> documents)
        {

            var documentCollection = new ProjectDocumentCollection
            {
                Documents = documents
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
