//-----------------------------------------------------------------------------------------
// <copyright file="ExportMetadataWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil Paramathma</author>
//      <description>
//          Class file for holding document detail extraction methods for export
//      </description>
//      <changelog>//          
//          <date value="16-Feb-2012">Fix for bug 95511</date>
//          <date value="04/23/2013">ADM-Export-003 Recreate Family Group</date>
//          <date value="06/04/2013">BugFix#144005</date>
//          <date value="07/19/2013">BugFix 146819, 144007, 147272, 146007</date>
//          <date value="12/20/2013">3.0 ADM-Export-007 Export Family Group</date>
//          <date value="01/02/2014">Task 159667 - ADM-EXPORT-005</date>
//          <date value="01/29/2014">Task 161755 - ADM-EXPORT-006</date>
//          <date value="02/05/2014">Task 162438 ADM-EXPORT-006 Integration testing issue fix</date>
//          <date value="03/25/2014">Task 163335 - Dev Testing - Avoid audit log call for GetDocumentData</date>
//          <date value="04/11/2014">Bug Fix # 168133, 168058 & 168087</date>
//          <date value="05/08/2014">Bug Fix # 169088, 169090</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------


using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LexisNexis.Evolution.Worker
{


    public class ExportMetadataWorker : WorkerBase
    {
        private ExportLoadJobDetailBEO _exportLoadJobDetailBeo;
        private long matterId;
        private DatasetBEO _dataset;
        private Dictionary<string, Tuple<string, string>> _beginsEnds; // DocId to [begin, end]
        private const string BeginsEndsPropertyName = "BeginsAndEnds";
        private const int ContentFieldTypeID = 2000;
      

        #region Overdrive

        protected override void BeginWork()
        {
            //Tracer.Debug("BeginWork started");
            base.BeginWork();
            _exportLoadJobDetailBeo = Utils.SmartXmlDeserializer(BootParameters) as ExportLoadJobDetailBEO;
            Debug.Assert(_exportLoadJobDetailBeo != null, "exportLoadJobDetailBEO != null");
           
            _dataset = DataSetBO.GetDatasetDetailsWithMatterInfo(Convert.ToInt64(_exportLoadJobDetailBeo.DatasetId),
                                                 _exportLoadJobDetailBeo.MatterId);
            matterId = _dataset.Matter.FolderID;

            if (_exportLoadJobDetailBeo.RecreateFamilyGroup)
            {
                PipelineProperty pipelineProperty = GetPipelineSharedProperty(BeginsEndsPropertyName);
                lock (pipelineProperty)
                {
                    if (pipelineProperty.Value == null)
                    {
                        //Tracer.Debug("CalculateBeginsAndEnds");
                        var endNumber = !string.IsNullOrEmpty(_exportLoadJobDetailBeo.FamilyEndNumberField) ? _exportLoadJobDetailBeo.FamilyEndNumberField : _exportLoadJobDetailBeo.FamilyBegNumberField;
                        pipelineProperty.Value = CalculateBeginsAndEnds(_exportLoadJobDetailBeo.FamilyBegNumberField, endNumber);
                    }
                    _beginsEnds = pipelineProperty.Value as Dictionary<string, Tuple<string, string>>;
                }
            }
            //Tracer.Debug("BeginWork completed");
        }

        protected override void EndWork()
        {
            base.EndWork();

            if (_exportLoadJobDetailBeo.RecreateFamilyGroup)
            {
                ReleasePipelineSharedProperty(BeginsEndsPropertyName);
            }
        }

        private Dictionary<string, Tuple<string, string>> CalculateBeginsAndEnds(string familyBegNumberField, string familyEndNumberField)
        {
            IEnumerable<RecreateFamilyEntity> familiesData = DocumentBO.GetBeginDocEndDocForDocument(matterId, _dataset.CollectionId, familyBegNumberField, familyEndNumberField)
                .OrderByDescending(t => t.FieldValue);

            Dictionary<string, RecreateFamilyEntity> beginnings = new Dictionary<string, RecreateFamilyEntity>(); // DocId to BEGDOC RecreateFamilyEntity
            Dictionary<string, string> endings = new Dictionary<string, string>(); // DocId to FieldValue
            foreach (var recreateFamilyEntity in familiesData)
            {
                string[] relation = recreateFamilyEntity.Path.Split('>');
                if (recreateFamilyEntity.BeginEndDocState.Equals("BEGDOC"))
                {
                    //Tracer.Debug(recreateFamilyEntity.ToString());
                    if (!beginnings.ContainsKey(relation[0]))
                    {
                        beginnings.Add(relation[0], recreateFamilyEntity);
                    }
                    if (!beginnings.ContainsKey(relation[1]))
                    {
                        beginnings.Add(relation[1], recreateFamilyEntity);
                    }
                }
                if (recreateFamilyEntity.BeginEndDocState.Equals("ENDDOC"))
                {
                    //Tracer.Debug(recreateFamilyEntity.ToString());
                    string rootDocId = recreateFamilyEntity.RootDocId;
                    if (!endings.ContainsKey(rootDocId))
                    {
                        endings.Add(rootDocId, recreateFamilyEntity.FieldValue);
                    }
                    else
                    {
                        //To set highest ending value in AlphaNumeric character(e.g BAL101,BAL005...)
                        Int64 currentValue;
                        Int64 previousValue;
                        if (!Int64.TryParse(Regex.Replace(recreateFamilyEntity.FieldValue, @"[^\d]", string.Empty),
                            out currentValue) ||
                            !Int64.TryParse(Regex.Replace(endings[rootDocId], @"[^\d]", string.Empty),
                                out previousValue)) continue;
                        if (currentValue > previousValue)
                        {
                            endings[rootDocId] = recreateFamilyEntity.FieldValue;
                        }
                    }
                }
            }

            //string ending = beginnings[rootDocId];
            var beginsEndsMap = new Dictionary<string, Tuple<string, string>>();
            foreach (var docId in beginnings.Keys)
            {
                RecreateFamilyEntity recreateFamilyEntity = beginnings[docId];
                string begin = recreateFamilyEntity.FieldValue;
                string rootDocId = recreateFamilyEntity.RootDocId;
                string end = string.Empty;
                if (endings.ContainsKey(rootDocId))
                    end = endings[rootDocId];
                beginsEndsMap.Add(docId, new Tuple<string, string>(begin, end));
                //Tracer.Debug("DocId = {0}, Begin = {1}, End = {2}", docId, begin, end);
            }
            return beginsEndsMap;
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                var exportDocumentCollection = (ExportDocumentCollection)message.Body;
                #region Assertion
                //Pre Condition
                PipelineId.ShouldNotBeEmpty();
                exportDocumentCollection.ShouldNotBe(null);
                exportDocumentCollection.Documents.ShouldNotBe(null);
                exportDocumentCollection.Documents.LongCount().ShouldBeGreaterThan(0);
                #endregion
                exportDocumentCollection.Dataset = _dataset;
                GetAllData(exportDocumentCollection);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }


        private void GetAllData(ExportDocumentCollection exportDocumentCollection)
        {
            #region Get Meta Data
            var documentDetails = new List<ExportDocumentDetail>();
            var metaDataLogList = new List<JobWorkerLog<ExportMetadataLogInfo>>();
            foreach (var doc in exportDocumentCollection.Documents)
            {
                JobWorkerLog<ExportMetadataLogInfo> metaDataLog;
                var exportDocument = ConstructDocumentData(exportDocumentCollection, doc.DocumentId, doc.CorrelationId, out metaDataLog);
                lock (documentDetails)
                {
                    if (exportDocument != null)
                    {
                        documentDetails.Add(exportDocument);
                    }

                    if (metaDataLog != null)
                    {
                        metaDataLogList.Add(metaDataLog);
                    }
                }
            }

            exportDocumentCollection.Documents.Clear();
            exportDocumentCollection.Documents.AddRange(documentDetails);

            #endregion

            #region Send Message
            //Send Message
            Send(exportDocumentCollection);
            //Send Log   
            if (metaDataLogList.Count > 0)
            {
                SendLog(metaDataLogList);
                metaDataLogList.Clear();
            }
            #endregion
        }


        private void Send(ExportDocumentCollection exportDocumentCollection)
        {
            if (OutputDataPipe == null)
            {
                Tracer.Error("OutputDataPipe == null");
                return;
            }
            exportDocumentCollection.Dataset = null;
            var message = new PipeMessageEnvelope()
            {
                Body = exportDocumentCollection
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(exportDocumentCollection.Documents.Count());
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ExportMetadataLogInfo>> log)
        {
            if (LogPipe == null)
            {
                Tracer.Error("LogPipe == null");
                return;
            }

            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }
        #endregion

        private RVWDocumentBEO GetDocumentData(string documentId, string collectionId, out bool isError)
        {
            RVWDocumentBEO document = null;
            isError = false;
            try
            {
                using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
                {
                    document = DocumentBO.GetDocumentDataViewFromVaultWithOutContent(matterId.ToString(CultureInfo.InvariantCulture), collectionId, documentId, string.Empty,
                        true, false);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                isError = true;
            }
            return document;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private ExportDocumentDetail ConstructDocumentData(ExportDocumentCollection exportDocumentCollection,
            string documentId, string correlationId, out JobWorkerLog<ExportMetadataLogInfo> metadataLog)
        {
            var exportDocument = new ExportDocumentDetail();
            try
            {
                exportDocument.DocumentId = documentId;
                exportDocument.CorrelationId = correlationId;
                bool isError;
                bool isErrorInTag = false;
              
                var document = DocumentBO.GetDocumentDataForExportJob(matterId, _dataset.CollectionId, documentId, out isError);
                if (document != null)
                {
                     exportDocument.DCN = document.DocumentControlNumber;

                    if (!String.IsNullOrEmpty(exportDocumentCollection.ExportOption.FieldForTextFileName))
                    {
                        var textFieldId = DocumentBO.GetFieldIdByNameForExportJob(_dataset,
                            exportDocumentCollection.ExportOption.FieldForTextFileName, false);
                        exportDocument.TextFileName = DocumentBO.GetFieldValueForExportJob(matterId,
                            _dataset.CollectionId, documentId, textFieldId);
                    }

                    if (!String.IsNullOrEmpty(exportDocumentCollection.ExportOption.FieldForNativeFileName))
                    {
                        var nativeFieldId = DocumentBO.GetFieldIdByNameForExportJob(_dataset,
                            exportDocumentCollection.ExportOption.FieldForNativeFileName, false);
                        exportDocument.NativeFileName = DocumentBO.GetFieldValueForExportJob(matterId,
                            _dataset.CollectionId, documentId, nativeFieldId);
                    }

                    #region "Recreate family group"
                    //2)Attachment Range
                    if (_beginsEnds != null)
                    {
                        Tuple<string, string> beginEnd;
                        _beginsEnds.TryGetValue(document.Id.ToString(), out beginEnd);
                        if (beginEnd != null)
                        {
                            exportDocument.BeginDoc = beginEnd.Item1;
                            exportDocument.EndDoc = beginEnd.Item2;
                        }
                    }
                    #endregion

                    //3) File Path (Native File & Text)
                    if (document.DocumentBinary.FileList != null)
                    {
                        if (exportDocumentCollection.ExportOption.IsNative)
                        {
                            //Get the tag name to check the tag exists for the document. If exists then export the native files.
                            string tagNameToIncludeNative = exportDocumentCollection.ExportOption.IncludeNativeTagName;
                            if (!string.IsNullOrEmpty(tagNameToIncludeNative)) //If "Include native for tagged document only" option is selected
                            {
                                //Check whether the document tagged with the selected tag. If yes, set the IsNativeTagExists property as true and get all native files to export out.
                                exportDocument.IsNativeTagExists = CheckTagExistsToIncludeNative(document.CollectionId, document.DocumentId, tagNameToIncludeNative);
                                if (exportDocument.IsNativeTagExists) //If document tagged then get the native files list to export.
                                    exportDocument.NativeFiles = GetFileList(document.DocumentBinary.FileList.Where(f => f.Type == Constants.NativeFileTypeId).ToList());
                            }
                            else //If "Include native for tagged document only" option not selected, get native files to export out.
                            {
                                exportDocument.NativeFiles = GetFileList(document.DocumentBinary.FileList.Where(f => f.Type == Constants.NativeFileTypeId).ToList());
                            }
                        }
                        if (exportDocumentCollection.ExportOption.IsText)
                        {
                            if (exportDocumentCollection.ExportOption.IsTextFieldToExportSelected && !string.IsNullOrEmpty(exportDocumentCollection.ExportOption.TextFieldToExport))
                            {
                                var fileList = new List<ExportFileInformation>
                                {
                                    new ExportFileInformation()
                                    {
                                        IsTextFieldExportEnabled = true
                                    }
                                };
                                exportDocument.TextFiles = fileList;
                            }
                            else
                            {
                                if (exportDocumentCollection.ExportOption.IsProduction && !string.IsNullOrEmpty(exportDocumentCollection.ExportOption.ProductionSetCollectionId))
                                {
                                    string[] lstOfProductionSets = exportDocumentCollection.ExportOption.ProductionSetCollectionId.Split(',');
                                    foreach (string productionSetId in lstOfProductionSets)
                                    {
                                        var productionDocumentData = GetDocumentData(documentId, productionSetId, out isError);
                                        if (exportDocumentCollection.ExportOption.TextOption1 == Constants.ScrubbedText) //If Text Prority 1 is Printed/Scrubbed text
                                        {
                                            //Get the scrubbed text files for one or more productions...
                                            if (exportDocument.TextFiles == null)
                                            {
                                                //Get the scrubbed text files for the first time....
                                                exportDocument.TextFiles = GetFileList(productionDocumentData.DocumentBinary.FileList.Where(f => f.Type == Constants.ScrubbedFileTypeId).ToList());
                                            }
                                            else
                                            {
                                                //Appnd to existing text files in case user chooses more than one production 
                                                exportDocument.TextFiles.AddRange(GetFileList(productionDocumentData.DocumentBinary.FileList.Where(f => f.Type == Constants.ScrubbedFileTypeId).ToList()));
                                            }
                                            if ((exportDocument.TextFiles == null || exportDocument.TextFiles.Count == 0 || !File.Exists(exportDocument.TextFiles.FirstOrDefault().SourceFilePath)) && exportDocumentCollection.ExportOption.TextOption2 == Constants.ExtractedText) //If scrubbed text not exists for this document then get the Extracted text
                                            {
                                                exportDocument.TextFiles = GetFileList(document.DocumentBinary.FileList.Where(f => f.Type == Constants.TextFileTypeId).ToList());
                                                exportDocument.TextFiles.SafeForEach(x => x.SourceFilePath = x.SourceFilePath.Contains(Constants.QuestionMark) ? x.SourceFilePath.Substring(0, x.SourceFilePath.LastIndexOf(Constants.QuestionMark)) : x.SourceFilePath);
                                            }
                                            else
                                                exportDocument.TextFiles.ForEach(s => s.IsScrubbedText = true);
                                        }
                                        else //Else If Text Prority 1 is Extracted text
                                        {
                                            //Get the extracted text files
                                            exportDocument.TextFiles = GetFileList(document.DocumentBinary.FileList.Where(f => f.Type == Constants.TextFileTypeId).ToList());
                                            exportDocument.TextFiles.SafeForEach(x => x.SourceFilePath = x.SourceFilePath.Contains(Constants.QuestionMark) ? x.SourceFilePath.Substring(0, x.SourceFilePath.LastIndexOf(Constants.QuestionMark)) : x.SourceFilePath); //Remove the querystring
                                            if ((exportDocument.TextFiles == null || exportDocument.TextFiles.Count == 0 || !File.Exists(exportDocument.TextFiles.FirstOrDefault().SourceFilePath)) && exportDocumentCollection.ExportOption.TextOption2 == Constants.ScrubbedText) //If extracted text not exists for this document then get the Scrubbed text if exists
                                            {
                                                exportDocument.TextFiles = GetFileList(productionDocumentData.DocumentBinary.FileList.Where(f => f.Type == Constants.ScrubbedFileTypeId).ToList());
                                                exportDocument.TextFiles.ForEach(s => s.IsScrubbedText = true);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    exportDocument.TextFiles = GetFileList(document.DocumentBinary.FileList.Where(f => f.Type == Constants.TextFileTypeId).ToList());
                                    exportDocument.TextFiles.SafeForEach(x => x.SourceFilePath = x.SourceFilePath.Contains(Constants.QuestionMark) ? x.SourceFilePath.Substring(0, x.SourceFilePath.LastIndexOf(Constants.QuestionMark)) : x.SourceFilePath); //Remove the querystring
                                }
                            }
                        }
                    }
                    //2.1) File Path (Images for Imageset or Production Set)
                    exportDocument.ImageFiles = new List<ExportFileInformation>();
                }
                //3) Tag
                if (exportDocumentCollection.ExportOption.IsTag && exportDocumentCollection.ExportOption.TagList != null && exportDocumentCollection.ExportOption.TagList.Count > 0)
                {
                    var tag = GetDocumentTags(documentId, out isErrorInTag);
                    if (tag != null && tag.Count > 0)
                    {
                        exportDocument.Tags = tag.Where(t => exportDocumentCollection.ExportOption.TagList.Contains(t.TagId.ToString(CultureInfo.InvariantCulture)) && t.Status).ToList();
                    }
                }

                #region Log
                metadataLog = ConstructLog(correlationId, (!isError), (!string.IsNullOrEmpty(exportDocument.DCN) ? exportDocument.DCN : string.Empty), isError, isErrorInTag, string.Empty);
                #endregion
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                metadataLog = ConstructLog(correlationId, false, string.Empty, true, true, "Error in get Metadata.");
            }
            return exportDocument;
        }

      

        /// <summary>
        /// Checks the tag exists to include native.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="documentId">The document identifier.</param>
        /// <param name="tagNameToIncludeNative">The tag name to include native.</param>
        /// <returns>boolean</returns>
        private bool CheckTagExistsToIncludeNative(string collectionId, string documentId, string tagNameToIncludeNative)
        {
            try
            {
                var tags = new List<RVWTagBEO>();
                //Get document all tags for the document
                DocumentBO.GetDocumentTags(ref tags, matterId.ToString(CultureInfo.InvariantCulture), collectionId, documentId, false, false);
                //return true if selected tag exists in the tag list else return false.
                return tags.Exists(f => f.TagDisplayName.Contains(tagNameToIncludeNative));
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                return false;
            }
        }

        private List<RVWDocumentTagBEO> GetDocumentTags(string documentId, out bool isError)
        {
            List<RVWDocumentTagBEO> tags = null;
            isError = false;
            try
            {
                var tagdetails = new List<RVWTagBEO>();
                using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
                {
                    tags = DocumentBO.GetDocumentTags(ref tagdetails, matterId.ToString(CultureInfo.InvariantCulture), _dataset.CollectionId, documentId, false, false);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                isError = true;
            }
            return tags;
        }

        //private RVWDocumentBEO GetFileFromDocumentSet(long matterId, string collectionId, string documentId, int binaryTypeId)
        //{
        //    RVWDocumentBEO document = null;
        //    try
        //    {
        //        using (new EVTransactionScope(System.Transactions.TransactionScopeOption.Suppress))
        //        {
        //            document = DocumentBO.GetDocumentFileDetails(matterId, collectionId, documentId, binaryTypeId, string.Empty);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Trace().Swallow();
        //    }
        //    return document;
        //}

        private List<ExportFileInformation> GetFileList(IEnumerable<RVWExternalFileBEO> externalFileList)
        {
            return externalFileList.Select(externalFile => new ExportFileInformation { SourceFilePath = externalFile.Path }).ToList();
        }

        /// <summary>
        /// Construct Log Data
        /// </summary>       
        public JobWorkerLog<ExportMetadataLogInfo> ConstructLog(string correlationId, bool success, string dcn, bool isErrorInDocument, bool isErrorInTag, string message)
        {
            var metadataLog = new JobWorkerLog<ExportMetadataLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.ExportMetadataWorkerRoleType,
                Success = success,
                CreatedBy = "NA",
                IsMessage = false,
                LogInfo = new ExportMetadataLogInfo { DCN = dcn }
            };
            if (isErrorInDocument)
            {
                metadataLog.LogInfo.IsErrorInField = true;
                metadataLog.LogInfo.IsErrorInNativeFile = true;
                metadataLog.LogInfo.IsErrorInImageFile = true;
                metadataLog.LogInfo.IsErrorInTextFile = true;
            }
            if (isErrorInTag)
            {
                metadataLog.LogInfo.IsErrorInTag = true;
            }
            if (!success)
            {
                metadataLog.LogInfo.Information = (!string.IsNullOrEmpty(message) ? (message + ". DCN:" + dcn) : string.Empty);
            }
            return metadataLog;
        }
    }


}
