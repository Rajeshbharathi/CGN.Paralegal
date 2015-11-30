namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class ControlSet
    {
        /// <summary>
        /// Get or set the confidence level
        /// </summary>
        public byte ConfidenceLevel { get; set; }

        /// <summary>
        /// Get or set the margin of error
        /// </summary>
        public float MarginOfError { get; set; }

        /// <summary>
        /// Get or set the samplesize
        /// </summary>
        public int SampleSize { get; set; }

    }

}