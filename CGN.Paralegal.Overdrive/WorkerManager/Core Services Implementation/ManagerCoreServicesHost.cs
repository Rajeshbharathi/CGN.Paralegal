using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    internal class ManagerCoreServicesHost : IDisposable
    {
        internal ManagerCoreServicesHost(IManagerCoreServices managerCoreServices)
        {
            _serviceHost = new ServiceHost(managerCoreServices, _baseAddress);
            // Enable metadata publishing.
            var smb = new ServiceMetadataBehavior { MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 } };
            _serviceHost.Description.Behaviors.Add(smb);

            // DEBUGGING
            ServiceDebugBehavior debug = _serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                _serviceHost.Description.Behaviors.Add(new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }


            // Usage BasicHttpBinding can be used if this is not going to be on the local machine.
            NetNamedPipeBinding binding = new NetNamedPipeBinding
                        {
                            CloseTimeout = new TimeSpan(0, 3, 0),
                            OpenTimeout = new TimeSpan(0, 3, 0),
                            ReceiveTimeout = new TimeSpan(2, 0, 10, 0),
                            SendTimeout = new TimeSpan(0, 3, 0),
                            ReaderQuotas = { MaxStringContentLength = 2147483647, MaxArrayLength = 2147483647, MaxDepth = 2147483647, MaxBytesPerRead = 2147483647, MaxNameTableCharCount = 2147483647 },
                            MaxReceivedMessageSize = 2147483647,
                        };
            // That's two days and 10 minutes


            _serviceHost.AddServiceEndpoint(typeof(IManagerCoreServices), binding, "ManagerCoreServices");
            _serviceHost.Open();

            Tracer.Info("ManagerCoreServices is ready at {0}", _baseAddress);
        }

        private ServiceHost _serviceHost;
        private readonly Uri _baseAddress = new Uri("net.pipe://localhost/Pipe");

        public void Dispose()
        {
            if (null != _serviceHost)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }
        }
    }
}
