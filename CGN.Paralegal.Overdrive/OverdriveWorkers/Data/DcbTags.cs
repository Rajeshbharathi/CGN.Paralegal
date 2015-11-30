using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class DcbTags
    {
        public string DatasetId { get; set; }
        public long MatterId { get; set; }
        public List<string> compositeTagNames { get; set; }
    }

    [Serializable]
    public class DcbDatabaseTags : DcbTags
    {
    }

    [Serializable]
    public class DcbDocumentTags : DcbTags
    {
        public string DocumentId { get; set; }
    }

}
