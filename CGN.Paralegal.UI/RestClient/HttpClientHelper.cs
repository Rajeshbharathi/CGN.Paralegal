using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json.Linq;

namespace CGN.Paralegal.UI.RestClient
{
    public class HttpClientHelper
    {
        private const string SessionCookieName = "ASP.Net_SessionId";
        private const string AuthorizationScheme = "Token";
        private const string ContentType = "application/json";

        /// <summary>
        /// Executes the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="method">The method.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static HttpResponseMessage Execute(string uri, Method method, object model = null)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler))
            {
                Authorization(client, cookieContainer, sessionId);
                return SendRequest(client, uri, method, model);
            }
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="method">The method.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        private static HttpResponseMessage SendRequest(HttpClient client, string uri, Method method, object model = null)
        {
            HttpResponseMessage response = null;
            switch (method)
            {
                case Method.Get:
                    response = client.GetAsync(uri).Result;
                    break;
                case Method.Post:
                    response = client.PostAsJsonAsync(uri, model).Result;
                    break;
                case Method.Put:
                    response = client.PutAsJsonAsync(uri, model).Result;
                    break;
                case Method.Delete:
                    response = client.DeleteAsync(uri).Result;
                    break;
            }

            AddCacheControlHeader(response);
            if (response != null && response.IsSuccessStatusCode) return response;
            throw new HttpResponseException(response);
        }

        /// <summary>
        /// Authorizations the specified client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="cookieContainer">The cookie container.</param>
        /// <param name="sessionId">The session identifier.</param>
        private static void Authorization(HttpClient client, CookieContainer cookieContainer, string sessionId)
        {
            client.DefaultRequestHeaders.Add("Accept", ContentType);
            //Authentication token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationScheme, sessionId);
            var domain = GetDomainName();
            cookieContainer.Add(new Uri(domain), new Cookie(SessionCookieName, sessionId));
        }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <returns></returns>
        private static string GetSessionId()
        {
            return HttpContext.Current.Session[SessionCookieName] != null
                ? (string) HttpContext.Current.Session[SessionCookieName]
                : null;
        }

        /// <summary>
        /// Gets the name of the domain.
        /// </summary>
        /// <returns></returns>
        private static string GetDomainName()
        {
            return HttpContext.Current.Request.Url.Scheme + Uri.SchemeDelimiter +
                         HttpContext.Current.Request.Url.Host +
                         (HttpContext.Current.Request.Url.IsDefaultPort
                             ? ""
                             : ":" + HttpContext.Current.Request.Url.Port);
        }

        /// <summary>
        /// Adds the no cache control header.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private static void AddCacheControlHeader(HttpResponseMessage response)
        {
            var cache = new CacheControlHeaderValue {NoCache = true, NoStore = true};
            response.Headers.CacheControl = cache;
        }
    }

    /// <summary>
    /// Enum for http request method
    /// </summary>
    public enum Method
    {
        Get,
        Post,
        Put,
        Delete
    }
}