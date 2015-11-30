using LexisNexis.Evolution.Business;

namespace LexisNexis.Evolution.Overdrive
{
    public class IncludeSubSystemsPipeline : EVPipeline
    {
        // This is the flag which basically checks if Completed() is called for the first time or for the second time
        private bool _finalizationRequested = false;
        private string _jobBootParameter;

        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);
            _jobBootParameter = activeJob.BootParameters.ToString();
        }

        protected override bool Completed()
        {
            base.Completed();

            if (_finalizationRequested) // Completed() called for the second time 
            {
                AnalyticsJob.IncludeSubSystemsUpdateJobStatus(JobId, _jobBootParameter);
                return true; // We can let pipeline to declare completion now
            }
            SendMessage();
            return false; // We cannot let the pipeline to declare completion yet
        }

        /// <summary>
        /// Send message
        /// </summary>
        private void SendMessage()
        {
            // We get here if Completed() is called for the first time. 
            // Need to send special message to DeleteProjectCleanup worker.
            var pipelineSection = FindPipelineSection("IncludeSubSystemsFinal");
            var dataPipeName = pipelineSection.DataPipeName;
            using (var dataPipe = new Pipe(dataPipeName))
            {
                dataPipe.Open();
                var envelope = new PipeMessageEnvelope() { Label = "PleaseBuildProject" };
                dataPipe.Send(envelope);
            }
            _finalizationRequested = true;
        }
       
    }
}
