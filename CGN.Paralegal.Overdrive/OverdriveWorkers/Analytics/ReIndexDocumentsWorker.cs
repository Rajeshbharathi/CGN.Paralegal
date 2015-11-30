using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;

namespace OverdriveWorkers.Analytics
{
    /// <summary>
    ///     ReIndexDocumentsWorker
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class ReIndexDocumentsWorker : WorkerBase
    {
        /// <summary>
        ///     The _example set
        /// </summary>
        private DocumentSource _documentSource;

        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _documentSource =
                (DocumentSource)XmlUtility.DeserializeObject(BootParameters, typeof(DocumentSource));
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            var analyticIndex = new AnalyticalIndex();
            analyticIndex.ProgressChanged += ControlSetProgressChanged;
            analyticIndex.ReIndexDocuments(WorkAssignment.JobId, _documentSource);
            return true;
        }

        /// <summary>
        /// Controls the set progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ControlSetProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount, e.ProcessedDocumentCount);
        }
    }
}
