using System.IO;
using Microsoft.Http;

namespace CGN.Paralegal.Infrastructure.Http
{
    public interface IEVHttpSend
    {
        TResponse Send<TRequest, TResponse>(HttpMethod httpMethod, string uri, TRequest obj) where TRequest : class;
        Stream GetStream<TRequest>(HttpMethod httpMethod, string uri, TRequest request) where TRequest : class;
    }
}
