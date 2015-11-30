using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ConversationInfo
    {
        public long JobRunId { get; set; }
        public string DocId { get; set; }
        public string ConversationIndex { get; set; }
        public string ParentId { get; set; }
    }
}
