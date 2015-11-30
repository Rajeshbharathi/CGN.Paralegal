#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ExportLoadFileWriterWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Baranitharan</author>
//      <description>
//          This file has ExportLoadFileWriterWorker related methods
//      </description>
//      <changelog>
//          <date value="02/June/2012">Task fix 101466 - Cr022</date>
//          <date value="02/19/2012">Bug fix 130947 - Nlog</date>
//          <date value="04/23/2013">ADM-Export-003 Recreate Family Group</date>
//          <date value="07/19/2013">BugFix 146819, 144007, 147272, 146007</date>
//          <date value="10/21/2013">Bug # 154582 - Changed the audit logging from single to bulk in export load file to avoid the performance problems with the data (4264027 audit log records) </date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//         <date value="03/14/2014">ADM-REPORTS-003  - Included code changes for New Audit Log</date>
//         <date value="04/10/2014">ADM-REPORTS-003  - Bug # 168058</date>
//          <date value="04/11/2014">Bug Fix # 168133, 168058 & 168087</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion


using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.AuditManagement;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.TraceServices;
using System.Threading.Tasks;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;

namespace LexisNexis.Evolution.Worker
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class ExportLoadFileWriterWorker : WorkerBase
    {
        private ExportDocumentCollection exportDocumentCollection;
        private ExportLoadJobDetailBEO parametersExportLoadFile;
        private char columnDelimiter;
        private char quoteCharacter;
        private char contentNewLine;
        private string newLine;
        private string loadFileName;
        private string imageHelperFileName;
        private string textHelperFileName;
        private List<ExportLoadFieldSelectionBEO> exportFieldsSelection;
        private string dateFormatForData = string.Empty;
        private string exportFolderPath = string.Empty;
        private char tagSeparator;
        private Encoding encoding;
        private bool isRelativePath = true;
        private string createdBy = string.Empty;
        private Int32 jobID = 0;
        private StreamWriter loadFileIoWriter = null;
        private StreamWriter imageFileIoWriter = null;
        private StreamWriter textFileIoWriter = null;
        private SortedDictionary<string, string> loadFileSortOrderCollection = null;
        private SortedDictionary<string, string> imageFileSortOrderCollection = null;
        private SortedDictionary<string, string> textFileSortOrderCollection = null;
        private Dictionary<string, string> documentContentFieldsValueCollection = null;
        private ExportLoadJobDetailBEO _exportLoadJobDetailBeo;
        private int _maxParallelThread;
        private DatasetBEO _dataset;
        private List<int> _selectedFieldIds = new List<int>();
        #region OverDrive

        protected override void BeginWork()
        {
            base.BeginWork();
            _maxParallelThread = Convert.ToInt32(ApplicationConfigurationManager.GetValue("NumberOfMaxParallelism", "Export"));
            _exportLoadJobDetailBeo = Utils.SmartXmlDeserializer(BootParameters) as ExportLoadJobDetailBEO;
            _dataset = DataSetBO.GetDatasetDetailsWithMatterInfo(Convert.ToInt64(_exportLoadJobDetailBeo.DatasetId),
                                                _exportLoadJobDetailBeo.MatterId);
        }

        protected override void EndWork()
        {
            base.EndWork();
        }

        protected void FinalizeFiles()
        {
            Tracer.Info("Files finalization in progress.");

            Parallel.Invoke(WriteLoadFile,
                            WriteImageHelperFile,
                            WriteTextHelperFile
                            );

            if (loadFileIoWriter != null)
            {
                loadFileIoWriter.Close();
            }
            if (imageFileIoWriter != null)
            {
                imageFileIoWriter.Close();
            }
            if (textFileIoWriter != null)
            {
                textFileIoWriter.Close();
            }

            Tracer.Info("Files successfully finalized.");
        }

        private void WriteLoadFile()
        {
            //1) Write DAT
            if (loadFileSortOrderCollection != null && loadFileSortOrderCollection.Any())
            {
                WriteToFile(loadFileSortOrderCollection, loadFileIoWriter);
                loadFileSortOrderCollection.Clear();
            }
        }

        private void WriteImageHelperFile()
        {
            //2) Write Image Helper File
            if ((exportDocumentCollection.ExportOption.IsImage || exportDocumentCollection.ExportOption.IsProduction) && (imageFileSortOrderCollection != null && imageFileSortOrderCollection.Count > 0))
            {
                WriteToFile(imageFileSortOrderCollection, imageFileIoWriter);
                imageFileSortOrderCollection.Clear();
            }
        }

        private void WriteTextHelperFile()
        {
            if (textFileSortOrderCollection != null && textFileSortOrderCollection.Any())
            {
                WriteToFile(textFileSortOrderCollection, textFileIoWriter);
                textFileSortOrderCollection.Clear();
            }
        }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            if (envelope.Label == "PleaseFinalize")
            {
                FinalizeFiles();
                return;
            }

            exportDocumentCollection = (ExportDocumentCollection)envelope.Body;
            #region Assertion
            //Pre Condition
            PipelineId.ShouldNotBeEmpty();
            BootParameters.ShouldNotBe(null);
            BootParameters.ShouldBeTypeOf<string>();
            exportDocumentCollection.ShouldNotBe(null);
            exportDocumentCollection.Documents.ShouldNotBe(null);
            exportDocumentCollection.Documents.LongCount().ShouldBeGreaterThan(0);
            #endregion
            exportDocumentCollection.Dataset = _dataset;
            try
            {
                if (parametersExportLoadFile == null)
                {
                    InitializeForProcessing(BootParameters);
                }

                GetDocumentFields(exportDocumentCollection.Documents);

                #region Get Content-Field value from Text file
                GetDocumentsContentField();
                #endregion

                if (exportDocumentCollection.ExportOption.IsImage || exportDocumentCollection.ExportOption.IsProduction)
                {
                    //Set Images File Path..
                    var loadFileHelper = new ExportLoadFileHelper(BootParameters);
                    Parallel.ForEach(exportDocumentCollection.Documents,
                        new ParallelOptions {MaxDegreeOfParallelism = _maxParallelThread},
                        (docDetail) =>
                            loadFileHelper.SetImageSourceFiles(docDetail, exportDocumentCollection.ExportOption));
                }

                var fileWriterLogList = WriteLoadFiles();
                #region Send Log
                if (fileWriterLogList != null && fileWriterLogList.Any())
                {
                    //Send to Log pipe
                    SendLog(fileWriterLogList);
                }
                #endregion

                documentContentFieldsValueCollection.Clear();
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        private void GetDocumentFields(List<ExportDocumentDetail> exportDocuments)
        {
            documentContentFieldsValueCollection = new Dictionary<string, string>();

            var documentIds= exportDocuments.Select(document => document.DocumentId).ToList();

            var resultDocuments =  DocumentBO.BulkGetDocumentsFieldsForExportJob(Convert.ToInt64(_exportLoadJobDetailBeo.MatterId),
                _dataset.CollectionId, documentIds, _selectedFieldIds);
            if (resultDocuments != null)
            {
                foreach (var exportDocument in exportDocuments)
                {
                    var document = resultDocuments.FirstOrDefault(d => d.DocumentId == exportDocument.DocumentId);
                    if (document != null)
                    {
                        exportDocument.Fields = document.FieldList;
                    }
                }
            }
        }

       

        private void InitializeForProcessing(string exportBootParameter)
        {
            parametersExportLoadFile = GetExportBEO<ExportLoadJobDetailBEO>(exportBootParameter);
            if (parametersExportLoadFile.ExportLoadFileInfo != null)
            {
                columnDelimiter = Convert.ToChar(Convert.ToInt32(parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.Column));
                quoteCharacter = Convert.ToChar(Convert.ToInt32(parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.Quote));
                contentNewLine = Convert.ToChar(Convert.ToInt32(parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.NewLine));
                newLine = Constants.ConcordanceRecordSplitter;
                if (parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat != null)
                    dateFormatForData = (!string.IsNullOrEmpty(parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.DateFormat)) ? parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.DateFormat : string.Empty;

                encoding = (parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.EncodingType == EncodingTypeSelection.Ansi) ? Encoding.GetEncoding(Constants.Ansi) : Encoding.Unicode;

                if (parametersExportLoadFile.ExportLoadFileInfo.ExportLoadNativeFileLocation != null)
                    isRelativePath = parametersExportLoadFile.ExportLoadFileInfo.ExportLoadNativeFileLocation.RelativePathLocation;

                if (!string.IsNullOrEmpty(parametersExportLoadFile.CreatedBy))
                    createdBy = parametersExportLoadFile.CreatedBy;
            }


            if (exportDocumentCollection.ExportOption != null)
            {
                loadFileName = exportDocumentCollection.ExportOption.LoadFilePath;
                imageHelperFileName = (!string.IsNullOrEmpty(exportDocumentCollection.ExportOption.LoadFileImageHelperFilePath)) ? exportDocumentCollection.ExportOption.LoadFileImageHelperFilePath : string.Empty;
                textHelperFileName = (!string.IsNullOrEmpty(exportDocumentCollection.ExportOption.LoadFileTextHelperFilePath)) ? exportDocumentCollection.ExportOption.LoadFileTextHelperFilePath : string.Empty;
                exportFolderPath = exportDocumentCollection.ExportOption.ExportDestinationFolderPath;
            }

            if (parametersExportLoadFile.ExportLoadFields != null)
            {
                exportFieldsSelection = parametersExportLoadFile.ExportLoadFields;
            }

            if (parametersExportLoadFile.ExportLoadTagInfo != null && !string.IsNullOrEmpty(parametersExportLoadFile.ExportLoadTagInfo.Delimeter))
            {
                tagSeparator = Convert.ToChar(Convert.ToInt32(parametersExportLoadFile.ExportLoadTagInfo.Delimeter));
            }

            if (!string.IsNullOrEmpty(PipelineId) && WorkAssignment != null)
            {
                jobID = WorkAssignment.JobId;
            }

            #region Initialize File writer
            //Initialize File writer for First time
            loadFileIoWriter = new StreamWriter(loadFileName, true, encoding);
            if (exportDocumentCollection.ExportOption.IsText && parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.Nameselection == TextFileNameSelection.UseOPT)
            {
                textFileIoWriter = new StreamWriter(textHelperFileName, true);
            }
            if (exportDocumentCollection.ExportOption.IsImage || exportDocumentCollection.ExportOption.IsProduction)
            {
                imageFileIoWriter = new StreamWriter(imageHelperFileName, true);
            }
            #endregion

            #region Initialize Sort Order collection
            loadFileSortOrderCollection = new SortedDictionary<string, string>();
            if (exportDocumentCollection.ExportOption.IsText)
            {
                textFileSortOrderCollection = new SortedDictionary<string, string>();
            }
            if (exportDocumentCollection.ExportOption.IsImage || exportDocumentCollection.ExportOption.IsProduction)
            {
                imageFileSortOrderCollection = new SortedDictionary<string, string>();
            }
            #endregion

            
            foreach (var exportField in exportFieldsSelection)
            {
                if(String.Equals(exportField.DataSetFieldName, "CONTENT", StringComparison.CurrentCultureIgnoreCase)) continue;
                var textFieldId = DocumentBO.GetFieldIdByNameForExportJob(_dataset, exportField.DataSetFieldName, false);
                _selectedFieldIds.Add(textFieldId);
            }
        }

        private List<JobWorkerLog<ExportLoadFileWritterLogInfo>> WriteLoadFiles()
        {
            var fileWriterLogList = new List<JobWorkerLog<ExportLoadFileWritterLogInfo>>();

            #region Construct Data

            Parallel.ForEach(exportDocumentCollection.Documents,
                new ParallelOptions {MaxDegreeOfParallelism = _maxParallelThread},
                (doc) =>
                {
                    var loadFileDataRow = string.Empty;
                    var textHelperFileRow = string.Empty;
                    var imageHelperFileRow = string.Empty;
                    JobWorkerLog<ExportLoadFileWritterLogInfo> fileWriterLog = null;

                    ConstructAllData(doc, out loadFileDataRow, out textHelperFileRow, out imageHelperFileRow,
                        out fileWriterLog);

                    lock (loadFileSortOrderCollection)
                    {

                        if (!string.IsNullOrEmpty(loadFileDataRow))
                        {
                            if (!loadFileSortOrderCollection.Keys.Contains(doc.DCN.Trim()))
                            {
                                loadFileSortOrderCollection.Add(doc.DCN.Trim(), loadFileDataRow);
                            }
                        }
                        if (!string.IsNullOrEmpty(textHelperFileRow))
                        {
                            if (!textFileSortOrderCollection.Keys.Contains(doc.DCN.Trim()))
                            {
                                textFileSortOrderCollection.Add(doc.DCN.Trim(), textHelperFileRow);
                            }
                        }
                        if (!string.IsNullOrEmpty(imageHelperFileRow))
                        {
                            if (!imageFileSortOrderCollection.Keys.Contains(doc.DCN.Trim()))
                            {
                                imageFileSortOrderCollection.Add(doc.DCN.Trim(), imageHelperFileRow);
                            }
                        }
                        if (fileWriterLog != null)
                            fileWriterLogList.Add(fileWriterLog);
                    }
                });
            
            #endregion

            IncreaseProcessedDocumentsCount(exportDocumentCollection.Documents.Count());

            return fileWriterLogList;
        }

       


        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ExportLoadFileWritterLogInfo>> log)
        {
            if (LogPipe == null)
            {
                Tracer.Warning("ExportLoadFileWriterWorker.SendLog: LogPipe == null");
                return;
            }
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// Construct Data for Load File(DAT/CSV/...) & Helper File (.opt)
        /// </summary>       
        public void ConstructAllData(ExportDocumentDetail docDetail, out string loadFileDataRow, out string textHelperFileRow, out string imageHelperFileRow, out JobWorkerLog<ExportLoadFileWritterLogInfo> fileWriterLog)
        {
            fileWriterLog = null;
            loadFileDataRow = string.Empty;
            textHelperFileRow = string.Empty;
            imageHelperFileRow = string.Empty;
            bool isErrorInLoadFile = false;
            bool isErrorInTextFile = false;
            bool isErrorInImageFile = false;
            bool sucess = true;
            string errorMessage = string.Empty;
            #region Load File
            var loadFileRow = ConstructLoadFileRow(docDetail, out isErrorInLoadFile);
            if (loadFileRow != string.Empty && loadFileRow.Length > 0)
            {
                loadFileDataRow = loadFileRow;
            }
            #endregion

            #region Text Helper File
            if (exportDocumentCollection.ExportOption.IsText && parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.Nameselection == TextFileNameSelection.UseOPT)
            {
                if (docDetail.TextFiles != null && docDetail.TextFiles.Any())
                {
                    textHelperFileRow = ConstructTextHelperFileRow(docDetail, out isErrorInTextFile);
                }
            }
            #endregion

            #region Image Helper File
            if ((exportDocumentCollection.ExportOption.IsImage || exportDocumentCollection.ExportOption.IsProduction))
            {
                if (docDetail.ImageFiles != null && docDetail.ImageFiles.Any())
                {
                    imageHelperFileRow = ConstructImageHelperFileRow(docDetail, out isErrorInImageFile);
                }
            }
            #endregion

            #region Log
            if (isErrorInLoadFile)
            {
                sucess = false;
                errorMessage = Constants.LoadFileWriteFailed;
            }
            fileWriterLog = ConstructLog(docDetail.CorrelationId, sucess, docDetail.DCN, isErrorInLoadFile, isErrorInTextFile, isErrorInImageFile, errorMessage);
            #endregion
        }

        /// <summary>
        /// Construct Log Data
        /// </summary>       
        public JobWorkerLog<ExportLoadFileWritterLogInfo> ConstructLog(string correlationId, bool success, string DCN, bool isErrorInNative, bool isErrorInText, bool isErrorInImage, string message)
        {
            var fileWriterDataLog = new JobWorkerLog<ExportLoadFileWritterLogInfo>();
            fileWriterDataLog.JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0;
            fileWriterDataLog.CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0;
            fileWriterDataLog.WorkerInstanceId = WorkerId;
            fileWriterDataLog.WorkerRoleType = Constants.ExportLaodFileWriterWorkerRoleType;
            fileWriterDataLog.Success = success;
            fileWriterDataLog.CreatedBy = createdBy;
            fileWriterDataLog.IsMessage = false;
            fileWriterDataLog.LogInfo = new ExportLoadFileWritterLogInfo();
            fileWriterDataLog.LogInfo.DCN = DCN;
            fileWriterDataLog.LogInfo.IsErrorInNativeFile = isErrorInNative;
            fileWriterDataLog.LogInfo.IsErrorInImageFile = isErrorInImage;
            fileWriterDataLog.LogInfo.IsErrorInTextFile = isErrorInText;
            fileWriterDataLog.LogInfo.Information = (!string.IsNullOrEmpty(message) ? (message + ". DCN:" + DCN) : string.Empty);
            if (isErrorInNative || isErrorInImage || isErrorInText)
            {
                fileWriterDataLog.IsMessage = true;
            }
            return fileWriterDataLog;
        }


        #endregion

        /// <summary>
        /// Get Document ContentFields
        /// </summary>
        private void GetDocumentsContentField()
        {
            try
            {
                var contentFieldSelection = exportFieldsSelection.FirstOrDefault(f => f.DataSetFieldName.ToUpper() == "CONTENT");
           
                if (contentFieldSelection!=null)
                { 
                  documentContentFieldsValueCollection = new Dictionary<string, string>();
                   Parallel.ForEach(exportDocumentCollection.Documents, new ParallelOptions { MaxDegreeOfParallelism = _maxParallelThread }, (docDeatil) =>
                   {
                      string contentFieldValue = GetContentFieldsValue(docDeatil);
                       lock (documentContentFieldsValueCollection)
                       {
                          if (!string.IsNullOrEmpty(docDeatil.DocumentId) && !documentContentFieldsValueCollection.ContainsKey(docDeatil.DocumentId))
                          {
                            documentContentFieldsValueCollection.Add(docDeatil.DocumentId, contentFieldValue);
                          }
                        }
                   });
               }

            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        ///  Get ContentFields
        /// </summary>       
        private string GetContentFieldsValue(ExportDocumentDetail docDetail)
        {
            string fieldValue = string.Empty;
            try
            {

                if (exportFieldsSelection != null)
                {
                    var contentField = exportFieldsSelection.FirstOrDefault(f => f.DataSetFieldName.ToUpper() == "CONTENT");
                    if (contentField != null)
                    {
                        fieldValue = DocumentBO.GetDocumentContentFieldsForExportJob(Convert.ToInt64(_exportLoadJobDetailBeo.MatterId), _dataset.CollectionId, docDetail.DocumentId);
                        fieldValue = DocumentBO.Base64DecodeForExportJob(fieldValue);  //Decode the content field
                        fieldValue = fieldValue.Replace(Constants.ConcordanceRecordSplitter, contentNewLine.ToString()).Replace(Constants.ConcordanceFieldSplitter, contentNewLine.ToString()).Replace(Constants.ConcordanceRowSplitter, contentNewLine.ToString());
                     }
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }

            return fieldValue;
        }

        #region Load File

        /// <summary>
        ///  Get fields value to write in .DAT file
        /// </summary>       
        private string GetFieldsValue(ExportDocumentDetail docDetail)
        {
            StringBuilder sbLoadFileData = new StringBuilder();
            if (exportFieldsSelection != null && docDetail != null && docDetail.Fields != null)
                foreach (
                    var resultField in
                        exportFieldsSelection.Select(
                            selectedFields =>
                                docDetail.Fields.FirstOrDefault(o => o.FieldName == selectedFields.DataSetFieldName)))
                {
                    string fieldValue = string.Empty;
                    if (resultField != null && !string.IsNullOrEmpty(resultField.FieldValue) &&
                        resultField.FieldTypeId != Constants.ContentFieldType)
                    {
                        fieldValue = resultField.FieldValue;

                        #region Date Format

                        if (resultField.FieldTypeId == 61 &&
                            dateFormatForData != string.Empty)
                        {
                            fieldValue = DocumentBO.ConvertFieldsValueIntoDateFormatForExportJob(resultField,
                                dateFormatForData);
                        }

                        #endregion
                    }
                    sbLoadFileData.Append(quoteCharacter);
                    if (!string.IsNullOrEmpty(fieldValue))
                    {
                        sbLoadFileData.Append(fieldValue);
                    }
                    sbLoadFileData.Append(quoteCharacter);
                    sbLoadFileData.Append(columnDelimiter);

                }

            var contentField = exportFieldsSelection.FirstOrDefault(f => f.DataSetFieldName.ToUpper() == "CONTENT");
            if (contentField != null)
            {
                if (documentContentFieldsValueCollection != null && documentContentFieldsValueCollection.Any() &&
                    !string.IsNullOrEmpty(docDetail.DocumentId))
                {
                    if (documentContentFieldsValueCollection.ContainsKey(docDetail.DocumentId))
                    {
                        var fieldValue = documentContentFieldsValueCollection[docDetail.DocumentId];

                        sbLoadFileData.Append(quoteCharacter);
                        if (!string.IsNullOrEmpty(fieldValue))
                        {
                            sbLoadFileData.Append(fieldValue.Replace(Constants.ConcordanceFieldSplitter,
                                contentNewLine.ToString()));
                        }
                        sbLoadFileData.Append(quoteCharacter);
                        sbLoadFileData.Append(columnDelimiter);
                    }
                }
            }

            return sbLoadFileData.ToString();
        }


        /// <summary>
        /// Construct Load File (.dat/csv/...) row 
        /// </summary>  
        private string ConstructLoadFileRow(ExportDocumentDetail docDetail, out bool isERROR)
        {
            isERROR = false;
            StringBuilder sbLoadFileData = new StringBuilder();
            try
            {
                #region Fields
                if (exportFieldsSelection != null && exportFieldsSelection.Any())
                {
                    var fieldValue = GetFieldsValue(docDetail);
                    sbLoadFileData.Append(fieldValue);
                }
                #endregion

                #region "Recreate Family Group"
                if (docDetail.Fields != null && parametersExportLoadFile.RecreateFamilyGroup)
                {
                    switch (parametersExportLoadFile.AttachmentField)
                    {
                        case "Beg Attach and End attach":
                            sbLoadFileData.Append(quoteCharacter);
                            sbLoadFileData.Append(docDetail.BeginDoc);
                            sbLoadFileData.Append(quoteCharacter);
                            sbLoadFileData.Append(columnDelimiter);
                            sbLoadFileData.Append(quoteCharacter);
                            sbLoadFileData.Append(docDetail.EndDoc);
                            sbLoadFileData.Append(quoteCharacter);
                            sbLoadFileData.Append(columnDelimiter);
                            break;
                        default:
                            sbLoadFileData.Append(quoteCharacter);
                            if (!string.IsNullOrEmpty(docDetail.BeginDoc) && !string.IsNullOrEmpty(docDetail.EndDoc))
                                sbLoadFileData.Append(docDetail.BeginDoc + " - " + docDetail.EndDoc);
                            else
                                sbLoadFileData.Append(string.Empty);
                            sbLoadFileData.Append(quoteCharacter);
                            sbLoadFileData.Append(columnDelimiter);
                            break;
                    }
                }

                #endregion

                #region Native File
                if (parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeNativeFile)
                {
                    string nativeFilepath = string.Empty;
                    if (docDetail.NativeFiles != null && docDetail.NativeFiles.Any())
                    {
                        nativeFilepath = (!string.IsNullOrEmpty(docDetail.NativeFiles.FirstOrDefault().DestinationFolder) ? docDetail.NativeFiles.FirstOrDefault().DestinationFolder : string.Empty);
                        if (isRelativePath)
                        {
                            nativeFilepath = nativeFilepath.Replace(exportFolderPath, "");
                        }
                    }
                    sbLoadFileData.Append(quoteCharacter);
                    if (!string.IsNullOrEmpty(nativeFilepath) && !Path.HasExtension(nativeFilepath))
                        nativeFilepath = string.Empty;
                    sbLoadFileData.Append(nativeFilepath);
                    sbLoadFileData.Append(quoteCharacter);
                    sbLoadFileData.Append(columnDelimiter);
                }
                #endregion
                #region Text File
                if (parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeTextFile && parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.Nameselection == TextFileNameSelection.UseLoadFIle)
                {
                    string textFilePath = string.Empty;
                    if (docDetail.TextFiles != null && docDetail.TextFiles.Any())
                    {
                        textFilePath = (!string.IsNullOrEmpty(docDetail.TextFiles.FirstOrDefault().DestinationFolder) ? docDetail.TextFiles.FirstOrDefault().DestinationFolder : string.Empty);
                        if (isRelativePath)
                        {
                            textFilePath = textFilePath.Replace(exportFolderPath, "");
                        }
                    }
                    sbLoadFileData.Append(quoteCharacter);
                    sbLoadFileData.Append(textFilePath);
                    sbLoadFileData.Append(quoteCharacter);
                    sbLoadFileData.Append(columnDelimiter);
                }
                #endregion
                #region Tag
                if (exportDocumentCollection.ExportOption.IsTag)
                {
                    if (docDetail.Tags != null && docDetail.Tags.Any())
                    {
                        var tagInfo = String.Join(tagSeparator.ToString(), docDetail.Tags.Select(t => t.TagDisplayName).ToArray());
                        sbLoadFileData.Append(quoteCharacter);
                        //Tag also one of the field, if tag contains record spliter(/n), Need to replace as newline delimiter
                        tagInfo = tagInfo.Replace(Constants.ConcordanceFieldSplitter, contentNewLine.ToString());
                        sbLoadFileData.Append(tagInfo);
                        sbLoadFileData.Append(quoteCharacter);
                        sbLoadFileData.Append(columnDelimiter);
                    }
                    else                                    //For No tag for a document - add empty columns
                    {
                        sbLoadFileData.Append(quoteCharacter);
                        sbLoadFileData.Append(quoteCharacter);
                        sbLoadFileData.Append(columnDelimiter);
                    }
                }
                #endregion

                sbLoadFileData.Remove(sbLoadFileData.ToString().Length - 1, 1);
                sbLoadFileData.Append(newLine);
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("ExportOption Load File Writer Worker: Error occurred on construct Load File (.DAT/.CSV....) row for job run id:{0}",
                    PipelineId).Trace().Swallow();
                isERROR = true;
            }
            return sbLoadFileData.ToString();
        }

        /// <summary>
        /// Construct Helper File (.OPT) row based on Helper file format for Content File
        /// </summary>       
        private string ConstructTextHelperFileRow(ExportDocumentDetail docDetail, out bool isERROR)
        {
            isERROR = false;
            StringBuilder sbTextHelperFileData = new StringBuilder();
            try
            {
                string textFile = string.Empty;
                textFile = docDetail.TextFiles.FirstOrDefault().DestinationFolder.Replace(exportFolderPath, "");
                string volumeName = textFile.Remove(0, 1).Split(new Char[] { '\\' }).FirstOrDefault();

                sbTextHelperFileData.Append(Path.GetFileNameWithoutExtension(textFile));  //ALIAS
                sbTextHelperFileData.Append(Constants.CommaSeparator);
                sbTextHelperFileData.Append(volumeName);                                  //Volume
                sbTextHelperFileData.Append(Constants.CommaSeparator);
                if (isRelativePath)
                {
                    sbTextHelperFileData.Append(textFile.Replace((exportFolderPath + "\\"), ""));     //PATH
                }
                else
                {
                    var fullPath = exportFolderPath + textFile;
                    sbTextHelperFileData.Append(fullPath);                                    //PATH
                }
                sbTextHelperFileData.Append(Constants.CommaSeparator);
                sbTextHelperFileData.Append(Constants.DocumentBreak);                     //DOC_BREAK
                sbTextHelperFileData.Append(Constants.CommaSeparator);
                //Not Applicable                                                          // FOLDER_BREAK
                sbTextHelperFileData.Append(Constants.CommaSeparator);
                //Not Applicable                                                           //BOX_BREAK
                sbTextHelperFileData.Append(Constants.CommaSeparator);
                sbTextHelperFileData.Append("1");                                         //PAGES
                sbTextHelperFileData.AppendLine();
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("ExportOption Load File Writer Worker: Error occurred on construct text Helper File row for job run id:{0}",
                    PipelineId).Trace().Swallow();
                isERROR = true;
            }
            return sbTextHelperFileData.ToString();
        }

        /// <summary>
        /// Construct Helper File (.OPT) row based on Helper file format for Image File
        /// </summary>     
        private string ConstructImageHelperFileRow(ExportDocumentDetail docDetail, out bool isERROR)
        {
            isERROR = false;
            StringBuilder sbImageHelperFileData = new StringBuilder();
            int fileCount = 0;
            try
            {
                foreach (ExportFileInformation imageDocFiles in docDetail.ImageFiles)
                {
                    var imageFile = imageDocFiles.DestinationFolder.Replace(exportFolderPath, "");
                    var volumeFolder = imageFile.Remove(0, 1).Split(new Char[] { '\\' }).FirstOrDefault();

                    sbImageHelperFileData.Append(Path.GetFileNameWithoutExtension(imageFile));  //ALIAS
                    sbImageHelperFileData.Append(Constants.CommaSeparator);
                    sbImageHelperFileData.Append(volumeFolder);                                 //Volume
                    sbImageHelperFileData.Append(Constants.CommaSeparator);
                    if (isRelativePath)
                    {
                        sbImageHelperFileData.Append(imageFile.Replace((exportFolderPath + "\\"), ""));     //PATH
                    }
                    else
                    {
                        var fullPath = exportFolderPath + imageFile;
                        sbImageHelperFileData.Append(fullPath);                                      //PATH
                    }
                    sbImageHelperFileData.Append(Constants.CommaSeparator);
                    if (fileCount == 0)
                    {
                        sbImageHelperFileData.Append(Constants.DocumentBreak);                 //DOC_BREAK
                    }
                    sbImageHelperFileData.Append(Constants.CommaSeparator);
                    //Not Applicable                                                          // FOLDER_BREAK
                    sbImageHelperFileData.Append(Constants.CommaSeparator);
                    //Not Applicable                                                           //BOX_BREAK
                    sbImageHelperFileData.Append(Constants.CommaSeparator);
                    if (fileCount == 0)
                    {
                        sbImageHelperFileData.Append(docDetail.ImageFiles.Count);             //PAGES
                    }
                    sbImageHelperFileData.AppendLine();
                    fileCount++;
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("ExportOption Load File Writer Worker: Error occurred on construct image Helper File row for job run id:{0}",
                    PipelineId).Trace().Swallow();
                isERROR = true;
            }
            return sbImageHelperFileData.ToString();
        }

        /// <summary>
        /// Write Data into file
        /// </summary>       
        private bool WriteToFile(SortedDictionary<string, string> documentsLoadFileInformation, StreamWriter fileWriter)
        {
            bool sucess = true;
            try
            {
                var sbText = new StringBuilder();
                foreach (var document in documentsLoadFileInformation)
                {
                  //  fileWriter.Write(document.Value);
                    sbText.Append(document.Value);
                }
                fileWriter.Write(sbText.ToString());
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                sucess = false;
            }
            return sucess;
        }


        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private T GetExportBEO<T>(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(T));

            //Deserialization of bootparameter to get ImportBEO
            return (T)xmlStream.Deserialize(stream);
        }

        #endregion
    }
}
