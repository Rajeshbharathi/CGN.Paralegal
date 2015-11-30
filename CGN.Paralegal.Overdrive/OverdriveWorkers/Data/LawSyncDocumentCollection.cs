using LexisNexis.Evolution.BusinessEntities.Law;
using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class LawSyncDocumentCollection
    {
        public List<LawSyncDocumentDetail> Documents { get; set; }
        public long DatasetId { get; set; }

        public string DatasetCollectionId { get; set; }

        public string RedactableSetCollectionId { get; set; }

        public long MatterId { get; set; }

        public long LawSynJobId { get; set; }

        public string DatasetExtractionPath { get; set; }

        public bool IsLawSyncReprocessJob { get; set; }

        public LawSyncBEO OrginalJobParameter { get; set; }
    }
}
