using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// This class represents the request entity for Matter document details
    /// </summary>
    [Serializable()]
    public class MatterReadRequest
    {
        /// <summary>
        /// This represents the matter identifier
        /// </summary>
        public long MatterId { get; set; }

        /// <summary>
        /// This represents the page number grouped by the document id
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// This represents the size of the document group
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Requested user guid
        /// </summary>
        public string RequestedBy { get; set; }

    }
}
