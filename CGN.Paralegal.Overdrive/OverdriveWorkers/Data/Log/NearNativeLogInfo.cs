using System;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Near Native(Redact IT) parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class NearNativeLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(NearNativeLogInfo log)
        {          
            return string.Empty;
        }
    }
}
