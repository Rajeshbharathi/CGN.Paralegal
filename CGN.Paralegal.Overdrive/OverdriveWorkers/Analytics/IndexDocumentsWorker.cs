using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;

namespace OverdriveWorkers.Analytics
{
    /// <summary>
    ///     IndexDocumentsWorker
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class IndexDocumentsWorker : WorkerBase
    {
        /// <summary>
        ///     Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
        }

        /// <summary>
        ///     Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
                DocumentSource docSource = GetJobParams(BootParameters);
            var analyticIndex = new AnalyticalIndex();
            docSource.ShouldNotBe(null);
            analyticIndex.ProgressChanged += IndexDocumentsProgressChanged;
            analyticIndex.IndexDocuments(docSource,WorkAssignment.JobId,WorkAssignment.ScheduleCreatedBy);
            
            return true;
        }

        /// <summary>
        ///     De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private static DocumentSource GetJobParams(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Creating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(DocumentSource));

            //Deserialization of bootparameter to get ImportBEO
            return (DocumentSource)xmlStream.Deserialize(stream);
        }


        /// <summary>
        /// Controls the set progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void IndexDocumentsProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount, e.ProcessedDocumentCount);
        }

    }
}
