using System;

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    /// This is the base worker class.
    /// </summary>
    /// <remarks></remarks>
    public abstract partial class WorkerBase : MarshalByRefObject
    {
        protected virtual void BeginWork()
        {
        }

        protected virtual void ProcessMessage(PipeMessageEnvelope envelope)
        {
        }

        protected virtual bool GenerateMessage()
        {
            return true;
        }

        protected virtual void HandleState(WorkerState workerState)
        {
        }

        protected virtual void EndWork()
        {
        }
    }
}
