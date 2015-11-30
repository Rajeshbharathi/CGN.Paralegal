#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="LoadFileDocumentManager.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil Paramathma</author>
//      <description>
//          Class file for holding document detail extraction methods
//      </description>
//      <changelog>//          
//          <date value="14-Feb-2012">Fix for bug 96652</date>
//          <date value="16-Feb-2012">Fix for bug 95511</date>
//          <date value="23-Feb-2012">Fix for bug 96718,96719,96721</date>
//          <date value="01-Mar-2012">Fix for bug 96543</date>
//          <date value="03/02/2012">Bug fix 96543 , Mismatch field issue fix</date>
//          <date value="11/June/2012">Fix for devbug 102132,102133</date>
//          <date value="14/June/2012">Fix for devbug 102478</date>
//          <date value="07/17/2013">CNEV 2.2.1 - CR005 Implementation : babugx</date>
//          <date value="03/25/2014">ADm-Admin-008 buddy bug 166800 fix</date>
//          <date value="05/26/2014">Bug Fix # 168718 </date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.TraceServices;

namespace LexisNexis.Evolution.Worker
{
    using System.Globalization;

    using LexisNexis.Evolution.Overdrive;
    using LexisNexis.Evolution.Infrastructure;

    public class LoadFileDocumentManager
    {

        private ImportBEO m_JobParameter;
        private char m_ColumnDelimiter;
        private char m_QuoteCharacter;
        private char m_FieldRowDelimiter;
        private string m_ConcordanceFieldSplitter;
        private string m_ThreadingConstraint;
        private Uri m_SourceFile;
        private string m_DatasetPath;
        private DatasetBEO m_Dataset;
        private string m_JobRunId;
        private string m_WorkerInstanceId;
        private string m_ImportDescription;
        private int _JobId;

        public LoadFileDocumentManager(ImportBEO jobParameter, string threadingString, string datasetPath, DatasetBEO dataset, string jobRunId, string workerInstanceId,int jobId)
        {
            m_JobParameter = jobParameter;
            m_ColumnDelimiter = Convert.ToChar(m_JobParameter.LoadFile.ColumnDelimiter);
            m_QuoteCharacter = Convert.ToChar(m_JobParameter.LoadFile.QuoteCharacter);
            m_FieldRowDelimiter = Convert.ToChar(m_JobParameter.LoadFile.NewlineDelimiter);
            m_SourceFile = new Uri(m_JobParameter.Locations.First());
            m_ThreadingConstraint = threadingString;
            m_DatasetPath = datasetPath;
            m_Dataset = dataset;
            m_ConcordanceFieldSplitter = Constants.ConcordanceFieldSplitter;
            m_JobRunId = jobRunId;
            m_WorkerInstanceId = workerInstanceId;
            m_ImportDescription = jobParameter.ImportDetail;
            _JobId = jobId;
        }

        /// <summary>
        /// Method to get the documents from loadfile
        /// </summary>
        /// <param name="correlationId">Correlation id</param>
        /// <param name="recordText">record text</param>
        /// <param name="documentCtrlNbr">document control number</param>
        /// <param name="imageFileList">imagefile list</param>
        /// <param name="textFileList">text file list</param>
        /// <param name="recordParserLog">recordParser Log</param>
        /// <returns>list of DocumentDetail</returns>
        public List<DocumentDetail> GetDocuments(string correlationId, string recordText, string documentCtrlNbr,
            List<string> textFileList, out JobWorkerLog<LoadFileDocumentParserLogInfo> recordParserLog)
        {
            recordText.ShouldNotBe(null);
            List<DocumentDetail> documentDetailList = new List<DocumentDetail>();
            recordParserLog = null;
            RVWDocumentBEO document = null;
            string missingNativeFile = null;
            List<string> missingImageFiles = new List<string>();
            bool isMissingContent = false;
            List<string> missingContentFiles = new List<string>();
            List<string> misMatchedFields = new List<string>();
            List<string> misMatchedFieldsMessage = new List<string>();
            Int32 importedImagesCount = 0;
            #region Parse Record Text
            var recordTokenizer = new RecordTokenizer(m_ColumnDelimiter, m_QuoteCharacter);
            var fields = recordTokenizer.ParseRecord(recordText);
            #endregion

            List<RVWDocumentFieldBEO> matchingKeyField = null; //For Overlay
            //Get document               
            document = ConsturctDocument(correlationId, fields, textFileList,
                 ref matchingKeyField, out missingNativeFile, missingImageFiles, out isMissingContent,
                 missingContentFiles, misMatchedFields, misMatchedFieldsMessage, out importedImagesCount);

            if (m_JobParameter.IsAppend)
            {
                // Assign DCN
                document.DocumentControlNumber = documentCtrlNbr;

                //1) Construct Native Set
                var nativeSetDocument = GetDocumentForNativeSet(document);
                var doc = new DocumentDetail();
                doc.CorrelationId = correlationId;
                doc.docType = DocumentsetType.NativeSet;
                doc.document = nativeSetDocument;
                doc.ConversationIndex = document.ConversationIndex;
                doc.IsNewDocument = true;
                //Add Native Document
                documentDetailList.Add(doc);

                //2) Construct Image Set                       
                if (m_JobParameter.IsImportImages && !string.IsNullOrEmpty(m_JobParameter.ImageSetId))
                {
                    var imageSetDocument = GetDocumentForImageSet(document, m_JobParameter.ImageSetId);
                    imageSetDocument.IsImageFilesNotAssociated = !(importedImagesCount > 0 || missingImageFiles.Any());
                    var docImg = new DocumentDetail();
                    docImg.CorrelationId = correlationId;
                    docImg.docType = DocumentsetType.ImageSet;
                    docImg.document = imageSetDocument;
                    docImg.IsNewDocument = true;
                    //Add Image Document
                    documentDetailList.Add(docImg);
                }
            }
            else
            {
                //Send original document to Search worker
                var doc = new DocumentDetail();
                doc.document = document;
                doc.ConversationIndex = document.ConversationIndex;
                #region Create a unique file name for extracted content file

                doc.document.DocumentBinary.FileList.ForEach(x => x.Path = (x.Type.ToLower() == Constants.TEXT_FILE_TYPE.ToLower()) ? string.Format("{0}?id={1}", x.Path, Guid.NewGuid().ToString()) : x.Path);

                #endregion
                doc.CorrelationId = correlationId;
                doc.OverlayMatchingField = matchingKeyField;
                doc.document.IsImageFilesNotAssociated = !(importedImagesCount > 0 || missingImageFiles.Any());
                documentDetailList.Add(doc);
            }

            //3) Construct Log
            #region Log
            var imageMappingKey=string.Empty;
            if (m_JobParameter.IsImportImages && m_JobParameter.LoadFile.ImageFile != null)
            {
                imageMappingKey = fields[m_JobParameter.LoadFile.ImageFile.ImageMatchingFieldId];
            }
            recordParserLog = ConstructLog(correlationId, true, document.DocumentId,
                missingNativeFile, missingImageFiles, isMissingContent, missingContentFiles, importedImagesCount, misMatchedFields, documentCtrlNbr, document.CrossReferenceFieldValue, misMatchedFieldsMessage, imageMappingKey);
            #endregion

            var firstDoc = documentDetailList.FirstOrDefault();
            if (firstDoc != null)
            {
                firstDoc.document.ImportMessage = recordParserLog.LogInfo.Message;
            }

            return documentDetailList;
        }



