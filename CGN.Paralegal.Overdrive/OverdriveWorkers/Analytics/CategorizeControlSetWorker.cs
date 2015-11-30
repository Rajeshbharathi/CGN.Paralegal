using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;

namespace OverdriveWorkers.Analytics
{
    internal class CategorizeControlSetWorker : WorkerBase
    {
        /// <summary>
        ///     The _job parameter
        /// </summary>
        private CategorizeInfo _jobParameter;

        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (CategorizeInfo)XmlUtility.DeserializeObject(BootParameters, typeof(CategorizeInfo));
        }
        protected override bool GenerateMessage()
        {
            var analysisSet = new AnalysisSet();
            analysisSet.ProgressChanged += CategorizeControlSetProgressChanged;
            analysisSet.CategorizeControlset(_jobParameter.MatterId, _jobParameter.DatasetId, _jobParameter.ProjectId,
                _jobParameter.TrainingsetRound, WorkAssignment.JobId, _jobParameter.CreatedBy, _jobParameter.IsRerunJob);
            return true;
        }

        private void CategorizeControlSetProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount, e.ProcessedDocumentCount);
        }

    }
}
