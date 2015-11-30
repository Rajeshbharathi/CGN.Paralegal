# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="WebApiConfig.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Henry Chen</author>
//      <description>
//          This is a file that contains config rules for web api
//      </description>
//      <changelog>
//          <date value="01/28/2015">initial version</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using Newtonsoft.Json;
using System.Web.Http;
using Newtonsoft.Json.Converters;

namespace LexisNexis.LTN.Administration
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );
            
            //JSON serialization
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            json.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}