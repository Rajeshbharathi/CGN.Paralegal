using System;
using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;

namespace OverdriveWorkers.Data
{
    [Serializable]
    public class LawDocumentCollection
    {
        /// <summary>
        /// Gets or sets the document collection.
        /// </summary>
        /// <remarks></remarks>
        public List<RVWDocumentBEO> Documents { get; set; }

        /// <summary>
        /// Gets or sets the dataset details.
        /// </summary>
        /// <remarks></remarks>
        public DatasetBEO Dataset { get; set; }
    }
}