        public RVWDocumentBEO ConsturctDocument(string correlationId, string[] fields,
            List<string> textFileList,
            ref List<RVWDocumentFieldBEO> matchingKeyField,
            out string missingNativeFile, List<string> missingImageFiles,
            out bool isMissingContent, List<string> missingContentFiles,
            List<string> misMatchedFields, List<string> misMatchedFieldsMessage, out int importedImagesCount)
        {
            var document = new RVWDocumentBEO();
            missingNativeFile = null;
            isMissingContent = false;
            document.CollectionId = m_JobParameter.CollectionId;
            document.MatterId = m_JobParameter.MatterId;
            document.CreatedBy = m_JobParameter.CreatedBy;
            document.ImportDescription = m_ImportDescription;
            importedImagesCount = 0;

            document.DocumentId = GetDocumentId();
            SetLoadFileRelationShipInformation(fields, m_FieldRowDelimiter, document);

            if (m_JobParameter.FamilyRelations != null && m_JobParameter.IsMapEmailThread)
            {
                document.ConversationIndex = GetConversionIndex(fields, m_FieldRowDelimiter);
            }

            if (m_JobParameter.LoadFile != null && m_JobParameter.LoadFile.CrossReferenceField > -1)
            {
                document.CrossReferenceFieldValue = GetCrossReferecneField(fields, m_FieldRowDelimiter);
            }
            if (m_JobParameter.LoadFile != null && m_JobParameter.LoadFile.ContentFile != null)
            {
                if (!m_Dataset.DatasetFieldList.Any(f => f.FieldType.DataTypeId == 2000 && f.Name == m_JobParameter.LoadFile.ContentFile.FieldNameToPopulateText))
                {
                    document.CustomFieldToPopulateText = m_JobParameter.LoadFile.ContentFile.FieldNameToPopulateText;
                }
            }
            #region Native File
            string nativeFilePath = string.Empty;
            if ((m_JobParameter.IsImportNativeFiles) && (m_JobParameter.LoadFile.NativeFile.LoadfileNativeField != string.Empty))
            {
                if (fields.Count() > Convert.ToInt32(m_JobParameter.LoadFile.NativeFile.LoadfileNativeField))
                    nativeFilePath = fields[Convert.ToInt32(m_JobParameter.LoadFile.NativeFile.LoadfileNativeField)];
                if (CheckValidFilePathFormat(nativeFilePath))
                {

                    if (m_JobParameter.LoadFile.NativeFile.NativePathSubstitution != null)
                    {
                        FilePathSubstitutionBEO nativePathSubstitution = m_JobParameter.LoadFile.NativeFile.NativePathSubstitution;

                        if (nativePathSubstitution.StringToMatch != string.Empty && nativePathSubstitution.StringToReplace != string.Empty)
                        {
                            nativeFilePath = PathSubstituion(nativeFilePath, nativePathSubstitution);
                        }
                        else
                        {
                            //Construct Absolute Path for Relative Path
                            nativeFilePath = ConstructAbsolutePath(nativeFilePath, m_SourceFile.OriginalString);
                        }
                    }
                    else
                    {
                        //Construct Absolute Path for Relative Path
                        nativeFilePath = ConstructAbsolutePath(nativeFilePath, m_SourceFile.OriginalString);
                    }
                    document.MimeType = GetMimeType(Path.GetExtension(nativeFilePath).Replace(".", "")); // Remove MimeType
                    document.FileName = Path.GetFileNameWithoutExtension(nativeFilePath);
                    document.NativeFilePath = nativeFilePath;

                    #region Update native file information in document binary object as well...
                    if (document.DocumentBinary == null) { document.DocumentBinary = new RVWDocumentBinaryBEO(); }
                    RVWExternalFileBEO externalFile = new RVWExternalFileBEO();
                    externalFile.Type = Constants.NATIVE_FILE_TYPE;
                    externalFile.Path = document.NativeFilePath;
                    document.DocumentBinary.FileList.Add(externalFile);
                    #endregion Update native file information in document binary object as well...

                    document.FileExtension = Path.GetExtension(nativeFilePath);
                    if (File.Exists(nativeFilePath))
                    {
                        //Calculating size of file in KB
                        FileInfo fileInfo = new FileInfo(nativeFilePath);
                        document.FileSize = (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);

                        document.MD5HashValue = DocumentHashHelper.GetMD5HashValue(nativeFilePath);
                        document.SHAHashValue = DocumentHashHelper.GetSHAHashValue(nativeFilePath);
                    }
                    else //Missing Native File
                    {
                        missingNativeFile = nativeFilePath;
                    }
                }
                else //Missing Native File
                {
                    missingNativeFile = nativeFilePath;
                }
            }
            #endregion

            #region Image File
            if (m_JobParameter.IsImportImages)
            {
                if (m_JobParameter.LoadFile.ImageFile != null)
                {
                    
                    #region "Image File Type"
                    string fileType = (!string.IsNullOrEmpty(m_JobParameter.LoadFile.ImageFile.ImageType.ToString())) ?
                        m_JobParameter.LoadFile.ImageFile.ImageType.ToString() : string.Empty;
                    #endregion
                
                    var lsImageFilePath = GetImageFilePaths(fields, _JobId);
                   
                    if (document.DocumentBinary == null) { document.DocumentBinary = new RVWDocumentBinaryBEO(); }
                    if (lsImageFilePath.Any())
                    {
                        //Int64 ImportedImagesCount = 0;
                        bool isAllFiles = false;
                        isAllFiles = (fileType.ToLower().Equals(Constants.ALL_FILE_TYPE.ToLower())) ? true : false;
                        foreach (string path in lsImageFilePath)
                        {
                            RVWExternalFileBEO textFile = null;
                            if (CheckValidFilePathFormat(path))
                            {
                                if ((isAllFiles) || (fileType.ToLower().Contains(Path.GetExtension(path).Replace(Constants.STR_DOT, string.Empty).ToLower())))  // Allows the user to select the type of images to import
                                {
                                    textFile = new RVWExternalFileBEO
                                    {
                                        Type = Constants.IMAGE_FILE_TYPE,
                                        Path = path
                                    };
                                    document.DocumentBinary.FileList.Add(textFile);
                                    #region Missing Images
                                    if (!File.Exists(path)) //Missing Image File
                                    {
                                        missingImageFiles.Add(path);
                                    }
                                    #endregion
                                }
                            }
                            else //Missing Image File
                            {
                                missingImageFiles.Add(path);
                            }
                        }
                        importedImagesCount = lsImageFilePath.Count - missingImageFiles.Count;
                    }
                    
                }
            }
            #endregion

            #region Text File (Content File)
            if (m_JobParameter.LoadFile.ContentFile != null)
            {
                string txtFilePath = string.Empty;
                string contentFilePath = string.Empty;
                if (m_JobParameter.LoadFile.ContentFile.TextExtractionOption != LoadFileTextExtractionOption.NoTextImport)
                {
                    if (m_JobParameter.LoadFile.ContentFile.TextExtractionOption == LoadFileTextExtractionOption.LoadFileField)
                    {
                        if (fields.Count() > Convert.ToInt32(m_JobParameter.LoadFile.ContentFile.LoadFileContentField))
                            txtFilePath = (fields[Convert.ToInt32(m_JobParameter.LoadFile.ContentFile.LoadFileContentField)] != string.Empty) ? fields[Convert.ToInt32(m_JobParameter.LoadFile.ContentFile.LoadFileContentField)] : string.Empty;
                        if (!string.IsNullOrEmpty(txtFilePath) && txtFilePath.IndexOfAny(Path.GetInvalidPathChars()) == -1) //Check Valid file path
                        {
                            if (CheckValidFilePathFormat(txtFilePath)) //Check its a file or not 
                            {
                                if (!string.IsNullOrEmpty(m_JobParameter.LoadFile.ContentFile.FolderLocation))
                                {
                                    txtFilePath = m_JobParameter.LoadFile.ContentFile.FolderLocation + Constants.BackSlash + txtFilePath;
                                }

                                if (m_JobParameter.LoadFile.ContentFile.TextFilePathSubstitution != null)
                                {
                                    FilePathSubstitutionBEO txtFilePathSubstitution = m_JobParameter.LoadFile.ContentFile.TextFilePathSubstitution;
                                    contentFilePath = PathSubstituion(txtFilePath, txtFilePathSubstitution);
                                }
                                else
                                {
                                    //Construct Absolute Path for Relative Path
                                    contentFilePath = ConstructAbsolutePath(txtFilePath, m_SourceFile.OriginalString);
                                }
                            }
                            else
                            {
                                if (missingContentFiles != null)
                                {
                                    missingContentFiles.Add(txtFilePath);
                                }
                                isMissingContent = true;
                            }
                        }
                        else
                        {
                            if (missingContentFiles != null)
                            {
                                missingContentFiles.Add(txtFilePath);
                            }
                            isMissingContent = true;
                        }
                    }
                    else if (m_JobParameter.LoadFile.ContentFile.TextExtractionOption == LoadFileTextExtractionOption.HelperFile)
                    {
                        if (textFileList != null && textFileList.Count > 0)
                        {
                            if (textFileList.Count == 1)
                                contentFilePath = textFileList.First();
                            else
                            {
                                //Create single text file after fetch content from multiple text file       
                                StringBuilder sbFilePath = new StringBuilder();
                                if (m_DatasetPath.EndsWith(Constants.BackSlash))
                                {
                                    sbFilePath.Append(m_DatasetPath);
                                }
                                else
                                {
                                    sbFilePath.Append(m_DatasetPath);
                                    sbFilePath.Append(Constants.BackSlash);
                                }
                                sbFilePath.Append(Guid.NewGuid().ToString().Replace("-", "").ToUpper());
                                sbFilePath.Append(DateTime.UtcNow.ToString(Constants.DateFormat));
                                sbFilePath.Append(Constants.TextFileExtension);
                                string filePath = sbFilePath.ToString();
                                CreateSingleContentFile(textFileList, filePath, missingContentFiles);
                                contentFilePath = filePath;
                                if (missingContentFiles != null && missingContentFiles.Count > 0)  //Capture log for missing content file.
                                {
                                    isMissingContent = true;
                                }
                            }
                        }
                    }
                    else if (m_JobParameter.LoadFile.ContentFile.TextExtractionOption == LoadFileTextExtractionOption.BodyTextField)
                    {
                        if (null != textFileList && textFileList.Any())
                        {
                            contentFilePath = textFileList[0];
                        }
                    }
                    else if (m_JobParameter.LoadFile.ContentFile.TextExtractionOption == LoadFileTextExtractionOption.TextFromFolderLocation)
                    {
                        string filename = string.Empty;
                        string fieldValueToSearchFile = string.Empty;
                        if (m_JobParameter.LoadFile != null && m_JobParameter.LoadFile.ContentFile.MatchingFieldIdForFileName > -1)
                        {
                            fieldValueToSearchFile = GetFieldValues(fields, m_FieldRowDelimiter, m_JobParameter.LoadFile.ContentFile.MatchingFieldIdForFileName);
                        }
                        if (!string.IsNullOrEmpty(m_JobParameter.LoadFile.ContentFile.FolderLocation) && !string.IsNullOrEmpty(fieldValueToSearchFile))
                        {
                            filename = string.Format("{0}.txt", fieldValueToSearchFile);
                            var files = Directory.GetFiles(m_JobParameter.LoadFile.ContentFile.FolderLocation, filename, SearchOption.AllDirectories);
                            if (files.Any())
                            {
                                contentFilePath = files.LastOrDefault();
                            }
                            else
                            {
                                if (missingContentFiles != null && !string.IsNullOrEmpty(filename))
                                {
                                    missingContentFiles.Add(filename);
                                }
                                isMissingContent = true;
                            }
                        }
                        else
                        {
                            isMissingContent = true;
                        }
                    }
                    // Checks in below statement 1) null check on "file path" and 2) are there any external files
                    if (document.DocumentBinary == null) { document.DocumentBinary = new RVWDocumentBinaryBEO(); }
                    if (!string.IsNullOrEmpty(contentFilePath))
                    {
                        if (CheckValidFilePathFormat(contentFilePath) && File.Exists(contentFilePath))
                        {
                            RVWExternalFileBEO textFile = new RVWExternalFileBEO
                            {
                                Type = Constants.TEXT_FILE_TYPE,
                                Path = contentFilePath
                            };
                            document.DocumentBinary.FileList.Add(textFile);
                        }
                        else
                        {
                            missingContentFiles.Add(contentFilePath);
                            isMissingContent = true;
                        }
                    }
                }
            }
            #endregion

            #region Field Mapping
            if (m_JobParameter.FieldMapping != null)
            {
                List<FieldMapBEO> mappedFields = m_JobParameter.FieldMapping;
                foreach (FieldMapBEO mappedField in mappedFields)
                {
                    string fieldValue = string.Empty;
                    FieldBEO field = m_Dataset.DatasetFieldList.FirstOrDefault(f => f.ID.Equals(mappedField.DatasetFieldID));
                    if (null == field)
                    {
                        continue;
                    }
                    var dataTypeId = (field.FieldType != null) ? field.FieldType.DataTypeId : 0;

                    if (fields.Count() > mappedField.SourceFieldID && fields.Length > mappedField.SourceFieldID)
                        fieldValue = (fields[mappedField.SourceFieldID] != string.Empty) ? fields[mappedField.SourceFieldID].Replace(m_FieldRowDelimiter.ToString(), m_ConcordanceFieldSplitter) : string.Empty;

                    // To maintain Equiset value as unique within a collection. Will append unique identifier with Equiset Value
                    // E.g : Equiset value in .DAT file :126
                    //       Imported in EV : UniqueIdentifier_126
                    if (!string.IsNullOrEmpty(mappedField.DatasetFieldName) && mappedField.DatasetFieldName.ToLower() == EVSystemFields.ND_FamilyID.ToLower() && !string.IsNullOrEmpty(fieldValue))
                    {
                        fieldValue = string.Format("{0}_{1}", m_ThreadingConstraint, fieldValue);
                    }

                    if (dataTypeId == Constants.DateFieldTypeId)
                    {
                        DateTime dateFieldValue;
                        DateTime.TryParse(fieldValue, out dateFieldValue);
                        if (dateFieldValue == DateTime.MinValue || dateFieldValue == DateTime.MaxValue)
                        {
                            misMatchedFields.Add(string.Format(Constants.MisMatchedToWrongData, field.Name));
                            misMatchedFieldsMessage.Add(string.Format(Constants.MsgMismatchedFile, field.Name));
                        }
                    }

                    // Create a Field Business Entity for each mapped field
                    RVWDocumentFieldBEO fieldBeo = new RVWDocumentFieldBEO()
                    {
                        // set required properties / field data
                        FieldId = mappedField.DatasetFieldID,
                        FieldName = mappedField.DatasetFieldName,
                        FieldValue = fieldValue,
                        FieldType = new FieldDataTypeBusinessEntity() { DataTypeId = dataTypeId }
                    };

                    // Add Field to the document
                    document.FieldList.Add(fieldBeo);

                } // End of loop through fields in a document.   

            }
            #endregion

            #region Overlay Matching Condition
            // Assign Field value.
            if (!m_JobParameter.IsAppend && m_JobParameter.OverlayKeys != null)
            {
                List<FieldMapBEO> overlayMatchingKeyFields = m_JobParameter.OverlayKeys;
                matchingKeyField = new List<RVWDocumentFieldBEO>();
                foreach (FieldMapBEO mappedField in overlayMatchingKeyFields)
                {
                    string fieldValue = string.Empty;
                    if (fields.Count() > mappedField.SourceFieldID)
                        fieldValue = (fields[mappedField.SourceFieldID] != string.Empty) ? fields[mappedField.SourceFieldID] : string.Empty;
                    // Create a Field Business Entity for each mapped field
                    RVWDocumentFieldBEO fieldBEO = new RVWDocumentFieldBEO()
                    {
                        // set required properties / field data
                        FieldId = mappedField.DatasetFieldID,
                        FieldName = mappedField.DatasetFieldName,
                        FieldValue = fieldValue

                    };
                    // Add Field to the document
                    matchingKeyField.Add(fieldBEO);

                }
            }

            #endregion
            return document;
        }

