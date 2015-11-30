using System;

namespace LexisNexis.Evolution.Overdrive
{
    public enum Command
    {
        Run,
        Pause,
        Cancel,
    };

    public enum WorkerState
    {
        Constructed,
        Starting,
        Started,
        Busy,
        Idle, // Workers with input data pipe get into this state when there are no messages to process
        Completed, // Workers without input data pipe get into that state when they are completed.
        Paused,
        CleaningUp,
        Quit,
    };

    [Serializable]
    public class WorkerStateChangedMessage
    {
        public WorkerStateChangedMessage(WorkAssignment workAssignment, WorkerState workerState)
        {
            WorkerBadge = workAssignment.WorkerBadge;
            WorkerState = workerState;
        }

        public WorkerBadge WorkerBadge { get; private set; }
        public WorkerState WorkerState { get; private set; }

        public static readonly string MessageLabel = "WorkerStateChanged";
    }
}