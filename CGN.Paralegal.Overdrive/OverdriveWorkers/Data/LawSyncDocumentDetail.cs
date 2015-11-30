using System;
using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class LawSyncDocumentDetail
    {
        /// <summary>
        /// Law document Id
        /// </summary>
        public int LawDocumentId { get; set; }

        /// <summary>
        /// Document reference id
        /// </summary>
        public string DocumentReferenceId { get; set; }
        
        /// <summary>
        /// Document control number
        /// </summary>
        public string DocumentControlNumber { get; set; }


        /// <summary>
        /// Document Correlation id.
        /// </summary>
        /// <remarks></remarks>
        public int CorrelationId { get; set; }


        /// <summary>
        /// Indicates documents need to be produced
        /// </summary>
        public bool IsImaging { get; set; }

        /// <summary>
        /// Holds List of selected Fields & Tags
        /// </summary>
        public List<LawMetadataBEO> MetadataList { get; set; }

        /// <summary>
        /// Produced images file path
        /// </summary>
        public List<string> ProducedImages { get; set; }


        /// <summary>
        /// Redact-It Heart Beat File Path
        /// </summary>
        public string RedactItHeartBeatFilePath { get; set; }


        /// <summary>
        /// Image folder name
        /// </summary>
        public string ImagesFolderPath { get; set; }

        /// <summary>
        /// Image Name starting number
        /// </summary>
        public int ImageStartingNumber { get; set; }

        /// <summary>
        /// Images XDL available
        /// </summary>
        public bool IsImagesXdlAvailable { get; set; }

        /// <summary>
        /// Path to extract/create source file like xdl, markup xml, etc
        /// </summary>
        public string DocumentExtractionPath { get; set; }


        /// <summary>
        /// Gets or sets document was queued for RedactIt conversion
        /// </summary>
        public DateTime? ConversionStartTime { get; set; }

        /// <summary>
        /// Gets or sets the conversion enqueue time.
        /// </summary>
        public DateTime? ConversionEnqueueTime { get; set; }

        /// <summary>
        /// Is error on sync metadata
        /// </summary>
        public bool IsErrorOnSyncMetadata { get; set; }

        /// <summary>
        /// Is error on get metadata
        /// </summary>
        public bool IsErrorOnGetMetadata { get; set; }
    }
}

