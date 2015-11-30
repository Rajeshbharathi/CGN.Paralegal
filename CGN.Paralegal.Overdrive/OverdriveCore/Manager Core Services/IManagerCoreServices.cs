using System;
using System.ServiceModel;

namespace LexisNexis.Evolution.Overdrive
{
    [ServiceContract(SessionMode = SessionMode.Allowed)]
    public interface IManagerCoreServices
    {
        [OperationContract]
        //[UseNetDataContractSerializer]
        WorkAssignment GetWorkAssignment(string workerId);

        [OperationContract]
        //[UseNetDataContractSerializer]
        Command GetWorkerState(string workerId);
    }
}
