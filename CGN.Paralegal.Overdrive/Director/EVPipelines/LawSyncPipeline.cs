using System;
using System.Globalization;
using System.IO;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.External.DataAccess;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.BusinessEntities.Common;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Overdrive
{
    public class LawSyncPipeline : EVPipeline
    {
        protected long LawSynJobId { get; set; }
        protected long MatterId { get; set; }
        protected bool SyncImage { get; set; }
        protected long LawCaseId { get; set; }

        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            LawSyncBEO jobParameter=null;
            base.SetPipelineTypeSpecificParameters(activeJob);
            switch (JobTypeId)
            {
                case 40:
                    jobParameter = (LawSyncBEO)XmlUtility.DeserializeObject(activeJob.BootParameters.ToString(), typeof(LawSyncBEO));
                    break;
                case 41:
                {
                    var reprocessJobParameter = (ConversionReprocessJobBeo)XmlUtility.DeserializeObject(activeJob.BootParameters.ToString(), typeof(ConversionReprocessJobBeo));

                    var baseConfig = ReconversionDAO.GetJobConfigInfo(Convert.ToInt32(reprocessJobParameter.OrginialJobId));
                    jobParameter = (LawSyncBEO)XmlUtility.DeserializeObject(baseConfig.BootParameters, typeof(LawSyncBEO));
                }
                    break;
            }
            if (jobParameter == null) return;
            MatterId = jobParameter.MatterId;
            SyncImage = jobParameter.IsProduceImage;
            LawCaseId = jobParameter.LawCaseId;
        }



        protected override bool Completed()
        {
            base.Completed();
            try
            {
                var lawSyncSummary = LawBO.GetLawSyncProcessSetJobSummary(MatterId, JobId);

                var jobSummaryKeyValuePairs = new EVKeyValuePairs();
                jobSummaryKeyValuePairs.Set("ImageFolder", SyncImage ? GetImageLocation() : "N/A");
                jobSummaryKeyValuePairs.Set("ImageSyncFailureCount",
                    SyncImage ? lawSyncSummary.ImageSyncFailureCount.ToString(CultureInfo.InvariantCulture) : "N/A");
                jobSummaryKeyValuePairs.Set("MetadatSyncFailureCount",
                    lawSyncSummary.MetadataSyncFailureCount.ToString(CultureInfo.InvariantCulture));
                jobSummaryKeyValuePairs.Set("TotalDocument",
                    (lawSyncSummary.LawSyncSuceessCount + lawSyncSummary.LawSyncFailureCount).ToString(
                        CultureInfo.InvariantCulture));


                JobMgmtBO.UpdateJobResult(JobId, lawSyncSummary.LawSyncSuceessCount, lawSyncSummary.LawSyncFailureCount,
                    jobSummaryKeyValuePairs);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
            return true;
        }

        /// <summary>
        /// Get image location
        /// </summary>
        /// <returns></returns>
        private string GetImageLocation()
        {
            var lawEvAdapter = LawBO.GetLawAdapter(LawCaseId);
            var imageArchiveDirector = lawEvAdapter.GetImageArchiveDirectory();
            return Path.Combine(imageArchiveDirector, "EVImages", string.Format("Job{0}", JobId));
        }


    }
}
