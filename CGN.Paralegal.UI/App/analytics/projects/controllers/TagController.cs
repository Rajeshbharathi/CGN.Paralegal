# region File Header

//-----------------------------------------------------------------------------------------
//<header>
//      <description>
//          This is a file that contains TagController Web Api class 
//      </description>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System.Collections.Generic;
using System.Globalization;
using System.Web.Http;

using CGN.Paralegal.UI.RestClient;

namespace CGN.Paralegal.UI
{
    using CGN.Paralegal.ClientContracts.Analytics;

    public class TagController : BaseApiController
    {
        /// <summary>
        /// Get Project Tags
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/project/tags")]
        public List<Tag> GetAnalyticProjectTags(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            var tags = client.GetAnalyticProjectTags(orgId.ToString(CultureInfo.InvariantCulture),
                matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture));

            if (tags != null && tags.Count > 0)
            {
            foreach (var t in tags)
            {
                t.MatterId = matterId.ToString(CultureInfo.InvariantCulture);
                t.DatasetId = datasetId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return tags;
        }
    }
}