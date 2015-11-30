using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Documents;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Business.Common;

namespace OverdriveWorkers.Analytics
{
    internal class ExportAnalysisSetWorker : WorkerBase
    {
        /// <summary>
        ///     The _job parameter
        /// </summary>
        private DocumentQuery _jobParameter;

        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =(DocumentQuery)XmlUtility.DeserializeObject(BootParameters, typeof(DocumentQuery));
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            var analysisSet = new AnalysisSet();
            analysisSet.ProgressChanged += ExportAnalysisSetProgressChanged;
            analysisSet.ExportAnalysisSetDocuments(_jobParameter,WorkAssignment.JobId);
            return true;
        }

        private void ExportAnalysisSetProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount, e.ProcessedDocumentCount);
        }
       
    }
}
