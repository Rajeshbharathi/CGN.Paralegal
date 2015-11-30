using System.Text;
using System;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// LoadFile Document parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class LoadFileDocumentParserLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(LoadFileDocumentParserLogInfo loadFileDocumentParserLogInfo)
        {
            var info = new StringBuilder();
            info.Append("Document Id : " + loadFileDocumentParserLogInfo.DocumentId + " \n, ");
            info.Append("Added Document : " + loadFileDocumentParserLogInfo.AddedDocument + " \n, ");
            info.Append("Added Images : " + loadFileDocumentParserLogInfo.AddedImages + " \n, ");
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

