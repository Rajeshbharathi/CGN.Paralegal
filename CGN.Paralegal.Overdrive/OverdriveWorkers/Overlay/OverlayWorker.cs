using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.BusinessEntities;
using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Business.Relationships;
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    public class OverlayWorker : WorkerBase
    {
        private ImportBEO _jobParameter;
        private DatasetBEO _dataset;
        private UserBusinessEntity _userInfo;
        private int _batchSize = 100;
        private bool _isIncludeNativeFile = false;
        #region "Overlay DocumentId Container"
        //Key : Newly generated Id during overlay
        //Value : Id generated earlier(during Append)
        #endregion

        #region OverDrive

        protected override void BeginWork()
        {
            base.BeginWork();
            _jobParameter = GetImportBEO(BootParameters);

            if (_jobParameter != null && !string.IsNullOrEmpty(_jobParameter.CreatedBy))
            {
                //Get User Information , Its needed for search
                _userInfo = UserBO.AuthenticateUsingUserGuid(_jobParameter.CreatedBy);
                _userInfo.CreatedBy = _jobParameter.CreatedBy;
                _isIncludeNativeFile = (_jobParameter != null && _jobParameter.IsImportNativeFiles);
            }
        }


        /// <summary>
        /// Processes the work item.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                DocumentCollection recordParserResponse = (DocumentCollection)message.Body;
                #region Assertion
                recordParserResponse.ShouldNotBe(null);
                recordParserResponse.documents.ShouldNotBe(null);
                #endregion

                if (_jobParameter.IsAppend)
                {
                    throw new Exception(Constants.ErrorMessageInvalidPipeLine);
                }
                if (recordParserResponse == null)
                {
                    return;
                }
                if (recordParserResponse.dataset != null)
                {
                    _dataset = recordParserResponse.dataset;
                }
                if (recordParserResponse.documents != null && recordParserResponse.documents.Any())
                {
                    ProcessDocuments(recordParserResponse);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        #endregion

        /// <summary>
        /// Send data to Data pipe
        /// </summary>
        /// <param name="docDetails"></param>
        private void Send(List<DocumentDetail> docDetails)
        {
            bool overlayIsRemoveTag = _jobParameter != null && _jobParameter != null && !_jobParameter.IsOverlayRetainTags;
            var documentList = new DocumentCollection() { documents = docDetails, dataset = _dataset, IsDeleteTagsForOverlay = overlayIsRemoveTag, IsIncludeNativeFile = _isIncludeNativeFile };
            var message = new PipeMessageEnvelope()
            {
                Body = documentList
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(docDetails.Count);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<OverlaySearchLogInfo>> log)
        {
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// Process each document 
        /// </summary>
        private void ProcessDocuments(DocumentCollection recordParserResponse)
        {
            var documentDetailList = new List<DocumentDetail>();
            var overlayLogList = new List<JobWorkerLog<OverlaySearchLogInfo>>();
            var docManager = new OverlayDocumentManager(_jobParameter, _dataset, PipelineId, WorkerId);

            //Bulk Search for entire batch
            documentDetailList = docManager.BulkSearch(recordParserResponse.documents, _userInfo, out overlayLogList);

            SendRelationshipsInfo(documentDetailList);

            #region Send Message
            //Send response   
            if (documentDetailList != null && documentDetailList.Any())
            {
                if (documentDetailList.Count > _batchSize)
                {
                    Send(documentDetailList.Take(_batchSize).ToList());
                    var remainDocumentList = documentDetailList.Skip(_batchSize).ToList();
                    Send(remainDocumentList); //send remaining list
                    documentDetailList.Clear();
                }
                else
                {
                    Send(documentDetailList);
                    documentDetailList.Clear();
                }
            }
            //Send Log   
            if (overlayLogList != null && overlayLogList.Any())
            {
                SendLog(overlayLogList);
                overlayLogList.Clear();
            }
            #endregion
        }

        private void SendRelationshipsInfo(IEnumerable<DocumentDetail> documentDetailList)
        {
            bool familiesLinkingRequested = _jobParameter.IsImportFamilyRelations;
            bool threadsLinkingRequested = _jobParameter.IsMapEmailThread;

            FamiliesInfo familiesInfo = familiesLinkingRequested ? new FamiliesInfo() : null;
            ThreadsInfo threadsInfo = threadsLinkingRequested ? new ThreadsInfo() : null;

            foreach (DocumentDetail doc in documentDetailList)
            {
                if (doc.docType != DocumentsetType.NativeSet)
                {
                    continue; // Only original documents may participate in relationships
                }

                string docReferenceId = doc.document.DocumentId;
                if (String.IsNullOrEmpty(docReferenceId))
                {
                    continue;
                }

                // Debug
                //Tracer.Warning("DOCID {0} corresponds to the document {1}", doc.document.FieldList[0].FieldValue, docReferenceId.Hint(4));
                //if (docReferenceId.Hint(4) == "AFE1")
                //{
                //    Tracer.Warning("STOP!");
                //}

                if (familiesLinkingRequested && !String.IsNullOrEmpty(doc.document.EVLoadFileDocumentId))
                    {
                    // We don't skip standalone documents for Families, because they always can appear to be topmost parents
                    FamilyInfo familyInfoRecord = new FamilyInfo(docReferenceId);
                    familyInfoRecord.OriginalDocumentId = doc.document.EVLoadFileDocumentId;
                    familyInfoRecord.OriginalParentId = String.IsNullOrEmpty(doc.document.EVLoadFileParentId) ? null : doc.document.EVLoadFileParentId;

                    //Tracer.Warning("SendRelationshipsInfo: OriginalDocumentId = {0}, OriginalParentId = {1}",
                    //    familyInfoRecord.OriginalDocumentId, familyInfoRecord.OriginalParentId);

                    if (String.Equals(familyInfoRecord.OriginalDocumentId, familyInfoRecord.OriginalParentId, StringComparison.InvariantCulture))
                    {
                        //Tracer.Warning("SendRelationshipsInfo: OriginalDocumentId = {0}, OriginalParentId reset to null", familyInfoRecord.OriginalDocumentId);
                        familyInfoRecord.OriginalParentId = null; // Document must not be its own parent
                    }

                    // Family has priority over thread, so if the document is part of the family we ignore its thread
                    //if (familyInfoRecord.OriginalParentId != null)
                    //{
                    //    //Tracer.Warning("SendRelationshipsInfo: OriginalDocumentId = {0}, ConversationIndex reset to null", familyInfoRecord.OriginalDocumentId);
                    //    doc.ConversationIndex = null;
                    //}
                    familiesInfo.FamilyInfoList.Add(familyInfoRecord);
                }

                // BEWARE: doc.document.ConversationIndex is not the right thing!!
                if (threadsLinkingRequested)
                {
                    // Sanitize the value
                    doc.ConversationIndex = String.IsNullOrEmpty(doc.ConversationIndex) ? null : doc.ConversationIndex;

                    // Debug
                    //Tracer.Warning("SendRelationshipsInfo: CollectionId = {0}", doc.document.CollectionId);

                    var threadInfo = new ThreadInfo(docReferenceId, doc.ConversationIndex);
                    threadsInfo.ThreadInfoList.Add(threadInfo);
                }
            }

            if (threadsLinkingRequested && threadsInfo.ThreadInfoList.Any())
            {
                SendThreads(threadsInfo);
            }

            if (familiesLinkingRequested && familiesInfo.FamilyInfoList.Any())
            {
                SendFamilies(familiesInfo);
            }
        }

        private void SendThreads(ThreadsInfo threadsInfo)
        {
            Pipe familiesAndThreadsPipe = GetOutputDataPipe("ThreadsLinker");
            familiesAndThreadsPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = threadsInfo
            };
            familiesAndThreadsPipe.Send(message);
        }

        private void SendFamilies(FamiliesInfo familiesInfo)
        {
            Pipe familiesAndThreadsPipe = GetOutputDataPipe("FamiliesLinker");
            familiesAndThreadsPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = familiesInfo
            };
            familiesAndThreadsPipe.Send(message);
        }

        #region Boot Parameter
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
            var xmlStream = new XmlSerializer(typeof(ImportBEO));

            //Deserialization of bootparameter to get ImportBEO
            return (ImportBEO)xmlStream.Deserialize(stream);
        }
        #endregion
    }
}
