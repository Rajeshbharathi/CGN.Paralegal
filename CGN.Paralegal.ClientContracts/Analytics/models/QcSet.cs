using System;

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class QcSet : AnalysisSet
    {

        /// <summary>
        /// Gets or sets the confidence level.
        /// </summary>
        /// <value>
        /// The confidence level.
        /// </value>
        public float ConfidenceLevel { get; set; }

        /// <summary>
        ///     Gets or sets the margin of error.
        /// </summary>
        /// <value>
        ///     The margin of error.
        /// </value>
        public float MarginOfError { get; set; }

        /// <summary>
        /// Gets or sets the sampling method.
        /// </summary>
        /// <value>
        /// The sampling method.
        /// </value>
        public SamplingMethod SamplingMethod { get; set; }

        /// <summary>
        /// Gets or sets the type of the sub.
        /// </summary>
        /// <value>
        /// The type of the sub.
        /// </value>
        public SubType SubType { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public Status Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is current.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is current; otherwise, <c>false</c>.
        /// </value>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// Gets or sets the false negative.
        /// </summary>
        /// <value>
        /// The false negative.
        /// </value>
        public float FalseNegative { get; set; }

        /// <summary>
        /// Gets or sets the completion date.
        /// </summary>
        /// <value>
        /// The completion date.
        /// </value>
        public DateTime CompletionDate { get; set; }

        /// <summary>
        /// Get or set Create QCset Job Id
        /// </summary>
        public int QcSetJobId { get; set; }
    }

    public enum SamplingMethod
    {
        Statistical = 1,
        FixedSize = 2
    }

    public enum SubType
    {
        RelevantNotRelevant = 1,
        OnlyRelevant = 2
    }
}
