using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace LexisNexis.Evolution.Overdrive
{
    class ManagerCoreServicesClient
    {
        internal ManagerCoreServicesClient()
        {
            NetNamedPipeBinding binding = new NetNamedPipeBinding
                                              {
                                                  CloseTimeout = new TimeSpan(0, 3, 0),
                                                  OpenTimeout = new TimeSpan(0, 3, 0),
                                                  ReceiveTimeout = new TimeSpan(2, 0, 10, 0),
                                                  SendTimeout = new TimeSpan(0, 3, 0),
                                                  ReaderQuotas = { MaxStringContentLength = 2147483647, MaxArrayLength = 2147483647, MaxDepth = 2147483647, MaxBytesPerRead = 2147483647, MaxNameTableCharCount = 2147483647 },
                                                  MaxReceivedMessageSize = 2147483647,
                                              };

            EndpointAddress endpointAddress = new EndpointAddress("net.pipe://localhost/Pipe/ManagerCoreServices");
            ChannelFactory<IManagerCoreServices> pipeFactory = new ChannelFactory<IManagerCoreServices>(binding, endpointAddress);

            ManagerCoreServicesProxy = pipeFactory.CreateChannel();
            ((IContextChannel)ManagerCoreServicesProxy).OperationTimeout = new TimeSpan(0, 10, 0); // 10 min to allow comfortable debugging
        }

        internal IManagerCoreServices ManagerCoreServicesProxy { get; set; }
    }
}
