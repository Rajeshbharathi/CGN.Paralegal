using System.Collections.Generic;
using System.ServiceModel;

namespace LexisNexis.Evolution.Overdrive
{
    [ServiceContract(SessionMode = SessionMode.Allowed)]
    public interface IDirectorCoreServices
    {
        [OperationContract]
        OpenJobs GetOpenJobs(string machineName);

        [OperationContract]
        JobInfo GetJobInfo(string pipelineId, string machineName);

        [OperationContract]
        List<WorkerStatistics> GetWorkerStatistics(string pipelineId, string machineName);
    }
}