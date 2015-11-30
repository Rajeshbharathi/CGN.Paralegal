using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class DocumentIdentityRecord
    {
        /// <summary>
        /// Contains the numeric document identifier
        /// </summary>
        public long Id { get; set; }

        public string DocumentId { get; set; }

        public string MatterId { get; set; }

        public string DatasetId { get; set; }

        public string CollectionId { get; set; }

        public string ReviewsetId { get; set; }

        public bool IsLocked { get; set; }        

        public string FamilyId { get; set; }

        public string DuplicateId { get; set; }

        //This group identifier is a special identifier to group the documents which belongs to duplicate and family combination
        public string GroupId { get; set; }

        public string ParentId { get; set; }

        public string DocumentControlNumber { get; set; }

        public Uri DocumentCrawlUrl { get; set; }

        private List<FieldRecord> _fields;

        /// <summary>
        /// List of fields associated to the document
        /// </summary>
        public List<FieldRecord> Fields
        {
            get
            {
                return _fields ?? (_fields = new List<FieldRecord>());
            }
        }
    }
}
