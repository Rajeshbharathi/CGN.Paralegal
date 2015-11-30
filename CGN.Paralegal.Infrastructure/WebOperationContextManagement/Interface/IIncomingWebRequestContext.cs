using System;
using System.Net;

namespace CGN.Paralegal.Infrastructure.WebOperationContextManagement
{
    public interface IIncomingWebRequestContext
    {
        string Accept { get; }

        long ContentLength { get; }

        string ContentType { get; }

        WebHeaderCollection Headers { get; }

        string Method { get; }

        UriTemplateMatch UriTemplateMatch { get; }

        string UserAgent { get; }
    }
}
