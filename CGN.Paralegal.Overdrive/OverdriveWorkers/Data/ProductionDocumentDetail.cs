using System;
using LexisNexis.Evolution.Business.Tags;
using LexisNexis.Evolution.BusinessEntities;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ProductionDocumentDetail
    {
        private readonly List<FieldResult> _lstProductionFieldValues = new List<FieldResult>();
        public string DocumentId { get; set; }
        public string OriginalDocumentReferenceId { get; set; }
        public int NumberOfPages { get; set; }
        public int RunningBatesNumber { get; set; }
        public string OriginalDocumentMimeType { get; set; }
        public bool IsDocumentExcluded { get; set; }
        public string StartingBatesNumber { get; set; }
        public string EndingBatesNumber { get; set; }
        public string DocumentProductionNumber { get; set; }
        public string AllBates { get; set; }
        public string OriginalCollectionId { get; set; }
        public string ProductionCollectionId { get; set; }
        public string DCNNumber { get; set; }
        public int StartBatesRunningNumber { get; set; }
        public ProductionProfile Profile { get; set; }
        public string MatterId { get; set; }
        public string CreatedBy { get; set; }
        public string DatasetCollectionId { get; set; }
        public string ArchivePath { get; set; }
        public string QueryString { get; set; }
        public string XdlThumbFileName { get; set; }
        public int ExpectedDocumentCount { get; set; }
        public DocumentOperationBusinessEntity DocumentSelectionContext { get; set; }
        public DocumentOperationBusinessEntity DocumentExclusionContext { get; set; }
        public string SourceDestinationPath { set; get; }
        public string SourceFile { get; set; }
        public int CorrelationId { get; set; }
        public string OriginalDatasetName { get; set; }
        public int OriginalDatasetId { get; set; }
        public string ExtractionLocation { get; set; }
        public bool IsVolumeContainExistingDocuments { get; set; }
        public List<FieldBEO> lstProductionFields { get; set; }
        public List<FieldBEO> lstDsFieldsBeo { get; set; }
        public List<FieldResult> lstProductionFieldValues
        {
            get
            {
                return _lstProductionFieldValues;
            }
        }
        public Dictionary<string, List<KeyValuePair<string, string>>> DocumentFields { get; set; }
        public SearchServerBEO SearchServerDetails { get; set; }
        public MatterBEO matterBeo { get; set; }
        public List<KeyValuePair<string, string>> Fields{ get; set; }
        public DatasetBEO dataSetBeo { get; set; }
        public int LoopCount { get; set; }
        public string HeartBeatFile { get; set; }
        public bool GetText { get; set; }
        public int NearNativeConversionPriority { get; set; }

        /// <summary>
        /// When Document was queued for RedactIt conversion
        /// </summary>
        public DateTime? ConversionStartTime { get; set; }

        /// <summary>
        /// Gets or sets the conversion enqueue time.
        /// </summary>
        /// <value>
        /// The conversion enqueue time.
        /// </value>
        public DateTime? ConversionEnqueueTime { get; set; }

        /// <summary>
        /// How many times document conversion was validated
        /// </summary>
        public int ConversionCheckCounter { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        internal string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the place holder field values.
        /// </summary>
        /// <value>
        /// The place holder field values.
        /// </value>
        public List<FieldIdentifierBEO> PlaceHolderFieldValues { get; set; }

        /// <summary>
        /// Gets or sets the volume tag.
        /// </summary>
        /// <value>
        /// The volume tag.
        /// </value>
        public TagIdentifierBEO VolumeTag { get; set; }


        /// <summary>
        /// Gets or sets the production tag group.
        /// </summary>
        /// <value>
        /// The production tag group.
        /// </value>
        public TagIdentifierBEO ProductionTagGroup { get; set; }

    }
}
