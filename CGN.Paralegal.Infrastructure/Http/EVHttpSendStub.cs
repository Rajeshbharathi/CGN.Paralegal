#region File Header
//---------------------------------------------------------------------------------------------------
// <copyright file="EVHttpSendStub.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi/Visu</author>
//      <description>
//          This class provides the EVHttpSendStub for EVConcordance app
//      </description>
//      <changelog>
//          <date value=""></date>
//          <date value="11/13/2014">Included the change for Billing Report</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using CGN.Paralegal.Infrastructure.ExceptionManagement;
using Microsoft.Http;


namespace CGN.Paralegal.Infrastructure.Http
{
    public class EVHttpSendStub : IEVHttpSend
    {
        #region Member variable

        /// <summary>
        /// This is a stub directory where if exists the data will be dumped
        /// </summary>
        private static string _stubdir = "c:\\EVStubData\\";

        /// <summary>
        /// Checks is Stub directory exists
        /// </summary>
        private static int _Stubdata = 3;

        public static bool StubData
        {
            get
            {
                if (_Stubdata == 3)
                    _Stubdata = System.IO.Directory.Exists(_stubdir) ? 1 : 0;

                return _Stubdata == 1;
            }
        }

        #endregion

        /// <summary>
        /// This is a generic Http send method which takes data from
        /// stub if needed else calls the service and returns the data
        /// back to the caller
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="httpMethod">HttpMethod type</param>
        /// <param name="uri">Uri of stubbed filename/Rest web service</param>
        /// <param name="obj">Request type object</param>
        /// <returns>Response type object</returns>

        public TResponse Send<TRequest, TResponse>(HttpMethod httpMethod, string uri, TRequest obj) where TRequest : class
        {
            HttpResponseMessage httpResp = null;
            string xml = string.Empty;

            // -- try to get stub data
            xml = GetStubData(uri);

            if (string.IsNullOrEmpty(xml))
            {
                httpResp = obj == null ? EVHttpClient.GetHttpClient().Send(httpMethod, uri) : EVHttpClient.GetHttpClient().Send(httpMethod, uri, HttpContentExtensions.CreateXmlSerializable<TRequest>(obj));

                if (!httpResp.IsStatusIsSuccessful())
                {
                    throw new EVException().AddHttpResponse(httpResp);
                }

                // -- get response data
                xml = GetResponseData(uri, httpResp);
            }

            return (TResponse)XmlUtility.DeserializeObject(xml, typeof(TResponse));
        }

        /// <summary>
        /// A Get method to fetch the stream from the URI
        /// </summary>
        /// <typeparam name="TRequest">The Request Object to pass to</typeparam>
        /// <param name="httpMethod">HTTP Method</param>
        /// <param name="uri">The Source URL from where Stream can be downloaded</param>
        /// <param name="obj">The input object which should be passed as part of request</param>
        /// <returns></returns>
        public Stream GetStream<TRequest>(HttpMethod httpMethod, string uri, TRequest obj) where TRequest : class
        {
            HttpResponseMessage httpResp = null;
            string xml = string.Empty;

            // -- try to get stub data
            xml = GetStubData(uri);

            if (string.IsNullOrEmpty(xml))
            {
                httpResp = obj == null ? EVHttpClient.GetHttpClient().Send(httpMethod, uri) : EVHttpClient.GetHttpClient().Send(httpMethod, uri, HttpContentExtensions.CreateXmlSerializable<TRequest>(obj));

                if (!httpResp.IsStatusIsSuccessful())
                {
                    throw new EVException().AddHttpResponse(httpResp);
                }

                // -- get response data
                return httpResp.Content.ReadAsStream();
            }

            return null;
        }



