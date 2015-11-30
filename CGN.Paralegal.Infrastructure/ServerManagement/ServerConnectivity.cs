using System;
using System.Net;
using System.Net.NetworkInformation;
using CGN.Paralegal.Infrastructure.EVContainer;
using CGN.Paralegal.Infrastructure.ExceptionManagement;

namespace CGN.Paralegal.Infrastructure.ServerManagement
{
    public sealed class ServerConnectivity
    {
        private ServerConnectivity()
        {

        }

        public static bool CheckConnectivity(string ipAddress)
        {
            IPingWrapper ping;
            IPingReplyWrapper pingreply;

            try
            {
                ping = EVUnityContainer.Resolve<IPingWrapper>();
                pingreply = ping.Send(ipAddress);
                if (pingreply.Status != IPStatus.Success)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                ex.Swallow();
                return false;
            }
            finally
            {
                pingreply = null;
                ping = null;
            }
        }

        public static string GetHostIPAddress()
        {
            string hostID = string.Empty;
            string hostName = string.Empty;
            hostName = Dns.GetHostName();
            IPHostEntry machineIp = Dns.GetHostEntry(hostName);
            foreach (IPAddress ip in machineIp.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    hostID = ip.ToString();
                    break;
                }
            }

            return hostID;
        }
    }
}
