# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="EVCorlibFileProcessorAdapter.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          This is a file that contains EVCorlibFileProcessorAdapter class 
//      </description>
//      <changelog>
//          <date value="19-August-2010"></date>
//          <date value="25-Oct-2011">Insert temporary relations issue for compund attachment</date>
//          <date value="01-05-2012">AvoidExcessiveComplexity </date>
//          <date value="04/02/2012">Bug fix for 98615</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentExtractionUtilities.ExtensionMethods;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Encapsulates file extraction and processing logic
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class EVCorlibFileProcessorAdapter : IFileProcessor
    {

        /// <summary>
        /// Errors specific to EvCorlibFileProcessorAdapaterErrors
        /// </summary>
        class EvCorlibFileProcessorAdapterErrors
        {
            /// <summary>
            /// Private constructor to avoid creating instance of this class
            /// </summary>
            private EvCorlibFileProcessorAdapterErrors() { }

            /// <summary>
            /// Error creating object of EvCorlibFileProcessorAdapater
            /// </summary>
            public static string CreateEvCorlibFileProcessAdapaterFailure = "CreateEvCorlibFileProcessAdapaterFailure";

            /// <summary>
            /// Error converting entities
            /// </summary>
            public static string ThreadEntityConversionError = "ThreadEntityConversionError";

            /// <summary>
            /// Error processing/extracting document
            /// </summary>
            public static string EvCorlibProcessDocumentError = "EvCorlibProcessDocumentError";

            /// <summary>
            /// Error adding temporary records for document relationships
            /// </summary>
            public static string AddDocumentRelationshipTemporaryRecordError = "AddDocumentRelationshipTemporaryRecordError";

            /// <summary>
            /// Error creating object of EvCorlibManager
            /// </summary>
            public static string EvCorlibManagerInstantiateError = "EvCorlibManagerInstantiateError";
        }

        /// <summary>
        /// Constants specific to EvCorlibFileAdapater
        /// </summary>
        public static class EVCorlibFileProcessorAdapterConstants
        {
            /// <summary>
            /// Depicts key that provides information about failure context. In an example, file name when extraction failed would be value for the Operation Key.
            /// It's generally used in Data variable of Exception object.
            /// </summary>
            public const string OperationKey = "taskKey";
        }

        #region IFileProcessor Members


        /// <summary>
        /// Extracts specified document and converts to a BEO which could be interpreted by rest of the application.
        /// Overload allows intermediate operations be performed as documents are extracted.
        /// In an example, a PST file extraction returns multiple documents. As each document is extracted import can be performed when written in intermediate operation delegate definition.
        /// </summary>
        /// <typeparam name="T">Additional information input to the callback function</typeparam>
        /// <param name="file">File object to be extracted</param>
        /// <param name="temporaryWorkingDirectory">Specify working directory to be used for extracting contents</param>
        /// <param name="batchSize">Batch size used while extracting mail stores</param>
        /// <param name="intermediateOperation">Additional operations to be performed as soon as extraction is done</param>
        /// <param name="errorCallback">Loop of operations expected - an error in the loop shouldn't break the loop - hence error call back.</param>
        /// <param name="obj"></param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public void ProcessDocumentWithCallBack<T>(IEnumerable<FileInfo> files, DirectoryInfo temporaryWorkingDirectory,
            int batchSize, long jobRunId, Action<EvDocumentDataEntity, T> intermediateOperation, Action<EVException> errorCallback, T obj,
            List<EmailThreadingEntity> rawDocumentRelationships)
        {
            EvCorlibManager evcorlibManager = null;
            IMailProcessor mailProcessor = null;

            try
            {
                evcorlibManager = new EvCorlibManager(temporaryWorkingDirectory, batchSize)
                {
                    Passwords = Passwords,
                    IsDeleteTemporaryFiles = (IsDeleteTemporaryFiles == DeleteExtractedFilesOptions.DeleteNonNativeFiles)
                };
                // If DeleteAll is the option, let EVCorlibManager delete all files/folders. 
                // If not this adapter will manage deleting files.
            }
            catch (EVException exception)
            {
                ((Exception)exception).Trace().Swallow();

                if (null != evcorlibManager)
                {
                    evcorlibManager.Cleanup();
                }
            }
            catch (Exception exception)
            {
                if (null != evcorlibManager)
                {
                    evcorlibManager.Cleanup();
                }

                string errorCode = EvCorlibFileProcessorAdapterErrors.EvCorlibManagerInstantiateError;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }

            // Step 1: EVCorlib manager processes the file to extract metadata and content in EDRM file format
            //foreach (EvCorlibEntity evcorlibEntity in evcorlibManager.ProcessFile(files))
            //{
            try
            {
                EvCorlibEntity evcorlibEntity = evcorlibManager.BatchProcessFiles(files);
                // Step 2-A: If it's an e-mail
                if (evcorlibEntity.HasMailStores)
                {
                    #region code for processing mail stores
                    // Factory creates appropriate mail processor adapter class (either outlook adapter or lotus notes adapter)
                    using (mailProcessor = new MailProcessorFactory().CreateMailProcessor(evcorlibEntity))
                    {
                        mailProcessor.IsDeleteTemporaryFiles = IsDeleteTemporaryFiles;
                        mailProcessor.FileProcessorInstance = evcorlibManager;
                        mailProcessor.batchIdentifier = jobRunId;

                        if (DatasetBeo != null)
                            mailProcessor.DatasetBeo = DatasetBeo;
                        if (FilterByMappedFields != null)
                            mailProcessor.FilterByMappedFields.AddRange(FilterByMappedFields);

                        mailProcessor.EvDocumentDataEntityAvailable += delegate(EvDocumentDataEntity documentEntity)
                        {
                            intermediateOperation(documentEntity, obj);
                        };

                        mailProcessor.IsDeleteTemporaryFiles = IsDeleteTemporaryFiles;

                        // Call the function to process mail documents                    
                        mailProcessor.ProcessMailDocuments(evcorlibEntity, temporaryWorkingDirectory, batchSize);

                        // capture all raw document relationship records and store them in DB at a shot.
                        if (mailProcessor.RawDocumentRelationships != null && mailProcessor.RawDocumentRelationships.Any())
                        {
                            rawDocumentRelationships.AddRange(mailProcessor.RawDocumentRelationships);
                        }
                    }

                    #endregion
                }
                if (!string.IsNullOrEmpty(evcorlibEntity.OutputFilePath))// Step 2-B: If it's an individual document   
                {
                    // code for handling individual documents
                    EvDocumentDataEntity evDocumentDataEntity = TransformEdrmToDocumentBusinessEntity(evcorlibEntity.OutputFilePath);

                    evDocumentDataEntity.PercentComplete = evcorlibEntity.HasMailStores ? 0 : 100;

                    #region Add Compound document relationships to temporary table.

                    AddRelationsToIndividualDocument(jobRunId, rawDocumentRelationships, evDocumentDataEntity);

                    #endregion Add Compound document relationships to temporary table.

                    // Importing individual document.
                    if (intermediateOperation != null)
                    {
                        intermediateOperation(evDocumentDataEntity, obj);
                    }
                }

                #region Identify and remove duplicate/incorrect relationship metadata
                // Get duplicate relationships of email type
                IEnumerable<EmailThreadingEntity> duplicateRelationships = GetDuplicateRecords(rawDocumentRelationships);

                // remove them from getting added to the system.
                if (duplicateRelationships != null && duplicateRelationships.Any())
                {
                    duplicateRelationships.ToList().ForEach(p => rawDocumentRelationships.Remove(p));
                }
                #endregion Identify and remove duplicate/incorrect relationship metadata
            }
            catch (EVException exception)
            {
                if (exception.ToUserString().Contains(Constants.DiskFullErrorMessage))
                {
                    exception.AddResMsg(ErrorCodes.ImportDiskFullErrorMessage);
                    throw;
                }
                else
                {
                    ((Exception)exception).Trace().Swallow();
                }
            }
            catch (Exception exception)
            {
                string errorCode = EvCorlibFileProcessorAdapterErrors.EvCorlibProcessDocumentError;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }
            finally
            {
                if (null != evcorlibManager)
                {
                    evcorlibManager.Cleanup();
                }
            }
            //}
        }

        /// <summary>
        /// Adds the relations to individual document.
        /// </summary>
        /// <param name="jobRunId">The job run id.</param>
        /// <param name="rawDocumentRelationships">The raw document relationships.</param>
        /// <param name="evDocumentDataEntity">The ev document data entity.</param>
        private static void AddRelationsToIndividualDocument(long jobRunId, List<EmailThreadingEntity> rawDocumentRelationships, EvDocumentDataEntity evDocumentDataEntity)
        {
            List<EmailThreadingEntity> compundDocumentRelationships = new List<EmailThreadingEntity>();
            // In case of individual document we can use list of relations in evDocumentDataEntity
            List<RelationshipBEO> lstEvRelations = evDocumentDataEntity.Relationships.ToList<RelationshipBEO>();
            if (lstEvRelations.Any())
            {
                string strCmnThreadingConstraint = Guid.NewGuid().ToString();
                lstEvRelations.ForEach(p => compundDocumentRelationships.Add(p.ToDataAccesEntity(jobRunId,
                    strCmnThreadingConstraint, ThreadRelationshipEntity.RelationshipType.CompoundDocumentAttachement)));

                rawDocumentRelationships.AddRange(compundDocumentRelationships);
            }
        }

        /// <summary>
        /// Creates instance of EvCorlibFileProcessorAdapater
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
        public EVCorlibFileProcessorAdapter()
        {
            try
            {
                _exceptions = new List<EVException>();
                _filterByMappedFields = new List<FieldMapBEO>();
                _datasetBEO = new DatasetBEO();
                _threads = new List<ThreadRelationshipEntity>();
                _miscMessages = new List<KeyValuePair<string, string>>();
                IsDeleteTemporaryFiles = DeleteExtractedFilesOptions.DeleteNone; // initialize if temporary files can be deleted
            }
            catch (Exception exception)
            {
                exception.AddResMsg(EvCorlibFileProcessorAdapterErrors.CreateEvCorlibFileProcessAdapaterFailure);
                throw;
            }
        }


        private List<ThreadRelationshipEntity> _threads;

        /// <summary>
        /// e-mail threads, replies
        /// </summary>
        public IEnumerable<RelationshipBEO> ThreadRelationships
        {
            get
            {
                return ConvertThreadRelationshipEntityToRelationshipBEO(_threads);
            }
        }

        /// <summary>
        /// Gets or Sets password file URI for extracting password protected archives
        /// </summary>
        public IEnumerable<string> Passwords
        {
            get;
            set;
        }

        DatasetBEO _datasetBEO;
        /// <summary>
        /// Dataset to which the documents being extracted belong
        /// </summary>
        public DatasetBEO DatasetBeo
        {
            get { return _datasetBEO; }
            set { _datasetBEO = value; }
        }


        IEnumerable<FieldMapBEO> _filterByMappedFields;
        /// <summary>
        /// Mapped fields - if only specific fields' data to be used.
        /// </summary>
        public IEnumerable<FieldMapBEO> FilterByMappedFields
        {
            get { return _filterByMappedFields; }
            set { _filterByMappedFields = value; }
        }

        private List<EVException> _exceptions;

        /// <summary>
        /// Gets list of exceptions while extracting files
        /// </summary>
        public IEnumerable<EVException> Errors
        {
            get { return _exceptions; }
        }

        /// <summary>
        /// Gets or Sets configuration to delete temporary files (true/false)
        /// </summary>
        public DeleteExtractedFilesOptions IsDeleteTemporaryFiles { get; set; }

        List<KeyValuePair<string, string>> _miscMessages;

        /// <summary>
        /// Gets the misc messages.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerable<KeyValuePair<string, string>> MiscMessages
        {
            get
            {
                return _miscMessages;
            }
        }

        #endregion


        /// <summary>
        /// Uses EDRM Manager and converts extracted file in EDRM file format to 
        /// </summary>
        /// <param name="edrmFilePath"> EDRM file to be converted </param>
        /// <returns> EV Document Entity for Document and relationship data </returns>
        private EvDocumentDataEntity TransformEdrmToDocumentBusinessEntity(string edrmFilePath)
        {
            // EDRM manger handles converting extracted data to BEOs (business entities) for the use of EV.
            EDRMManager edrmManager = new EDRMManager(edrmFilePath);
            List<RelationshipBEO> relationships = new List<RelationshipBEO>(); // using new because it's call by reference
            List<RVWDocumentBEO> documents = null;

            #region Obtain and set misc messages - these messages contain information like Password protected archive file extraction failure
            foreach (DocumentEntity eDRMDocument in edrmManager.Documents)
            {
                foreach (TagEntity eDRMTags in
                    eDRMDocument.Tags.Where(p => p.TagName.Equals(Constants.ExtractionErrorTagName)).ToList())
                {
                    List<TagEntity> eDRMTagsForFileName =
                        eDRMDocument.Tags.Where(p => p.TagName.Equals(Constants.FileNameTag)).ToList();
                    string fileName = (eDRMTagsForFileName.Count > 0) ? eDRMTagsForFileName[0].TagValue : string.Empty;

                    _miscMessages.Add(new KeyValuePair<string, string>(fileName, eDRMTags.TagValue));
                    //If Extraction fail due to password protection then return empty EvDocument entity so that no document will be imported
                    if (eDRMTags.TagValue.ToLower().Contains(Constants.ExtractionFailString))
                    {
                        return new EvDocumentDataEntity(); // Empty EvDocument entity
                    }
                }
            }
            #endregion

            // i) Get Document Business Entities

            // if dataset BEO and filter for mapped fields is set - pass this information to EDRM manager so that filter condition is applied and dataset details are prefilled.
            // else get all documents
            if (DatasetBeo != null && FilterByMappedFields != null)
            {
                documents = edrmManager.GetDocuments((List<FieldMapBEO>)FilterByMappedFields,
                    DatasetBeo.Matter.FolderID, DatasetBeo.CollectionId, ref relationships);
            }
            else
            {
                documents = edrmManager.GetDocuments();
            }

            // Adding created By property for each documents
            documents.ForEach(x => x.CreatedBy = DatasetBeo.CreatedBy);

            // Cleanup if delete non native files is set
            if (IsDeleteTemporaryFiles == DeleteExtractedFilesOptions.DeleteNonNativeFiles)
            {
                edrmManager.DeleteNonNativeFiles();
            }

            // Return the object for with extracted data.
            return new EvDocumentDataEntity { Documents = documents, Relationships = relationships };
        }

        /// <summary>
        /// Convert ThreadRelationshipEntity To RelationshipBEO
        /// </summary>
        /// <param name="threads"> List of ThreadRelationshipEntities </param>
        /// <returns> List of RelationshipBEO </returns>
        private static IEnumerable<RelationshipBEO> ConvertThreadRelationshipEntityToRelationshipBEO(IEnumerable<ThreadRelationshipEntity> threads)
        {
            try
            {
                return threads.Select(thread => new RelationshipBEO()
                {
                    ChildDocumentId = thread.ChildDocumentId,
                    FamilyDocumentId = thread.FamilyId,
                    ParentDocId = thread.ParentDocumentId,
                    Type = thread.ThreadRelationshipType.ToString()
                }).ToList();
            }
            catch (EVException exception)
            {
                ((Exception)exception).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = EvCorlibFileProcessorAdapterErrors.ThreadEntityConversionError;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }
            return null;
        }

        /// <summary>
        /// Gets the duplicate records (relationships with duplication child document Ids).
        /// </summary>
        /// <param name="relationships">The relationships.</param>
        /// <returns> Duplicate records</returns>
        private static IEnumerable<EmailThreadingEntity> GetDuplicateRecords(IEnumerable<EmailThreadingEntity> relationships)
        {

            List<EmailThreadingEntity> duplicateRelationships = new List<EmailThreadingEntity>();

            // loop through original relationship list
            foreach (IEnumerable<EmailThreadingEntity> duplicateRelationship in relationships.Select(relationship => relationships.Where(p => p.ChildDocumentID.Equals(relationship.ChildDocumentID.ToString(), StringComparison.InvariantCultureIgnoreCase))).Where(matchingRelationshps => matchingRelationshps != null && matchingRelationshps.Count() > 1).Select(matchingRelationshps => matchingRelationshps.Where(p => p.RelationshipType == ThreadRelationshipEntity.RelationshipType.OutlookEmailThread
                                                                                                                                                                                                                                                                                                                                                                                                        || p.RelationshipType == ThreadRelationshipEntity.RelationshipType.LotusNotesEmailThread)).Where(duplicateRelationship => duplicateRelationship != null && duplicateRelationship.Any()))
            {
                duplicateRelationships.AddRange(duplicateRelationship);
            }

            return duplicateRelationships;

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
    }
}
