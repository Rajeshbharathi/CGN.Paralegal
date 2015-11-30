#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetRecord.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Cognizant</author>
//      <description>
//          Entity For Reviewset creation
//      </description>
//      <changelog>
//          <date value="12-Jan-2012"></date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces
using System;
using LexisNexis.Evolution.BusinessEntities;
using System.Collections.Generic;
using LexisNexis.Evolution.DataContracts;

#endregion

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class BulkTagRecord
    {

        private TagRecord _tagDetails;
        /// <summary>
        /// Object containing details of tag to be tagged with
        /// </summary>
        public TagRecord TagDetails
        {
            get { return _tagDetails ?? (_tagDetails = new TagRecord()); }
            set
            {
                _tagDetails = value;
            }
        }


        private List<BulkDocumentInfoBEO> _documents;
        /// <summary>
        /// Object containing details about the documents to tag
        /// </summary>
        public List<BulkDocumentInfoBEO> Documents
        {
            get { return _documents ?? (_documents = new List<BulkDocumentInfoBEO>()); }
            set
            {
                _documents = value;
            }
        }

        /// <summary>
        /// Object containing search context for the document retrieval
        /// </summary>
        public DocumentQueryEntity SearchContext
        {
            get;
            set;
        }

        public List<DocumentTagBEO> CurrentState { get; set; }

        public List<DocumentTagBEO> NewState
        {
            get;
            set;
        }

        public List<DocumentTagBEO> VaultState
        {
            get;
            set;
        }

        public BulkTaggingNotificationBEO Notification
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the review set ID.
        /// </summary>
        public string ReviewSetId { get; set; }

        /// <summary>
        /// Gets or Sets the Dataset Id
        /// </summary>

        public long DatasetId { get; set; }

        /// <summary>
        /// Gets or Sets the Dataset Id
        /// </summary>
        public long MatterId { get; set; }

        /// <summary>
        /// Gets or Sets the Collection Id
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// Gets or sets the Binder ID.
        /// </summary>
        public string BinderId { get; set; }

        /// <summary>
        /// Gets or sets the number of documents per Review Set.
        /// </summary>
        /// <value>The number of documents.</value>
        public long NumberOfOriginalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the number of batches that documents will be sent to next workers
        /// </summary>
        public int NumberOfBatches { get; set; }

        /// <summary>
        /// Gets or sets the number of documents per review set.
        /// </summary>
        /// <value>The number of documents per set.</value>
        public int NumberOfDocumentsPerSet { get; set; }

        /// <summary>
        /// Gets or sets the originator forindex enqueue validation
        /// </summary>
        public string Originator { get; set; }

        /// <summary>
        /// Gets or sets the user guid
        /// </summary>
        public string CreatedByUserGuid { get; set; }
        /// <summary>
        /// Gets or sets the Timestamp taggig took place
        /// </summary>
        public DateTime TagTimeStamp { get; set; }
    }
}
