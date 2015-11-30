using System.Globalization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.BusinessEntities.Common;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Overdrive;
using System;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.BusinessEntities;
using OverdriveWorkers.Data;


namespace LexisNexis.Evolution.Worker
{
    public class IncludeDocumentsStartupWorker : WorkerBase
    {

        private AnalyticsProjectInfo _jobParameter;
        private AnalyticsProject _analyticProject;
        private DatasetBEO _dataset;
        private int _totalDocumentCount;
        private int _documentBachSize;
        private int _docStart;
        private int _docEnd;

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
            _totalDocumentCount = _analyticProject.GetSelectedDocumentsCount(_dataset.CollectionId, _jobParameter, WorkAssignment.JobId);
            //Update job log initial state
            var jobSummaryKeyValuePairs = new EVKeyValuePairs();
            JobMgmtBO.UpdateJobResult(WorkAssignment.JobId, 0, _totalDocumentCount,
                           jobSummaryKeyValuePairs);
            _documentBachSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("IncludeDocumentsIntoProjectJobBatchSize",
                "AnalyticsProject"));
            _jobParameter.DocumentSource.CollectionId = _dataset.CollectionId;
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

            var recordInfo = new ProjectDocumentRecordInfo
            {
                StartNumber = _docStart,
                EndNumber = _docEnd
            };

            Send(recordInfo);

            return _docEnd >= _totalDocumentCount;
        }





        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(ProjectDocumentRecordInfo recordInfo)
        {
            if (recordInfo == null) return;

            var message = new PipeMessageEnvelope
            {
                Body = recordInfo
            };
            if (OutputDataPipe != null)
                OutputDataPipe.Send(message);
        }
    }
}

