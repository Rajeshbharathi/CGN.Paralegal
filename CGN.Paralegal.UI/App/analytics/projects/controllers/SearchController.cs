# region File Header

//-----------------------------------------------------------------------------------------
// // <header>
//      <description>
//          This is a file that contains SearchController Web Api class 
//      </description>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System.Collections.Generic;
using System.Globalization;
using System.Web.Http;

using Newtonsoft.Json.Linq;

namespace CGN.Paralegal.UI
{
    using CGN.Paralegal.ClientContracts.Search;
    using CGN.Paralegal.ClientContracts.Analytics;

    public class SearchController : BaseApiController
    {
        /// <summary>
        /// Get saved searches
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/search/{searchkeyword}")]
        public List<ParaLegalProfile> GetSearchList(string searchkeyword)
        {
            var client = GetAnalyticsRestClient();
            var searchList = client.GetSearchList(searchkeyword);
            return searchList;

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/search/reviews/{paralegalid}")]
        public List<string> GetReviewList(int paralegalid)
        {
            var client = GetAnalyticsRestClient();
            var searchList = client.GetReviewList(paralegalid);
            return searchList;

        }

        /// <summary>
        /// Get saved searches
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/search/paralegal/{paralegalid}")]
        public ParaLegalProfile GetParalegalDetails(int paralegalid)
        {
            var client = GetAnalyticsRestClient();
            var paralegalDetails = client.GetParalegalDetails(paralegalid);
            return paralegalDetails;

        }
        /// <summary>
        /// Get saved searches
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/search/aop/top10")]
        public List<AreaOfPractise> GetTopTenAOP()
        {
            var client = GetAnalyticsRestClient();
            var toptenaop = client.GetTopTenAOP();
            return toptenaop;

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/search/city/top10")]
        public List<Location> GetTopTenCity()
        {
            var client = GetAnalyticsRestClient();
            var toptencity = client.GetTopTenCity();
            return toptencity;

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/search/paralegal/top10")]
        public List<PLDetail> GetTopTenParaLegal()
        {
            var client = GetAnalyticsRestClient();
            var toptenparalegal = client.GetTopTenParaLegal();
            return toptenparalegal;

        }


        /// <summary>
        /// Get saved searches
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/project/savedsearches")]
        public List<SavedSearch> GetSavedSearches(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            var savedSearches = client.GetSavedSearches(orgId.ToString(CultureInfo.InvariantCulture), matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture)
                );

            if (savedSearches != null && savedSearches.Count > 0)
            {
            foreach (var s in savedSearches)
            {
                s.MatterId = matterId.ToString(CultureInfo.InvariantCulture);
                s.DatasetId = datasetId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return savedSearches;

        }

        /// <summary>
        /// Get Search count
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="documentSelection">documentSelection</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/documentcounts")]
        [HttpPost]
        public long GetSearchCount(long orgId, long matterId, long datasetId, long projectId, JObject documentSelection)
        {
            var client = GetAnalyticsRestClient();
            return client.GetSearchCount(
                orgId.ToString(CultureInfo.InvariantCulture), 
                matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture),
                documentSelection
                );  
        }
    }


}