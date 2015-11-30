//-----------------------------------------------------------------------------------------
// <copyright file="InheritedCode.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kokila Bai S L</author>
//      <description>
//          Part of DcbParserWorker class
//      </description>
//      <changelog>
//          <date value="7-Feb-2012">Fix for bug 93851</date>
//          <date value="14-Feb-2012">Fix for bug 96652</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Runtime.CompilerServices;
using ClassicServicesLibrary;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.TraceServices;

using Field = ClassicServicesLibrary.Field;

namespace LexisNexis.Evolution.Worker
{
    using System.Linq;

    using LexisNexis.Evolution.Business.Relationships;

    public partial class DcbParserWorker : WorkerBase
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        private DcbOpticonJobBEO PopulateImportRequest(ProfileBEO profiledata)
        {
            DcbOpticonJobBEO request = new DcbOpticonJobBEO();

            request.JobTypeName = profiledata.ImportTypeName;
            request.JobName = profiledata.ImportJobName;
            request.SysDocId = profiledata.SysDocID;
            request.SysImportType = profiledata.SysImportTypeID;
            // Default settings
            request.StatusBrokerType = BrokerType.Database;
            request.CommitIntervalBrokerType = BrokerType.ConfigFile;
            request.CommitIntervalSettingType = SettingType.CommonSetting;
            //MatterName
            request.MatterName = profiledata.DatasetDetails.Matter.FolderName;
            //Source Path
            request.DcbSourcePath = profiledata.Locations[0].ToString(CultureInfo.InvariantCulture);

            //Target DatasetId
            request.TargetDatasetId = profiledata.DatasetDetails.CollectionId;

            //DatasetFolderId
            request.DatasetFolderId = profiledata.DatasetDetails.FolderID;
            //fieldMappinga
            request.FieldMappings = profiledata.FieldMapping;
            //ContentFieldMappings
            request.ContentFields = profiledata.ContentFields;
            request.MatterId = profiledata.DatasetDetails.Matter.FolderID;
            request.IncludeTags = profiledata.IncludeAssociatedTags;
            request.IncludeNotes = profiledata.IncludeNotes;
            request.DcbCredentialList = profiledata.DcbUNPWs;
            request.NativeFilePath = profiledata.NativeFilePathField;

            request.ImageSetName = profiledata.ImageSetName;
            request.ImportImages = profiledata.IsImportImages;
            request.NewImageset = profiledata.IsNewImageSet;
            request.JobName = profiledata.ImportJobName;

            _dataset = DataSetBO.GetDataSetDetailForDataSetId(request.DatasetFolderId);

            //Populate Family Info
            request.IsImportFamilies = profiledata.IsImportFamilyRelations;
            request.FamilyRelations = profiledata.FamilyRelations;

            return request;
        }

        private void InitializeDefaultFields()
        {
            _evDocumentSysImportTypeField = new RVWDocumentFieldBEO
                                                {
                                                    FieldId = Convert.ToInt32(DcbOpticonJobBEO.SysImportType),
                                                    FieldName = EVSystemFields.ImportType,
                                                    FieldValue = Constants.DCB_IMPORT_TYPE,
                                                    IsSystemField = true,
                                                    IsRequired = true
                                                };
            if (_dataset != null)
            {
                FieldBEO contentField = _dataset.DatasetFieldList.Find(o => o.FieldType.DataTypeId == Constants.ContentFieldType);
                if (contentField != null)
                {
                    _contentFieldId = contentField.ID;
                    _contentFieldName = contentField.Name;
                }
            }
        }

        private void IncludeDcbFieldsForContentInFieldMapping()
        {
            foreach (string contentfld in DcbOpticonJobBEO.ContentFields.Field)
            {
                if (null == DcbOpticonJobBEO.FieldMappings.Find(o => o.SourceFieldName == contentfld))
                {
                    FieldMapBEO fieldMap = new FieldMapBEO
                                               {
                                                   SourceFieldName = contentfld,
                                                   SourceFieldID =
                                                       DcbFacade.GetFields().Items.Find(o => o.Name == contentfld).Code,
                                                   DatasetFieldTypeID = Constants.ContentFieldType,
                                                   DatasetFieldID = _contentFieldId,
                                                   DatasetFieldName = _contentFieldName
                                               };
                    DcbOpticonJobBEO.FieldMappings.Add(fieldMap);
                }
            }
        }

