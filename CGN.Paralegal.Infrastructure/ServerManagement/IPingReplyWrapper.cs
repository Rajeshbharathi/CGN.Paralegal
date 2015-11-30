namespace CGN.Paralegal.Infrastructure.ServerManagement
{
    public interface IPingReplyWrapper
    {
        System.Net.IPAddress Address { get; }
        System.Net.NetworkInformation.PingOptions Options { get; }
        long RoundtripTime { get; }
        System.Net.NetworkInformation.IPStatus Status { get; }
    }
}
