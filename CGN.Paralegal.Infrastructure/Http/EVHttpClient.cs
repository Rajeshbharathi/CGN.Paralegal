//---------------------------------------------------------------------------------------------------
// <copyright file="EVHttpClient.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi/Visu</author>
//      <description>
//          Factory class for 'HttpClient'
//      </description>
//      <changelog>
//          <date value="5/4//2015">Changed to hold Token in Authorization Header</date>
//          <date value="11/13/2014">Included the change for Billing Report</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System.IO;

namespace CGN.Paralegal.Infrastructure.Http
{
    using System;
    using System.Linq;
    using System.Configuration;
    using System.Web;
    using System.Xml.Serialization;
    using EVContainer;
    using ExceptionManagement;
    using Infrastructure;
    using Microsoft.Http;
    using Microsoft.Http.Headers;
    
    /// <summary>
    /// Factory class for 'HttpClient'
    /// </summary>
    public static class EVHttpClient
    {
        #region Properties
        /// <summary>
        /// Get/Set RestClientforBrava
        /// </summary>
        public static HttpClient RestClientforNearNative
        {
            get;
            set;
        }
        #endregion

        #region Member functions


        const string HTTP_SEND = "HttpSend";

        // -- get the http interface to call stub/real decided in config file
        static IEVHttpSend http = EVUnityContainer.Resolve<IEVHttpSend>(HTTP_SEND);


        /// <summary>
        /// GetHttpClient function return a HttpClient that has the cookie set in
        /// header so that the session is authorized by the service
        /// </summary>
        /// <returns>HttpClient object</returns>
        public static HttpClient GetHttpClient()
        {
            HttpClient httpClient = GetHttpClientInternal();

            string correlationId = System.Diagnostics.Trace.CorrelationManager.ActivityId.ToString();
            httpClient.DefaultHeaders["CorrelationId"] = correlationId;

            return httpClient;
        }

        private static HttpClient GetHttpClientInternal()
        {
            if (HttpContext.Current != null && HttpContext.Current.Items.Contains(Constants.UserGUID))
            {
                if (RestClientforNearNative != null)
                {
                    return RestClientforNearNative;
                }
                SetHttpClient();
                return RestClientforNearNative;
            }
            return GetDefaultClient();
        }

        /// This is used for authenticating Jobs/Near Native API calls.
        private static void SetHttpClient()
        {
            string UserGUID = string.Empty;
            UserGUID = Convert.ToString(HttpContext.Current.Items[Constants.UserGUID]);
            if (!string.IsNullOrEmpty(UserGUID))
            {
                string uri = ConfigurationManager.AppSettings.Get(Constants.UserService) + Constants.User;
                AuthenticateByGuid(uri, UserGUID, Client.NearNative);
            }
        }

        /// <summary>
        /// This is used for authenticating Web Application API calls.
        /// </summary>
        private static HttpClient GetDefaultClient()
        {
            HttpClient httpClient = new HttpClient();
            //string strAuthValue = EVSessionManager.Get<string>(Constants.EvolutionCookieName);
            //if (strAuthValue != null)
            //{
                //Cookie objCookie = new Cookie();
                //objCookie.Add(Constants.EvolutionCookieName, strAuthValue);
                //// -- set the cookie in the header
                //httpClient.DefaultHeaders.Cookie.Add(objCookie);
                //httpClient.DefaultHeaders.Add(Constants.EvAuth, strAuthValue);
                ////Add authorization header
                //Credential evcredential = new Credential();
                //evcredential.Parameters.Add(Constants.Token + strAuthValue);
                //httpClient.DefaultHeaders.Authorization = evcredential;
                //httpClient.TransportSettings.ConnectionTimeout = TimeSpan.FromMinutes(60);
                //httpClient.TransportSettings.ReadWriteTimeout = TimeSpan.FromMinutes(60);
            //}
            return httpClient;
        }

        /// <summary>
        /// This is used for authenticating Jobs/Near Native API calls.
        /// </summary>
        // Suppressed until we can change / deprecate base job
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static HttpClient GetNearNativeClient()
        {
            HttpClient httpClient = null;

            string UserGUID = string.Empty;
            UserGUID = Convert.ToString(HttpContext.Current.Items[Constants.UserGUID]);
            if (!string.IsNullOrEmpty(UserGUID))
            {
                string uri = ConfigurationManager.AppSettings.Get(Constants.UserService) + Constants.User;
                httpClient = AuthenticateByGuid(uri, UserGUID);
            }

            return httpClient;
        }

        /// <summary>
        /// GetHttpClient function return a HttpClient that has the cookie set in
        /// header so that the session is authorized by the service and the range to be used for pagination
        /// </summary>
        /// <param name="range">Range specified as a string array, used for pagination</param>
        /// <returns>HttpClient object</returns>         
        public static HttpClient GetHttpClient(string[] range)
        {
            HttpClient httpClient = null;
            httpClient = GetHttpClient();
            httpClient.DefaultHeaders.Add(Constants.DataRange, range);
            return httpClient;
        }

