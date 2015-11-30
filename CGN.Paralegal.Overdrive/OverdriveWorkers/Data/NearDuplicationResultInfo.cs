using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class NearDuplicationResultInfo
    {
        public string DocumentId { get; set; }
        public int ClusterSort { get; set; }
        public int FamilySort { get; set; }
        public int DocumentSort { get; set; }
        public bool IsMaster { get; set; }
        public string Source { get; set; }
        public string Similarity { get; set; }
    }
}
