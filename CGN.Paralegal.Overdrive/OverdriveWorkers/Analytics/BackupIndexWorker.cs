using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;

namespace OverdriveWorkers.Analytics
{
    internal class BackupIndexWorker : WorkerBase
    {
        protected override void BeginWork()
        {
            base.BeginWork();
            BootParameters.ShouldNotBe(null);
            JobParameters.ShouldNotBe(null);
        }

        protected override bool GenerateMessage()
        {
            return true;
        }
    }
}
