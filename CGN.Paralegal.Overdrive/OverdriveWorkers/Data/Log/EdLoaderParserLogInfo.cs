using System.Text;
using System;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// EDLoader parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class EdLoaderParserLogInfo : BaseWorkerProcessLogInfo
    {
        public string DocumentId { get; set; }
        public string DCN { get; set; }
        public int AddedDocument { get; set; }
        public int UpdatedDocument { get; set; }

        public static implicit operator string(EdLoaderParserLogInfo log)
        {
            var info = new StringBuilder();
            info.Append("DCN : " + log.DCN + " \n, ");
            info.Append("Added Document : " + log.AddedDocument + " \n, ");          
            return info.ToString();
        }
    }
}
