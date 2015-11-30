using System;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Vault parser - worker Processor Log Information
    /// </summary>
     [Serializable]
    public class VaultLogInfo : BaseWorkerProcessLogInfo
    {
        //Additional properties will be added if required
        public static implicit operator string(VaultLogInfo log)
        {
            return string.Empty;
        }
        public string DCN { get; set; }
    }
}
