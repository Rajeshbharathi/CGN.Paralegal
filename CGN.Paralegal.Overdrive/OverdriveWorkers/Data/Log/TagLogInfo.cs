using System;
using System.Text;


namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Near Duplication Worker Log Information
    /// </summary>
    [Serializable]
    public class TagLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(TagLogInfo logInfo)
        {
            var info = new StringBuilder();
            info.Append("Failure for DCN : " + logInfo.DocumentControlNumber + "  \n, ");
            return info.ToString();
        }
        public string DocumentId { get; set; }
        public string DocumentControlNumber { get; set; }
        public bool IsFailureInDatabaseUpdate { get; set; }
        public bool IsFailureInSearchUpdate { get; set; }
    }
}
