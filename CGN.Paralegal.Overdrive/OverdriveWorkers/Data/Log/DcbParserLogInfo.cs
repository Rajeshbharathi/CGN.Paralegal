using System.Text;
using System;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// DCB document parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class DcbParserLogInfo : BaseWorkerProcessLogInfo
    {
        public string DocumentId { get; set; }
        public int AddedImages { get; set; }

        public static implicit operator string(DcbParserLogInfo log)
        {
            var info = new StringBuilder();
            info.Append("Added Images : " + log.AddedImages + " \n, ");
            return info.ToString();
        }
    }
}
