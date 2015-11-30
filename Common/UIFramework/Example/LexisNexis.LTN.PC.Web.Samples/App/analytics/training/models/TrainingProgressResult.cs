namespace LexisNexis.LTN.PC.Web.Samples.Models
{
    public class TrainingProgressResult {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the precision.
        /// </summary>
        /// <value>
        /// The precision.
        /// </value>
        public int Precision { get; set; }

        /// <summary>
        /// Gets or sets the recall.
        /// </summary>
        /// <value>
        /// The recall.
        /// </value>
        public int Recall { get; set; }

        /// <summary>
        /// Gets or sets the accuracy.
        /// </summary>
        /// <value>
        /// The accuracy.
        /// </value>
        public int Accuracy { get; set; }
    }
}