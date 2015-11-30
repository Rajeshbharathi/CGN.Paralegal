using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CGN.Paralegal.UI.RestClient;
using Newtonsoft.Json.Linq;

namespace CGN.Paralegal.UI
{
    using CGN.Paralegal.ClientContracts.Analytics;

    public class ControlsetController : BaseApiController
    {
        private const string ConfidenceLevel = "ConfidenceLevel";
        private const string MarginOfError = "MarginOfError";
        private const string Size = "size";


        /// <summary>
        /// Calculate Sample size
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="controlSet">Control Set</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/controlset/samplesize")]
        public int PostCalculateSampleSize(long orgId, long matterId, long datasetId, ControlSet controlSet)
        {
            var confidenceLevel = Convert.ToDouble(controlSet.ConfidenceLevel, CultureInfo.InvariantCulture)/100;
            var marginOfError = Convert.ToDouble(controlSet.MarginOfError, CultureInfo.InvariantCulture)/100;
            var client = GetAnalyticsRestClient();
            return client.GetControlsetSampleSize(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture), confidenceLevel.ToString(CultureInfo.InvariantCulture),
                marginOfError.ToString(CultureInfo.InvariantCulture));
            
        }

        /// <summary>
        /// Create ControlSet
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="controlSet">ControlSet</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/controlset")]
        public ControlSet PostCreateControlset(long orgId, long matterId, long datasetId, long projectId, ControlSet controlSet)
        {
            var client = GetAnalyticsRestClient();
            return client.CreateControlset(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture),
                controlSet);
        }

        /// <summary>
        ///  Create Job for categorize Controlset
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="trainingRound">training round</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/controlset/categorize/{trainingRound}")]
        public int PostCreateJobForCategorizeControlset(long orgId, long matterId, long datasetId, long projectId, long trainingRound)
        {
            var client = GetAnalyticsRestClient();
            return client.CreateJobForCategorizeControlset(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture),
                trainingRound.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Posts the create manual job for categorize controlset.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/controlset/categorize/manual")]
        public int PostCreateManualJobForCategorizeControlset(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.CreateManualJobForCategorizeControlset(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets the validate predict controlset job.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/controlset/categorize/validate")]
        public bool GetValidatePredictControlsetJob(long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.ValidatePredictControlsetJob(matterId, datasetId, projectId);
        }
    }
}