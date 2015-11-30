namespace CGN.Paralegal.ClientContracts.Analytics
{
    /// <summary>
    ///     DocumentSource Class
    /// </summary>
    public class DocumentSource
    {
        /// <summary>
        ///     SelectionMode
        /// </summary>
        public enum SelectionModes
        {
            All,
            Tag,
            SavedSearch,
            SearchText
        };

        /// <summary>
        ///     Gets or sets the selected mode.
        /// </summary>
        /// <value>
        ///     The selected mode.
        /// </value>
        public SelectionModes SelectedMode { get; set; }

        /// <summary>
        ///     Gets or sets the original collection identifier.
        /// </summary>
        /// <value>
        ///     The original collection identifier.
        /// </value>
        public string CollectionId { get; set; }


        /// <summary>
        ///     Gets or sets the tag identifier.
        /// </summary>
        /// <value>
        ///     The tag identifier.
        /// </value>
        public string TagId { get; set; }


        /// <summary>
        ///     Gets or sets the saved search identifier.
        /// </summary>
        /// <value>
        ///     The saved search identifier.
        /// </value>
        public string SavedSearchId { get; set; }

        /// <summary>
        ///  Gets or sets the search text
        /// </summary>
        public string SearchText { get; set; }
    }
}