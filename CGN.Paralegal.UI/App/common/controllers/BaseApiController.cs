# region File Header

//-----------------------------------------------------------------------------------------
// <header>
//     <description>
//          This is a file that contains BaseApiController Web Api base class 
//      </description>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CGN.Paralegal.UI
{
    using System.Configuration;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using CGN.Paralegal.ClientContracts.Analytics;
    using CGN.Paralegal.ClientContracts.AppState;
    using CGN.Paralegal.Mocks;
    using CGN.Paralegal.UI.RestClient;

    /// <summary>
    /// Base Web Api Controller 
    /// </summary>
    public class BaseApiController : ApiController
    {
        /// <summary>
        /// Convert to JSON object from JSON string
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static JObject ConvertToJsonObjectFromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<JObject>(jsonString);
        }

        public static bool IsMockMode()
        {
            const string MockMode = "mock";
            const string RunMode = "RunMode";
            var mockSetting = ConfigurationManager.AppSettings.Get(RunMode);

            return MockMode.Equals(mockSetting);
        }

        public static IAnalyticsRestClient GetAnalyticsRestClient()
        {
            if (IsMockMode())
            {
                return new MockAnalyticsRestClient();
            }
            else
            {
                return new AnalyticsRestClient();
            }

        }
        
        public static IAppStateRestClient GetAppStateRestClient()
        {
            if (IsMockMode())
            {
                return new MockAppStateRestClient();
            }
            else
            {
                return new AppStateRestClient();
            }

        }
    }
}