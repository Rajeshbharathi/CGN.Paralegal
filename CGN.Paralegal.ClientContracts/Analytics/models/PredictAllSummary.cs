namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class PredictAllSummary 
    {
        /// <summary>
        /// Gets or sets relevant document count.
        /// </summary>
        /// <value>
        /// relevant document count.
        /// </value>
        public long RelevantDocumentCount { get; set; }

        /// <summary>
        /// Gets or sets not relevant documents count.
        /// </summary>
        /// <value>
        /// Not relevant document count.
        /// </value>
        public long NotRelevantDocumentCount { get; set; }

        /// <summary>
        ///     Gets or sets related traning round.
        /// </summary>
        /// <value>
        ///     The related traning round.
        /// </value>
        public string TrainingRound { get; set; }

        /// <summary>
        ///     Gets or sets whether related traning round is current.
        /// </summary>
        /// <value>
        ///     Is the related training round current.
        /// </value>
        public bool IsTrainingRoundCurrent { get; set; }

    }
}