#region File Header
//---------------------------------------------------------------------------------------------------
// <copyright file="EVHttpSend.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi/Visu</author>
//      <description>
//          This class provides the EVHttpSend for EVConcordance app
//      </description>
//      <changelog>
//          <date value=""></date>
//          <date value="11/13/2014">Included the change for Billing Report</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
using System;
using System.IO;
using System.Xml.Serialization;
using CGN.Paralegal.Infrastructure.ExceptionManagement;
using Microsoft.Http;

namespace CGN.Paralegal.Infrastructure.Http
{
    public class EVHttpSend : IEVHttpSend
    {

       
        /// <summary>
        /// This is a generic Http send method which send the response 
        /// object back to the caller
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="httpMethod">HttpMethod type</param>
        /// <param name="uri">Uri of stubbed filename/Rest web service</param>
        /// <param name="request">Request type object</param>
        /// <returns>Response type object</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public TResponse Send<TRequest, TResponse>(HttpMethod httpMethod, string uri, TRequest request) where TRequest : class
        {
            HttpClient httpClient = EVHttpClient.GetHttpClient();

            try
            {
                HttpResponseMessage httpResp;
                if (null == request)
                {
                    httpResp = httpClient.Send(httpMethod, uri);
                }
                else
                {
                    httpResp = httpClient.Send(httpMethod, uri, HttpContentExtensions.CreateXmlSerializable<TRequest>(request));
                }

                if (!httpResp.IsStatusIsSuccessful())
                {
                    throw new EVException().AddHttpResponse(httpResp);
                }
                

                // -- get the xml
                return httpResp.Content.ReadAsXmlSerializable<TResponse>();
            }
            catch (Exception exception)
            {
                CheckAndThrowWebException(exception);
                throw;   
            }
        }

        private void CheckAndThrowWebException(Exception exception)
        {
            // Below lines of code is added to handle the scenario where exception is thrown before the EVWebException is constructed
            string errorCode = exception.GetErrorCode();
            switch (errorCode)
            {
                case ErrorCodes.AlreadyLoggedIn:
                case ErrorCodes.ForcedLogOut:
                case ErrorCodes.SessionExpired:
                case ErrorCodes.UnauthorizedError:
                    break;
            }
            
        }
        /// <summary>
        /// A Get method to fetch the stream from the URI
        /// </summary>
        /// <typeparam name="TRequest">The Request Object to pass to</typeparam>
        /// <param name="httpMethod">HTTP Method</param>
        /// <param name="uri">The Source URL from where Stream can be downloaded</param>
        /// <param name="request">The input object which should be passed as part of request</param>
        /// <returns></returns>
        public Stream GetStream<TRequest>(HttpMethod httpMethod, string uri, TRequest request) where TRequest : class
        {
            HttpClient httpClient = EVHttpClient.GetHttpClient();

            try
            {
                HttpResponseMessage httpResp;
                if (null == request)
                {
                    httpResp = httpClient.Send(httpMethod, uri);
                }
                else
                {
                    httpResp = httpClient.Send(httpMethod, uri,
                        HttpContentExtensions.CreateXmlSerializable<TRequest>(request));
                }

                if (!httpResp.IsStatusIsSuccessful())
                {
                    throw new EVException().AddHttpResponse(httpResp);
                }


                // -- get the xml
                return httpResp.Content.ReadAsStream();
            }
            catch (Exception exception)
            {
                CheckAndThrowWebException(exception);
                throw;
            }
        }
    }
}
