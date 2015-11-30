#region File Header

//---------------------------------------------------------------------------------------------------
// <copyright file="DoumentSetBEO.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Thaniakiarajan </author>
//      <description>
//        This class represents the Export Startup Worker.
//      </description>
//      <changelog>
//          <date value="03/02/2012">Bug Fix 86335</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="19/02/2013">BugFix#130887</date>
//          <date value="04/23/2013">ADM-Export-003 Recreate Family Group</date>
//          <date value="05/23/2013">BugFix #142173, 142178</date>
//          <date value="06/04/2013">BugFix#144005</date>
//          <date value="06/13/2013">BugFix#144423</date>
//          <date value="07/19/2013">BugFix 146819, 144007, 147272, 146007</date>
//      <date value="07/17/2013">CNEV 2.2.1 - CR005 Implementation : babugx</date>
//          <date value="01/02/2014">Task 159667 - ADM-EXPORT-005</date>
//          <date value="01/29/2014">Task 161755 - ADM-EXPORT-006</date>
//          <date value="02/11/2014">Bug 164054, 164126, 164168, 164211 - Buddy bug fix for ADM EXPORT 004-006</date>
//          <date value="08/24/2015">Task 196112- all documents and tag option to retrieve documents from DB</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#endregion

using System.Globalization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.FolderManagement;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.DataAccess.ServerManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.ServiceImplementation.ReviewerSearch;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Business.Document;

namespace LexisNexis.Evolution.Worker
{
    public class ExportStartupWorker : WorkerBase
    {
        #region Local Variable

        #region LoadFile

        private ExportLoadJobDetailBEO _parametersExportLoadFile;

        #endregion

        #region Common

        private string _createdBy;
        private long _totalDocumentCount;
        private long _endDocumentIndex;
        private string _searchQuery = string.Empty;
        private DatasetBEO _dataset;
        private Int32 _correlationId;
        private ExportOption _exportOption;
        private string _reviewsetId = string.Empty;
        private bool _isIncludeConceptSearch;
        private MockWebOperationContext _webContext;
        private int _pageIndex;
        private DocumentQueryEntity _queryEntity;
        private string _loadFileFullyQualifiedName = string.Empty;
        private string _imageHelperFileName = string.Empty;
        private string _contentHelperFileName = string.Empty;
        private int _batchSize; //value set by configuration
        private int _documentsRetrievalbatchSize; //value set by configuration
        #endregion

        #endregion

        #region OverDrive

        protected override void BeginWork()
        {
            base.BeginWork();

            #region Assertion

            //Pre Condition
            BootParameters.ShouldNotBe(null);
            BootParameters.ShouldBeTypeOf<string>();
            PipelineId.ShouldNotBeEmpty();

            #endregion

            _batchSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("ExportBatchSize", "Export"));
            _documentsRetrievalbatchSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("ExportGetDocumentsBatchSize", "Export")); 
          

            #region Load File

            Initialize(BootParameters);

            #endregion

            #region DCB - Restored When DCB Export is migrated to Overdrive

