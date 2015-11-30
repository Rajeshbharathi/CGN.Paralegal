using System;

namespace LexisNexis.Evolution.Overdrive
{
    public class Pipe : IPipe
    {
        public Pipe(PipeName pipeName)
        {
            concretePipe = ConcretePipeFactory.CreatePipe(pipeName);
        }

        public PipeName Name
        {
            get
            {
                return concretePipe.Name;
            }
        }

        public void Create()
        {
            concretePipe.Create();
        }

        public void Open()
        {
            concretePipe.Open();
        }

        public void Delete()
        {
            concretePipe.Delete();
        }

        public void Purge()
        {
            concretePipe.Purge();
        }

        public void Send(PipeMessageEnvelope pipeMessage)
        {
            try
            {
                if (null != Before) Before();
                concretePipe.Send(pipeMessage);
            }
            finally
            {
                if (null != After) After();
            }
        }

        public PipeMessageEnvelope Receive()
        {
            try
            {
                if (null != Before) Before();
                return concretePipe.Receive();
            }
            finally
            {
                if (null != After) After();
            }
        }

        public PipeMessageEnvelope Receive(TimeSpan timeout)
        {
            try
            {
                if (null != Before) Before();
                return concretePipe.Receive(timeout);
            }
            finally
            {
                if (null != After) After();
            }
        }

        #region "static"
        //enumerate pipes on the machine
        //public IEnumerable<PipeName> GetPipes(string machineName)
        //{
        //    return concretePipe.GetPipes(machineName);
        //}
        #endregion

        #region IDisposable

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
                    // Dispose managed resources.
                    if (concretePipe != null)
                    {
                        concretePipe.Dispose();
                    }
                }
                // Dispose unmanaged resources.
            }
            disposed = true;
        }

        #endregion

        private readonly IPipe concretePipe;

        public Action Before { get; set; }
        public Action After { get; set; }
    }
}
