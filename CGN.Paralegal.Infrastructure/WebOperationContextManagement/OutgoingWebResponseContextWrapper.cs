﻿using System;
using System.ServiceModel.Web;

namespace CGN.Paralegal.Infrastructure.WebOperationContextManagement
{
    public class OutgoingWebResponseContextWrapper : IOutgoingWebResponseContext
    {
        private OutgoingWebResponseContext context;

        public OutgoingWebResponseContextWrapper(OutgoingWebResponseContext outgoingContext)
        {
            this.context = outgoingContext;
        }

        #region IOutgoingWebResponseContext Members

        public long ContentLength
        {
            get
            {
               return context.ContentLength;
            }
            set
            {
                context.ContentLength=value;
            }
        }

        public string ContentType
        {
            get
            {
               return context.ContentType;
            }
            set
            {
                context.ContentType = value;
            }
        }

        public string ETag
        {
            get
            {
                return context.ETag;
            }
            set
            {
                context.ETag = value;
            }
        }

        public System.Net.WebHeaderCollection Headers
        {
            get 
            {
                return context.Headers;
            }
        }

        public DateTime LastModified
        {
            get
            {
                return context.LastModified;
            }
            set
            {
                context.LastModified = value;
            }
        }

        public string Location
        {
            get
            {
                return context.Location;
            }
            set
            {
                context.Location = value;
            }
        }

        public System.Net.HttpStatusCode StatusCode
        {
            get
            {
                return context.StatusCode;
            }
            set
            {
                context.StatusCode = value;
            }
        }

        public string StatusDescription
        {
            get
            {
                return context.StatusDescription;
            }
            set
            {
                context.StatusDescription = value;
            }
        }

        public bool SuppressEntityBody
        {
            get
            {
                return context.SuppressEntityBody;
            }
            set
            {
                context.SuppressEntityBody = value;
            }
        }

        public void SetStatusAsCreated(Uri locationUri)
        {
            context.SetStatusAsCreated(locationUri);
        }

        public void SetStatusAsNotFound()
        {
            context.SetStatusAsNotFound();
        }

        public void SetStatusAsNotFound(string description)
        {
            context.SetStatusAsNotFound(description);
        }

        #endregion
    }
}
