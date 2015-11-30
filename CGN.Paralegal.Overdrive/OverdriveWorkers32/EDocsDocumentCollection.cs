using System;
using System.Collections.Generic;
using LexisNexis.Evolution.DocumentExtractionUtilities;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Encapsulates data collection specific to EDocs extraction process.
    /// </summary>
    [Serializable]
    public class EDocsDocumentCollection
    {
        /// <summary>
        /// Gets or sets the document data collection.
        /// </summary>
        /// <value>
        /// The document data collection.
        /// </value>        
        public DocumentCollection DocumentDataCollection 
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the outlook mail store data entity.
        /// </summary>
        /// <value>
        /// The outlook mail store data entity.
        /// </value>        
        public List<OutlookMailStoreEntity> OutlookMailStoreDataEntity
        {
            get;
            set;
        }
    }
}