        /// <summary>
        /// This is a generic Http send method which takes data from
        /// stub if needed else calls the service and returns the data
        /// back to the caller
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="httpMethod">HttpMethod type</param>
        /// <param name="uri">Uri of stubbed filename/Rest web service</param>
        /// <param name="obj">Request type object</param>
        /// <returns>Response type object</returns>
        public TResponse SendUsingDataContract<TRequest, TResponse>(HttpMethod httpMethod, string uri, TRequest obj)
        {
            HttpResponseMessage httpResp = null;
            string xml = string.Empty;

            // -- try to get stub data
            xml = GetStubData(uri);

            if (string.IsNullOrEmpty(xml))
            {
                httpResp = obj == null ? EVHttpClient.GetHttpClient().Send(httpMethod, uri) : EVHttpClient.GetHttpClient().Send(httpMethod, uri, HttpContentExtensions.CreateDataContract<TRequest>(obj));

                if (httpResp != null && !httpResp.IsStatusIsSuccessful())
                {
                    throw new EVException().AddHttpResponse(httpResp);
                }
                // -- get response data
                xml = GetResponseData(uri, httpResp);
            }

            return (TResponse)XmlUtility.DeserializeObject(xml, typeof(TResponse));
        }


        /// <summary>
        /// This function gets the stored data for a given Uri.
        /// This will work when "STUBENABLE" is set to a value in the config
        /// </summary>
        /// <param name="uri">Uri of the stubbed file name</param>
        /// <param name="xml">Stubbed data content</param>
        /// <returns>true/false</returns>
        public static string GetStubData(string uri)
        {
            string xml = string.Empty;

            // -- if stubdata is not set please return false so 
            // -- the code will go and actually fetch from the service
            // -- instead of file
            if (!StubData)
            {
                return xml;
            }

            string filename = _stubdir + GetStubFileName(uri);

            if (File.Exists(filename))
                // -- read from file and send the xml back through reference
                xml = File.ReadAllText(filename);

            return xml;
        }

        /// <summary>
        /// gets the response data
        /// </summary>
        /// <param name="uri">Uri of stubbed file name</param>
        /// <param name="resp">HttpResponseMessage object</param>
        /// <returns>response data as string</returns>
        public static string GetResponseData(string uri, HttpResponseMessage response)
        {
            // -- read the content
            string xml = response.Content.ReadAsString();

            if (StubData)
            {
                // -- if stubbing enabled then write the
                // -- returned content to a file for future
                // -- serves...
                string filename = uri;
                filename = _stubdir + GetStubFileName(filename);

                // -- dump to a file if "stubdata" is set
                // -- from next time the file data will be served instead
                // -- of calling the service
                File.WriteAllText(filename, xml);
            }

            return xml;
        }

        /// <summary>
        /// -- Creates a stub file name from the URI after
        /// -- strips the non alpha numeric and IP and http://, https://
        /// </summary>
        /// <param name="uri">Uri of stubbed filename</param>
        /// <returns>Stubbed filename Uri as string</returns>
        public static string GetStubFileName(string uri)
        {
            const int LEN = 196;
            const string HttpToken = @"http://";
            const string HttpsToken = @"https://";

            uri = uri.ToLower(CultureInfo.CurrentCulture);

            // -- check for http and remove it
            int index = uri.IndexOf(HttpToken, StringComparison.Ordinal);
            if (index == 0)
            {
                uri = uri.Substring(HttpToken.Length);
            }
            else
            {
                /* -- check for https and remove it */
                index = uri.IndexOf(HttpsToken, StringComparison.Ordinal);

                if (index == 0)
                {
                    uri = uri.Substring(HttpsToken.Length);
                }
            }

            // -- remove the IP:Port/
            index = uri.IndexOf(@"/", StringComparison.Ordinal);

            if (index > 0)
            {
                uri = uri.Substring(index);
            }

            // -- remove any non-alphanumeric characters
            // -- with which we cannot create file name
            string str = Regex.Replace(uri, "[\\W]", string.Empty);
            uri = str.Length > LEN ? str.Substring(0, LEN) : str;

            // -- append xml to the stub file name created 
            // -- and return the file name
            return uri + ".xml";
        }
    }
}
