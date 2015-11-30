namespace LexisNexis.LTN.PC.Web.Samples.Models
{
    public class DocumentSet {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the documents.
        /// </summary>
        /// <value>
        /// The documents.
        /// </value>
        public int Documents { get; set; }

        /// <summary>
        /// Gets or sets the reviewed.
        /// </summary>
        /// <value>
        /// The reviewed.
        /// </value>
        public int Reviewed { get; set; }

        /// <summary>
        /// Gets or sets the not reviewed.
        /// </summary>
        /// <value>
        /// The not reviewed.
        /// </value>
        public int NotReviewed { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }
    }
}