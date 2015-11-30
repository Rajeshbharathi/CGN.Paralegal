# region File Header

//-----------------------------------------------------------------------------------------
// <header>
//      <description>
//          This is a file that contains CategorizeController Web Api class 
//      </description>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System.Collections.Generic;
using System.Web.Http;

using CGN.Paralegal.ClientContracts.Analytics;

namespace CGN.Paralegal.UI
{
    public class CategorizeController : BaseApiController
    {
        /// <summary>
        /// Get saved searches
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
         Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/" +
               "analysisSetTypes/{setType}/analysisSets/{setId}/categorization/scores")]
        public List<PredictionScore> GetPredictionScores(long orgId, long matterId, long datasetId, long projectId, string setType, string setId)
        {
            var client = GetAnalyticsRestClient();
            var scores = client.GetPredictionScores(orgId, matterId, datasetId, projectId, setType, setId);

            return scores;
        }

        /// <summary>
        /// Get categorization discrepancies
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
         Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/" +
               "analysisSetTypes/{setType}/analysisSets/{setId}/categorization/discrepancies")]
        public List<List<int>> GetDiscrepancies(long orgId, long matterId, long datasetId, long projectId, string setType, string setId)
        {
            var client = GetAnalyticsRestClient();
            var results = client.GetDiscrepancies(orgId, matterId, datasetId, projectId, setType, setId);

            return results;
        }
    }
}