        protected void FetchDocumentFromDCB(int documentNumber,
            List<DocumentDetail> documentDetailList, FamiliesInfo familiesInfo, JobWorkerLog<DcbParserLogInfo> dcbParserLogEntry)
        {
            #region Precondition asserts
            documentDetailList.ShouldNotBe(null);
            dcbParserLogEntry.ShouldNotBe(null);
            #endregion
            RVWDocumentBEO evDocument = new RVWDocumentBEO();
            try
            {
                //Get the document from DcbFacade
                Document currentDcbDocument = DcbFacade.GetDocument(documentNumber);

                //Throw exception if GetDocument fails
                currentDcbDocument.ShouldNotBe(null);

                //Create the target EV document
                evDocument.DocumentId = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
                dcbParserLogEntry.LogInfo.DocumentId = evDocument.DocumentId;
                evDocument.CollectionId = DcbOpticonJobBEO.TargetDatasetId;
                evDocument.MatterId = DcbOpticonJobBEO.MatterId;

                //Add the fields required for casemap
                RVWDocumentFieldBEO evDocumentAccessionNumField = new RVWDocumentFieldBEO
                                                   {
                                                       FieldId = Convert.ToInt32(DcbOpticonJobBEO.SysDocId),
                                                       FieldName = EVSystemFields.DcbId,
                                                       IsSystemField = true,
                                                       IsRequired = true,
                                                       FieldValue = Convert.ToString(currentDcbDocument.UUID, CultureInfo.InvariantCulture)
                                                   };
                evDocument.FieldList.Add(evDocumentAccessionNumField);
                evDocument.FieldList.Add(_evDocumentSysImportTypeField);

                //Set the fields from field mapping except content field
                foreach (FieldMapBEO fieldMap in DcbOpticonJobBEO.FieldMappings)
                {
                    Field dcbField = currentDcbDocument.FieldItems.Find(o => (o.Code == fieldMap.SourceFieldID));

                    //Profile fieldmapping has duplicates
                    RVWDocumentFieldBEO evDocumentFieldBEO = evDocument.FieldList.Find(o => o.FieldId.Equals(fieldMap.DatasetFieldID));
                    if ((null != dcbField) && (evDocumentFieldBEO == null) && (fieldMap.DatasetFieldID != _contentFieldId))
                    {
                        RVWDocumentFieldBEO evDocuemtnField = new RVWDocumentFieldBEO
                                                                  {
                                                                      FieldId = fieldMap.DatasetFieldID,
                                                                      FieldName = fieldMap.DatasetFieldName
                                                                  };


                        FieldBEO evfieldDef = _dataset.DatasetFieldList.Find(o => o.ID == evDocuemtnField.FieldId);
                        evDocuemtnField.FieldValue = evfieldDef.FieldType.DataTypeId == Constants.DateDataType
                                                         ? GetDateFiedlValue(dcbField, dcbParserLogEntry)
                                                         : Regex.Replace(dcbField.Value, "\r\n", "\n");
                        evDocument.FieldList.Add(evDocuemtnField);
                    }
                }

                //Separate logic for content
                StringBuilder sbContent = new StringBuilder();
                if (DcbOpticonJobBEO.ContentFields != null)
                {
                    foreach (string contentfield in DcbOpticonJobBEO.ContentFields.Field)
                    {
                        Field dcbContentField = currentDcbDocument.FieldItems.Find(o => (o.Name.Equals(contentfield)));
                        if (null != dcbContentField)
                        {
                            sbContent.Append(dcbContentField.Value);
                        }
                    }
                }
                string text = sbContent.ToString().Replace("\r\n", "\n");
                //evDocument.DocumentBinary.Content = Regex.Replace(sbContent.ToString(), "\r\n", "\n");

                if (!DumpTextToFile(evDocument, text, dcbParserLogEntry))
                {
                    return;
                }

                //Set the native file path if selected
                evDocument.NativeFilePath = GetNativeFilePath(currentDcbDocument);

                if (!String.IsNullOrEmpty(evDocument.NativeFilePath) && File.Exists(evDocument.NativeFilePath))
                {
                    FileInfo fileInfo = new FileInfo(evDocument.NativeFilePath);
                    //Tracer.Trace("DcbParcer located native document {0} for DocumentId = {1} and the file length is {2}",
                    //    evDocument.NativeFilePath, evDocument.DocumentId, fileInfo.Length);
                    if (fileInfo.Length > 0)
                    {
                        evDocument.FileSize = (int)Math.Ceiling(fileInfo.Length / 1024.0);
                    }
                    else
                    {
                        evDocument.FileSize = 0;
                    }

                    evDocument.MD5HashValue = DocumentHashHelper.GetMD5HashValue(evDocument.NativeFilePath);
                    evDocument.SHAHashValue = DocumentHashHelper.GetSHAHashValue(evDocument.NativeFilePath);
                }

                //Set the MIME type
                string extn = string.Empty;
                string newExtn = string.Empty;
                extn = Path.GetExtension(evDocument.NativeFilePath);
                if (!String.IsNullOrEmpty(extn))
                    newExtn = extn.Remove(0, 1);
                evDocument.MimeType = GetMimeType(newExtn);
                evDocument.FileExtension = extn;

                string createdByGuid = String.Empty;
                if (null != ProfileBEO && null != ProfileBEO.CreatedBy)
                {
                    createdByGuid = ProfileBEO.CreatedBy;
                }
                evDocument.CreatedBy = createdByGuid;
                evDocument.ModifiedBy = createdByGuid;

                if (File.Exists(evDocument.NativeFilePath))
                {
                    //Calculating size of file in KB
                    FileInfo fileInfo = new FileInfo(evDocument.NativeFilePath);
                    evDocument.FileSize = (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);

                    if (evDocument.DocumentBinary == null) { evDocument.DocumentBinary = new RVWDocumentBinaryBEO(); }
                    RVWExternalFileBEO nativeFile = new RVWExternalFileBEO
                    {
                        Type = NATIVE_FILE_TYPE,
                        Path = evDocument.NativeFilePath
                    };
                    evDocument.DocumentBinary.FileList.Add(nativeFile);
                }

                DocumentDetail documentDetail = new DocumentDetail
                                                    {
                                                        // CorrId is the same as TaskId and it is 1 based.
                                                        CorrelationId = checked(documentNumber + 1).ToString(CultureInfo.InvariantCulture),
                                                        IsNewDocument = true,
                                                        docType = DocumentsetType.NativeSet,
                                                        document = evDocument
                                                    };
                documentDetailList.Add(documentDetail);

                //Add Tags
                if (DcbOpticonJobBEO.IncludeTags && null != currentDcbDocument.TagItems && currentDcbDocument.TagItems.Count > 0)
                {
                    if (null == documentDetail.DcbTags)
                    {
                        documentDetail.DcbTags = new List<DcbTags>();
                    }
                    DcbDocumentTags dcbDocumentTags = new DcbDocumentTags
                                                          {
                        compositeTagNames = currentDcbDocument.TagItems,
                        DatasetId = DcbOpticonJobBEO.TargetDatasetId,
                        MatterId = DcbOpticonJobBEO.MatterId,
                        DocumentId = evDocument.DocumentId
                    };
                    documentDetail.DcbTags.Add(dcbDocumentTags);
                }

                // Add notes
                AddComments(documentDetail, evDocument, currentDcbDocument);

                //Add Images
                if (DcbOpticonJobBEO.ImportImages)
                {
                    RVWDocumentBEO images = ImportDocumentImages(evDocument.DocumentId, currentDcbDocument);
                    if (null != images)
                    {
                        DocumentDetail imageDocumentDetail = new DocumentDetail
                                                                 {
                                                                     // CorrId is the same as TaskId and it is 1 based.
                                                                     CorrelationId = checked(documentNumber + 1).ToString(CultureInfo.InvariantCulture),
                                                                     IsNewDocument = true,
                                                                     docType = DocumentsetType.ImageSet,
                                                                     document = images
                                                                 };
                        documentDetailList.Add(imageDocumentDetail);
                        dcbParserLogEntry.LogInfo.AddedImages = images.DocumentBinary.FileList.Count;
                    }

                    //Add Redlines
                    //ImportDocumentRedlines();
                }

                //Add Document Relation
                if (DcbOpticonJobBEO.IsImportFamilies)
                {
                    ImportDocumentRelationship(evDocument.DocumentId, currentDcbDocument, familiesInfo);
                }
                #region Postcondition asserts
                documentDetailList.ShouldNotBe(null);
                #endregion
            }
            catch (Exception ex)
            {
                //TaskLogInfo.AddParameters(Constants.ErrorDoAtomicWork + "<br/>" + ex.Message);
                //TaskLogInfo.StackTrace = ex.Source + "<br/>" + ex.Message + "<br/>" + ex.StackTrace;
                //TaskLogInfo.IsError = true;

                ex.Trace().Swallow();
                dcbParserLogEntry.Success = false;
                if (ex.ToUserString().Contains(Constants.DiskFullErrorMessage))
                {
                    dcbParserLogEntry.LogInfo.Message = "There is not enough space on the disk";
                    throw;
                }
                else
                {
                    dcbParserLogEntry.LogInfo.Message = ex.ToUserString();
                }
            }
        }

