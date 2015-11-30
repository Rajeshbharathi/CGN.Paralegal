namespace CGN.Paralegal.ClientContracts.AppState
{
    public interface IAppStateRestClient
    {
        /// <summary>
        /// Gets the App state data
        /// </summary>
        /// <returns></returns>
        AppState GetAppState();
    }
}