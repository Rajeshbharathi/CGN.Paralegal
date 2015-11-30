using LexisNexis.Evolution.Overdrive.MSMQ;

namespace LexisNexis.Evolution.Overdrive
{
    internal class ConcretePipeFactory
    {
        internal static IPipe CreatePipe(PipeName pipeName)
        {
            return new MSMQPipe(pipeName);
        }
    }
}
