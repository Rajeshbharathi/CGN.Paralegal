using CGN.Paralegal.Services;

using Microsoft.Owin;

[assembly: OwinStartup(typeof(OwinStartup))]

namespace CGN.Paralegal.Services
{
    using Microsoft.AspNet.SignalR;

    using Owin;

    public class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=316888
            var hubConfiguration = new HubConfiguration { EnableDetailedErrors = true };

            app.MapSignalR(hubConfiguration);
        }
    }
}
