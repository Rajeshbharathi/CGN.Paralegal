using System.Text;
using System.Web;
using System.Reflection;

using NLog;
using NLog.LayoutRenderers;

using CGN.Paralegal.Infrastructure.ExceptionManagement;

namespace CGN.Paralegal.Infrastructure
{
    using System;
    using System.Collections.Specialized;

    [LayoutRenderer("webvariables")]
    public class WebVariablesRenderer : LayoutRenderer
    {
        ///
        /// Renders the current date and appends it to the specified .
        ///
        /// <param name="builder"/>The  to append the rendered data to.
        /// <param name="logEvent"/>Logging event.
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (null == HttpContext.Current)
            {
                return;
            }

            // This is the check if Request object can be safely queried without throwing exceptions
            // If we throw here ExceptionIntercept would kick in and try to log the exception which would cause 
            // us to come back here and blow with infinite recursion
            var fieldInfo = typeof(HttpContext).GetField("HideRequestResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null && (bool)fieldInfo.GetValue(HttpContext.Current))
            {
                return;
            }

            StringBuilder mess = new StringBuilder();

            if (HttpContext.Current.Session == null)
            {
                mess.AppendLine("Session is NULL!");
            }
            else
            {
                mess.AppendFormat("Session ID = {0}\r\n", HttpContext.Current.Session.SessionID);
            }
            
            if (null == HttpContext.Current.Request)
            {
                return;
            }

            if (null != HttpContext.Current.Request.Url)
            {
                mess.AppendFormat("URL = {0}\r\n", HttpContext.Current.Request.Url);
            }

            if (null != HttpContext.Current.Request.HttpMethod)
            {
                mess.AppendFormat("HttpMethod = {0}\r\n", HttpContext.Current.Request.HttpMethod);
            }

            mess.AppendFormat("Number of parameters = {0}\r\n", HttpContext.Current.Request.Params.Count);

            foreach (string parameterName in HttpContext.Current.Request.Params.AllKeys)
            {
                mess.AppendFormat("    [{0}] = {1}\r\n", parameterName, HttpContext.Current.Request.Params[parameterName]);
            }

            if (null != HttpContext.Current.Session)
            {
                foreach (string sessionKey in HttpContext.Current.Session.Keys)
                {
                    mess.AppendFormat("Session[{0}] = {1}\r\n", sessionKey, HttpContext.Current.Session[sessionKey]);
                }
            }

            if (null != HttpContext.Current.Request.Cookies)
            {
                foreach (var cookieKey in HttpContext.Current.Request.Cookies.AllKeys)
                {
                    HttpCookie cookieVal = HttpContext.Current.Request.Cookies[cookieKey];
                    if (cookieVal != null)
                    {
                        mess.AppendFormat("Cookie[{0}] = {1}\r\n", cookieKey, cookieVal.Value);
                    }
                }
            }

            if (null != HttpContext.Current.Request.Headers)
            {
                foreach (var headerKey in HttpContext.Current.Request.Headers.AllKeys)
                {
                    mess.AppendFormat("Header[{0}] = {1}\r\n", headerKey, HttpContext.Current.Request.Headers[headerKey]);
                }
            }

            NameValueCollection serverVariables = HttpContext.Current.Request.ServerVariables;
            if (null != serverVariables)
            {
                foreach (var serverVariableKey in HttpContext.Current.Request.ServerVariables.AllKeys)
                {
                    string serverVariableValue;
                    try
                    {
                        serverVariableValue = HttpContext.Current.Request.ServerVariables[serverVariableKey];
                    }
                    catch (Exception ex)
                    {
                        // In some weird cases ServerVariables can actually throw, because their internal _request is null.
                        serverVariableValue = "Value UNKNOWN, because exception was thrown. " + ex.ToDebugString();
                    }
                        
                    mess.AppendFormat("ServerVariable[{0}] = {1}\r\n", serverVariableKey, serverVariableValue);
                }
            }

            builder.Append(mess);
        }

        public static string WebVariablesDump()
        {
            WebVariablesRenderer webVariablesRenderer = new WebVariablesRenderer();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            webVariablesRenderer.Append(sb, null);
            return sb.ToString();
        }
    }
}
