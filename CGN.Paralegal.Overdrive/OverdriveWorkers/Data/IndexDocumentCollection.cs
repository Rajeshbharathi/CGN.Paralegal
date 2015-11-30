using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class IndexDocumentCollection
    {
        public MatterReadRequest MatterDetails { get; set; }

        public List<IndexDocumentRecord> Documents { get; set; }
    }
}
