namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class Sort
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        public SortOrder Order { get; set; }
    }

    public enum SortOrder
    {
        Ascending = 0,
        Descending = 1
    }
}