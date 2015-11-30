using System;

namespace LexisNexis.Evolution.Overdrive
{
    interface IPipe : IDisposable
    {
        PipeName Name { get; }

        void Create();

        void Open();

        void Delete();

        void Purge();

        void Send(PipeMessageEnvelope pipeMessage);

        PipeMessageEnvelope Receive();
        PipeMessageEnvelope Receive(TimeSpan timeout);

        #region "static"
        //enumerate pipes on the machine
        //IEnumerable<PipeName> GetPipes(string machineName);
        #endregion
    }
}

// FYI: "abstract" always implies "virtual" and requires "override" in derived classes