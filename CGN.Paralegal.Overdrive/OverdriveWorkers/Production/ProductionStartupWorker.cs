# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ProductionStartupWorker.cs" company="LexisNexis">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <author>Prabhu</author>
//      <description>
//          This is a file that contains ProductionStartupWorker class 
//      </description>
//      <changelog>
//          <date value="03/02/2012">Bug Fix 86335</date>
//          <date value="07/02/2012">Bug Fix 95652</date>
//          <date value="20/02/2012">97039- BVT issue fix</date>
//          <date value="23/02/2012">97039- BVT issue fix</date>
//          <date value="02/03/2012">Bug fix 95615</date>
//          <date value="03/14/2012">Bug fix 97522</date>
//          <date value="03/15/2012">code warnings fix</date>
//          <date value="03/15/2012">Bug fix 98529</date>
//          <date value="05/21/2012">Fix for error in overdrive log</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/12/2012">Bug Fix # 101652</date>
//          <date value="02/26/2013">Bug Fix # 130801 </date>
//          <date value="05-21-2013">Bug # 142937,143536 and 143037 -ReConvers Buddy Defects</date>
//          <date value="07-03-2013">Bug # 146561 and 145022 - Fix to show  all the documents are listing out in production manage conversion screen</date>
//          <date value="10/07/2013">Dev Bug  # 154336 -ADM -ADMIN - 006 - Import /Production Reprocessing reprocess all documents even with filter and all and other migration fixes
//          <date value="08/06/2013">Binary Externalization Implementation</date>
//          <date value="10/29/2013">Bug  # 155811 - Fix to sort the production set documents in proper DCN order
//          <date value="10/29/2013">Bug  # 155811  - Fix to  sort production set documents in proper dcn order
//          <date value="11/19/2013">Bug  # 157916 - CR05 - Production placeholder change request </date>
//          <date value="02/02/2014">Bug  # 162978 - files not produced properly if the default DCN is changed </date>
//         <date value="03/17/2015">CNEV 3.3 - Bug # 184140 - Fix for duplicates bates number in production</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespace
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.ServiceContracts;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
#endregion

namespace LexisNexis.Evolution.Worker
{
    public class ProductionStartupWorker : WorkerBase
    {
        #region Private Variables
        private ProductionDetailsBEO _mBootParameters;
        private ProductionDocumentDetail _mProductionDocumentDetail;
        private UserBusinessEntity _mUserProp;
        private const string ImageSetTypeId = "3";
        private const string NativeSetTypeId = "2";
        private  int _batchSize = 100;
        private string _mCreatedBy;
        private string _mDatasetId;
        string _batesPrefix = string.Empty;
        string _dpnPrefix = string.Empty;
        int _dpnRunningNumber;
        int _batesRunningNumber;
        private int _taskId;
     

        private string _dcnField;
        private bool _isPathValid;
        readonly StringBuilder _errorBuilder = new StringBuilder();
        private const string ConMessageProductionSetLocation = "Production set location is not exists to produce the document(s).";
        private string _volumeFolderName = string.Empty;
        private int _volumeMaximumDocumentCount;
        private int _volumeDocumentCount;
        private bool _volumeContainExistingDocuments;
        #endregion
        private IDocumentVaultManager _vaultManager;



        #region Protected Methods

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();
            _mBootParameters = GetProductionDetailsBusinessEntity(BootParameters);
            _mBootParameters.ShouldNotBe(null);
            _mBootParameters.Profile.ShouldNotBe(null);
            _mBootParameters.Profile.ProductionSetName.ShouldNotBeEmpty();
            if (!Directory.Exists(_mBootParameters.Profile.ProductionSetLocation))
            {
                Tracer.Warning(Constants.ProductionsetStartError);
                LogMessage(false, ConMessageProductionSetLocation);
                throw new Exception(ConMessageProductionSetLocation);
            }
            _vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
            _mProductionDocumentDetail = ProductionStartupHelper.ConstructProductionModelDocument(_mBootParameters);
            _batesPrefix = _mProductionDocumentDetail.Profile.ProductionPrefix ?? string.Empty;
            _dpnPrefix = _mProductionDocumentDetail.Profile.DpnPrefix ?? string.Empty;
            _mCreatedBy = _mBootParameters.CreatedBy;
            _mDatasetId = _mBootParameters.SearchCriteria.SelectedDatasets[0];
            _isPathValid = true;
            GetVolumeSettings(_mProductionDocumentDetail.ExtractionLocation);

            var dataset = DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(_mDatasetId));
            var field = dataset.DatasetFieldList.FirstOrDefault(f => f.FieldType.DataTypeId == Constants.DCNFieldTypeId);
            if (field != null) _dcnField = field.Name;

            MockSession();
            //CNEV3.1 - Design Specification - Production update - Call Production BO methods for creating the production tags 
            //CNEV3.1 - Design Specification - Production update - Refactor the worker such a way that worker is just a shim and it just calls the respective business methods 
           

