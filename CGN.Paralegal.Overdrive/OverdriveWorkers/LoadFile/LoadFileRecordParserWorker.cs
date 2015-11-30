//-----------------------------------------------------------------------------------------
// <copyright file="LoadFileRecordParserWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil Paramathma</author>
//      <description>
//          Class file for holding document detail extraction methods
//      </description>
//      <changelog>//          
//          <date value="14-Feb-2012">Fix for bug 96652</date>
//          <date value="23-Feb-2012">Fix for bug 96718,96719,96721</date>
//      <date value="07/17/2013">CNEV 2.2.1 - CR005 Implementation : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using System.Threading.Tasks;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Business.Relationships;

    public class LoadFileRecordParserWorker : WorkerBase
    {
        private ImportBEO _jobParameter;
        //LoadFile parser response.
        private LoadFileRecordCollection _parserResponse;
        private int _batchSize = 100;
        private string _datasetPath;
        private DatasetBEO _dataset;

        /// <summary>
        /// Unique identifier associated with a load file. 
        /// This value helps 1) identify document id (using record id in load file)
        /// 2) restrict relationships to a single load file (no relationships across load files)
        /// </summary>
        private string _uniqueThreadString;

        #region Load File

        /// <summary>
        /// Method to parse the load file
        /// </summary>
        public void ParseRecordText()
        {
            var documentDetailList = new List<DocumentDetail>();
            var recordParserLogList = new List<JobWorkerLog<LoadFileDocumentParserLogInfo>>();

            if (_parserResponse == null || _parserResponse.Records.Count <= 0)
            {
                return;
            }

            var docManager = new LoadFileDocumentManager(_jobParameter, _uniqueThreadString, _datasetPath, _dataset, PipelineId, WorkerId,WorkAssignment.JobId);

            #region Generate Document - Parallel
           
            Parallel.ForEach(_parserResponse.Records, rec =>
            {
                JobWorkerLog<LoadFileDocumentParserLogInfo> recordParserLog = null;
                var docs = docManager.GetDocuments(rec.CorrelationId, 
                    rec.RecordText, rec.DocumentControlNumber, rec.ContentFile, out recordParserLog);
                lock (documentDetailList)
                {
                    if (docs != null)
                    {
                        documentDetailList.AddRange(docs);
                    }
                    if (recordParserLog != null)
                    {
                        recordParserLogList.Add(recordParserLog);
                    }
                } // lock                     
            });
            
            #endregion

            if (_jobParameter.IsAppend) // For Overlay similar method is in the OverlayWorker
            {
                SendRelationshipsInfo(documentDetailList);
            }

            #region Send Message
            //Send response   
            if (documentDetailList.Any())
            {
                if (documentDetailList.Count > _batchSize)
                {
                    Send(documentDetailList.Take(_batchSize).ToList());
                    var remainDocumentList = documentDetailList.Skip(_batchSize).ToList();
                    Send(remainDocumentList); //send remaining list
                }
                else
                {
                    Send(documentDetailList);
                }
            }

            //Send Log   
            if (recordParserLogList.Any())
            {
                SendLog(recordParserLogList);
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

                if (familiesLinkingRequested && !String.IsNullOrEmpty(doc.document.EVLoadFileDocumentId))
                {
                    // We don't skip standalone documents for Families, because they always can appear to be topmost parents.
                    // And also we need all of them to provide Original to Real Id translation. 
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

                    familiesInfo.FamilyInfoList.Add(familyInfoRecord);
                }

                // BEWARE: doc.document.ConversationIndex is not the right thing!!
                if (threadsLinkingRequested)
                {
                    // Sanitize the value
                    doc.ConversationIndex = String.IsNullOrEmpty(doc.ConversationIndex) ? null : doc.ConversationIndex;

                    // On Append we only calculate relationships between new documents, 
                    // therefore we don't even send standalone documents to threads linker
                    if (doc.ConversationIndex == null)
                    {
                        continue;
                    }

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
            Pipe threadsPipe = GetOutputDataPipe("ThreadsLinker");
            threadsPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = threadsInfo
            };
            threadsPipe.Send(message);
        }

        private void SendFamilies(FamiliesInfo familiesInfo)
        {
            Pipe familiesPipe = GetOutputDataPipe("FamiliesLinker");
            familiesPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = familiesInfo
            };
            familiesPipe.Send(message);
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        internal void LogMessage(bool status, string information)
        {
            List<JobWorkerLog<LoadFileDocumentParserLogInfo>> log = new List<JobWorkerLog<LoadFileDocumentParserLogInfo>>();
            JobWorkerLog<LoadFileDocumentParserLogInfo> parserLog = new JobWorkerLog<LoadFileDocumentParserLogInfo>();
            parserLog.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            parserLog.CorrelationId = 0;// TaskId
            parserLog.WorkerRoleType = Constants.LoadFileRecordParserWorkerRoleType;
            parserLog.WorkerInstanceId = WorkerId;
            parserLog.IsMessage = false;
            parserLog.Success = status;
            parserLog.CreatedBy = (!string.IsNullOrEmpty(_jobParameter.CreatedBy) ? _jobParameter.CreatedBy : "N/A");
            parserLog.LogInfo = new LoadFileDocumentParserLogInfo();
            parserLog.LogInfo.Information = information;
            if (!status)
                parserLog.LogInfo.Message = information;
            log.Add(parserLog);
            SendLog(log);
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
            var xmlStream = new XmlSerializer(typeof(ImportBEO));

            //Deserialization of bootparameter to get ImportBEO
            return (ImportBEO)xmlStream.Deserialize(stream);
        }

        #endregion

        /// <summary>
        /// Send data to Data pipe
        /// </summary>
        /// <param name="docDetails"></param>
        private void Send(List<DocumentDetail> docDetails)
        {
            OutputDataPipe.ShouldNotBe(null);
            var documentList = new DocumentCollection() { documents = docDetails, dataset = _dataset };
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
        private void SendLog(List<JobWorkerLog<LoadFileDocumentParserLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }


        #region Overdrive

        protected override void BeginWork()
        {
            base.BeginWork();
            _jobParameter = GetImportBEO((string)BootParameters);
        }

        /// <summary>
        /// Processes the work item.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                _parserResponse = (LoadFileRecordCollection)message.Body;

                #region Dataset Detaills

                if (_dataset == null)
                {
                    _dataset = _parserResponse.dataset;
                    if (_dataset != null && !string.IsNullOrEmpty(_dataset.CompressedFileExtractionLocation))
                    {
                        _datasetPath = _dataset.CompressedFileExtractionLocation;
                    }
                }
                if (String.IsNullOrEmpty(_uniqueThreadString))
                {
                    _uniqueThreadString = _parserResponse.UniqueThreadString;
                }

                #endregion

                ParseRecordText();
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, ex.ToUserString());
            }
        }

        #endregion
    }
}
