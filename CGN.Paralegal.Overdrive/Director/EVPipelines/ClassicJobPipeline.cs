
namespace LexisNexis.Evolution.Overdrive
{
    public class ClassicJobPipeline : EVPipeline
    {
        protected override bool Completed()
        {
            base.Completed();

            // We never want classic job to come into Completed state, because it would cause Director to do all the activities related to Completed jobs 
            // and we want to avoid that. Classic jobs do everything themselves.
            return false;
        }
    }
}
