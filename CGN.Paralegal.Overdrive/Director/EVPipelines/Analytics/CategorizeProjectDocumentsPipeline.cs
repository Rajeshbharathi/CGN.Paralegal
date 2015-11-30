using LexisNexis.Evolution.Business;

namespace LexisNexis.Evolution.Overdrive
{
    public class CategorizeProjectDocumentsPipeline : EVPipeline
    {
        private string _jobBootParameter;
        
        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);
            _jobBootParameter = activeJob.BootParameters.ToString();
            
        }



        /// <summary>
        /// Completed this instance.
        /// </summary>
        /// <returns></returns>
        protected override bool Completed()
        {
            base.Completed();
            return AnalyticsJob.CategorizeProjectDocumentsPipelineComplete(JobId, _jobBootParameter);
        }
    }
}
