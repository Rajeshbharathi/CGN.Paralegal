using System;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Near Duplication Worker Log Information
    /// </summary>
    [Serializable]
    public class LawSyncLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(LawSyncLogInfo logInfo)
        {
            var info = new StringBuilder();
            info.Append("Law Document Id : " + logInfo.LawDocumentId + "  \n, ");
            info.Append("DCN : " + logInfo.DocumentControlNumber + "  \n ");
            return info.ToString();
        }
        public int LawDocumentId { get; set; }
        public string DocumentControlNumber { get; set; }
        public bool IsFailureInSyncImage { get; set; }
        public bool IsFailureInSyncMetadata { get; set; }
        public bool IsFailureInImageConversion { get; set; }
    }
}
