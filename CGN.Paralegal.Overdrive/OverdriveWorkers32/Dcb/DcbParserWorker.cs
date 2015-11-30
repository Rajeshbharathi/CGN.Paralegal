# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="DcbSlicerWorker.cs" company="Lexis Nexis">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <changelog>
//          <date value="06/18/2013">Bug Fix 86335</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure;
using ClassicServicesLibrary;

namespace LexisNexis.Evolution.Worker
{
    using System.Linq;

    using LexisNexis.Evolution.Business.Relationships;
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
    using LexisNexis.Evolution.TraceServices;

    public partial class DcbParserWorker : WorkerBase
    {
        #region Overdrive framework methods

        protected override void BeginWork()
        {
            base.BeginWork();
            ProfileBEO = Utils.SmartXmlDeserializer(BootParameters) as ProfileBEO;
            DcbOpticonJobBEO = PopulateImportRequest(ProfileBEO);

            InitializeDefaultFields();

            PrepareLocationForTextFiles();

            SelectDocumentIdGenertationAlgorithm();

            //Tracer.Trace("DcbParser: Finished BeginWork");
        }

        private void SelectDocumentIdGenertationAlgorithm()
        {
            if (DcbOpticonJobBEO.IsImportFamilies &&
                !DcbOpticonJobBEO.FamilyRelations.IsEmailDCB)
            {
                // TODO: More safety checks need to be added here
                SelectedDocumentIdGenerationAlgorithm = DocumentIdGenerationAlgorithm.FromDcbDocId;
                return;
            }

            SelectedDocumentIdGenerationAlgorithm = DocumentIdGenerationAlgorithm.FromPhysicalDocumentNumber;
        }

        private void PrepareLocationForTextFiles()
        {
            string compressedFileExtractionLocation = _dataset.CompressedFileExtractionLocation;
            if (null == compressedFileExtractionLocation) return;
            string folder = compressedFileExtractionLocation;
            if (null == folder || !Directory.Exists(folder))
            {
                Tracer.Error("DcbParser: PrepareLocationForTextFiles: Folder for text files {0} does not exists", folder);
            }
            TextFileFolder = folder;
        }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            if (null == envelope || null == envelope.Body)
            {
                return;
            }

            try
            {
                DcbSlice dcbSlice = (DcbSlice)envelope.Body;

                OpenDCB(dcbSlice.DcbCredentials);

                ImageSetId = dcbSlice.ImageSetId;

                FamiliesInfo familiesInfo = DcbOpticonJobBEO.IsImportFamilies ? new FamiliesInfo() : null;

                var documentDetailList = new List<DocumentDetail>();
                var dcbParserLogEntries = new List<JobWorkerLog<DcbParserLogInfo>>();
                int lastDocumentInTheBatch = dcbSlice.FirstDocument + dcbSlice.NumberOfDocuments - 1;
                for (int currentDocumentNumber = dcbSlice.FirstDocument;
                     currentDocumentNumber <= lastDocumentInTheBatch;
                     currentDocumentNumber++)
                {
                    JobWorkerLog<DcbParserLogInfo> dcbParserLogEntry =
                        new JobWorkerLog<DcbParserLogInfo>()
                            {
                                JobRunId = long.Parse(PipelineId),
                                WorkerInstanceId = WorkerId,
                                CorrelationId = currentDocumentNumber + 1, // CorrId is the same as TaskId and it is 1 based.
                                // This magic GUID is set in [EVMaster].[dbo].[EV_JOB_WorkerRoleType] to identify DcbParcer for Log Worker
                                WorkerRoleType = "e754adb7-23c8-44cc-8d4c-12f33aef41b6",
                                Success = true,
                                CreatedBy = ProfileBEO.CreatedBy,
                                IsMessage = false,
                                LogInfo = new DcbParserLogInfo()
                            };

                    FetchDocumentFromDCB(currentDocumentNumber, documentDetailList, familiesInfo, dcbParserLogEntry);

                    dcbParserLogEntries.Add(dcbParserLogEntry);
                }

                if (0 == dcbSlice.FirstDocument)
                {
                    DcbDatabaseTags dcbDatabaseTags = FetchDatabaseLevelTags();
                    if (null != dcbDatabaseTags)
                    {
                        if (null == documentDetailList[0].DcbTags)
                        {
                            documentDetailList[0].DcbTags = new List<DcbTags>();
                        }
                        documentDetailList[0].DcbTags.Add(dcbDatabaseTags);
                    }
                }

                if (DcbOpticonJobBEO.IsImportFamilies)
                {
                    SendRelationshipsInfo(familiesInfo.FamilyInfoList);
                }

                Send(documentDetailList);
                SendLog(dcbParserLogEntries);
            }
            catch (ArgumentOutOfRangeException exRange)
            {
                exRange.AddUsrMsg(Constants.ExportPathFull);
                ReportToDirector(exRange);
                exRange.Trace();
                LogMessage(false, Constants.ExportPathFull);
                throw;
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(false, "DcbParser failed to process document. Error: " + ex.ToUserString());
            }
        }