            /* else if (PipelineType == PipelineType.ExportDcb)
            {
                _parametersExportDCB = GetExportBEO<ExportDCBJobDetailBEO>((string)BootParameters);
                if (_parametersExportDCB != null)
                {
                    //dataset
                    if (!string.IsNullOrEmpty(_parametersExportDCB.DatasetId) && !string.IsNullOrEmpty(_parametersExportDCB.MatterId))
                    {
                        _dataset = GetDatasetDetails(Convert.ToInt64(_parametersExportDCB.DatasetId), _parametersExportDCB.MatterId);
                    }

                    #region ExportOption Option
                    _exportOption = new ExportOption();
                    //Native
                    if (_parametersExportDCB.ExportDCBFileInfo.ExportDCBFileOption.IncludeNativeFiles)
                        _exportOption.IsNative = true;

                    //Production/Image
                    if (_parametersExportDCB.ExportDCBFileInfo.PriImgSelection == SetSelection.ProductionSet)
                    {
                        _exportOption.IsProduction = true;
                        _exportOption.ProductionSetCollectionId = _parametersExportDCB.ExportDCBFileInfo.ProdImgCollectionId;
                    }
                    else if (_parametersExportDCB.ExportDCBFileInfo.PriImgSelection == SetSelection.ImageSet)
                    {
                        _exportOption.IsImage = true;
                        _exportOption.ImageSetCollectionId = _parametersExportDCB.ExportDCBFileInfo.ProdImgCollectionId;
                    }

                    //Fields          
                    if (_parametersExportDCB.ExportDCBFields.ExportDCBFields != null && _parametersExportDCB.ExportDCBFields.ExportDCBFields.Count > 0)
                        _exportOption.IsField = true;

                    //Tag
                    if (_parametersExportDCB.ExportDCBTagInfo != null && _parametersExportDCB.ExportDCBTagInfo.IncludeTag)
                        _exportOption.IsTag = true;

                    //Comments
                    if (_parametersExportDCB.ExportDCBTagInfo != null && (_parametersExportDCB.ExportDCBTagInfo.IncludeTextDocumentComment || _parametersExportDCB.ExportDCBTagInfo.IncludeTextLevelComment))
                        _exportOption.IsComments = true;

                    if (!string.IsNullOrEmpty(_parametersExportDCB.ExportDCBFileInfo.FilePath))
                        _exportOption.ExportDestinationFolderPath = _parametersExportDCB.ExportDCBFileInfo.FilePath;
                    #endregion

                    #region Set User
                    if (!string.IsNullOrEmpty(_parametersExportDCB.CreatedBy))
                        _createdBy = _parametersExportDCB.CreatedBy;
                    else
                    {
                        Tracer.Error("ExportOption Startup Worker: Job created by user id not specified in boot parameters for job run id:{0}", PipelineId);
                        //TODO: throw appropriate exception after analysis.
                    }
                    MockSession();
                    #endregion

                    #region Construct Search Query
                    _searchQuery = GetSearchQueryForExportDCB(out _reviewsetId, out _isIncludeConceptSearch);
                    #endregion
                }
            }*/

            #endregion

