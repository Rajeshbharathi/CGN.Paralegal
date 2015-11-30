using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.External.DataAccess;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;

namespace LexisNexis.Evolution.Worker
{

    internal class IncludeDocumentsReprocessWorker : WorkerBase
    {
        private ConversionReprocessJobBeo _reprocessJobParameter;
        private AnalyticsProjectInfo _jobParameter;
        private DatasetBEO _dataset;

        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();

            _reprocessJobParameter = (ConversionReprocessJobBeo)XmlUtility.DeserializeObject(BootParameters, typeof(ConversionReprocessJobBeo));

            var baseConfig = ReconversionDAO.GetJobConfigInfo(Convert.ToInt32(_reprocessJobParameter.OrginialJobId));

            _jobParameter =
                (AnalyticsProjectInfo)XmlUtility.DeserializeObject(baseConfig.BootParameters, typeof(AnalyticsProjectInfo));

            _dataset = DataSetBO.GetDataSetDetailForDataSetId(_jobParameter.DatasetId);
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            //Get document id list for reprocess from user selection
            var reprocessDocumentList = GetDocumentsFromReprocessSelection(_reprocessJobParameter.FilePath,
                    _reprocessJobParameter.JobSelectionMode,
                    _jobParameter.MatterId,
                    _jobParameter.DatasetId,
                    _reprocessJobParameter.OrginialJobId,
                    _reprocessJobParameter.Filters);

            //Reprocess Include documents
            var analyticProject = new AnalyticsProject();
            analyticProject.ProgressChanged += AnalyticProjectProgressChanged;
            analyticProject.ReprocessIncludeProjectDocuments(_jobParameter.MatterId,
                _jobParameter.DatasetId, WorkAssignment.JobId, Convert.ToInt32(_reprocessJobParameter.OrginialJobId), _jobParameter, reprocessDocumentList);
            return true;
        }

        private void AnalyticProjectProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount, e.ProcessedDocumentCount);
        }


        public List<ReconversionDocumentBEO> GetDocumentsFromReprocessSelection(
        string inputFilePath, ReProcessJobSelectionMode selectionMode, long matterId, long datasetId, long jobId,
        string filters = null)
        {
            var reprocessDocumentList = new List<ReconversionDocumentBEO>();
            switch (selectionMode)
            {
                case ReProcessJobSelectionMode.Selected:
                    {
                        var docidList = ConversionReprocessStartupHelper.GetDocumentIdListFromFile(inputFilePath,
                            Constants.DocId);
                        reprocessDocumentList.AddRange(ConversionReprocessStartupHelper.GetImportDocumentListForIDList(docidList, Constants.DocId, null, matterId));
                        break;
                    }
                case ReProcessJobSelectionMode.CrossReference:
                    {
                        var docidList = ConversionReprocessStartupHelper.GetDocumentIdListFromFile(inputFilePath,
                            Constants.DCN);
                        reprocessDocumentList.AddRange(ConversionReprocessStartupHelper.GetImportDocumentListForIDList(docidList, Constants.DCN, _dataset.CollectionId, matterId));
                        break;
                    }
                case ReProcessJobSelectionMode.Csv:
                    var dictIds = ConversionReprocessStartupHelper.GetDocumentIdListFromFile(inputFilePath,
                        Constants.DCN, Constants.DocumentSetName);
                    var lstDocumentSet = DataSetBO.GetAllDocumentSet(datasetId.ToString(CultureInfo.InvariantCulture));
                    foreach (var key in dictIds.Keys)
                    {
                        var firstOrDefault = lstDocumentSet.FirstOrDefault(d => d.DocumentSetName.Equals(key));
                        if (firstOrDefault == null) continue;
                        var collectionId = firstOrDefault.DocumentSetId;
                        reprocessDocumentList.AddRange(ConversionReprocessStartupHelper.GetImportDocumentListForIDList(dictIds[key], Constants.DCN, collectionId, matterId));
                    }
                    break;
                case ReProcessJobSelectionMode.All:
                    reprocessDocumentList.AddRange(ConversionReprocessStartupHelper.GetReconversionDocumentBeosForJobId(matterId, jobId, filters));
                    break;
            }
            return reprocessDocumentList;
        }
    }
}
