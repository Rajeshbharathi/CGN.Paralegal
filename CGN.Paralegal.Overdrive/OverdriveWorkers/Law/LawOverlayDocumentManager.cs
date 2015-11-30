#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="LawOverlayDocumentManager.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Niranjan</author>
//      <description>
//          This file contains all overlay related helper methods
//      <date value="07/31/2013">Fix for 148855 - [CHEV 2.2.1][CR005] The modified _EVImportDescription value provided during law import job is not getting displayed in the table view : babugx</date>
//      </description>
//      <changelog>
//          <date value="03/19/2014">ADM-Admin-008 Overlay issue fix</date>
//          <date value="03/24/2014">ADM-Admin-008 buddy bug fix 166795 and 166796</date>
//          <date value="05/23/2014">Bug fix 169887, 170192 & 168709</date>
//          <date value="05/26/2014">Bug Fix # 168718 </date>
//          <date value="12/02/2014">Bug Fix # 180610 </date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion Header

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Worker;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.External.VaultManager;
using System.Globalization;
using LexisNexis.Evolution.Infrastructure.Common;
using System.IO;

namespace OverdriveWorkers.Law
{
    public class LawOverlayDocumentManager
    {
        private readonly LawImportBEO _jobParams;
        private readonly DatasetBEO _datasetBEO;
        private readonly string _jobRunId;
        private readonly string _workerInstanceId;
        private readonly Dictionary<string, string> _overlayNewAndOldDocumentIdPairs;

        public LawOverlayDocumentManager(LawImportBEO jobParams, DatasetBEO datasetBEO, string jobRunId, string workerInstanceId)
        {
            _jobParams = jobParams;
            _datasetBEO = datasetBEO;
            _jobRunId = jobRunId;
            _workerInstanceId = workerInstanceId;
            _overlayNewAndOldDocumentIdPairs = new Dictionary<string, string>();
        }


        /// <summary>
        /// Bulk search for entire batch document
        /// </summary>       
        public List<DocumentDetail> BulkSearch(List<DocumentDetail> docDetailsList, UserBusinessEntity userInfo,
            out List<JobWorkerLog<OverlaySearchLogInfo>> overlayLogList,
            out Dictionary<string, string> overlayDocumentIdPair)
        {
            var documentDetailList = new List<DocumentDetail>();
            var logList = new List<JobWorkerLog<OverlaySearchLogInfo>>();
            var overlayUniqueThreadString = string.Empty;
            overlayDocumentIdPair = new Dictionary<string, string>();
            var searchQueryText = new StringBuilder();
            var docCount = 0;
            var outputFields = new List<Field>();

            #region Construct Bulk Query

            foreach (var doc in docDetailsList)
            {
                if (docCount == 0)
                {
                    outputFields.AddRange(doc.OverlayMatchingField.Select(field => new Field { FieldName = field.FieldName }));
                }

                docCount++;
                searchQueryText.Append(ConstructSearchQuery(doc.OverlayMatchingField));
                if (docCount != docDetailsList.Count)
                    searchQueryText.Append(" OR ");
            }

            #endregion

            #region Bulk Search

            ReviewerSearchResults bulkSearchresult;
            using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                var searchHelper = new OverlaySearchHelper();
                bulkSearchresult = searchHelper.Search(searchQueryText.ToString(), _jobParams.CollectionId,
                                                       _jobParams.DatasetId, _jobParams.MatterId,
                                                       _datasetBEO.Matter.MatterDBName, _jobParams.CreatedBy, userInfo, outputFields);
            }

            #endregion

            #region Construct Document From Search

            foreach (var docDetail in docDetailsList)
            {
                JobWorkerLog<OverlaySearchLogInfo> overlayLog;
                string threadConstraint;
                var docs = ConstructDocumentFromSearch(docDetail, bulkSearchresult.ResultDocuments,
                                                       out threadConstraint,
                                                       out overlayLog);

                if (docs != null)
                    documentDetailList.AddRange(docs);
                if (overlayLog != null)
                    logList.Add(overlayLog);
                if (threadConstraint != string.Empty && overlayUniqueThreadString == string.Empty)
                    overlayUniqueThreadString = threadConstraint;
            }

            overlayLogList = logList;

            #endregion


