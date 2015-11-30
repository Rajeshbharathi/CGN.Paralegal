#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="OverlayDocumentManager.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Baranitharan</author>
//      <description>
//          This file contains all overlay related helper methods
//      </description>
//      <changelog>
//          <date value="06/2/2012">Fix for Bugs 101490,94121,101319</date>
//          <date value="06-17-2013">Bug # 144954 - Fix to display dcn for image set documensts in manage conversion</date>
//          <date value="03/24/2014">ADM-Admin-008 buddy bug fix 166795 and 166796</date>
//          <date value="05/26/2014">Bug Fix # 168718 </date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//          <date value="02/20/2015">CNEV 4.0 - Devbug # - 184669 - Search Sub-system Replacement - Overlay custom field fix : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion Header

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Worker.Data;
using System.Threading.Tasks;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.Common;
using System.IO;

namespace LexisNexis.Evolution.Worker
{
    public class OverlayDocumentManager
    {
        private ImportBEO m_JobParameter;
        private DatasetBEO m_Dataset;
        private string m_JobRunId;
        private string m_WorkerInstanceId;

        public OverlayDocumentManager(ImportBEO jobParameter, DatasetBEO dataset, string jobRunId, string workerInstanceId)
        {
            m_JobParameter = jobParameter;
            m_Dataset = dataset;
            m_JobRunId = jobRunId;
            m_WorkerInstanceId = workerInstanceId;
        }


