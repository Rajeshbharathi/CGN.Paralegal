#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="TagStartupWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Giri</author>
//      <description>
//          This file does the start-up activity for bulk tag job
//      </description>
//      <changelog>
//          <date value="19-Mar-2014">created</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespaces

using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVPolicy;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.ServiceContracts.JobManagement;
using LexisNexis.Evolution.ServiceContracts.Tags;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.ServiceImplementation.JobMgmt;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.IR;

#endregion

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// This class represents the startup worker
    /// </summary>
    public class TagStartupWorker : WorkerBase
    {
        #region Private Variables

        // Boot Object
        private BulkTagJobBusinessEntity _mBootObject;

        // Instance variable for tag details
        private RVWTagBEO _mTagDetails;

        //private DatasetBEO _mDatasetEntity;
        //private ReviewsetDetailsBEO _mReviewSetEntity;
        private long _mTotalDocumentCount;
        private int _mWindowSize; //determine number of documents that will be processed in a message
        private DocumentQueryEntity _mSearchContext;

        private const string WorkerRoletype = "bulktag1-9256-40f3-a5c5-b7258e4f856c";
        internal const int MDefaultWindowSize = 500;
        internal const int JobStatusCancelled = 8;
        internal const string EvolutionJobUpdateStatus = "STA";
        internal const string MBulkTaggingWindowSize = "BulkTaggingWindowSize";
        internal const string DcnField = "DCN";
        internal const string AxlFilePrefix = "mk-";
        internal const string AlphabetD = "D";
        internal const string XmlExtension = ".xml";

        private RVWTagService _mTagService;
        private JobMgmtService _mJobService;

        /// <summary>
        /// Document Vault Manager Properties
        /// </summary>
        private static IDocumentVaultManager _mDocumentVaultMngr;

        public static IDocumentVaultManager DocumentVaultMngr
        {
            get { return _mDocumentVaultMngr ?? (_mDocumentVaultMngr = new DocumentVaultManager()); }
            set { _mDocumentVaultMngr = value; }
        }

        #endregion

        #region Instance variables

        /// <summary>
        /// Read-only property of tag service
        /// </summary>
        public IRVWTagService RvwTagService
        {
            get { return _mTagService ?? (_mTagService = new RVWTagService()); }
        }

        /// <summary>
        /// Read-only property of job service
        /// </summary>
        public IJobMgmtService JobService
        {
            get { return _mJobService ?? (_mJobService = new JobMgmtService()); }
        }

        #endregion

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();

            #region Assert conditions

            BootParameters.ShouldNotBe(null);
            BootParameters.ShouldBeTypeOf<string>();
            PipelineId.ShouldNotBeEmpty();

            #endregion

            try
            {
                if (!Int32.TryParse(ApplicationConfigurationManager.GetValue(MBulkTaggingWindowSize), out _mWindowSize))
                {
                    _mWindowSize = MDefaultWindowSize;
                }
                DoBeginWork(BootParameters);
            }
            catch (Exception ex)
            {
                LogMessage(true, string.Format("Error in TagStartupWorker - Exception: {0}", ex.ToUserString()));
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Absorb the boot parameters, deserialize and pass on the messages to the Search Worker
        /// </summary>
        public void DoBeginWork(string bootParameter)
        {
            bootParameter.ShouldNotBeEmpty();
            // Deserialize and determine the boot object
            _mBootObject = GetBootObject(bootParameter);
            _mBootObject.ShouldNotBe(null);

            _mBootObject.TagDetails.ShouldNotBe(null);
            _mBootObject.TagDetails.Id.ShouldBeGreaterThan(0);
            _mBootObject.TagDetails.MatterId.ShouldBeGreaterThan(0);
            _mBootObject.TagDetails.CollectionId.ShouldNotBeEmpty();

            _mBootObject.DocumentListDetails.ShouldNotBe(null);
            _mBootObject.DocumentListDetails.SearchContext.ShouldNotBe(null);

            _mBootObject.DocumentListDetails.SearchContext.MatterId.ShouldBeGreaterThan(0);
            _mBootObject.JobScheduleCreatedBy.ShouldNotBeEmpty();
            MockSession(_mBootObject.JobScheduleCreatedBy);

            // Fetch tag details
            _mTagDetails = RvwTagService.GetTag(_mBootObject.TagDetails.Id.ToString(CultureInfo.InvariantCulture),
                _mBootObject.TagDetails.CollectionId,
                _mBootObject.TagDetails.MatterId.ToString(
                    CultureInfo.InvariantCulture));

            if (_mTagDetails == null)
            {
                throw new Exception("Given tag does not exists");
            }

            if (!_mTagDetails.Status)
            {
                throw new Exception(string.Format("Tag {0} does not exists in active state", _mTagDetails.TagDisplayName));
            }

            _mSearchContext = GetSearchContext(_mBootObject, true, false);

            var documents = GetDocuments(_mSearchContext);

            if (!documents.Any())
            {
                LogMessage(true,
                    String.Format("Tag Startup Worker : No document(s) qualified to tag {0} for the job run id : {1}",
                        _mTagDetails.Name, PipelineId));
                return;
            }

            //// Retrieve the total documents qualified
            _mTotalDocumentCount = documents.Count;
            var strMsg = String.Format("{0} documents are qualified to tag {1} for the job run id : {2}",
                _mTotalDocumentCount, _mTagDetails.Name, PipelineId);
            LogMessage(false, strMsg);

            // Group the results and send it in batches
            GroupDocumentsAndSend(documents);
        }

        /// <summary>
        /// This method groups the results into batches and send
        /// </summary>
        /// <param name="documents"></param>
        private void GroupDocumentsAndSend(List<BulkDocumentInfoBEO> documents)
        {
            documents = GatherFamiliesAndDuplicates(documents);

            // Construct the record object for the successive worker, with the boot param
            var bulkTagRecord = new BulkTagRecord
            {
                BinderId = _mBootObject.BinderId,
                CollectionId = _mBootObject.TagDetails.CollectionId,
                MatterId = _mBootObject.TagDetails.MatterId,
                DatasetId = _mBootObject.TagDetails.DatasetId,
                ReviewSetId = _mBootObject.DocumentListDetails.SearchContext.ReviewSetId,
                NumberOfOriginalDocuments = _mTotalDocumentCount,
                CreatedByUserGuid = _mBootObject.JobScheduleCreatedBy
            };

            // Fill the tag details in record object
            var tagRecord = new TagRecord
            {
                Id = _mBootObject.TagDetails.Id,
                ParentId = _mBootObject.TagDetails.ParentTagId,
                Name = _mBootObject.TagDetails.Name,
                TagDisplayName = _mBootObject.TagDetails.TagDisplayName,
                IsOperationTagging = _mBootObject.IsOperationTagging,
                IsTagAllDuplicates = _mBootObject.IsTagAllDuplicates,
                IsTagAllFamily = _mBootObject.IsTagAllFamily
            };

            tagRecord.TagBehaviors.AddRange(_mBootObject.TagDetails.TagBehaviors);

            bulkTagRecord.SearchContext = _mSearchContext;
            bulkTagRecord.TagDetails = tagRecord;

            // Determine # of batches for documents to be sent
            var documentsCopy = new List<BulkDocumentInfoBEO>(documents);
            var noOfBatches = (documentsCopy.Count % _mWindowSize == 0)
                ? (documents.Count / _mWindowSize)
                : (documents.Count / _mWindowSize) + 1;
            bulkTagRecord.NumberOfBatches = noOfBatches;

            Tracer.Info(string.Format("Total batch of document(s) determined : {0}", noOfBatches));
            var processedDocCount = 0;
            for (var i = 0; i < noOfBatches; i++)
            {
                // Group documents and send it to next worker
                bulkTagRecord.Documents = documentsCopy.Skip(processedDocCount).Take(_mWindowSize).ToList();
                processedDocCount += _mWindowSize;
                Send(bulkTagRecord);
            }
        }

        private static void MockSession(string userGuid)
        {
            if (EVHttpContext.CurrentContext == null)
            {
                Utility.SetUserSession(userGuid);
            }
        }


        /// <summary>
        /// Get filtered list of documents from the search context
        /// </summary>
        /// <param name="jobParameters">Input parameters of the job</param>
        /// <param name="isTotalRecall"></param>
        /// <param name="justCount"></param>
        /// <returns>List of filtered documents</returns>
        private DocumentQueryEntity GetSearchContext(BulkTagJobBusinessEntity jobParameters, bool isTotalRecall, bool justCount)
        {
            if (!string.IsNullOrEmpty(jobParameters.JobSource) &&
                jobParameters.JobSource.Equals("reprocessfilterdocuments", StringComparison.InvariantCultureIgnoreCase))
            {
                SetSelectedDocumentsForReprocessTagging(jobParameters);
            }

            var documentQueryEntity = new DocumentQueryEntity
            {
                QueryObject = new SearchQueryEntity
                {
                    ReviewsetId = jobParameters.DocumentListDetails.SearchContext.ReviewSetId,
                    DatasetId = jobParameters.DocumentListDetails.SearchContext.DataSetId,
                    MatterId = jobParameters.DocumentListDetails.SearchContext.MatterId,
                    IsConceptSearchEnabled = jobParameters.DocumentListDetails.SearchContext.IsConceptSearchEnabled,
                },
            };

            #region Initialize Bin filters

            if (!string.IsNullOrEmpty(jobParameters.DocumentListDetails.SearchContext.BinningState))
            {
                var binquerys = new List<BinFilter>();
                string[] separator = { "AND" };
                var selectedList = jobParameters.DocumentListDetails.SearchContext.BinningState.Trim()
                    .Split(separator, StringSplitOptions.None);
                foreach (var query in selectedList)
                {
                    var bins = query.Split(':');
                    if (bins.Length > 0)
                    {
                        var binvalue = string.Empty;
                        for (var i = 1; i < bins.Length; i++)
                        {
                            if (binvalue != string.Empty)
                            {
                                binvalue = binvalue + ":";
                            }
                            binvalue = binvalue + bins[i];
                        }

                        binquerys.Add(new BinFilter { BinField = bins[0], BinValue = binvalue });
                        documentQueryEntity.QueryObject.BinFilters.Clear();
                        documentQueryEntity.QueryObject.BinFilters.AddRange(binquerys);
                    }
                }
            }

            #endregion

            documentQueryEntity.DocumentCount = justCount ? 1 : 999999999;

            if (isTotalRecall)
            {
                var outputFields = new List<Field>();
                outputFields.AddRange(new List<Field>
                {
                    new Field {FieldName = DcnField},
                    new Field {FieldName = EVSystemFields.ReviewSetId},
                    new Field {FieldName = EVSystemFields.FamilyId},
                    new Field {FieldName = EVSystemFields.DuplicateId}
                });
                documentQueryEntity.OutputFields.AddRange(outputFields); //Populate fetch duplicates fields

                documentQueryEntity.TotalRecallConfigEntity.IsTotalRecall = true;
            }

            documentQueryEntity.QueryObject.QueryList.Clear();
            documentQueryEntity.QueryObject.QueryList.Add(new Query { SearchQuery = ConstructSearchQuery(jobParameters) });
            documentQueryEntity.SortFields.Add(new Sort { SortBy = Constants.Relevance });
            documentQueryEntity.IgnoreDocumentSnippet = true;
            documentQueryEntity.DocumentStartIndex = 0;
            return documentQueryEntity;
        }

        /// <summary>
        /// Sets the selected documents for reprocess tagging.
        /// </summary>
        /// <param name="jobParameters">The job parameters.</param>
        private void SetSelectedDocumentsForReprocessTagging(BulkTagJobBusinessEntity jobParameters)
        {
            var docs = RVWTagBO.GetDocumentsForReprocessTagging(
                jobParameters.DocumentListDetails.SearchContext.MatterId,
                jobParameters.ReprocessJobId, jobParameters.Filters);

            if (!docs.Any()) return;
            LogMessage(false, string.Format("{0} documents determined for tag reprocessing", docs.Count));
            jobParameters.DocumentListDetails.SelectedDocuments.AddRange(docs);
        }


        /// <summary>
        /// This method fetch the documents from search engine for the given search context
        /// </summary>
        public List<BulkDocumentInfoBEO> GetDocuments(DocumentQueryEntity searchContext)
        {
            var tagDocuments = new List<BulkDocumentInfoBEO>();
            searchContext.TransactionName = "TagStartupWorker - GetDocuments";
            var watch = new Stopwatch();
            watch.Start();
            var searchResults = SearchBo.Search(searchContext, true);
            watch.Stop();
            Tracer.Info("Total time in retrieving document(s) from Search sub-system is {0} milli seconds",
                watch.Elapsed.TotalMilliseconds);

            if (searchResults.Documents.Any())
            {
                searchResults.Documents.ForEach(r => tagDocuments.Add(ConvertToDocumentIdentityRecord(r)));
            }
            return tagDocuments;
        }

        private List<BulkDocumentInfoBEO> GatherFamiliesAndDuplicates(List<BulkDocumentInfoBEO> documents)
        {
            var watch = new Stopwatch();
            watch.Start();
            var originalDocuments = documents;
            //dictionary to hold list of documents to update
            var documentList = originalDocuments.ToDictionary(a => a.DocumentId, a => a);

            if (_mBootObject.IsTagAllDuplicates ||
                _mBootObject.TagDetails.TagBehaviors.Exists(
                    x => string.Compare(x.BehaviorName, Constants.TagAllDuplicatesBehaviorName, true,
                        CultureInfo.InvariantCulture) == 0))
            {
                var distinctDuplicates = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.DuplicateId)).
                    DistinctBy(d => d.DuplicateId).ToList();

                if (distinctDuplicates.Any())
                {
                    var dupBatches = distinctDuplicates.Batch(_mWindowSize);
                    Parallel.ForEach(dupBatches, dupBatch =>
                    {
                        MockSession(_mBootObject.JobScheduleCreatedBy);
                        var dupDocs = GetDuplicateDocumentList(_mBootObject.TagDetails.MatterId.ToString(),
                            _mBootObject.TagDetails.CollectionId, dupBatch.ToList());

                        if (dupDocs != null && dupDocs.Any())
                        {
                            lock (documentList)
                            {
                                foreach (var duplicateDocument in dupDocs)
                                {
                                    if (!documentList.ContainsKey(duplicateDocument.DocumentId))
                                    {
                                        documentList.Add(duplicateDocument.DocumentId, duplicateDocument);
                                    }
                                }
                            }
                        }
                    });
                }
            }

            // Refine family documents based on the tag behavior (families / threads/ both)
            if (PolicyManager.IsAllowFolderPolicy(EVPolicies.CanChangeTagState,
                _mBootObject.TagDetails.DatasetId.ToString(CultureInfo.InvariantCulture)))
            {
                if (_mBootObject.IsTagAllFamily || (
                    _mBootObject.TagDetails.TagBehaviors.Exists(
                        x =>
                            string.Compare(x.BehaviorName, Constants.TagAllFamilyBehaviorName, true,
                                CultureInfo.InvariantCulture) == 0) &&
                    _mBootObject.TagDetails.TagBehaviors.Exists(
                        x =>
                            string.Compare(x.BehaviorName, Constants.TagAllThreadBehaviorName, true,
                                CultureInfo.InvariantCulture) == 0)
                    ))
                {
                    var distinctFamilies = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.FamilyId)).
                        DistinctBy(d => d.FamilyId).ToList();

                    if (distinctFamilies.Any())
                    {
                        var familyBatches = distinctFamilies.Batch(_mWindowSize);
                        Parallel.ForEach(familyBatches, familyBatch =>
                        {
                            MockSession(_mBootObject.JobScheduleCreatedBy);
                            var famDocs = FillEntireFamilies(_mBootObject.TagDetails.MatterId.ToString(),
                                _mBootObject.TagDetails.CollectionId, familyBatch.ToList());

                            if (famDocs != null && famDocs.Any())
                            {
                                lock (documentList)
                                {
                                    foreach (var doc in famDocs)
                                    {
                                        if (!documentList.ContainsKey(doc.DocumentId))
                                        {
                                            documentList.Add(doc.DocumentId, doc);
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
                else
                {                    
                    if (_mBootObject.TagDetails.TagBehaviors.Exists(
                        x =>
                            string.Compare(x.BehaviorName, Constants.TagAllFamilyBehaviorName, true,
                                CultureInfo.InvariantCulture) == 0))
                    {
                        var familyDocuments = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.FamilyId));                       

                        if (familyDocuments.Any())
                        {
                            var familyBatches = familyDocuments.Batch(_mWindowSize);
                            Parallel.ForEach(familyBatches, familyBatch =>
                            {
                                MockSession(_mBootObject.JobScheduleCreatedBy);
                                var famDocs = GetDocumentFamilySubsetBulk(_mBootObject.TagDetails.MatterId.ToString(),
                                    _mBootObject.TagDetails.CollectionId, familyBatch.ToList(), false);

                                if (famDocs != null && famDocs.Any())
                                {
                                    lock (documentList)
                                    {
                                        foreach (var doc in famDocs)
                                        {
                                            if (!documentList.ContainsKey(doc.DocumentId))
                                            {
                                                documentList.Add(doc.DocumentId, doc);
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    }
                    //if tag behavior is tag all threads
                    if (_mBootObject.TagDetails.TagBehaviors.Exists(
                        x =>
                            string.Compare(x.BehaviorName, Constants.TagAllThreadBehaviorName, true,
                                CultureInfo.InvariantCulture) == 0))
                    {
                        var distinctFamilies = originalDocuments.FindAll(x => !String.IsNullOrEmpty(x.FamilyId)).
                        DistinctBy(d => d.FamilyId).ToList();
                        if (distinctFamilies.Any())
                        {
                            var familyBatches = distinctFamilies.Batch(_mWindowSize);
                            Parallel.ForEach(familyBatches, familyBatch =>
                            {
                                MockSession(_mBootObject.JobScheduleCreatedBy);
                                var famDocs = GetDocumentFamilySubsetBulk(_mBootObject.TagDetails.MatterId.ToString(),
                                    _mBootObject.TagDetails.CollectionId, familyBatch.ToList(), true);

                                if (famDocs != null && famDocs.Any())
                                {
                                    lock (documentList)
                                    {
                                        foreach (var doc in famDocs)
                                        {
                                            if (!documentList.ContainsKey(doc.DocumentId))
                                            {
                                                documentList.Add(doc.DocumentId, doc);
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    }
                }
            }
            watch.Stop();
            Tracer.Info("Total time in retrieving family & duplicate document(s) is {0} milli seconds",
                watch.Elapsed.TotalMilliseconds);

            return documentList.Select(d => d.Value).ToList();
        }

        private static List<BulkDocumentInfoBEO> GetDuplicateDocumentList(string matterId, string collectionId,
            List<BulkDocumentInfoBEO> duplicateMaster)
        {
            var duplicateDocuments = DocumentBO.GetDocumentDuplicatesExpressWithFamilyId
                (matterId, collectionId, duplicateMaster);

            var duplicateDocumentsConverted = new List<BulkDocumentInfoBEO>();
            if (duplicateDocuments != null && duplicateDocuments.Any())
            {
                duplicateDocuments.SafeForEach(o => duplicateDocumentsConverted.Add(new BulkDocumentInfoBEO
                {
                    DocumentId = o.DocumentReferenceId,
                    FromOriginalQuery = false,
                    DuplicateId = o.DuplicateId,
                    FamilyId = o.Relationship,
                    DCN = o.DCN
                }));
            }
            return duplicateDocumentsConverted;
        }

        private static List<BulkDocumentInfoBEO> FillEntireFamilies(string matterId,
            string collectionId, List<BulkDocumentInfoBEO> documentList)
        {
            var familyDocuments = DocumentVaultMngr.GetEntireFamilyDocuments(matterId, collectionId,
                documentList);

            var familyDocumentsConverted = new List<BulkDocumentInfoBEO>();

            if (familyDocuments != null && familyDocuments.Count > 0)
            {
                familyDocuments.SafeForEach(o => familyDocumentsConverted.Add(new BulkDocumentInfoBEO
                {
                    DocumentId = o.DocumentReferenceId,
                    FromOriginalQuery = false,
                    DCN = o.DocTitle
                }));
            }
            return familyDocumentsConverted;
        }

        private static List<BulkDocumentInfoBEO> GetDocumentFamilySubsetBulk(string matterId,
            string collectionId, List<BulkDocumentInfoBEO> documentList, bool isTagAllThread)
        {
            var familyDocuments = DocumentVaultMngr.GetDocumentFamilySubsetBulk(matterId, collectionId,
                documentList, !isTagAllThread);
            var familyDocumentsConverted = new List<BulkDocumentInfoBEO>();

            if (familyDocuments != null && familyDocuments.Any())
            {
                familyDocuments.SafeForEach(o => familyDocumentsConverted.Add(new BulkDocumentInfoBEO
                {
                    DocumentId = o.DependentDocumentReferenceId,
                    FromOriginalQuery = false,
                    DCN = o.DependentDCN
                }));
            }
            return familyDocumentsConverted;
        }

        /// <summary>
        /// converts result document to document identity record
        /// </summary>
        /// <param name="resultDocument">ResultDocument</param>
        /// <returns>DocumentIdentityRecord</returns>
        private BulkDocumentInfoBEO ConvertToDocumentIdentityRecord(ResultDocument resultDocument)
        {
            return new BulkDocumentInfoBEO
            {
                DocumentId = resultDocument.DocumentId.DocumentId,
                FamilyId = resultDocument.DocumentId.FamilyId,
                DuplicateId = resultDocument.DocumentId.DuplicateId,
                FromOriginalQuery = true,
                DCN = GetDCN(resultDocument.FieldValues)
            };
        }

        /// <summary>
        /// Get DCN
        /// </summary>

        /// <param name="metaDataList"></param>
        /// <returns>Field value</returns>
        private static string GetDCN(List<DocumentField> metaDataList)
        {
            if (metaDataList == null || !metaDataList.Any())
                return string.Empty;
            var dcnField = metaDataList.Find(
                x => string.Compare(x.FieldName, Constants.DcnField, true, CultureInfo.InvariantCulture) == 0);

            if (dcnField == null)
            {
                return string.Empty;
            }
            return dcnField.Value;
        }


        /// <summary>
        /// Construct the query to retrive selected documents along with duplicates if required
        /// </summary>
        /// <param name="jobParameters">Job parameters</param>
        /// <returns>Search query string</returns>
        private static string ConstructSearchQuery(BulkTagJobBusinessEntity jobParameters)
        {
            var tmpQuery = string.Empty;
            var selectionQuery = string.Empty;
            if (!string.IsNullOrEmpty(jobParameters.DocumentListDetails.SearchContext.Query))
            {
                tmpQuery = jobParameters.DocumentListDetails.SearchContext.Query;
            }
            switch (jobParameters.DocumentListDetails.GenerateDocumentMode)
            {
                case DocumentSelectMode.UseSelectedDocuments:
                    {
                        //Resetting the tmpQuery to empty string since it is not required when selected documents are sent - 
                        //to handle the issue when there are OR operators in the query or the search is done using concept search 
                        //and the search term has relevant synonyms
                        tmpQuery = string.Empty;
                        jobParameters.DocumentListDetails.SelectedDocuments.ForEach(d =>
                            selectionQuery +=
                                string.Format("{0}:\"{1}\" OR ",
                                    EVSystemFields.DocumentKey, d));
                        if (!string.IsNullOrEmpty(selectionQuery))
                        {
                            selectionQuery = selectionQuery.Substring(0,
                                selectionQuery.LastIndexOf(" OR ",
                                    StringComparison.Ordinal));
                            tmpQuery = string.IsNullOrEmpty(tmpQuery)
                                ? selectionQuery
                                : string.Format("({0} AND {1})", tmpQuery, selectionQuery);
                        }

                        break;
                    }
                case DocumentSelectMode.QueryAndExclude:
                    {
                        jobParameters.DocumentListDetails.DocumentsToExclude.ForEach(d =>
                            selectionQuery +=
                                string.Format("(NOT {0}:\"{1}\") AND ",
                                    EVSystemFields.DocumentKey, d));
                        if (!string.IsNullOrEmpty(selectionQuery))
                        {
                            selectionQuery = selectionQuery.Substring(0,
                                selectionQuery.LastIndexOf(" AND ", System.StringComparison.Ordinal));
                            tmpQuery = string.IsNullOrEmpty(tmpQuery)
                                ? selectionQuery
                                : string.Format("({0} AND {1})", tmpQuery, selectionQuery);
                        }
                        break;
                    }
            }
            return tmpQuery;
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        /// <param name="bulkTagRecord"></param>
        private void Send(BulkTagRecord bulkTagRecord)
        {
            var message = new PipeMessageEnvelope
            {
                Body = bulkTagRecord
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(bulkTagRecord.Documents.Count);
        }

        /// <summary>
        /// This method deserializes and determine the Xml BulkTagJobBusinessEntity object
        /// </summary>
        /// <param name="bootParameter"></param>
        private BulkTagJobBusinessEntity GetBootObject(string bootParameter)
        {
            //Creating a stringReader stream for the bootparameter
            using (var stream = new StringReader(bootParameter))
            {
                //Creating xmlStream for xml serialization
                var xmlStream = new XmlSerializer(typeof(BulkTagJobBusinessEntity));
                //De serialization of boot parameter to get BulkTagJobBusinessEntity
                return (BulkTagJobBusinessEntity)xmlStream.Deserialize(stream);
            }
        }

        /// <summary>
        /// Construct the log and send it to log worker
        /// </summary>
        public void LogMessage(bool isError, string msg)
        {
            try
            {
                if (isError)
                {
                    Tracer.Error(msg);
                }
                else
                {
                    Tracer.Info(msg);
                    return;
                }

                var logInfoList = new List<JobWorkerLog<TagLogInfo>>
                {
                    ConstructTagLog(true, false, false, string.Empty, msg, WorkerRoletype)
                };

                LogPipe.ShouldNotBe(null);
                var message = new PipeMessageEnvelope
                {
                    Body = logInfoList
                };
                LogPipe.Send(message);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Construct Near Duplication Log Info for Document
        /// </summary>
        private JobWorkerLog<TagLogInfo> ConstructTagLog(bool isError, bool isErrorInDatabase,
            bool isErrorInSearchEngine,
            string docTitle, string information, string workerRoleType)
        {
            var tagLogInfo = new JobWorkerLog<TagLogInfo>
            {
                JobRunId = !string.IsNullOrEmpty(PipelineId) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = (String.IsNullOrEmpty(docTitle) ? 0 : Convert.ToInt32(docTitle)),
                WorkerInstanceId = WorkerId,
                WorkerRoleType = workerRoleType,
                Success = !isError,
                CreatedBy = _mBootObject.JobScheduleCreatedBy,
                IsMessage = !isError,
                LogInfo = new TagLogInfo
                {
                    DocumentControlNumber = docTitle,
                    IsFailureInDatabaseUpdate = isErrorInDatabase,
                    IsFailureInSearchUpdate = isErrorInSearchEngine,
                    Information = information
                }
            };
            return tagLogInfo;
        }
    }
}