        /// <summary>
        /// This is a special method for authentication
        /// but the request and response are type independent as it is
        /// in the generic EVHttpClient class
        /// </summary>
        /// <typeparam name="TResponse">Response Type</typeparam>
        /// <param name="uri">Uri of stubbed filename/Rest webservice</param>
        /// <param name="httpClient">HttpClient object</param>
        /// <param name="sessionId">SessionId object</param>
        /// <returns>TResponse TypeParam</returns>
        public static object Authenticate(string uri, HttpClient httpClient, Type entityType, ref string sessionId)
        {
            const string SESSION_ID = "ASP.Net_SessionId";
            string xml = string.Empty;

            HttpResponseMessage httpResp = null;
            HeaderValues<Cookie> myCookies;

            httpResp = httpClient.Post(uri, HttpContentExtensions.CreateXmlSerializable(string.Empty));

            if (!httpResp.IsStatusIsSuccessful())
            {
                throw new EVException().AddHttpResponse(httpResp);
            }

            // -- read the content as an xml string
            xml = httpResp.Content.ReadAsString();

            // -- get the cookies from the response headers
            myCookies = httpResp.Headers.SetCookie;

            if (myCookies != null)
                foreach (Cookie var in myCookies.Where(var => var.ContainsKey(SESSION_ID)))
                {
                    // -- set the session ID
                    sessionId = var[SESSION_ID];
                    break;
                }
            return XmlUtility.DeserializeObject(xml, entityType);
        }

