using System;
using System.Net;

namespace CGN.Paralegal.Infrastructure.WebOperationContextManagement
{
    public interface IOutgoingWebResponseContext
    {
       long ContentLength { get; set; }
       
       string ContentType { get; set; }
       
       string ETag { get; set; }
       
        WebHeaderCollection Headers { get; }
        
        DateTime LastModified { get; set; }
        
        string Location { get; set; }
        
        HttpStatusCode StatusCode { get; set; }
        
        string StatusDescription { get; set; }
        
        bool SuppressEntityBody { get; set; }

        void SetStatusAsCreated(Uri locationUri);
        
        void SetStatusAsNotFound();
        
        void SetStatusAsNotFound(string description);
    }
}
