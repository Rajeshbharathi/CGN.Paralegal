using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Overdrive
{
    using LexisNexis.Evolution.BusinessEntities;

    [Serializable]
    public class JobInfo
    {
        public JobInfo()
        { }

        public JobInfo(int jobId, int jobRunId, int jobTypeId, List<WorkRequest> workRequests, string bootParameters, BaseJobBEO jobParameters)
        {
            JobId = jobId;
            PipelineId = jobRunId.ToString();
            JobTypeId = jobTypeId;
            WorkRequests = workRequests;
            BootParameters = bootParameters;
            Command = Command.Run;
        }

        public int JobId { get; private set; }
        public string PipelineId { get; private set; }
        public int JobTypeId { get; private set; }
        public List<WorkRequest> WorkRequests { get; private set; }
        public string BootParameters { get; set; }
        public BaseJobBEO JobParameters { get; set; }
        public Command Command { get; set; }
        public int ScheduleRunDuration { get; set; }
        public string ScheduleCreatedBy { get; set; }
        public long NotificationId { get; set; }
        public string Frequency { get; set; }
    }
}
