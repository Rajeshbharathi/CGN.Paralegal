using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Near Duplication Worker Log Information
    /// </summary>
    [Serializable]
    public class NearDuplicationLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(NearDuplicationLogInfo logInfo)
        {
            var info = new StringBuilder();
            info.Append("Failure for DCN : " + logInfo.DocumentControlNumber + "  \n, ");
            return info.ToString();
        }
        public string DocumentControlNumber { get; set; }
        public bool IsMissingText { get; set; }
        public bool IsFailureInDatabaseUpdate { get; set; }
        public bool IsFailureInSearchUpdate { get; set; }
    }
}