        /// <summary>
        /// Get unique document id for each records
        /// First part is unique id for a import job
        /// Scond part is unique for each record
        /// </summary>
        /// <returns></returns>
        private string GetDocumentId()
        {
            var documentID = m_ThreadingConstraint + Guid.NewGuid().ToString().Replace("-", "");
            return documentID.ToUpper();
        }

        #region Family Relation
        private string GetConversionIndex(string[] fields, char fieldRowDelimiter)
        {
            if (m_JobParameter.FamilyRelations != null && m_JobParameter.IsMapEmailThread && !string.IsNullOrEmpty(m_JobParameter.FamilyRelations.ConversationIndexField))
            {
                if (fields.Count() > m_JobParameter.FamilyRelations.ConversationIndexFieldId)
                {
                    return GetFieldValue(fields[m_JobParameter.FamilyRelations.ConversationIndexFieldId],
                            fieldRowDelimiter, false);
                }
            }
            return null;
        }

        private string GetCrossReferecneField(string[] fields, char fieldRowDelimiter)
        {
            if (m_JobParameter.LoadFile != null && m_JobParameter.LoadFile.CrossReferenceField > -1)
            {
                if (fields.Count() > m_JobParameter.LoadFile.CrossReferenceField)
                {
                    return GetFieldValue(fields[m_JobParameter.LoadFile.CrossReferenceField], fieldRowDelimiter, false);
                }
            }
            return null;
        }

