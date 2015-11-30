
using System.Collections.Generic;

namespace CGN.Paralegal.Mocks
{
    using ClientContracts.AppState;

    public class MockAppStateRestClient : IAppStateRestClient
    {
        private readonly AppState _appState = new AppState { OrgId = 1, MatterId = 1, DatasetId = 1, ProjectId = 1 };
        public AppState GetAppState()
        {
            _appState.UserGrops = new List<string> { "User Group 1", "User Group 2" };
            return _appState;
        }
    }
}