            overlayDocumentIdPair = AddKeyValues(overlayDocumentIdPair, _overlayNewAndOldDocumentIdPairs);
            #region Find and Purify Duplicate match document

            RemoveDuplicateUpdateDocument(documentDetailList, overlayLogList);

            #endregion

            return documentDetailList;
        }

        /// <summary>
        /// Remove Duplicate Update Document
        /// </summary>  
        private static void RemoveDuplicateUpdateDocument(List<DocumentDetail> docDetailsList, List<JobWorkerLog<OverlaySearchLogInfo>> overlayLogList)
        {
            var docIdList = new List<string>();
            var duplicateDocCorrelationIdList = new List<string>();
            var nativeDocDetailsList = docDetailsList.Where(d => d.docType == DocumentsetType.NativeSet);
            foreach (var docDetail in nativeDocDetailsList)
            {
                if (!docIdList.Contains(docDetail.document.DocumentId))
                {
                    docIdList.Add(docDetail.document.DocumentId);
                }
                else
                {
                    duplicateDocCorrelationIdList.Add(docDetail.CorrelationId);
                }
            }
            if (duplicateDocCorrelationIdList.Count <= 0) return;
            foreach (var correlationId in duplicateDocCorrelationIdList)
            {
                var duplicateDoc = docDetailsList.FirstOrDefault(m => m.CorrelationId == correlationId);
                docDetailsList.RemoveAll(m => m.CorrelationId == correlationId); //Remove the document
                var log = overlayLogList.FirstOrDefault(l => l.CorrelationId.ToString(CultureInfo.InvariantCulture) == correlationId);
                if (log == null) continue;
                log.Success = true;
                log.IsMessage = true;
                var documentControlNumber = string.Empty;
                if (log.LogInfo == null)
                {
                    log.LogInfo = new OverlaySearchLogInfo();
                }
                log.LogInfo.IsDocumentUpdated = false;
                log.LogInfo.Information = Constants.ErrorMessageDuplicateSearch;

                if (duplicateDoc != null && duplicateDoc.document != null)
                {
                    log.LogInfo.CrossReferenceField = duplicateDoc.document.CrossReferenceFieldValue;
                    if (duplicateDoc.document.FieldList != null)
                    {
                        var field = duplicateDoc.document.FieldList.FirstOrDefault(f => f.FieldName == "DCN");
                        if (field != null)
                        {
                            documentControlNumber = field.FieldValue;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(documentControlNumber))
                {
                    log.LogInfo.Message = Constants.MsgOverlayMisMatch + " For DCN: " + documentControlNumber;
                }
                else
                {
                    log.LogInfo.Message = Constants.MsgOverlayMisMatch;
                }
            }
        }

        #region "Search Query"

        /// <summary>
        /// Construct search query for Search.
        /// </summary>
        /// <param name="matchingKeyField"></param>
        /// <returns></returns>
        private static string ConstructSearchQuery(List<RVWDocumentFieldBEO> matchingKeyField)
        {
            var query = string.Empty;
            if (matchingKeyField != null && matchingKeyField.Count > 0)
            {
                // -- prefix and suffix the field value with quotes
                matchingKeyField.ForEach(f => f.FieldValue = string.Format("\"{0}\"", f.FieldValue));

                // -- formualte the search string combining with AND operator
                query = String.Join(" AND ", matchingKeyField.Select
                    (s => String.Format("{0}:{1}", s.FieldName, s.FieldValue)).ToArray());
            }

            query = (query != string.Empty) ? string.Format("({0})", query) : string.Empty;
            return query;
        }

        #endregion

        /// <summary>
        /// Construct document based on search 
        /// </summary>      
        public List<DocumentDetail> ConstructDocumentFromSearch(DocumentDetail docDetail, List<DocumentResult> bulkSearchresult,
            out string existingThreadConstraint,
            out JobWorkerLog<OverlaySearchLogInfo> overlayLog)
        {
            var documentDetailList = new List<DocumentDetail>();
            existingThreadConstraint = string.Empty;
            #region Overlay
            var searchQuery = string.Empty;
            var isExactMatch = false;
            var isNoMatch = false;
            var isNewRecord = false;
            var searchMessage = string.Empty;
            var isImportDocument = true;
            var isNewImageSet = true;
            var nonMatchingOverlayDCN = string.Empty;
            List<DocumentResult> documentResult = null;
            var misMatchedFields = new List<string>();
            var misMatchedFieldsMessage = new List<string>();
            var dcn = string.Empty;
            //Filter document from bulk Search Result
            if (bulkSearchresult != null && bulkSearchresult.Any())
                documentResult = DocumentMatchFromBulkSearchReult(docDetail, bulkSearchresult);

            if (documentResult != null)
            {
                if (documentResult.Count == 1)
                {
                    isExactMatch = true;
                    searchMessage = Constants.MessageMatchRecord;
                }
                else
                {
                    isNoMatch = true;
                    #region Log
                    searchMessage = (documentResult.Count == 0) ? Constants.MessageNoMatchRecord : Constants.MessageMoreThanOneRecord;
                    //Return more than one record
                    if (documentResult.Count > 0)
                    {
                        nonMatchingOverlayDCN = string.Join(",", documentResult.Select(x => x.DocumentControlNumber).ToArray());
                    }
                    #endregion
                }
            }
            else
            {
                //No Match Found
                isNoMatch = true;
            }

            #region Log
            if (docDetail.OverlayMatchingField != null)
                searchQuery = ConstructSearchQuery(docDetail.OverlayMatchingField);
            var overlayField = searchQuery.Replace(Constants.SearchAndCondition.Trim(), ",");
            #endregion
            if (isExactMatch)
            {
                UpdateDocumentForExactMatch(docDetail, ref existingThreadConstraint, ref isNewRecord, ref isNewImageSet, documentResult, misMatchedFields, misMatchedFieldsMessage);
                var dcnField = documentResult.First().Fields.FirstOrDefault(f => f.DataTypeId == Constants.DCNFieldType);
                if (dcnField != null)
                {
                    if (docDetail.document != null) docDetail.document.DocumentControlNumber = dcnField.Value;
                    dcn = dcnField.Value;
                }
            }
            else if (_jobParams.ImportOptions == ImportOptionsBEO.AppendAndReplaceMatching)
            {
                //Insert as new record for non matching record
                isNewRecord = true;
                #region "RelationShip"

                if (docDetail.docType == DocumentsetType.NativeSet)
                {
                    // <Newly generated Id>,<Old Id>
                    if (!_overlayNewAndOldDocumentIdPairs.Keys.Contains(docDetail.document.DocumentId))
                    {
                        _overlayNewAndOldDocumentIdPairs.Add(docDetail.document.DocumentId, docDetail.document.DocumentId);
                    }
                }

                #endregion
            }
            else if (_jobParams.ImportOptions != ImportOptionsBEO.AppendAndReplaceMatching)
            {
                // Not applicable for Insert/Update
                isImportDocument = false;
            }

            #endregion

            #region Construct Documents

            if (isImportDocument)
            {
                var docdetails = ConstructNativeImageSet(isNewRecord, isNewImageSet, docDetail, misMatchedFields, misMatchedFieldsMessage);
                if (docdetails != null && docdetails.Count > 0)
                {
                    documentDetailList.AddRange(docdetails);
                }
            }
            if (string.IsNullOrEmpty(dcn))
                dcn = docDetail.document.DocumentControlNumber;
            //3) Construct Log          
            overlayLog = ConstructLog(docDetail.CorrelationId, true, isExactMatch, isNoMatch, isImportDocument,
                                      searchMessage, overlayField, nonMatchingOverlayDCN, docDetail.document.DocumentId,
                                      dcn, isNewRecord, docDetail.document.CrossReferenceFieldValue, misMatchedFields,
                                      misMatchedFieldsMessage);

            #endregion

            return documentDetailList;
        }

        /// <summary>
        /// Update Document For ExactMatch
        /// </summary>      
        private void UpdateDocumentForExactMatch(DocumentDetail docDetail, ref string existingThreadConstraint, ref bool isNewRecord, ref bool isNewImageSet, List<DocumentResult> documentResult, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            if (existingThreadConstraint == null) throw new ArgumentNullException("existingThreadConstraint");
            //Update record 
            #region "RelationShip"

            if (docDetail.docType == DocumentsetType.NativeSet)
            {
                // <Newly generated Id>,<Old Id>
                if (!_overlayNewAndOldDocumentIdPairs.Keys.Contains(docDetail.document.DocumentId))
                {
                    _overlayNewAndOldDocumentIdPairs.Add(docDetail.document.DocumentId, documentResult.First().DocumentID);
                }
            }

            #endregion

            docDetail.document.Id = Convert.ToInt32(documentResult.First().Id);
            docDetail.document.DocumentId = documentResult.First().DocumentID;
            DocumentAddExistingFields(docDetail, documentResult, misMatchedFields, misMatchedFieldsMessage);

            //Add Existing Native File Info during overlay , if Native file was not included during overlay
            if (!_jobParams.IsImportNative)
            {
                AddNativeFileInfo(docDetail.document);
            }

            existingThreadConstraint = documentResult.First().DocumentID.Substring(0, 32);
            isNewRecord = false; // Update existing document  
            isNewImageSet = !_jobParams.IsExistingImageSet;
            if (isNewImageSet) return;
            var firstOrDefault = documentResult.FirstOrDefault();
            if (firstOrDefault == null || (firstOrDefault.Fields == null)) return;
            var imageSetfields = firstOrDefault.Fields.FirstOrDefault(f => f.Name.ToLower() == EVSystemFields.ImageSets.ToLower());
            if (imageSetfields != null && !string.IsNullOrEmpty(imageSetfields.Value))
            {
                if (!string.IsNullOrEmpty(_jobParams.ImageSetId))
                {
                    isNewImageSet = !imageSetfields.Value.ToLower().Contains(_jobParams.ImageSetId.ToLower());
                }
            }
            else  //If Imageset field is not exist in Fields List then there is no imageset
            {
                isNewImageSet = true;
            }
        }

        /// <summary>
        /// Add Existing Native File Info during overlay , if Native file was not included
        /// </summary>       
        private void AddNativeFileInfo(RVWDocumentBEO document)
        {
            if (document == null) return;
            var nativeFilePath = GetNativeFilePath(document.DocumentId);
            if (string.IsNullOrEmpty(nativeFilePath)) return;
            var extension = Path.GetExtension(nativeFilePath);
            if (extension != null)
                document.MimeType = GetMimeType(extension.Replace(".", ""));
            document.FileName = Path.GetFileNameWithoutExtension(nativeFilePath);
            document.NativeFilePath = nativeFilePath;
            document.FileExtension = Path.GetExtension(nativeFilePath);
            if (!File.Exists(nativeFilePath)) return;
            //Calculating size of file in KB
            var fileInfo = new FileInfo(nativeFilePath);
            document.FileSize = (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);
            document.MD5HashValue = DocumentHashHelper.GetMD5HashValue(nativeFilePath);
            document.SHAHashValue = DocumentHashHelper.GetSHAHashValue(nativeFilePath);
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


        /// <summary>
        ///   Get Native File Path
        /// </summary>       
        private string GetNativeFilePath(String documentId)
        {
            string nativeFile;
            var documentVaultManager = new DocumentVaultManager();
            using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                nativeFile = documentVaultManager.GetNativeFilePath(_datasetBEO.Matter.FolderID,
                                                                    new Guid(_jobParams.CollectionId),
                                                                    documentId);
            }
            return nativeFile;
        }

        /// <summary>
        /// Add Existing Fields to document
        /// </summary>
        /// <param name="docDetail"></param>
        /// <param name="documentResult"></param>
        /// <param name="misMatchedFields"></param>
        /// <param name="misMatchedFieldsMessage"></param>
        private void DocumentAddExistingFields(DocumentDetail docDetail, List<DocumentResult> documentResult, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            if (documentResult.First().Fields == null) return;
            var resultFields = documentResult.First().Fields;
            var docFields = docDetail.document.FieldList;

            foreach (var resultField in resultFields.Where(resultField => !docFields.Exists(f => f.FieldName.ToLower() == resultField.Name.ToLower()) && resultField.Name.ToLower() != "snippet"))
            {
                if (_datasetBEO.DatasetFieldList.Exists(f => f.Name.ToLower() == resultField.Name.ToLower() && !f.IsSystemField))
                {
                    //Field id not availble in search result object, so get from datasetBEO
                    var id = _datasetBEO.DatasetFieldList.First(f => f.Name.ToLower() == resultField.Name.ToLower()).ID;
                    var field = new RVWDocumentFieldBEO
                        {
                            FieldId = id,
                            FieldName = resultField.Name,
                            FieldValue = resultField.Value
                        };
                    //Add Fields into document object
                    docDetail.document.FieldList.Add(field);
                    //Need to add log for Mismatch Field only for Fields mapped during Overlay.
                    if (_jobParams.MappingFields.Any() &&
                        _jobParams.MappingFields.Exists(f => f.Name.ToLower().Equals(resultField.Name.ToLower())))
                    {
                        FieldValueValidation(field, misMatchedFields, misMatchedFieldsMessage);
                    }

                }
                else if (!_datasetBEO.DatasetFieldList.Exists(f => f.Name.ToLower() == resultField.Name.ToLower()))  //Add Existing fields list - Need to reinsert during overlay
                {
                    var field = new RVWDocumentFieldBEO { FieldName = resultField.Name, FieldValue = resultField.Value };
                    //Add Fields into document object
                    if (docDetail.OverlayReImportField == null)
                        docDetail.OverlayReImportField = new List<RVWDocumentFieldBEO>();
                    docDetail.OverlayReImportField.Add(field);

                }

                //Filling DCN field for overlay
                if (resultField.DataTypeId == Constants.DCNFieldType && !string.IsNullOrEmpty(resultField.Value))
                    docDetail.document.DocumentControlNumber = resultField.Value;

                if (string.Equals(resultField.Name, EVSystemFields.PagesNatives, StringComparison.CurrentCultureIgnoreCase))
                    docDetail.document.PagesNatives = resultField.Value;
                if (string.Equals(resultField.Name, EVSystemFields.PagesImages, StringComparison.CurrentCultureIgnoreCase))
                    docDetail.document.PagesImages = resultField.Value;
            }
        }

        /// <summary>
        /// Construct Nativeset & ImageSet
        /// </summary>        
        private List<DocumentDetail> ConstructNativeImageSet(bool isNewRecord, bool isNewImageSet, DocumentDetail docDetail, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            var documentDetailList = new List<DocumentDetail>();
            //1) Construct Native Set
            var nativeSetDocument = GetDocumentForNativeSet(docDetail.document, misMatchedFields, misMatchedFieldsMessage);
            var doc = new DocumentDetail
                {
                    CorrelationId = docDetail.CorrelationId,
                    docType = DocumentsetType.NativeSet,
                    document = nativeSetDocument,
                    ParentDocId = docDetail.ParentDocId,
                    IsNewDocument = isNewRecord,
                    ConversationIndex = docDetail.ConversationIndex,
                    OverlayReImportField = docDetail.OverlayReImportField
                };

            #region Check Same Content File

            var existingContentFilePath = GetExistingContentFile(nativeSetDocument.CollectionId, nativeSetDocument.DocumentId);
            var contentFilePath = string.Empty;
            if (nativeSetDocument.DocumentBinary.FileList != null && nativeSetDocument.DocumentBinary.FileList.FirstOrDefault(f => f.Type == Constants.TEXT_FILE_TYPE) != null)
            {
                var rvwExternalFileBEO = nativeSetDocument.DocumentBinary.FileList.FirstOrDefault(f => f.Type == Constants.TEXT_FILE_TYPE);
                contentFilePath = (rvwExternalFileBEO != null && !string.IsNullOrEmpty(rvwExternalFileBEO.Path) ? rvwExternalFileBEO.Path : string.Empty);
            }
            if (!string.IsNullOrEmpty(existingContentFilePath)) //If text File is exist during Append 
            {

                if (!string.IsNullOrEmpty(contentFilePath))
                {
                    if (existingContentFilePath.Trim().ToLower() != contentFilePath.Trim().ToLower()) //New Content File Used in Overlay
                    {
                        doc.OverlayIsNewContentFile = true;
                    }
                }
                else  //If there is no text File during overlay , Need to maintain old content File
                {
                    doc.OverlayIsNewContentFile = false;
                    var file = new RVWExternalFileBEO
                                {
                                    Type = Constants.TEXT_FILE_TYPE,
                                    Path = existingContentFilePath
                                };
                    doc.document.DocumentBinary.FileList.Add(file);
                }
            }
            else if (string.IsNullOrEmpty(nativeSetDocument.CustomFieldToPopulateText) && !string.IsNullOrEmpty(contentFilePath)) //If there is no text File during Append 
            {
                doc.OverlayIsNewContentFile = true;
            }
            else if (!string.IsNullOrEmpty(contentFilePath))
            {
                doc.OverlayIsNewContentFile = false;
            }
            #endregion

            documentDetailList.Add(doc);

            //2) Construct Image Set                       
            if (_jobParams.IsImportImages && !string.IsNullOrEmpty(_jobParams.ImageSetId))
            {
                var imageSetDocument = GetDocumentForImageSet(docDetail.document, _jobParams.ImageSetId);
                var docImg = new DocumentDetail
                    {
                        CorrelationId = docDetail.CorrelationId,
                        docType = DocumentsetType.ImageSet,
                        document = imageSetDocument,
                        IsNewDocument = isNewImageSet
                    };
                documentDetailList.Add(docImg);
            }
            return documentDetailList;
        }

        /// <summary>
        /// Overlay- Check ContentFile (i.e  same file as used during Append)
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="documentId"></param>
        /// <returns></returns>
        private string GetExistingContentFile(string collectionId, string documentId)
        {
            string contentFilePath = null;
            var docVaultManager = new DocumentVaultManager();
            RVWDocumentBEO document;
            using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                document = docVaultManager.GetDocumentFileDetails(_datasetBEO.Matter.FolderID, new Guid(collectionId), documentId, 1, string.Empty);
            }
            if (document != null && document.DocumentBinary != null && document.DocumentBinary.FileList != null && document.DocumentBinary.FileList.Count > 0)
            {
                var contentFile = document.DocumentBinary.FileList.Where(f => f.Type == Constants.ExtractedText_FILE_TYPE);
                var rvwExternalFileBeos = contentFile as RVWExternalFileBEO[] ?? contentFile.ToArray();
                if (rvwExternalFileBeos.FirstOrDefault() != null)
                {
                    contentFilePath = (!string.IsNullOrEmpty(rvwExternalFileBeos.First().Path) ? rvwExternalFileBeos.First().Path : string.Empty);
                }
            }
            return contentFilePath;
        }

        /// <summary>
        /// Match document fomr  bulk search result
        /// </summary>  
        private static List<DocumentResult> DocumentMatchFromBulkSearchReult(DocumentDetail document,
                                                                             ICollection<DocumentResult>
                                                                                 bulkSearchresult)
        {
            var resultDocuments = new List<DocumentResult>();
            if (bulkSearchresult != null && bulkSearchresult.Count > 0)
            {
                var matchingField = document.OverlayMatchingField.First();
                if (null == matchingField) return resultDocuments;
                var value = matchingField.FieldValue.Trim('"');
                foreach (var result in bulkSearchresult)
                {
                    var isMatch = false;
                    if (result.Fields.Exists(f => f.Name == matchingField.FieldName && f.Value == value))
                    {
                        isMatch = true;
                        //set document file size
                        var fileSizeField = result.Fields.Find(f => f.Name.Equals("_EVFileSize"));
                        if (fileSizeField != null)
                        {
                            int fileSize;
                            int.TryParse(fileSizeField.Value, out fileSize);
                            if (fileSize > 0) //Don't override the file size if native file not imported while append.
                                document.document.FileSize = fileSize;
                        }
                    }
                    if (isMatch) resultDocuments.Add(result);
                }
            }
            return resultDocuments;
        }

        #region Parent Id

        /// <summary>
        /// Add key value pair into main list
        /// </summary>
        /// <param name="source"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public Dictionary<string, string> AddKeyValues(Dictionary<string, string> source, Dictionary<string, string> collection)
        {
            if (collection != null)
            {
                foreach (var item in collection.Where(item => !string.IsNullOrEmpty(item.Key) && !string.IsNullOrEmpty(item.Value)).Where(item => !source.ContainsKey(item.Key)))
                {
                    source.Add(item.Key, item.Value);
                }
            }
            return source;
        }
        #endregion

        #region DocumentSet

        /// <summary>
        /// Clear image file information in DocumentBEO 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="misMatchedFields"></param>
        /// <param name="misMatchedFieldsMessage"></param>
        /// <returns></returns>
        private RVWDocumentBEO GetDocumentForNativeSet(RVWDocumentBEO document, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            var nativeSetDocument = CopyDocumentObject(document, true);
            var fileList = nativeSetDocument.DocumentBinary.FileList.Where(x => x.Type.ToLower() != Constants.IMAGE_FILE_TYPE.ToLower()).ToList();
            nativeSetDocument.DocumentBinary.FileList.Clear();
            if (fileList.Count > 0)
            {
                fileList.ForEach(x => nativeSetDocument.DocumentBinary.FileList.Add(x));
            }
            if (document.DocumentBinary != null)
            {
                if (!string.IsNullOrEmpty(document.DocumentBinary.Content))  //Assign Content Value
                {
                    nativeSetDocument.DocumentBinary.Content = document.DocumentBinary.Content;
                }
            }

            //Need to add log for Mismatch Field
            foreach (var field in document.FieldList)
            {
                if (_jobParams.MappingFields.Any() &&
                    _jobParams.MappingFields.Exists(f => f.Name.ToLower().Equals(field.FieldName.ToLower())))
                {
                    FieldValueValidation(field, misMatchedFields, misMatchedFieldsMessage);
                }
            }

            return nativeSetDocument;
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
                    DocumentId = document.DocumentId,
                    MimeType = document.MimeType,
                    FileName = document.FileName,
                    NativeFilePath = document.NativeFilePath,
                    FileExtension = document.FileExtension,
                    FileSize = document.FileSize,
                    DocumentControlNumber = document.DocumentControlNumber,
                    LawDocumentId = document.LawDocumentId,
                    ImportDescription = document.ImportDescription,
                    CustomFieldToPopulateText = document.CustomFieldToPopulateText
                };
            if (isNativeDoc)
            {
                documentsetDocument.EVLoadFileDocumentId = document.EVLoadFileDocumentId;
                documentsetDocument.EVLoadFileParentId = document.EVLoadFileParentId;
                documentsetDocument.EVInsertSystemRelationShipFields = document.EVInsertSystemRelationShipFields;
            }
            if (!string.IsNullOrEmpty(document.MD5HashValue)) documentsetDocument.MD5HashValue = document.MD5HashValue;
            if (!string.IsNullOrEmpty(document.SHAHashValue)) documentsetDocument.SHAHashValue = document.SHAHashValue;
            if (!string.IsNullOrEmpty(document.ImportMessage)) documentsetDocument.ImportMessage = document.ImportMessage;
            if (!string.IsNullOrEmpty(document.PagesNatives)) documentsetDocument.PagesNatives = document.PagesNatives;
            if (!string.IsNullOrEmpty(document.PagesImages)) documentsetDocument.PagesImages = document.PagesImages;
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

        /// <summary>
        ///  Clear except image file information in DocumentBEO 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        private static RVWDocumentBEO GetDocumentForImageSet(RVWDocumentBEO document, string collectionId)
        {
            var imageSetDocument = CopyDocumentObject(document, false);
            if (imageSetDocument.DocumentBinary.FileList != null && imageSetDocument.DocumentBinary.FileList.Count > 0)
            {
                var imagefileList = imageSetDocument.DocumentBinary.FileList.Where(
                    x => x.Type.ToLower().Equals(Constants.IMAGE_FILE_TYPE.ToLower())
                    && !string.IsNullOrEmpty(x.Path)
                    ).ToList();
                imageSetDocument.DocumentBinary.FileList.Clear();
                imagefileList.ForEach(x => imageSetDocument.DocumentBinary.FileList.Add(x));
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
            imageSetDocument.FileSize = 0;
            imageSetDocument.DocumentControlNumber = document.DocumentControlNumber;

            return imageSetDocument;
        }

        #endregion

        #region Log

        /// <summary>
        /// Construct Log Data
        /// </summary>     
        public JobWorkerLog<OverlaySearchLogInfo> ConstructLog(string correlationId, bool success)
        {
            return ConstructLog(correlationId, success, false, false, false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty, null, null);
        }

        /// <summary>
        /// Construct Log Data
        /// </summary>       
        private JobWorkerLog<OverlaySearchLogInfo> ConstructLog(string correlationId, bool success, bool isExactMatch, bool isNoMatch, bool overlayAction, string searchMessage, string overlayField, string nonMatchOverlayDCN, string overlayDocumentIdPair, string documentCtrlNbr, bool isAppend, string crossReferenceValue, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            try
            {
                var overlayLog = new JobWorkerLog<OverlaySearchLogInfo>
                    {
                        CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0,
                        JobRunId = (!string.IsNullOrEmpty(_jobRunId)) ? Convert.ToInt64(_jobRunId) : 0,
                        WorkerInstanceId = _workerInstanceId,
                        Success = success,
                        WorkerRoleType = Constants.OverlayWorkerRoleType,
                        CreatedBy = _jobParams.CreatedBy,
                        IsMessage = false,
                        LogInfo = new OverlaySearchLogInfo
                            {
                                DCN = !string.IsNullOrEmpty(documentCtrlNbr) ? documentCtrlNbr : string.Empty,
                                Message = string.Empty
                            }
                    };
                if (success)
                {
                    if (isExactMatch)
                        overlayLog.LogInfo.OverlayDocumentId = overlayDocumentIdPair;
                    overlayLog.LogInfo.IsDocumentUpdated = isExactMatch;
                    overlayLog.LogInfo.IsNoMatch = isNoMatch;
                    overlayLog.LogInfo.OverlayFieldInfo = overlayField;
                    overlayLog.LogInfo.SearchMessage = searchMessage;
                    overlayLog.LogInfo.Information = Constants.OverlaySuccessMessage;
                    if (isNoMatch)
                    {
                        overlayLog.LogInfo.NonMatchOverlayDCN = nonMatchOverlayDCN;
                        overlayLog.IsMessage = true;
                        overlayLog.LogInfo.CrossReferenceField = crossReferenceValue;
                        overlayLog.LogInfo.Message = Constants.MessageNoMatchRecord;
                        overlayLog.LogInfo.Information = Constants.ErrorMessageSearchNoMatch;
                    }
                    if (!overlayAction)
                        overlayLog.LogInfo.Information += Constants.OverlayNoActionMessage;
                    overlayLog.LogInfo.IsDocumentAdded = isAppend; //Overlay Non matching record inserted into datasetBEO
                }
                else
                {
                    overlayLog.LogInfo.Information = Constants.OverlayFailureMessage;
                    overlayLog.LogInfo.CrossReferenceField = crossReferenceValue;
                    overlayLog.LogInfo.Message = Constants.MsgDocumentSearch;
                }

                if (misMatchedFields != null && misMatchedFields.Count > 0)
                {
                    overlayLog.IsMessage = true;
                    if (!string.IsNullOrEmpty(documentCtrlNbr))
                    {
                        overlayLog.LogInfo.Information += string.Format(Constants.MisMatchedFieldMessage, documentCtrlNbr);
                    }
                    overlayLog.LogInfo.CrossReferenceField = crossReferenceValue;
                    misMatchedFields.ForEach(x => overlayLog.LogInfo.Information += x);
                    if (misMatchedFieldsMessage != null)
                    {
                        misMatchedFieldsMessage.ForEach(
                            x =>
                            overlayLog.LogInfo.Message =
                            overlayLog.LogInfo.Message.Contains(x)
                                ? overlayLog.LogInfo.Message
                                : overlayLog.LogInfo.Message + x);
                    }
                }
                return overlayLog;
            }
            catch (Exception ex)
            {
                throw new Exception(Constants.ErrorConstrcutLogData + ex.Message);
            }
        }

        #endregion

        private void FieldValueValidation(RVWDocumentFieldBEO documentField, ICollection<string> misMatchedFields, ICollection<string> misMatchedFieldsMessage)
        {
            var field = _datasetBEO.DatasetFieldList.FirstOrDefault(f => f.ID.Equals(documentField.FieldId));
            if (field == null) return;
            var dataTypeId = (field.FieldType != null) ? field.FieldType.DataTypeId : 0;

            if (dataTypeId != Constants.DateFieldTypeId) return;
            DateTime dateFieldValue;
            DateTime.TryParse(documentField.FieldValue, out dateFieldValue);
            if (dateFieldValue != DateTime.MinValue && dateFieldValue != DateTime.MaxValue) return;
            misMatchedFields.Add(string.Format(Constants.MisMatchedToWrongData, field.Name));
            misMatchedFieldsMessage.Add(string.Format(Constants.MsgMismatchedFile, field.Name));
        }
    }
}
