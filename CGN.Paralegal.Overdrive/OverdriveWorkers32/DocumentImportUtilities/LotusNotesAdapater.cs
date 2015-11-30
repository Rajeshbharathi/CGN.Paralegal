# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="LotusNotesAdapater.cs">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          This is a file that contains LotusNotesAdapater class 
//      </description>
//      <changelog>
//          <date value="19-August-2010"></date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentExtractionUtilities.ExtensionMethods;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class LotusNotesAdapater : IMailProcessor
    {

        /// <summary>
        /// Encapsulates errors that might be caused in Lotus Notes Adapter
        /// </summary>
        public class LotusNotesAdapaterErrorCodes
        {
            /// <summary>
            /// Failures to process Lotus Notes Mail stores
            /// </summary>
            public const string LotusNotesMailStoreProcessFailure = "LotusNotesMailStoreProcessFailure";

            /// <summary>
            /// Failure to convert extracted EDRM file to document entities
            /// </summary>
            public const string LotusNotesEDRMToDocumentTransformFailure = "LotusNotesEDRMToDocumentTransformFailure";

            /// <summary>
            /// Failure to calculate thread relationship for Lotus Notes e-mail messages
            /// </summary>
            public const string ThreadCalculationFailure = "ThreadCalculationFailure";

            /// <summary>
            /// Failure to create LotusNotesAdapater object.
            /// </summary>
            public const string CreateLotusNotesAdapaterFailure = "CreateLotusNotesAdapaterFailure";

            /// <summary>
            /// Error indicating length of threading constraint exceeds the valid limit.
            /// </summary>
            public const string IllegalLotusNotesThreadingConstraint = "IllegalThreadingConstraint";
        }

        private string threadingConstraint;
        ///<summary>
        /// Gets or Sets threading constraint
        /// E-mail threading shall be limited to one or more mail stores. By default the adapter limits to one instance - if it need to be more than one, calling module should set the value.
        /// For example threading is done among e-mails of one NSF file. It should NOT consider e-mails in a different NSF files. This constraint is enforced by this value.
        /// </summary>
        public string ThreadingConstraint
        {
            get { return threadingConstraint; }
            set
            {
                // It shall be used in document id. Length of the value in database 64 characters - rest 32 come from document.
                if (value.Length > 32) throw new EVException().AddResMsg(LotusNotesAdapaterErrorCodes.IllegalLotusNotesThreadingConstraint);
                threadingConstraint = value;
            }
        }

        /// <summary>
        /// Instantiates Lotus Notes Adapter
        /// </summary>
        public LotusNotesAdapater()
        {
            try
            {
                _documentRelationships = new List<EmailThreadingEntity>();
                // Threading constraint by default is hash of a GUID. Hash is calculated to set the size to 32 to characters. rest 32 characters come from each document.
                threadingConstraint = EmailThreadingHelper.GetMD5Hash(ASCIIEncoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                _errors = new List<EVException>();
                _filterByMappedFields = new List<FieldMapBEO>();
                IsDeleteTemporaryFiles = DeleteExtractedFilesOptions.DeleteNone; // initialize if temporary files can be deleted
            }            
            catch (Exception exception)
            {
                exception.AddResMsg(LotusNotesAdapaterErrorCodes.CreateLotusNotesAdapaterFailure);
                throw;
            }
        }

        #region IMailProcessor Members


        private List<EVException> _errors;

        public IEnumerable<EVException> Errors
        {
            get { return _errors; }
        }

        /// <summary>
        /// Gets or sets the batch identifier. This used to identify groups of threads (document relationships)
        /// </summary>
        /// <value>
        /// The batch identifier.
        /// </value>
        public long batchIdentifier
        {
            get;
            set;
        }

        private List<EmailThreadingEntity> _documentRelationships;
        /// <summary>
        /// Gets or sets the document relationships.
        /// </summary>
        /// <value>
        /// The document relationships.
        /// </value>
        public IEnumerable<EmailThreadingEntity> RawDocumentRelationships
        {
            get
            {
                return _documentRelationships;
            }
        }

        /// <summary>
        /// Gets or Sets configuration to delete temporary files (true/false)
        /// </summary>
        public DeleteExtractedFilesOptions IsDeleteTemporaryFiles { get; set; }

        /// <summary>
        /// Dataset to which the documents being extracted belong
        /// </summary>
        public DatasetBEO DatasetBeo
        {
            get;
            set;
        }

        private List<FieldMapBEO> _filterByMappedFields;

        /// <summary>
        /// Mapped fields - if only specific fields' data to be used.
        /// </summary>
        public List<FieldMapBEO> FilterByMappedFields
        {
            get { return _filterByMappedFields; }
        }

        /// <summary>
        /// Gets or sets the file processor instance. Sometimes same instance has to be used across adapters to maintain state information.
        /// In those situations this property can be used.
        /// </summary>
        /// <value>
        /// The file processor instance.
        /// </value>
        public EvCorlibManager FileProcessorInstance
        {
            get;
            set;
        }

        /// <summary>
        /// This event is raised when a document is available to be utilized by calling function/module
        /// Mail processing is tedious and goes in a loop.
        /// As each document is available additional operations can be performed rather than waiting for whole loop to complete.
        /// This even helps intimate calling module that a document is available to be used.
        /// </summary>
        public event Action<EvDocumentDataEntity> EvDocumentDataEntityAvailable;

        /// <summary>
        /// Extracts specified mail document and converts to a BEO which could be interpreted by rest of the application.
        /// Overload allows intermediate operations be performed as documents are extracted. 
        /// In an example, a PST file extraction returns multiple documents. As each document is extracted import can be performed.
        /// The logic to import need to be written in event handler for EvDocumentDataEntityAvailable
        /// This approach is useful when there is a loop of time taking operations to be done.
        /// </summary>
        /// <param name="evCorlibEntity"> </param>
        /// <param name="temporaryWorkingDirectory"> Specify working directory to be used for extracting contents</param>
        /// <param name="batchSize"> Batch size used while extracting mail stores </param>        
        /// <param name="errorCallback"> Loop of operations expected - an error in the loop shouldn't break the loop - hence error call back. </param>        
        /// <returns> EvDocumentDataEntity that abstracts documents and relationships </returns>
        public void ProcessMailDocuments(EvCorlibEntity evCorlibEntity, DirectoryInfo temporaryWorkingDirectory, int batchSize)
        {
            try
            {
                // if file processor instance is null, create new instance.
                if (FileProcessorInstance == null)
                    FileProcessorInstance = new EvCorlibManager(outputDirectory: temporaryWorkingDirectory, outputBatchSize: batchSize);

                // If DeleteAll is the option, let EVCorlibManager delete all files/folders. 
                // If not this Adapter will manage deleting files.
                FileProcessorInstance.IsDeleteTemporaryFiles = (IsDeleteTemporaryFiles == DeleteExtractedFilesOptions.DeleteNonNativeFiles);

                FileProcessorInstance.ProcessMailStores(evCorlibEntity, TransformLotusNotesEDRMToDocumentBusinessEntity);
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                exception.AddErrorCode(LotusNotesAdapaterErrorCodes.LotusNotesMailStoreProcessFailure).Trace().Swallow();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false; // to detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }

                disposed = true;
            }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Transforms Outlook specific EDRM file to Document Business Entity
        /// </summary>
        /// <param name="edrmFileName">EDRM file to be transformed</param>
        /// <param name="filePath"> </param>
        /// <param name="currentDocumentIndex">Index of the current document in list of documents being extracted</param>
        /// <returns>
        /// EvDocumentDataEntity that abstracts documents and relationships
        /// </returns>
        private EvDocumentDataEntity TransformLotusNotesEDRMToDocumentBusinessEntity(string edrmFileName, string filePath, int? currentDocumentIndex)
        {
            try
            {
                LotusNotesEdrmManager lotusNotesEdrmManager = new LotusNotesEdrmManager(edrmFileName);
                List<RelationshipBEO> relationships = new List<RelationshipBEO>();

                lotusNotesEdrmManager.CreateDocumentId += GetEmailDocumentIdFromMessageId;

                List<RVWDocumentBEO> documents = lotusNotesEdrmManager.GetDocuments(FilterByMappedFields, DatasetBeo.Matter.FolderID,
                                                            DatasetBeo.CollectionId, ref relationships);

                // Adding created By property for each documents
                documents.ForEach(x => x.CreatedBy = DatasetBeo.CreatedBy);

                EvDocumentDataEntity evDocumentDataEntity = new EvDocumentDataEntity { Documents = documents };

                // null check and percentage calculation
                if (currentDocumentIndex.HasValue)
                {
                    // update percent complete.
                    evDocumentDataEntity.PercentComplete = (100.00 / FileProcessorInstance.EntryIdsCount) * currentDocumentIndex.Value;
                }

                if (EvDocumentDataEntityAvailable != null)
                    EvDocumentDataEntityAvailable(evDocumentDataEntity);


                #region insert temporary records for thread calculation

                List<EmailThreadingEntity> lstEmailThreadEntities = new List<EmailThreadingEntity>();
                if (relationships != null && relationships.Count > 0)
                {

                    relationships.ForEach(relation =>
                                                 lstEmailThreadEntities.Add(
                                                          relation.ToDataAccesEntity(
                                                                batchIdentifier,
                                                                ThreadingConstraint,
                                                                ThreadRelationshipEntity.RelationshipType.LotusNotesAttachment)));
                }

                IEnumerable<LotusNotesEMailDocumentEntity> lotusNotesEmails = lotusNotesEdrmManager.LotusNotesEmailDocuments;
                // store temporary record for calculating email relationship
                if (lotusNotesEmails != null && lotusNotesEmails.Any())
                {
                    lstEmailThreadEntities.AddRange(from document in lotusNotesEmails
                                                    where !string.IsNullOrEmpty(document.ReferenceId)
                                                    select new EmailThreadingEntity
                                                               {
                                                                   ChildDocumentID = document.ReferenceId,
                                                                   ParentDocumentID = GetEmailDocumentIdFromMessageId(document.MessageID),
                                                                   FamilyID = string.Empty,
                                                                   JobRunID = batchIdentifier,
                                                                   ThreadingConstraint = ThreadingConstraint,
                                                                   RelationshipType = ThreadRelationshipEntity.RelationshipType.LotusNotesEmailThread
                                                               });
                }

                //Used Bulk Add Temp Relationships in EmailThreadingHelper
                if (lstEmailThreadEntities.Count > 0)
                {
                    _documentRelationships.AddRange(lstEmailThreadEntities);
                }

                #endregion

                // Cleanup if delete non native files is set
                if (IsDeleteTemporaryFiles == DeleteExtractedFilesOptions.DeleteNonNativeFiles)
                {
                    lotusNotesEdrmManager.DeleteNonNativeFiles();
                }

                return evDocumentDataEntity;
            }            
            catch (Exception exception)
            {
                exception.AddResMsg(LotusNotesAdapaterErrorCodes.LotusNotesEDRMToDocumentTransformFailure);
                throw;
            }
        }

        /// <summary>
        /// Calculate and return email document id from message id
        /// </summary>
        /// <param name="messageId"> Message identifier </param>
        /// <returns> Lotus notes e-mail document id </returns>
        private string GetEmailDocumentIdFromMessageId(string messageId) { return ThreadingConstraint + EmailThreadingHelper.GetMD5Hash(ASCIIEncoding.UTF8.GetBytes(messageId)); }

        #endregion
    }
}
