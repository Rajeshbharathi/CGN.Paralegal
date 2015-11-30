using System.Collections.Generic;
using System;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// This class represents a single load file record parsed by the load file parser.
    /// </summary>
    /// <remarks></remarks>
    [Serializable]
    public class LoadFileRecord
    {
        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        /// <remarks></remarks>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets the record text.
        /// </summary>
        /// <remarks></remarks>
        public string RecordText { get; set; }

        /// <summary>
        /// Gets the record number.
        /// </summary>
        /// <remarks></remarks>
        public uint RecordNumber { get; set; }

        /// <summary>
        /// Gets the image file.
        /// </summary>
        /// <remarks></remarks>
        public List<string> ImageFile { get; set; }

        /// <summary>
        /// Gets the content file.
        /// </summary>
        /// <remarks></remarks>
        public List<string> ContentFile { get; set; }
        
        /// <summary>
        /// Gets or sets the document control number
        /// </summary>
        public string DocumentControlNumber { get; set; }
    }
}
