using System;

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    /// Main purpose of this class is to provide workers with their assignments
    /// Object of this type is returned by WorkerManager WCF call in return to WorkerId string.
    /// This happens only once in worker life. After that worker holds on to this object.
    /// </summary>
    [Serializable]
    public class WorkAssignment
    {
        public WorkAssignment(WorkRequest workRequest, RoleSlotToken roleSlotToken, 
            int jobId, int scheduleRunDuration, string scheduleCreatedBy, long notificationId, string frequency)
        {
            WorkRequest = workRequest;
            RoleSlotToken = roleSlotToken;

            WorkerBadge = new WorkerBadge(this);

            // Information required to start classic jobs
            JobId = jobId;
            ScheduleRunDuration = scheduleRunDuration;
            ScheduleCreatedBy = scheduleCreatedBy;
            NotificationId = notificationId;
            Frequency = frequency;
        }

        public WorkRequest WorkRequest { get; private set; }
        public RoleSlotToken RoleSlotToken { get; private set; }

        public WorkerBadge WorkerBadge  { get; private set; }
        public string WorkerId
        {
            get { return WorkerBadge.WorkerId; }
        }

        // Information required to start classic jobs
        public int JobId { get; private set; }
        public int ScheduleRunDuration { get; private set; }
        public string ScheduleCreatedBy { get; private set; }
        public long NotificationId { get; private set; }
        public string Frequency { get; private set; }
    }
}