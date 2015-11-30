using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// LoadFile Document parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class LawImportLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(LawImportLogInfo lawImportLogInfo)
        {
            var info = new StringBuilder();
            info.Append("Document Id : " + lawImportLogInfo.DocumentId + " \n, ");
            info.Append("Added Document : " + lawImportLogInfo.AddedDocument + " \n, ");
            info.Append("Added Images : " + lawImportLogInfo.AddedImages + " \n, ");
            return info.ToString();
        }
        public string DocumentId { get; set; }
        public bool IsMissingNative { get; set; }
        public bool IsMissingImage { get; set; }
        public bool IsMissingText { get; set; }
        public int AddedDocument { get; set; }
        public int UpdatedDocument { get; set; }
        public int NonMatchRecord { get; set; }
        public int AddedImages { get; set; }
        public string DCN { get; set; }
    }
}
