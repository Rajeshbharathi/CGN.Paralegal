namespace CGN.Paralegal.ClientContracts.Analytics
{
    using System.Collections.Generic;

    public class DocumentList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentList"/> class.
        /// </summary>
        public DocumentList()
        {
            this.Documents = new List<Document>();
        }
        /// <summary>
        /// Gets or sets the documents.
        /// </summary>
        /// <value>
        /// The documents.
        /// </value>
        public List<Document> Documents { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        /// <value>
        /// The total.
        /// </value>
        public int Total { get; set; }
    }
}