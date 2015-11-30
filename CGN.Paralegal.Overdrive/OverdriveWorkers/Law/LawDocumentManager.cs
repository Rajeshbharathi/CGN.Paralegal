using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker;
using LexisNexis.Evolution.Worker.Data;
using Constants = LexisNexis.Evolution.Worker.Constants;

namespace OverdriveWorkers.Law
{
    public class LawDocumentManager
    {
        private readonly LawImportBEO _jobParams;
        private readonly string _jobRunId;
        private readonly string _workerInstanceId;
        private readonly DatasetBEO _datasetDetails;
        private const string LawDocumentId = "_EVLawDocId";

        public LawDocumentManager(LawImportBEO jobParams, string jobRunId, string workerInstanceId, DatasetBEO datasetDetails)
        {
            _jobParams = jobParams;
            _jobRunId = jobRunId;
            _workerInstanceId = workerInstanceId;
            _datasetDetails = datasetDetails;
        }

        /// <summary>
        /// Gets the law documents based on filter tags it includes mapped fields and tags
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="documentCtrlNbr"></param>
        /// <param name="rawDocument"></param>
        /// <param name="logs"></param>
        /// <returns></returns>
        public List<DocumentDetail> GetDocuments(string correlationId, string documentCtrlNbr,
                                                 RVWDocumentBEO rawDocument,
                                                 out JobWorkerLog<LawImportLogInfo> logs)
        {
            var documentDetailList = new List<DocumentDetail>();
            var document = ConsturctDocument(correlationId, rawDocument, out logs);
            if (_jobParams.ImportOptions == ImportOptionsBEO.AppendNew)
            {
                // Assign DCN
                document.DocumentControlNumber = documentCtrlNbr;
                //1) Construct Native Set
                var nativeSetDocument = GetNativeDocument(document);
                var doc = new DocumentDetail
                    {
                        CorrelationId = correlationId,
                        docType = DocumentsetType.NativeSet,
                        document = nativeSetDocument,
                        ConversationIndex = document.ConversationIndex,
                        IsNewDocument = true
                    };
                //Add Native Document
                documentDetailList.Add(doc);

                //2) Construct Image Set                       
                if (_jobParams.IsImportImages && !string.IsNullOrEmpty(_jobParams.ImageSetId))
                {
                    var imageSetDocument = GetImageDocuments(document, _jobParams.ImageSetId);
                    var docImg = new DocumentDetail
                        {
                            CorrelationId = correlationId,
                            docType = DocumentsetType.ImageSet,
                            document = imageSetDocument,
                            IsNewDocument = true
                        };
                    //Add Image Document
                    documentDetailList.Add(docImg);
                }
            }
            else
            {
                GetOverlayDocuments(document, documentDetailList, correlationId);
            }

            return documentDetailList;
        }

        /// <summary>
        /// To get the overlay documents
        /// </summary>
        /// <param name="document"></param>
        /// <param name="documentDetailList"></param>
        /// <param name="correlationId"></param>
        private void GetOverlayDocuments(RVWDocumentBEO document, List<DocumentDetail> documentDetailList, string correlationId)
        {
            var doc = new DocumentDetail { document = document, ConversationIndex = document.ConversationIndex };
            //Create a unique file name for content file
            doc.document.DocumentBinary.FileList.ForEach(
                x =>
                x.Path =
                (x.Type.ToLower() == Constants.TEXT_FILE_TYPE.ToLower())
                    ? string.Format("{0}?id={1}", x.Path, Guid.NewGuid().ToString()/*.Replace("-", "").ToUpper()*/)
                    : x.Path);
            doc.CorrelationId = correlationId;

            var lawDocId = _datasetDetails.DatasetFieldList.FirstOrDefault(x => x.Name.Equals(LawDocumentId));
            if (lawDocId != null)
            {
                // Create a Field Business Entity for each mapped field
                var matchingField = new RVWDocumentFieldBEO
                {
                    // set required properties / field data
                    FieldId = lawDocId.ID,
                    FieldName = lawDocId.Name,
                    FieldValue = document.LawDocumentId.ToString(CultureInfo.InvariantCulture)
                };
                doc.OverlayMatchingField = new List<RVWDocumentFieldBEO> { matchingField };
            }

            doc.ParentDocId = document.FamilyId;
            documentDetailList.Add(doc);
        }

