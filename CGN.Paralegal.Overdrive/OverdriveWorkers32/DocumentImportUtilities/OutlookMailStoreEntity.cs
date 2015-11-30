using System;
using System.Collections.Generic;
using System.IO;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Encapsulates Outlook specific message file data
    /// </summary>
    [Serializable]
    public class OutlookMailStoreEntity
    {
        /// <summary>
        /// Gets or sets the PST file object.
        /// </summary>
        /// <value>
        /// The PST file.
        /// </value>
        public FileInfo PSTFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the entry id and email message pair.
        /// </summary>
        /// <value>
        /// The entry id and email message pair.
        /// </value>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerable<KeyValuePair<string, string>> EntryIdAndEmailMessagePairs
        {
            get;
            set;
        }
    }
}
