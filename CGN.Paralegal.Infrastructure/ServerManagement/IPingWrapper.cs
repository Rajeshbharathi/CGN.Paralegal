namespace CGN.Paralegal.Infrastructure.ServerManagement
{
    public interface IPingWrapper
    {
        IPingReplyWrapper Send(string hostNameOrAddress);
    }
}
