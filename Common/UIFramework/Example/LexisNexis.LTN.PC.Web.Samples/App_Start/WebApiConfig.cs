using System.Web.Http;
using System.Web.Http.OData.Builder;
using LexisNexis.LTN.PC.Web.Samples.Models;
using Newtonsoft.Json;

namespace LexisNexis.LTN.PC.Web.Samples
{
    public static class WebApiConfig
    {
        /// <summary>
        /// Registers the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            //OData
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Project>("ProjectsOData");
            builder.EntitySet<DocumentSet>("DocumentSets");
            config.Routes.MapODataRoute("odata", "odata", builder.GetEdmModel());

            //JSON serialization
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}