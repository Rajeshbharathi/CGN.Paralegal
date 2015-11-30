//---------------------------------------------------------------------------------------------------
// <copyright file="EVHttpBehavior.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi</author>
//      <description>
//          Extends the Rest starter kit WebHttpBehaviour2 to format dispatched message
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace CGN.Paralegal.Infrastructure
{
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using Http;
    using Microsoft.ServiceModel.Web;

    /// <summary>
    /// Extends the Rest starter kit WebHttpBehaviour2 to format dispatched message
    /// </summary>
    public class EVHttpBehavior : WebHttpBehavior2
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the EVHttpBehavior class
        /// </summary>
        public EVHttpBehavior()
            : base()
        {
        }

        #endregion

        #region Member functions

        /// <summary>
        ///  Applies the dispatch behavior to service endpoint.
        /// </summary>
        /// <param name="endpoint">ServiceEndPoint object</param>
        /// <param name="endpointDispatcher">EndpointDispatcher object</param>
        public override void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.ApplyDispatchBehavior(endpoint, endpointDispatcher);
        }

        /// <summary>
        /// Adds the server error handler to the service endpoint
        /// </summary>
        /// <param name="endpoint">ServiceEndPoint object</param>
        /// <param name="endpointDispatcher">EndpointDispatcher object</param>
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.AddServerErrorHandlers(endpoint, endpointDispatcher);
        }

        /// <summary>
        /// Gets the Dispatch format request.
        /// </summary>
        /// <param name="operationDescription">OperationDescription object</param>
        /// <param name="endpoint">ServiceEndpoint object</param>
        /// <returns><see cref="DispatchMessageFormatter">DispatchMessageFormatter type</see></returns>
        protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            /*TODO:This still uses FormPostDispatchFormatter. Need to eliminate/override it completely.*/
            IDispatchMessageFormatter inner = base.GetRequestDispatchFormatter(operationDescription, endpoint);
            return new EVDispatchFormatter(operationDescription, inner, this.GetQueryStringConverter(operationDescription));
        }

        #endregion
    }
}

