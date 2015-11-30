using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class IndexDocumentRecord
    {
        public string Id { get; set; }

        public long MatterId { get; set; }

        public long DatasetId { get; set; }

        public string CollectionId { get; set; }
        public string TextFilePath { get; set; }

        public string NativeFilePath { get; set; }

        public string Dcn { get; set; }


        public IDictionary<string, string> Fields = new Dictionary<string, string>();
    }
}
