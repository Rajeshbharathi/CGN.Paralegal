using System;
using System.Collections.Generic;
using System.IO;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Defines contract for mail processing adapaters
    /// </summary>
    public interface IMailProcessor : IDisposable
    {
        /// <summary>
        /// Dataset to which the documents being extracted belong
        /// </summary>
        DatasetBEO DatasetBeo
        {
            get;
            set;
        }

        /// <summary>
        /// Mapped fields - if only specific fields' data to be used.
        /// </summary>
        List<FieldMapBEO> FilterByMappedFields
        {
            get;
        }

        /// <summary>
        /// Gets or sets the batch identifier. This used to identify groups of threads (document relationships)
        /// </summary>
        /// <value>
        /// The batch identifier.
        /// </value>
        long batchIdentifier { get; set; }


        /// <summary>
        /// Gets the document relationships in Raw form - may not have family ID details.
        /// </summary>
        IEnumerable<EmailThreadingEntity> RawDocumentRelationships { get; }

        /// <summary>
        /// Gets or Sets configuration to delete temporary files (true/false)
        /// </summary>
        DeleteExtractedFilesOptions IsDeleteTemporaryFiles { get; set; }

        /// <summary>
        /// Gets errors while extracting e-mail documents.
        /// </summary>
        IEnumerable<EVException> Errors { get; }

        /// <summary>
        /// This event is raised when a document is available to be utilized by calling function/module
        /// Mail processing is tedious and goes in a loop.
        /// As each document is available additional operations can be performed rather than waiting for whole loop to complete.
        /// This even helps intimate calling module that a document is available to be used.
        /// </summary>

        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        event Action<EvDocumentDataEntity> EvDocumentDataEntityAvailable;


        /// <summary>
        /// Extracts specified mail document and converts to a BEO which could be interpreted by rest of the application.
        /// Overload allows intermediate operations be performed as documents are extracted. 
        /// In an example, a PST file extraction returns multiple documents. As each document is extracted import can be performed.
        /// The logic to import need to be written in event handler for EvDocumentDataEntityAvailable
        /// This approach is usefull when there is a loop of time taking operations to be done.
        /// </summary>
        /// <param name="mailEntity">List of mail stores to be processed</param>        
        /// <param name="temporaryWorkingDirectory"> Specify working directory to be used for extracting contents</param>
        /// <param name="batchSize"> Batch size used while extracting mail stores </param>        
        /// <param name="errorCallback"> Loop of operations expected - an error in the loop shouldn't break the loop - hence error call back. </param>        
        /// <returns> EvDocumentDataEntity that abstracts documents and relationships </returns>
        void ProcessMailDocuments(EvCorlibEntity mailEntity, DirectoryInfo temporaryWorkingDirectory, int batchSize);

        /// <summary>
        /// Gets or sets the file processor instance. Sometimes same instance has to be used across adapters to maintain state information. 
        /// In those situations this property can be used.
        /// </summary>
        /// <value>
        /// The file processor instance.
        /// </value>
        EvCorlibManager FileProcessorInstance { get; set; }


    }
}