        private void SendRelationshipsInfo(IEnumerable<FamilyInfo> familyInfoRecords)
        {
            FamiliesInfo familiesInfo = new FamiliesInfo();

            foreach (FamilyInfo familyInfoRecord in familyInfoRecords)
            {
                // We don't skip standalone documents for Families, because they always can appear to be topmost parents
                //FamilyInfo familyInfoRecord = new FamilyInfo(docReferenceId);
                //familyInfoRecord.OriginalDocumentId = doc.document.EVLoadFileDocumentId;
                //familyInfoRecord.OriginalParentId = String.IsNullOrEmpty(doc.document.EVLoadFileParentId) ? null : doc.document.EVLoadFileParentId;

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

            if (familiesInfo.FamilyInfoList.Any())
            {
                SendFamilies(familiesInfo);
            }
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

        private DcbDatabaseTags FetchDatabaseLevelTags()
        {
            // If there are database level tags - fetch and store them to be sent with the first document
            if (!DcbOpticonJobBEO.IncludeTags)
            {
                return null;
            }

            List<string> compositeTagNames = DcbFacade.GetDatabaseTags();
            if (0 == compositeTagNames.Count)
            {
                return null;
            }

            DcbDatabaseTags dcbDatabaseTags = new DcbDatabaseTags()
                                    {
                                        compositeTagNames = compositeTagNames,
                                        DatasetId = DcbOpticonJobBEO.TargetDatasetId,
                                        MatterId = DcbOpticonJobBEO.MatterId
                                    };
            return dcbDatabaseTags;
        }

        private void OpenDCB(DcbCredentials dcbCredentials)
        {
            if (null != DcbFacade)
            {
                return; // Already opened
            }

            DcbFacade = new DcbFacade();
            if (null == dcbCredentials)
            {
                Tracer.Debug("DcbParser: Opening unsecured DCB database {0}", DcbOpticonJobBEO.DcbSourcePath);
                DcbFacade.OpenDCB(DcbOpticonJobBEO.DcbSourcePath, null, null);
            }
            else
            {
                Tracer.Debug("DcbParser: Opening secured DCB database {0}", DcbOpticonJobBEO.DcbSourcePath);
                DcbFacade.OpenDCB(DcbOpticonJobBEO.DcbSourcePath, dcbCredentials.Login, dcbCredentials.Password);
            }

            //Fix for UI bug
            IncludeDcbFieldsForContentInFieldMapping();
        }

        private void Send(List<DocumentDetail> docDetails)
        {
            var documentList = new DocumentCollection() { documents = docDetails, dataset = _dataset };
            var message = new PipeMessageEnvelope()
            {
                Body = documentList
            };
            OutputDataPipe.Send(message);
            //IncreaseProcessedDocumentsCount(docDetails.Count(DocumentDetail => DocumentDetail.docType == DocumentsetType.NativeSet));
            IncreaseProcessedDocumentsCount(docDetails.Count);
        }

        private void LogMessage(bool status, string information)
        {
            List<JobWorkerLog<DcbParserLogInfo>> log = new List<JobWorkerLog<DcbParserLogInfo>>();
            JobWorkerLog<DcbParserLogInfo> parserLog = new JobWorkerLog<DcbParserLogInfo>();
            parserLog.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            parserLog.CorrelationId = 0;// TaskId
            parserLog.WorkerRoleType = "6d7f04b0-f323-4f51-b6ba-959afb818e17";
            parserLog.WorkerInstanceId = WorkerId;
            parserLog.IsMessage = false;
            parserLog.Success = status;
            parserLog.CreatedBy = (!string.IsNullOrEmpty(ProfileBEO.CreatedBy) ? ProfileBEO.CreatedBy : "N/A");
            parserLog.LogInfo = new DcbParserLogInfo();
            parserLog.LogInfo.Information = information;
            parserLog.LogInfo.Message = information;
            log.Add(parserLog);
            SendLog(log);
        }

        private void SendLog(List<JobWorkerLog<DcbParserLogInfo>> log)
        {
            var message = new PipeMessageEnvelope()
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        protected override void EndWork()
        {
            base.EndWork();

            if (DcbFacade != null)
            {
                DcbFacade.Dispose();
            }

            // This supposed to be called only once per process using DcbFacade
            //DcbFacade.Terminate();
        }

        #endregion

        private string TextFileFolder { get; set; }
        private DcbFacade DcbFacade { get; set; }

        private string ImageSetId { get; set; }

        private enum DocumentIdGenerationAlgorithm
        {
            FromPhysicalDocumentNumber,
            FromDcbDocId
        };

        private DocumentIdGenerationAlgorithm SelectedDocumentIdGenerationAlgorithm { get; set; }
    }
}