        private void ImportDocumentRelationship(string documentId, Document currentDcbDocument, FamiliesInfo familiesInfo)
        {
            // Debug
            //Tracer.Warning("DCB documentId = {0}", documentId);

            if (DcbOpticonJobBEO.FamilyRelations.IsEmailDCB)
            {
                if (null != currentDcbDocument.children && currentDcbDocument.children.Any())
                {
                    foreach (int physicalChildDocumentNumber in currentDcbDocument.children)
                    {
                        FamilyInfo familyInfoRecord1 = new FamilyInfo(null);
                        familyInfoRecord1.OriginalDocumentId = GenerateDocumentIdForEV(physicalChildDocumentNumber.ToString(CultureInfo.InvariantCulture));
                        familyInfoRecord1.OriginalParentId = GenerateDocumentIdForEV(currentDcbDocument);
                        familiesInfo.FamilyInfoList.Add(familyInfoRecord1);
                    }
                }

                // We don't skip standalone documents for Families, because they always can appear to be topmost parents.
                // And also we need all of them to provide Original to Real Id translation. 
                FamilyInfo familyInfoRecord2 = new FamilyInfo(documentId);
                familyInfoRecord2.OriginalDocumentId = GenerateDocumentIdForEV(currentDcbDocument.PhysicalNumber.ToString(CultureInfo.InvariantCulture));
                familyInfoRecord2.OriginalParentId = null;
                familiesInfo.FamilyInfoList.Add(familyInfoRecord2);

                return;
            }

            /*One way of maintaining parent-child relationship in DCB is through PARENT_DOCID & DOCID fields
            Most of the case it is assumed that PARENT_DOCID & DOCID will have UUID, but in CNClassic there is a 
            way to map DOCID and PARENT_DOCID field during import process 
            For Document realtion the first priority is given to PAREN_DOCID & DOCID field */

            Field dcbDocIdField = currentDcbDocument.FieldItems.Find(o => o.Code.Equals(DcbOpticonJobBEO.FamilyRelations.DocId));
            string dcbDocId = null;
            if (null != dcbDocIdField)
            {
                dcbDocId = ExtractDcbDocumentId(dcbDocIdField.Value, DcbOpticonJobBEO.FamilyRelations.IsDocIDFormatRange);
            }

            if (dcbDocId == null)
            {
                Tracer.Warning("Potentially bad DCB source data: document {0} has no DocId field", documentId);
                return; // No point in sending family record with the original document Id missing
            }

            Field dcbParentDocIdField = currentDcbDocument.FieldItems.Find(o => o.Code.Equals(DcbOpticonJobBEO.FamilyRelations.ParentDocId));
            string dcbParentDocId = null;
            if (null != dcbParentDocIdField)
            {
                dcbParentDocId = ExtractDcbDocumentId(dcbParentDocIdField.Value, DcbOpticonJobBEO.FamilyRelations.IsParentDocIDFormatRange);
            }

            FamilyInfo familyInfo = new FamilyInfo(documentId);
            familyInfo.OriginalDocumentId = GenerateDocumentIdForEV(dcbDocId);
            familyInfo.OriginalParentId = GenerateDocumentIdForEV(dcbParentDocId);
            familiesInfo.FamilyInfoList.Add(familyInfo);
            //Tracer.Trace("Adding Relationship record. Parent = {0}, Child = {1}", threadingEntity.ParentDocumentID, threadingEntity.ChildDocumentID);
        }

