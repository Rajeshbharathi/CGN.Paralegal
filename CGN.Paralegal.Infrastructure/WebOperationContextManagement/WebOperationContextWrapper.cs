
using System.ServiceModel.Web;

namespace CGN.Paralegal.Infrastructure.WebOperationContextManagement
{
    public class WebOperationContextWrapper : IWebOperationContext
    {
        private WebOperationContext context;

        public WebOperationContextWrapper(WebOperationContext webOperationContext)
        {
            this.context = webOperationContext;
        }

        #region IWebOperationContext Members

        public IIncomingWebRequestContext IncomingRequest
        {
            get 
            {
                return new IncomingWebRequestContextWrapper(this.context.IncomingRequest);
            }
        }

        public IOutgoingWebResponseContext OutgoingResponse
        {
            get 
            {
                return new OutgoingWebResponseContextWrapper(this.context.OutgoingResponse);
            }
        }

        #endregion
    }
}