        /// <summary>
        /// Clear image file information in DocumentBEO 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static RVWDocumentBEO GetNativeDocument(RVWDocumentBEO document)
        {
            var nativeSetDocument = CopyDocumentObject(document, true);
            var fileList =
                nativeSetDocument.DocumentBinary.FileList.Where(
                    x => x.Type.ToLower() != Constants.IMAGE_FILE_TYPE.ToLower()).ToList();
            nativeSetDocument.DocumentBinary.FileList.Clear();
            if (fileList.Any())
            {
                fileList.ForEach(x => nativeSetDocument.DocumentBinary.FileList.Add(x));
                nativeSetDocument.DocumentBinary.FileList.ForEach(
                    x =>
                    x.Path =
                    (x.Type == Constants.TEXT_FILE_TYPE)
                        ? string.Format("{0}?id={1}", x.Path, Guid.NewGuid().ToString()/*.Replace("-", "").ToUpper()*/)



                        : x.Path);
            }
            if (document.DocumentBinary != null)
            {
                if (!string.IsNullOrEmpty(document.DocumentBinary.Content)) //Assign Content Value
                {
                    nativeSetDocument.DocumentBinary.Content = document.DocumentBinary.Content;
                }
            }
            return nativeSetDocument;
        }

        /// <summary>
        ///  Clear except image file information in DocumentBEO 
        /// </summary>
        /// <param name="document">document</param>
        /// <param name="collectionId">CollectionId</param>
        /// <returns>RVWDocumentBEO</returns>
        private static RVWDocumentBEO GetImageDocuments(RVWDocumentBEO document, string collectionId)
        {
            document.ShouldNotBe(null);
            collectionId.ShouldNotBe(null);
            var imageSetDocument = CopyDocumentObject(document, false);
            var imageSize = 0;
            if (imageSetDocument.DocumentBinary.FileList != null && imageSetDocument.DocumentBinary.FileList.Count > 0)
            {
                var imagefileList =
                    imageSetDocument.DocumentBinary.FileList.Where(
                        x => x.Type.ToLower().Equals(Constants.IMAGE_FILE_TYPE.ToLower())
                        && !String.IsNullOrEmpty(x.Path)
                        ).ToList();
                imageSetDocument.DocumentBinary.FileList.Clear();
                imagefileList.ForEach(x => imageSetDocument.DocumentBinary.FileList.Add(x));
                imageSize += (from docFile in imageSetDocument.DocumentBinary.FileList
                              where File.Exists(docFile.Path)
                              select new FileInfo(docFile.Path)
                                  into fileInfo
                                  where fileInfo != null
                                  select (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant)).Sum();
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
        /// Copy Document Object
        /// </summary>
        /// <param name="document"></param>
        /// <param name="isNativeDoc"></param>
        /// <returns></returns>
        private static RVWDocumentBEO CopyDocumentObject(RVWDocumentBEO document, bool isNativeDoc)
        {
            var documentsetDocument = new RVWDocumentBEO
                {
                    CollectionId = document.CollectionId,
                    MatterId = document.MatterId,
                    CreatedBy = document.CreatedBy,
                    Id = document.Id,
                    DocumentId = document.DocumentId,
                    MimeType = document.MimeType,
                    FileName = document.FileName,
                    NativeFilePath = document.NativeFilePath,
                    FileExtension = document.FileExtension,
                    FileSize = document.FileSize,
                    DocumentControlNumber = document.DocumentControlNumber,
                    ImportDescription = document.ImportDescription,
                    CustomFieldToPopulateText = document.CustomFieldToPopulateText
                };

            if (isNativeDoc)
            {
                documentsetDocument.LawDocumentId = document.LawDocumentId;
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
            if (document.Tags != null && document.Tags.Any())
            {
                document.Tags.ForEach(x => documentsetDocument.Tags.Add(x));
            }
            if (!string.IsNullOrEmpty(document.CrossReferenceFieldValue))
            {
                documentsetDocument.CrossReferenceFieldValue = document.CrossReferenceFieldValue;
            }
            return documentsetDocument;
        }

        public RVWDocumentBEO ConsturctDocument(string correlationId, RVWDocumentBEO document,
                                                out JobWorkerLog<LawImportLogInfo> logs)
        {
            int importedImagesCount;
            document.CollectionId = _jobParams.CollectionId;
            document.MatterId = _jobParams.MatterId;
            document.CreatedBy = _jobParams.CreatedBy;
            document.ImportDescription = _jobParams.ImportDetail;
            bool isMissingContent;

            SetFamilyRelationshipIds(document);

            #region Native File

            var missingNativeFile = GetMissingNativeFiles(document);

            #endregion

            #region Image File

            var missingImageFiles = GetMissingImageFiles(document, out importedImagesCount);

            #endregion

            #region Text File (Content File)
            if (_jobParams != null)
            {
                if (!_datasetDetails.DatasetFieldList.Any(f => f.FieldType.DataTypeId == 2000 && f.Name == _jobParams.FieldNameToPopulateText))
                {
                    document.CustomFieldToPopulateText = _jobParams.FieldNameToPopulateText;
                }
            }
            var missingContentFiles = GetMissingContentFiles(document, out isMissingContent);

            #endregion

            logs = ConstructLog(correlationId, document.DocumentId,
                                missingNativeFile, missingImageFiles, isMissingContent, missingContentFiles,
                                importedImagesCount, document.DocumentControlNumber,
                                document.CrossReferenceFieldValue);

            return document;
        }

        /// <summary>
        /// To set family id and document relationship.
        /// </summary>
        /// <param name="document"></param>
        private void SetFamilyRelationshipIds(RVWDocumentBEO document)
        {
            if (!_jobParams.CreateFamilyGroups) return;
            document.EVInsertSystemRelationShipFields = true;
            document.EVLoadFileDocumentId = document.LawDocumentId.ToString(CultureInfo.InvariantCulture);
            document.EVLoadFileParentId = document.FamilyId;
        }

        //To get the missing native files
        private string GetMissingNativeFiles(RVWDocumentBEO document)
        {
            string missingNativeFile = null;
            if (document.DocumentBinary == null) return null;
            if (_jobParams.IsImportNative)
            {
                var nativeFile =
                    document.DocumentBinary.FileList.Find(x => x.Type == Constants.NATIVE_FILE_TYPE);
                if (nativeFile != null)
                {
                    var nativeFilePath = nativeFile.Path;
                    var extension = Path.GetExtension(nativeFilePath);
                    if (extension != null)
                        document.MimeType = GetMimeType(extension.Replace(".", "")); // Remove MimeType
                    document.FileName = Path.GetFileNameWithoutExtension(nativeFilePath);
                    document.NativeFilePath = nativeFilePath;
                    nativeFile.Type = Constants.NATIVE_FILE_TYPE;
                    nativeFile.Path = document.NativeFilePath;
                    document.FileExtension = Path.GetExtension(nativeFilePath);
                    if (File.Exists(nativeFilePath))
                    {
                        //Calculating size of file in KB
                        var fileInfo = new FileInfo(nativeFilePath);
                        document.FileSize = (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);
                        document.MD5HashValue = DocumentHashHelper.GetMD5HashValue(nativeFilePath);
                        document.SHAHashValue = DocumentHashHelper.GetSHAHashValue(nativeFilePath);
                    }
                    else //Missing Native File
                    {
                        missingNativeFile = nativeFilePath;
                    }
                }
            }
            else
            {
                document.DocumentBinary.FileList.RemoveAll(x => x.Type == Constants.NATIVE_FILE_TYPE);
            }
            return missingNativeFile;
        }

        /// <summary>
        /// To get the missing image files
        /// </summary>
        /// <param name="document"></param>
        /// <param name="importedImagesCount"></param>
        /// <returns></returns>
        private List<string> GetMissingImageFiles(RVWDocumentBEO document, out int importedImagesCount)
        {
            importedImagesCount = 0;
            var missingImageFiles = new List<string>();
            if (_jobParams.IsImportImages && document.DocumentBinary != null)
            {
                var imageFiles =
                    document.DocumentBinary.FileList.FindAll(x => x.Type == Constants.IMAGE_FILE_TYPE);
                if (imageFiles.Any())
                {
                    missingImageFiles.AddRange(from image in imageFiles
                                               where !string.IsNullOrWhiteSpace(image.Path) && !File.Exists(image.Path)
                                               select image.Path);
                    importedImagesCount = imageFiles.Count() - missingImageFiles.Count();
                }
            }
            return missingImageFiles;
        }

        /// <summary>
        /// To get the missing content files.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="isMissingContent"></param>
        /// <returns></returns>
        private IEnumerable<string> GetMissingContentFiles(RVWDocumentBEO document, out bool isMissingContent)
        {
            isMissingContent = false;
            var missingContentFiles = new List<string>();
            if (document.DocumentBinary == null) return missingContentFiles;
            if (_jobParams.IsImportTextOCR)
            {
                var textFiles = document.DocumentBinary.FileList.FindAll(x => x.Type == Constants.TEXT_FILE_TYPE);
                var textPriority = _jobParams.TextImportOrder.Select(item => item.Replace(" ", string.Empty).ToLower()).ToList();

                if (textFiles.Any())
                {
                    var matchText = string.Empty;
                    //Loop through each priority text and find the available content text file
                    foreach (var pText in textPriority)
                    {
                        var textFile = textFiles.Find(t => t.Text.ToLower().Equals(pText));
                        if (textFile == null) continue;
                        if (string.IsNullOrWhiteSpace(textFile.Path) || !File.Exists(textFile.Path)) continue;
                        matchText = textFile.Text;
                        break;
                    }

                    //Removing the other text files from FileList when the priority is met
                    document.DocumentBinary.FileList.RemoveAll(
                        x =>
                        x.Type == Constants.TEXT_FILE_TYPE && !string.IsNullOrEmpty(x.Text) &&
                        x.Text.ToLower() != matchText.ToLower());

                    //Set as missing content file when the priority text file is not found.
                    var contentFiles = document.DocumentBinary.FileList.FindAll(x => x.Type == Constants.TEXT_FILE_TYPE);
                    if (!contentFiles.Any())
                    {
                        isMissingContent = true;
                        missingContentFiles.AddRange(from textFile in textFiles where !string.IsNullOrEmpty(textFile.Path) select textFile.Path);
                    }
                }
            }
            else
            {
                document.DocumentBinary.FileList.RemoveAll(x => x.Type == Constants.TEXT_FILE_TYPE);
            }
            return missingContentFiles;
        }

        /// <summary>
        /// Construct Log Data
        /// </summary>       
        private JobWorkerLog<LawImportLogInfo> ConstructLog(string correlationId, string docId,
                                                            string missingNativeFile, List<string> missingImageFiles,
                                                            bool isMissingContent,
                                                            IEnumerable<string> missingContentFiles,
                                                            Int32 importedImagesCount, string documentCtrlNbr,
                                                            string crossReferenceFieldValue)
        {
            try
            {
                docId.ShouldNotBe(null);
                var log = new JobWorkerLog<LawImportLogInfo>
                    {
                        JobRunId = (!string.IsNullOrEmpty(_jobRunId)) ? Convert.ToInt64(_jobRunId) : 0,
                        CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0,
                        WorkerInstanceId = _workerInstanceId,
                        WorkerRoleType = Constants.LawImportStartupWorkerRoleType,
                        Success = true,
                        CreatedBy = _jobParams.CreatedBy,
                        IsMessage = false,
                        LogInfo = new LawImportLogInfo()
                    };
                if (log.LogInfo.Message == null)
                    log.LogInfo.Message = string.Empty;

                log.LogInfo.Information = Constants.RecordParserSuccessMessage;
                if (null != missingNativeFile || missingImageFiles.Count > 0 || isMissingContent)
                {
                    log.Success = false;
                    log.LogInfo.Information = !string.IsNullOrEmpty(documentCtrlNbr)
                                                  ? string.Format(Constants.MissingFileMessage, documentCtrlNbr)
                                                  : Constants.MissingFiles;
                }

                if (null != missingNativeFile)
                {
                    log.LogInfo.Information += Constants.MissingNativeFileMessage + missingNativeFile;
                    log.LogInfo.Message = log.LogInfo.Message.Contains(Constants.MsgMissingNativeFiles)
                                              ? log.LogInfo.Message
                                              : log.LogInfo.Message + Constants.MsgMissingNativeFiles;
                    log.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                }

                foreach (var missingImageFile in missingImageFiles)
                {
                    log.LogInfo.Information += Constants.MissingImageFileMessage + missingImageFile;
                    log.LogInfo.Message = log.LogInfo.Message.Contains(Constants.MsgMissingImage)
                                              ? log.LogInfo.Message
                                              : log.LogInfo.Message + Constants.MsgMissingImage;
                    log.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                }

                foreach (var missingContentFile in missingContentFiles)
                {
                    log.LogInfo.Information += Constants.MissingContentFileMessage + missingContentFile;
                    log.LogInfo.Message = log.LogInfo.Message.Contains(Constants.MsgMissingContentFile)
                                              ? log.LogInfo.Message
                                              : log.LogInfo.Message + Constants.MsgMissingContentFile;
                    log.LogInfo.CrossReferenceField = crossReferenceFieldValue;
                }

                log.LogInfo.IsMissingImage = missingImageFiles.Count > 0;
                log.LogInfo.IsMissingNative = null != missingNativeFile;
                log.LogInfo.IsMissingText = isMissingContent;
                log.LogInfo.AddedDocument = 1;
                log.LogInfo.AddedImages = importedImagesCount;
                log.LogInfo.DocumentId = docId;
                log.LogInfo.DCN = documentCtrlNbr;
                return log;
            }
            catch (Exception ex)
            {
                throw new Exception(Constants.ErrorConstrcutLogData + ex.Message);
            }
        }

        /// <summary>
        /// Method to get the file Mime Type from file extension
        /// </summary>
        /// <param name="fileExt">The file ext.</param>
        /// <returns></returns>     
        public static string GetMimeType(string fileExt)
        {
            var fileTypeHelper = new FileMimeTypeHelper();
            return fileTypeHelper.GetMimeType(fileExt);
        }
    }
}