        private static string ExtractDcbDocumentId(string source, bool pair)
        {
            if (String.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            string dcbDocId = source.Replace("\r", "").Replace("\n", "").Trim();

            if (pair)
            {
                dcbDocId = dcbDocId.Substring(0, (dcbDocId.Length / 2)).Trim();
            }

            return dcbDocId;
        }

        private string GenerateDocumentIdForEV(string dcbDocId)
        {
            return String.Format("{0}-{1}", DcbOpticonJobBEO.BaseKeyDocId, dcbDocId);
        }
        
        private string GenerateDocumentIdForEV(Document currentDcbDocument)
        {
            if (currentDcbDocument == null) return null;

            string generatedDocumentIdForEV;
            switch (SelectedDocumentIdGenerationAlgorithm)
            {
                case DocumentIdGenerationAlgorithm.FromDcbDocId:
                    Field dcbDocIdField = currentDcbDocument.FieldItems.Find(o => o.Code.Equals(DcbOpticonJobBEO.FamilyRelations.DocId));
                    string dcbDocId = null;
                    if (null != dcbDocIdField)
                    {
                        dcbDocId = ExtractDcbDocumentId(dcbDocIdField.Value, DcbOpticonJobBEO.FamilyRelations.IsDocIDFormatRange);
                    }
                    if (String.IsNullOrEmpty(dcbDocId))
                    {
                        dcbDocId = currentDcbDocument.PhysicalNumber.ToString(CultureInfo.InvariantCulture);
                    }
                    generatedDocumentIdForEV = String.Format("{0}-{1}", DcbOpticonJobBEO.BaseKeyDocId, dcbDocId);
                    break;
                case DocumentIdGenerationAlgorithm.FromPhysicalDocumentNumber:
                default:
                    generatedDocumentIdForEV = String.Format("{0}-{1}", DcbOpticonJobBEO.BaseKeyDocId, currentDcbDocument.PhysicalNumber);
                    break;
            }
            return generatedDocumentIdForEV;
        }

        private void AddComments(DocumentDetail documentDetail, RVWDocumentBEO rVwDocumentBEO, Document currentDcbDocument)
        {
            if (!DcbOpticonJobBEO.IncludeNotes || null == currentDcbDocument.Notes || currentDcbDocument.Notes.Count == 0)
            {
                return;
            }

            DcbDocumentTags dcbDocumentTags = new DcbDocumentTags
                                                  {
                compositeTagNames = new List<string>(),
                DatasetId = DcbOpticonJobBEO.TargetDatasetId,
                MatterId = DcbOpticonJobBEO.MatterId,
                DocumentId = rVwDocumentBEO.DocumentId
            };
            List<DocumentCommentBEO> comments = FetchComments(rVwDocumentBEO, currentDcbDocument, dcbDocumentTags);

            if (null == documentDetail.DcbComments)
            {
                documentDetail.DcbComments = new List<DocumentCommentBEO>();
            }
            documentDetail.DcbComments = comments;

            if (dcbDocumentTags.compositeTagNames.Count == 0)
            {
                return; // Notes don't contain any tags
            }

            if (null == documentDetail.DcbTags)
            {
                documentDetail.DcbTags = new List<DcbTags>();
            }
            documentDetail.DcbTags.Add(dcbDocumentTags);
        }

        private bool DumpTextToFile(RVWDocumentBEO rVwDocumentBEO, string contentText, JobWorkerLog<DcbParserLogInfo> dcbParserLogEntry)
        {
            TextFileFolder.ShouldNotBe(null);
            if (string.IsNullOrEmpty(contentText)) return true;

            string filePath = Path.Combine(TextFileFolder, rVwDocumentBEO.DocumentId + Constants.FileExtension);
            File.AppendAllText(filePath, contentText);

            if (rVwDocumentBEO.DocumentBinary == null) { rVwDocumentBEO.DocumentBinary = new RVWDocumentBinaryBEO(); }
            RVWExternalFileBEO textFile = new RVWExternalFileBEO
            {
                Type = TEXT_FILE_TYPE,
                Path = filePath
            };
            rVwDocumentBEO.DocumentBinary.FileList.Add(textFile);
            return true;
        }

        private static string GetDateFiedlValue(Field dcbField, JobWorkerLog<DcbParserLogInfo> dcbParserLogEntry)
        {
            string strDateTime = dcbField.Value;
            DateTime dt;
            bool success = DateTime.TryParseExact(strDateTime, Constants.DateFormatType3, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
            if (success)
            {
                return dt.ToString(Constants.EVDateTimeFormat);    
            }

            string errorMessage = String.Format("Cannot parse string \"{0}\" as date/time format \"{1}\"", strDateTime, Constants.DateFormatType3);
            Tracer.Error(errorMessage);
            dcbParserLogEntry.Success = false;
            dcbParserLogEntry.LogInfo.Message = errorMessage;
            return String.Empty;
        }

        private string GetNativeFilePath(Document currentDcbDocument)
        {
            Field nativeFilePathField = currentDcbDocument.FieldItems.Find(o => o.Name.Equals(DcbOpticonJobBEO.NativeFilePath));
            string nativeFilePath = String.Empty;
            if ((null != nativeFilePathField) && (false == String.IsNullOrEmpty(nativeFilePathField.Value)))
            {
                NoteRecord2 nr2 = currentDcbDocument.Notes.Find(o => o.LinkFieldCode == nativeFilePathField.Code);
                try
                {
                    //There are five cases I identified for finding the file path on different scnarios

                    //Native File Field has valid path
                    nativeFilePath = File.Exists(nativeFilePathField.Value) ? nativeFilePathField.Value : ProcessFromRelativePath(nativeFilePathField.Value);

                    //Process from notes database
                    if ((!File.Exists(nativeFilePath)) && (null != nr2))
                    {
                        nativeFilePath = File.Exists(nr2.Attachment) ? nr2.Attachment : ProcessFromRelativePath(nr2.Attachment);
                    }


                    //Next Try
                    if (!File.Exists(nativeFilePath))
                    {
                        nativeFilePath = String.Format("{0}\\{1}", Path.GetDirectoryName(DcbOpticonJobBEO.DcbSourcePath), Path.GetFileName(nativeFilePathField.Value));
                    }

                    if (!File.Exists(nativeFilePath)) return String.Empty;
                }
                catch (Exception ex)
                {
                    ex.Trace().Swallow();
                    return string.Empty;
                }
            }
            return nativeFilePath;
        }

        string ProcessFromRelativePath(string nativeFilePath)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                //Extrace relative path from Native File Path
                string[] dirs = Regex.Split(nativeFilePath, "\\\\");
                int pathcounter = dirs.Length;

                if (pathcounter < 2) // Unexpected form of nativeFilePath
                {
                    return String.Empty;
                }

                int index = DcbOpticonJobBEO.DcbSourcePath.LastIndexOf("\\", StringComparison.Ordinal);

                string dcbPath = DcbOpticonJobBEO.DcbSourcePath.Substring(0, index);

                sb.Append(String.Format("{0}\\{1}\\{2}", dcbPath, dirs[pathcounter - 2], dirs[pathcounter - 1]));

                return sb.ToString();
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static string GetMimeType(string fileExt)
        {
            string mimeType = string.Empty;
            switch (fileExt)
            {
                case Constants.File_Ext_Bmp:
                    {
                        mimeType = Constants.MimeType_Bmp;
                        break;
                    }
                case Constants.File_Ext_Doc:
                    {
                        mimeType = Constants.MimeType_Word;
                        break;
                    }
                case Constants.File_Ext_Excel:
                    {
                        mimeType = Constants.MimeType_Excel;
                        break;
                    }
                case Constants.File_Ext_Html:
                    {
                        mimeType = Constants.MimeType_Html;
                        break;
                    }
                case Constants.File_Ext_Jpeg:
                    {
                        mimeType = Constants.MimeType_Jpeg;
                        break;
                    }
                case Constants.File_Ext_Outlook:
                    {
                        mimeType = Constants.MimeType_Outlook;
                        break;
                    }
                case Constants.File_Ext_Pdf:
                    {
                        mimeType = Constants.MimeType_Pdf;
                        break;
                    }
                case Constants.File_Ext_Ppt:
                    {
                        mimeType = Constants.MimeType_Ppt;
                        break;
                    }
                case Constants.File_Ext_Tiff:
                    {
                        mimeType = Constants.MimeType_Tiff;
                        break;
                    }
                case Constants.File_Ext_Txt:
                    {
                        mimeType = Constants.MimeType_Text;
                        break;
                    }
                case Constants.File_Ext_Xml:
                    {
                        mimeType = Constants.MimeType_Xml;
                        break;
                    }
                case Constants.File_Ext_docm:
                    {
                        mimeType = Constants.MimeType_docm;
                        break;
                    }
                case Constants.File_Ext_docx:
                    {
                        mimeType = Constants.MimeType_docx;
                        break;
                    }
                case Constants.File_Ext_dotm:
                    {
                        mimeType = Constants.MimeType_dotm;
                        break;
                    }
                case Constants.File_Ext_dotx:
                    {
                        mimeType = Constants.MimeType_dotx;
                        break;
                    }
                case Constants.File_Ext_potm:
                    {
                        mimeType = Constants.MimeType_potm;
                        break;
                    }
                case Constants.File_Ext_potx:
                    {
                        mimeType = Constants.MimeType_potx;
                        break;
                    }
                case Constants.File_Ext_ppam:
                    {
                        mimeType = Constants.MimeType_ppam;
                        break;
                    }
                case Constants.File_Ext_ppsm:
                    {
                        mimeType = Constants.MimeType_ppsm;
                        break;
                    }
                case Constants.File_Ext_ppsx:
                    {
                        mimeType = Constants.MimeType_ppsx;
                        break;
                    }
                case Constants.File_Ext_pptm:
                    {
                        mimeType = Constants.MimeType_pptm;
                        break;
                    }
                case Constants.File_Ext_pptx:
                    {
                        mimeType = Constants.MimeType_pptx;
                        break;
                    }
                case Constants.File_Ext_xlam:
                    {
                        mimeType = Constants.MimeType_xlam;
                        break;
                    }
                case Constants.File_Ext_xlsb:
                    {
                        mimeType = Constants.MimeType_xlsb;
                        break;
                    }
                case Constants.File_Ext_xlsm:
                    {
                        mimeType = Constants.MimeType_xlsm;
                        break;
                    }
                case Constants.File_Ext_xlsx:
                    {
                        mimeType = Constants.MimeType_xlsx;
                        break;
                    }
                case Constants.File_Ext_xltm:
                    {
                        mimeType = Constants.MimeType_xltm;
                        break;
                    }
                case Constants.File_Ext_xltx:
                    {
                        mimeType = Constants.MimeType_xltx;
                        break;
                    }
                case Constants.File_Ext_zip:
                    {
                        mimeType = Constants.MimeType_Zip;
                        break;
                    }
                default:
                    mimeType = Constants.MimeType_OpenXml;
                    break;
            }
            return mimeType;
        }

        private List<DocumentCommentBEO> FetchComments(RVWDocumentBEO rVwDocumentBEO, Document currentDcbDocument,
            DcbDocumentTags dcbDocumentTags)
        {
            List<DocumentCommentBEO> documentCommentBEOList = new List<DocumentCommentBEO>();
            try
            {
                foreach (NoteRecord2 dcbnotes in currentDcbDocument.Notes)
                {
                    DocumentCommentBEO comment = new DocumentCommentBEO
                                                     {
                                                         DocumentId = rVwDocumentBEO.DocumentId,
                                                         CollectionId = new Guid(DcbOpticonJobBEO.TargetDatasetId),
                                                         MatterId = Convert.ToInt64(DcbOpticonJobBEO.MatterId),
                                                         MetadataTypeVersionId = Constants.One
                                                     };

                    FieldMapBEO fieldmap = DcbOpticonJobBEO.FieldMappings.Find(o => (o.SourceFieldID == dcbnotes.LinkFieldCode));

                    if (null != fieldmap)
                    {
                        comment.Comment.FieldId = fieldmap.DatasetFieldID;
                        comment.MetadataType = MetadataType.TextLevelComments;
                        Field fld = currentDcbDocument.FieldItems.Find(o => o.Code == fieldmap.SourceFieldID);

                        int startindex = 0;
                        if (DcbOpticonJobBEO.ContentFields.Field.Contains(fld.Name))
                        {
                            foreach (string contentfld in DcbOpticonJobBEO.ContentFields.Field)
                            {
                                if (contentfld.Equals(fld.Name))
                                {
                                    break;
                                }
                                else
                                {
                                    if (!String.IsNullOrEmpty(fld.Value))
                                        startindex = startindex + Regex.Replace(fld.Value, "\r\n", "\n").Length;
                                }
                            }
                        }
                        JsonComment jscomment = new JsonComment
                                                    {
                                                        FieldId = Convert.ToString(fieldmap.DatasetFieldID),
                                                        IndexInDocument =
                                                            Convert.ToString(startindex + dcbnotes.LinkOffset),
                                                        SelectedText =
                                                            Regex.Replace(fld.Value, Constants.ReturnAndNewLineFeed, Constants.NewLineFeed).Substring(
                                                                dcbnotes.LinkOffset, dcbnotes.LinkLength)
                                                    };
                        jscomment.SelectedText = Regex.Replace(jscomment.SelectedText, Constants.NewLineFeed, Constants.HtmlBreakWithNewLine);
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        comment.Comment.SelectedText = serializer.Serialize(jscomment);


                        comment.SequenceId = 0;
                        comment.VersionId = 0;
                        comment.Comment.Comment = dcbnotes.Text;
                        comment.Comment.FontSize = int.Parse(ConfigurationManager.AppSettings.Get(Constants.DCBCommentsFontSize));
                        comment.Comment.FontName = ConfigurationManager.AppSettings.Get(Constants.DCBCommentsFont);
                        comment.Comment.Color = ConfigurationManager.AppSettings.Get(Constants.DCBCommentsFontColor);
                        string createdByGuid = String.Empty;
                        if (null != ProfileBEO && null != ProfileBEO.CreatedBy)
                        {
                            createdByGuid = ProfileBEO.CreatedBy;
                        }
                        comment.CreatedBy = createdByGuid;
                        comment.ModifiedBy = createdByGuid;

                        documentCommentBEOList.Add(comment);

                        if (DcbOpticonJobBEO.IncludeTags && (dcbnotes.Tags != null) && (dcbnotes.Tags.Count > 0))
                        {
                            dcbDocumentTags.compositeTagNames.AddRange(dcbnotes.Tags);
                        }
                    }
                    else
                    {
                        if (DcbOpticonJobBEO.IncludeTags && (dcbnotes.Tags != null) && (dcbnotes.Tags.Count > 0))
                        {
                            Field fld = currentDcbDocument.FieldItems.Find(o => o.Code == dcbnotes.LinkFieldCode);
                            StringBuilder sbTags = new StringBuilder();
                            foreach (string tag in dcbnotes.Tags)
                            {
                                sbTags.AppendFormat(" {0}, ", tag);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }

            return documentCommentBEOList;
        }

        /// <summary>
        /// Method to import document images
        /// </summary>
        /// <param name="currentDocumentId">current DocumentId</param>
        /// <param name="currentDcbDocument">current Dcb Document</param>
        /// <returns>RVWDocumentBEO entity</returns>
        private RVWDocumentBEO ImportDocumentImages(string currentDocumentId, Document currentDcbDocument)
        {
            RVWDocumentBEO evImageDocument = new RVWDocumentBEO();
            try
            {
                if ((currentDcbDocument.Images != null) && (currentDcbDocument.Images.Count > 0))
                {
                    int imageSize = 0;
                    evImageDocument = new RVWDocumentBEO
                                          {
                                              DocumentId = currentDocumentId,
                                              CollectionId = ImageSetId,
                                              MatterId = DcbOpticonJobBEO.MatterId
                                          };


                    foreach (DcbImage imageObj in currentDcbDocument.Images)
                    {
                        RVWExternalFileBEO rvwExternalfilebeo = new RVWExternalFileBEO
                                                                    {
                                                                        Path = imageObj.ImageData.FullImagePath,
                                                                        Type = Constants.Image
                                                                    };
                        evImageDocument.DocumentBinary.FileList.Add(rvwExternalfilebeo);
                        if (File.Exists(rvwExternalfilebeo.Path))
                        {
                            //Calculating size of file in KB
                            FileInfo fileInfo = new FileInfo(rvwExternalfilebeo.Path);
                            if (fileInfo != null)
                            {
                                imageSize += (int) Math.Ceiling(fileInfo.Length/Constants.KBConversionConstant);
                            }
                        }
                    }

                    #region Assertion
                    evImageDocument.DocumentBinary.FileList.ShouldNotBe(null);
                    evImageDocument.DocumentBinary.FileList.Count.ShouldBeGreaterThan(0); 
                    #endregion

                    string createdByGuid = String.Empty;
                    if (null != ProfileBEO && null != ProfileBEO.CreatedBy)
                    {
                        createdByGuid = ProfileBEO.CreatedBy;
                    }                    
                    evImageDocument.CreatedBy = createdByGuid;
                    evImageDocument.ModifiedBy = createdByGuid;
                    evImageDocument.MimeType = string.Empty;
                    evImageDocument.FileName = string.Empty;
                    evImageDocument.NativeFilePath = string.Empty;
                    evImageDocument.FileExtension = string.Empty;
                    evImageDocument.FileSize = imageSize;
                    return evImageDocument;
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
            return null;
        }

        private ProfileBEO ProfileBEO { get; set; }
        private DcbOpticonJobBEO DcbOpticonJobBEO { get; set; }
        internal const string TEXT_FILE_TYPE = "Text";
        internal const string NATIVE_FILE_TYPE = "Native";
        internal const string IMAGE_FILE_TYPE = "Image";
        private int _contentFieldId = -1;
        private string _contentFieldName;
        private RVWDocumentFieldBEO _evDocumentSysImportTypeField = null;
        private DatasetBEO _dataset = null;
    }

    /// <summary>
    /// Json comment
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    [Serializable]
    public class JsonComment
    {
        public string SelectedText { get; set; }
        public string FieldId { get; set; }
        public string IndexInDocument { get; set; }
    }
}
