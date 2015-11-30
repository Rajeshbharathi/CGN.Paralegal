# region File Header
//----------------------------------------------------------------------------------------- 
// <copyright file="ExportFileCopyWorker.cs" company="LexisNexis"> 
//      Copyright (c) Lexis Nexis. All rights reserved. 
// </copyright> 
// <header> 
//      <author>Prabhu</author> 
//      <description> 
//          This is a file that contains ExportFileCopyWorker class  
//      </description> 
//      <changelog> 
//          <date value="03/02/2012">Bug Fix 86335</date> 
//          <date value="15/02/2012">Bug Fix 95670</date>
//          <date value="24/04/2013">BugFix 135905</date>
//          <date value="12/11/2013">Task 159195 - ADM-EXPORT-004</date>
//          <date value="01/02/2014">Task 159667 - ADM-EXPORT-005</date>
//          <date value="01/29/2014">Task 161755 - ADM-EXPORT-006</date>
//          <date value="02/11/2014">Bug 164054, 164126, 164168, 164211, 164233 - Buddy bug fix for ADM EXPORT 004-006</date>
//          <date value="04/11/2014">Bug Fix # 168133, 168058 & 168087</date>
//          <date value="05/08/2014">Bug Fix # 169088, 169090</date>
//          <date value="05/20/2014">Bug Fix # 169090</date>
//          <date value="06/11/2014">Bug Fix # 170958</date>
//      </changelog> 
// </header> 
//----------------------------------------------------------------------------------------- 
#endregion

