namespace LexisNexis.Evolution.Overdrive
{
    using LexisNexis.Evolution.Infrastructure;

    public class TestPipeline : EVPipeline
    {
        protected override void PeriodicPipelineServicingHook()
        {
            base.PeriodicPipelineServicingHook();

            if (CurrentContinuation > NumberOfContinuations)
            {
                return;
            }

            if (PipelineStatus.PipelineState < PipelineState.FirstWorkerCompleted)
            {
                Tracer.Warning("FIRST WORKER HAS NOT COMPLETED YET!");
                return;
            }

            PipelineSection triggerSection = PipelineSections.Find(pipelineSection => pipelineSection.OrderNumber == 1);
            if (triggerSection == null)
            {
                return;
            }
            if (triggerSection.DataPipeName != null && triggerSection.DataPipeName.Count > 0)
            {
                Tracer.Warning("SECOND SECTION IS STILL BUSY!");
                return;
            }

            Tracer.Warning("ACTION GOES HERE!");
            CurrentContinuation++;
        }

        //protected override bool Completed()
        //{
        //    base.Completed();

        //    //Thread.Sleep(30000);

        //    CurrentContinuation++;
        //    if (CurrentContinuation > NumberOfContinuations)
        //    {
        //        return true;
        //    }

        //    PipelineSection pipelineSection = FindPipelineSection("S3");
        //    DataPipeName dataPipeName = pipelineSection.DataPipeName;
        //    using (var dataPipe = new Pipe(dataPipeName))
        //    {
        //        dataPipe.Open();

        //        var envelope = new PipeMessageEnvelope() { Label = "ContinuationRequest" };

        //        dataPipe.Send(envelope);
        //    }
        //    return false;
        //}

        private int CurrentContinuation { get; set; }

        private const int NumberOfContinuations = 0; // Could be 3
    }
}
