//-----------------------------------------------------------------------------------------
// <copyright file="LoadFileParserWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil Paramathma</author>
//      <description>
//          Class file for holding document detail extraction methods
//      </description>
//      <changelog>//          
//          <date value="21-May-2012">Fix for overdrive error 100849</date>
//          <date value="05/21/2012">Fix for error in overdrive log</date>//          <date value="12/18/2012">Fixed defect#: 114062 - Schedule details given in the 'Create import job' page is not getting  retained after navigating to' Configure settings' page and coming back to 'Create Import job' page.<date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.DataAccess.ServerManagement;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;


namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Business.MatterManagement;
    using Tracer = LexisNexis.Evolution.Infrastructure.Tracer;
    using System.Data;
    using LexisNexis.Evolution.Business.IR;

    /// <summary>
    /// Load File Manager: Parse Load File(DAT,CSV, TXT...) 
    /// </summary>
    public class LoadFileParserWorker : WorkerBase, IDisposable
    {
        #region Properties

        private Uri m_LoadFileUri;

        private char m_ColumnDelimiter;
        private char m_QuoteCharacter;
        private char m_NewlineCharacter;

        private Encoding m_EncodingType;
        private bool m_IsFirstLineHeader;
        private ImportBEO m_Parameters;
        private int m_BatchSize = 100;
        private StreamReader m_StreamReader;
        private uint m_CurrentRecordNumber;
        private DatasetBEO m_Dataset;
        private string m_DatasetPath;
        private HelperFileParser _imageHelperFileParser { get; set; }
        private bool _isImageUpdated;
        private HelperFile TextHelperFile { get; set; }
        private string m_CurrentDcn = string.Empty, m_NewDcn = string.Empty;
        private int m_ContentFieldNumber;

        const string ColumnJobId = "JobId";
        const string ColumnDocumentKey = "DocumentKey";
        const string ColumnImagePaths = "ImagePaths";

        private RecordTokenizer m_RecordTokenizer;

        /// Unique identifier associated with a load file.
        private string _uniqueThreadString;

        #endregion

        #region Load File

        private struct Interval
        {
            public uint StartLineNumber { get; set; }
            public uint Count { get; set; }
        }

        private const char Delimiter = ',';
        private const string Yes = "Y";

        private class HelperFile
        {
            public HelperFile(LoadFileParserWorker parent, string filePath)
            {
                FilePath = filePath;
                RecordCount = 0;
                uint lineCount = 0;

                var isFirst = true;
                uint docNumber = 1;
                Index = new Dictionary<uint, uint>();
                Data = new List<string>();
                using (var sr = new StreamReader(filePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var columns = sr.ReadLine().Split(new[] {Delimiter});
                        lineCount++;

                        if (columns.Length < 2)
                        {
                            continue; // Skip empty lines
                        }

                        if (columns.Length != 7)
                        {
                            var errorMessage =
                                String.Format(
                                    "File {0} has unrecognized record at line {1}. 7 columns are expected, but {2} are found.",
                                filePath, lineCount, columns.Length);
                            parent.LogMessage(false, errorMessage);
                            Tracer.Error(errorMessage);
                            continue;
                        }

                        Data.Add(columns[2]);
                        if (isFirst)
                        {
                            Index.Add(docNumber, RecordCount);
                            docNumber++;
                            isFirst = false;
                        }
                        else
                        {
                            if (columns[3].ToUpper().Trim() == Yes)
                            {
                                Index.Add(docNumber, RecordCount);
                                docNumber++;
                            }
                        }
                        RecordCount++;
                    }
                }
            }

            public string FilePath { get; private set; }
            public uint RecordCount { get; private set; }

            // documentNumber to the first line in the file for this document
            public Dictionary<uint, uint> Index { get; private set; }
            public List<string> Data { get; private set; }

            /// <summary>
            /// Method to get the document interval
            /// </summary>
            /// <param name="docIndex">uint</param>
            /// <returns>interval object</returns>
            private Interval GetDocumentInterval(uint docIndex)
            {
                Index.ShouldNotBe(null);
                var interval = new Interval();
                if (Index.ContainsKey(docIndex))
                {
                    interval.StartLineNumber = Index[docIndex];
                    var endLineNumber = (docIndex == Index.Count) ? RecordCount - 1 : Index[docIndex + 1] - 1;
                    interval.Count = endLineNumber - interval.StartLineNumber + 1;
                }
                return interval;
            }

            public List<string> GetFileList(uint docIndex)
            {
                var interval = GetDocumentInterval(docIndex);
                return Data.GetRange((int) interval.StartLineNumber, (int) interval.Count);
            }
        }

        /// <summary>
        /// Parse helper file and combine data based on key 
        /// </summary>
        public class HelperFileParser
        {
            public HelperFileParser(LoadFileParserWorker parent, string filePath)
            {

                uint lineCount = 0;
                using (var sr = new StreamReader(filePath))
                {
                    var filePaths = new List<string>();
                    var keyData = string.Empty;
                    var isFirstRecord = true;
                    FileData = new Dictionary<string, List<string>>();
                    while (!sr.EndOfStream)
                    {
                        var columns = sr.ReadLine().Split(new[] { Delimiter });
                        lineCount++;

                        if (columns.Length < 2)
                        {
                            continue; // Skip empty lines
                        }

                        if (columns.Length != 7)
                        {
                            var errorMessage =
                                String.Format(
                                    "File {0} has unrecognized record at line {1}. 7 columns are expected, but {2} are found.",
                                    filePath, lineCount, columns.Length);
                            parent.LogMessage(false, errorMessage);
                            Tracer.Error(errorMessage);
                            continue;
                        }

                        if (isFirstRecord)
                        {
                            keyData = columns[0];  //Mapping Field Key
                            filePaths.Add(columns[2]); //File Path                           
                        }


                        if (columns[3].ToUpper().Trim() == Yes && !isFirstRecord)
                        {
                            if (FileData.ContainsKey(keyData))
                            {
                                FileData[keyData].AddRange(filePaths);
                            }
                            else
                            {
                                FileData.Add(keyData, filePaths);
                            }
                            filePaths = new List<string>();
                            keyData = columns[0];
                        }
                        if (!isFirstRecord)
                        {
                            filePaths.Add(columns[2]);
                        }
                        isFirstRecord = false;
                    }
                    //For Last records
                    if (FileData.ContainsKey(keyData))
                    {
                        FileData[keyData].AddRange(filePaths);
                    }
                    else
                    {
                        FileData.Add(keyData, filePaths);
                    }
                }
            }
            public Dictionary<string, List<string>> FileData { get; set; }

        }

        protected override bool GenerateMessage()
        {
            if (!_isImageUpdated)
            {
                if (_imageHelperFileParser != null && _imageHelperFileParser.FileData != null)
                {
                    BulkAddLoadFileImageRecords(WorkAssignment.JobId, _imageHelperFileParser.FileData);
                    _imageHelperFileParser.FileData.Clear();
                }
                _isImageUpdated = true;
            }

            if (m_IsFirstLineHeader && m_CurrentRecordNumber == 0)
            {
                m_StreamReader.ReadLine(); // Skip over header record

                // Debugging
                //for (int i = 0; i < 84600; i++)
                //{
                //    m_StreamReader.ReadLine();
                //}
            }
            var recordList = new List<LoadFileRecord>();
            while (!m_StreamReader.EndOfStream)
            {
                var record = m_StreamReader.ReadLine();
                m_CurrentRecordNumber++;

                if (record.IndexOf(m_NewlineCharacter) != -1)
                {
                    record = record.Replace(m_NewlineCharacter.ToString(CultureInfo.InvariantCulture), "\r\n");
                }

                try
                {
                    var loadFileRecord = ConstructLoadFileRecord(record, m_CurrentRecordNumber);
                    recordList.Add(loadFileRecord);
                }
                catch (Exception ex)
                {
                    LogMessage(false, ex.Message);
                    ex.Trace();
                }

                if (recordList.Count%m_BatchSize == 0)
                {
                    Send(recordList);
                    return false;
                }
            }

            if (recordList.Any())
            {
                Send(recordList);
            }
            LogMessage(true, Constants.ParserSuccessMessage);
            return true;
        }

        private void AssignDocumentControlNumber(List<LoadFileRecord> records)
        {
            long numericPartOfDcn = 0;

            #region Delegate - logic to get complete DCN readily, given the numeric value as input

            Func<string, string> getDcn = delegate(string newLastDcnNumericPart)
            {
                var padString = string.Empty;
                // pad zeros
                if (newLastDcnNumericPart.Length < m_Dataset.DCNStartWidth.Length)
                {
                    var numberOfZerosTobePadded = m_Dataset.DCNStartWidth.Length - newLastDcnNumericPart.Length;

                    for (var i = 0; i < numberOfZerosTobePadded; i++)
                    {
                        padString += "0";
                    }
                }
                return m_Dataset.DCNPrefix + padString + newLastDcnNumericPart;
            };

            #endregion Delegate - logic to get complete DCN readily, given the numeric value as input

            using (var lowerTransScope = new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                m_CurrentDcn = DataSetBO.GetLastDocumentControlNumber(m_Dataset.FolderID);

                // If DCN is not obtained, no documents are imported for the dataset till now.
                // So set current DCN to first DCN value.
                if (string.IsNullOrWhiteSpace(m_CurrentDcn))
                {
                    m_CurrentDcn = m_Dataset.DCNPrefix + Constants.StringZero;
                }
                else
                {
                    if (!m_CurrentDcn.Contains(m_Dataset.DCNPrefix))
                    {
                        var currentNumber = Convert.ToInt32(m_CurrentDcn);
                            currentNumber = currentNumber - 1;
                            m_CurrentDcn = currentNumber.ToString();
                            m_CurrentDcn = m_Dataset.DCNPrefix + m_CurrentDcn;
                    }
                }
                // 1) Get Last DCN from EVMaster DB and 2) Pick Numeric part of it
                // throws exception if numeric part couldn't be retrieved, throw Exception.
                if (IsNumeric(m_CurrentDcn.Substring(m_Dataset.DCNPrefix.Length), out numericPartOfDcn))
                {
                    // Update new DCN after bulk add, assuming bulk add would be successful.
                    // The delegate, GetNewLastDCNAfterBulkAdd gets DCN to be updated back to DB.
                    // Delegates takes numeric part of WOULD BE DCN value as input, returns complete DCN - so that it can readily be updated back to Dataset table.
                    m_NewDcn = getDcn((numericPartOfDcn + records.Count()).ToString(CultureInfo.InvariantCulture));
                    DataSetBO.UpdateLastDocumentControlNumber(m_Dataset.FolderID, m_NewDcn);
                    lowerTransScope.Complete();
                }
                else
                {
                    throw new Exception(ErrorCodes.InvalidDCNValueObtainedForDataset);
                }
            }

            #region Assign DCN to all documents

            var dCNIncrementalCounter = numericPartOfDcn;
            records.ForEach(p =>
            {
                dCNIncrementalCounter += 1;
                p.DocumentControlNumber = getDcn(dCNIncrementalCounter.ToString(CultureInfo.InvariantCulture));
            });

            #endregion
        }

        /// <summary>
        /// Determines whether the specified value is numeric.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="result">The result.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is numeric; otherwise, <c>false</c>.
        /// </returns>
        private bool IsNumeric(string value, out long result)
        {
            result = 0;
            try
            {
                result = Int64.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Construct Record(Load File Record) Information
        /// </summary>
        /// <param name="recordText"></param>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        private LoadFileRecord ConstructLoadFileRecord(string recordText, uint recordNumber)
        {
            var record = new LoadFileRecord
            {
                CorrelationId = recordNumber.ToString(CultureInfo.InvariantCulture),
                //  CorrelationId = Guid.NewGuid().ToString(),  Reason : In Table TaskId column support int data type only
                RecordNumber = recordNumber
            };


            if (null != m_Parameters.LoadFile.ContentFile &&
                m_Parameters.LoadFile.ContentFile.TextExtractionOption == LoadFileTextExtractionOption.BodyTextField)
            {
                record.ContentFile = ExtractContentToFile(ref recordText, recordNumber);
            }
            else
            {
                record.ContentFile = GetContentFilePathList(recordNumber);
            }

            // RecordText assignment is moved to the end, because ExtractContentToFile can possibly change recordText
            record.RecordText = recordText;

            return record;
        }

        private List<string> ExtractContentToFile(ref string recordText, uint recordNumber)
        {
            var extractedContent = m_RecordTokenizer.GetContentFieldValueAndRemoveContentInRecord(ref recordText,
                m_ContentFieldNumber);
            if (recordText.Length > 40960)
            {
                var msg =
                    String.Format(
                        "Document record number {0} is skipped: after document content is removed the metadata fields are still bigger than 40K. Possibly malformed document record.",
                    recordNumber);
                throw new ApplicationException(msg);
            }
            var extractedContentFilePath = (m_DatasetPath.EndsWith(@"\") ? m_DatasetPath : (m_DatasetPath + @"\")) +
                                           Guid.NewGuid().ToString().Replace("-", "").ToUpper() +
                                           DateTime.UtcNow.ToString(Constants.DateFormat) + Constants.TextFileExtension;
            File.WriteAllText(extractedContentFilePath, extractedContent, Encoding.UTF8);
            return new List<string> {extractedContentFilePath};
        }

        /// <summary>
        /// Parse Content Helper file(LOG/OPT) and get file lists.
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        private List<string> GetContentFilePathList(uint recordNumber)
        {
            var contentFile = new List<string>();
            try
            {
                var lsTextFilePath = new List<string>();
                if (TextHelperFile != null)
                {
                    var lsTextFile = TextHelperFile.GetFileList(recordNumber);

                    if (m_Parameters.LoadFile.ContentFile.TextFilePathSubstitution != null)
                    {
                        var txtFilePathSubstitution = m_Parameters.LoadFile.ContentFile.TextFilePathSubstitution;
                        lsTextFilePath.AddRange(lsTextFile.Select(path => PathSubstituion(path, txtFilePathSubstitution)));
                    }
                    else
                    {
                        //Construct Absolute Path for Realative Path      
                        lsTextFilePath.AddRange(
                            lsTextFile.Select(path => ConstructAbsolutePath(path, TextHelperFile.FilePath)));
                    }

                    //Replace Image file extension as txt file extension for Content File . Its based on FSD
                    if (lsTextFilePath.Count > 0)
                    {
                        contentFile.AddRange(
                            lsTextFilePath.Select(f => f.Replace(Path.GetExtension(f), Constants.TextFileExtension)));
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage =
                    String.Format("Error getting Content File Path List for Load File record number {0}: {1}",
                    recordNumber, ex.ToDebugString());
                LogMessage(false, errorMessage);
                Tracer.Error(errorMessage);
            }
            return contentFile;
        }

        /// <summary>
        /// Path Substitution
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="txtFilePathSubstitution"></param>
        /// <returns></returns>
        private string PathSubstituion(string filePath, FilePathSubstitutionBEO txtFilePathSubstitution)
        {
            var modifiedPath = (!String.IsNullOrEmpty(Path.GetDirectoryName(filePath)))
                                      ? Path.GetDirectoryName(filePath).Replace(
                                          txtFilePathSubstitution.StringToMatch, txtFilePathSubstitution.StringToReplace)
                                      : String.Empty;
            return (modifiedPath != String.Empty)
                       ? (modifiedPath + @"\" + Path.GetFileName(filePath))
                       : filePath;
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
            if (0 == path.IndexOf(@"\\") || path.Contains(@":\"))
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
        /// Bulk Add Load File image records
        /// </summary>
        /// <param name="jobId">Job Id</param>
        /// <param name="fileData">File Data</param>
        private bool BulkAddLoadFileImageRecords(int jobId, Dictionary<string, List<string>> fileData)
        {
            try
            {
                var sourceData = new DataTable();
                var jobIdColum = new DataColumn
                                 {
                                     DataType = Type.GetType("System.Int32"),
                                     ColumnName = ColumnJobId
                                 };
                sourceData.Columns.Add(jobIdColum);

                var mappingFieldColumn = new DataColumn
                                         {
                                             DataType = Type.GetType("System.String"),
                                             ColumnName = ColumnDocumentKey
                                         };
                sourceData.Columns.Add(mappingFieldColumn);


                var filePathsColumn = new DataColumn
                                      {
                                          DataType = Type.GetType("System.String"),
                                          ColumnName = ColumnImagePaths
                                      };
                sourceData.Columns.Add(filePathsColumn);

                foreach (var data in fileData)
                {
                    var row = sourceData.NewRow();
                    row[ColumnJobId] = jobId;
                    row[ColumnDocumentKey] = data.Key;
                    row[ColumnImagePaths] = string.Join(",", data.Value);
                    sourceData.Rows.Add(row);
                }

                var stopWatch = Stopwatch.StartNew();
                var status = DocumentBO.BulkAddImageRecords(m_Parameters.MatterId, sourceData);
                stopWatch.Stop();
                Tracer.Info("Job {0} : Time taken for add load file image records in database {1} m.s", jobId,
                    stopWatch.ElapsedMilliseconds);

                return status;
            }
            catch (Exception ex)
            {
                var errorMessage = String.Format("Error on bulk copy image files records {0} :",
                    ex.ToDebugString());
                LogMessage(false, errorMessage);
                ex.Trace();
                throw;
            }
        }

        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private ImportBEO GetImportBEO(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof (ImportBEO));

            //Deserialization of bootparameter to get ImportBEO
            return (ImportBEO) xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        internal void LogMessage(bool status, string information)
        {
            var log = new List<JobWorkerLog<LoadFileParserLogInfo>>();
            var parserLog = new JobWorkerLog<LoadFileParserLogInfo>();
            parserLog.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            parserLog.CorrelationId = 0; // TaskId
            parserLog.WorkerRoleType = Constants.LoadFileParserWorkerRoleType;
            parserLog.WorkerInstanceId = WorkerId;
            parserLog.IsMessage = false;
            parserLog.Success = status;
            parserLog.CreatedBy = (!string.IsNullOrEmpty(m_Parameters.CreatedBy) ? m_Parameters.CreatedBy : "N/A");
            parserLog.LogInfo = new LoadFileParserLogInfo();
            parserLog.LogInfo.Information = information;
            if (!status)
                parserLog.LogInfo.Message = "Unhandled Exception:" + information;
            log.Add(parserLog);
            SendLog(log);
        }

        #endregion

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        /// <param name="recordList"></param>
        private void Send(List<LoadFileRecord> recordList)
        {
            // In case of append, assign the dcn for the documents here
            if (m_Parameters.IsAppend)
            {
                // Assign DCN
                AssignDocumentControlNumber(recordList);
            }

            var recordCollection = new LoadFileRecordCollection
            {
                Records = recordList,
                dataset = m_Dataset,
                UniqueThreadString = _uniqueThreadString
            };

            OutputDataPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
            {
                Body = recordCollection
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(recordList.Count);
        }


        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<LoadFileParserLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        #region "Overdrive"

        protected override void BeginWork()
        {
            try
            {
                base.BeginWork();
                m_Parameters = GetImportBEO(BootParameters);
                m_Parameters.ShouldNotBe(null);
                m_LoadFileUri = new Uri(m_Parameters.Locations.First());

                m_ColumnDelimiter = (char) m_Parameters.LoadFile.ColumnDelimiter;
                m_QuoteCharacter = (char) m_Parameters.LoadFile.QuoteCharacter;
                m_NewlineCharacter = (char) m_Parameters.LoadFile.NewlineDelimiter;

                m_RecordTokenizer = new RecordTokenizer(m_ColumnDelimiter, m_QuoteCharacter);
                m_EncodingType = Encoding.GetEncoding(m_Parameters.LoadFile.EncodingType);
                m_IsFirstLineHeader = m_Parameters.LoadFile.IsFirstLineHeader;

                var loadFilePath = HttpUtility.UrlDecode(m_LoadFileUri.OriginalString);
                ReportToDirector("LoadFileParser works on load file {0}", loadFilePath);
                m_StreamReader = new StreamReader(loadFilePath, m_EncodingType);

                #region Dataset Detaills

                m_Parameters.DatasetId.ShouldBeGreaterThan(0);
                m_Dataset = DataSetBO.GetDataSetDetailForDataSetId(m_Parameters.DatasetId);
                var matterDetails = MatterDAO.GetMatterDetails(m_Parameters.MatterId.ToString());
                matterDetails.ShouldNotBe(null);
                m_Dataset.Matter = matterDetails;
                var searchServerDetails = ServerDAO.GetSearchServer(matterDetails.SearchServer.Id);
                searchServerDetails.ShouldNotBe(null);
                m_Dataset.Matter.SearchServer = searchServerDetails;
                m_DatasetPath = m_Dataset.CompressedFileExtractionLocation;

                #endregion

                if (m_Parameters != null &&
                    m_Parameters.IsImportImages &&
                    m_Parameters.LoadFile.ImageFile != null &&
                    m_Parameters.LoadFile.ImageFile.ImageExtractionOption == LoadFileImageExtractionOption.HelperFile)
                {
                    var imageHelperFileName = m_Parameters.LoadFile.ImageFile.HelperFileName;
                    ReportToDirector("LoadFileParser uses image helper file {0}", imageHelperFileName);
                    _imageHelperFileParser =new HelperFileParser(this,imageHelperFileName);
                }

                if (m_Parameters != null &&
                    m_Parameters.LoadFile.ContentFile != null &&
                    m_Parameters.LoadFile.ContentFile.TextExtractionOption == LoadFileTextExtractionOption.HelperFile)
                {
                    var contentHelperFileName = m_Parameters.LoadFile.ContentFile.HelperFileName;
                        ReportToDirector("LoadFileParser uses content (text) helper file {0}", contentHelperFileName);
                        TextHelperFile = new HelperFile(this, contentHelperFileName);
                    }

                if (null != m_Parameters &&
                    null != m_Parameters.LoadFile &&
                    null != m_Parameters.LoadFile.ContentFile &&
                    null != m_Parameters.LoadFile.ContentFile.LoadFileContentField)
                {
                    m_ContentFieldNumber = Convert.ToInt32(m_Parameters.LoadFile.ContentFile.LoadFileContentField);
                }

                _uniqueThreadString = Guid.NewGuid().ToString().Replace("-", "").ToUpper();

                SetMessageBatchSize(m_Parameters);
               
            }
            catch (Exception ex)
            {
                //Send log to Log Pipe
                LogMessage(false, Constants.ParserFailureMessageOnInitialize);
                ex.Trace();
                ReportToDirector("Exception in LoadFileParser.BeginWork", ex.ToDebugString());
                throw;
            }
        }

        private void SetMessageBatchSize(ImportBEO jobParameter)
        {
            try
            {
                if (jobParameter != null)
                {
                    if (jobParameter.IsAppend)
                    {
                        m_BatchSize =
                            Convert.ToInt32(ApplicationConfigurationManager.GetValue("LoadFileBatchSize", "Imports"));
                    }
                    else
                    {
                        m_BatchSize =
                            Convert.ToInt32(ApplicationConfigurationManager.GetValue("LoadFileOverlayBatchSize",
                                                                                     "Imports"));
                    }
                }
            }
            catch (Exception)
            {
                Tracer.Error("Load File Parser: Failed to set message batch size for job run id {0}", PipelineId);
            }
        }

        protected override void EndWork()
        {
            if (m_StreamReader != null)
            {
                m_StreamReader.Close();
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (null != m_StreamReader)
                    {
                        m_StreamReader.Dispose();
                    }
                }
                // shared cleanup logic
                disposed = true;
            }
        }
    }
}
