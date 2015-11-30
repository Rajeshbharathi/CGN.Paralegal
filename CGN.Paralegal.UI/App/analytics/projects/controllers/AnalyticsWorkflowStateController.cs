using System.Collections.Generic;
using System.Web.Http;
using CGN.Paralegal.ClientContracts.Analytics;

namespace CGN.Paralegal.UI
{
    

    public class AnalyticsWorkflowStateController : BaseApiController
    {
        /// <summary>
        /// Get Workflow State for Analytics Project
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analytics/workflow-state"),
        HttpGet]
        public List<AnalyticsWorkflowState> GetWorkflowState(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetAnalyticWorkflowState(matterId, datasetId, projectId);
        }

        /// <summary>
        /// Update Workflow State for Analytics Project
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="binderId">The binder identifier.</param>
        /// <param name="state">workflow state.</param>
        /// <returns>
        /// Updated state
        /// </returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysisset/{binderid}/analytics/workflow-state"),
        HttpPut]
        public List<AnalyticsWorkflowState> PutWorkflowState(long orgId, long matterId, long datasetId, long projectId, string binderId, List<AnalyticsWorkflowState> state)
        {
            var client = GetAnalyticsRestClient();
            return client.UpdateAnalyticWorkflowState(matterId, datasetId, projectId, binderId, state);
        }

        /// <summary>
        /// Gets the state of the next workflow.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analytics/changed-workflow-state"),
        HttpGet]
        public AnalyticsWorkflowState GetChangedWorkflowState(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetChangedWorkflowState(matterId, datasetId, projectId);
        }
    }
}