using System;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class WorkerMessage
    {
        public WorkerMessage(string workerId, string message)
        {
            WorkerId = workerId;
            Message = message;
            TimeStamp = DateTime.Now;
        }

        public string WorkerId { get; private set; }

        public string Message { get; private set; }

        public DateTime TimeStamp { get; private set; }

        public override string ToString()
        {
            string messageString = WorkerId + ':';
            if (!String.IsNullOrEmpty(Message))
            {
                messageString += ' ' + Message; 
            }
            return messageString;
        }

        [NonSerialized]
        public static readonly string MessageLabel = "WorkerMessage";
    }
}