        private string GetFieldValues(string[] fields, char fieldRowDelimiter, int fieldId)
        {
            if (fieldId > -1 && fields.Count() > fieldId)
            {
                return GetFieldValue(fields[fieldId], fieldRowDelimiter, false);
            }
            return null;
        }


        /// <summary>
        /// Set Load file realtionship field values for each document
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldRowDelimiter"></param>
        /// <param name="document"></param>
        private void SetLoadFileRelationShipInformation(string[] fields, char fieldRowDelimiter,
            RVWDocumentBEO document)
        {
            if (m_JobParameter.FamilyRelations != null &&
                m_JobParameter.IsImportFamilyRelations)
            {
                int childDocumentFieldId = m_JobParameter.FamilyRelations.DocId;
                int parentDocumentFieldId = m_JobParameter.FamilyRelations.ParentDocId;
                bool isParentIdRange = m_JobParameter.FamilyRelations.IsParentDocIDFormatRange;
                bool isDocumentIdRange = m_JobParameter.FamilyRelations.IsDocIDFormatRange;
                document.EVInsertSystemRelationShipFields = true;
                if (fields.Length > childDocumentFieldId && fields.Length > parentDocumentFieldId)
                {
                    //Document Id
                    string docid = string.Empty;
                    if (fields.Count() > childDocumentFieldId)
                    {
                        docid = GetFieldValue(fields[childDocumentFieldId], fieldRowDelimiter,
                                              isDocumentIdRange);
                    }
                    document.EVLoadFileDocumentId = docid;

                    //Parent Id
                    string parentId = string.Empty;
                    if (fields.Count() > parentDocumentFieldId)
                    {
                        parentId = GetFieldValue(fields[parentDocumentFieldId], fieldRowDelimiter,
                                        isParentIdRange);
                    }
                    document.EVLoadFileParentId = parentId;

                    //Tracer.Warning("OLD: OriginalDocumentId = {0}, OriginalParentId = {1}",
                    //    document.EVLoadFileDocumentId, document.EVLoadFileParentId);
                }
            }
        }


