using System.Web.Http;

using CGN.Paralegal.UI.RestClient;

namespace CGN.Paralegal.UI
{
    using CGN.Paralegal.ClientContracts.AppState;

    public class AppStateController : BaseApiController
    {
        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), Route("api/appstate")]
        public AppState GetAppState()
        {
            var appState = GetAppStateRestClient();
            return appState.GetAppState();
        }
    }
}
