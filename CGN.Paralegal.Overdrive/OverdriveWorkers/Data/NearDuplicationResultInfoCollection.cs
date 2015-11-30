using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class NearDuplicationResultInfoCollection
    {
        /// <summary>
        /// Gets Records.
        /// </summary>
        /// <remarks></remarks>
        public List<NearDuplicationResultInfo> ResultDocuments { get; set; }
    }
}
