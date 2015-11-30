using LexisNexis.Evolution.BusinessEntities;
using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker
{
    [Serializable]
    public class ExportDocumentDetail
    {
        public string DocumentId { get; set; }
        public string DCN { get; set; }
        public List<RVWDocumentFieldBEO> Fields { get; set; }       
        public List<RVWDocumentTagBEO> Tags { get; set; }
        public List<string> Comments { get; set; }         
        public string CorrelationId { get; set; }
        public List<ExportFileInformation> NativeFiles { get; set; }
        public List<ExportFileInformation> TextFiles { get; set; }
        public List<ExportFileInformation> ImageFiles { get; set; }
        public string BeginDoc { get; set; }
        public string EndDoc { get; set; }
        public bool IsNativeTagExists { get; set; }
        public string TextFileName { get; set; }
        public string NativeFileName { get; set; }

        public string ExportBasePath { get; set; }
    }
}
