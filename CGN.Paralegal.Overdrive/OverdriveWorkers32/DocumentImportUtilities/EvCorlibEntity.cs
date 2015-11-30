using System.Collections.Generic;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// Encapsulates EVCorlib data post extraction.
    /// </summary>
    public class EvCorlibEntity
    {
        readonly List<MailStoresEntity> mailStoreEntity;

        public EvCorlibEntity()
        {
            mailStoreEntity = new List<MailStoresEntity>();
        }

        /// <summary>
        /// Gets or Sets output file path - output file is EDRM generated after extracting the file.
        /// A value will not be set for mail stores.
        /// </summary>
        public string OutputFilePath { get; set; }

        /// <summary>
        /// Gets or Sets true or false value, depicting if there mail stores associated with the given file
        /// </summary>
        public bool HasMailStores
        {
            get;
            set;
        }

        /// <summary>
        /// Encapsulates mail store related properties
        /// </summary>
        public List<MailStoresEntity> MailStoreEntity
        {
            get { return mailStoreEntity; }

        }
    }
}
