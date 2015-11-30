

namespace LexisNexis.Evolution.Overdrive
{
    public class EVPipeline : Pipeline
    {
        public int JobId { get; private set; }
        public int JobTypeId { get; private set; }

        public void SetGeneralProperties(int jobId, int jobTypeId)
        {
            JobId = jobId;
            JobTypeId = jobTypeId;
        }

        internal virtual void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {

        }

      
    }
}
