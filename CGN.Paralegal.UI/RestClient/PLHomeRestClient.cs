using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;

namespace CGN.Paralegal.UI.RestClient
{
    using CGN.Paralegal.ClientContracts.AppState;

    public class AppStateRestClient : IAppStateRestClient
    {
        private const string SessionMatterIdentifier = "PC_MATTERID";
        private const string SessionDatasetIdentifier = "PC_DATASETID";
        private const string SessionProjectIdentifier = "PC_PROJECTID";

        /// <summary>
        /// Gets the state of the application.
        /// </summary>
        /// <returns></returns>
        public AppState GetTopcities()
        {
            var appState = new AppState
            {
                OrgId = GetOrgIdFromContext(),
                MatterId = GetMatterIdFromContext(),
                DatasetId = GetDatasetIdFromContext(),
                ProjectId = GetProjectIdFromContext()
            };
            appState.UserGrops = GetAuthorizeduserGroups(appState.MatterId, appState.DatasetId, appState.ProjectId);
            return appState;
        }

        /// <summary>
        /// Gets the matter identifier from context.
        /// </summary>
        /// <returns></returns>
        private static long GetMatterIdFromContext()
        {
            long matterId = 0;
            if (HttpContext.Current.Session[SessionMatterIdentifier] != null)
            {
                matterId = Convert.ToInt64(HttpContext.Current.Session[SessionMatterIdentifier], CultureInfo.InvariantCulture);
            }
            return matterId;
        }

        /// <summary>
        /// Gets the org identifier from context.
        /// </summary>
        /// <returns></returns>
        private static long GetOrgIdFromContext()
        {
            //TODO: Return OrgId from context; To be implemented if needed later 
            return 1;
        }

        /// <summary>
        /// Gets the dataset identifier from context.
        /// </summary>
        /// <returns></returns>
        private static long GetDatasetIdFromContext()
        {
            long datasetId = 0;
            if (HttpContext.Current.Session[SessionDatasetIdentifier] != null)
            {
                datasetId = Convert.ToInt64(HttpContext.Current.Session[SessionDatasetIdentifier], CultureInfo.InvariantCulture);
            }
            return datasetId;
        }

        /// <summary>
        /// Gets the project identifier from context.
        /// </summary>
        /// <returns></returns>
        private static long GetProjectIdFromContext()
        {
            long projectId = 0;
            if (HttpContext.Current.Session[SessionProjectIdentifier] != null)
            {
                projectId = Convert.ToInt64(HttpContext.Current.Session[SessionProjectIdentifier], CultureInfo.InvariantCulture);
            }
            return projectId;
        }

        /// <summary>
        /// Gets the authorizeduser groups.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        private static List<string> GetAuthorizeduserGroups(long matterId, long datasetId, long projectId)
        {
            return AnalyticsRestClient.GetAutherizedUserGroups(matterId, datasetId, projectId);
        }
    }
}