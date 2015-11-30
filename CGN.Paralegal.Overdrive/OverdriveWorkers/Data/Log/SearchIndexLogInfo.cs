using System;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Search Index - worker Processor Log Information
    /// </summary>
     [Serializable]
    public class SearchIndexLogInfo : BaseWorkerProcessLogInfo
    {
        //Additional properties will be added if required
        public static implicit operator string(SearchIndexLogInfo log)
        {
            return string.Empty;
        }

        public string DocumentId { get; set; }
        public string DCNNumber { get; set; }
    }
}
