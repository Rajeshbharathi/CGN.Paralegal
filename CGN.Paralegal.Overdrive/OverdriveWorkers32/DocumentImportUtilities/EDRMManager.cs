//-----------------------------------------------------------------------------------------
// <copyright file=" EDRMManager.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          Helper Class containing methods to parse edrm xml
//      </description>
//      <changelog>
//          <date value="28-april-2011">created</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    /// <summary>
    /// EDRM XML file manager
    /// </summary>
    public class EDRMManager
    {

        /// <summary>
        /// EDRM XML's Entity representation
        /// </summary>
        private EDRMEntity edrmEntity;

        protected EDRMEntity EDRMEntity
        {
            get { return edrmEntity; }
            set { edrmEntity = value; }
        }

        /// <summary>
        /// List of Documents
        /// </summary>
        public IEnumerable<DocumentEntity> Documents
        {
            get
            {
                return edrmEntity.BatchEntity.DocumentEntity;
            }
        }

        #region Constructor

        /// <summary>
        /// Creates EDRMManager object with eDRMfile at specified location
        /// </summary>
        /// <param name="eDRMFile">EDRM file location on disk</param>
        public EDRMManager(string eDRMFile)
        {
            try
            {
                // Make source path usable in C# code.
                sourcePath = eDRMFile.Replace("/", @"\");

                // EDRM file name
                edrmFileName = sourcePath.Substring(sourcePath.LastIndexOf(@"\", StringComparison.Ordinal));

                // Set the source path. Folder in which the EDRM file exists
                sourcePath = sourcePath.Substring(0, sourcePath.LastIndexOf(@"\", StringComparison.Ordinal));

                // Deserialize EDRM XML
                //_EDRMEntity = (EDRMEntity)(new XmlSerializer(typeof(EDRMEntity)).Deserialize(new StreamReader(eDRMFile)));
                edrmEntity = new EdrmXmlDeserializer().DeserializeEdrmXml(eDRMFile);

                if (edrmEntity.BatchEntity != null)
                {
                    if (edrmEntity.BatchEntity.Relationships != null)
                    {
                        relationships = edrmEntity.BatchEntity.Relationships;
                    }
                }

            }
            catch (Exception ex)
            {
                // This is not an exception caught in superficial layers. So EVException and error code need to be used.
                Tracer.Error(string.Format("{0}:{1}", "EDRM Manager", ex.ToDebugString()));
                ex.AddUsrMsg("EDRM Manager object couldn't be created. ");
                throw;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Relationships in the current EDRM file
        /// </summary>
        private readonly List<RelationshipEntity> relationships;

        /// <summary>
        /// Gets or Sets Relationships in the current EDRM file
        /// </summary>
        public List<RelationshipEntity> Relationships
        {
            get
            {
                return relationships;
            }
        }

        /// <summary>
        /// Location of EDRM file. This is used for finding relative path for referenced documents, considering it the root.
        /// </summary>
        private string sourcePath;
        /// <summary>
        /// Gets or Sets location of EDRM file. This is used for finding relative path for referenced documents, considering it the root.
        /// </summary>
        public string SourcePath
        {
            get
            {
                return sourcePath;
            }

            set
            {
                sourcePath = value;
            }
        }


        /// <summary>
        /// EDRM XML file name
        /// </summary>
        private readonly string edrmFileName;
        /// <summary>
        /// Gets or sets EDRM file name (only the file name not complete URI)
        /// Use SourcePath Property for location of the file.
        /// </summary>
        public string EDRMFileName
        {
            get
            {
                return edrmFileName;
            }
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Gets documents from EDRM file
        /// </summary>
        /// <returns> List of Document Business Entities </returns>
        public virtual List<RVWDocumentBEO> GetDocuments()
        {
            List<RVWDocumentBEO> documents = new List<RVWDocumentBEO>();

            // Loop through documents and extract fields and file details.
            foreach (DocumentEntity edrmDocument in edrmEntity.BatchEntity.DocumentEntity)
            {
                RVWDocumentBEO rvwDocumentBEO = new RVWDocumentBEO();

                // Transform "EDRM document entity" to EV's Document BEO

                // This function doesn't set field data
                // Field data is set separately as there are two variances of this operation. 
                // 1) set all fields available in the EDRM file
                // 2) set mapped fields only.
                TransformEDRMDocumentEntityToDocumentBusinessEntity(ref rvwDocumentBEO, edrmDocument);

                // Set field data - set all fields available in the EDRM file.
                #region Add all fields from EDRM file

                foreach (RVWDocumentFieldBEO rvwDocumentFieldBEO in edrmDocument.Tags.Select(tagEntity => new RVWDocumentFieldBEO
                                                                                                              {
                                                                                                                  FieldName = tagEntity.TagName,
                                                                                                                  FieldValue = tagEntity.TagValue
                                                                                                              }))
                {
                    rvwDocumentBEO.FieldList.Add(rvwDocumentFieldBEO);
                }

                #endregion

                documents.Add(rvwDocumentBEO);
            }

            return documents;
        }

        /// <summary>
        /// Gets Documents from EDRM file, extracts specified mapped fields only.
        /// </summary>
        /// <param name="mappedFields"> Mapped fields with Dataset </param>
        /// <param name="matterID"> Matter folder id into which documents are being imported (Dataset of exists in this matter - documents are imported into dataset) </param>
        /// <param name="collectionID"> Identifier for Collection into which documents are expected to be imported </param>
        /// <param name="relationshipBEOs"> By Reference - List of relationships  </param>
        /// <returns> List of Document BEOs </returns>
        public virtual List<RVWDocumentBEO> GetDocuments(List<FieldMapBEO> mappedFields, long matterID, string collectionID, 
ref List<RelationshipBEO> relationshipBEOs)
        {
            List<RVWDocumentBEO> documents = new List<RVWDocumentBEO>();

            #region Null Checks

            if (relationshipBEOs == null)
                relationshipBEOs = new List<RelationshipBEO>();

            // Set collection ID - if it's empty throw error
            if (collectionID == string.Empty)
                throw new EVException().AddResMsg("CollectionIDIsEmpty");

            // Set matter id. if the object is not available throw error
            if (matterID == 0)
                throw new EVException().AddResMsg("MatterObjectIsEmpty");

            #endregion

            try
            {
                // Loop through documents and extract fields and file details.
                foreach (DocumentEntity edrmDocument in edrmEntity.BatchEntity.DocumentEntity)
                {
                    // Create a document BEO for each document entity
                    RVWDocumentBEO rvwDocumentBEO = new RVWDocumentBEO { CollectionId = collectionID, MatterId = matterID };

                    // Transform "EDRM document entity" to EV's Document BEO
                    // UPDATE RELATIONSHIP INFORMATION WITH ACCURATE DOCUMENT ID (override EDRM document id with the id tobe stored in Vault)
                    // This function doesn't set field data
                    // Field data is set separately as there are two variances of this operation. 
                    // 1) set all fields available in the EDRM file
                    // 2) set mapped fields only.
                    TransformDocumentsAndRelationships(ref rvwDocumentBEO, edrmDocument);

                    // Add all mapped fields from EDRM file
                    TransformEDRMFieldsToDocumentFields(ref rvwDocumentBEO, edrmDocument, mappedFields);

                    documents.Add(rvwDocumentBEO);
                }// End of loop throgh documents in EDRM file

                // Create relationships
                relationshipBEOs = TransformEDRMRelationshipsToDocumentRelationships(collectionID);
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("ErrorGettingDocumentsFromEDRM");
                throw;
            }

            return documents;
        }

        #endregion

        #region Protected Functions

        /// <summary>
        /// Transform "EDRM document entity" to EV's Document BEO
        /// This function doesn't set field data
        /// Field data is set separately as there are two variances of this operation. </summary>
        /// 1) set all fields available in the EDRM file<param name="rvwDocumentBEO"></param>
        /// 2) set mapped fields only.
        /// </summary>
        /// <param name="rvwDocumentBEO"> Document Business Entity, by reference so that the transformed data is updated in this object. </param>
        /// <param name="edrmDocument"> EDRM document entity which is going tobe transformed to EV Document Business Entity. </param>
        protected virtual void TransformEDRMDocumentEntityToDocumentBusinessEntity(ref RVWDocumentBEO rvwDocumentBEO, DocumentEntity edrmDocument)
        {
            // Initialize if the object is not created
            if (rvwDocumentBEO == null)
                rvwDocumentBEO = new RVWDocumentBEO();

            // Document ID is MD5 hash of EDRM file + EDRM Document ID.
            // This makes document unique in the system and identifies if same EDRM file attempted to be imported twice.
            // Special character to 
            rvwDocumentBEO.DocumentId = GetDocumentId(edrmDocument);

            // Set MIME type
            rvwDocumentBEO.MimeType = edrmDocument.MIMEType;

            #region Add associate file details/names from EDRM file

            // NOTES: 
            // Binary and text is set later by the consuming module. This is to reduce weight of objects
            // Binary and text could increase memory by great extent.

            foreach (FileEntity fileEntity in edrmDocument.Files)
            {
                // Check if type is native
                if (fileEntity.FileType.ToLower().Equals(Constants.EDRMAttributeNativeFileType.ToLower()))
                {
                    // Set file path for native file types.
                    // Condition - check if native file entity has external file entities in it or not.
                    if (fileEntity.ExternalFile != null)
                    {
                        // Checks in below statement 1) null check on "file path" and 2) are there any external files
                        rvwDocumentBEO.FileName = (fileEntity.ExternalFile.Count > 0) ? fileEntity.ExternalFile[0].FileName : string.Empty;
                        rvwDocumentBEO.NativeFilePath = (fileEntity.ExternalFile.Count > 0) ? CreateFileURI(fileEntity.ExternalFile[0].FilePath, fileEntity.ExternalFile[0].FileName) : string.Empty;
                        rvwDocumentBEO.FileExtension = (fileEntity.ExternalFile.Count > 0) ? fileEntity.ExternalFile[0].FileName.Substring(fileEntity.ExternalFile[0].FileName.LastIndexOf(".")) : string.Empty;

                        if (!string.IsNullOrEmpty(rvwDocumentBEO.NativeFilePath) && File.Exists(rvwDocumentBEO.NativeFilePath))
                        {
                            //Calculating size of file in KB
                            FileInfo fileInfo = new FileInfo(rvwDocumentBEO.NativeFilePath);
                            rvwDocumentBEO.FileSize = (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);
                        }
                    }
                }

                // Check if the type is Text.
                if (fileEntity.FileType.ToLower().Equals(Constants.EDRMAttributeTextFileType.ToLower()))
                {
                    if (fileEntity.ExternalFile != null)
                    {
                        // For text set file details in TextContent property. Set the real text just before import.
                        // This is to help reduce weight of the object.

                        // Checks in below statement 1) null check on "file path" and 2) are there any external files
                        if (rvwDocumentBEO.DocumentBinary == null) { rvwDocumentBEO.DocumentBinary = new RVWDocumentBinaryBEO(); }

                        RVWExternalFileBEO textFile = new RVWExternalFileBEO
                        {
                            Type = Constants.EDRMAttributeTextFileType,
                            Path = (fileEntity.ExternalFile.Count > 0) ? CreateFileURI(fileEntity.ExternalFile[0].FilePath, fileEntity.ExternalFile[0].FileName) : string.Empty
                        };

                        rvwDocumentBEO.DocumentBinary.FileList.Add(textFile);
                    }
                }
            }

            #endregion
        }

        /// <summary>
        /// Transform "EDRM document entity" to EV's Document BEO
        /// This function doesn't set field data
        /// Field data is set separately as there are two variances of this operation. </summary>
        /// 1) set all fields available in the EDRM file
        /// 2) set mapped fields only.
        /// 
        /// In relationship objects for the EDRM Manager document IDs are updated in a format compatible with Vault/EV system.
        /// </summary>
        /// <param name="rvwDocumentBEO"> Document Business Entity, by reference so that the transformed data is updated in this object. </param>
        /// <param name="edrmDocument"> EDRM document entity which is going tobe transformed to EV Document Business Entity. </param>
        protected virtual void TransformDocumentsAndRelationships(ref RVWDocumentBEO rvwDocumentBEO, DocumentEntity edrmDocument)
        {
            // Transform "EDRM document entity" to EV's Document BEO
            TransformEDRMDocumentEntityToDocumentBusinessEntity(ref rvwDocumentBEO, edrmDocument);

            // Update relationship's IDs 
            foreach (RelationshipEntity relationship in relationships)
            {
                if (edrmDocument.DocumentID.Equals(relationship.ParentDocID))
                {
                    relationship.ParentDocID = rvwDocumentBEO.DocumentId;
                }

                if (edrmDocument.DocumentID.Equals(relationship.ChildDocID))
                {
                    relationship.ChildDocID = rvwDocumentBEO.DocumentId;
                }
            }
        }

        /// <summary>
        /// Transforms EDRM fields to list of EV Document field Business entities
        /// </summary>
        /// <param name="rvwDocumentBEO"> call by reference - Document object for which fields to be updated. </param>
        /// <param name="edrmDocument"> EDRM document object </param>
        /// <param name="mappedFields"> fields mapped while scheduling import. </param>
        protected virtual void TransformEDRMFieldsToDocumentFields(ref RVWDocumentBEO rvwDocumentBEO, DocumentEntity edrmDocument, List<FieldMapBEO> mappedFields)
        {
            #region Add all mapped fields
            foreach (FieldMapBEO mappedField in mappedFields)
            {
                //Create a Tag Entity for each mapped field
                TagEntity tag = new TagEntity();

                // Get tag/field from EDRM file for give mapped field
                IEnumerable<TagEntity> tagEnumerator = edrmDocument.Tags.Where(p => p.TagName.Equals(mappedField.SourceFieldName, StringComparison.CurrentCultureIgnoreCase));
                if (tagEnumerator.Count<TagEntity>() > 0) { tag = tagEnumerator.First<TagEntity>(); }

                // Adding Field map information only if Tag Value is available.

                // Create a Field business Entity for each mapped field
                RVWDocumentFieldBEO fieldBEO = new RVWDocumentFieldBEO()
                {
                    // set required properties / field data
                    FieldId = mappedField.DatasetFieldID,
                    FieldName = mappedField.DatasetFieldName,
                    FieldValue = tag.TagValue ?? string.Empty,
                    FieldType = new FieldDataTypeBusinessEntity() { DataTypeId = mappedField.DatasetFieldTypeID }
                };

                // Add tag to the document
                rvwDocumentBEO.FieldList.Add(fieldBEO);

            } // End of loop through fields in a document.           
            #endregion
        }


        /// <summary>
        /// Transforms EDRM relationships to EV's document relationships (list)
        /// IMPORTANT: This function has dependency TransformDocumentsAndRelationships. It can't be called unless the other function was run.
        /// If called without the first call, EV's document IDs are not updated in relationships. It will remain EDRM document ID, which can be duplicated. 
        /// </summary>
        /// <param name="collectionID"> Dataset collection ID </param>
        /// <returns> list of relationship entities </returns>
        protected virtual List<RelationshipBEO> TransformEDRMRelationshipsToDocumentRelationships(string collectionID)
        {
            List<RelationshipBEO> relationships = this.relationships.Select(eDRMRelationship => new RelationshipBEO()
                                                                                                {
                                                                                                    CollectionId = collectionID,
                                                                                                    ParentDocId = eDRMRelationship.ParentDocID,
                                                                                                    ChildDocumentId = eDRMRelationship.ChildDocID,
                                                                                                    Type = eDRMRelationship.Type
                                                                                                }).ToList();

            // Set logic - Select all parents. All of these can't be family IDs as a parent could be child of some other document
            IEnumerable<string> AllParents = relationships.Select(p => p.ParentDocId);

            // Parents EXCEPT Child documents gives ONLY PARENT and ONLY CHILD documents
            // INTERSECT with ALL PARENTS give all TOP LEVEL PARENTS
            IEnumerable<string> TopLevelParents = (AllParents).Except(relationships.Select(p => p.ChildDocumentId)).Intersect(AllParents);

            // Set family IDs for children of top level parents.
            foreach (string TopLevelParent in TopLevelParents)
            {
                SetFamily(TopLevelParent, TopLevelParent, relationships);
            }

            return relationships;
        }

        /// <summary>
        /// Sets family ID for relationship BEO, calls the function recursively for children in the relationship
        /// </summary>
        /// <param name="parentID"> Parent document Id for which family ID need to be set</param>
        /// <param name="familyID"></param>
        /// <param name="relationships"></param>
        protected virtual void SetFamily(string parentID, string familyID, List<RelationshipBEO> relationships)
        {
            IEnumerable<RelationshipBEO> relationshipList = from RelationshipBEO irelationship in relationships
                                                            where irelationship.ParentDocId.Equals(parentID)
                                                            select irelationship;

            foreach (RelationshipBEO relationship in relationshipList.Where(relationship => relationship != null))
            {
                relationship.FamilyDocumentId = familyID;
                SetFamily(relationship.ChildDocumentId, familyID, relationships);
            }
        }


        protected virtual string GetDocumentId(DocumentEntity edrmDocument)
        {
            return Guid.NewGuid().ToString().Replace("-", "").ToUpper();

        }

        /// <summary>
        /// Creates file URI for specified file in EDRM document.
        /// Handles 1) relative path from EDRM location, 2) file at EDRM location and 3) absolute path the file 
        /// </summary>
        /// <param name="fileLocation"> directory in which file exists </param>
        /// <param name="fileName"> file name </param>
        /// <returns> Complete file URI </returns>
        private string CreateFileURI(string fileLocation, string fileName)
        {
            // file at EDRM location
            if (string.IsNullOrEmpty(fileLocation))
            {
                return sourcePath + @"\" + fileName;
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

                    return fileLocation + fileName;
                }
                else // relative path to the file from EDRM location
                {
                    // does last character of file location have \. if not add it.
                    if (!fileLocation.Substring(fileLocation.Length - 1, 1).Equals(@"\")) fileLocation = fileLocation + @"\";

                    return sourcePath + @"\" + fileLocation + fileName;
                }
            }
        }


        #endregion
    }
}