using LexisNexis.Evolution.Business.AuditManagement;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace LexisNexis.Evolution.Worker
{

    public class ExportFileCopyWorker : WorkerBase
    {
        private const string TxtExtension = ".txt";
        private const string ErrorMessage = "Please check image file name. Special character at the end of name.";
        private const string NativeFileError = "Unable to create native file because selected field for native file name has no value.";
        private const string TextFileError = "Unable to create text file because selected field for text file name has no value.";
        private ExportDocumentCollection _exportDocumentCollection;
        private DatasetBEO _dataset;
        private Encoding _encodingType;
        private string _fieldImageFileName = string.Empty;
        private bool _includeImages;
        private bool _isAutoIncrementFileName;
        private bool _isIncludeNative;
        private bool _isIncludeText;
        private bool _isNativeTag;
        private string _createdBy = string.Empty;
        static readonly object DocObject = new object();
        private const char OpenBrace = '(';
        private const string OpenBraceWithSpace = " (";
        private const char ClosedBrace = ')';
        private const long ExceptionHr = -2147024816;
        private string _bootParameter;
        private ExportLoadJobDetailBEO _exportLoadJobDetailBeo;
        private int _imageNameFieldType;
        private readonly List<Int32> _lstBatesAndDpnFieldTypes = new List<int> { 3004, 3005, 3006, 3007, 3008 };
        private const int ContentFieldTypeID = 2000;
        #region Overdrive

        protected override void BeginWork()
        {
            base.BeginWork();
            _bootParameter = BootParameters;
            _exportLoadJobDetailBeo = Utils.SmartXmlDeserializer(_bootParameter) as ExportLoadJobDetailBEO;
            _dataset = DataSetBO.GetDatasetDetailsWithMatterInfo(Convert.ToInt64(_exportLoadJobDetailBeo.DatasetId),
                                                _exportLoadJobDetailBeo.MatterId);
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                _exportDocumentCollection = (ExportDocumentCollection)message.Body;
                #region Assertion
                //Pre Condition
                PipelineId.ShouldNotBeEmpty();
                BootParameters.ShouldNotBe(null);
                BootParameters.ShouldBeTypeOf<string>();
                _exportDocumentCollection.ShouldNotBe(null);
                _exportDocumentCollection.Documents.ShouldNotBe(null);
                _exportDocumentCollection.Documents.LongCount().ShouldBeGreaterThan(0);
                #endregion
                if (_exportDocumentCollection == null)
                {
                    Tracer.Error("ExportOption File Copy Worker: Pipe message body contains empty data for job run id:{0}", PipelineId);
                    return;
                }
                _exportDocumentCollection.Dataset = _dataset;
                InitializeForProcessing(BootParameters);

                List<JobWorkerLog<ExportFileCopyLogInfo>> fileCopyLogList;
                PerformCopy(out fileCopyLogList);

                //Audit Log
                InsertAuditLog(_exportDocumentCollection, _exportLoadJobDetailBeo.JobName);

                #region Send Message
              
                Send();
                if (fileCopyLogList != null && fileCopyLogList.Count > 0)
                {
                    SendLog(fileCopyLogList);
                }
                #endregion
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        private void InitializeForProcessing(string exportBootParameter)
        {
            if (string.IsNullOrEmpty(_fieldImageFileName))
            {
                var bootParam = GetExportBEO<ExportLoadJobDetailBEO>(exportBootParameter);
                _encodingType = bootParam.ExportLoadFileInfo.ExportLoadFileFormat.EncodingType == EncodingTypeSelection.Ansi ? Encoding.GetEncoding(Constants.Ansi) : Encoding.Unicode;
                if (bootParam.ExportLoadFileInfo.ExportLoadFileOption != null)
                {
                    if (bootParam.ExportLoadFileInfo.PriImgSelection != SetSelection.Dataset)
                    {
                        _includeImages = true;
                    }
                    _isIncludeNative = bootParam.ExportLoadFileInfo.ExportLoadFileOption.IncludeNativeFile;
                    _isIncludeText = bootParam.ExportLoadFileInfo.ExportLoadFileOption.IncludeTextFile;
                    _isNativeTag = !string.IsNullOrEmpty(bootParam.ExportLoadFileInfo.ExportLoadFileOption.TagToIncludeNative);

                    if (!string.IsNullOrEmpty(bootParam.ExportLoadFileInfo.ExportLoadFileOption.ImageFileName))
                    {
                        _fieldImageFileName = bootParam.ExportLoadFileInfo.ExportLoadFileOption.ImageFileName;
                        _isAutoIncrementFileName = bootParam.ExportLoadFileInfo.ExportLoadFileOption.AutoIncrementFileName;
                        if (_dataset.DatasetFieldList != null)
                        {
                            var imageField =_dataset.DatasetFieldList.FirstOrDefault(f => f.Name == _fieldImageFileName);
                            if (imageField != null)
                            {
                               _imageNameFieldType= imageField.FieldType.DataTypeId;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(bootParam.CreatedBy))
                {
                    _createdBy = bootParam.CreatedBy;
                }

                #region DCB - Restored When DCB Export is migrated to Overdrive
                /*
                switch (PipelineType)
                {
                    case PipelineType.ExportLoadFile:
                        {
                            var bootParam = GetExportBEO<ExportLoadJobDetailBEO>((string)BootParameters);
                            if (bootParam.ExportLoadFileInfo.ExportLoadFileOption != null && !string.IsNullOrEmpty(bootParam.ExportLoadFileInfo.ExportLoadFileOption.ImageFileName))
                            {
                                _fieldImageFileName = bootParam.ExportLoadFileInfo.ExportLoadFileOption.ImageFileName;
                            }
                            if (!string.IsNullOrEmpty(bootParam.CreatedBy))
                            {
                                _createdBy = bootParam.CreatedBy;
                            }
                        }
                        break;
                    case PipelineType.ExportDcb:
                        {
                            var bootParam = GetExportBEO<ExportDCBJobDetailBEO>((string)BootParameters);
                            if (bootParam.ExportDCBFileInfo.ExportDCBFileOption != null && !string.IsNullOrEmpty(bootParam.ExportDCBFileInfo.ExportDCBFileOption.ImageFieldName))
                            {
                                _fieldImageFileName = bootParam.ExportDCBFileInfo.ExportDCBFileOption.ImageFieldName;
                            }
                        }
                        break;
                } */
                #endregion
            }
        }

        private void PerformCopy(out List<JobWorkerLog<ExportFileCopyLogInfo>> fileCopyLogList)
        {
            fileCopyLogList = new List<JobWorkerLog<ExportFileCopyLogInfo>>();
            #region New
            var documentDetails = new List<ExportDocumentDetail>();
            var tempFileCopyLogList = new List<JobWorkerLog<ExportFileCopyLogInfo>>();
            var loadFileHelper = new ExportLoadFileHelper(_bootParameter);

            foreach (var doc in _exportDocumentCollection.Documents)
            {
                JobWorkerLog<ExportFileCopyLogInfo> fileCopyLog;
                loadFileHelper.SetImageSourceFiles(doc, _exportDocumentCollection.ExportOption);
                var exportDocument = CopyFiles(doc, out fileCopyLog);
                lock (DocObject)
                {
                    if (exportDocument != null)
                        documentDetails.Add(exportDocument);
                    if (fileCopyLog != null)
                        tempFileCopyLogList.Add(fileCopyLog);
                }
            }
            _exportDocumentCollection.Documents.Clear();
            loadFileHelper.RemoveImageFile(documentDetails, _exportDocumentCollection.ExportOption.ExportDestinationFolderPath);
            _exportDocumentCollection.Documents.AddRange(documentDetails);
            if (tempFileCopyLogList.Any())
            {
                fileCopyLogList.AddRange(tempFileCopyLogList);
            }
            #endregion

            #region Old Debug Purpose
            /*  foreach (ExportDocumentDetail docDetail in _exportDocumentCollection.Documents)
            {
                JobWorkerLog<ExportFileCopyLogInfo> fileCopyLog = null;
                var exportDocument = CopyFiles(docDetail, out fileCopyLog);
                if (exportDocument != null)
                    documentDetails.Add(exportDocument);
                if (fileCopyLog != null)
                    fileCopyLogList.Add(fileCopyLog);

            }
            _exportDocumentCollection.Documents.Clear();
            _exportDocumentCollection.Documents.AddRange(documentDetails);  */
            #endregion
        }

        /// <summary>
        /// Sends this instance.
        /// </summary>
        private void Send()
        {
            _exportDocumentCollection.Dataset = null;
            var message = new PipeMessageEnvelope()
                                {
                                    Body = _exportDocumentCollection
                                };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(_exportDocumentCollection.Documents.Count());
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ExportFileCopyLogInfo>> log)
        {
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        #endregion

        #region Copy
        /// <summary>
        /// Copy document files(Native, Text & Image)
        /// </summary>       
        private ExportDocumentDetail CopyFiles(ExportDocumentDetail documentDetail, out JobWorkerLog<ExportFileCopyLogInfo> fileCopyLog)
        {
            var sucess = true;
            var message = string.Empty;
            var errorBuilder = new StringBuilder();
            try
            {
                string messageInNativeFile;
                var isErrorInNative = !(CopyNativeFiles(documentDetail, out messageInNativeFile));

                string messageInTextFile;
                var isErrorInText = !(CopyTextFiles(documentDetail, out messageInTextFile));

                string messageInImageFile = string.Empty;
                bool isErrorInImage = false;

                if (_includeImages) //Copy the image file only when the include image option selected
                    isErrorInImage = !(CopyImageFiles(documentDetail, out messageInImageFile));

                if (isErrorInNative || isErrorInText || isErrorInImage)
                {
                    sucess = false;
                    errorBuilder.Append(Constants.ExportDCNMessage);
                    errorBuilder.Append(documentDetail.DCN);
                    errorBuilder.Append(Constants.ExportBreak);
                    errorBuilder.Append(messageInNativeFile);
                    errorBuilder.Append(messageInTextFile);
                    errorBuilder.Append(messageInImageFile);
                    message = errorBuilder.ToString();
                }

                fileCopyLog = ConstructLog(documentDetail.CorrelationId, sucess, documentDetail.DCN, isErrorInNative, isErrorInText, isErrorInImage, message);
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Failed to copy document files for job run id:{0}", PipelineId).Trace().Swallow();
                fileCopyLog = ConstructLog(documentDetail.CorrelationId, false, documentDetail.DCN, true, true, true, Constants.ExportFileCopyErrorMessage);
            }
            return documentDetail;
        }

        private bool CopyNativeFiles(ExportDocumentDetail documentDetail, out string messageInNativeFile)
        {
            bool result = true;
            string updatedTargeFiletPath = null;
            var nativeErrorBuilder = new StringBuilder();
            try
            {
                if (documentDetail.NativeFiles != null && documentDetail.NativeFiles.Count > 0)
                {
                    foreach (var file in documentDetail.NativeFiles)
                    {
                        if (file.SourceFilePath == null) continue;

                        var nativeFileName = documentDetail.NativeFileName;

                        if (string.IsNullOrEmpty(nativeFileName))
                        {
                            nativeErrorBuilder.Append(NativeFileError);
                            result = false;
                        }
                        else
                        {
                            nativeFileName = Regex.Replace(nativeFileName, Constants.FileSpecialCharactersRegex, string.Empty); //Remove the illegal character in the filename
                            nativeFileName = string.Concat(nativeFileName, Path.GetExtension(file.SourceFilePath));
                        }
                        if (!result) continue;
                        var targetPath = Path.Combine(file.DestinationFolder, nativeFileName);
                        bool retry;
                        do
                        {
                            try
                            {
                                updatedTargeFiletPath = string.Empty;
                                var startIncrementIndex = 2;
                                //Check file exists or not, if we have an overlapping file the second record with the same name would get a (2) after the file name.
                                //Example: if sample.txt already exists then it will copy the second, third,... with name as sample (2).txt, sample (3).txt, ...
                                var isFileExists = IsFileExists(targetPath, out updatedTargeFiletPath, ref startIncrementIndex);
                                if (!isFileExists)
                                    File.Copy(file.SourceFilePath, updatedTargeFiletPath);
                                retry = false;
                            }
                            catch (IOException ioEx)
                            {
                                var hResult = System.Runtime.InteropServices.Marshal.GetHRForException(ioEx);
                                if (hResult == ExceptionHr)  //Capture "File already exists" exception only
                                {
                                    var fileName = GetFileNameForDuplicates(Path.GetFileName(updatedTargeFiletPath), file.DestinationFolder);
                                    targetPath = Path.Combine(file.DestinationFolder, fileName);
                                    retry = true;
                                }
                                else
                                {
                                    nativeErrorBuilder.Append(Constants.ExportNativeTextCopyErrorMessage);
                                    nativeErrorBuilder.Append(file.SourceFilePath);
                                    nativeErrorBuilder.Append(Constants.ExportBreakMessage);
                                    throw;
                                }
                            }
                        } while (retry);
                        file.DestinationFolder = updatedTargeFiletPath;
                    }
                }
                else if (_isIncludeNative && documentDetail.IsNativeTagExists)
                {
                    //If IncludeNative option and Inlucde Native for Taged document option is selected and this document belogs to that tag and it has no native file then we log it as error.
                    nativeErrorBuilder.Append(Constants.MsgMissingNativeFiles);
                    result = false;
                }
                else if (_isIncludeNative && !_isNativeTag)
                {
                    //If include native option selected and not selected the include native for tagged doc only option, then we have to check native exists for all the selected documents ot not. 
                    nativeErrorBuilder.Append(NativeFileError);
                    result = false;
                }

            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Export File Copy Worker: Unable to copy native files for job run id: {0}", PipelineId).Trace().Swallow();
                result = false;
            }
            messageInNativeFile = nativeErrorBuilder.ToString();
            return result;
        }

        /// <summary>
        /// Get FileName For Duplicates documents in a directory
        /// Create running number for duplicate document to differentiate from previous one.  e.g: source(1).pdf,source(2).pdf ...
        /// </summary>     
        private string GetFileNameForDuplicates(string fileName, string path)
        {
            var count = 1;
            var prefix = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            while (File.Exists(Path.Combine(path, fileName)))
            {
                fileName = prefix + "(" + count + ")" + ext;
                count++;
            }
            return fileName;
        }
        

        private bool CopyTextFiles(ExportDocumentDetail documentDetail, out string errorMessage)
        {
            var result = true;
            var textErrorBuilder = new StringBuilder();
            try
            {
                if (documentDetail.TextFiles != null && documentDetail.TextFiles.Count > 0)
                {
                    foreach (var file in documentDetail.TextFiles)
                    {
                        var targetPath = string.Empty;
                        GetFileName(documentDetail, ref result, textErrorBuilder, file, ref targetPath);
                        if (!result) continue;
                        bool retry;
                        do
                        {
                            try
                            {
                                if (file.IsTextFieldExportEnabled)
                                {
                                    var contentFieldValue = string.Empty;
                                    var textFieldId = DocumentBO.GetFieldIdByNameForExportJob(_dataset, _exportDocumentCollection.ExportOption.TextFieldToExport, false);
                                    var field = _dataset.DatasetFieldList.FirstOrDefault(f => f.Name.ToLower().Equals(_exportDocumentCollection.ExportOption.TextFieldToExport.ToLower()));

                                    if (field != null && field.FieldType != null)
                                    {
                                        if (field.FieldType.DataTypeId == ContentFieldTypeID)
                                        {
                                             contentFieldValue =
                                                DocumentBO.GetDocumentContentFieldsForExportJob(Convert.ToInt64(_exportLoadJobDetailBeo.MatterId),_dataset.CollectionId, documentDetail.DocumentId);
                                             contentFieldValue = DocumentBO.Base64DecodeForExportJob(contentFieldValue);  //Decode the content field
                                              
                                        }
                                        else
                                        {
                                             contentFieldValue =
                                                DocumentBO.GetFieldValueForExportJob(Convert.ToInt64(_exportLoadJobDetailBeo.MatterId),_dataset.CollectionId, documentDetail.DocumentId, textFieldId);
                                        }
                                    }

                                    File.WriteAllText(targetPath, contentFieldValue, _encodingType);
                                }
                                else
                                {
                                    File.Copy(file.SourceFilePath, targetPath);
                                }
                                retry = false;
                            }
                            catch (IOException ioEx)
                            {
                                result = false;
                                var hResult = System.Runtime.InteropServices.Marshal.GetHRForException(ioEx);
                                if (hResult == ExceptionHr)  //Capture "File already exists" exception only and generate the new filename and retry to copy it again
                                {
                                    var fileName = GetFileNameForDuplicates(Path.GetFileName(targetPath), file.DestinationFolder);
                                    targetPath = Path.Combine(file.DestinationFolder, fileName);
                                    retry = true;
                                }
                                else
                                {
                                    textErrorBuilder.Append(Constants.ExportNativeTextCopyErrorMessage);
                                    textErrorBuilder.Append(file.SourceFilePath);
                                    textErrorBuilder.Append(Constants.ExportBreakMessage);
                                    retry = false;
                                }
                            }
                            catch (Exception)
                            {
                                result = false;
                                textErrorBuilder.Append(Constants.ExportNativeTextCopyErrorMessage);
                                textErrorBuilder.Append(file.SourceFilePath);
                                textErrorBuilder.Append(Constants.ExportBreakMessage);
                                retry = false;
                            }
                        } while (retry);
                        file.DestinationFolder = targetPath;
                    }
                }
                else if (_isIncludeText)
                {
                    textErrorBuilder.Append(TextFileError);
                    result = false;
                }
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Export File Copy Worker: Unable to copy text files for job run id: {0}", PipelineId).Trace().Swallow();
                result = false;
            }
            errorMessage = textErrorBuilder.ToString();
            return result;
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <param name="documentDetail">The document detail.</param>
        /// <param name="result">if set to <c>true</c> [result].</param>
        /// <param name="textErrorBuilder">The text error builder.</param>
        /// <param name="file">The file.</param>
        /// <param name="targetPath">The target path.</param>
        private static void GetFileName(ExportDocumentDetail documentDetail, ref bool result, StringBuilder textErrorBuilder, ExportFileInformation file, ref string targetPath)
        {
            var txtFileName = documentDetail.TextFileName;
            if (string.IsNullOrEmpty(txtFileName))
            {
                textErrorBuilder.Append(TextFileError);
                result = false;
            }
            else
            {
                txtFileName = Regex.Replace(txtFileName, Constants.FileSpecialCharactersRegex, string.Empty); //Remove the illegal character in the filename
                targetPath = Path.Combine(file.DestinationFolder, txtFileName + TxtExtension);
            }
            var updatedTargeFiletPath = string.Empty;
            var startIncrementIndex = 2;
            //Check file exists or not, if we have an overlapping file the second record with the same name would get a (2) after the file name.
            //Example: if sample.txt already exists then it will copy the second, third,... with name as sample (2).txt, sample (3).txt, ...
            var isFileExists = IsFileExists(targetPath, out updatedTargeFiletPath, ref startIncrementIndex);
            if (!isFileExists)
            {
                targetPath = updatedTargeFiletPath;
            }

        }

        /// <summary>
        /// Copy Image Files
        /// </summary>
        /// <param name="documentDetail">documentDetail</param>
        /// <param name="errorMessage">errorMessage</param>
        /// <returns></returns>
        private bool CopyImageFiles(ExportDocumentDetail documentDetail, out string errorMessage)
        {

            #region assertion
            documentDetail.ShouldNotBe(null);
            #endregion

            var result = true;
            var imageErrorBuilder = new StringBuilder();
            try
            {
                var pageCount = 0;

                if (documentDetail.ImageFiles != null && documentDetail.ImageFiles.Count > 0)
                {

                    var imageFieldId = DocumentBO.GetFieldIdByNameForExportJob(_dataset, _fieldImageFileName, false);
                    var fieldValue = DocumentBO.GetFieldValueForExportJob(Convert.ToInt64(_exportLoadJobDetailBeo.MatterId), _dataset.CollectionId, documentDetail.DocumentId, imageFieldId);

                    var imageFileName = (!string.IsNullOrEmpty(fieldValue)) ?fieldValue: string.Empty;
                   
                    imageFileName = Regex.Replace(imageFileName, Constants.FileSpecialCharactersRegex, string.Empty); //Remove the illegal character in the filename
                    if (string.IsNullOrEmpty(imageFileName))
                    {
                        imageErrorBuilder.Append(Constants.ExportImageNoValueCopyErrorMessage);
                        errorMessage = imageErrorBuilder.ToString();
                        return false;
                    }
                    Int64 noOfImageFiles = documentDetail.ImageFiles.Count;
                    var imgFileNames = new List<string>();
                    if (_lstBatesAndDpnFieldTypes.Contains(_imageNameFieldType) && imageFileName.Contains(','))//if  field value contains , then it means field is bates field
                    {
                        imgFileNames = GetFileNamesForCommaSeperatedFields(imageFileName, noOfImageFiles, imgFileNames);
                    }
                    else
                    {
                        bool status = GetFileNamesWithStatus(ref imgFileNames, noOfImageFiles, imageFileName, out errorMessage);
                        if (!status) return false;
                    }
                    CopyFilesToTargetPath(documentDetail, ref result, imageErrorBuilder, ref pageCount, imgFileNames);
                }
                else
                {
                    imageErrorBuilder.Append(Constants.ExportImageNoValueCopyErrorMessage);
                    errorMessage = imageErrorBuilder.ToString();
                    return false;
                }

            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Export File Copy Worker: Unable to copy image files for job run id: {0}", PipelineId).Trace().Swallow();
                result = false;
            }
            errorMessage = imageErrorBuilder.ToString();
            return result;
        }

        /// <summary>
        /// Gets the milti page file names.
        /// </summary>
        /// <param name="imgFileNames">The img file names.</param>
        /// <param name="noOfImageFiles">The no of image files.</param>
        /// <param name="imageFileName">Name of the image file.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>status true/false</returns>
        private bool GetFileNamesWithStatus(ref List<string> imgFileNames, long noOfImageFiles, string imageFileName, out string errorMessage)
        {
            if (noOfImageFiles > 1) //Single image per page document
            {
                if (!_isAutoIncrementFileName)
                {
                    //if it is normal field ,  field value + running  number will be the image file name 
                    for (var i = 1; i <= noOfImageFiles; i++)
                    {
                        imgFileNames.Add(imageFileName + "_" + i.ToString("D4"));
                    }
                }
                else
                {
                    var lastChar = imageFileName.Last(); //Get the last character of the field 
                    var intLastChar = (int)lastChar;
                    int numericLastChar;
                    var isNumbericIncrement = int.TryParse(lastChar.ToString(CultureInfo.InvariantCulture), out numericLastChar);

                    if (isNumbericIncrement || (intLastChar >= 65 && intLastChar <= 90) || (intLastChar >= 97 && intLastChar <= 122))
                    {
                        //In field values, the last value ends with numbers then it whould be increment by numbers
                        //Example: If the fieldvalue is Test001 and it has 5 pages then the filename should be Test001, Test002,Test003,Test004 & Test005
                        if (isNumbericIncrement)
                            GetFileNamesForNumberIncrement(imageFileName, noOfImageFiles, imgFileNames);
                        else
                            GetFileNamesForLetterIncrement(imageFileName, noOfImageFiles, imgFileNames, lastChar);
                    }
                    else
                    {
                        //Throw error if last char is a special character; A=65; Z=90; a=97; z=122
                        Tracer.Error(ErrorMessage);
                        errorMessage = ErrorMessage;
                        return false;
                    }

                }
            }
            else
            {
                imgFileNames.Add(imageFileName);
            }
            errorMessage = string.Empty;
            return true;
        }



        /// <summary>
        /// Gets the file names for comma seperated fields.
        /// </summary>
        /// <param name="imageFileName">Name of the image file.</param>
        /// <param name="noOfImageFiles">The no of image files.</param>
        /// <param name="imgFileNames">The img file names.</param>
        /// <returns></returns>
        private static List<string> GetFileNamesForCommaSeperatedFields(string imageFileName, Int64 noOfImageFiles, List<string> imgFileNames)
        {
            //Tracer.Trace("Bates /Dpn configuration");
            imgFileNames = imageFileName.Split(',').ToList();
            Int64 differenceOfBatesNosAndImageFiles = imgFileNames.Count() - noOfImageFiles;
            //Tracer.Trace("Difference of Image Files And Configured Image File Names", differenceOfBatesNosAndImageFiles);
            if (differenceOfBatesNosAndImageFiles > 0)//if  page no's provide less than image files
            {
                string lastPageNameinBatesFieldValues = imgFileNames.Last();
                //get the highest page no:For example   if bates holds "MyBates01,MyBates02" then 3 will be the highest page no in the bates field value
                Match highestPageNoInBatesFieldValues = Regex.Match(lastPageNameinBatesFieldValues, @"\d+");
                long highestPageNo = 0;
                //get the page name of the page bates field values :For example   if bates holds "MyBates01,MyBates02" then MyBates will be the page name
                string pageNameInBateFieldValues = lastPageNameinBatesFieldValues.Substring(0,
                                                                                     lastPageNameinBatesFieldValues
                                                                                         .Count() -
                                                                                     highestPageNoInBatesFieldValues
                                                                                         .Value.Count());
                long.TryParse(highestPageNoInBatesFieldValues.Value, out highestPageNo);
                //generate the image file names for the reaming image files
                while (differenceOfBatesNosAndImageFiles-- != 0)
                {

                    imgFileNames.Add(pageNameInBateFieldValues + ((++highestPageNo).ToString().PadLeft(highestPageNoInBatesFieldValues.Value.Length, '0')));
                }

            }
            return imgFileNames;
        }

        /// <summary>
        /// Files the names for letter increment.
        /// </summary>
        /// <param name="imageFileName">Name of the image file.</param>
        /// <param name="noOfImageFiles">The no of image files.</param>
        /// <param name="imgFileNames">The img file names.</param>
        /// <param name="lastChar">The last character.</param>
        private void GetFileNamesForLetterIncrement(string imageFileName, Int64 noOfImageFiles, List<string> imgFileNames, char lastChar)
        {
            //In field values, the last value ends with letter then it should be increment by letters like a,b,c, etc
            //Example: If the fieldvalue is Test001x and it has 5 pages then the filename should be Test001x, Test001y,Test001z,Test001aa & Test001ab
            var sufixValues = GetSufixForLetterAutoIncrement(Convert.ToInt32(noOfImageFiles), lastChar.ToString(CultureInfo.InvariantCulture));
            foreach (var sufix in sufixValues)
            {
                var appendValue = sufix.ToLower();
                //If its the capital letter the convert the value to uppercase, by default it will be in lowercase
                if (lastChar >= 65 && lastChar <= 90)
                    appendValue = sufix.ToUpper();

                imgFileNames.Add(imageFileName.Substring(0, imageFileName.Length - 1) + appendValue);
            }
        }

        /// <summary>
        /// Files the names for number increment.
        /// </summary>
        /// <param name="imageFileName">Name of the image file.</param>
        /// <param name="noOfImageFiles">The no of image files.</param>
        /// <param name="imgFileNames">The img file names.</param>
        private static void GetFileNamesForNumberIncrement(string imageFileName, Int64 noOfImageFiles, List<string> imgFileNames)
        {
            //It will get the number which exists at the end of the string Ex: for DCN000010 it will return the value as 000010
            var highestPageNoInBatesFieldValues = Regex.Split(imageFileName, @"\D+").LastOrDefault();
            //Assign that value as startingNumber Ex: 000010 
            var startingNumber = Convert.ToInt32(highestPageNoInBatesFieldValues);
            string numberOfDigits = string.Empty;
            //This should return the number of zeros to format the number Ex:000010 will be considered as 000010 instead of 10
            highestPageNoInBatesFieldValues.SafeForEach(f => numberOfDigits += "0");
            for (long i = 1; i <= noOfImageFiles; i++)
            {
                //Format the number (Ex:000010 will be considered as 000010 instead of 10) and append to the filename Ex:it will increment as  DCN000010, DCN000011, etc
                if (highestPageNoInBatesFieldValues != null)
                    imgFileNames.Add(imageFileName.Substring(0, imageFileName.Length - highestPageNoInBatesFieldValues.Length) + startingNumber.ToString(numberOfDigits));
                startingNumber++;
            }
        }

        /// <summary>
        /// Copies the files to target path.
        /// </summary>
        /// <param name="documentDetail">The document detail.</param>
        /// <param name="result">if set to <c>true</c> [result].</param>
        /// <param name="imageErrorBuilder">The image error builder.</param>
        /// <param name="pageCount">The page count.</param>
        /// <param name="imgFileNames">The img file names.</param>
        private static void CopyFilesToTargetPath(ExportDocumentDetail documentDetail, ref bool result, StringBuilder imageErrorBuilder, ref int pageCount, List<string> imgFileNames)
        {
            foreach (var file in documentDetail.ImageFiles)
            {
                try
                {
                    var updatedTargeFiletPath = string.Empty;
                    var fileName = imgFileNames[pageCount] + Path.GetExtension(file.SourceFilePath);
                    var targetPath = Path.Combine(file.DestinationFolder, fileName);

                    var startIncrementIndex = 2;
                    //Check file exists or not, if we have an overlapping file the second record with the same name would get a (2) after the file name.
                    //Example: if sample.txt already exists then it will copy the second, third,... with name as sample (2).txt, sample (3).txt, ...
                    var isFileExists = IsFileExists(targetPath, out updatedTargeFiletPath, ref startIncrementIndex);
                    if (!isFileExists)
                        File.Copy(file.SourceFilePath, updatedTargeFiletPath);
                    file.DestinationFolder = updatedTargeFiletPath;
                }
                catch (Exception)
                {
                    result = false;
                    imageErrorBuilder.Append(Constants.ExportImageCopyErrorMessage);
                    imageErrorBuilder.Append(file.SourceFilePath);
                    imageErrorBuilder.Append(Constants.ExportBreakMessage);
                }
                pageCount++;
            }
        }


        /// <summary>
        /// Determines whether [is file exists] [the specified file path].
        /// Its the recursive method to check the exist file and update the file name (if exists) and copy it to target.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="targetFilePath">The target path.</param>
        /// <param name="incrementValue">The increment value.</param>
        /// <returns></returns>
        private static bool IsFileExists(string filePath, out string targetFilePath, ref int incrementValue)
        {
            targetFilePath = filePath;
            if (!File.Exists(filePath)) return false;
            //Get the directory from the path.
            var directory = Path.GetDirectoryName(filePath);
            //Get the file name without extension.
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            if (fileNameWithoutExtension == null) return false;
            //Check whether the file name has value like (2) to do increment as (3),(4),...
            if (fileNameWithoutExtension.LastIndexOf(OpenBrace) >= 0 && fileNameWithoutExtension.Last() == ClosedBrace)
            //If file name has value like (2) example sample (2).txt then get only sample
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                if (nameWithoutExtension != null)
                    fileNameWithoutExtension = nameWithoutExtension.Remove(fileNameWithoutExtension.LastIndexOf(OpenBrace));
            }
            //Get the file extension
            var fileExtension = Path.GetExtension(filePath);
            //If file exists in target then update the value like sample (2).txt, sample (3).txt, ... recursively
            if (directory == null) return false;
            var updatedPath = Path.Combine(directory, fileNameWithoutExtension.TrimEnd() + OpenBraceWithSpace + incrementValue + ClosedBrace + fileExtension);
            targetFilePath = updatedPath; //assign the final path to the output parameter
            incrementValue = incrementValue + 1;
            //Call the method recursively till it creates the file which is not exists in the target path
            IsFileExists(updatedPath, out targetFilePath, ref incrementValue);
            return false;
        }

        /// <summary>
        /// Gets the sufix for letter automatic increment.
        /// </summary>
        /// <param name="totalPages">The total pages.</param>
        /// <param name="startLetter">The start letter.</param>
        /// <returns>list of string</returns>
        private static IEnumerable<string> GetSufixForLetterAutoIncrement(int totalPages, string startLetter)
        {
            startLetter = startLetter.ToLower();
            //a=97; z=122; so get index of letter we should getASCII value of a letter and minus 97 Ex: For b ASCII value is 98 so index is 98-97 = 1
            var indexToStart = Convert.ToChar(startLetter) - 97;
            return GetSufixLetters(totalPages, indexToStart).ToList();
        }

        /// <summary>
        /// Calculates the sufix letter.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>string</returns>
        public static string CalculateSufixLetter(int t)
        {
            var d = t;
            var name = string.Empty;
            while (d > 0)
            {
                var mod = (d - 1) % 26;
                name = Convert.ToChar('a' + mod) + name;
                d = (d - mod) / 26;
            }
            return name;
        }

        /// <summary>
        /// Gets the sufix letters.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="t">The t.</param>
        /// <returns>list of string</returns>
        public static IEnumerable<string> GetSufixLetters(long n, int t)
        {
            for (var i = 1; i <= n; i++)
            {
                yield return CalculateSufixLetter(t + i);
            }
        }

        /// <summary>
        /// Construct Log Data
        /// </summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="success">if set to <c>true</c> [success].</param>
        /// <param name="dcn">The DCN.</param>
        /// <param name="isErrorInNative">if set to <c>true</c> [is error in native].</param>
        /// <param name="isErrorInText">if set to <c>true</c> [is error in text].</param>
        /// <param name="isErrorInImage">if set to <c>true</c> [is error in image].</param>
        /// <param name="message">The message.</param>
        /// <returns>ExportFileCopyLogInfo</returns>
        public JobWorkerLog<ExportFileCopyLogInfo> ConstructLog(string correlationId, bool success, string dcn, bool isErrorInNative, bool isErrorInText, bool isErrorInImage, string message)
        {
            var metadataLog = new JobWorkerLog<ExportFileCopyLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = (!string.IsNullOrEmpty(correlationId)) ? Convert.ToInt64(correlationId) : 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.ExportFileCopyWorkerRoleType,
                Success = success,
                CreatedBy = _createdBy,
                IsMessage = false,
                LogInfo =
                    new ExportFileCopyLogInfo
                    {
                        DCN = dcn,
                        IsErrorInField = false,
                        IsErrorInNativeFile = isErrorInNative,
                        IsErrorInImageFile = isErrorInImage,
                        IsErrorInTextFile = isErrorInText,
                        Information = (!string.IsNullOrEmpty(message) ? (message) : string.Empty)
                    }
            };
            return metadataLog;
        }


        /// <summary>
        /// Inserts the audit log.
        /// </summary>
        /// <param name="exportDocumentCollection">The export document collection.</param>
        /// <param name="sJobName">Name of the s job.</param>
        private void InsertAuditLog(ExportDocumentCollection exportDocumentCollection, string sJobName)
        {
            try
            {
                Utility.SetUserSession(_exportLoadJobDetailBeo.CreatedBy);
                var documentIdentifierEntities = exportDocumentCollection.Documents.Select(exportDocumentDetail => new DocumentIdentifierEntityBEO
                                                                                                                   {
                                                                                                                       CollectionId = exportDocumentCollection.Dataset.CollectionId,
                                                                                                                       CollectionName = exportDocumentCollection.Dataset.FolderName, 
                                                                                                                       Dcn = exportDocumentDetail.DCN,
                                                                                                                       DocumentReferenceId = exportDocumentDetail.DocumentId
                                                                                                                   }).ToList();

                AuditBO.LogDocumentsExported(exportDocumentCollection.Dataset.Matter.FolderID,
                    documentIdentifierEntities, sJobName);
            }
            catch (Exception ex)
            {
                Tracer.Info("Failure on insert audit log for export {0}", ex.Message);
            }
        }

        #endregion

        #region Common
        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private T GetExportBEO<T>(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            using (var stream = new StringReader(bootParamter))
            {

                //Ceating xmlStream for xmlserialization
                var xmlStream = new XmlSerializer(typeof(T));

                //Deserialization of bootparameter to get ImportBEO
                return (T)xmlStream.Deserialize(stream);
            }
        }
        #endregion
    }
}
