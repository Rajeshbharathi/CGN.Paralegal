# region File Header
# endregion

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class AnalysisSetDocumentInfo
    {
        /// <summary>
        ///  Gets or sets the Project Id
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        ///  Gets or sets the DocumentId
        /// </summary>
        public int DocumentId { get; set; }

        /// <summary>
        ///  Gets or sets the Document Reference Id
        /// </summary>
        public string DocumentReferenceId { get; set; }


        /// <summary>
        /// Gets or sets the Document Text Path
        /// </summary>
        public string DocumentTextPath { get; set; }

        /// <summary>
        ///  Gets or sets the Predicted Score
        /// </summary>
        public decimal PredictedScore { get; set; }


        /// <summary>
        /// Gets or sets the Reviewer CategoryId
        /// </summary>
        public int ReviewerCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the Reviewer Category
        /// </summary>
        public string ReviewerCategory { get; set; }

        /// <summary>
        /// Gets or sets the  Predicted Category
        /// </summary>
        public string PredictedCategory { get; set; }

        /// <summary>
        /// Gets or sets the  Predicted CategoryId
        /// </summary>
        public int PredictedCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the  Document Text
        /// </summary>
        public string DocumentText { get; set; }

        /// <summary>
        /// Gets or sets the  Document DCN
        /// </summary>
        public string DocumentDcn { get; set; }

        /// <summary>
        /// Gets or sets the total document for the set
        /// </summary>
        public int TotalDocumentCount { get; set; }

        /// <summary>
        /// Gets or sets the index id of the document (within the set)
        /// </summary>
        public int DocumentIndexId { get; set; }

        /// <summary>
        ///  Gets or sets the Project Name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        ///  Gets or sets the Coding Info 
        /// </summary>
        public CodingInfo Coding { get; set; }

        /// <summary>
        ///  Gets or sets the Pages Info 
        /// </summary>
        public DocumentPageContent Pages { get; set; }
    }
}