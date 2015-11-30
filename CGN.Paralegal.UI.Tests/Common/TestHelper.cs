using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.UI.Tests.Common
{
    using System.Globalization;
    using System.Net.Http;

    using Newtonsoft.Json.Linq;

    public class TestHelper
    {
        private static string workflowStateURL = null;

        public static string UpdateWorkflowState(string workflowState)
        {
            HttpResponseMessage response = null; 
            string json = null;
            if (workflowStateURL == null)
            {
                response = HttpClientHelper.Execute(
                    Settings.BaseUrl + Settings.PCAppStateURL,
                    Method.Get);
                json = response.Content.ReadAsStringAsync().Result;
                var appState = JObject.Parse(json);

                workflowStateURL = String.Format(
                    CultureInfo.InvariantCulture,
                    Settings.PCWorkflowStateURL,
                    appState.GetValue("OrgId", StringComparison.Ordinal),
                    appState.GetValue("MatterId", StringComparison.Ordinal),
                    appState.GetValue("DatasetId", StringComparison.Ordinal),
                    appState.GetValue("ProjectId", StringComparison.Ordinal),
                    Guid.NewGuid().ToString());
            }

            response = HttpClientHelper.Execute(Settings.BaseUrl + workflowStateURL, Method.Put, JArray.Parse(workflowState));
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
