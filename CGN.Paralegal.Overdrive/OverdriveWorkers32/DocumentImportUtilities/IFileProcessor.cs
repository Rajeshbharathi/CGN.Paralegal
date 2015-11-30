using System;
using System.Collections.Generic;
using System.IO;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using LexisNexis.Evolution.DocumentImportUtilities;

    /// <summary>
    /// Declares functions needed for document extraction.
    /// </summary>
    public interface IFileProcessor : IDisposable
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
        IEnumerable<FieldMapBEO> FilterByMappedFields
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or Sets password file URI for extracting password protected archives
        /// </summary>
        IEnumerable<string> Passwords
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the misc messages.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        IEnumerable<KeyValuePair<string, string>> MiscMessages
        {
            get;            
        }

        /// <summary>
        /// Gets or Sets configuration to delete temporary files
        /// </summary>
        DeleteExtractedFilesOptions IsDeleteTemporaryFiles { get; set; }

        /// <summary>
        /// e-mail threads, replies
        /// </summary>
        IEnumerable<RelationshipBEO> ThreadRelationships { get; }


        /// <summary>
        /// Extracts specified document and converts to a BEO which could be interpreted by rest of the application.
        /// Overload allows intermediate operations be performed as documents are extracted.
        /// In an example, a PST file extraction returns multiple documents.
        /// As each document is extracted import can be performed when written in intermediate operation delegate definition.
        /// This approach is useful when there is a loop of time taking operations to be done.
        /// </summary>
        /// <typeparam name="T"> Additional information input to the callback function </typeparam>
        /// <param name="file">file object to be extracted</param>
        /// <param name="temporaryWorkingDirectory">Specify working directory to be used for extracting contents</param>
        /// <param name="batchSize">batch size used while extracting mail stores</param>
        /// <param name="intermediateOperation">additional operations to be performed as soon as extraction is done</param>
        /// <param name="errorCallback">Loop of operations expected - an error in the loop shouldn't break the loop - hence error call back.</param>
        void ProcessDocumentWithCallBack<T>(IEnumerable<FileInfo> file, DirectoryInfo temporaryWorkingDirectory, 
            int batchSize,long jobRunId, Action<EvDocumentDataEntity,T> intermediateOperation, Action<EVException> errorCallback, T obj,
            List<EmailThreadingEntity> rawDocumentRelationships);

    }


    /// <summary>
    /// Enumeration to represent options for deleting all files, deleting extracted text and DRM XMLs only or not deleting any files
    /// </summary>
    public enum DeleteExtractedFilesOptions
    {
        DeleteAll,
        DeleteNonNativeFiles,
        DeleteNone
    }

}
