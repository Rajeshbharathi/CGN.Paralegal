using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using CGN.Paralegal.UI.Log.Exception;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CGN.Paralegal.UI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApp",
                routeTemplate: "app/{module}/approot",
                defaults: new { controller = "App" }
            );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );
            config.Services.Add(typeof(IExceptionLogger), new WebExceptionLogger());

            //JSON serialization
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            json.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.Remove(config.Formatters.XmlFormatter);  
            
        }
    }
}