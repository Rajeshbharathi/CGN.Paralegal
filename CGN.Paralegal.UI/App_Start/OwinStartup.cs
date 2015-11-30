using Microsoft.Owin;
using NLog;
using Owin;
using System;
using System.Configuration;

[assembly: OwinStartup(typeof(CGN.Paralegal.UI.OwinStartup))]

namespace CGN.Paralegal.UI
{
    using Microsoft.AspNet.SignalR;

    public class OwinStartup
    {
        private const string AspStateConStr = "ASPStateConnection";
        private static readonly Logger logger = LogManager.GetLogger("PCWeb");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void Configuration(IAppBuilder app)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[AspStateConStr].ConnectionString;
                GlobalHost.DependencyResolver.UseSqlServer(connectionString);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Failed to setup SignalR SQL backplane.", ex.GetBaseException());
            }

            var hubConfiguration = new HubConfiguration { EnableDetailedErrors = true };

            app.MapSignalR(hubConfiguration);
        }
    }
}
