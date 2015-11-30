using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// Encapsulates Document and relationship details
    /// </summary>
    public class EvDocumentDataEntity
    {
        private List<RVWDocumentBEO> documents;
        /// <summary>
        /// Set of Documents ready to be used by EV
        /// </summary>
        public IEnumerable<RVWDocumentBEO> Documents
        {
            get { return documents; }
            set 
            {
                documents = new List<RVWDocumentBEO>();
                documents = (List<RVWDocumentBEO>)value; 
            }
        }


        private List<RelationshipBEO> relationships;
        /// <summary>
        /// Relationships for documents in effect.
        /// </summary>
        public IEnumerable<RelationshipBEO> Relationships
        {
            get { return relationships; }
            set 
            {
                relationships = new List<RelationshipBEO>();
                relationships = (List<RelationshipBEO>)value; 
            }
        }

        private double percentComplete = 100;

        /// <summary>
        /// Gets or sets the percent complete in overall list of documents pushed for conversion
        /// </summary>
        /// <value>
        /// The percent complete.
        /// </value>
        public double PercentComplete
        {
            get
            {
                return percentComplete;
            }
            set
            {
                percentComplete = value;
            }
        }


        /// <summary>
        /// Gets or sets the outlook mail store data entity.
        /// </summary>
        /// <value>
        /// The outlook mail store data entity.
        /// </value>
        public OutlookMailStoreEntity OutlookMailStoreDataEntity
        {
            get;
            set;
        }

    }
}
