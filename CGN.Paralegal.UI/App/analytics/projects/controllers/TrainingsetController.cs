using System.Globalization;
using System.Web.Http;

namespace CGN.Paralegal.UI
{
    public class TrainingsetController : BaseApiController
    {
        /// <summary>
        /// Create ControlSet
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="previousRound">The previous round.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/trainingset/{previousRound?}")]
        public string PostCreateTrainingset(long orgId, long matterId, long datasetId, long projectId, long previousRound=0)
        {
            var client = GetAnalyticsRestClient();
            
            //Create training set
            var binderId = client.CreateTrainingset(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture));

            return binderId;
        }

        /// <summary>
        /// Create Job for categorize analysisset
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSetType">The analysis set type</param>
        /// <param name="binderId">The binder identifier.</param>
        /// <param name="trainingRound">training round</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysisSetType/{analysisSetType}/binderid/{binderId}/categorize/{trainingRound}")]
        public int PostCreateJobForCategorizeTrainingset(long orgId, long matterId, long datasetId, long projectId, 
            string analysisSetType, string binderId, long trainingRound)
        {
            var client = GetAnalyticsRestClient();
            return client.CreateJobForCategorizeAnalysisset(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture),
                analysisSetType,
                binderId.ToString(CultureInfo.InvariantCulture),
                trainingRound.ToString(CultureInfo.InvariantCulture));
        }
    }
}