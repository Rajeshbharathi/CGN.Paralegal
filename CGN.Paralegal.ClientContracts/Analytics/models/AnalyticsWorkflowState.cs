namespace CGN.Paralegal.ClientContracts.Analytics
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class AnalyticsWorkflowState
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public State Name { get; set; }

        /// <summary>
        /// Gets or sets the create status.
        /// </summary>
        /// <value>
        /// The create status.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public Status CreateStatus { get; set; }

        /// <summary>
        /// Gets or sets the review status.
        /// </summary>
        /// <value>
        /// The review status.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public Status ReviewStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is current.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is current; otherwise, <c>false</c>.
        /// </value>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        public int Order { get; set; }
    }

    public enum State
    {
        ProjectSetup = 1,
        ControlSet = 2,
        TrainingSet = 3,
        PredictSet = 4,
        QcSet = 5,
        Done = 6
    };

    public enum Status
    {
        NotStarted = 1,
        Inprogress = 2,
        Completed = 3
    };
}