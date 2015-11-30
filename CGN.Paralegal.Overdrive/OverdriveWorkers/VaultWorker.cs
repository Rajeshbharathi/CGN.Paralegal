# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="VaultWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil P</author>
//      <description>
//          This is a file that contains Constants class 
//      </description>
//      <changelog>
//          <date value="23-Dec-2011"> bug fix for 92766</date>
//          <date value="16-Feb-2012"> bug fix for 96671</date>
//          <date value="06/2/2012">Fix for Bugs 101490,94121,101319</date>
//          <date value="11/16/2012">BVT fix 113201</date>
//          <date value="08/13/2012">Bug Fix # 149498: [MTS CERT 250K and 1300 K]Documents Mismatch Between Search Count and Dataset Dashboard Count in 250K data import.</date>
//          <date value="03/19/2014">ADM-Admin-008 Overlay issue fix</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

#region All Namespaces

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure.EVContainer;

#endregion All Namespaces

namespace LexisNexis.Evolution.Worker
{
    using System.Diagnostics;
    using Infrastructure;
    using Infrastructure.TransactionManagement;
    using System.Transactions;
    using Business.Binder;

    /// <summary>
    /// Imports list of documents to Vault.
    /// </summary>
    public class VaultWorker : WorkerBase
    {
        private DatasetBEO dataset;

        private IDocumentVaultManager vaultManager;

        private readonly List<string> informationalErrorCodes = new List<string>
        {
                           ErrorCodes.VaultWorker_DocumentHashmapAddFailure, 
                           ErrorCodes.VaultWorker_DocumentHashmapUpdateFailure,
                           ErrorCodes.VaultWorker_DocumentHashmapDeleteforUpdateFailure
                       };

        private readonly Byte[] ignorableSqlErrorStates = {2, 3};

        private readonly List<string> ignoraableErrorCodes = new List<string>
                                                                 {
                                                                     ErrorCodes.VaultWorkerDocumentFieldsInsertFailure,
                                                                     ErrorCodes.VaultWorkerDocumentTextInsertFailure
                                                                 };

        // Debugging
        //private static int totalCount;

        #region Job Framework functions

        /// <summary>
        /// Processes the work item.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            var documentCollection = message.Body as DocumentCollection;

            if (documentCollection == null)
            {
                Tracer.Warning("VaultWorker.ProcessMessage: documentCollection == null");
                return;
            }

            if (documentCollection.dataset == null)
            {
                Tracer.Warning("VaultWorker.ProcessMessage: documentCollection.dataset == null");
                return;
            }

            if (documentCollection.documents == null)
            {
                Tracer.Warning("VaultWorker.ProcessMessage: documentCollection.documents == null");
                return;
            }

            if (documentCollection.dataset.FolderID == 0)
            {
                Tracer.Warning("VaultWorker.ProcessMessage: documentCollection.dataset.FolderID == 0");
                return;
            }

            dataset = documentCollection.dataset;

            // Debugging
            //int initialCount = documentCollection.documents.Count;
            //totalCount += initialCount;
            //Tracer.Debug("Vault received {0}, total is {1}", initialCount, totalCount);

