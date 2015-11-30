# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="ProductionStartupWorker.cs" company="LexisNexis">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <author>P Senthil</author>
//      <description>
//          This is a file that contains IndexingWorker class 
//      </description>
//      <changelog>
//          <date value="05/21/2012">Fix for error in overdrive log</date>
//          <date value="05/30/2012">Fix for Bug 101490</date>
//          <date value="06/2/2012">Fix for Bugs 101490,94121,101319</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="6/5/2012">Fix for bug 100692 & 100624 - babugx</date>
//          <date value="02/28/2014">Included error handling </date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//          <date value="04/03/2014">CNEV 4.0 - Task# 186758 - Search Sub System and IndexBO Integration Changes : babugx</date>
//          <date value="04/03/2014">CNEV 4.0 - Bug# 191443 - custom fields vs Snippet fix : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using LexisNexis.Evolution.Business.Documents.Entities;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Worker
{
    using Infrastructure.Common;
    using System.IO;
    using Infrastructure.EVContainer;

    public class IndexingWorker : SearchEngineWorkerBase
    {
        private MatterBEO _mMatter;



        private IndexManagerProxy _indexManagerProxy;

        /// <summary>
        /// The _matter identifier
        /// </summary>
        private long _matterId;

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();
            BootParameters.ShouldNotBe(null);
            ReadMatterId();
            SetCommiyIndexStatusToInitialized(_matterId);
  
        }

        /// <summary>
        /// Reads the matter identifier.
        /// </summary>
        private void ReadMatterId()
        {
            var edocsNDcbPipeLineTypeNames = new List<string>() {"ImportEdocs", "ImportDcb"};
            var loadFilePipeLineTypeNames = new List<string>()
            {
                "ImportLoadFileAppend",
                "ImportLoadFileOverlay"
            };


            if (!string.IsNullOrEmpty(PipelineType.Moniker))
            {
                if (edocsNDcbPipeLineTypeNames.Contains(PipelineType.Moniker))
                {
                    var importJobParameters = XmlUtility.DeserializeObject(BootParameters, typeof(ProfileBEO)) as ProfileBEO; 
                    importJobParameters.ShouldNotBe(null);
                    _matterId = importJobParameters.DatasetDetails.Matter.FolderID;
                }
                else if (PipelineType.Moniker.Contains("ImportLoadFile") ||
                         loadFilePipeLineTypeNames.Contains(PipelineType.Moniker))
                {
                    var importJobParameters =
                        XmlUtility.DeserializeObject(BootParameters, typeof(ImportBEO)) as ImportBEO; 
                    importJobParameters.ShouldNotBe(null);
                    _matterId = importJobParameters.MatterId;
                }
                else if (PipelineType.Moniker.Equals("ImportLaw"))
                {

                    var importJobParameters = XmlUtility.DeserializeObject(BootParameters, typeof(LawImportBEO)) as LawImportBEO;
                    importJobParameters.ShouldNotBe(null);
                    _matterId = importJobParameters.MatterId;
                }
            }
        }

      
        /// <summary>
        /// Processes the work item.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            var documentCollection = message.Body as DocumentCollection;
            if (documentCollection == null || documentCollection.documents == null)
            {
                Tracer.Warning("Indexing worker receives empty batch");
                return;
            }
            var documentErrorCollection = new DocumentErrorCollection();
            try
            {
                #region Assertion
                documentCollection.ShouldNotBe(null);
                documentCollection.dataset.ShouldNotBe(null);

                #endregion

                if (documentCollection.documents != null && documentCollection.documents.Any())
                //This check done to avoid null reference exception in MatterDAO.GetMatterDetails
                {
                    var isDeleteTags = documentCollection.IsDeleteTagsForOverlay;
                    if (null == _mMatter)
                    {
                        _mMatter = documentCollection.dataset.Matter;
                     
                    }

                    // Initialize the instance of IndexBO
                    _indexManagerProxy = new IndexManagerProxy(_mMatter.FolderID, documentCollection.dataset.CollectionId);

                    using (new EVTransactionScope(TransactionScopeOption.Suppress))
                    {
                        #region Insert

                        var nativeDocumentListInsert =
                            documentCollection.documents.FindAll(
                                n => n.docType == DocumentsetType.NativeSet && n.IsNewDocument);
                        if (nativeDocumentListInsert.Any())
                        {
                            //// Adding imageset documents.
                            
                            AppendImagesetIdentifiers(ref nativeDocumentListInsert, documentCollection);

                            
                           documentErrorCollection= _indexManagerProxy.BulkIndexDocuments(nativeDocumentListInsert.ToDocumentList());
                        }

                        #endregion

                        #region Update

                        #region "Is Not Same Content File

                        var nativeDocumentListUpdate =
                            documentCollection.documents.FindAll(
                                n =>
                                    n.docType == DocumentsetType.NativeSet && !n.IsNewDocument &&
                                    n.OverlayIsNewContentFile).ToList();

                        if (nativeDocumentListUpdate.Any())
                        {
                            //TODO : Verify - delete and recreate is necessary, in case of overlay
                            //TODO: Search Engine Replacement - Search Sub System - Implement batch delete of documents from index 
                            ConstructOverlayDocuments(documentCollection, nativeDocumentListUpdate,
                                documentCollection.IsDeleteTagsForOverlay);

                            //TODO: Search Engine Replacement - Search Sub System - Ensure appropriate annotations happens in search index
                            //TODO: Search Engine Replacement - Search Sub System - Ensure tags are updated for the documents in search index

                            AssignReviewsetIdentifiers(ref nativeDocumentListUpdate);
                            AssignImagesetIdentifiers(ref nativeDocumentListUpdate, documentCollection);
                            AssignTagIdentifiers(ref nativeDocumentListUpdate, isDeleteTags);

                            //TODO: Search Engine Replacement - Search Sub System - Implement to ingest batch of documents into search index
                            _indexManagerProxy.BulkUpdateDocumentsAsync(nativeDocumentListUpdate.ToDocumentList());
                        }

                        #endregion

                        #region Same Content File

                        var nativeDocumentListOverlayInsert =
                            documentCollection.documents.FindAll(
                                n =>
                                    n.docType == DocumentsetType.NativeSet && !n.IsNewDocument &&
                                    !n.OverlayIsNewContentFile).ToList();

                        if (nativeDocumentListOverlayInsert.Any())
                        {
                            var imageDocumentList =
                                documentCollection.documents.FindAll(i => i.docType == DocumentsetType.ImageSet);
                            var imagesetId = string.Empty;
                            if (imageDocumentList.Any())
                            {
                                imagesetId = imageDocumentList.First().document.CollectionId.ToLower();
                            }

                            if (documentCollection.IsIncludeNativeFile || documentCollection.IsDeleteTagsForOverlay ||
                                !string.IsNullOrWhiteSpace(imagesetId))
                            {
                                foreach (var doc in nativeDocumentListOverlayInsert)
                                {
                                    //Initializes the tag and redactable field values
                                    ResetRedactableFields(documentCollection, imagesetId, doc);
                                }
                            }

                            AssignImagesetIdentifiers(ref nativeDocumentListOverlayInsert, documentCollection);
                            AssignTagIdentifiers(ref nativeDocumentListOverlayInsert, isDeleteTags);

                            //bulk documents indexing.
                            _indexManagerProxy.BulkUpdateDocumentsAsync(
                                nativeDocumentListOverlayInsert.ToDocumentList());
                        }

                        #endregion

                        #endregion


                        #region "Update document Index status and contenet size in DB"
                        var includedDocuments =
                       documentCollection.documents.FindAll(
                           n =>
                               n.docType == DocumentsetType.NativeSet).ToList();
                        BulkUpsertDocumentContentSizeAndIndexStatusInfo(_mMatter.FolderID,
                            documentCollection.dataset.CollectionId, includedDocuments);
                        #endregion
                    }
                    Send(documentCollection);
                    SendLog(documentCollection, true, documentErrorCollection);
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Problem in indexing the documents in search sub system");
                ex.Trace().Swallow();
                ReportToDirector(ex);
                SendLog(documentCollection, false);
            }

            IncreaseProcessedDocumentsCount(documentCollection.documents.Count);
            // Debug
            //Tracer.Warning("Indexing worker handled {0} documents", documentCollection.documents.Count);
        }
        /// <summary>
        /// Ends the work.
        /// </summary>
        protected override void EndWork()
        {
            base.EndWork();
            SetCommitIndexStatusToCompleted(_matterId);
        }

      

        private void BulkUpsertDocumentContentSizeAndIndexStatusInfo(long matterId, string datasetCollectionId, IEnumerable<DocumentDetail> documentDetails)
        {
            try
            {
                
                var documentList = new List<RVWDocumentBEO>();
                foreach (var documentDetail in documentDetails)
                {
                    if (documentDetail.document == null) continue;
                    var document = new RVWDocumentBEO
                                   {
                                       DocumentId = documentDetail.document.DocumentId,
                                       DocumentIndexingStatus = true
                                   };
                    var filePath = Extensions.GetTextFilePath(documentDetail.document);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var textFile = new FileInfo(filePath);
                        if (textFile.Exists)
                        {
                            document.FileSize = Convert.ToInt32(textFile.Length);
                        }
                    }
                    documentList.Add(document);
                }
                var vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>("DocumentVaultManager");
                vaultManager.BulkUpsertDocumentContentSizeAndIndexStatusInfo(matterId,
                    datasetCollectionId, documentList);
            }
            catch (Exception ex)
            {
                Tracer.Error("Error occured in Update Index status and Text File size for Job Id {0}",WorkAssignment.JobId);
                ex.Trace().Swallow();
            } 
        }


        /// <summary>
        /// Appends new ImagesetId against all documents
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="record"></param>
        private void AppendImagesetIdentifiers(ref List<DocumentDetail> documents, DocumentCollection record)
        {
            // Determine for ImageSetId to update against all documents
            var imageSetDoc =
                record.documents.FirstOrDefault(i => i.docType == DocumentsetType.ImageSet);
            if (imageSetDoc != null && imageSetDoc.document != null)
            {
                documents.ForEach(d =>
                    d.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        FieldName = EVSystemFields.ImageSets.ToLower(),
                        FieldValue = imageSetDoc.document.CollectionId
                    }));
            }
        }

        /// <summary>
        /// Overlay - Assign the Reviewset Identifiers for the documents
        /// </summary>
        /// <param name="documents"></param>
        private void AssignReviewsetIdentifiers(ref List<DocumentDetail> documents)
        {
            var reviewSetDocs = new List<KeyValuePair<int, List<string>>>();
            reviewSetDocs.AddRange(from nativeDocument in documents
                                   where nativeDocument.Reviewsets != null &&
                                         nativeDocument.Reviewsets.Any()
                                   select
                                       new KeyValuePair<int, List<string>>(nativeDocument.document.Id, nativeDocument.Reviewsets));
            if (!reviewSetDocs.Any())
            {
                reviewSetDocs.AddRange(from doc in documents
                                       where
                                           doc.document.FieldList.Any(
                                               x =>
                                                   String.Equals(x.FieldName, EVSystemFields.ReviewSetId,
                                                       StringComparison.OrdinalIgnoreCase))
                                       select
                                           new KeyValuePair<int, List<string>>(doc.document.Id,
                                               doc.document.FieldList.Where(
                                                   x => String.Equals(x.FieldName,
                                                       EVSystemFields.ReviewSetId, StringComparison.OrdinalIgnoreCase)).
                                                   Select(t => t.FieldValue).ToList()));
            }

            documents.ForEach(d =>
                d.document.FieldList.RemoveAll
                    (f => String.Equals(f.FieldName, EVSystemFields.ReviewSetId, StringComparison.OrdinalIgnoreCase)));

            if (!reviewSetDocs.Any()) return;

            foreach (var doc in reviewSetDocs)
            {
                var docIdentifier = doc.Key;
                var rSetDoc = documents.FirstOrDefault(d => d.document.Id == docIdentifier);
                if (rSetDoc != null)
                {
                    rSetDoc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        FieldName = EVSystemFields.ReviewSetId.ToLower(),
                        FieldValue = String.Join(",", doc.Value)
                    });
                }
            }
        }

        /// <summary>
        /// Overlays new ImagesetId against all documents
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="record"></param>
        private void AssignImagesetIdentifiers(ref List<DocumentDetail> documents, DocumentCollection record)
        {
            // Determine for ImageSetId to update against all documents
            var imageSetDoc =
                record.documents.FirstOrDefault(i => i.docType == DocumentsetType.ImageSet);
            if (imageSetDoc != null)
            {
                documents.ForEach(d =>
                    d.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        FieldName = EVSystemFields.ImageSets.ToLower(),
                        FieldValue = imageSetDoc.document.CollectionId
                    }));
            }
            AssignImagesetIdentifiers(ref documents);
        }

        /// <summary>
        /// Overlay - Assign the Imageset Identifiers for the documents
        /// </summary>
        /// <param name="documents"></param>
        private void AssignImagesetIdentifiers(ref List<DocumentDetail> documents)
        {
            var imageSetDocs = new List<KeyValuePair<int, List<string>>>();
            imageSetDocs.AddRange(from doc in documents
                                  where
                                      doc.document.FieldList.Any(
                                          x => String.Equals(x.FieldName, EVSystemFields.ImageSets, StringComparison.OrdinalIgnoreCase))
                                  select
                                      new KeyValuePair<int, List<string>>(doc.document.Id,
                                          doc.document.FieldList.Where(
                                              x =>
                                                  String.Equals(x.FieldName, EVSystemFields.ImageSets, StringComparison.OrdinalIgnoreCase))
                                              .
                                              Select(t => t.FieldValue).Distinct().ToList()));


            documents.ForEach(d =>
                d.document.FieldList.RemoveAll
                    (f => String.Equals(f.FieldName, EVSystemFields.ImageSets, StringComparison.OrdinalIgnoreCase)));

            if (!imageSetDocs.Any()) return;

            foreach (var doc in imageSetDocs)
            {
                var docIdentifier = doc.Key;
                var rSetDoc = documents.FirstOrDefault(d => d.document.Id == docIdentifier);
                if (rSetDoc != null)
                {
                    rSetDoc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        FieldName = EVSystemFields.ImageSets.ToLower(),
                        FieldValue = String.Join(",", doc.Value)
                    });
                }
            }
        }

        /// <summary>
        /// Overlay - Assign the Tag Identifiers for the documents
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="deleteTag">Specifies whether the existing tags to be replaced with system tags</param>
        private void AssignTagIdentifiers(ref List<DocumentDetail> documents, bool deleteTag)
        {
            var tagDocs = new List<KeyValuePair<string, List<string>>>();

            if (deleteTag)
            {
                var firstDoc = documents.First();

                foreach (var doc in documents)
                {
                    if (firstDoc.SystemTags != null &&
               firstDoc.SystemTags.Any() && doc.SystemTags.Any())
                    {
                        // Determine the system tags to update
                        tagDocs.Add(new KeyValuePair<string, List<string>>(doc.document.DocumentId,
                                                 doc.SystemTags.Select(t => string.Format("evtag{0}", t.Id)).Distinct().ToList()));
                    }
                    else
                    {
                        tagDocs.Add(new KeyValuePair<string, List<string>>(doc.document.DocumentId,
                                                 new List<string> { string.Empty }));
                    }
                }
            }
            else
            {
                // Retain the existing tags
                tagDocs.AddRange(from doc in documents
                                 where
                                     doc.document.FieldList.Any(
                                         x => String.Equals(x.FieldName, EVSystemFields.Tag, StringComparison.OrdinalIgnoreCase))
                                 select
                                     new KeyValuePair<string, List<string>>(doc.document.DocumentId,
                                         doc.document.FieldList.Where(
                                             x => String.Equals(x.FieldName, EVSystemFields.Tag, StringComparison.OrdinalIgnoreCase))
                                             .
                                             Select(t => t.FieldValue).Distinct().ToList()));
            }


            // Remove the tags from the existing fields metadata list, to overlay again with the new list of tags
            documents.ForEach(d =>
                d.document.FieldList.RemoveAll
                    (f => String.Equals(f.FieldName, EVSystemFields.Tag, StringComparison.OrdinalIgnoreCase)));


            if (!tagDocs.Any()) return;
            foreach (var doc in tagDocs)
            {
                var docIdentifier = doc.Key;
                var rSetDoc = documents.FirstOrDefault(d => d.document.DocumentId == docIdentifier);
                if (rSetDoc != null)
                {
                    rSetDoc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        FieldName = EVSystemFields.Tag.ToLower(),
                        FieldValue = String.Join(",", doc.Value)
                    });
                }
            }
        }


        /// <summary>
        /// Method to add removable fields
        /// </summary>
        /// <param name="documentCollection">DocumentCollection</param>
        /// <param name="imagesetId">string</param>
        /// <param name="doc">DocumentDetail</param>
        private void ResetRedactableFields(DocumentCollection documentCollection, string imagesetId, DocumentDetail doc)
        {
            #region Remove Tag

            //Removing the Tag field for overlay updated documents here.        
            if (documentCollection.IsDeleteTagsForOverlay)
            {
                var tagField = new RVWDocumentFieldBEO
                {
                    FieldName = EVSystemFields.Tag.ToLower(),
                    FieldValue =
                        doc.SystemTags != null && doc.SystemTags.Any()
                            ? string.Format(EVSearchSyntax.TagValueFormat + "{0}", doc.SystemTags.First().Id)
                            : string.Empty
                };
                doc.document.FieldList.Add(tagField);
            }

            #endregion

            #region Remove Redactions In nativeSet

            var redactionText = EVSystemFields.RedactionText.ToLower();
            var markup = EVSystemFields.MarkUp.ToLower();

            if (documentCollection.IsIncludeNativeFile)
            {
                if (!doc.document.FieldList.Exists(field => field.FieldName.ToLower().Equals(redactionText)))
                {
                    doc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        IsRequired = true,
                        FieldName = redactionText,
                        FieldValue = string.Empty
                    });
                }
                else
                {
                    doc.document.FieldList.Where(field => field.FieldName.ToLower().Equals(redactionText)).ToList().
                        ForEach(x => x.FieldValue = string.Empty);
                }
                if (!doc.document.FieldList.Exists(field => field.FieldName.ToLower().Equals(markup)))
                {
                    doc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        IsRequired = true,
                        FieldName = markup,
                        FieldValue = string.Empty
                    });
                }
                else
                {
                    doc.document.FieldList.Where(field => field.FieldName.ToLower().Equals(markup)).ToList().
                        ForEach(x => x.FieldValue = string.Empty);
                }
            }

            #endregion

            #region Remove Redactions In ImageSet

            if (!string.IsNullOrWhiteSpace(imagesetId))
            {
                redactionText = string.Format("{0}_{1}", imagesetId.Replace('-', '_'), redactionText);
                markup = string.Format("{0}_{1}", imagesetId.Replace('-', '_'), markup);

                if (!doc.document.FieldList.Exists(field => field.FieldName.ToLower().Equals(redactionText)))
                {
                    doc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        IsRequired = true,
                        FieldName = redactionText,
                        FieldValue = string.Empty
                    });
                }
                else
                {
                    doc.document.FieldList.Where(field => field.FieldName.ToLower().Equals(redactionText))
                        .ToList()
                        .ForEach(x => x.FieldValue = string.Empty);
                }
                if (!doc.document.FieldList.Exists(field => field.FieldName.ToLower().Equals(markup)))
                {
                    doc.document.FieldList.Add(new RVWDocumentFieldBEO
                    {
                        IsRequired = true,
                        FieldName = markup,
                        FieldValue = string.Empty
                    });
                }
                else
                {
                    doc.document.FieldList.Where(field => field.FieldName.ToLower().Equals(markup)).ToList().
                        ForEach(x => x.FieldValue = string.Empty);
                }
            }

            #endregion
        }


        /// <summary>
        /// Constructs the overlay fields
        /// </summary>
        /// <param name="documentCollection">DocumentCollection</param>
        /// <param name="nativeDocumentListUpdate">list of DocumentDetail</param>
        /// <param name="isDeleteTag">true or false</param>
        private static void ConstructOverlayDocuments(DocumentCollection documentCollection,
            List<DocumentDetail> nativeDocumentListUpdate, bool isDeleteTag)
        {
            var imagesetId = string.Empty;
            var imageDocumentList = documentCollection.documents.FindAll(i => i.docType == DocumentsetType.ImageSet);
            if (imageDocumentList.Count > 0)
            {
                imagesetId = imageDocumentList.FirstOrDefault().document.CollectionId.ToLower();
            }
            //b) Set Existing Fields 
            foreach (var docs in nativeDocumentListUpdate)
            {
                foreach (var field in docs.OverlayReImportField)
                {
                    if (!docs.document.FieldList.Exists(f => f.FieldName.ToLower() == field.FieldName.ToLower()))
                    {
                        if (field.FieldName.ToLower() == EVSystemFields.ImageSets.ToLower() &&
                            !string.IsNullOrEmpty(imagesetId))
                        {
                            var imagesetValue = (!string.IsNullOrEmpty(field.FieldValue)
                                ? field.FieldValue
                                : string.Empty);
                            field.FieldValue = (!string.IsNullOrEmpty(imagesetValue)
                                ? (imagesetValue + "," + imagesetId)
                                : imagesetValue);
                        }
                        else if (field.FieldName.ToLower() == EVSystemFields.Tag.ToLower() && isDeleteTag)
                        {
                            field.FieldValue = docs.SystemTags != null && docs.SystemTags.Any()
                                ? string.Format(EVSearchSyntax.TagValueFormat + "{0}", docs.SystemTags.First().Id)
                                : string.Empty;
                        }
                        field.IsRequired = true;
                        docs.document.FieldList.Add(field);
                    }
                }

                var redactionTextMatch = EVSystemFields.RedactionText.ToLower();
                var markupMatch = EVSystemFields.MarkUp.ToLower();

                #region Remove Redactions in NativeSet

                if (documentCollection.IsIncludeNativeFile)
                {
                    docs.document.FieldList.RemoveAll(field => field.FieldName.ToLower().Equals(redactionTextMatch));
                    docs.document.FieldList.RemoveAll(field => field.FieldName.ToLower().Equals(markupMatch));
                }

                #endregion

                #region Remove Redactions in ImageSet

                if (!string.IsNullOrWhiteSpace(imagesetId))
                {
                    redactionTextMatch = string.Format("{0}_{1}", imagesetId.Replace('-', '_'), redactionTextMatch);
                    markupMatch = string.Format("{0}_{1}", imagesetId.Replace('-', '_'), markupMatch);

                    docs.document.FieldList.RemoveAll(field => field.FieldName.ToLower().Equals(redactionTextMatch));
                    docs.document.FieldList.RemoveAll(field => field.FieldName.ToLower().Equals(markupMatch));
                }

                #endregion
            }
        }


        /// <summary>
        /// Sends the log.
        /// </summary>
        /// <param name="documentCollection">The document collection.</param>
        /// <param name="isSentForIndexing">if set to <c>true</c> [is sent for indexing].</param>
        /// <param name="documentErrorCollection"></param>
        private  void SendLog(DocumentCollection documentCollection, bool isSentForIndexing,DocumentErrorCollection documentErrorCollection=null)
        {
            if (documentCollection == null || documentCollection.documents == null) return;
            var message = isSentForIndexing ? "Sent for indexing." : "Failed to send for indexing.";
            var nativeDocumentList =
                documentCollection.documents.FindAll(
                    n => n.docType == DocumentsetType.NativeSet);
            if (!nativeDocumentList.Any()) return;

            var searchIndexLogInfos = new List<JobWorkerLog<SearchIndexLogInfo>>();
            try
            {
                foreach (var documentDetail in nativeDocumentList)
                {
                    if (documentDetail.document == null) continue;
                    var logInfo = new SearchIndexLogInfo
                    {
                        Information =
                            string.Format("DCN:{0}", documentDetail.document.DocumentControlNumber),
                        DocumentId = documentDetail.document.DocumentId,
                        DCNNumber = documentDetail.document.DocumentControlNumber,
                        CrossReferenceField = documentDetail.document.CrossReferenceFieldValue,
                        Message = message
                    };
                    SetDocumentError(documentErrorCollection, documentDetail, logInfo);
                    if (String.IsNullOrEmpty(documentDetail.CorrelationId))
                    {
                        documentDetail.CorrelationId = "0";
                    }
                    var searchIndexLogInfo = new JobWorkerLog<SearchIndexLogInfo>
                    {
                        JobRunId = Convert.ToInt32(PipelineId),
                        CorrelationId = long.Parse(documentDetail.CorrelationId),
                        WorkerInstanceId = WorkerId,
                        WorkerRoleType = "8A65E2DC-753C-E311-82FA-005056850057",
                        Success = isSentForIndexing,
                        LogInfo = logInfo
                    };


                    searchIndexLogInfos.Add(searchIndexLogInfo);
                }
                LogPipe.Open();
                var pipleMessageEnvelope = new PipeMessageEnvelope
                {
                    Body = searchIndexLogInfos
                };
                LogPipe.Send(pipleMessageEnvelope);
            }
            catch (Exception exception)
            {
                exception.AddDbgMsg("Failed to log document details");
                exception.Trace().Swallow();
                ReportToDirector(exception);
            }
        }

        /// <summary>
        /// Sets the document error.
        /// </summary>
        /// <param name="documentErrorCollection">The document error collection.</param>
        /// <param name="documentDetail">The document detail.</param>
        /// <param name="logInfo">The log information.</param>
        private static void SetDocumentError(DocumentErrorCollection documentErrorCollection, DocumentDetail documentDetail,
            SearchIndexLogInfo logInfo)
        {
            if (documentErrorCollection == null||documentErrorCollection.FailedDocumentCount==0) return;
            var documentError = documentErrorCollection.DocumentErrors.FirstOrDefault(
                d => documentDetail.document.DocumentId.Equals(d.Id, StringComparison.CurrentCultureIgnoreCase));
            if (documentError != null)
                logInfo.Message = documentError.ErrorMessage;
        }

      

        /// <summary>
        /// To send the collection to the pipeline director.
        /// </summary>
        /// <param name="collection"></param>
        private void Send(DocumentCollection collection)
        {
            var message = new PipeMessageEnvelope
            {
                Body = collection
            };
            OutputDataPipe.Send(message);
        }
    }

    public static class Extensions
    {
        public static List<RVWDocumentBEO> ToDocumentBEOList(this List<DocumentDetail> documents)
        {
            var documentBeoList = new List<RVWDocumentBEO>();
            RVWDocumentBEO document;
            documents.ForEach(d =>
            {
                document = new RVWDocumentBEO { MatterId = d.document.MatterId };
                document.FieldList.AddRange(d.document.FieldList);
                document.CollectionId = d.document.CollectionId;
                document.NativeFilePath = d.document.NativeFilePath;
                document.DocumentRelationShip = d.document.DocumentRelationShip;
                document.DocumentId = d.document.DocumentId;
                document.Id = d.document.Id;
                document.DocumentBinary = d.document.DocumentBinary;
                document.CustomFieldToPopulateText = d.document.CustomFieldToPopulateText;
                if (d.document.Tags != null && d.document.Tags.Any())
                {
                    d.document.Tags.ForEach(x => document.Tags.Add(x));
                }
                documentBeoList.Add(document);
            });
            return documentBeoList;
        }


        public static List<DocumentBeo> ToDocumentList(this List<DocumentDetail> documents)
        {
            var documentBeoList = new List<DocumentBeo>();

            documents.ForEach(d =>
            {
                if (d.document == null) return;
                var document = new DocumentBeo
                {
                    Id = d.document.DocumentId,
                    Path = d.document.NativeFilePath,
                    Dcn = d.document.DocumentControlNumber,
                };

                if (d.document.FieldList != null && d.document.FieldList.Any())
                {
                    foreach (var field in d.document.FieldList)
                    {
                        if (document.Fields.ContainsKey(field.FieldName))
                        {
                            document.Fields.Remove(field.FieldName);
                        }
                        document.Fields.Add(new KeyValuePair<string, string>(field.FieldName, field.FieldValue));
                    }
                }

                if (document.Fields.ContainsKey(EVSystemFields.MatterId))
                {
                    document.Fields.Remove(EVSystemFields.MatterId);
                }
                if (document.Fields.ContainsKey(EVSystemFields.DatasetId))
                {
                    document.Fields.Remove(EVSystemFields.DatasetId);
                }
                document.Fields.Add(new KeyValuePair<string, string>(EVSystemFields.MatterId, d.document.MatterId.ToString()));
                document.Fields.Add(new KeyValuePair<string, string>(EVSystemFields.DatasetId, d.document.CollectionId));

                if (string.IsNullOrEmpty(d.document.CustomFieldToPopulateText))
                {
                    var txtUrl = GetTextFilePath(d.document);
                    document.Text = (d.document.DocumentBinary != null && !StringUtility.IsNullOrWhiteSpace(d.document.DocumentBinary.Content)) ?
                        d.document.DocumentBinary.Content : (!string.IsNullOrEmpty(txtUrl) ? GetContent(txtUrl) : string.Empty);
                }

                if (d.document.Tags != null && d.document.Tags.Any())
                {
                    var tagStr = new StringBuilder();
                    foreach (var tag in d.document.Tags)
                    {
                        if (!string.IsNullOrEmpty(tagStr.ToString()))
                        {
                            tagStr.Append(",");
                        }
                        tagStr.Append(tag.TagName);
                    }
                    if (document.Fields.ContainsKey(EVSystemFields.Tag))
                    {
                        document.Fields.Remove(EVSystemFields.Tag);
                    }
                    document.Fields.Add(new KeyValuePair<string, string>(EVSystemFields.Tag, tagStr.ToString()));
                }
                documentBeoList.Add(document);
            });
            return documentBeoList;
        }

        /// <summary>
        /// To get the text file path.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static string GetTextFilePath(RVWDocumentBEO document)
        {
            var textFilePath = string.Empty;

            if (document == null || document.DocumentBinary == null || document.DocumentBinary.FileList == null ||
                !document.DocumentBinary.FileList.Any()) return textFilePath;
            // Get text file from external file list
            var txtFile = document.DocumentBinary.FileList.FirstOrDefault
                (x => x.Type.Equals(Constants.TEXT_FILE_TYPE, StringComparison.InvariantCultureIgnoreCase)
                      || x.Type.Equals("1"));

            if (txtFile == null || string.IsNullOrEmpty(txtFile.Path)) return textFilePath;
            textFilePath = txtFile.Path;
            if (textFilePath.Contains('?'))
            {
                textFilePath = textFilePath.Substring(0, (textFilePath.IndexOf('?')));
            }
            return textFilePath;
        }

        /// <summary>
        /// Get Content
        /// </summary>
        /// <param name="filePath">File Path</param>
        /// <returns>Content</returns>
        private static string GetContent(string filePath)
        {
            var content = string.Empty;
            if (File.Exists(filePath))
            {
                content = File.ReadAllText(filePath, Encoding.UTF8);
            }
            return content;
        }
    }
}