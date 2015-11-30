using System;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Base Worker Processor Log Information
    /// </summary>
    [Serializable]
    public class BaseWorkerProcessLogInfo
    {
        public string Information { get; set; }
        public string Message { get; set; }
        public string CrossReferenceField { get; set; }        
    }   
}