            try
            {
                try
                {
                    #region Import Native Set

                    // Select all native documents being inserted for the first time.
                    var nativeNewDocumentDetails =
                        documentCollection.documents.Where(
                            p => p.docType == DocumentsetType.NativeSet && p.IsNewDocument);

                    // insert new native documents.
                    InsertNewDocuments(nativeNewDocumentDetails, DocumentsetType.NativeSet);

                    LogVaultMessages(nativeNewDocumentDetails, true, false);

                    // Select all existing documents came for update into Vault.
                    var nativeExistingDocumentDetails =
                        documentCollection.documents.Where(
                            p => p.docType == DocumentsetType.NativeSet && (!p.IsNewDocument));

                    UpdateExistingDocuments(nativeExistingDocumentDetails, DocumentsetType.NativeSet,
                                            documentCollection.IsDeleteTagsForOverlay);

                    LogVaultMessages(nativeExistingDocumentDetails, true, false);

                    #endregion Import Native Set
                }
                catch (SqlException sqlException)
                {
                    //This is to make sure documents send to search server even though there is probleming in processing text/fields
                    //ToDo: Dev Bug # 149941 -Look for better way to handle duplicate text /fields issue from Law 1.3 million documents Or why duplicate text Or fields are                    received here
                    if (!ignorableSqlErrorStates.Contains(sqlException.State) ||
                        !ignoraableErrorCodes.Contains(sqlException.Message))
                        throw;

                    sqlException.AddUsrMsg(
                        String.Format("Failed to add a document {0} to valut.Please see application log for details.",
                                      sqlException.State == 2 ? "Text" : "Fields"));
                    LogVaultMessages(documentCollection.documents.Where(p => p.docType == DocumentsetType.NativeSet),
                                     false,
                                     false, sqlException.ToUserString());
                    sqlException.Trace().Swallow();
                    ReportToDirector(sqlException);
                }
                catch (EVException ex)
                {
                    var exceptionIsIgnorable = informationalErrorCodes.Contains(ex.GetErrorCode());
                    if (!exceptionIsIgnorable)
                    {
                        throw;
                    }
                    ex.Trace().Swallow();
                    ReportToDirector(ex);
                }

                try
                {
                    #region Import Image Set

                    // select new image documents to be inserted into Vault system for the first time.
                    var imagesetNewDocumentDetails =
                        documentCollection.documents.Where(p => p.docType == DocumentsetType.ImageSet && p.IsNewDocument);

                    InsertNewDocuments(imagesetNewDocumentDetails, DocumentsetType.ImageSet);

                    // select existing image set detail to be updated in Vault system.                  
                    var imagesetExistingDocumentDetails =
                        documentCollection.documents.Where(
                            p => p.docType == DocumentsetType.ImageSet && (!p.IsNewDocument));

                    // False for delete tags because there are no tags for image sets. 
                    // Hard coding false will avoid an additional DB call in Document Vault Manager.                    
                    UpdateExistingDocuments(imagesetExistingDocumentDetails, DocumentsetType.ImageSet, false);

                    #endregion Import Image Set
                }
                catch (EVException ex)
                {
                    var exceptionIsIgnorable = informationalErrorCodes.Contains(ex.GetErrorCode());
                    if (!exceptionIsIgnorable)
                    {
                        throw;
                    }
                    ex.Trace().Swallow();
                }
                finally
                {
                    Send(documentCollection);
                    IncreaseProcessedDocumentsCount(documentCollection.documents.Count);
                }

                // Debugging
                //Tracer.Debug("Vault send {0}, debug total {1}, reported total {2}", documentCollection.documents.Count, totalCount, WorkerStatistics.ProcessedDocuments);
            }

            catch (Exception ex)
            {
                ex.AddUsrMsg("Failed to add a document to Vault. Please see application log for details.");
                LogVaultMessages(documentCollection.documents.Where(p => p.docType == DocumentsetType.NativeSet), false,
                                 false, ex.ToUserString());
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Begins the worker process.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();
            vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
            Debug.Assert(vaultManager != null);
        }

        /// <summary>
        /// Appends/inserts a document into Vault system.
        /// </summary>
        /// <param name="documentDetailObjects"> Document details object that contains document business entities</param>
        /// <param name="documentSetType"> Nativeset Vs Imageset </param>
        private void InsertNewDocuments(IEnumerable<DocumentDetail> documentDetailObjects,
            DocumentsetType documentSetType)
        {
            Debug.Assert(documentDetailObjects != null);
            if (!documentDetailObjects.Any())
            {
                return;
            }

            var documents = documentDetailObjects.Select(p => p.document);
            var documentBeos = documents as RVWDocumentBEO[] ?? documents.ToArray();
            var respDocs = vaultManager.BulkAddOrUpdateDocuments(documentBeos, dataset, documentSetType);

            var rvwDocumentBeos = respDocs as RVWDocumentBEO[] ?? respDocs.ToArray();
            if (respDocs != null && rvwDocumentBeos.Any())
            {
                foreach (var doc in rvwDocumentBeos)
                {
                    // Retrieve and assign numeric document identifier
                    var matchDoc = documentBeos.FirstOrDefault(d => d.DocumentId == doc.DocumentId);
                    if (matchDoc != null)
                    {
                        matchDoc.Id = doc.Id;
                    }
                }
            }

            if (documentSetType != DocumentsetType.NativeSet)
            {
                return;
            }

            #region Document Hash

            try
            {
                var firstDocument = documentBeos.First();

                if (!vaultManager.BulkAddDocumentHashMap(documentBeos, firstDocument.MatterId))
                {
                    throw new EVException().AddResMsg(ErrorCodes.VaultWorker_DocumentHashmapAddFailure);
                }
            }
            catch (Exception exception)
            {
                exception.Trace().Swallow();
                LogVaultMessages(documentDetailObjects, false, false,
                                 "Failed to add  hash map to the document,Please see application log for details.");
            }

            #endregion Document Hash
        }

