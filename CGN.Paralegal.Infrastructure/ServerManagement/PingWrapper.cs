using System.Net;
using System.Net.NetworkInformation;

namespace CGN.Paralegal.Infrastructure.ServerManagement
{
    /// <summary>
    /// PingWrapper Class
    /// </summary>
    public class PingWrapper : IPingWrapper
    {
        private Ping _ping;

        /// <summary>
        /// Calls the Send Method of Ping Object
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <returns></returns>
        public IPingReplyWrapper Send(string hostNameOrAddress)
        {
            return new PingReplyWrapper(_ping.Send(hostNameOrAddress));
        }

        /// <summary>
        /// Constructor for PingWrapper.
        /// </summary>
        /// <param name="ping">Ping Type Instance.</param>
        public PingWrapper(Ping ping)
        {
            _ping = ping;
        }
    }

    public class PingReplyWrapper : IPingReplyWrapper
    {
        private PingReply _pingReply;

        // Summary:
        //     Gets the address of the host that sends the Internet Control Message Protocol
        //     (ICMP) echo reply.
        //
        // Returns:
        //     An System.Net.IPAddress containing the destination for the ICMP echo message.
        public IPAddress Address
        {
            get
            {
                return _pingReply.Address;
            }
        }

        //
        // Summary:
        //     Gets the options used to transmit the reply to an Internet Control Message
        //     Protocol (ICMP) echo request.
        //
        // Returns:
        //     A System.Net.NetworkInformation.PingOptions object that contains the Time
        //     to Live (TTL) and the fragmentation directive used for transmitting the reply
        //     if System.Net.NetworkInformation.PingReply.Status is System.Net.NetworkInformation.IPStatus.Success;
        //     otherwise, null.
        public PingOptions Options
        {
            get
            {
                return _pingReply.Options;
            }
        }
        //
        // Summary:
        //     Gets the number of milliseconds taken to send an Internet Control Message
        //     Protocol (ICMP) echo request and receive the corresponding ICMP echo reply
        //     message.
        //
        // Returns:
        //     An System.Int64 that specifies the round trip time, in milliseconds.
        public long RoundtripTime
        {
            get
            {
                return _pingReply.RoundtripTime;
            }
        }
        //
        // Summary:
        //     Gets the status of an attempt to send an Internet Control Message Protocol
        //     (ICMP) echo request and receive the corresponding ICMP echo reply message.
        //
        // Returns:
        //     An System.Net.NetworkInformation.IPStatus value indicating the result of
        //     the request.
        public IPStatus Status
        {
            get
            {
                return _pingReply.Status;
            }
        }

        public PingReplyWrapper(PingReply pingReply)
        {
            _pingReply = pingReply;
        }
    }
}
