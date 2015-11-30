namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class ControlSetSummary : AnalysisSet
    {
        /// <summary>
        /// Gets or sets the estimated total documents.
        /// </summary>
        /// <value>
        /// The estimated total documents.
        /// </value>
        public long EstimatedTotalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the percentage of total population.
        /// </summary>
        /// <value>
        /// The percentage of total population.
        /// </value>
        public float PercentageOfTotalPopulation { get; set; }

        /// <summary>
        ///     Gets or sets the project identifier.
        /// </summary>
        /// <value>
        ///     The project identifier.
        /// </value>
        public string ProjectId { get; set; }

        /// <summary>
        /// Get or set the confidence level
        /// </summary>
        public byte ConfidenceLevel { get; set; }

        /// <summary>
        /// Get or set the margin of error
        /// </summary>
        public float MarginOfError { get; set; }


    }
}