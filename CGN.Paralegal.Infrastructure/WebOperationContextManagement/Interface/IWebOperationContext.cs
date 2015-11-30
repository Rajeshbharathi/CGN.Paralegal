
namespace CGN.Paralegal.Infrastructure.WebOperationContextManagement
{
    public interface IWebOperationContext
    {
        IIncomingWebRequestContext IncomingRequest { get; }
        IOutgoingWebResponseContext OutgoingResponse { get; }
    }
}