        /// <summary>
        /// Gets the field value, verifies if it's a range and extracts document id
        /// </summary>
        /// <param name="inputFieldValue">The input field value.</param>
        /// <param name="isRange">if set to <c>true</c> [is range].</param>
        /// <returns></returns>
        private string GetFieldValue(string inputFieldValue, char fieldRowDelimiter, bool isRange)
        {
            if (isRange)
            {
                // if input field value is range, split to half and use the first part
                // Eg. range is expected to be doc0001 - doc0005 format. so split and pick the first half.
                return inputFieldValue.Replace(fieldRowDelimiter.ToString(), m_ConcordanceFieldSplitter).Substring(0, ((int)Math.Floor((double)(inputFieldValue.Length / 2)))).Trim();
            }
            else
                return inputFieldValue.Replace(fieldRowDelimiter.ToString(), m_ConcordanceFieldSplitter);
        }
        #endregion

        /// <summary>
        /// Path Substitution
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="txtFilePathSubstitution"></param>
        /// <returns></returns>
        private string PathSubstituion(string filePath, FilePathSubstitutionBEO txtFilePathSubstitution)
        {
            string modifiedPath = (!string.IsNullOrEmpty(Path.GetDirectoryName(filePath))) ? Path.GetDirectoryName(filePath).Replace(txtFilePathSubstitution.StringToMatch, txtFilePathSubstitution.StringToReplace) : string.Empty;
            return (modifiedPath != string.Empty) ? (modifiedPath + Constants.BackSlash + Path.GetFileName(filePath)) : filePath;
        }


