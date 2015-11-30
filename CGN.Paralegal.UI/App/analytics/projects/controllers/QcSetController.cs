using System.Collections.Generic;
using System.Web.Http;
using CGN.Paralegal.ClientContracts.Analytics;

namespace CGN.Paralegal.UI
{
    public class QcSetController : BaseApiController
    {
        /// <summary>
        /// Posts the create qcset.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="qcSet">The qc set.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/qcset")]
        public QcSet PostCreateQcset(long orgId, long matterId, long datasetId, long projectId, QcSet qcSet)
        {
            var client = GetAnalyticsRestClient();
            qcSet.Status = Status.NotStarted;
            return client.CreateQcSet(matterId, datasetId, projectId, qcSet);
        }

        /// <summary>
        /// Get the non coded document flag.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/qcset/validate")]
        public bool GetQcSetPrerequisiteStatus(long orgId, long matterId, long datasetId, long projectId, QcSet qcSet)
        {
            var client = GetAnalyticsRestClient();
            return client.ValidateQcSetCreationPrerequisite(orgId,matterId, datasetId, projectId);
        }

        /// <summary>
        /// Gets the available document count for qc set.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/availabledoccount")]
        public long GetAvailableDocumentCount(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetAvailableDocumentCount(orgId, matterId, datasetId, projectId);
        }

        /// <summary>
        /// Gets qc sets detail info.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/qcSets")]
        public List<QcSet> GetQcSetsInfo(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetQcSetsInfo(orgId, matterId, datasetId, projectId);
        }
    }
}