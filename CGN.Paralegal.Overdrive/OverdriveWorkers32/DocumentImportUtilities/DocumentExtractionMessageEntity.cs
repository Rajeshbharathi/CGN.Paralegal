using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    /// <summary>
    /// Encapsulates information sent from EDocs Parser worker to EDocs extraction worker
    /// </summary>
    [Serializable]
    public class DocumentExtractionMessageEntity
    {
        /// <summary>
        /// Gets or sets the file collection.
        /// </summary>
        /// <value>
        /// The file collection.
        /// </value>
        public IEnumerable<string> FileCollection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is last message in batch.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is last message in batch; otherwise, <c>false</c>.
        /// </value>
        public bool IsLastMessageInBatch { get; set; }
    }
}
