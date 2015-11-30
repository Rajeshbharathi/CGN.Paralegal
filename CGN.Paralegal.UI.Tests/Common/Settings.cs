namespace CGN.Paralegal.UI.Tests
{
    using System;
    using System.Configuration;
    using System.Globalization;

    internal class Settings
    {
        private Settings()
        {
        }

        internal static int WaitTimeOut = Convert.ToInt32(ConfigurationManager.AppSettings["LoadWaitTimeOut"], CultureInfo.InvariantCulture);
        internal static int ResilienceCount = Convert.ToInt32(ConfigurationManager.AppSettings["ResilienceCount"], CultureInfo.InvariantCulture);
        internal static int ImplicitWait = Convert.ToInt32(ConfigurationManager.AppSettings["ImplicitWait"], CultureInfo.InvariantCulture);
        internal static string Host = ConfigurationManager.AppSettings["Host"];
        internal static int Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"], CultureInfo.InvariantCulture);

        internal static string PCCreateProjectUrl = ConfigurationManager.AppSettings["PCCreateProjectURL"];

        //TODO un-comment these settings when we start using them in the code. I commented them to avoid code analysis errors
        internal static string PCAdminDashboardUrl = ConfigurationManager.AppSettings["PCAdminDashboardURL"];
        internal static string PCReviewDashboardUrl = ConfigurationManager.AppSettings["PCReviewDashboardURL"];
        //internal static string PCReviewerUrl = ConfigurationManager.AppSettings["PCReviewerURL"];
        //internal static string PCDocListUrl = ConfigurationManager.AppSettings["PCDocListURL"];
        //internal static string PCControlSetReviewURL = ConfigurationManager.AppSettings["PCControlSetReviewURL"];

        internal static string PCAppStateURL = ConfigurationManager.AppSettings["PCAppStateURL"];
        internal static string PCWorkflowStateURL = ConfigurationManager.AppSettings["PCWorkflowStateURL"];

        public static string BaseUrl
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}:{1}/", Host, Port);
            }
        }


    }
}