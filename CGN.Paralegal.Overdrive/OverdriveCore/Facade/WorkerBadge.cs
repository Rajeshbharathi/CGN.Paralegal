using System;

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    /// This class is compact (but full and unique) identification of the worker
    /// Used as part of different messages workers send to ReportPipe
    /// </summary>
    [Serializable]
    public class WorkerBadge
    {
        public WorkerBadge(WorkAssignment workAssignment)
        {
            SectionName = workAssignment.WorkRequest.SectionName;
            RoleSlotToken = workAssignment.RoleSlotToken;

            MachineName = Environment.MachineName;

            WorkerId = workAssignment.WorkRequest.PipelineId + "_" +
                       workAssignment.WorkRequest.SectionName + "_" +
                       workAssignment.RoleSlotToken.SlotId + "_" +
                       Environment.MachineName;
        }

        public string SectionName { get; private set; }
        public RoleSlotToken RoleSlotToken { get; private set; }

        public string MachineName { get; private set; }

        public string WorkerId { get; private set; }
    }
}