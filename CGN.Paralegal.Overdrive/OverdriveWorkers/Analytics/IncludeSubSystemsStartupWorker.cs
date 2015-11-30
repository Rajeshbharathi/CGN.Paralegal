using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Common;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.TraceServices;
using OverdriveWorkers.Data;
using DocumentIdentifier = LexisNexis.Evolution.Business.Analytics.DocumentIdentifier;

namespace LexisNexis.Evolution.Worker
{
    public class IncludeSubSystemsStartupWorker : WorkerBase
    {

        private AnalyticsProjectInfo _jobParameter;
        private AnalyticsProject _analyticProject;
        private DatasetBEO _dataset;
    
        private int _totalDocumentCount;
        private int _documentBachSize;
        private int _docStart;
        private int _docEnd;
        private string _indexId;


        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();

            _jobParameter =
                (AnalyticsProjectInfo) XmlUtility.DeserializeObject(BootParameters, typeof (AnalyticsProjectInfo));
            _analyticProject = new AnalyticsProject();

            _documentBachSize =
                Convert.ToInt32(
                    ApplicationConfigurationManager.GetValue("IncludeDocumentsIntoProjectInSubSystemJobBatchSize",
                        "AnalyticsProject"));
            _dataset =
                DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(_jobParameter.DatasetId,
                    CultureInfo.CurrentCulture));
            _jobParameter.DocumentSource.CollectionId = _dataset.CollectionId;
            
            _totalDocumentCount =
            _analyticProject.GetProjectDocumentsCountByTaskId(
                Convert.ToInt64(_jobParameter.MatterId, CultureInfo.CurrentCulture),
                _jobParameter.ProjectCollectionId, _jobParameter.PrimarySystemJobId);
            //Update job log initial state
            var jobSummaryKeyValuePairs = new EVKeyValuePairs();
            JobMgmtBO.UpdateJobResult(WorkAssignment.JobId, 0, _totalDocumentCount,
                           jobSummaryKeyValuePairs);

            if (_jobParameter.IsRerunJob || _jobParameter.IsAddAdditionalDocuments) //Rerun job or Add additional documents- need get to get existing IndexId ,if already created
            {
                _indexId = AnalyticsProject.GetIndexIdForProject(_jobParameter.MatterId, WorkAssignment.JobId,
                    _dataset.CollectionId, _jobParameter.ProjectCollectionId,false);
            }

            if(string.IsNullOrEmpty(_indexId))
            {
                _indexId = "idx-" + Guid.NewGuid().ToString().ToLowerInvariant();
                _analyticProject.InsertIndexId(_jobParameter.MatterId, WorkAssignment.JobId, _dataset.CollectionId,
                    _jobParameter.ProjectCollectionId, _indexId);
            }
            AnalyticsProject.CreateAnalyticalIndex(_jobParameter.MatterId, WorkAssignment.JobId, _indexId);  //Create Index in Spark SVM..
            IncreaseProcessedDocumentsCount(_totalDocumentCount);
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            _docStart = _docEnd + 1;
            _docEnd = _docEnd + _documentBachSize;

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
                var documents = _analyticProject.GetProjectDocumentsByTaskId(_jobParameter.MatterId, _dataset.CollectionId,
                    _jobParameter.ProjectCollectionId, _docStart, _docEnd,_jobParameter.PrimarySystemJobId);

                if (_jobParameter.IsRerunJob)  //---Rerun
                {
                    return GetDocumentsForRerunJob(documents);
                }

                var projectDocumentDataList = documents.Select(document => new ProjectDocumentDetail
                {
                    DocumentReferenceId = document.ReferenceId,
                    DocId = document.DocId,
                    TextFilePath = document.Url
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
        /// Get documents for Rerun job
        /// </summary>
        /// <param name="documents"></param>
        /// <returns></returns>
        private List<ProjectDocumentDetail> GetDocumentsForRerunJob(List<DocumentIdentifier> documents)
        {
            var documentStatusList = _analyticProject.GetProjectDocumentsStausFromProcessSet(_jobParameter.MatterId,
                _dataset.CollectionId,
                _jobParameter.PrimarySystemJobId,
                WorkAssignment.JobId, documents.Select(d => d.ReferenceId).ToList());

            var projectDocumentDetailList = new List<ProjectDocumentDetail>();

            foreach (var document in documents)
            {
                var resultdocumentStatus =
                    documentStatusList.FirstOrDefault(d => d.DocumentReferenceId == document.ReferenceId);

                if (resultdocumentStatus != null && resultdocumentStatus.SubSystemStatus)
                    continue; //Document processed earlier and already succeed, then no need to process again

                var projectDocumentDetail = new ProjectDocumentDetail
                                            {
                                                DocumentReferenceId =
                                                    document.ReferenceId,
                                                TextFilePath = document.Url
                                            };
                projectDocumentDetailList.Add(projectDocumentDetail);
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
                Documents = documents
            };
            var message = new PipeMessageEnvelope
            {
                Body = documentCollection
            };
            if (OutputDataPipe != null)
                OutputDataPipe.Send(message);
        }



    }
}