            try
            {
                _totalDocumentCount = SetTotalDocumentsCount();
            }
            catch (Exception ex)
            {
                Tracer.Error(
                    "ExportOption Startup Worker: On beginWork failed to set total documents count for job run id:{0}, exception:{1}",
                    PipelineId, ex);
                LogMessage(false, Constants.FailureInSearch);
                throw;
            }
            if (_totalDocumentCount <= 0)
            {
                Tracer.Error("ExportOption Startup Worker: Search return empty records for job run id:{0}", PipelineId);
                LogMessage(false, Constants.ExportSearchNoRecords);
                CleanFileResources();
                throw new EVException().AddUsrMsg(Constants.ExportSearchNoRecords);
            }
        }

        protected override bool GenerateMessage()
        {
            if (_queryEntity == null)
                _queryEntity = ConstructDocumentQuery();

            _queryEntity.DocumentStartIndex = _pageIndex * _batchSize;

            _endDocumentIndex = _queryEntity.DocumentStartIndex + _batchSize - 1;

            if (_totalDocumentCount < _endDocumentIndex)
            {
                _endDocumentIndex = _totalDocumentCount;
            }
            var isGetDocumentsFromSearch = false;
            var documentIdList = new List<string>();
            if (_parametersExportLoadFile.ExportLoadFileInfo != null)
            {
                switch (_parametersExportLoadFile.ExportLoadFileInfo.DocumentSelection)
                {
                    case DocumentSelection.SavedQuery:
                        isGetDocumentsFromSearch = true;
                        documentIdList = GetDocumentsBySearch(_queryEntity);
                        break;
                    case DocumentSelection.Tag:
                        documentIdList = DocumentBO.GetDocumentsForExportJob(_queryEntity.QueryObject.MatterId,
                                       _dataset.CollectionId, _parametersExportLoadFile.ExportLoadFileInfo.TagId,
                                        _totalDocumentCount, _documentsRetrievalbatchSize, "tag");
                        break;
                    default:
                        documentIdList = DocumentBO.GetDocumentsForExportJob(_queryEntity.QueryObject.MatterId,
                                         _dataset.CollectionId, string.Empty, _totalDocumentCount, _documentsRetrievalbatchSize, "all");
                        break;
                }
            }
            #region Assertion

            //Pre condition before send message to next worker
            documentIdList.ShouldNotBe(null);
            documentIdList.LongCount().ShouldBeGreaterThan(0);

            #endregion


            if (isGetDocumentsFromSearch)  //Search use batch retrieval
            {
                Send(documentIdList);
                _pageIndex++;

                if (_pageIndex*_batchSize < _totalDocumentCount)
                {
                    return false;
                }
            }
            else  //All & Tag options retrieved in bulk batch from DB
            {
                Tracer.Info("Documents retrieved from database for export on All/Tag options - document count {0}", documentIdList.Count);
                Send(documentIdList); //Send all documents by _batchsize
            }

            LogMessage(true, "Export Startup Worker successfully completed.");
            return true;
        }

        /// <summary>
        /// Clean Export File resources
        /// </summary>
        private void CleanFileResources()
        {
            if (!string.IsNullOrEmpty(_loadFileFullyQualifiedName)) //Delete DAT File
            {
                DeleteFile(_loadFileFullyQualifiedName);
            }

            if (!string.IsNullOrEmpty(_contentHelperFileName)) //Delete Text File
            {
                DeleteFile(_contentHelperFileName);
            }

            if (!string.IsNullOrEmpty(_imageHelperFileName)) //Delete image File
            {
                DeleteFile(_imageHelperFileName);
            }
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="filePath">file path</param>
        private void DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")
        ]
        private void Initialize(string bootParameter)
        {
            _parametersExportLoadFile = GetExportBEO<ExportLoadJobDetailBEO>(bootParameter);

            #region Assertion

            _parametersExportLoadFile.ShouldNotBe(null);
            _parametersExportLoadFile.ExportLoadFileInfo.FilePath.ShouldNotBeEmpty();

            #endregion

            Directory.CreateDirectory(_parametersExportLoadFile.ExportLoadFileInfo.FilePath);

            if (!Utils.CanWriteToFolder(_parametersExportLoadFile.ExportLoadFileInfo.FilePath))
            {
                Tracer.Error("ExportOption Startup Worker: Invalid export path for job run id:{0}", PipelineId);
                LogMessage(false, Constants.ExportPathInvalid);
                throw new EVException().AddUsrMsg(Constants.ExportPathInvalid);
            }

            if (_parametersExportLoadFile != null &&
                _parametersExportLoadFile.ExportLoadFileInfo != null)
            {
                #region Get Dataset Details

                if (!string.IsNullOrEmpty(_parametersExportLoadFile.DatasetId) &&
                    !string.IsNullOrEmpty(_parametersExportLoadFile.MatterId))
                {
                    _dataset = DataSetBO.GetDatasetDetailsWithMatterInfo(Convert.ToInt64(_parametersExportLoadFile.DatasetId),
                                                 _parametersExportLoadFile.MatterId);

                    #region Assertion

                    _dataset.ShouldNotBe(null);

                    #endregion

                    if (_dataset == null)
                    {
                        Tracer.Error("ExportOption Startup Worker: Cannot get dataset details for job run id: {0}",
                                     PipelineId);
                        // TODO: Throw appropriate exception after analysis.
                    }
                }

                #endregion

                #region Setup ExportOption Options

                _exportOption = new ExportOption
                {
                    IsNative =
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption != null &&
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeNativeFile,
                    IncludeNativeTagName =
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption != null
                            ? _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.TagToIncludeNative
                            : string.Empty,
                    IsText =
                         _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption != null &&
                         _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeTextFile,
                    IsField =
                        _parametersExportLoadFile.ExportLoadFields != null &&
                        _parametersExportLoadFile.ExportLoadFields.Count > 0,
                    IsTag =
                        _parametersExportLoadFile.ExportLoadTagInfo != null &&
                        _parametersExportLoadFile.ExportLoadTagInfo.IncludeTag,
                    ExportDestinationFolderPath =
                        _parametersExportLoadFile.ExportLoadFileInfo.FilePath != null
                            ? _parametersExportLoadFile.ExportLoadFileInfo.FilePath.Replace(@"/", @"\")
                                .TrimEnd(new[] {'\\'})
                            : string.Empty,
                    TextOption1 = _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.TextOption1,
                    TextOption2 = _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.TextOption2,
                    FieldForNativeFileName =
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.FieldForNativeFileName,
                    FieldForTextFileName =
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.Nameselection ==
                        TextFileNameSelection.UseOPT
                        &&
                        !string.IsNullOrEmpty(
                            _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.ImageFileName)
                    ? _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.ImageFileName
                    : _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.FieldForTextFileName,
                    IsTextFieldToExportSelected =
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IsTextFieldToExportSelected,
                    TextFieldToExport =
                        _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.TextFieldToExport
                };
                switch (_parametersExportLoadFile.ExportLoadFileInfo.PriImgSelection)
                {
                    case SetSelection.ProductionSet:
                        _exportOption.IsProduction = true;
                        _exportOption.ProductionSetCollectionId =
                            _parametersExportLoadFile.ExportLoadFileInfo.ProdImgCollectionId;
                        break;
                    case SetSelection.ImageSet:
                        _exportOption.IsImage = true;
                        _exportOption.ImageSetCollectionId =
                            _parametersExportLoadFile.ExportLoadFileInfo.ProdImgCollectionId;
                        break;
                }

                if (_parametersExportLoadFile.ExportLoadTagInfo != null &&
                    _parametersExportLoadFile.ExportLoadTagInfo.IncludeTag)
                {
                    _exportOption.TagList = _parametersExportLoadFile.ExportLoadTagInfo.TagList;
                }

                #endregion

                #region Get delimiters

                var columnDelimiter =
                    Convert.ToChar(
                        Convert.ToInt32(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.Column));
                var quoteCharacter =
                    Convert.ToChar(
                        Convert.ToInt32(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.Quote));

                if (string.IsNullOrEmpty(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.Column) ||
                    string.IsNullOrEmpty(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.Quote) ||
                    string.IsNullOrEmpty(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.NewLine) ||
                    (_exportOption.IsTag && string.IsNullOrEmpty(_parametersExportLoadFile.ExportLoadTagInfo.Delimeter)))
                {
                    Tracer.Info(
                        "ExportOption Startup Worker: One or more delimiters are null or empty for job run id:{0}",
                        PipelineId);
                }

                #endregion

                #region Create files

                _loadFileFullyQualifiedName = _parametersExportLoadFile.ExportLoadFileInfo.FilePath + @"\" +
                                                 _parametersExportLoadFile.ExportLoadFileInfo.FileName + "." +
                                                 _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.
                                                     FileExtension;

                var encoding = _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileFormat.EncodingType ==
                               EncodingTypeSelection.Ansi
                    ? Encoding.GetEncoding(Constants.Ansi)
                    : Encoding.Unicode;

                try
                {
                    Tracer.Info("Export Load File path = {0}", _loadFileFullyQualifiedName);
                    CreateLoadFileWithHeader(_loadFileFullyQualifiedName, columnDelimiter, quoteCharacter, encoding);
                }
                catch (Exception)
                {
                    LogMessage(false, Constants.FailureInCreateLoadFile);
                    throw;
                }

                _exportOption.LoadFilePath = _loadFileFullyQualifiedName;
                if (_parametersExportLoadFile.ExportLoadFileInfo.PriImgSelection == SetSelection.ProductionSet ||
                    _parametersExportLoadFile.ExportLoadFileInfo.PriImgSelection == SetSelection.ImageSet)
                {
                    _imageHelperFileName = _parametersExportLoadFile.ExportLoadFileInfo.FilePath + Constants.BackSlash +
                                           _parametersExportLoadFile.ExportLoadFileInfo.FileName +
                                           Constants.OptFileExtension;
                    _exportOption.LoadFileImageHelperFilePath = _imageHelperFileName;
                }
                if (_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeTextFile &&
                    _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.Nameselection ==
                    TextFileNameSelection.UseOPT)
                {
                    _contentHelperFileName = _parametersExportLoadFile.ExportLoadFileInfo.FilePath +
                                             Constants.TextHelperFileName;
                    _exportOption.LoadFileTextHelperFilePath = _contentHelperFileName;
                }

                #endregion

                #region Assertion

                _parametersExportLoadFile.CreatedBy.ShouldNotBeEmpty();

                #endregion

                #region Set User

                if (!string.IsNullOrEmpty(_parametersExportLoadFile.CreatedBy))
                {
                    _createdBy = _parametersExportLoadFile.CreatedBy;
                }
                else
                {
                    Tracer.Error(
                        "ExportOption Startup Worker: Job created by user id not specified in boot parameters for job run id:{0}",
                        PipelineId);
                    //TODO: throw appropriate exception after analysis.
                }
                MockSession();

                #endregion

                BuildSearchQueryForExportLoadFile();
            }
        }

        protected override void EndWork()
        {
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(List<string> documentIdList)
        {
            var documentList = new List<ExportDocumentDetail>();
            foreach (var doc in documentIdList.Select(documentId => new ExportDocumentDetail {DocumentId = documentId}))
            {
                _correlationId++;
                doc.CorrelationId = (_correlationId).ToString(CultureInfo.InvariantCulture);
                documentList.Add(doc);
                if (documentList.Count == _batchSize)
                {
                    SendData(documentList);
                    documentList.Clear();
                }
            }
            if (documentList.Any())
            {
                SendData(documentList);
            }
        }

        private void SendData(List<ExportDocumentDetail> documentList)
        {
            var documentCollection = new ExportDocumentCollection
                                     {
                                         Documents = documentList,
                                         ExportOption = _exportOption
                                     };
            var message = new PipeMessageEnvelope
                          {
                              Body = documentCollection
                          };
            if (OutputDataPipe != null)
                OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(documentList.Count);
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information)
        {
            var log = new List<JobWorkerLog<ExportStartupLogInfo>>();
            var parserLog = new JobWorkerLog<ExportStartupLogInfo>
            {
                JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = 0,
                WorkerRoleType = Constants.ExportStartupWorkerRoleType,
                WorkerInstanceId = WorkerId,
                IsMessage = false,
                Success = status,
                CreatedBy = _createdBy,
                LogInfo = new ExportStartupLogInfo {Information = information}
            };
            log.Add(parserLog);
            SendLog(log);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ExportStartupLogInfo>> log)
        {
            if (LogPipe != null)
            {
                var message = new PipeMessageEnvelope
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
        }

        #endregion

        #region Load File

        #region Create Load File With Header

        /// <summary>
        /// Create the Load file with heder
        /// </summary>
        /// <param name="filePath">Fully qualified path to Load file.</param>
        /// <param name="columnDelimiter">Column delimiter.</param>
        /// <param name="quoteCharacter">Quote character.</param>
        /// <param name="encoding">Encoding</param>
        private void CreateLoadFileWithHeader(string filePath, char columnDelimiter, char quoteCharacter,
                                              Encoding encoding)
        {
            const string beginEndAttach = "Beg Attach and End attach";
            const string beginAttach = "BegAttach";
            const string endAttach = "EndAttach";
            const string attachRange = "AttachRange";

            var headerRow = new StringBuilder();
            if (_parametersExportLoadFile.ExportLoadFields != null)
            {
                foreach (var fieldSelection in _parametersExportLoadFile.ExportLoadFields)
                {
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(fieldSelection.loadFileField);
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(columnDelimiter);
                }
            }

            #region "Recreate Family Group"

            if (_parametersExportLoadFile.RecreateFamilyGroup)
            {
                if (_parametersExportLoadFile.AttachmentField == beginEndAttach)
                {
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(beginAttach);
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(columnDelimiter);

                    headerRow.Append(quoteCharacter);
                    headerRow.Append(endAttach);
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(columnDelimiter);
                }
                else
                {
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(attachRange);
                    headerRow.Append(quoteCharacter);
                    headerRow.Append(columnDelimiter);
                }
            }

            #endregion

            if (_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeNativeFile)
            {
                headerRow.Append(quoteCharacter);
                headerRow.Append(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.NativeFieldName);
                headerRow.Append(quoteCharacter);
                headerRow.Append(columnDelimiter);
            }
            if (_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.IncludeTextFile &&
                _parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.Nameselection ==
                TextFileNameSelection.UseLoadFIle)
            {
                headerRow.Append(quoteCharacter);
                headerRow.Append(_parametersExportLoadFile.ExportLoadFileInfo.ExportLoadFileOption.TextFileName);
                headerRow.Append(quoteCharacter);
                headerRow.Append(columnDelimiter);
            }
            if (_parametersExportLoadFile.ExportLoadTagInfo.IncludeTag)
            {
                headerRow.Append(quoteCharacter);
                headerRow.Append(_parametersExportLoadFile.ExportLoadTagInfo.TagFieldName);
                headerRow.Append(quoteCharacter);
                headerRow.Append(columnDelimiter);
            }

            headerRow.Remove(headerRow.ToString().Length - 1, 1);
            headerRow.Append(Constants.ConcordanceRecordSplitter);
            //if (!File.Exists(filePath))
            //{
            File.WriteAllText(filePath, headerRow.ToString(), encoding);
            //}
            //else
            //{
            //    throw new Exception(Constants.ExportLoadFileExists).AddDbgMsg("Export file path = {0}", filePath);
            //}
        }

        #endregion

        #region Build Search Query For Load File

        /// <summary>
        /// Construct search query for ExportOption Load File from Load File boot parameter.
        /// </summary>       
        private void BuildSearchQueryForExportLoadFile()
        {
            _searchQuery = string.Empty;
            _reviewsetId = string.Empty;
            _isIncludeConceptSearch = false;
            if (_parametersExportLoadFile.ExportLoadFileInfo != null)
            {
                switch (_parametersExportLoadFile.ExportLoadFileInfo.DocumentSelection)
                {
                    case DocumentSelection.SavedQuery:
                        var savedQueryId = _parametersExportLoadFile.ExportLoadFileInfo.SavedqueryId;
                        var reviewerSearchService = new ReviewerSearchService(_webContext.Object);
                        var savedSearchlist = reviewerSearchService.GetAllSavedSearch("1",
                            int.MaxValue.ToString(CultureInfo.InvariantCulture),
                                                                                        Constants.CreatedDate,
                                                                                        Constants.Ascending);
                        var matchingSavedSearch =
                            savedSearchlist.FirstOrDefault(
                                x => x.SavedSearchId.ToString(CultureInfo.InvariantCulture).Equals(savedQueryId));
                        if (matchingSavedSearch != null)
                        {
                            _searchQuery += matchingSavedSearch.DocumentQuery.QueryObject.DisplayQuery;
                            _reviewsetId = string.Empty;
                            _isIncludeConceptSearch =
                                matchingSavedSearch.DocumentQuery.QueryObject.IsConceptSearchEnabled;
                        }
                        break;
                    case DocumentSelection.Tag:
                        _searchQuery += EVSearchSyntax.Tag + "\"" +
                                        _parametersExportLoadFile.ExportLoadFileInfo.TagId + "\"";
                        break;
                    default:
                        //This condition will not occur in normal conditions.Either tag or saved search will be selected.
                        _searchQuery += string.Empty;
                        break;
                }
            }
        }

        #endregion

        #endregion

        #region Search
        /// <summary>
        /// Get document list by velocity search.
        /// Get document list by Searching in search sub-system
        /// </summary>
        /// <returns></returns>
        private List<string> GetDocumentsBySearch(DocumentQueryEntity queryEntity)
        {
            queryEntity.ShouldNotBe(null);
            var documentId = new List<string>();
         
            queryEntity.TransactionName = "ExportStartupWorker - GetDocumentsBySearch";


            var results = (RvwReviewerSearchResults) SearchBo.Search(queryEntity);

            if (results == null || results.Documents == null || !results.Documents.Any()) return documentId;
                documentId.AddRange(results.Documents.Select(document => document.DocumentId.DocumentId));
            return documentId;
        }

        /// <summary>
        /// Construct Document Query
        /// </summary>
        /// <returns></returns>
        private DocumentQueryEntity ConstructDocumentQuery()
        {
            var queryEntity = new DocumentQueryEntity
            {
                DocumentStartIndex = 0,
                DocumentCount = _batchSize,
                QueryObject = new SearchQueryEntity()
            };
            //Default maximum 100 document batch to retrieve from search sub-system

            #region Output Fields

            //Get only the ImportedDate as output fields rather than fetch all fields for document. This helps to improve performance
            var outputFields = new List<Field> {new Field {FieldName = EVSystemFields.ImportDescription}};
            queryEntity.OutputFields.Clear();
            queryEntity.OutputFields.AddRange(outputFields);

            #endregion

            queryEntity.QueryObject.QueryList.Add(new Query(_searchQuery) {Precedence = 1});
            queryEntity.QueryObject.MatterId = Convert.ToInt32(_dataset.Matter.FolderID);
            queryEntity.QueryObject.DatasetId = Convert.ToInt32(_dataset.FolderID);
            queryEntity.QueryObject.IsConceptSearchEnabled = _isIncludeConceptSearch;
            if (!string.IsNullOrEmpty(_reviewsetId))
            {
                queryEntity.QueryObject.ReviewsetId = _reviewsetId;
            }
            queryEntity.QueryObject.LogSearchHistory = false;

            //Explicitly set - to not to return snippet from search engine..Will be scrapped as part of search engine replacement
            queryEntity.IgnoreDocumentSnippet = true;
            return queryEntity;
        }

        /// <summary>
        /// Get total document count.
        /// </summary> 
        private Int64 SetTotalDocumentsCount()
        {
            var queryObject = new SearchQueryEntity();
            queryObject.QueryList.Add(new Query(_searchQuery));
            queryObject.MatterId = Convert.ToInt32(_dataset.Matter.FolderID);
            queryObject.DatasetId = Convert.ToInt32(_dataset.FolderID);
            if (!string.IsNullOrEmpty(_reviewsetId))
            {
                queryObject.ReviewsetId = _reviewsetId;
            }
            queryObject.IsConceptSearchEnabled = _isIncludeConceptSearch;
            queryObject.LogSearchHistory = false;
            // Getting the All document and Tag document count from DB
            if (_parametersExportLoadFile.ExportLoadFileInfo != null)
            {
                switch (_parametersExportLoadFile.ExportLoadFileInfo.DocumentSelection)
                {
                    case DocumentSelection.SavedQuery:
                        var reviewerSearchService = new RVWReviewerSearchService(_webContext.Object);
                        using (var transScope = new EVTransactionScope(TransactionScopeOption.Suppress))
                        {
                            _totalDocumentCount = reviewerSearchService.GetDocumentCount(queryObject);
                        }
                        break;
                    case DocumentSelection.Tag:
                        _totalDocumentCount = DocumentBO.GetNoOfDocumentsByTag(queryObject.MatterId,
                            _dataset.CollectionId, _parametersExportLoadFile.ExportLoadFileInfo.TagId);
                        break;
                    default:
                        _totalDocumentCount = DocumentBO.GetNoOfDocuments(queryObject.MatterId,
                            _dataset.CollectionId);
                        break;
                }
            }
            return _totalDocumentCount;
        }

        /// <summary>
        /// Mock Session.
        /// </summary>
        private void MockSession()
        {
            _webContext = new MockWebOperationContext();
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            var userProp = UserBO.GetUserByGuid(_createdBy);
            var userSession = new UserSessionBEO();
            SetUserSession(_createdBy, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
        }

        /// <summary>
        /// Sets the user session object using the UserBusinessEntity details.
        /// </summary>
        /// <param name="createdByGuid">Created by User Guid.</param>
        /// <param name="userProp">User Properties.</param>
        /// <param name="userSession">User Session.</param>
        private static void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            if (userProp.Organizations.Any())
                userSession.Organizations.AddRange(userProp.Organizations);
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
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof (T));

            //Deserialization of bootparameter to get ImportBEO
            return (T) xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// Get dataset detail.
        /// </summary>     
        internal static DatasetBEO GetDatasetDetails(long datasetId, string matterId)
        {
            var dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(datasetId));
            var matterDetails = MatterDAO.GetMatterDetails(matterId);
            if (matterDetails == null) return dataset;
            dataset.Matter = matterDetails;
            var searchServerDetails = ServerDAO.GetSearchServer(matterDetails.SearchServer.Id);
            if (searchServerDetails != null)
            {
                dataset.Matter.SearchServer = searchServerDetails;
            }
            return dataset;
        }

        private long GetOrganizationId(long datasetId)
        {
            long id = 0;
            try
            {
                var folder = new FolderDAO();
                id = folder.GetOrganizationIdForFolderId(datasetId);
            }
            catch (Exception ex)
            {
                Tracer.Error(
                    "ExportOption Startup Worker: On beginWork failed to set organization id for job run id:{0}, exception:{1}",
                    PipelineId, ex);
            }
            return id;
        }

        #endregion
    }
}
