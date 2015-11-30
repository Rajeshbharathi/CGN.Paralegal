using System;
using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.Worker
{
    [Serializable]
    public class ExportDocumentCollection
    {
        public List<ExportDocumentDetail> Documents { get; set; }
        public DatasetBEO Dataset { get; set; }
        public ExportOption ExportOption { get; set; }
    }


}
