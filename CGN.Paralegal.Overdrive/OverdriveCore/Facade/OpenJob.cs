using System;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class OpenJob
    {
        public string PipelineId { get; set; }
        public Command Command { get; set; }
    }
}
