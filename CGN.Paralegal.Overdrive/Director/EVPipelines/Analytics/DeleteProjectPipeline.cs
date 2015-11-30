namespace LexisNexis.Evolution.Overdrive
{
    public class DeleteProjectPipeline : EVPipeline
    {
        // This is the flag which basically checks if Completed() is called for the first time or for the second time
        private bool _finalizationRequested = false;


        protected override bool Completed()
        {
            base.Completed();

            if (_finalizationRequested) // Completed() called for the second time 
            {
                return true; // We can let pipeline to declare completion now
            }
            SendMessage();
            return false; // We cannot let the pipeline to declare completion yet
        }

        /// <summary>
        /// Send Message
        /// </summary>
        private void SendMessage()
        {
            // We get here if Completed() is called for the first time. 
            // Need to send special message to DeleteProjectCleanup worker.
            var pipelineSection = FindPipelineSection("DeleteProjectCleanup");
            var dataPipeName = pipelineSection.DataPipeName;
            using (var dataPipe = new Pipe(dataPipeName))
            {
                dataPipe.Open();
                var envelope = new PipeMessageEnvelope() { Label = "PleaseCleanup" };
                dataPipe.Send(envelope);
            }
            _finalizationRequested = true;
        }
    }
}
