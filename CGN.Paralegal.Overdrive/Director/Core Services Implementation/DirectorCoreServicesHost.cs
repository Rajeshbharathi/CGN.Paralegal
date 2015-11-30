using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    public class DirectorCoreServicesHost : IDisposable
    {
        private readonly ServiceHost serviceHost;
        private readonly Uri tcpBaseAddress = new Uri("net.tcp://localhost:2011");
        private readonly Uri httpBaseAddress = new Uri("http://localhost:2010");

        public DirectorCoreServicesHost()
        {
            this.serviceHost = new ServiceHost(typeof(DirectorCoreServices), this.tcpBaseAddress, this.httpBaseAddress);

            this.serviceHost.AddServiceEndpoint(typeof(IDirectorCoreServices), new NetTcpBinding(), this.tcpBaseAddress);
            this.serviceHost.AddServiceEndpoint(typeof(IDirectorCoreServices), new BasicHttpBinding(), this.httpBaseAddress);

            ServiceMetadataBehavior metadataBehavior = new ServiceMetadataBehavior() { HttpGetEnabled = true, MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 } };
            this.serviceHost.Description.Behaviors.Add(metadataBehavior);
            Binding mexBinding = MetadataExchangeBindings.CreateMexTcpBinding();
            this.serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, this.tcpBaseAddress + @"/mex");

            // DEBUGGING
            ServiceDebugBehavior debug = this.serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                this.serviceHost.Description.Behaviors.Add(new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                debug.IncludeExceptionDetailInFaults = true;
            }


            foreach (ServiceEndpoint serviceEndpoint in this.serviceHost.Description.Endpoints)
            {
                Binding binding = serviceEndpoint.Binding;

                NetTcpBinding netTcpBinding = binding as NetTcpBinding;
                if (netTcpBinding != null)
                {
                    netTcpBinding.MaxBufferSize = 2147483647;
                    netTcpBinding.MaxReceivedMessageSize = 2147483647;
                    netTcpBinding.Security.Mode = SecurityMode.None;
                    netTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                    netTcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.None;
                }

                BasicHttpBinding basicHttpBinding = binding as BasicHttpBinding;
                if (basicHttpBinding != null)
                {
                    basicHttpBinding.MaxReceivedMessageSize = 2147483647;
                    basicHttpBinding.Security.Mode = BasicHttpSecurityMode.None;
                    basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                    basicHttpBinding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                }
            }

            // Open the ServiceHost to start listening for messages. Since
            // no endpoints are explicitly configured, the runtime will create
            // one endpoint per base address for each service contract implemented
            // by the service.
            this.serviceHost.Open();

            Tracer.Info("DirectorCoreServices is ready at {0}", this.tcpBaseAddress);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false; // to detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (null != serviceHost)
                    {
                        serviceHost.Close();
                    }
                }

                // shared cleanup logic
                disposed = true;
            }
        }
    }
}