        /// <summary>
        /// Updates/Overlays existing documents in the Vault system.
        /// </summary>
        /// <param name="documentDetailObjects"> Document Details object that contains document business entities.</param>
        /// <param name="documentSetType"> Nativeset Vs Imageset </param>
        /// <param name="isDeleteTags"> if yes, document tags will be deleted from Vault system. </param>
        private void UpdateExistingDocuments(IEnumerable<DocumentDetail> documentDetailObjects,
            DocumentsetType documentSetType, bool isDeleteTags)
        {
            Debug.Assert(documentDetailObjects != null);
            if (!documentDetailObjects.Any())
            {
                return;
            }

            var documents = documentDetailObjects.Select(p => p.document);

            vaultManager.BulkAddOrUpdateDocuments(documents, dataset, documentSetType,
                isDeleteTags: isDeleteTags,
                isOverlay: true); 

            if (documentSetType != DocumentsetType.NativeSet)
            {
                return;
            }

            if (isDeleteTags && documentDetailObjects.Any(doc => doc.SystemTags != null && doc.SystemTags.Any()))
            {
                UpdateNotReviewedTagForDocuments(documentDetailObjects);
            }

            #region Document Hash

            //Delete Hash Value
            if (!vaultManager.BulkDeleteDocumentHashMap(documents, documents.First().MatterId))
            {
                throw new EVException().AddResMsg(ErrorCodes.VaultWorker_DocumentHashmapDeleteforUpdateFailure);
            }

            //Insert Hash Value
            if (!vaultManager.BulkAddDocumentHashMap(documents, documents.First().MatterId))
            {
                throw new EVException().AddResMsg(ErrorCodes.VaultWorker_DocumentHashmapUpdateFailure);
            }

            #endregion Document Hash
        }

