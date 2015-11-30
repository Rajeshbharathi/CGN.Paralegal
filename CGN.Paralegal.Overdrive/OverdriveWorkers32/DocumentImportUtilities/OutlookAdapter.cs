//-----------------------------------------------------------------------------------------
// <copyright file="OutlookAdapater.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          This is a file that contains OutlookAdapater class 
//      </description>
//      <changelog>
//          <date value="19-August-2010"></date>
//          <date value="02-02-2012"> Bug Fix:94806</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
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

    using LexisNexis.Evolution.Infrastructure;

    /// <summary>
    /// Adapter to abstract outlook file extraction and relationship (threading calculation)
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class OutlookAdapter : IMailProcessor
    {

        /// <summary>
        /// This field is required to track all document Ids per threading constraint. (A threading constraint could be associated to a PST).
        /// Tracking all document Ids is required as older versions of Outlook/PST allow duplicate Conversation Index from which document id is calculated.
        /// This field tracks and eliminates duplicate document Ids.
        /// </summary>
        private List<string> _documentsIds;


        private string _threadingConstraint;

        /// <summary>
        /// Gets or Sets threading constraint
        /// E-mail threading shall be limited to one or more mail stores. By default the adapter limits to one instance - if it need to be more than one, calling module should set the value.
        /// For example threading is done among e-mails of one PST. it should not consider e-mails in a different PST. This constraint is enforced by this value.        /// 
        /// </summary>
        public string ThreadingConstraint
        {
            get { return _threadingConstraint; }
            set
            {
                // every new threading constraint will have new set of document ids tracked. Duplicates identified to the level of PST
                _documentsIds = new List<string>();

                // It shall be used in document id. Length of the value in database 64 characters - rest 32 come from document.
                if (value.Length > 32) throw new EVException().AddResMsg(ErrorCodes.IllegalThreadingConstraint);
                _threadingConstraint = value;
            }
        }

        /// <summary>
        /// Encapsulates errors that might be caused in Outlook Adapter
        /// </summary>
        public class ErrorCodes
        {
            /// <summary>
            /// Failures to process Outlook Mail stores
            /// </summary>
            public const string OutlookMailStoreProcessFailure = "OutlookMailStoreProcessFailure";

            /// <summary>
            /// Failure to convert extracted EDRM file to document entities
            /// </summary>
            public const string OutlookEDRMToDocumentTransformFailure = "OutlookEDRMToDocumentTransformFailure";

            /// <summary>
            /// Failure to create object of Outlook Adapter
            /// </summary>
            public const string CreateOutlookAdapterInstanceFailure = "CreateOutlookAdapterInstanceFailure";

            /// <summary>
            /// Failure to calculate thread relationship for Outlook e-mail messages
            /// </summary>
            public const string ThreadCalculationFailure = "OutlookThreadCalculationFailure";

            /// <summary>
            /// Error indicating length of threading constraint exceeds the valid limit.
            /// </summary>
            public const string IllegalThreadingConstraint = "IllegalOutlookThreadingConstraint";

            /// <summary>
            /// Error indicating length of conversation index is incorrect.
            /// </summary>
            public const string IllegalConversationIndex = "IllegalConversationIndex";
        }

        #region IMailProcessor Members



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

        /// <summary>
        /// Event raised when document is available for further processing, for example importing.
        /// </summary>
        public event Action<EvDocumentDataEntity> EvDocumentDataEntityAvailable;

        private List<EVException> _errors;
        /// <summary>
        /// Gets errors while extracting e-mail documents.
        /// </summary>
        public IEnumerable<EVException> Errors
        {
            get { return _errors; }
        }

        /// <summary>
        /// Dataset to which outlook documents belong
        /// </summary>
        public DatasetBEO DatasetBeo
        {
            get;
            set;
        }

        private List<FieldMapBEO> _filterByMappedFields;
        /// <summary>
        /// Indicates to the outlook adapter that, do not extract all fields from the outlook files, filter specific fields mentioned here.
        /// </summary>
        public List<FieldMapBEO> FilterByMappedFields
        {
            get { return _filterByMappedFields; }
        }

        /// <summary>
        /// Gets or Sets configuration to delete temporary files (true/false)
        /// </summary>
        public DeleteExtractedFilesOptions IsDeleteTemporaryFiles { get; set; }


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
        /// Creates instance of OutlookAdapater class.
        /// </summary>
        public OutlookAdapter()
        {
            try
            {
                _documentsIds = new List<string>();
                _documentRelationships = new List<EmailThreadingEntity>();
                // Threading constraint by default is hash of a GUID. Hash is calculated to set the size to 32 to characters. rest 32 characters come from each document.
                _threadingConstraint = EmailThreadingHelper.GetMD5Hash(Guid.NewGuid().ToByteArray());
                _errors = new List<EVException>();
                _filterByMappedFields = new List<FieldMapBEO>();
                IsDeleteTemporaryFiles = DeleteExtractedFilesOptions.DeleteNone; // Initialize if temporary files can be deleted
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = ErrorCodes.CreateOutlookAdapterInstanceFailure;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }
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
        /// Extracts specified mail document and converts to a BEO which could be interpreted by rest of the application.
        /// Overload allows intermediate operations be performed as documents are extracted. 
        /// In an example, a PST file extraction returns multiple documents. As each document is extracted import can be performed. 
        /// The logic to import need to be written in event handler for EvDocumentDataEntityAvailable
        /// This approach is useful when there is a loop of time taking operations to be done.
        /// </summary>
        /// <param name="mailEntity">List of mail stores to be processed</param>        
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
                FileProcessorInstance.IsDeleteTemporaryFiles =
                    (IsDeleteTemporaryFiles == DeleteExtractedFilesOptions.DeleteNonNativeFiles) ? true : false;

                FileProcessorInstance.ProcessMailStores(evCorlibEntity, TransformOutlookEDRMToDocumentBusinessEntity);

            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = ErrorCodes.OutlookMailStoreProcessFailure;
                exception.AddErrorCode(errorCode).Trace().Swallow();
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

        #region private functions

        /// <summary>
        /// Transforms Outlook specific EDRM file to Document Business Entity
        /// </summary>
        /// <param name="edrmFileName">EDRM file to be transformed</param>
        /// <param name="currentDocumentIndex"> Index of current document in list of documents being extracted
        /// Null able column - can be null when percent complete need not be calculated.
        /// </param>
        /// <returns>
        /// EvDocumentDataEntity that abstracts documents and relationships
        /// </returns>
        private EvDocumentDataEntity TransformOutlookEDRMToDocumentBusinessEntity(string edrmFileName, string mailStorePath, int? currentDocumentIndex)
        {
            try
            {
                OutlookEdrmManager outlookEdrmManager = new OutlookEdrmManager(edrmFileName);
                List<RelationshipBEO> relationships = new List<RelationshipBEO>();
                outlookEdrmManager.GetDocumentIdFromConversationIndex += GetUniqueEmailDocumentIDFromConversationIndex;
                List<RVWDocumentBEO> documents = outlookEdrmManager.GetDocuments(FilterByMappedFields, DatasetBeo.Matter.FolderID,
                                                                    DatasetBeo.CollectionId.ToString(), ref relationships);
                //Replace EVCorlib Mail Message file extension with CNEV file extension
                documents.ForEach
                    (doc => doc.FileExtension = doc.FileExtension.Replace(Constants.EVCorlibEMailMessageExtension, Constants.CNEvEmailMessageExtension));
                UpdateNativeDocumentFileNameForEMailMessages(documents);

                // Adding created By property for each documents
                documents.ForEach(x => x.CreatedBy = DatasetBeo.CreatedBy);

                #region Create Ev Document Data Entity and raise event for processing/importing available documents
                EvDocumentDataEntity evDocumentDataEntity = new EvDocumentDataEntity()
                {
                    Documents = documents,
                    OutlookMailStoreDataEntity = new OutlookMailStoreEntity
                    {
                        EntryIdAndEmailMessagePairs = GetEMailAndEntryIdPairs(outlookEdrmManager, edrmFileName),
                        PSTFile = new FileInfo(mailStorePath)
                    },
                    PercentComplete = currentDocumentIndex ?? (100.00 / FileProcessorInstance.EntryIdsCount) * currentDocumentIndex.Value
                };
                #endregion Create Ev Document Data Entity and raise event for processing/importing available documents

                // raise event as the document processing completes
                if (EvDocumentDataEntityAvailable != null)
                    EvDocumentDataEntityAvailable(evDocumentDataEntity);

                #region insert temporary records for thread calculation

                List<EmailThreadingEntity> lstEmailThreadEntities = new List<EmailThreadingEntity>();
                if (relationships != null && relationships.Count > 0)
                {
                    foreach (var relation in relationships)
                    {
                        EmailThreadingEntity ete = relation.ToDataAccesEntity(batchIdentifier, ThreadingConstraint, ThreadRelationshipEntity.RelationshipType.OutlookAttachment);
                        lstEmailThreadEntities.Add(ete);
                    }
                }

                IEnumerable<OutlookEMailDocumentEntity> outlookEmails = outlookEdrmManager.OutlookEmailDocumentEntities;
                // store temporary record for calculating email relationship
                if (outlookEmails != null && outlookEmails.Any())
                {
                    List<OutlookEMailDocumentEntity> lstOutLookEmails = new List<OutlookEMailDocumentEntity>();
                    foreach (OutlookEMailDocumentEntity outLookEmail in outlookEmails)
                    {
                        if ((!string.IsNullOrEmpty(outLookEmail.ConversationIndex)) && outLookEmail.ConversationIndex.Length >= 44)
                        {
                            lstOutLookEmails.Add(outLookEmail);
                        }
                    }
                    lstOutLookEmails.ForEach(outLookEmail =>
                                                  lstEmailThreadEntities.Add(
                                                        ToEmailThreadEntity(
                                                                    outLookEmail,
                                                                    batchIdentifier,
                                                                    ThreadingConstraint,
                                                                    ThreadRelationshipEntity.RelationshipType.OutlookEmailThread)));



                }
                //Used Bulk Add Temp Relationships in EmailThreadingHelper
                if (lstEmailThreadEntities.Any())
                {
                    //EmailThreadingHelper.BulkAddRelationshipTemporaryRecord(lstEmailThreadEntities);
                    _documentRelationships.AddRange(lstEmailThreadEntities);
                }

                #endregion

                // Cleanup if delete non native files is set
                if (IsDeleteTemporaryFiles == DeleteExtractedFilesOptions.DeleteNonNativeFiles)
                {
                    outlookEdrmManager.DeleteNonNativeFiles();
                }

                return evDocumentDataEntity;
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = ErrorCodes.OutlookEDRMToDocumentTransformFailure;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }

            return null;
        }


        /// <summary>
        /// Updates the native document file name for E mail messages.
        /// </summary>
        /// <param name="documents">The documents.</param>
        private void UpdateNativeDocumentFileNameForEMailMessages(List<RVWDocumentBEO> documents)
        {
            // EDLoader outputs msg as .htm files.
            // we later in the import job override .htm files with .msg files.
            // Below fixes/overrides .htm file names is document object with .msg extension
            if (documents != null)
            {
                foreach (RVWDocumentBEO document in documents)
                {
                    if (document.MimeType.Equals("application/vnd.ms-outlook", StringComparison.InvariantCultureIgnoreCase) &&
                        string.IsNullOrEmpty(document.NativeFilePath) == false && File.Exists(document.NativeFilePath) &&
                        Path.GetExtension(document.NativeFilePath).Equals(".htm", StringComparison.InvariantCultureIgnoreCase))
                    {
                        document.NativeFilePath = string.Format("{0}.{1}", Path.Combine(Path.GetDirectoryName(document.NativeFilePath), Path.GetFileNameWithoutExtension(document.NativeFilePath)), "msg");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the E mail and entry id pairs from give EdrmManager object. This information can be used for email message generation
        /// </summary>
        /// <param name="outlookEdrmManager">The outlook edrm manager.</param>
        /// <param name="edrmFilePath">The edrm file path.</param>
        /// <returns>E mail and entry id pairs </returns>
        private IEnumerable<KeyValuePair<string, string>> GetEMailAndEntryIdPairs(OutlookEdrmManager outlookEdrmManager, string edrmFilePath)
        {
            if (outlookEdrmManager.OutlookEmailDocumentEntities != null)
            {
                List<KeyValuePair<string, string>> EntryIdEMailMessagePair = new List<KeyValuePair<string, string>>();
                foreach (OutlookEMailDocumentEntity outlookEmailDocumentEntity in outlookEdrmManager.OutlookEmailDocumentEntities)
                {
                    string externalFileName = string.Empty;

                    // Take first file in external file entities, use same name but different extension for msg file.
                    // This msg file doesn't exist here - but will be created later in the flow with the given name, decided here..
                    if (outlookEmailDocumentEntity.Files != null && outlookEmailDocumentEntity.Files.Count > 0 &&
                        outlookEmailDocumentEntity.Files[0].ExternalFile != null && outlookEmailDocumentEntity.Files[0].ExternalFile.Count > 0)
                    {
                        ExternalFileEntity externalFile = outlookEmailDocumentEntity.Files[0].ExternalFile[0];
                        externalFileName = CreateFileURIForEmailMessage(Path.GetDirectoryName(edrmFilePath), externalFile.FilePath, externalFile.FileName);
                    }

                    EntryIdEMailMessagePair.Add(new KeyValuePair<string, string>(outlookEmailDocumentEntity.EntryId, externalFileName));
                }

                return EntryIdEMailMessagePair;
            }
            return null;
        }


        /// <summary>
        /// Identifies Parent document id for outlook e-mail, given the conversation index
        /// </summary>
        /// <param name="conversationIndex"> Conversation index obtained in EDRM format - metadata for outlook messages </param>
        /// <returns></returns>
        private string GetParentDocumentId(string conversationIndex)
        {
            if (conversationIndex.Length == 44) return string.Empty;
            else if (conversationIndex.Length < 44) throw new EVException().AddResMsg(ErrorCodes.IllegalConversationIndex);

            string parentConversationIndex = conversationIndex.Substring(0, conversationIndex.Length - 10);
            return GetEmailDocumentIDFromConversationIndex(parentConversationIndex);
        }

        /// <summary>
        /// Identify and return first document's id in the chain
        /// </summary>
        /// <param name="conversationIndex"> Conversation index obtained in EDRM format - metadata for outlook messages </param>
        /// <returns> family id </returns>
        private string GetFamilyId(string conversationIndex)
        {
            if (conversationIndex.Length < 44) throw new EVException().AddResMsg(ErrorCodes.IllegalConversationIndex);
            return GetEmailDocumentIDFromConversationIndex(conversationIndex.Substring(0, 44));
        }

        /// <summary>
        /// identify and return unique document id given the conversation index.
        /// </summary>
        /// <param name="conversationIndex"> Conversation index obtained in EDRM format - metadata for outlook messages </param>
        /// <param name="allowDuplicateIds"> Duplicates are allowed by default as this code is used to calculates parent document Ids and such other purposes </param>
        /// <returns> document id </returns>
        private string GetUniqueEmailDocumentIDFromConversationIndex(string conversationIndex)
        {
            string documentId = ThreadingConstraint + EmailThreadingHelper.GetMD5Hash(ASCIIEncoding.UTF8.GetBytes(conversationIndex));

            // There is a possibility of document Ids being duplicated when created from threading constraint. 
            // Hence if it's found to be a duplicate id, replace with GUID.
            if (_documentsIds.Contains(documentId))
            {
                documentId = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            }
 
            _documentsIds.Add(documentId);

            return documentId;
        }

        /// <summary>
        /// identify and return document id given the conversation index.
        /// </summary>
        /// <param name="conversationIndex"> Conversation index obtained in EDRM format - metadata for outlook messages </param>        
        /// <returns> document id </returns>
        private string GetEmailDocumentIDFromConversationIndex(string conversationIndex)
        {
            return ThreadingConstraint + EmailThreadingHelper.GetMD5Hash(ASCIIEncoding.UTF8.GetBytes(conversationIndex));
        }

        /// <summary>
        /// converts OutLookEmailDocumentEntity to EmailThreadEntity
        /// </summary>
        /// <param name="outlookEmailDocumentEntity">The outlook email document entity.</param>
        /// <param name="jobRunId">The job run id.</param>
        /// <param name="threadingConstraint">The threading constraint.</param>
        /// <param name="relationshipType">Type of the relation ship.</param>
        /// <returns></returns>
        private EmailThreadingEntity ToEmailThreadEntity(OutlookEMailDocumentEntity outlookEmailDocumentEntity, long jobRunId, string threadingConstraint, ThreadRelationshipEntity.RelationshipType relationshipType)
        {
            EmailThreadingEntity toReturn = new EmailThreadingEntity()
            {
                JobRunID = jobRunId,
                ChildDocumentID = GetEmailDocumentIDFromConversationIndex(outlookEmailDocumentEntity.ConversationIndex),
                ParentDocumentID = GetParentDocumentId(outlookEmailDocumentEntity.ConversationIndex),
                RelationshipType = relationshipType,
                ThreadingConstraint = threadingConstraint,
                FamilyID = GetFamilyId(outlookEmailDocumentEntity.ConversationIndex),
                ConversationIndex = outlookEmailDocumentEntity.ConversationIndex
            };
            // Debug 
            //Tracer.Warning("Subj: {0} DocId: {1}", outlookEmailDocumentEntity.ConversationTopic, toReturn.ChildDocumentID);
            return toReturn;
        }

        /// <summary>
        /// Creates file URI for specified file in EDRM document.
        /// Handles 1) relative path from EDRM location, 2) file at EDRM location and 3) absolute path the file 
        /// </summary>
        /// <param name="fileLocation"> directory in which file exists </param>
        /// <param name="fileName"> file name </param>
        /// <returns> Complete file URI </returns>
        private string CreateFileURIForEmailMessage(string baseFilepath, string fileLocation, string fileName)
        {
            // create email message file name with same name as that of other external files. Just change the extension.
            string emailMessageFilename = string.Format("{0}.msg", Path.GetFileNameWithoutExtension(fileName));

            // file at EDRM location
            if (string.IsNullOrEmpty(fileLocation))
            {
                return baseFilepath + @"\" + emailMessageFilename;
            }
            else
            {
                // Check if file location is absolute path
                // Condition 1: if file location contains ":", it's drive location. for example C:\ - hence it's absolute path.
                // Condition 2: if file location's first character is "\\" it's shared drive - hence it's absolute path.
                if (fileLocation.Contains(":") || fileLocation.Substring(0, 1).Equals("\\"))
                {
                    // does last character of file location have \. if not add it.
                    if (!fileLocation.Substring(fileLocation.Length - 1, 1).Equals(@"\")) fileLocation = fileLocation + @"\";

                    return fileLocation + emailMessageFilename;
                }
                else // relative path to the file from EDRM location
                {
                    // does last character of file location have \. if not add it.
                    if (!fileLocation.Substring(fileLocation.Length - 1, 1).Equals(@"\")) fileLocation = fileLocation + @"\";

                    return baseFilepath + @"\" + fileLocation + emailMessageFilename;
                }
            }
        }

        #endregion




    }
}
