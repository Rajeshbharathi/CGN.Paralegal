using System;
using System.ServiceModel.Web;


namespace CGN.Paralegal.Infrastructure.WebOperationContextManagement
{
    public class IncomingWebRequestContextWrapper : IIncomingWebRequestContext
    {
        private IncomingWebRequestContext context;

        public IncomingWebRequestContextWrapper(IncomingWebRequestContext context )
        {
            this.context = context;
        }

        #region IIncomingWebRequestContext Members

        public string Accept
        {
            get 
            { 
                return this.context.Accept;
            }
        }

        public long ContentLength
        {
            get 
            {
                return this.context.ContentLength;
            }
        }

        public string ContentType
        {
            get 
            {
                return this.context.ContentType;
            }
        }

        public System.Net.WebHeaderCollection Headers
        {
            get 
            {
                return this.context.Headers;
            }
        }

        public string Method
        {
            get 
            {
                return this.context.Method;
            }
        }

        public UriTemplateMatch UriTemplateMatch
        {
            get 
            {
                return this.context.UriTemplateMatch;
            }
        }

        public string UserAgent
        {
            get 
            {
                return this.context.UserAgent;
            }
        }

        #endregion
    }
}
