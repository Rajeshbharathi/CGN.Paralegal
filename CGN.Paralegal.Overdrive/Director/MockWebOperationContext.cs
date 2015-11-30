using System.Net;
using LexisNexis.Evolution.Infrastructure.WebOperationContextManagement;
using Moq;

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    /// This class will be used to Mock the Operation context object
    /// </summary>
    public class MockWebOperationContext : Mock<IWebOperationContext>
    {
        private Mock<IIncomingWebRequestContext> m_RequestContextMock = new Mock<IIncomingWebRequestContext>();
        private Mock<IOutgoingWebResponseContext> m_ResponseContextMock = new Mock<IOutgoingWebResponseContext>();
        /// <summary>
        /// Constructor 
        /// </summary>
        public MockWebOperationContext()
            : base()
        {
            this.SetupGet(webContext => webContext.IncomingRequest).Returns(m_RequestContextMock.Object);
            this.SetupGet(webContext => webContext.OutgoingResponse).Returns(m_ResponseContextMock.Object);
            WebHeaderCollection requestHeaders = new WebHeaderCollection();
            WebHeaderCollection responseHeaders = new WebHeaderCollection();
            m_RequestContextMock.SetupGet(requestContext => requestContext.Headers).Returns(requestHeaders);
            m_ResponseContextMock.SetupGet(responseContext => responseContext.Headers).Returns(responseHeaders);
        }

        /// <summary>
        /// Get the Incoming Request
        /// </summary>
        public Mock<IIncomingWebRequestContext> IncomingRequest
        {
            get
            {
                return m_RequestContextMock;
            }
        }
        /// <summary>
        /// Get the Outgoing Response
        /// </summary>
        public Mock<IOutgoingWebResponseContext> OutgoingResponse
        {
            get
            {
                return m_ResponseContextMock;
            }
        }
    }
}