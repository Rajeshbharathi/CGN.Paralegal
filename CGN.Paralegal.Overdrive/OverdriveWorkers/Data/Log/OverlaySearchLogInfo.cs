using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
     /// <summary>
    /// Overlay search - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class OverlaySearchLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(OverlaySearchLogInfo overlayLogInfo)
        {
            var info = new StringBuilder();
            info.Append("Search : " + overlayLogInfo.SearchMessage + " \n, ");
            info.Append("Overlay Doocument Updated : " + ((overlayLogInfo.IsDocumentUpdated)?"Yes":"No") + " \n, ");
            info.Append("Overlay Field : " + overlayLogInfo.OverlayFieldInfo + " \n, ");
            if(!overlayLogInfo.IsDocumentUpdated)
                 info.Append("Non match overlay DCN : " + overlayLogInfo.NonMatchOverlayDCN + " \n, ");   
            return info.ToString();
        }
        public string SearchMessage { get; set; }
        public bool IsDocumentUpdated { get; set; }
        public string OverlayDocumentId { get; set; }
        public bool IsNoMatch { get; set; }
        public string OverlayFieldInfo { get; set; }
        public string NonMatchOverlayDCN { get; set; }
        public string DCN { get; set; }
        public bool IsDocumentAdded { get; set; }
    }
}
