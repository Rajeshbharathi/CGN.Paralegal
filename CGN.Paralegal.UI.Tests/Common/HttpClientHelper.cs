namespace CGN.Paralegal.UI.Tests.Common
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Web;
    using System.Web.Http;

    using Newtonsoft.Json.Linq;

    public class HttpClientHelper
    {
        private const string ContentType = "application/json";

        /// <summary>
        /// Executes the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="method">The method.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static HttpResponseMessage Execute(string uri, Method method, JArray data = null)
        {
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept", ContentType);

                return SendRequest(client, uri, method, data);
            }
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="method">The method.</param>
        /// <param name="data">The request body</param>
        /// <returns></returns>
        private static HttpResponseMessage SendRequest(HttpClient client, string uri, Method method, JArray data = null)
        {
            HttpResponseMessage response = null;
            switch (method)
            {
                case Method.Get:
                    response = client.GetAsync(uri).Result;
                    break;
                case Method.Post:
                    response = client.PostAsJsonAsync(uri, data).Result;
                    break;
                case Method.Put:
                    response = client.PutAsJsonAsync(uri, data).Result;
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
