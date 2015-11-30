namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class Discrepancy 
    {
        /// <summary>
        /// Gets or sets set name.
        /// </summary>
        /// <value>
        /// analysis set name
        /// </value>
        public string SetName { get; set; }

        /// <summary>
        /// Gets or sets the binder identifier.
        /// </summary>
        /// <value>
        /// The binder identifier.
        /// </value>
        public string BinderId { get; set; }

        /// <summary>
        /// Gets or sets total documents.
        /// </summary>
        /// <value>
        /// total documents
        /// </value>
        public long TotalDocuments { get; set; }

        /// <summary>
        /// Gets or sets True Positive count.
        /// </summary>
        /// <value>
        /// True Positive count
        /// </value>
        public long TruePostive { get; set; }

        /// <summary>
        /// Gets or sets True Negative count.
        /// </summary>
        /// <value>
        /// True Negative count.
        /// </value>
        public long TrueNegative { get; set; }

        /// <summary>
        /// Gets or sets False Positive count.
        /// </summary>
        /// <value>
        /// False Positive count.
        /// </value>
        public long FalsePostive { get; set; }

        /// <summary>
        /// Gets or sets False Negative count.
        /// </summary>
        /// <value>
        /// False Negative count.
        /// </value>
        public long FalseNegative { get; set; }

        /// <summary>
        /// Gets or sets difference number.
        /// </summary>
        /// <value>
        /// difference number
        /// </value>
        public long TotalDifference { get; set; }

        /// <summary>
        /// Gets or sets different rate.
        /// </summary>
        /// <value>
        /// difference rate
        /// </value>
        public float DifferenceRate { get; set; }

    }
}