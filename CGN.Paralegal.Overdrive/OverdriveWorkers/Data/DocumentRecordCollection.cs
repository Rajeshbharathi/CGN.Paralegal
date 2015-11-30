using LexisNexis.Evolution.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class DocumentRecordCollection
    {
        /// <summary>
        /// This contains the reviewset creation details and description
        /// </summary>
        public ReviewsetRecord ReviewsetDetails { get; set; }

        /// <summary>
        /// This contains the search worker retrieved documents for the given query context
        /// </summary>
        public List<DocumentIdentityRecord> Documents { get; set; }

        /// <summary>
        /// This contains the total document count from the search performed in startup worker
        /// </summary>
        public int TotalDocumentCount { get; set; }

        public List<DocumentTagBEO> DocumentTags { get; set; }
    }
}