        /// <summary>
        /// Construct Absolute Path for Realtive 
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="baseFile">Base File : Source File (DAT/CSV/TXT..) OR Helper File (OPT/LOG)</param>
        /// <returns></returns>
        public string ConstructAbsolutePath(string path, string baseFile)
        {
            string fullPath;
            if (path.IndexOf(@"\\") == 0 || path.Contains(@":\"))
            {
                //Absolute path
                fullPath = path;
            }
            else
            {
                //Relative path
                path = path.IndexOf(@"\") != 0 ? @"\" + path : path;
                fullPath = Path.GetDirectoryName(baseFile) + path;
            }
            return fullPath;
        }


        /// <summary>
        /// Method to get the file Mime Type from file extension
        /// </summary>
        /// <param name="fileExt">The file ext.</param>
        /// <returns></returns>     
        public static string GetMimeType(string fileExt)
        {
            string mimeType = string.Empty;
            FileMimeTypeHelper fileTypeHelper = new FileMimeTypeHelper();
            mimeType = fileTypeHelper.GetMimeType(fileExt);
            return mimeType;
        }


        /// <summary>
        /// Clear image file information in DocumentBEO 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private RVWDocumentBEO GetDocumentForNativeSet(RVWDocumentBEO document)
        {
            RVWDocumentBEO nativeSetDocument = CopyDocumentObject(document, true);
            List<RVWExternalFileBEO> fileList = nativeSetDocument.DocumentBinary.FileList.Where(x => x.Type.ToLower() != Constants.IMAGE_FILE_TYPE.ToLower()).ToList();
            nativeSetDocument.DocumentBinary.FileList.Clear();
            if (fileList.Any())
            {
                fileList.ForEach(x => nativeSetDocument.DocumentBinary.FileList.Add(x));
                nativeSetDocument.DocumentBinary.FileList.ForEach(x => x.Path = (x.Type == Constants.TEXT_FILE_TYPE) ? string.Format("{0}?id={1}", x.Path, Guid.NewGuid().ToString()) : x.Path);
            }
            if (document.DocumentBinary != null)
            {
                if (!string.IsNullOrEmpty(document.DocumentBinary.Content))  //Assign Content Value
                {
                    nativeSetDocument.DocumentBinary.Content = document.DocumentBinary.Content;
                }
            }
            return nativeSetDocument;
        }
        /// <summary>
        /// Copy Document Object
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private RVWDocumentBEO CopyDocumentObject(RVWDocumentBEO document, bool IsNativeDoc)
        {
            var documentsetDocument = new RVWDocumentBEO();
            documentsetDocument.CollectionId = document.CollectionId;
            documentsetDocument.MatterId = document.MatterId;
            documentsetDocument.CreatedBy = document.CreatedBy;
            documentsetDocument.Id = document.Id;
            documentsetDocument.DocumentId = document.DocumentId;
            documentsetDocument.MimeType = document.MimeType;
            documentsetDocument.FileName = document.FileName;
            documentsetDocument.NativeFilePath = document.NativeFilePath;
            documentsetDocument.FileExtension = document.FileExtension;
            documentsetDocument.FileSize = document.FileSize;
            documentsetDocument.DocumentControlNumber = document.DocumentControlNumber;
            documentsetDocument.ImportDescription = document.ImportDescription;
            documentsetDocument.CustomFieldToPopulateText = document.CustomFieldToPopulateText;
            if (IsNativeDoc)
            {
                documentsetDocument.EVLoadFileDocumentId = document.EVLoadFileDocumentId;
                documentsetDocument.EVLoadFileParentId = document.EVLoadFileParentId;
                documentsetDocument.EVInsertSystemRelationShipFields = document.EVInsertSystemRelationShipFields;
            }

            if (!string.IsNullOrEmpty(document.MD5HashValue))
                documentsetDocument.MD5HashValue = document.MD5HashValue;
            if (!string.IsNullOrEmpty(document.SHAHashValue))
                documentsetDocument.SHAHashValue = document.SHAHashValue;
            if (!string.IsNullOrEmpty(document.ImportMessage))
                documentsetDocument.ImportMessage = document.ImportMessage;
            if (document.FieldList != null && document.FieldList.Count > 0)
            {
                document.FieldList.ForEach(x => documentsetDocument.FieldList.Add(x));
            }
            documentsetDocument.DocumentBinary = new RVWDocumentBinaryBEO();
            if (document.DocumentBinary != null)
            {
                if (document.DocumentBinary.FileList.Count > 0) //Add File List 
                {
                    document.DocumentBinary.FileList.ForEach(x => documentsetDocument.DocumentBinary.FileList.Add(x));
                }
            }
            if (!string.IsNullOrEmpty(document.CrossReferenceFieldValue))
            {
                documentsetDocument.CrossReferenceFieldValue = document.CrossReferenceFieldValue;
            }
            return documentsetDocument;
        }

        /// <summary>
        ///  Clear except image file information in DocumentBEO 
        /// </summary>
        /// <param name="document">document</param>
        /// <param name="collectionId">CollectionId</param>
        /// <returns>RVWDocumentBEO</returns>
        private RVWDocumentBEO GetDocumentForImageSet(RVWDocumentBEO document, string collectionId)
        {
            document.ShouldNotBe(null);
            collectionId.ShouldNotBe(null);
            var imageSetDocument = CopyDocumentObject(document, false);
            int imageSize = 0;
            if (imageSetDocument.DocumentBinary.FileList != null && imageSetDocument.DocumentBinary.FileList.Count > 0)
            {
                List<RVWExternalFileBEO> imagefileList = imageSetDocument.DocumentBinary.FileList.Where(x => x.Type.ToLower().Equals(Constants.IMAGE_FILE_TYPE.ToLower())).ToList();
                imageSetDocument.DocumentBinary.FileList.Clear();
                imagefileList.ForEach(x => imageSetDocument.DocumentBinary.FileList.Add(x));
                foreach (var docFile in imageSetDocument.DocumentBinary.FileList)
                {
                    if (File.Exists(docFile.Path))
                    {
                        //Calculating size of file in KB
                        FileInfo fileInfo = new FileInfo(docFile.Path);
                        if (fileInfo != null)
                        {
                            imageSize += (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);
                        }
                    }
                }
            }
            //Set Imageset Collection Id.
            imageSetDocument.CollectionId = collectionId;
            //Clear Field Mapping
            imageSetDocument.FieldList.Clear();
            //Clear Native File Info
            imageSetDocument.MimeType = string.Empty;
            imageSetDocument.FileName = string.Empty;
            imageSetDocument.NativeFilePath = string.Empty;
            imageSetDocument.FileExtension = string.Empty;
            imageSetDocument.FileSize = imageSize;
            return imageSetDocument;
        }

        /// <summary>
        /// Create text file for content value.
        /// </summary>      
        private void CreateContentFile(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        /// <summary>
        /// Read content from multiple content files and write it into single file
        /// </summary>
        /// <param name="lsContentFile"></param>
        /// <param name="fileName"> </param>
        /// <param name="missingContentFiles"> </param>
        /// <returns></returns>
        private void CreateSingleContentFile(List<string> lsContentFile, string fileName, List<string> missingContentFiles)
        {
            StringBuilder sbContent = new StringBuilder();
            foreach (string path in lsContentFile)
            {
                if (File.Exists(path))
                {
                    sbContent.Append(File.ReadAllText(path));
                }
                else
                {
                    missingContentFiles.Add(path);
                }
            }

            string content = sbContent.ToString();
            if (String.IsNullOrWhiteSpace(content))
            {
                return;
            }

            CreateContentFile(fileName, content);
        }

        /// <summary>
        /// To check valid file path
        /// </summary>    
        private bool CheckValidFilePathFormat(string filePath)
        {
            bool isValidFile = false;
            try
            {
                isValidFile = Path.HasExtension(filePath);

            }
            catch (Exception)
            {
                Tracer.Error("Load File Record Parser: Invalid file path {0} for job run id {1}", filePath, m_JobRunId);
            }
            return isValidFile;
        }

        /// <summary>
        /// Get image file paths
        /// </summary>
        /// <param name="fields">Document fields</param>
        /// <param name="jobId">Job Id</param>
        private List<string> GetImageFilePaths(string[] fields, int jobId)
        {
            var lsImageFilePath = new List<string>();

            var imageMappingKey = fields[m_JobParameter.LoadFile.ImageFile.ImageMatchingFieldId];
            var paths = string.Empty;
            if (!string.IsNullOrEmpty(imageMappingKey))
            {
                paths = DocumentBO.GetLoadFileImagePath(m_JobParameter.MatterId, jobId, imageMappingKey);
            }
            if (string.IsNullOrEmpty(paths)) return lsImageFilePath;

            var imagePaths = paths.Split(',');

            if (!imagePaths.Any()) return lsImageFilePath;

            if (m_JobParameter.LoadFile.ImageFile.ImagePathSubstitution != null)
            {
                var imagePathSubstitution = m_JobParameter.LoadFile.ImageFile.ImagePathSubstitution;
                lsImageFilePath.AddRange(imagePaths.Select(path => PathSubstituion(path, imagePathSubstitution)));
            }
            else
            {
                // Construct Absolute Path for Relative Path                                
                lsImageFilePath.AddRange(
                    imagePaths.Select(
                        path => ConstructAbsolutePath(path, m_JobParameter.LoadFile.ImageFile.HelperFileName)));
            }
            return lsImageFilePath;

        }

        #region Log

        /// <summary>
        /// Construct Log Data
        /// </summary>       
        public JobWorkerLog<LoadFileDocumentParserLogInfo> ConstructLog(string correlationId, bool isInserted, string docId,
            string missingNativeFile, List<string> missingImageFiles, bool isMissingContent, List<string> missingContentFiles,
            Int32 importedImagesCount, List<string> misMatchedFields, string documentCtrlNbr, string crossReferenceFieldValue, List<string> misMatchedFieldsMessage,string imageMappingKey)
        {
            try
            {
                docId.ShouldNotBe(null);
               

                var recordParserLog = new JobWorkerLog<LoadFileDocumentParserLogInfo>();
                recordParserLog.JobRunId = (!string.IsNullOrEmpty(m_JobRunId)) ? Convert.ToInt64(m_JobRunId) : 0;
                recordParserLog.CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0;
                recordParserLog.WorkerInstanceId = m_WorkerInstanceId;
                recordParserLog.WorkerRoleType = Constants.LoadFileRecordParserWorkerRoleType;
                recordParserLog.Success = isInserted;
                recordParserLog.CreatedBy = m_JobParameter.CreatedBy;
                recordParserLog.IsMessage = false;
                recordParserLog.LogInfo = new LoadFileDocumentParserLogInfo();
                if (recordParserLog.LogInfo.Message == null)
                    recordParserLog.LogInfo.Message = string.Empty;
                if (isInserted)
                {
                    recordParserLog.LogInfo.Information = Constants.RecordParserSuccessMessage;
                    if (null != missingNativeFile || missingImageFiles.Count > 0 || isMissingContent)
                    {
                        //Changes based on Bug#98867 (If any part of record fails, then need to fail that record(document) )
                        recordParserLog.Success = false;
                        recordParserLog.LogInfo.Information = !string.IsNullOrEmpty(documentCtrlNbr) ? string.Format(Constants.MissingFileMessage, documentCtrlNbr) : Constants.MissingFiles;
                    }

                    if (null != missingNativeFile)
                    {
                        recordParserLog.LogInfo.Information += Constants.MissingNativeFileMessage + missingNativeFile;
                        recordParserLog.LogInfo.Message = recordParserLog.LogInfo.Message.Contains(Constants.MsgMissingNativeFiles) ? recordParserLog.LogInfo.Message : recordParserLog.LogInfo.Message + Constants.MsgMissingNativeFiles;
                        recordParserLog.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                    }

                    foreach (string missingImageFile in missingImageFiles)
                    {
                        if (!string.IsNullOrEmpty(imageMappingKey))
                        {
                            recordParserLog.LogInfo.Information += string.Format("{0} {1}", Constants.MissingImageForKey, imageMappingKey);
                            imageMappingKey = string.Empty;
                        }
                        recordParserLog.LogInfo.Information += Constants.MissingImageFileMessage + missingImageFile;
                        recordParserLog.LogInfo.Message = recordParserLog.LogInfo.Message.Contains(Constants.MsgMissingImage) ? recordParserLog.LogInfo.Message : recordParserLog.LogInfo.Message + Constants.MsgMissingImage;
                        recordParserLog.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                    }

                    foreach (string missingContentFile in missingContentFiles)
                    {
                        recordParserLog.LogInfo.Information += Constants.MissingContentFileMessage + missingContentFile;
                        recordParserLog.LogInfo.Message = recordParserLog.LogInfo.Message.Contains(Constants.MsgMissingContentFile) ? recordParserLog.LogInfo.Message : recordParserLog.LogInfo.Message + Constants.MsgMissingContentFile;
                        recordParserLog.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                    }

                    if (misMatchedFields != null && misMatchedFields.Count > 0 && !string.IsNullOrEmpty(documentCtrlNbr))
                    {
                        if (recordParserLog.Success)
                        {   //If document success then its come to warning message count otherwise document go to Error document count
                            recordParserLog.IsMessage = true;
                        }
                        recordParserLog.LogInfo.Information += string.Format(Constants.MisMatchedFieldMessage, documentCtrlNbr);
                        //recordParserLog.LogInfo.Message += string.Format(Constants.MsgMismatchedFile, documentCtrlNbr);
                        recordParserLog.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                        misMatchedFields.ForEach(x => recordParserLog.LogInfo.Information += x);
                        misMatchedFieldsMessage.ForEach(x => recordParserLog.LogInfo.Message = recordParserLog.LogInfo.Message.Contains(x) ? recordParserLog.LogInfo.Message : recordParserLog.LogInfo.Message + x);
                    }

                    recordParserLog.LogInfo.IsMissingImage = missingImageFiles.Count > 0;
                    recordParserLog.LogInfo.IsMissingNative = null != missingNativeFile;
                    recordParserLog.LogInfo.IsMissingText = isMissingContent;
                    recordParserLog.LogInfo.AddedDocument = 1;
                    recordParserLog.LogInfo.AddedImages = importedImagesCount;
                    recordParserLog.LogInfo.DocumentId = docId;
                    recordParserLog.LogInfo.DCN = documentCtrlNbr;
                }
                else
                {
                    recordParserLog.LogInfo.Information = Constants.RecordParserFailureMessage;
                    recordParserLog.LogInfo.Message = Constants.MsgFailedParsing;
                    recordParserLog.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                    if (!string.IsNullOrEmpty(documentCtrlNbr))
                    {
                        recordParserLog.LogInfo.Information += string.Format(Constants.FailedRecordMessage, documentCtrlNbr);
                        recordParserLog.LogInfo.Message += Constants.MsgFailedRecord;
                        recordParserLog.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                    }
                }
                return recordParserLog;
            }
            catch (Exception ex)
            {
                throw new Exception(Constants.ErrorConstrcutLogData + ex.Message);
            }
        }
        #endregion
    }
}