        // Suppressed until we can change / deprecate base job
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "client")]
        public static void AuthenticateByGuid(string uri, string securityToken, Client client)
        {
            HttpClient httpClient =  AuthenticateByGuid(uri, securityToken);

            switch (client)
            {
                case Client.NearNative:
                    RestClientforNearNative = httpClient;
                    break;
            }
        }

        /// <summary>
        /// Authenticates the by GUID.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="securityToken">The security token.</param>
        private static HttpClient AuthenticateByGuid(string uri, string securityToken)
        {
            const string SESSION_ID = "ASP.Net_SessionId";
            const string UserGuid = "UserGuid";
            //Stores the sessionid generated and returned by the server
            string sessionId = string.Empty;
            HttpResponseMessage httpResp = null;
            HeaderValues<Cookie> myCookies;
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultHeaders.Add(UserGuid, securityToken);
            httpResp = httpClient.Post(uri, HttpContentExtensions.CreateXmlSerializable(string.Empty));
            if (!httpResp.IsStatusIsSuccessful())
            {
                throw new EVException().AddHttpResponse(httpResp);
            }
            myCookies = httpResp.Headers.SetCookie;
            if (myCookies != null)
                foreach (Cookie var in myCookies.Where(var => var.ContainsKey(SESSION_ID)))
                {
                    // -- set the session ID
                    sessionId = var[SESSION_ID];
                    break;
                }
            Cookie objCookie = new Cookie();
            objCookie.Add(Constants.EvolutionCookieName, sessionId);
            // -- set the cookie in the header
            httpClient.DefaultHeaders.Cookie.Add(objCookie);
            //Add authorization header
            Credential evcredential = new Credential();
            evcredential.Parameters.Add(sessionId);
            httpClient.DefaultHeaders.Authorization = evcredential;
            httpClient.TransportSettings.ConnectionTimeout = TimeSpan.FromMinutes(60);
            httpClient.TransportSettings.ReadWriteTimeout = TimeSpan.FromMinutes(60);
            return httpClient;

            
        }

        /// <summary>
        /// This is a generic Send HTTP methods
        /// Uses unity to resolve whether to use Stub or to use the actual call.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="method"></param>
        /// <param name="uri"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static TResponse Send<TRequest, TResponse>(HttpMethod method, string uri, TRequest obj) where TRequest : class
        {
            return http.Send<TRequest, TResponse>(method, uri, obj);
        }
        /// <summary>
        /// A generic Get method to fetch the stream from the URI
        /// </summary>
        /// <param name="uri">The Source URL from where Stream can be downloaded</param>
        /// <returns></returns>
        public static Stream GetStream(string uri)
        {
            return GetStream<object>(HttpMethod.GET, uri, null);
        }
        /// <summary>
        /// A Get method to fetch the stream from the URI
        /// </summary>
        /// <typeparam name="TRequest">The Request Object to pass to</typeparam>
        /// <param name="method">HTTP Method</param>
        /// <param name="uri">The Source URL from where Stream can be downloaded</param>
        /// <param name="obj">The input object which should be passed as part of request</param>
        /// <returns></returns>
        public static Stream GetStream<TRequest>(HttpMethod method, string uri, TRequest obj) where TRequest : class
        {
            return http.GetStream(method,uri,obj);
        }

        /// <summary>
        /// POST command with a generic Request and Response objects
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/Rest webservice</param>
        /// <param name="obj">Request typeparam</param>
        /// <returns>Response typeparam</returns>
        public static TResponse Post<TRequest, TResponse>(string uri, TRequest obj) where TRequest : class
        {
            return Send<TRequest, TResponse>(HttpMethod.POST, uri, obj);
        }

        /// <summary>
        /// POST command with a generic Request and HTTPResponse objects
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/Rest webservice</param>
        /// <param name="obj">Request typeparam</param>   
        // Added for calling methods that do updates but returns nothing
        public static void Post<TRequest>(string uri, TRequest obj) where TRequest : class
        {
            HttpResponseMessage httpResp = Send<TRequest, HttpResponseMessage>(HttpMethod.POST, uri, obj);
            if (!httpResp.IsStatusIsSuccessful())
            {
                throw new EVException().AddHttpResponse(httpResp);
            }
        }

        /// <summary>
        /// This is a generic HTTP PUT method which takes a URI and generic object
        /// and returns a generic object
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/rest webservice</param>
        /// <param name="obj">Request typeparam</param>
        /// <returns>Response typeparam</returns>
        public static TResponse Put<TRequest, TResponse>(string uri, TRequest obj) where TRequest : class
        {
            return Send<TRequest, TResponse>(HttpMethod.PUT, uri, obj);
        }



        /// <summary>
        /// Puts the specified URI.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="obj">The obj.</param>
        public static void Put<TRequest>(string uri, TRequest obj)
        {
            HttpResponseMessage httpResp = GetHttpClient().Put(uri, HttpContentExtensions.CreateXmlSerializable<TRequest>(obj));
            if (!httpResp.IsStatusIsSuccessful())
            {
                throw new EVException().AddHttpResponse(httpResp);
            }
        }


        /// <summary>
        /// A generic HTTP Delete method which will
        /// get a URI and return a bool for success or failure
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/Rest webservice</param>
        /// <returns>Response typeparam</returns>
        public static TResponse Delete<TResponse>(string uri)
        {
            return Send<object, TResponse>(HttpMethod.DELETE, uri, null);
        }
        
        /// <summary>
        /// A generic HTTP Delete method which will
        /// get a URI and return a bool for success or failure
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/Rest webservice</param>
        /// <param name="obj">Request typeparam</param>
        /// <returns>Response typeparam</returns>
        public static TResponse Delete<TRequest, TResponse>(string uri, TRequest obj) where TRequest : class
        {
            return Send<TRequest, TResponse>(HttpMethod.DELETE, uri, obj);
        }

        /// <summary>
        /// A generic GET command takes a URI and returns
        /// xml deserialized to an object
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/rest webservice</param>
        /// <returns>Response typeparam</returns>
        public static TResponse Get<TResponse>(string uri)
        {
            return Send<object, TResponse>(HttpMethod.GET, uri, null);
        }

        /// <summary>
        /// A generic GET command takes a URI and range attribute in header and returns
        /// xml deserialized to an object
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="uri">Uri of stubbed filename/rest webservice</param>
        /// <param name="range">Range specified as a string array, used for pagination</param>
        /// <returns>Response typeparam</returns>
        public static TResponse Get<TResponse>(string uri, string[] range)
        {
            HttpResponseMessage httpResp = null;

            // -- Get the Response
            httpResp = GetHttpClient(range).Send(HttpMethod.GET, uri);
            try
            {
                if (!httpResp.IsStatusIsSuccessful())
                {
                    throw new EVException().AddHttpResponse(httpResp);
                }
                // -- return the xml serializable content
                return httpResp.Content.ReadAsXmlSerializable<TResponse>();
            }
            catch (Exception ex)
            {
                ex.AddResMsg(ErrorCodes.HttpContentSerializeException);
                ex.AddDbgMsg("HttpResponseContent = " + httpResp.Content.ReadAsString());
                throw;
            }
        }

        /// <summary>
        /// A generic HTTP Delete method which will
        /// get a URI and throws exception if failed
        /// </summary>
        /// <param name="uri">Uri of stubbed filename/rest webservice</param>   
        // Added for calling methods that do delete but returns nothing 
        public static void Delete(string uri)
        {
            HttpResponseMessage httpResp = GetHttpClient().Delete(uri);

            if (!httpResp.IsStatusIsSuccessful())
            {
                throw new EVException().AddHttpResponse(httpResp);
            }
        }

        #endregion
    }

    public enum Client { Job = 0, NearNative = 1 }
}