        /// <summary>
        /// Bulk search for entire batch document
        /// </summary>       
        public List<DocumentDetail> BulkSearch(List<DocumentDetail> docDetailsList, UserBusinessEntity userInfo,
            out List<JobWorkerLog<OverlaySearchLogInfo>> overlayLogList)
        {
            var documentDetailList = new List<DocumentDetail>();
            var logList = new List<JobWorkerLog<OverlaySearchLogInfo>>();
            var overlayUniqueThreadString = string.Empty;
            var searchQueryText = new StringBuilder();
            var outputFields = new List<Field>();
            var docCount = 0;
            #region Construct Bulk Query
            foreach (var doc in docDetailsList)
            {
                if (docCount == 0)
                {
                    outputFields.AddRange(doc.OverlayMatchingField.Select(field => new Field {FieldName = field.FieldName}));
                }

                docCount++;
                var overlayMatchingField = doc.OverlayMatchingField.Where(f => !string.IsNullOrEmpty(f.FieldValue)).ToList();
                if (!overlayMatchingField.Any()) continue;
                searchQueryText.Append(ConstructSearchQuery(overlayMatchingField.ToList()));
                if (docCount != docDetailsList.Count)
                    searchQueryText.Append(Constants.SearchORCondition);
            }
            var strSearchQueryText = searchQueryText.ToString();
            //To remove last OR Operator( " OR ") which added in search query.
            var searchOrOperatorLastIndex = searchQueryText.ToString().LastIndexOf(Constants.SearchORCondition);
            if (searchOrOperatorLastIndex != -1)
            {
                if ((searchQueryText.ToString().Length - searchOrOperatorLastIndex) == Constants.SearchORCondition.Length)
                {
                    strSearchQueryText = searchQueryText.ToString().Substring(0, searchOrOperatorLastIndex);
                }
            }

            #endregion

            #region Bulk Search
            ReviewerSearchResults bulkSearchresult;
            using (var transScope = new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                var searchHelper = new OverlaySearchHelper();
                bulkSearchresult = searchHelper.Search(strSearchQueryText, m_JobParameter.CollectionId,
                    m_JobParameter.DatasetId, m_JobParameter.MatterId, m_Dataset.Matter.MatterDBName, m_JobParameter.CreatedBy, userInfo, outputFields);
            }
            #endregion

            #region Construct Document From Search
            // DEBUG
            //docDetailsList.ForEach(docDetail =>
            Parallel.ForEach(docDetailsList, docDetail =>
            {
                JobWorkerLog<OverlaySearchLogInfo> overlayLog = null;
                string threadConstraint;
                var docs = ConstructDocumentFromSearch(docDetail, bulkSearchresult.ResultDocuments, out threadConstraint, out overlayLog);
                lock (documentDetailList)
                {
                    if (docs != null) documentDetailList.AddRange(docs);
                    if (overlayLog != null) logList.Add(overlayLog);
                    if (threadConstraint != string.Empty && overlayUniqueThreadString == string.Empty) overlayUniqueThreadString = threadConstraint;
                }
            });            
            overlayLogList = logList;
            #endregion

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
            List<string> docIdList = new List<string>();
            List<string> duplicateDocCorrelationIdList = new List<string>();
            var nativeDocDetailsList = docDetailsList.Where(d => d.docType == DocumentsetType.NativeSet);
            foreach (DocumentDetail docDetail in nativeDocDetailsList)
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
            if (duplicateDocCorrelationIdList.Count > 0)
            {
                foreach (string correlationId in duplicateDocCorrelationIdList)
                {
                    var duplicateDoc = docDetailsList.FirstOrDefault(m => m.CorrelationId == correlationId);
                    docDetailsList.RemoveAll(m => m.CorrelationId == correlationId); //Remove the document
                    var log = overlayLogList.FirstOrDefault(l => l.CorrelationId.ToString() == correlationId);
                    if (log != null)
                    {
                        log.Success = true;
                        log.IsMessage = true;
                        string documentControlNumber = string.Empty;
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
                                var field =
                                    duplicateDoc.document.FieldList.Where(f => f.FieldName == "DCN").FirstOrDefault();
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
            }
        }

        #region "Search Query"
        /// <summary>
        /// Construct search query for to perform search.
        /// </summary>
        /// <param name="matchingKeyField"></param>
        /// <returns></returns>
        private string ConstructSearchQuery(List<RVWDocumentFieldBEO> matchingKeyField)
        {
            string query = string.Empty;
            if (matchingKeyField != null && matchingKeyField.Count > 0)
            {
                // -- prefix and suffix the field value with quotes
                matchingKeyField.ForEach(f => f.FieldValue = string.Format("\"{0}\"", f.FieldValue));

                // -- formualte the search string combining with AND operator
                // Supply plain field name instead of transformed field name
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
            out string existingThreadConstraint, out JobWorkerLog<OverlaySearchLogInfo> overlayLog)
        {
            List<DocumentDetail> documentDetailList = new List<DocumentDetail>();
            existingThreadConstraint = string.Empty;
            #region Overlay
            string searchQuery = string.Empty;
            bool isExactMatch = false;
            bool isNoMatch = false;
            bool isNewRecord = false;
            string searchMessage = string.Empty;
            bool isImportDocument = true;
            bool isNewImageSet = true;
            string overlayField;
            string nonMatchingOverlayDCN = string.Empty;
            List<DocumentResult> documentResult = null;
            var misMatchedFields = new List<string>();
            var misMatchedFieldsMessage = new List<string>();
            string dcn = string.Empty;
            //Filter document from bulk Search Result
            if (bulkSearchresult != null && bulkSearchresult.Count > 0)
                documentResult = DocumentMatchFromBulkSearchResult(docDetail, bulkSearchresult);
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
                    foreach (var result in documentResult)
                    {
                        if (result.Fields == null || !result.Fields.Any()) continue;
                        var dcnField = result.Fields.FirstOrDefault(f => f.DataTypeId.Equals(Constants.DCNFieldType));
                        if (dcnField != null)
                        {
                            nonMatchingOverlayDCN = !string.IsNullOrEmpty(nonMatchingOverlayDCN) ? string.Format("{0},{1}", nonMatchingOverlayDCN, dcnField.Value) : dcnField.Value;

                        }
                    }

                    if (!string.IsNullOrEmpty(nonMatchingOverlayDCN))
                        nonMatchingOverlayDCN = " DCN:" + nonMatchingOverlayDCN;
                    else
                        searchMessage = Constants.MessageNoMatchRecord;

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
            overlayField = searchQuery.Replace(Constants.SearchAndCondition.Trim(), ",");
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
            else if (m_JobParameter.IsOverlayReplaceAndAppend)
            {
                //Insert as new record for non matching record
                isNewRecord = true;
            }
            else
            {
                // Not applicable for Insert/Update
                isImportDocument = false;
                isNewRecord = false;
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
            overlayLog = ConstructLog(docDetail.CorrelationId, true, isExactMatch, isNoMatch, isImportDocument, searchMessage, overlayField, nonMatchingOverlayDCN, docDetail.document.DocumentId, dcn, isNewRecord, docDetail.document.CrossReferenceFieldValue, misMatchedFields, misMatchedFieldsMessage);
            #endregion
            return documentDetailList;
        }

        /// <summary>
        /// Update Document For ExactMatch
        /// </summary>      
        private void UpdateDocumentForExactMatch(DocumentDetail docDetail, ref string existingThreadConstraint, ref bool isNewRecord, ref bool isNewImageSet, List<DocumentResult> documentResult, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            //Update record 
            docDetail.document.DocumentId = documentResult.First().DocumentID;
            docDetail.document.Id = Convert.ToInt32(documentResult.First().Id);

            DocumentAddExistingFields(docDetail, documentResult, misMatchedFields, misMatchedFieldsMessage);

            //Add Existing Native File Info during overlay , if Native file was not included during overlay
            if (m_JobParameter != null && !m_JobParameter.IsImportNativeFiles)
            {
                AddNativeFileInfo(docDetail.document);
            }

            existingThreadConstraint = documentResult.First().DocumentID.Substring(0, 32);
            isNewRecord = false; // Update existing document  
            isNewImageSet = m_JobParameter.IsNewImageSet;
            if (!m_JobParameter.IsNewImageSet)  //If Image is updated then check for each document (i.e ImageSet collection is already exist or not)
            {
                if (documentResult.FirstOrDefault() != null && documentResult.FirstOrDefault().Fields != null)
                {
                    var imageSetfields = documentResult.FirstOrDefault().Fields.FirstOrDefault(f => f.Name.ToLower() == EVSystemFields.ImageSets.ToLower());
                    if (imageSetfields != null && !string.IsNullOrEmpty(imageSetfields.Value))
                    {
                        if (!string.IsNullOrEmpty(m_JobParameter.ImageSetId))
                        {
                            if (imageSetfields.Value.ToLower().Contains(m_JobParameter.ImageSetId.ToLower())) //Exists then Update imageset document, Otherwise add image 
                            {
                                isNewImageSet = false;
                            }
                            else
                            {
                                isNewImageSet = true;
                            }
                        }
                    }
                    else  //If Imageset field is not exist in Fields List then there is no imageset
                    {
                        isNewImageSet = true;
                    }
                }
            }
        }

        /// <summary>
        /// Add Existing Native File Info during overlay , if Native file was not included
        /// </summary>       
        private void AddNativeFileInfo(RVWDocumentBEO document)
        {
            if (document != null)
            {
                var nativeFilePath = GetNativeFilePath(document.DocumentId);
                if (!string.IsNullOrEmpty(nativeFilePath))
                {
                    document.MimeType = GetMimeType(Path.GetExtension(nativeFilePath).Replace(".", ""));
                    document.FileName = Path.GetFileNameWithoutExtension(nativeFilePath);
                    document.NativeFilePath = nativeFilePath;
                    document.FileExtension = Path.GetExtension(nativeFilePath);
                    if (File.Exists(nativeFilePath))
                    {
                        //Calculating size of file in KB
                        FileInfo fileInfo = new FileInfo(nativeFilePath);
                        document.FileSize = (int)Math.Ceiling(fileInfo.Length / Constants.KBConversionConstant);
                        document.MD5HashValue = DocumentHashHelper.GetMD5HashValue(nativeFilePath);
                        document.SHAHashValue = DocumentHashHelper.GetSHAHashValue(nativeFilePath);
                    }
                }
            }
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
        ///   Get Native File Path
        /// </summary>       
        private String GetNativeFilePath(String documentId)
        {
            string nativeFile = string.Empty;
            var documentVaultManager = new DocumentVaultManager();
            using (EVTransactionScope transScope = new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                nativeFile = documentVaultManager.GetNativeFilePath(m_Dataset.Matter.FolderID,
                                                                    new Guid(m_JobParameter.CollectionId),
                                                                    documentId);
            }
            return nativeFile;
        }

        /// <summary>
        /// Add Existing Fields to document
        /// </summary>
        /// <param name="docDetail"></param>
        /// <param name="documentResult"></param>
        private void DocumentAddExistingFields(DocumentDetail docDetail, List<DocumentResult> documentResult, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            if (documentResult.First().Fields != null)
            {
                List<FieldResult> resultFields = documentResult.First().Fields;
                List<RVWDocumentFieldBEO> docFields = docDetail.document.FieldList;

                foreach (FieldResult resultField in resultFields)
                {
                    if (!docFields.Exists(f => f.FieldName.ToLower() == resultField.Name.ToLower()) && resultField.Name.ToLower() != "snippet")
                    {
                        if (m_Dataset.DatasetFieldList.Exists(f => f.Name.ToLower() == resultField.Name.ToLower() && !f.IsSystemField)
                        || resultField.Name.ToLower().Equals(EVSystemFields.LawDocumentId.ToLower()))
                        {
                            //Field id not availble in search result object, so get from dataset
                            int id = m_Dataset.DatasetFieldList.First(f => f.Name.ToLower() == resultField.Name.ToLower()).ID;
                            RVWDocumentFieldBEO field = new RVWDocumentFieldBEO();
                            field.FieldId = id;
                            field.FieldName = resultField.Name;
                            field.FieldValue = resultField.Value;
                            //Add Fields into document object
                            docDetail.document.FieldList.Add(field);
                            //Need to add log for Mismatch Field
                            //Need to add log for Mismatch Field only for Fields mapped during Overlay.
                            if (m_JobParameter.FieldMapping.Any() &&
                                m_JobParameter.FieldMapping.Exists(f => f.DatasetFieldName.ToLower().Equals(resultField.Name.ToLower())))
                            {
                                FieldValueValidation(field, misMatchedFields, misMatchedFieldsMessage);
                            }

                        }
                        else if (!m_Dataset.DatasetFieldList.Exists(f => f.Name.ToLower() == resultField.Name.ToLower()))  //Add Existing fields list - Need to reinsert during overlay
                        {
                            RVWDocumentFieldBEO field = new RVWDocumentFieldBEO();
                            field.FieldName = resultField.Name;
                            field.FieldValue = resultField.Value;
                            //Add Fields into document object
                            if (docDetail.OverlayReImportField == null)
                                docDetail.OverlayReImportField = new List<RVWDocumentFieldBEO>();
                            docDetail.OverlayReImportField.Add(field);

                        }
                    }

                    if (string.IsNullOrEmpty(resultField.Name) || string.IsNullOrEmpty(resultField.Value)) continue;
                    if (resultField.Name.ToLower().Equals(EVSystemFields.ReviewSetId.ToLower()))
                    {
                        if (docDetail.Reviewsets == null)
                        {
                            docDetail.Reviewsets = new List<string>();
                        }
                        docDetail.Reviewsets.Add(resultField.Value);
                    }
                    if (resultField.Name.ToLower().Equals(EVSystemFields.DcnField.ToLower()))
                        docDetail.document.DocumentControlNumber = resultField.Value;
                    if (string.Equals(resultField.Name, EVSystemFields.PagesNatives, StringComparison.CurrentCultureIgnoreCase))
                        docDetail.document.PagesNatives = resultField.Value;
                    if (string.Equals(resultField.Name, EVSystemFields.PagesImages, StringComparison.CurrentCultureIgnoreCase))
                        docDetail.document.PagesImages = resultField.Value;
                     if (string.Equals(resultField.Name, EVSystemFields.LawDocumentId, StringComparison.CurrentCultureIgnoreCase))
                         docDetail.document.LawDocumentId =  !string.IsNullOrEmpty(resultField.Value)?Convert.ToInt32(resultField.Value):0;

                }
            }
        }

        /// <summary>
        /// Construct Nativeset & ImageSet
        /// </summary>        
        private List<DocumentDetail> ConstructNativeImageSet(bool isNewRecord, bool isNewImageSet, DocumentDetail docDetail, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            List<DocumentDetail> documentDetailList = new List<DocumentDetail>();
            //1) Construct Native Set
            var nativeSetDocument = GetDocumentForNativeSet(docDetail.document, misMatchedFields, misMatchedFieldsMessage);
            var doc = new DocumentDetail();
            doc.CorrelationId = docDetail.CorrelationId;
            doc.docType = DocumentsetType.NativeSet;
            doc.document = nativeSetDocument;
            doc.ParentDocId = docDetail.ParentDocId;
            doc.IsNewDocument = isNewRecord;
            doc.ConversationIndex = docDetail.ConversationIndex;
            doc.OverlayReImportField = docDetail.OverlayReImportField;
            doc.Reviewsets = docDetail.Reviewsets;
            #region Check Same Content File
            string existingContentFilePath = GetExistingContentFile(nativeSetDocument.CollectionId, nativeSetDocument.DocumentId);
            string contentFilePath = string.Empty;
            if (nativeSetDocument.DocumentBinary.FileList != null && nativeSetDocument.DocumentBinary.FileList.Where(f => f.Type == Constants.TEXT_FILE_TYPE).FirstOrDefault() != null)
            {
                contentFilePath = (!string.IsNullOrEmpty(nativeSetDocument.DocumentBinary.FileList.Where(f => f.Type == Constants.TEXT_FILE_TYPE).FirstOrDefault().Path) ? nativeSetDocument.DocumentBinary.FileList.Where(f => f.Type == Constants.TEXT_FILE_TYPE).FirstOrDefault().Path : string.Empty);
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
                    RVWExternalFileBEO file = new RVWExternalFileBEO
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

            doc.SystemTags = m_JobParameter.SystemTags;
            doc.document.DocumentControlNumber = docDetail.document.DocumentControlNumber;

            documentDetailList.Add(doc);

            //2) Construct Image Set                       
            if (m_JobParameter.IsImportImages && !string.IsNullOrEmpty(m_JobParameter.ImageSetId))
            {
                var imageSetDocument = GetDocumentForImageSet(docDetail.document, m_JobParameter.ImageSetId);
                var docImg = new DocumentDetail();
                docImg.CorrelationId = docDetail.CorrelationId;
                docImg.docType = DocumentsetType.ImageSet;
                docImg.document = imageSetDocument;
                docImg.IsNewDocument = isNewImageSet;
                documentDetailList.Add(docImg);
            }
            return documentDetailList;
        }

        /// <summary>
        /// Overlay- Check ContentFile (i.e  same file as used during Append)
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="documentId"></param>
        /// <param name="textFilePath"></param>
        /// <param name="newTextFile"></param>
        /// <returns></returns>
        private string GetExistingContentFile(string collectionId, string documentId)
        {
            string contentFilePath = null;
            DocumentVaultManager docVaultManager = new DocumentVaultManager();
            var collectionGUILD = new Guid(collectionId);
            RVWDocumentBEO document = null;
            using (EVTransactionScope transScope = new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                document = docVaultManager.GetDocumentFileDetails(m_Dataset.Matter.FolderID, new Guid(collectionId), documentId, 1, string.Empty);
            }
            if (document != null && document.DocumentBinary != null && document.DocumentBinary.FileList != null && document.DocumentBinary.FileList.Count > 0)
            {
                var contentFile = document.DocumentBinary.FileList.Where(f => f.Type == Constants.ExtractedText_FILE_TYPE);
                if (contentFile != null && contentFile.FirstOrDefault() != null)
                {
                    contentFilePath = (!string.IsNullOrEmpty(contentFile.FirstOrDefault().Path) ? contentFile.FirstOrDefault().Path : string.Empty);
                }
            }
            return contentFilePath;
        }

        /// <summary>
        /// Match document fomr  bulk search result
        /// </summary>  
        private static List<DocumentResult> DocumentMatchFromBulkSearchResult(DocumentDetail document, List<DocumentResult> bulkSearchresult)
        {
            List<DocumentResult> resultDocuments = new List<DocumentResult>();
            if (bulkSearchresult != null && bulkSearchresult.Count > 0)
            {
                foreach (DocumentResult result in bulkSearchresult)
                {
                    List<RVWDocumentFieldBEO> docFields = document.OverlayMatchingField;
                    bool isMatch = false;
                    foreach (RVWDocumentFieldBEO field in docFields)
                    {
                        string value = field.FieldValue.Trim('"');
                        if (result.Fields.Exists(f => f.Name == field.FieldName && f.Value == value))
                            isMatch = true;
                        else
                            break;
                    }
                    if (isMatch)
                    {
                        resultDocuments.Add(result);
                    }
                }
            }
            return resultDocuments;
        }

        #region Parent Id
        /// <summary>
        /// Add key value pair into main list
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public Dictionary<string, string> AddKeyValues(Dictionary<string, string> source, Dictionary<string, string> collection)
        {
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    if (!string.IsNullOrEmpty(item.Key) && !string.IsNullOrEmpty(item.Value))
                    {
                        if (!source.ContainsKey(item.Key))
                        {
                            source.Add(item.Key, item.Value);
                        }
                    }
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
            RVWDocumentBEO nativeSetDocument = CopyDocumentObject(document, true);
            List<RVWExternalFileBEO> fileList = nativeSetDocument.DocumentBinary.FileList.Where(x => x.Type.ToLower() != Constants.IMAGE_FILE_TYPE.ToLower()).ToList();
            nativeSetDocument.DocumentBinary.FileList.Clear();
            if (fileList.Any())
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
                if (m_JobParameter.FieldMapping.Any() &&
                    m_JobParameter.FieldMapping.Exists(f => f.DatasetFieldName.ToLower().Equals(field.FieldName.ToLower())))
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
            documentsetDocument.PagesNatives = document.PagesNatives;
            documentsetDocument.PagesImages = document.PagesImages;
            documentsetDocument.CustomFieldToPopulateText = document.CustomFieldToPopulateText;
            documentsetDocument.LawDocumentId = document.LawDocumentId;
            documentsetDocument.IsImageFilesNotAssociated = document.IsImageFilesNotAssociated;
            if (isNativeDoc)
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
        /// <param name="document"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        private static RVWDocumentBEO GetDocumentForImageSet(RVWDocumentBEO document, string collectionId)
        {
            var imageSetDocument = CopyDocumentObject(document, false);
            if (imageSetDocument.DocumentBinary.FileList != null && imageSetDocument.DocumentBinary.FileList.Count > 0)
            {
                List<RVWExternalFileBEO> imagefileList = imageSetDocument.DocumentBinary.FileList.Where(x => x.Type.ToLower().Equals(Constants.IMAGE_FILE_TYPE.ToLower())).ToList();
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
            if (!string.IsNullOrEmpty(document.DocumentControlNumber))
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
            var overlayLog = new JobWorkerLog<OverlaySearchLogInfo>();
            overlayLog.CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0;
            overlayLog.JobRunId = (!string.IsNullOrEmpty(m_JobRunId)) ? Convert.ToInt64(m_JobRunId) : 0;
            overlayLog.WorkerInstanceId = m_WorkerInstanceId;
            overlayLog.Success = success;
            overlayLog.WorkerRoleType = Constants.OverlayWorkerRoleType; //Need to Add Role Type
            overlayLog.CreatedBy = m_JobParameter.CreatedBy;
            overlayLog.IsMessage = false;
            overlayLog.LogInfo = new OverlaySearchLogInfo();
            overlayLog.LogInfo.DCN = !string.IsNullOrEmpty(documentCtrlNbr) ? documentCtrlNbr : string.Empty;
            overlayLog.LogInfo.Message = string.Empty;
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
                    overlayLog.LogInfo.Message = !String.IsNullOrEmpty(nonMatchOverlayDCN)
                        ? Constants.MessageMoreThanOneRecord : Constants.MessageNoMatchRecord;
                    overlayLog.LogInfo.Information = !String.IsNullOrEmpty(nonMatchOverlayDCN)
                        ? Constants.ErrorMessageSearchNoMatch + nonMatchOverlayDCN
                        : Constants.ErrorMessageSearchNoMatch;
                }
                if (!overlayAction)
                    overlayLog.LogInfo.Information += Constants.OverlayNoActionMessage;
                overlayLog.LogInfo.IsDocumentAdded = isAppend; //Overlay Non matching record inserted into dataset
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
        #endregion

        private void FieldValueValidation(RVWDocumentFieldBEO documentField, List<string> misMatchedFields, List<string> misMatchedFieldsMessage)
        {
            FieldBEO field = m_Dataset.DatasetFieldList.FirstOrDefault(f => f.ID.Equals(documentField.FieldId));
            if (field != null)
            {
                var dataTypeId = (field.FieldType != null) ? field.FieldType.DataTypeId : 0;

                if (dataTypeId == Constants.DateFieldTypeId)
                {
                    DateTime dateFieldValue;
                    DateTime.TryParse(documentField.FieldValue, out dateFieldValue);
                    if (dateFieldValue == DateTime.MinValue || dateFieldValue == DateTime.MaxValue)
                    {
                        misMatchedFields.Add(string.Format(Constants.MisMatchedToWrongData, field.Name));
                        misMatchedFieldsMessage.Add(string.Format(Constants.MsgMismatchedFile, field.Name));
                    }
                }

            }
        }
    }
}