            ReadProductionBatchSize();
        }

        /// <summary>
        /// Reads the size of the production batch.
        /// </summary>
        private void ReadProductionBatchSize()
        {
            try
            {
                _batchSize = Convert.ToInt32(ApplicationConfigurationManager.GetValue("Production",
                    "ProductionBatchSize"));
                _batchSize = _batchSize <= 0 ? 100 : _batchSize;
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Problem in reading production batch size");
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Get Production Volume Settings
        /// </summary>
        private void GetVolumeSettings(string productionExtractionPath)
        {
            _volumeMaximumDocumentCount = Convert.ToInt32(_mBootParameters.Profile.VolumeMaximumDocumentCount);
            _volumeFolderName = _mBootParameters.Profile.VolumeFolderName;
            if (_mBootParameters.Profile.IsAppend)
            {
                var lstVolume = Directory.GetDirectories(productionExtractionPath).Select(d => d.Remove(0, productionExtractionPath.Length).Replace("/", "").Replace(@"\", "")).ToList();
                if (lstVolume.Any())
                {
                    _volumeFolderName = lstVolume.OrderByDescending(Convert.ToInt32).FirstOrDefault();
                    if (!string.IsNullOrEmpty(_volumeFolderName))
                    {
                        if (_volumeFolderName != null)
                        {
                            var volumePath = Path.Combine(productionExtractionPath, _volumeFolderName);
                            if (Directory.GetFiles(volumePath).Length > 0)
                            {
                                _volumeDocumentCount = GetVolumeDocumentCount(volumePath);
                                _volumeContainExistingDocuments = true;
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(_volumeFolderName)) return;
            if (_volumeFolderName != null)
            {
                var volume = Path.Combine(productionExtractionPath, _volumeFolderName);
                if (Directory.Exists(volume)) return;
                Directory.CreateDirectory(volume);
            }
        }

        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            try
            {
                //Check share path is valid.
                if (_isPathValid)
                {
                    GetDocuments(0);
                    //Check if there is any invalid path
                    LogMessage(true, Constants.ProductionsetGenerateSuccess);
                }
                else
                {
                    _errorBuilder.Insert(0, Constants.DirectoryMaxLimitError);
                    _errorBuilder.Append(Constants.AllDirectoryMaxLimitError);
                    LogMessage(false, _errorBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                LogMessage(false, Constants.ProductionsetGenerateDocError);
                Tracer.Error(Constants.ProductionsetGenerateError, ex);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Gets the documents.
        /// </summary>
        /// <param name="documentIndex">Index of the document.</param>
        private void GetDocuments(int documentIndex)
        {
            var documents = new List<ProductionDocumentDetail>();
            using (new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                try
                {
                    List<FilteredDocumentBusinessEntity> qualifiedDocuments;
                    List<FilteredDocumentBusinessEntity> excludedDocuments;
                    //Get All selected document details (search the document based on batch size)
                    int selectedDocumentCount = GetQualifiedDocuments(_mProductionDocumentDetail.DocumentSelectionContext, documentIndex, out qualifiedDocuments);
                    selectedDocumentCount.ShouldBeGreaterThan(0);

                    //Get all excluded document details (search all documents)
                    if (GetQualifiedDocuments(_mProductionDocumentDetail.DocumentExclusionContext, 0, out excludedDocuments) == 0) //Get excluded documents
                    {
                        Tracer.Info("Documents are not excluded");
                    }

                    if (excludedDocuments.Any())
                    {
                        //Set IsExclude property as true for the excluded documents exists in the qualified document list
                        qualifiedDocuments.FindAll(x => excludedDocuments.Exists(y => y.DCN == x.DCN)).SafeForEach(f => f.IsExclude = true);
                    }
                    qualifiedDocuments = qualifiedDocuments.DistinctBy(f => f.Id).ToList();

                    //Fill the production job details for selected documents
                    FillSelectedDocuments(qualifiedDocuments.OrderBy(f => f.DCN).ToList(), ref documents);
                }
                catch (Exception ex)
                {
                    Tracer.Error("ProductionStartupWorker: GetDocuments: {0}", ex);
                }
            }
        }

        private void FillSelectedDocuments(IEnumerable<FilteredDocumentBusinessEntity> combinedDocuments, ref List<ProductionDocumentDetail> documents)
        {
            using (new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                int runningDocCount = 0;

                var processSetDocuments = new List<DocumentConversionLogBeo>();
                //Fill production document detail for selected documents
                foreach (FilteredDocumentBusinessEntity document in combinedDocuments)
                {
                    try
                    {
                        if (document == null) continue;

                        if (runningDocCount == _batchSize)
                        {
                            runningDocCount = 0;
                            Send(documents);
                            documents = new List<ProductionDocumentDetail>();
                        }
                        if (!document.IsExclude)
                        {
                            var productionSelectedDocument = new ProductionDocumentDetail
                            {
                                MatterId = _mProductionDocumentDetail.MatterId,
                                CreatedBy = _mProductionDocumentDetail.CreatedBy,
                                DocumentSelectionContext = _mProductionDocumentDetail.DocumentSelectionContext,
                                DatasetCollectionId = _mProductionDocumentDetail.DatasetCollectionId,
                                OriginalCollectionId = _mProductionDocumentDetail.OriginalCollectionId,
                                DocumentExclusionContext = _mProductionDocumentDetail.DocumentExclusionContext,
                                ProductionCollectionId = _mProductionDocumentDetail.ProductionCollectionId,
                                Profile = _mProductionDocumentDetail.Profile,
                                ArchivePath = _mProductionDocumentDetail.ArchivePath
                            };
                            _taskId += 1;
                            productionSelectedDocument.CorrelationId = _taskId; //increment the count for each document
                            productionSelectedDocument.DocumentId = document.Id;
                            string originalDocRefId = document.Id;
                            productionSelectedDocument.OriginalDocumentReferenceId = originalDocRefId;
                            productionSelectedDocument.OriginalDatasetId = _mProductionDocumentDetail.OriginalDatasetId;
                            productionSelectedDocument.OriginalDatasetName =
                                _mProductionDocumentDetail.OriginalDatasetName;
                            productionSelectedDocument.GetText = _mProductionDocumentDetail.GetText;
                            productionSelectedDocument.lstProductionFields =
                                _mProductionDocumentDetail.lstProductionFields;
                            productionSelectedDocument.dataSetBeo = _mProductionDocumentDetail.dataSetBeo;
                            productionSelectedDocument.lstDsFieldsBeo = _mProductionDocumentDetail.lstDsFieldsBeo;
                            productionSelectedDocument.matterBeo = _mProductionDocumentDetail.matterBeo;
                            productionSelectedDocument.SearchServerDetails = _mProductionDocumentDetail.SearchServerDetails;
                            productionSelectedDocument.NearNativeConversionPriority =
                                _mProductionDocumentDetail.NearNativeConversionPriority;

                            productionSelectedDocument.lstProductionFieldValues.AddRange(document.OutPutFields);

                            var numberOfPagesInDoc = GetNumberOfPagesInDocument(productionSelectedDocument, originalDocRefId, document);

                            CreateVolume();
                            productionSelectedDocument.ExtractionLocation =
                                Path.Combine(_mProductionDocumentDetail.ExtractionLocation, _volumeFolderName);
                            productionSelectedDocument.IsVolumeContainExistingDocuments =
                                _volumeContainExistingDocuments;
                            SetBatesConfiguration(numberOfPagesInDoc, productionSelectedDocument);
                            _batesRunningNumber += numberOfPagesInDoc; //For bates number

                            //Add DPN only when xdl exists i.e numberOfPagesInDoc is not zero
                            if (numberOfPagesInDoc > 0)
                            {
                                productionSelectedDocument.DocumentProductionNumber = _dpnPrefix +
                                                                                      (productionSelectedDocument.
                                                                                          Profile.
                                                                                          DpnStartingNumber == null
                                                                                          ? string.Empty
                                                                                          : GetStartingNumber(
                                                                                              _dpnRunningNumber,
                                                                                              productionSelectedDocument
                                                                                                  .
                                                                                                  Profile.
                                                                                                  DpnStartingNumber));
                                _dpnRunningNumber++;
                                productionSelectedDocument.DCNNumber = document.DCN;
                                documents.Add(productionSelectedDocument);
                            }
                            else
                            {
                                processSetDocuments.Add(ConvertToDocumentConversionLogBeo(document,
                                    EVRedactItErrorCodes.Failed,
                                    EVRedactItErrorCodes.XdlFileMissingReasonId)
                                    );
                                //Mark document with no binaries as failed with the reason can not find file
                                LogMessage(productionSelectedDocument, false,
                                    string.Format("Document with DCN:{0} is {1}{2}", document.DCN, Constants.ProductionPreFailure, Constants.EmptyPagesInDoc));
                            }
                        }
                        else
                        {
                            FillExcludedDoucuments(document, ref documents);


                        }

                        runningDocCount = runningDocCount + 1;
                    }
                    catch (Exception ex)
                    {
                        processSetDocuments.Add(ConvertToDocumentConversionLogBeo(document,
                            EVRedactItErrorCodes.Failed,
                            EVRedactItErrorCodes.FailedToSendFile));

                        //If there is a problem in processing one document , continue with other documents
                        ex.Trace().Swallow();
                        LogMessage(false, string.Format("Document with DCN:{0} is {1}-{2}", document.DCN, Constants.ProductionPreFailure, ex.ToUserString()));
                    }

                }

                if (documents.Count > 0)
                {
                    Send(documents);
                }
                BulkInsertProcessSetDocuments(processSetDocuments);
            }
        }

        /// <summary>
        /// Gets the number of pages in document.
        /// </summary>
        /// <param name="productionSelectedDocument">The production selected document.</param>
        /// <param name="originalDocRefId">The original document reference identifier.</param>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        private int GetNumberOfPagesInDocument(ProductionDocumentDetail productionSelectedDocument,
            string originalDocRefId, FilteredDocumentBusinessEntity document)
        {
            var numberOfPagesInDoc = 0;

            var getDocumentSetType = DocumentBO.GetDocumentSetType(productionSelectedDocument.OriginalCollectionId);
            getDocumentSetType.ShouldNotBe(null);
            FieldResult fieldResults;
            switch (getDocumentSetType.DocumentSetTypeId)
            {
                case ImageSetTypeId:
                    fieldResults =
                        document.OutPutFields.FirstOrDefault(f => f.Name == EVSystemFields.PagesImages);
                    if (fieldResults != null && !string.IsNullOrEmpty(fieldResults.Value))
                        numberOfPagesInDoc = Convert.ToInt32(fieldResults.Value);
                    break;
                case NativeSetTypeId:
                    fieldResults =
                        document.OutPutFields.FirstOrDefault(f => f.Name == EVSystemFields.PagesNatives);
                    if (fieldResults != null && !string.IsNullOrEmpty(fieldResults.Value))
                        numberOfPagesInDoc = Convert.ToInt32(fieldResults.Value);
                    break;
                default:
                    numberOfPagesInDoc = CountPagesInDocument(productionSelectedDocument.MatterId,
                        productionSelectedDocument.OriginalCollectionId,
                        originalDocRefId);
                    break;
            }
            return numberOfPagesInDoc;
        }

        /// <summary>
        /// Bulks the insert process set documents.
        /// </summary>
        /// <param name="documentConversionLogBeos">The document conversion log beos.</param>
        private void BulkInsertProcessSetDocuments(IEnumerable<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {

                _mProductionDocumentDetail.MatterId.ShouldNotBe(null);
                var runningDocumentNumber = 0;
                IList<DocumentConversionLogBeo> documentConversionLogBeosBatch = new List<DocumentConversionLogBeo>();
                foreach (var documentConversionLogBeo in documentConversionLogBeos)
                {
                    documentConversionLogBeosBatch.Add(documentConversionLogBeo);
                    runningDocumentNumber++;
                    if (runningDocumentNumber < 100) continue;
                    _vaultManager.AddOrUpdateConversionLogs(Convert.ToInt64(_mProductionDocumentDetail.MatterId),
                                                               documentConversionLogBeosBatch, false);
                    runningDocumentNumber = 0;
                    documentConversionLogBeosBatch = new List<DocumentConversionLogBeo>();
                }
                _vaultManager.AddOrUpdateConversionLogs(Convert.ToInt64(_mProductionDocumentDetail.MatterId),
                                                             documentConversionLogBeosBatch, false);
            }
            catch (Exception exception)
            {
                exception.Trace().Swallow();
            }
        }

        /// <summary>
        /// Converts the specified source collection.
        /// </summary>
        /// <param name="filteredDocument"> </param>
        /// <param name="processStatus"> </param>
        /// <param name="reasonId"> </param>
        /// <returns></returns>
        private DocumentConversionLogBeo ConvertToDocumentConversionLogBeo(FilteredDocumentBusinessEntity filteredDocument, int processStatus = EVRedactItErrorCodes.Submitted, short reasonId = EVRedactItErrorCodes.Na)
        {
            return new DocumentConversionLogBeo
            {
                DocumentId = filteredDocument.Id,
                CollectionId = _mProductionDocumentDetail.OriginalCollectionId,
                JobRunId = WorkAssignment.JobId,
                DCN = filteredDocument.DCN,
                CrossReferenceId = "N/A",
                ProcessJobId = WorkAssignment.JobId,
                Status = (byte)processStatus,
                ReasonId = reasonId,
                ModifiedDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            };
        }



        /// <summary>
        /// Create Volume Folder
        /// </summary>
        private void CreateVolume()
        {
            _volumeDocumentCount = _volumeDocumentCount + 1;
            if (_volumeDocumentCount < (_volumeMaximumDocumentCount + 1)) return;
            var paddingFormat = "{0:D" + _volumeFolderName.Length.ToString(CultureInfo.InvariantCulture) + "}";
            _volumeFolderName = String.Format(paddingFormat, (Convert.ToInt32(_volumeFolderName) + 1));
            var path = Path.Combine(_mProductionDocumentDetail.ExtractionLocation, _volumeFolderName);
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            //For New Volume -Need to reset below values
            _volumeDocumentCount = 1;
            _volumeContainExistingDocuments = false;

        }

        /// <summary>
        /// Logs document count in job log
        /// </summary>
        /// <param name="documentDetail"></param>
        /// <param name="success"></param>
        /// <param name="message"></param>
        private void LogMessage(ProductionDocumentDetail documentDetail, bool success, string message)
        {
            try
            {
                var log = new List<JobWorkerLog<ProductionParserLogInfo>>();
                var parserLog = new JobWorkerLog<ProductionParserLogInfo>
                {
                    JobRunId =
                        (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                    CorrelationId = documentDetail.CorrelationId,
                    WorkerRoleType = "prod0fc6-113e-4217-9863-ec58c3f7sw89",
                    WorkerInstanceId = WorkerId,
                    IsMessage = false,
                    Success = success, //!string.IsNullOrEmpty(documentDetail.DocumentProductionNumber) && success,
                    CreatedBy = documentDetail.CreatedBy,
                    LogInfo =
                        new ProductionParserLogInfo
                        {
                            Information = message, //string.IsNullOrEmpty(documentDetail.DocumentProductionNumber) ? "Unable to produce this document" : message,
                            BatesNumber = documentDetail.AllBates,
                            DatasetName = documentDetail.OriginalDatasetName,
                            DCN = documentDetail.DCNNumber,
                            ProductionDocumentNumber = documentDetail.DocumentProductionNumber,
                            ProductionName = documentDetail.Profile.ProfileName
                        }
                };
                // TaskId
                log.Add(parserLog);
                SendLog(log);
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to log production documents");
                ex.Trace().Swallow();
            }
        }
        private void SetBatesConfiguration(int numberOfPagesInDoc, ProductionDocumentDetail productionSelectedDocument)
        {
            productionSelectedDocument.NumberOfPages = numberOfPagesInDoc;
            productionSelectedDocument.StartBatesRunningNumber = _batesRunningNumber; //For bates number

            //Form a comma seperated list of bates numbers
            productionSelectedDocument.AllBates = GetAllBatesInRange(_batesPrefix, productionSelectedDocument.Profile.ProductionStartingNumber, productionSelectedDocument.StartBatesRunningNumber, numberOfPagesInDoc);

            productionSelectedDocument.StartingBatesNumber = _batesPrefix + (productionSelectedDocument.Profile.ProductionStartingNumber == null ? string.Empty : GetStartingNumber(Convert.ToInt64(productionSelectedDocument.StartBatesRunningNumber), productionSelectedDocument.Profile.ProductionStartingNumber));
            productionSelectedDocument.EndingBatesNumber = _batesPrefix + (productionSelectedDocument.Profile.ProductionStartingNumber == null ? string.Empty : GetStartingNumber(Convert.ToInt64(productionSelectedDocument.StartBatesRunningNumber) + numberOfPagesInDoc - 1, productionSelectedDocument.Profile.ProductionStartingNumber));
        }


        private void FillExcludedDoucuments(FilteredDocumentBusinessEntity document, ref List<ProductionDocumentDetail> documents)
        {
            using (new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                //Fill production document detail for excluded documents
                var productionExcludeDocument = new ProductionDocumentDetail
                {
                    MatterId = _mProductionDocumentDetail.MatterId,
                    CreatedBy = _mProductionDocumentDetail.CreatedBy,
                    DocumentSelectionContext = _mProductionDocumentDetail.DocumentSelectionContext,
                    DatasetCollectionId = _mProductionDocumentDetail.DatasetCollectionId,
                    OriginalCollectionId = _mProductionDocumentDetail.OriginalCollectionId,
                    DocumentExclusionContext = _mProductionDocumentDetail.DocumentExclusionContext,
                    ProductionCollectionId = _mProductionDocumentDetail.ProductionCollectionId,
                    Profile = _mProductionDocumentDetail.Profile
                };
                productionExcludeDocument.Profile.DatasetId = _mProductionDocumentDetail.Profile.DatasetId;
                productionExcludeDocument.ArchivePath = _mProductionDocumentDetail.ArchivePath;
                CreateVolume();
                productionExcludeDocument.ExtractionLocation = Path.Combine(_mProductionDocumentDetail.ExtractionLocation, _volumeFolderName);
                productionExcludeDocument.IsVolumeContainExistingDocuments = _volumeContainExistingDocuments;
                productionExcludeDocument.OriginalDatasetId = _mProductionDocumentDetail.OriginalDatasetId;
                productionExcludeDocument.OriginalDatasetName = _mProductionDocumentDetail.OriginalDatasetName;
                productionExcludeDocument.GetText = _mProductionDocumentDetail.GetText;
                productionExcludeDocument.lstProductionFields = _mProductionDocumentDetail.lstProductionFields;
                productionExcludeDocument.dataSetBeo = _mProductionDocumentDetail.dataSetBeo;
                productionExcludeDocument.lstDsFieldsBeo = _mProductionDocumentDetail.lstDsFieldsBeo;
                productionExcludeDocument.matterBeo = _mProductionDocumentDetail.matterBeo;
                productionExcludeDocument.SearchServerDetails = _mProductionDocumentDetail.SearchServerDetails;
                productionExcludeDocument.NearNativeConversionPriority =
                    _mProductionDocumentDetail.NearNativeConversionPriority;
                productionExcludeDocument.lstProductionFieldValues.AddRange(document.OutPutFields);

                _taskId += 1;
                productionExcludeDocument.CorrelationId = _taskId;
                //If a document is excluded & placeholder page is also not requested then the document is not added as a handler document
                //Also None of the numbers will be generated for the same
                if (!productionExcludeDocument.Profile.IsInsertPlaceHolderPage)
                {
                    return;
                }

                SetBatesConfiguration(1, productionExcludeDocument);
                // We must increment the bates running number so that we get the correct bates field values...
                _batesRunningNumber++;


                productionExcludeDocument.DocumentId = document.Id;

                string originalDocRefId = document.Id;
                productionExcludeDocument.OriginalDocumentReferenceId = originalDocRefId;


                productionExcludeDocument.NumberOfPages = 1;
                productionExcludeDocument.IsDocumentExcluded = true;

                //Add DPN
                productionExcludeDocument.DocumentProductionNumber = _dpnPrefix + (productionExcludeDocument.Profile.DpnStartingNumber == null ? string.Empty : GetStartingNumber(_dpnRunningNumber, productionExcludeDocument.Profile.DpnStartingNumber));

                _dpnRunningNumber++;
                productionExcludeDocument.DCNNumber = document.DCN;
                documents.Add(productionExcludeDocument);
            }
        }

        #endregion


        #region Helper Method
        /// <summary>
        /// To get number of pages in document
        /// </summary>
        /// <param name="matterId"></param>
        /// <param name="collectionId"></param>
        /// <param name="docReferenceId"></param>
        /// <returns>Number of pages</returns>  
        private int CountPagesInDocument(string matterId, string collectionId, string docReferenceId)
        {
            int pageCount = 0;
            //Get all the brava file names for the document in a list (.xdl, .zdl..)
            List<string> fileNames = DocumentBO.GetBinaryReferenceIdFromPropertiesOnly(matterId, collectionId, docReferenceId, "4");

            //Check if xdl files exist. Write to disk if exists
            if (fileNames != null)
            {
                if (fileNames.Count > 0)
                {
                    int xdlCount = fileNames.Distinct().Count(s => s.EndsWith("xdl"));
                    if (xdlCount > 0)
                    {
                        pageCount = fileNames.Distinct().Count(s => s.EndsWith("zdl"));
                    }
                }
            }

            return pageCount;
        }

        /// <summary>
        /// Get All Bates In Range
        /// </summary>
        /// <param name="batesPrefix">Bates prefix</param>
        /// <param name="productionStartingNumber">Production starting number</param>
        /// <param name="startBatesRunningNumber">Start bates running number</param>
        /// <param name="numberOfPagesInDoc">Number of pages in document</param>
        /// <returns></returns>
        private string GetAllBatesInRange(string batesPrefix, string productionStartingNumber, int startBatesRunningNumber, int numberOfPagesInDoc)
        {
            var bates = new StringBuilder();
            for (int i = 0; i < numberOfPagesInDoc; i++)
            {
                bates.Append(batesPrefix + (productionStartingNumber == null ? string.Empty : GetStartingNumber(startBatesRunningNumber + i, productionStartingNumber)));
                bates.Append(",");
            }
            return bates.ToString().TrimEnd(',');
        }

        /// <summary>
        /// Get the starting number.
        /// </summary>
        /// <param name="runningNumber">Running number.</param>
        /// <param name="intialStart">Initial start number</param>
        private string GetStartingNumber(long runningNumber, string intialStart)
        {
            long intialStartNumber;
            string format = "";

            for (int i = 1; i <= intialStart.Length; i++)
                format = format + "0";

            //Check if initial start is numeric - if not return
            if (!long.TryParse(intialStart, out intialStartNumber))
                return string.Empty;

            long newNumber = runningNumber + intialStartNumber;

            //Check if length of the new number is greater than initial start length
            //if (newNumber.ToString().Length > intialStart.Length)
            //    return string.Empty;

            return newNumber.ToString(format);
        }

        /// <summary>
        /// Get filtered list of documents from the search context
        /// </summary>
        /// <param name="documentSelectionContext">The document selection context.</param>
        /// <param name="documentIndex">Index of the document.</param>
        /// <param name="filteredDocuments">The filtered documents.</param>
        /// <returns>
        /// List of filtered documents
        /// </returns>
        private int GetQualifiedDocuments(DocumentOperationBusinessEntity documentSelectionContext, int documentIndex, out List<FilteredDocumentBusinessEntity> filteredDocuments)
        {
            filteredDocuments = new List<FilteredDocumentBusinessEntity>();
            try
            {
                var documentQueryEntity = new DocumentQueryEntity
                {
                    QueryObject = new SearchQueryEntity
                    {
                        ReviewsetId = documentSelectionContext.SearchContext.ReviewSetId,
                        DatasetId = documentSelectionContext.SearchContext.DataSetId,
                        MatterId = documentSelectionContext.SearchContext.MatterId,
                        IsConceptSearchEnabled = documentSelectionContext.SearchContext.IsConceptSearchEnabled
                    },
                    DocumentStartIndex = documentIndex
                };
                documentQueryEntity.IgnoreDocumentSnippet = true;
                documentQueryEntity.QueryObject.QueryList.Add(new Query(documentSelectionContext.SearchContext.Query));
                //documentQueryEntity.SortFields.Add(new Sort { SortBy = "Relevance" });
                filteredDocuments = GetFilteredListOfDocuments(documentQueryEntity, documentSelectionContext);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(EVException))
                {
                    var evEx = (EVException)ex;
                    if (!string.IsNullOrEmpty(evEx.GetErrorCode()))
                    {
                        ex.Data[Constants.ErrorCode] = evEx.GetErrorCode();
                        ex.Data[Constants.Message] = evEx.ToUserString();
                        ex.Trace();
                    }
                    else
                    {
                        Tracer.Error(string.Format("ProductionStartupWorker - {0}", Constants.ErrorInGetQualifiedDocuments));
                    }
                }
                else
                {
                    Tracer.Error(string.Format("ProductionStartupWorker - {0}", Constants.ErrorInGetQualifiedDocuments));
                }
            }
            return filteredDocuments.Count();
        }

        /// <summary>
        /// Filter and get the final list of documents required
        /// </summary>
        /// <param name="documentQueryEntity">The document query entity.</param>
        /// <param name="documentListDetails">Object containing filter parameters</param>
        /// <returns>
        /// List of Filtered documents
        /// </returns>
        private List<FilteredDocumentBusinessEntity> GetFilteredListOfDocuments(DocumentQueryEntity documentQueryEntity, DocumentOperationBusinessEntity documentListDetails)
        {
            var filteredDocuments = new List<FilteredDocumentBusinessEntity>();
            if (documentListDetails != null)
            {
                switch (documentListDetails.GenerateDocumentMode)
                {
                    case DocumentSelectMode.QueryAndExclude:
                        {
                            filteredDocuments = FetchFilteredSearchResultDocuments(documentQueryEntity,
                                documentListDetails.DocumentsToExclude, true);
                            break;
                        }
                    case DocumentSelectMode.UseSelectedDocuments:
                        {
                            filteredDocuments = FetchFilteredSearchResultDocuments(documentQueryEntity,
                                documentListDetails.SelectedDocuments, false);
                            break;
                        }
                }
            }
            return filteredDocuments;
        }


        private static IRvwReviewerSearchService _rvwSearchServiceInstance;
        /// <summary>
        /// Gets the RVW reviewer search service instance.
        /// </summary>
        public static IRvwReviewerSearchService RVWReviewerSearchServiceInstance
        {
            get { return _rvwSearchServiceInstance ?? (_rvwSearchServiceInstance = new RVWReviewerSearchService()); }
            set
            {
                _rvwSearchServiceInstance = value;
            }
        }

        /// <summary>
        /// Gets all documents.
        /// </summary>
        /// <param name="documentQueryEntity">The document query entity.</param>
        /// <returns></returns>
        private ReviewerSearchResults GetAllDocuments(DocumentQueryEntity documentQueryEntity)
        {
            
            using (new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                documentQueryEntity.IgnoreDocumentSnippet = true;
                var outputFields = new List<Field>();
                outputFields.AddRange(new List<Field>
                {
                    new Field {FieldName = _dcnField},
                    new Field{FieldName = EVSystemFields.PagesNatives},
                    new Field{FieldName = EVSystemFields.PagesImages}
                });
                AddProductionFieldsAsOutPutFields(outputFields);
                documentQueryEntity.OutputFields.AddRange(outputFields);//Populate fetch duplicates fields
                documentQueryEntity.DocumentStartIndex = 0;
                documentQueryEntity.DocumentCount = 999999;
                documentQueryEntity.TransactionName = "ProductionStartupWorker - GetAllDocuments";
                documentQueryEntity.TotalRecallConfigEntity.IsTotalRecall = true;
                var rvwReviewerSearchResults = (RvwReviewerSearchResults)SearchBo.Search(documentQueryEntity);
                return ConvertRvwReviewerSearchResultsToReviewerSearchResults(rvwReviewerSearchResults);
            }
        }
        /// <summary>
        /// adds all the production fields to  out put fields
        /// this is to get the existing field value 
        /// existing field value is required in case of updating bates and dpn fields for already produced document
        /// </summary>
        /// <param name="outPutFields"></param>
        private void AddProductionFieldsAsOutPutFields(List<Field> outPutFields)
        {
            if (_mProductionDocumentDetail.lstProductionFields != null && _mProductionDocumentDetail.lstProductionFields.Any())
            {
                _mProductionDocumentDetail.lstProductionFields.SafeForEach(prodField => outPutFields.Add(new Field { FieldName = prodField.Name }));
            }
        }
        /// <summary>
        /// Mock Session : Windows job doesn't 
        /// </summary>
        private void MockSession()
        {
            #region Mock
            new MockWebOperationContext();
            UserBusinessEntity userProp;
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            if (_mUserProp != null)
                userProp = _mUserProp;
            else
            {
                _mUserProp = UserBO.AuthenticateUsingUserGuid(_mCreatedBy);
                userProp = _mUserProp;
            }
            var userSession = new UserSessionBEO();
            SetUserSession(userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
            #endregion
        }


        /// <summary>
        /// Sets the usersession object using the UserBusinessEntity details
        /// </summary>
        /// <param name="userProp">The user property.</param>
        /// <param name="userSession">The user session.</param>
        private static void SetUserSession(UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = userProp.UserGUID;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
        }

        /// <summary>
        ///  Get document count in Volume
        /// </summary>
        private int GetVolumeDocumentCount(string volumePath)
        {
            var docManager = new DocumentVaultManager();
            return docManager.GetVolumeDocumentCount(Convert.ToInt64(_mProductionDocumentDetail.MatterId), _mProductionDocumentDetail.ProductionCollectionId, volumePath);
        }


        /// <summary>
        /// Converts the result document to document result.
        /// </summary>
        /// <param name="resultDocument">The result document.</param>
        /// <returns></returns>
        private static DocumentResult ConvertResultDocumentToDocumentResult(ResultDocument resultDocument)
        {
            var docResult = new DocumentResult
            {
                DocumentID = resultDocument.DocumentId.DocumentId,
                IsLocked = resultDocument.IsLocked,
                RedactableDocumentSetId = resultDocument.RedactableDocumentSetId,
                MatterID = long.Parse(resultDocument.DocumentId.MatterId),
                CollectionID = resultDocument.DocumentId.CollectionId,
                //family id is needed so that bulk tagging/ reviweset creation can be done for family options
                FamilyID = resultDocument.DocumentId.FamilyId
            };

            if (resultDocument.FieldValues != null)
                foreach (FieldResult fieldResult in resultDocument.FieldValues.Select(ConvertDocumentFieldToFieldResult))
                {
                    docResult.Fields.Add(fieldResult);
                    if (fieldResult.DataTypeId == 3000) docResult.DocumentControlNumber = fieldResult.Value;
                }
            return docResult;
        }


        /// <summary>
        /// Converts the document field to field result.
        /// </summary>
        /// <param name="docField">The doc field.</param>
        /// <returns></returns>
        private static FieldResult ConvertDocumentFieldToFieldResult(DocumentField docField)
        {
            var newField = new FieldResult
            {
                Name = docField.FieldName,
                Value = docField.Value,
                ID = Convert.ToInt32(docField.Id),
                DataTypeId = Convert.ToInt32(docField.Type)
            };
            return newField;
        }

        /// <summary>
        /// Converts the RVW reviewer search results to reviewer search results.
        /// </summary>
        /// <param name="rvwReviewerSearchResults">The RVW reviewer search results.</param>
        /// <returns></returns>
        private static ReviewerSearchResults ConvertRvwReviewerSearchResultsToReviewerSearchResults(RvwReviewerSearchResults rvwReviewerSearchResults)
        {
            var queryBEO = new QueryContainerBEO();
            queryBEO.QuerySearchTerms.AddRange(rvwReviewerSearchResults.MatchContextQueries);

            var reviewerSearchResults = new ReviewerSearchResults
            {
                TotalRecordCount = rvwReviewerSearchResults.Documents.Count,
                TotalHitCount = rvwReviewerSearchResults.TotalHitResultCount,
                SearchRequest = new RVWSearchBEO
                {
                    QueryContainerEntity = queryBEO
                }
            };

            foreach (ResultDocument resultDocument in rvwReviewerSearchResults.Documents)
            {
                reviewerSearchResults.ResultDocuments.Add(ConvertResultDocumentToDocumentResult(resultDocument));
            }
            return reviewerSearchResults;
        }

        ///// <summary>
        ///// Fetch filtered list of search results, given the search context
        ///// </summary>
        ///// <param name="searchContext">Search context to get all search results</param>
        ///// <param name="documentIds">List of document Ids to be excluded from the list</param>
        ///// <param name="exclude">
        ///// true if documentIds contain documents to be excluded, false if documentIds contain only the
        ///// documents to be selected from search results and returned
        ///// </param>
        ///// <returns>Filtered list of documents</returns>
        private List<FilteredDocumentBusinessEntity> FetchFilteredSearchResultDocuments(DocumentQueryEntity documentQueryEntity, List<string> documentIds, bool exclude)
        {
            var filteredDocuments = new List<FilteredDocumentBusinessEntity>();
            //Fetch search results - initially fetches only first 10 documents
            ReviewerSearchResults searchResult = GetAllDocuments(documentQueryEntity);
            if (searchResult != null)
            {
                List<DocumentResult> lstDocuments = searchResult.ResultDocuments;
                //Filter search results
                if (lstDocuments != null && lstDocuments.Any())
                {
                    if (exclude)
                    {
                        //Filter documents - Exclude documents in excludedDocuments from search result documents

                        if (documentIds != null && documentIds.Any())
                        {
                            lstDocuments = searchResult.ResultDocuments.Where(doc => !documentIds.Contains(doc.DocumentID)).ToList();
                        }


                    }
                    else
                    {
                        //Filter documents - Select documents in selectedDocumentIds list from search result documents
                        if (documentIds != null && documentIds.Any())
                        {
                            lstDocuments = searchResult.ResultDocuments.Where(doc => documentIds.Contains(doc.DocumentID)).ToList();
                        }

                    }
                    if (lstDocuments.Any())
                    {
                        lstDocuments.SafeForEach(excludedDoc => filteredDocuments.Add(ConstructFilteredDocument(excludedDoc)));
                    }
                }
            }
            return filteredDocuments;
        }

        private static FilteredDocumentBusinessEntity ConstructFilteredDocument(DocumentResult documentResult)
        {
            var filteredDocument = new FilteredDocumentBusinessEntity
            {
                Id = documentResult.DocumentID,
                MatterId = documentResult.MatterID.ToString(CultureInfo.InvariantCulture),
                CollectionId = documentResult.CollectionID,
                IsLocked = documentResult.IsLocked,
                ReviewsetId = documentResult.ReviewsetID,
                DCN = documentResult.DocumentControlNumber,
                FamilyId = documentResult.FamilyID
            };
            filteredDocument.OutPutFields.AddRange(documentResult.Fields);
            return filteredDocument;
        }
        /// <summary>
        /// This method will return production details BEO out of the passed bootparamter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns>production details entity object</returns>
        private static ProductionDetailsBEO GetProductionDetailsBusinessEntity(string bootParamter)
        {
            if (string.IsNullOrEmpty(bootParamter)) return null;
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(ProductionDetailsBEO));

            //Deserialization of bootparameter to get ProductionDetailsBEO
            return (ProductionDetailsBEO)xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        /// <param name="documentCollection"></param>
        private void Send(List<ProductionDocumentDetail> documentCollection)
        {
            var message = new PipeMessageEnvelope
            {
                Body = documentCollection
            };
            if (OutputDataPipe != null)
            {
                OutputDataPipe.Send(message);
            }
            IncreaseProcessedDocumentsCount(documentCollection.Count);
        }




        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information)
        {
            try
            {
                string createdBy = string.Empty;
                if (_mBootParameters != null)
                {
                    createdBy = _mBootParameters.CreatedBy;
                }
                var log = new List<JobWorkerLog<ProductionParserLogInfo>>();
                var parserLog = new JobWorkerLog<ProductionParserLogInfo>
                {
                    JobRunId =
                        (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                    CorrelationId = 0,
                    WorkerRoleType = Infrastructure.Jobs.Constants.ProductionStartupRoleId,
                    WorkerInstanceId = WorkerId,
                    IsMessage = false,
                    Success = status,
                    CreatedBy = createdBy,
                    LogInfo = new ProductionParserLogInfo { Information = information }
                };
                // TaskId
                log.Add(parserLog);
                SendLog(log);
            }
            catch (Exception ex)
            {
                Tracer.Error("ProdcutionStartupWorker" + ex.Message);
            }
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ProductionParserLogInfo>> log)
        {
            try
            {
                LogPipe.Open();
                var message = new PipeMessageEnvelope
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


    }
        #endregion


}
