using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ProductionDocumentCollection
    {
        public List<ProductionDocumentDetail> Documents { get; set; }
    }
}