        /// <summary>
        /// Update Not-Reviewed Tag for documents
        /// </summary>
        /// <param name="documentDetailObjects"></param>
        private void UpdateNotReviewedTagForDocuments(IEnumerable<DocumentDetail> documentDetailObjects)
        {
            try
            {
                //Updates the documents with 'Not Reviewed' Tag
                var jobParameter = (ImportBEO) XmlUtility.DeserializeObject(BootParameters, typeof (ImportBEO));
                var tags = new Dictionary<int, List<BulkDocumentInfoBEO>>();
                var tagKeys = new Dictionary<int, string>();
                foreach (var doc in documentDetailObjects)
                {
                    var bulkDocument = new BulkDocumentInfoBEO
                    {
                        DocumentId = doc.document.DocumentId,
                        DuplicateId = doc.document.DuplicateId,
                        FromOriginalQuery = true,
                        CreatedBy = doc.document.CreatedBy,
                        FamilyId = doc.document.FamilyId
                    };

                    foreach (var tag in doc.SystemTags)
                    {
                        if (tags.ContainsKey(tag.Id))
                        {
                            var bulkDocumentInfoBeOs = tags.FirstOrDefault(t => t.Key == tag.Id).Value;
                            bulkDocumentInfoBeOs.Add(bulkDocument);
                        }
                        else
                        {
                            var bulkDocumentInfoBeOs = new List<BulkDocumentInfoBEO> {bulkDocument};
                            tags.Add(tag.Id, bulkDocumentInfoBeOs);
                            tagKeys.Add(tag.Id, tag.Name);
                        }
                    }
                }
                if (!tags.Any() || !tagKeys.Any()) return;
                using (new EVTransactionScope(TransactionScopeOption.Suppress))
                {
                    BulkTagging(tags, tagKeys, jobParameter.MatterId.ToString(CultureInfo.InvariantCulture),
                                jobParameter.DatasetId.ToString(CultureInfo.InvariantCulture),
                                jobParameter.CollectionId);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Bulk tagging the documents that are selected for import
        /// </summary>
        private void BulkTagging(IEnumerable<KeyValuePair<int, List<BulkDocumentInfoBEO>>> tags,
            IDictionary<int, string> tagKeys, string matterId, string datasetId, string collectionId)
        {
            var listBinders = BinderBO.GetAllBinders(datasetId);
            var includeDuplicates = "False" + Constants.Colon + Constants.Zero;
            var includeFamilies = "False" + Constants.Colon + Constants.Zero;
            foreach (var tag in tags)
            {
                var binderDetails = listBinders.FirstOrDefault(b => b.NotReviewedTagId == tag.Key);

                if (binderDetails == null) continue;
                //Sending for bulk tagging
                BulkTagBO.DoBulkOperation(tag.Key, tagKeys[tag.Key], tag.Value, byte.Parse("1"),
                                               matterId,
                                               collectionId,
                                               datasetId,
                                               includeDuplicates, includeFamilies, binderDetails.BinderId);
            }
        }

        #endregion Job Framework functions

        #region Private helper functions

        /// <summary>
        /// Sends the specified collection. Output message from the worker.
        /// </summary>
        /// <param name="collection">The collection.</param>
        private void Send(DocumentCollection collection)
        {
            var message = new PipeMessageEnvelope
            {
                Body = collection
            };

            if (null != OutputDataPipe)
            {
                OutputDataPipe.Send(message);
            }
            else
            {
                Tracer.Error("VaultWorker.Send: OutputDataPipe is null");
            }
        }

        /// <summary>
        /// Deserializes boot parameter and gets the import BEO.
        /// </summary>
        /// <param name="bootParamter">The boot parameter.</param>
        /// <returns> Deserialized Import BEO</returns>
        private ImportBEO GetImportBEO(string bootParamter)
        {
            //Creating a stringReader stream for the boot parameter
            var stream = new StringReader(bootParamter);

            //Creating xmlStream for xml serialization
            var xmlStream = new XmlSerializer(typeof (ImportBEO));

            // Deserialization of boot parameter to get ImportBEO
            return (ImportBEO) xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// Logs the vault messages.
        /// </summary>
        /// <param name="documentDetails">The document details.</param>
        /// <param name="isSuccessful">if set to <c>true</c> message is logged as informational successful message.</param>
        /// <param name="isMesasge">if set to <c>true</c> is a message Vs error.</param>
        /// <param name="message">The message.</param>
        private void LogVaultMessages(IEnumerable<DocumentDetail> documentDetails, bool isSuccessful,
                                            bool isMesasge, string message = "Document added to Vault")
        {
            if (!documentDetails.Any())
            {
                return;
            }

            var vaultLogList = new List<JobWorkerLog<VaultLogInfo>>();

            #region Loop through document details to create vault log list.

            foreach (var document in documentDetails)
            {
                var vaultLog = new JobWorkerLog<VaultLogInfo>();
                vaultLog.JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0;
                vaultLog.CorrelationId = (!string.IsNullOrEmpty(document.CorrelationId))
                    ? Convert.ToInt64(document.CorrelationId)
                    : 0;
                vaultLog.WorkerInstanceId = WorkerId;
                vaultLog.WorkerRoleType = "c9d79f4a-427a-4d3e-b7a7-abe10774796f";
                vaultLog.Success = isSuccessful;
                vaultLog.CreatedBy = "";
                vaultLog.IsMessage = isMesasge;

                var fieldValidationErrors = ValidateFieldTypes(document);
                if (!String.IsNullOrEmpty(fieldValidationErrors))
                {
                    vaultLog.IsMessage = true;
                }

                vaultLog.LogInfo = new VaultLogInfo
                {
                    DCN = document.document.DocumentControlNumber,
                    Information =
                        string.Format("DCN is {0}.{1}{2}", document.document.DocumentControlNumber, message,
                            fieldValidationErrors),
                    CrossReferenceField = document.document.CrossReferenceFieldValue
                };

                vaultLogList.Add(vaultLog);
            }

            #endregion Loop through document details to create vault log list.

            #region Use Log API and log the message

            var logMessage = new PipeMessageEnvelope
            {
                Body = vaultLogList
            };

            if (null != LogPipe)
            {
                LogPipe.Send(logMessage);
            }
            else
            {
                Tracer.Error("VaultWorker.Send: LogPipe is null");
            }

            #endregion Use Log API and log the message
        }

        private string ValidateFieldTypes(DocumentDetail documentDetail)
        {
            var result = "";
            var documentBeo = documentDetail.document;
            var fieldList = documentBeo.FieldList;

            foreach (var rvwDocumentFieldBeo in fieldList)
            {
                var fieldName = rvwDocumentFieldBeo.FieldName;
                var fieldValue = rvwDocumentFieldBeo.FieldValue;
                var fieldTypeID = rvwDocumentFieldBeo.FieldTypeId;

                switch (fieldTypeID)
                {
                    case 61: // Date
                        {
                            DateTime dummy;
                            if (!DateTime.TryParse(fieldValue, out dummy))
                            {
                                result += " \r\n" + "Field " + fieldName + " contains value " + fieldValue +
                                          " which is not valid for Date field type";
                            }
                        }
                        break;
                    case 104: // Boolean
                        {
                            Boolean dummy;
                            if (!Boolean.TryParse(fieldValue, out dummy))
                            {
                                result += " \r\n" + "Field " + fieldName + " contains value " + fieldValue +
                                          " which is not valid for Boolean field type";
                            }
                        }
                        break;
                    case 108: // Numeric
                        {
                            Double dummy;
                            if (!Double.TryParse(fieldValue, out dummy))
                            {
                                result += " \r\n" + "Field " + fieldName + " contains value " + fieldValue +
                                          " which is not valid for Numeric field type";
                            }
                        }
                        break;
                    case 231: // Text
                        {
                            if (fieldValue.Length > 4000)
                            {
                                result += " \r\n" + "Field " + fieldName + " contains string which is " + fieldValue.Length +
                                          " bytes long. Text field is limited to 4000 characters only";
                            }
                        }
                        break;
                }
            }
            return result;
        }

        #endregion Private helper functions
    }
}
