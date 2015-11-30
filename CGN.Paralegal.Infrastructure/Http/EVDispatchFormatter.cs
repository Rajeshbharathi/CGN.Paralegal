//---------------------------------------------------------------------------------------------------
// <copyright file="EVDispatchFormatter.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi</author>
//      <description>
//          Message formatter class
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace CGN.Paralegal.Infrastructure.Http
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Web;
    using System.Xml;
    using ExceptionManagement;

    /// <summary>
    /// This class formats the message dispatched.
    /// </summary>
    public class EVDispatchFormatter : IDispatchMessageFormatter
    {
        #region Member variables

        /// <summary>
        /// DispatchMessageFormatter object variable.
        /// </summary>
        private IDispatchMessageFormatter inner;

        /// <summary>
        /// OperationDescription object
        /// </summary>
        private OperationDescription od;

        /// <summary>
        /// Iterator index variable
        /// </summary>
        private int nvcIndex = -1;

        /// <summary>
        /// QueryStringConverter object
        /// </summary>
        private QueryStringConverter queryStringConverter;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the EVDispatchFormatter class
        /// </summary>
        /// <param name="od">OperationDescription object</param>
        /// <param name="inner">DispatchMessageFormatter object</param>
        /// <param name="queryStringConverter">QueryStringConverter object</param>
        public EVDispatchFormatter(OperationDescription od, IDispatchMessageFormatter inner, QueryStringConverter queryStringConverter)
        {
            this.inner = inner;
            this.od = od;
            this.queryStringConverter = queryStringConverter;
            MessageDescription request = od.Messages.FirstOrDefault(message => message.Direction == MessageDirection.Input);

            if (request != null && request.MessageType == null)
            {
                for (int i = 0; i < request.Body.Parts.Count; ++i)
                {
                    if (request.Body.Parts[i].Type == typeof(NameValueCollection))
                    {
                        this.nvcIndex = i;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Member Functions

        /// <summary>
        /// Deserializes the request.
        /// </summary>
        /// <param name="message">Message object</param>
        /// <param name="parameters">array of object</param>
        public void DeserializeRequest(Message message, object[] parameters)
        {
            if (message == null)
            {
                return;
            }

            if (this.nvcIndex >= 0 && string.Equals(WebOperationContext.Current.IncomingRequest.ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                using (XmlDictionaryReader r = message.GetReaderAtBodyContents())
                {
                    r.ReadStartElement("Binary");
                    byte[] buffer = r.ReadContentAsBase64();
                    string queryString = new UTF8Encoding().GetString(buffer);
                    NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
                    parameters[this.nvcIndex] = nvc;
                }

                // bind the Uri template parameters
                UriTemplateMatch match = message.Properties["UriTemplateMatchResults"] as UriTemplateMatch;
                ParameterInfo[] paramInfos = this.od.SyncMethod.GetParameters();
                var binder = this.CreateParameterBinder(match);
                object[] values = (from p in paramInfos
                                   where p.ParameterType != typeof(NameValueCollection)
                                   select binder(p)).ToArray<object>();
                int index = 0;
                for (int i = 0; i < paramInfos.Length; ++i)
                {
                    if (i != this.nvcIndex)
                    {
                        parameters[i] = values[index];
                        ++index;
                    }
                }
            }
            else
            {
                /*Throws exception if message doesn't comply with schema. This has to be changed to do custom deserilization.*/
                try
                {
                    this.inner.DeserializeRequest(message, parameters);
                }
                catch (Exception ex)
                {
                    throw ex.AddResMsg(ErrorCodes.MessageValidation).ToWebProtocolException();
                }
            }
        }

        /// <summary>
        /// Serializes the reply message.
        /// </summary>
        /// <param name="messageVersion">MessageVersion object</param>
        /// <param name="parameters">array of parameter objects</param>
        /// <param name="result">object type</param>
        /// <returns>Message object</returns>
        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates the parameter binder
        /// </summary>
        /// <param name="match">UriTemplateMatch object</param>
        /// <returns>Func expression</returns>
        private Func<ParameterInfo, object> CreateParameterBinder(UriTemplateMatch match)
        {
            return delegate(ParameterInfo pi)
            {
                string value = match.BoundVariables[pi.Name];
                if (!string.IsNullOrEmpty(value))
                {
                    return this.queryStringConverter.ConvertStringToValue(value, pi.ParameterType);
                }
                else
                {
                    return pi.RawDefaultValue;
                }
            };
        }

        #endregion
    }
}

