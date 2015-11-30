using System.Collections.Generic;

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class TrainingSetSummary
    {
        /// <summary>
        /// Gets or sets the current round progress.
        /// </summary>
        /// <value>
        /// The current round progress.
        /// </value>
        public AnalysisSet CurrentRoundProgress { get; set; }

        /// <summary>
        /// Gets or sets the completed rounds summary.
        /// </summary>
        /// <value>
        /// The completed rounds summary.
        /// </value>
        public AnalysisSet CompletedRoundsSummary { get; set; }

        /// <summary>
        /// Gets or sets the completed round details.
        /// </summary>
        /// <value>
        /// The completed round details.
        /// </value>
        public List<AnalysisSet> CompletedRoundDetails { get; set; }

        /// <summary>
        /// Gets or sets the current round.
        /// </summary>
        /// <value>
        /// The current round.
        /// </value>
        public int CurrentRound { get; set; }

        /// <summary>
        /// Gets or sets the rounds completed.
        /// </summary>
        /// <value>
        /// The rounds completed.
        /// </value>
        public int RoundsCompleted { get; set; }
    }
}