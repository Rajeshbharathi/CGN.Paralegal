using System.Globalization;
using System.Web;
using System.Web.Http;

namespace CGN.Paralegal.UI
{
    using ClientContracts.Analytics;

    public class ProjectController : BaseApiController
    {
        private const string SessionAdminTree = "AdminUserFolders";
        private const float ConfidenceDefaultValue = 1;
        private const float MarginOfErrorDefaultValue = 1;
        private const float OverturnErrorThresholdValue = 2;
        private const float TargetF1DefaultValue = 3;
        private const float TargetPrecisionDefaultValue = 4;
        private const float TargetRecallDefaultValue = 5;
        /// <summary>
        /// Get Project details
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/project")]
        public AnalyticsProjectInfo GetProject(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetAnalyticProject(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Create Project
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="projectInfo">The project info.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/project")]
        public AnalyticsProjectInfo PostCreateProject(long orgId, long matterId, long datasetId, long projectId, AnalyticsProjectInfo projectInfo)
        {           
            //Set Project default values..
            projectInfo.Confidence = ConfidenceDefaultValue;
            projectInfo.MarginOfError = MarginOfErrorDefaultValue;
            projectInfo.OverturnErrorThreshold = OverturnErrorThresholdValue;
            projectInfo.TargetF1 = TargetF1DefaultValue;
            projectInfo.TargetPrecision = TargetPrecisionDefaultValue;
            projectInfo.TargetRecall = TargetRecallDefaultValue;
            projectInfo.MatterId = matterId;
            projectInfo.DatasetId = datasetId;
            projectInfo.Id =  System.Convert.ToInt32(projectId);

            var client = GetAnalyticsRestClient();
            HttpContext.Current.Session.Remove(SessionAdminTree);
            return client.CreateAnalyticProject(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture), projectInfo);
        }

        /// <summary>
        /// Deletes the project.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}")]
        public void DeleteProject(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            HttpContext.Current.Session.Remove(SessionAdminTree);
            client.DeleteAnalyticProject(matterId, datasetId, projectId);
        }

        /// <summary>
        ///  Create Job for categorize all documents
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="jobScheduleInfo">Job Schedule Info </param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/categorize")]
        public int PostCreateJobForCategorizeAll(long orgId, long matterId, long datasetId, long projectId, JobScheduleInfo jobScheduleInfo)
        {
            var client = GetAnalyticsRestClient();
            return client.CreateJobForCategorizeAll(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture),
                projectId.ToString(CultureInfo.InvariantCulture),
                jobScheduleInfo);
        }
      

        /// <summary>
        /// Validate create project info
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="projectInfo">The project info.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/project/validate")]
        public AnalyticsProjectInfo PostValidateCreateProjectInfo(long orgId, long matterId, long datasetId, long projectId, AnalyticsProjectInfo projectInfo)
        {
          
            projectInfo.MatterId = matterId;
            projectInfo.DatasetId = datasetId;

            var client = GetAnalyticsRestClient();
            return client.ValidateCreateProjectInfo(matterId.ToString(CultureInfo.InvariantCulture),
                datasetId.ToString(CultureInfo.InvariantCulture), projectInfo);
        }
      
    }
}