using System;
using System.Web;
using System.Web.Http;
using System.Web.SessionState;
using CGN.Paralegal.Infrastructure;
using CGN.Paralegal.Infrastructure.Caching;
using CGN.Paralegal.Infrastructure.ExceptionManagement;
using LexisNexis.LTN.Administration;

namespace CGN.Paralegal.Services
{
    public class Global : HttpApplication
    {
        public static Global Instance;
        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            Instance = this;
            Tracer.Create("EVServices");
            Tracer.Debug("Application_Start.");
        }

        protected void Application_End(object sender, EventArgs e)
        {
            Tracer.Debug("Application_End.");
            NotifiedDataCache.Instance.Dispose();
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Commented out as it generates to much traffic
            //Tracer.Trace("Session_Start, Id = " + Utils.SessionId);
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Commented out as it generates to much traffic
            //Tracer.Trace("Session_End, Id = " + Utils.SessionId);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // This code propagates client CorrelationId, sent in request header, to service side ActivityId
            // Currently we decided that each WCF request should have its own CorrelationId
            // Parent ASP.NET request CorrelationId is still visible in the Web Variables part of the log
            //string strCorrelationId = HttpContext.Current.Request.Headers["CorrelationId"];
            //Guid guidCorrelationId;
            //if (Guid.TryParse(strCorrelationId, out guidCorrelationId))
            //{
            //    System.Diagnostics.Trace.CorrelationManager.ActivityId = guidCorrelationId;
            //}

            Tracer.Trace("BeginRequest: {0}", HttpContext.Current.Request.Url);
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            // Commented out as it generates to much traffic
            //Tracer.Trace("EndRequest: {0}", HttpContext.Current.Request.Url);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            if (null == ex)
            {
                return;
            }

            System.Threading.ThreadAbortException threadAbortException = ex as System.Threading.ThreadAbortException;
            if (null != threadAbortException)
            {
                return;
            }

            string requestUrl = HttpContext.Current.Request.Url.ToString();
            ex.AddDbgMsg("Request = {0}", requestUrl);

            if (ex.Message == "File does not exist." && requestUrl.Contains("CmgGeneralConfig.svc"))
            {
                // Suppress errors from configuration services to avoid main log pollution 
                Tracer.Trace("Suppressed exception from configuration services: " + ex.ToDebugString());
                ex.Swallow();
            }
            else
            {
                ex.Trace();
            }

            Server.ClearError();
        }
        /// <summary>
        /// Init method used by Web Api 
        /// </summary>
        public override void Init()
        {
            PostAuthenticateRequest += Application_PostAuthenticateRequest;
            base.Init();
        }
        /// <summary>
        /// Set web api session state behavior
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            HttpContext.Current.SetSessionStateBehavior(
                SessionStateBehavior.Required);
        }
    }
}