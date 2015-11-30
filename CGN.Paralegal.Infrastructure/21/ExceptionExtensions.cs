using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;
using System.Xml.Serialization;
using CGN.Paralegal.ServiceContracts.Common;
using CGN.Paralegal.BusinessEntities;
using Microsoft.ServiceModel.Web;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Microsoft.Http;

namespace CGN.Paralegal.Infrastructure.ExceptionManagement
{
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public static class PublicExceptionExtensions
    {
        private static readonly bool LogExplicitlySwallowedExceptions;

        static PublicExceptionExtensions()
        {
            string str = ConfigurationManager.AppSettings["LogExplicitlySwallowedExceptions"];
            // ReSharper disable once SimplifyConditionalTernaryExpression
            LogExplicitlySwallowedExceptions = String.IsNullOrEmpty(str) ? false : bool.Parse(str);
        }

        public static TException SetInnerException<TException>(this TException ex, Exception innerException) where TException : Exception
        {
            Debug.Assert(null == ex.InnerException, "Attempt to override existing inner exception");

            if (ex == innerException)
            {
                Tracer.Error("Detected serious bug: attempt to set inner exception field to the outer exception reference");
                return ex;
            }

            InternalExceptionExtensions.InnerExceptionFieldInfo.SetValue(ex, innerException);
            ExceptionPayload payload = ex.GetOrCreatePayload();
            payload.Dirty = true;
            return ex;
        }

        public static TException AddDbgMsg<TException>(this TException ex, string messageFormat, params Object[] args) where TException: Exception
        {
            string expandedMsg = Utils.SafeFormat(messageFormat, args);
            return ex.AddNamedProperty(InternalExceptionExtensions.DebugMessageLabel, expandedMsg);
        }

        public static TException AddUsrMsg<TException>(this TException ex, string messageFormat, params Object[] args) where TException: Exception
        {
            string expandedMsg = Utils.SafeFormat(messageFormat, args);
            return ex.AddNamedProperty(InternalExceptionExtensions.UserMessageLabel, expandedMsg);
        }

        public static TException AddResMsg<TException>(this TException ex, string resourceID, params Object[] args) where TException: Exception
        {
            if (String.IsNullOrWhiteSpace(resourceID))
            {
                return ex;
            }
            ex.AddErrorCode(resourceID);
            return ex.AddNamedProperty(InternalExceptionExtensions.UserMessageLabel, Msg.FromRes(resourceID, args));
        }

        #region EVException specific properties
        public static TException AddErrorCode<TException>(this TException ex, string errorCode) where TException : Exception
        {
            if (String.IsNullOrWhiteSpace(errorCode))
            {
                return ex;
            }
            return ex.AddNamedProperty(InternalExceptionExtensions.ErrorCodeLabel, errorCode);
        }

        public static string GetErrorCode(this Exception ex)
        {
            List<object> errorCodes = ex.GetNamedProperty(InternalExceptionExtensions.ErrorCodeLabel);
            if (errorCodes.Any())
            {
                return errorCodes.First() as string;
            }
            return String.Empty;
        }

        public static TException AddStatusCode<TException>(this TException ex, HttpStatusCode statusCode) where TException : Exception
        {
            return ex.AddNamedProperty(InternalExceptionExtensions.StatusCodeLabel, statusCode);
        }

        public static HttpStatusCode GetStatusCode(this Exception ex)
        {
            List<object> statusCodes = ex.GetNamedProperty(InternalExceptionExtensions.StatusCodeLabel);
            if (!statusCodes.Any())
            {
                return HttpStatusCode.InternalServerError;
            }

            object objHttpStatusCode = statusCodes.First();
            if (objHttpStatusCode is HttpStatusCode)
            {
                return (HttpStatusCode)objHttpStatusCode;
            }
            Debug.Assert(false, "Exception property named StatusCode is of the wrong type!");
            return HttpStatusCode.InternalServerError;
        }

        public static TException AddCorrelationId<TException>(this TException ex, Guid correlationId) where TException : Exception
        {
            ExceptionPayload payload = ex.GetOrCreatePayload();
            bool savedDirtyFlag = payload.Dirty;
            try
            {
                return ex.AddNamedProperty(InternalExceptionExtensions.CorrelationIdLabel, correlationId);
            }
            finally
            {
                // Assigning of the Correlation Id happens for ALL exceptions and it should not make them dirty.
                payload.Dirty = savedDirtyFlag;
            }
        }

        public static Guid GetCorrelationId(this Exception ex)
        {
            List<object> correlationIds = ex.GetNamedProperty(InternalExceptionExtensions.CorrelationIdLabel);

            if (correlationIds.Any())
            {
                return (Guid)(correlationIds.First());
            }

            // If exception does not have CorrelationId assigned let's try to get it from System.Diagnostics.Trace.CorrelationManager.ActivityId

            if (System.Diagnostics.Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                // If System.Diagnostics.Trace.CorrelationManager.ActivityId is not set, then let's set it and use it
                System.Diagnostics.Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }

            ex.AddCorrelationId(System.Diagnostics.Trace.CorrelationManager.ActivityId);
            return System.Diagnostics.Trace.CorrelationManager.ActivityId;
        }

        public static TException AddHttpResponse<TException>(this TException ex, HttpResponseMessage responseMessage) where TException : Exception
        {
            string contentType = responseMessage.Content.ContentType;

            if (contentType != Utils.FaultContentType)
            {
                string errorMessage1 = responseMessage.ToString();
                ex.AddDbgMsg(errorMessage1);
                string errorMessage2 = responseMessage.Content.ReadAsString();
                ex.AddDbgMsg(errorMessage2);
                return ex;
            }

            ErrorEntity objErrorEntity = responseMessage.Content.ReadAsXmlSerializable<ErrorEntity>();
            string errorCode = objErrorEntity.Reason.Code;

            if (errorCode == ErrorCodes.UnauthorizedError)
            {
                // [Ningjun] This occurs when the session is expired or removed. Unfortunately the UI pages did not handle it properly. 
                // As a work around, I will replace it by ErrorCodes.SessionExpired which will be handled properly by the UI pages
                errorCode = ErrorCodes.SessionExpired;
            }
            ex.AddErrorCode(errorCode);

            string combinedUserMessage = objErrorEntity.Reason.Message;
            string[] stringSeparators = new[] { Utils.NewLine };
            string[] separateUserMessages = combinedUserMessage.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string userMessage in separateUserMessages)
            {
                ex.AddUsrMsg(userMessage);    
            }

            switch (errorCode)
            {
                case ErrorCodes.AlreadyLoggedIn:
                case ErrorCodes.ForcedLogOut:
                case ErrorCodes.SessionExpired:
                case ErrorCodes.UnauthorizedError:
                    
                    break;
            }

            return ex;
        }

