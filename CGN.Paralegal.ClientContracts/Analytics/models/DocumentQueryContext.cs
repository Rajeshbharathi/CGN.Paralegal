namespace CGN.Paralegal.ClientContracts.Analytics
{
    using System.Collections.Generic;

    public class DocumentQueryContext
    {
        /// <summary>
        /// Gets or sets the matter identifier.
        /// </summary>
        /// <value>
        /// The matter identifier.
        /// </value>
        public long MatterId { get; set; }

        /// <summary>
        /// Gets or sets the dataset identifier.
        /// </summary>
        /// <value>
        /// The dataset identifier.
        /// </value>
        public long DatasetId { get; set; }

        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        /// <value>
        /// The project identifier.
        /// </value>
        public long ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the key word.
        /// </summary>
        /// <value>
        /// The key word.
        /// </value>
        public string KeyWord { get; set; }

        /// <summary>
        /// Gets or sets the column filters.
        /// </summary>
        /// <value>
        /// The filters.
        /// </value>
        public List<string> ExportFilters { get; set; }

        /// <summary>
        /// Gets or sets the filters.
        /// </summary>
        /// <value>
        /// The filters.
        /// </value>
        public List<Field> Filters { get; set; }

        /// <summary>
        /// Gets or sets the sort.
        /// </summary>
        /// <value>
        /// The sort.
        /// </value>
        public List<Sort> Sort { get; set; }

        /// <summary>
        /// Gets or sets the analysis set.
        /// </summary>
        /// <value>
        /// The analysis set.
        /// </value>
        public AnalysisSet AnalysisSet { get; set; }

        /// <summary>
        /// Gets or sets the index of the page.
        /// </summary>
        /// <value>
        /// The index of the page.
        /// </value>
        public int PageIndex { get; set; }

        /// <summary>
        /// Gets or sets the size of the page.
        /// </summary>
        /// <value>
        /// The size of the page.
        /// </value>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets document sequence id.
        /// </summary>
        /// <value>
        /// document seuqnce id
        /// </value>
        public int DocumentSequenceId { get; set; }
    }

    public enum AnalysisSetType
    {
        ControlSet = 0,
        TrainingSet = 1,
        QcSet = 2,
        PredictSet = 3,
        AllDocuments = 4
    }
}