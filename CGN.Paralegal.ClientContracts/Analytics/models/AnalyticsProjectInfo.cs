namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class AnalyticsProjectInfo 
    {
        /// <summary>
        ///     Gets or sets the data set identifier.
        /// </summary>
        /// <value>
        ///     The data set identifier.
        /// </value>
        public string ProjectCollectionId { get; set; }

        /// <summary>
        ///     Gets or sets the collection identifier.
        /// </summary>
        /// <value>
        ///     The collection identifier.
        /// </value>
        public int Id { get; set; }
        
        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Name { get; set; }


        /// <summary>
        ///     Gets or sets the description.
        /// </summary>
        /// <value>
        ///     The description.
        /// </value>
        public string Description { get; set; }


        /// <summary>
        /// Gets or sets the confidence.
        /// </summary>
        /// <value>
        /// The confidence.
        /// </value>
        public float Confidence { get; set; }

        /// <summary>
        ///     Gets or sets the margin of error.
        /// </summary>
        /// <value>
        ///     The margin of error.
        /// </value>
        public float MarginOfError { get; set; }

        /// <summary>
        /// Gets or sets the name of the matter.
        /// </summary>
        /// <value>
        /// The name of the matter.
        /// </value>
        public string MatterName { get; set; }

        /// <summary>
        ///  Gets or sets the Organization Name for Project
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        ///     Gets or sets the MatterId
        /// </summary>     
        public long MatterId { get; set; }

        /// <summary>
        ///     Gets or sets the DatasetId
        /// </summary>     
        public long DatasetId { get; set; }

        /// <summary>
        ///     Gets or sets the target f1.
        /// </summary>       
        public float TargetF1 { get; set; }

        /// <summary>
        ///     Gets or sets the target precision.
        /// </summary>      
        public float TargetPrecision { get; set; }

        /// <summary>
        ///     Gets or sets the target recall.
        /// </summary>
        /// <value>
        ///     The target recall.
        /// </value>
        public float TargetRecall { get; set; }


        /// <summary>
        ///     Gets or sets the overturn error threshold.
        /// </summary>      
        public float OverturnErrorThreshold { get; set; }  

        /// <value>
        ///     The document source.
        /// </value>
        public DocumentSource DocumentSource { get; set; }

        /// <summary>
        /// Gets or sets the total document count.
        /// </summary>
        /// <value>
        /// The total document count.
        /// </value>
        public long TotalDocumentCount { get; set; }

        /// <summary>
        /// Gets or sets the created by.
        /// </summary>
        /// <value>
        /// The created by.
        /// </value>
        public string CreatedBy { get; set; }

        /// <summary>
        ///     Gets or sets the created date.
        /// </summary>
        /// <value>
        ///     The created date.
        /// </value>
        public string CreatedDate { get; set; }


        /// <summary>
        /// Field pre fix
        /// </summary>
        public string FieldPreFix { get; set; }

        /// <summary>
        /// Indicates valid project name
        /// </summary>
        public bool IsValidProjectName { get; set; }

        /// <summary>
        /// Indicates valid field prefix
        /// </summary>
        public bool IsValidFieldPrefix { get; set; }

        /// <summary>
        /// Indicates to add additional documents to project
        /// </summary>
        public bool IsAddAdditionalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the name of the reviewer category field.
        /// </summary>
        /// <value>
        /// The name of the reviewer category field.
        /// </value>
        public string ReviewerCategoryFieldName { get; set; }

        /// <summary>
        /// Gets or sets the name of the predicted category field.
        /// </summary>
        /// <value>
        /// The name of the predicted category field.
        /// </value>
        public string PredictedCategoryFieldName { get; set; }
    }
}