        public static string ToJson(this Exception ex)
        {
            var errorObj = new { ErrorCode = ex.GetErrorCode(), ErrorDesc = ex.ToUserString() };
            return SerializeJson(errorObj);
        }

        private static string SerializeJson(object jsonString)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string serializedString = serializer.Serialize(jsonString);
            return serializedString;
        }

        #endregion

        public static string ToDebugString(this Exception ex)
        {
            StringBuilder sbMessage = new StringBuilder(ex.GetDetailedExceptionMessage(false));
            
            //String s = "\"" + sbMessage + "\"";
            //Debug.WriteLine(s);

            sbMessage.Combine(ex.AllMessages(), Utils.NewLine);

            // Usage of GetDetailedExceptionMessage gives us type name anyway, so no need to duplicate it
            //StringBuilder sbFull = new StringBuilder(ex.TrimExceptionClassName());
            //if (sbMessage.Length > 0)
            //{
            //    sbFull.Append(": ");
            //    sbFull.Append(sbMessage);
            //}

            StringBuilder sbFull = sbMessage;

            if (ex.InnerException != null)
            {
                sbFull.AppendLine();
                sbFull.Append("Inner exception: ");
                sbFull.Append(ex.InnerException.ToDebugString());
                if (!String.IsNullOrEmpty(ex.InnerException.GetFullStackTrace()))
                {
                    sbFull.Append(Utils.NewLine + "   --- End of inner exception stack trace ---");
                }
            }

            sbFull.Combine(ex.GetFullStackTrace(), Utils.NewLine);

            //ex.SetDirty(false);

            return sbFull.ToString();
        }

        public static string ToUserString(this Exception ex)
        {
            StringBuilder sbMessage = new StringBuilder(ex.GetDetailedExceptionMessage(true));

            sbMessage.Combine(ex.UserMessages(), Utils.NewLine);

            StringBuilder sbFull = sbMessage;

            if (ex.InnerException != null)
            {
                sbFull.Combine(ex.InnerException.ToUserString(), " ");
            }

            //ex.SetDirty(false);

            return sbFull.ToString();
        }

        public static TException Trace<TException>(this TException ex, [CallerFilePath] string callerFilePath = "",
                       [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "") where TException : Exception
        {
            ExceptionPayload payload = ex.GetOrCreatePayload();

            CodeLocation codeLocation = new CodeLocation(callerFilePath, callerLineNumber, callerMemberName);
            ex.AddNamedProperty("TraceLocation", codeLocation.ToString());

            if (!payload.Dirty)
            {
                Tracer.Debug("Detected potential bug: logging exception which has already been logged. Duplicate exception log follows.");
            }

            Tracer.LogException(ex);
            payload.Dirty = false;
            return ex;
        }

        public static TException Swallow<TException>(this TException ex) where TException : Exception
        {
            ExceptionPayload payload = ex.GetOrCreatePayload();

            if (LogExplicitlySwallowedExceptions)
            {
                Tracer.LogSwallowedException(ex);
            }
            
            payload.Dirty = false;
            return ex;
        }

        public static TException AddNamedProperty<TException>(this TException ex, string name, object value) where TException : Exception
        {
            ExceptionPayload payload = ex.GetOrCreatePayload();

            ExceptionProperty exceptionProperty = new ExceptionProperty(name, value);

            // Deduplication
            if (payload.ExceptionProperties.Contains(exceptionProperty))
            {
                Tracer.Warning("Detected potential bug: attempt to add duplicated exception property: Name = {0}, Value = {1}" +
                Environment.NewLine + Environment.StackTrace, exceptionProperty.Name, exceptionProperty.Value);
                return ex;
            }

            payload.ExceptionProperties.Add(exceptionProperty);
            payload.Dirty = true;
            return ex;
        }

        public static List<object> GetNamedProperty(this Exception ex, string name)
        {
            List<object> res = new List<object>();
            ExceptionPayload payload = ex.GetOrCreatePayload();
            foreach (ExceptionProperty exceptionProperty in payload.ExceptionProperties)
            {
                if (exceptionProperty.Name == name)
                {
                    res.Add(exceptionProperty.Value);
                }
            }
            return res;
        }

        public static WebProtocolException ToWebProtocolException(this Exception ex)
        {
            ex.AddDbgMsg("Exception converted to WebProtocolException.");
            ex.Trace();

            Reason reason = new Reason { Message = ex.ToUserString(), Code = ex.GetErrorCode() };
            ErrorEntity errorEntity = new ErrorEntity { Reason = reason };

            HttpStatusCode statusCode = ex.GetStatusCode();

            if (WebOperationContext.Current != null && WebOperationContext.Current.OutgoingResponse != null)
            {
                // -- if comes from web
                WebOperationContext.Current.OutgoingResponse.ContentType = Utils.FaultContentType;
                WebOperationContext.Current.OutgoingResponse.StatusCode = statusCode;
            }

            WebProtocolException webProtocolException = new WebProtocolException(statusCode, ex.ToUserString(), errorEntity, ex, true);
            return webProtocolException;
        }

        /// <summary>
        /// To the web fault exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        public static WebFaultException<ServiceFault> ToWebFaultException(this Exception ex)
        {
            ex.Trace();
            var statusCode = ex.GetStatusCode();
            var errorMessage = "Unknown Error";
            if (!string.IsNullOrWhiteSpace(ex.GetErrorCode()))
            {
                errorMessage = Msg.FromRes(ex.GetErrorCode());
            }
            
            var fault = new ServiceFault { Code = ex.GetErrorCode(), Message = errorMessage };
            return new WebFaultException<ServiceFault>(fault, statusCode);
        }

        public static FullExceptionStackTrace GetCallStack(this Exception ex)
        {
            object objFullExceptionStackTrace = ex.GetNamedProperty(InternalExceptionExtensions.FullExceptionStackTraceLabel).FirstOrDefault();
            if (objFullExceptionStackTrace == null)
            {
                return null;
            }
            return objFullExceptionStackTrace as FullExceptionStackTrace;
        }
    }

    [Serializable]
    public class FullExceptionStackTrace
    {
        public FullExceptionStackTrace(Exception ex)
        {
            exceptionToReportingPoint = new StackTrace(ex, true);
            reportingPointToTop = GetReportingPointStackTrace();

            // Here reportingPointToTop may have frames overlapping with exceptionToReportingPoint. 
            // Removing those duplicate frames here

            //StackFrame lastExceptionStackFrame = null;
            //if (exceptionToReportingPoint.FrameCount > 0)
            //{
            //    lastExceptionStackFrame = exceptionToReportingPoint.GetFrame(exceptionToReportingPoint.FrameCount - 1);
            //}
            //int framesToSkip = 0;
            //if (lastExceptionStackFrame != null)
            //{
            //    for (int frameNum = 0; frameNum < reportingPointToTop.FrameCount; frameNum++)
            //    {
            //        StackFrame reportPointStackFrame = reportingPointToTop.GetFrame(frameNum);
            //        if (lastExceptionStackFrame.GetMethod() == reportPointStackFrame.GetMethod() &&
            //            lastExceptionStackFrame.GetILOffset() == reportPointStackFrame.GetILOffset())
            //        {
            //            framesToSkip = frameNum + 1;
            //            break;
            //        }
            //    }
            //}
            //reportingPointToTop = new StackTrace(framesToSkip, true);

            // Tracer.Debug(ToString());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (exceptionToReportingPoint.FrameCount > 0)
            {
                sb.AppendFormat("   *** Exception to reporting point stack trace ***{0}", Utils.NewLine);
                sb.Append(exceptionToReportingPoint);
                sb.AppendFormat("   *** Reporting point to the top stack trace ***{0}", Utils.NewLine);
            }

            sb.Append(reportingPointToTop);

            return sb.ToString();
        }

        private static StackTrace GetReportingPointStackTrace()
        {
            StackTrace draft = new StackTrace(true); // It cannot possibly be empty

            // Skips frames which belong to Infrastructure 
            int framesToSkip = 0;
            for (; framesToSkip < draft.FrameCount; framesToSkip++)
            {
                StackFrame reportPointStackFrame = draft.GetFrame(framesToSkip);
                MethodBase methodBase = reportPointStackFrame.GetMethod();
                if (methodBase.DeclaringType == null || 
                    methodBase.DeclaringType.Namespace == null ||
                    !methodBase.DeclaringType.Namespace.StartsWith("CGN.Paralegal.Infrastructure"))
                {
                    break;
                }
            }

            return new StackTrace(framesToSkip, true);
        }

        private readonly StackTrace exceptionToReportingPoint;
        private readonly StackTrace reportingPointToTop;
    }

    internal static class InternalExceptionExtensions
    {
        internal const string InternalPropertyNamePrefix = "$$$ ";

        internal const string PayloadLabel = InternalPropertyNamePrefix + "Payload";

        internal const string FullExceptionStackTraceLabel = InternalPropertyNamePrefix + "FullExceptionStackTrace";

        internal const string UserMessageLabel = "User Message";
        internal const string DebugMessageLabel = "Debug Message";

        internal const string ErrorCodeLabel = "Error Code";
        internal const string StatusCodeLabel = "Status Code";
        internal const string CorrelationIdLabel = "Correlation Id";
        internal const string FileNameLabel = "File Name";

        /// <summary>
        /// Non-recursive method to get ALL payload messages associated with exception
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        internal static string AllMessages(this Exception ex)
        {
            ExceptionPayload payload = ex.GetOrCreatePayload();
            StringBuilder sbAllMessages = new StringBuilder();
            foreach (ExceptionProperty exceptionProperty in payload.ExceptionProperties)
            {
                if (exceptionProperty.Name.StartsWith(InternalPropertyNamePrefix))
                    continue; // Our internal stuff
                string strNameValuePair = String.Format("\"{0}\" = \"{1}\"", exceptionProperty.Name, exceptionProperty.Value);
                sbAllMessages.Combine(strNameValuePair, Utils.NewLine);
            }

            foreach (DictionaryEntry de in ex.Data)
            {
                if (de.Key.ToString().StartsWith(InternalPropertyNamePrefix))
                    continue; // Our internal stuff
                string message = String.Format("\"{0}\" = \"{1}\"", de.Key, de.Value);
                sbAllMessages.Combine(message, Utils.NewLine);
            }
            return sbAllMessages.ToString();
        }

        /// <summary>
        /// Non-recursive method to get User payload messages associated with exception
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        internal static string UserMessages(this Exception ex)
        {
            List<object> userMessages = ex.GetNamedProperty(UserMessageLabel);

            if (userMessages.Any())
            {
                return userMessages.Join(Utils.NewLine);    
            }

            // If no user messages available we may try to see if any error codes are available
            List<object> errorCodes = ex.GetNamedProperty(ErrorCodeLabel);
            if (errorCodes.Any())
            {
                StringBuilder sbErrors = new StringBuilder();
                foreach (string errorCode in errorCodes)
                {
                    string error = Msg.FromRes(errorCode);
                    sbErrors.Combine(error, Utils.NewLine);
                }
                return sbErrors.ToString();
            }

            // If no user messages or error codes are available then return generic error message and correlation id.
            return "No user friendly message available. Correlation Id = " + ex.GetCorrelationId();
        }

        // Returning ex.Message is Ok for some Exceptions, but for instance for System.IO.FileNotFoundException it would miss the file name
        [SuppressMessage("CodeQuality.Exceptions", "KE1010:ExceptionToString")]
        internal static string GetDetailedExceptionMessage(this Exception ex, bool forUser)
        {
            // Must handle EVException separately otherwise its ToString would call us again and cause infinite recursion
            if (ex is EVException)
            {
                return String.Empty;    
            }

            if (forUser)
            {
                // Suppress default message CLR produces if no message is specified in exception
                string realMessage = MessageFieldInfo.GetValue(ex) as string;
                if (String.IsNullOrWhiteSpace(realMessage))
                {
                    return String.Empty;
                }
            }

            // Debugging
            //FieldInfo[] exceptionFields = typeof(Exception).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            // Save InnerException so we can restore it back 
            Exception innerException = ex.InnerException;
            // Reset InnerException so that ToString() would not go recursive
            InnerExceptionFieldInfo.SetValue(ex, null);

            // Save StackTraceString so we can restore it back 
            object strStackTraceString = StackTraceStringFieldInfo.GetValue(ex);
            // Reset strStackTraceString to empty string so that it would not show up in ToString() output
            StackTraceStringFieldInfo.SetValue(ex, String.Empty);

            // Getting single level (non-recursive) ToString() without StackTrace
            string detailedExceptionMessage = ex.ToString();

            if (forUser)
            {
                // End users are not interested to see the type of exception being thrown. Unfortunately most system exceptions tend to 
                // prepend it in their implementation of ToString() overload.
                string typePrefix = ex.GetType().FullName + ": ";
                detailedExceptionMessage = Utils.RemovePrefix(detailedExceptionMessage, typePrefix);
            }

            detailedExceptionMessage = Utils.RemoveSuffix(detailedExceptionMessage, "\r\n");

            IOException ioEx = ex as IOException;
            if (ioEx != null && !ex.GetOrCreatePayload().ContainsProperty(FileNameLabel))
            {
                object objFileName = MaybeFullPathFieldInfo.GetValue(ioEx);
                string fileName = objFileName == null ? "Unknown" : objFileName.ToString();
                ex.AddNamedProperty(FileNameLabel, fileName);
            }

            // Restoring InnerException and StackTrace back to their original values 
            InnerExceptionFieldInfo.SetValue(ex, innerException);
            StackTraceStringFieldInfo.SetValue(ex, strStackTraceString);

            return detailedExceptionMessage;
        }

        internal static readonly FieldInfo MessageFieldInfo = typeof(Exception).GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo InnerExceptionFieldInfo = typeof(Exception).GetField("_innerException", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo StackTraceStringFieldInfo = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo MaybeFullPathFieldInfo = typeof(IOException).GetField("_maybeFullPath", BindingFlags.NonPublic | BindingFlags.Instance);
        
        internal static StringBuilder Combine(this StringBuilder sb, string strToAppend, string strDelimiter)
        {
            if (sb.Length > 0 && !String.IsNullOrEmpty(strToAppend))
            {
                sb.Append(strDelimiter);
            }
            return sb.Append(strToAppend);
        }

        internal static string Join(this List<object> list, string strDelimiter)
        {
            if (!list.Any())
            {
                return "";
            }

            StringBuilder joined = new StringBuilder();
            foreach (var item in list)
            {
                joined.Combine(item.ToString(), strDelimiter);
            }

            return joined.ToString();
        }

        internal static ExceptionPayload GetOrCreatePayload(this Exception ex)
        {
            ExceptionPayload obj = ex.Data[PayloadLabel] as ExceptionPayload;
            if (null != obj)
            {
                return obj;
            }

            obj = new ExceptionPayload(ex);
            ex.Data[PayloadLabel] = obj;
            return obj;
        }

        internal static TException SetDirty<TException>(this TException ex, bool dirty) where TException : Exception
        {
            ExceptionPayload payload = ex.GetOrCreatePayload();
            payload.Dirty = dirty;
            return ex;
        }

        internal static string GetFullStackTrace(this Exception ex)
        {
            FullExceptionStackTrace fullExceptionStackTrace = ex.GetCallStack();
            if (fullExceptionStackTrace == null)
            {
                fullExceptionStackTrace = new FullExceptionStackTrace(ex);
                ex.SetCallStack(fullExceptionStackTrace);
            }

            return fullExceptionStackTrace.ToString();
        }

        internal static TException SetCallStack<TException>(this TException ex, FullExceptionStackTrace fullExceptionStackTrace) where TException : Exception
        {
            if (ex.GetNamedProperty(InternalExceptionExtensions.FullExceptionStackTraceLabel).FirstOrDefault() != null)
            {
                return ex; // We never set stack trace more than once 
            }

            return ex.AddNamedProperty(FullExceptionStackTraceLabel, new FullExceptionStackTrace(ex));
        }
    }

    [Serializable]
    internal struct ExceptionProperty
    {
        public ExceptionProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name;
        public object Value;
    }

    [Serializable]
    internal class ExceptionPayload
    {
        public ExceptionPayload(Exception ex)
        {
            this.ex = ex;
            Dirty = true;
        }

        public List<ExceptionProperty> ExceptionProperties = new List<ExceptionProperty>();

        public bool ContainsProperty(string propertyName)
        {
            foreach (ExceptionProperty exceptionProperty in ExceptionProperties)
            {
                if (String.Equals(exceptionProperty.Name, propertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Dirty { get; set; }

        private readonly Exception ex;

        ~ExceptionPayload()
        {
            //Tracer.Trace("Finalizer is called for exception with Correlation Id = {0}", ex.GetCorrelationId());
            if (!Dirty)
            {
                return;
            }
            Tracer.Debug("Detected potential bug: updated exception was not reported. Unreported exception follows.");
            Tracer.LogException(ex);
        }
    }
}
