using System;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class ProblemReport
    {
        public ProblemReport(string workerId, WorkerStage workerStage, Exception ex)
        {
            WorkerId = workerId;
            WorkerStage = workerStage;
            if (ex != null)
            {
                ExceptionString = ex.ToDebugString();
            }
        }

        public string WorkerId { get; private set; }
        public WorkerStage WorkerStage { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public string ExceptionString { get; private set; } 

        public void SendProblemReport(Pipe ReportPipe)
        {
            TimeStamp = DateTime.Now;
            var envelope = new PipeMessageEnvelope()
                                {
                                    Body = this,
                                    Label = MessageLabel,
                                };
            ReportPipe.Send(envelope);
        }

        public override string ToString()
        {
            return String.Format("  Worker Id: {0}\n  Worker Stage: {1}\n  Exception: {2}", WorkerId, WorkerStage, ExceptionString);
        }

        [NonSerialized]
        public static readonly string MessageLabel = "ProblemReport";
    }

    [Serializable]
    public enum WorkerStage
    {
        Unknown,
        BeginWork,
        WorkerStep,
        EndWork
    }
}
