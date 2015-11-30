namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class AnalysisSet
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the binder identifier.
        /// </summary>
        /// <value>
        /// The binder identifier.
        /// </value>
        public string BinderId { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public AnalysisSetType Type { get; set; }

        /// <summary>
        ///     Gets or sets the total documents.
        /// </summary>
        /// <value>
        ///     The total documents.
        /// </value>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the number of relevant documents.
        /// </summary>
        /// <value>
        /// The number of relevant documents.
        /// </value>
        public int NumberOfRelevantDocuments { get; set; }

        /// <summary>
        /// Gets or sets the number of not relevant documents.
        /// </summary>
        /// <value>
        /// The number of not relevant documents.
        /// </value>
        public int NumberOfNotRelevantDocuments { get; set; }

        /// <summary>
        /// Gets or sets the number of not coded documents.
        /// </summary>
        /// <value>
        /// The number of not coded documents.
        /// </value>
        public int NumberOfNotCodedDocuments { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped documents.
        /// </summary>
        /// <value>
        /// The number of skipped documents.
        /// </value>
        public int NumberOfSkippedDocuments { get; set; }
    }
}