#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="EDocsExtractionWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Keerti/Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  EDocsExtractionWorker
//      </description>
//      <changelog>
//           <date value="01/10/2012">Bugs Fixed #92992</date>
//           <date value="01/11/2012">Bugs Fixed #95197</date>
//           <date value="01/17/2012">Bugs Fixed #95197-Made a fix for email redact issue by using overdrive non-linear pipeline </date>
//           <date value="04/02/2012">Bug fix for 98615</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
#region Namespaces
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.DocumentExtractionUtilities;
using LexisNexis.Evolution.DataAccess.ServerManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.TraceServices;
#endregion
namespace LexisNexis.Evolution.Worker
{
    using LexisNexis.Evolution.Business.Relationships;

    public class EDocsExtractionWorker : WorkerBase
    {
        ProfileBEO m_Parameters;
        DatasetBEO m_Dataset;
        int m_OutputBatchSize;
        int m_PercenatgeCompletion;
        List<RVWDocumentBEO> m_Documents;
        List<OutlookMailStoreEntity> m_OutlookMailStoreDataEntities;
        uint m_PipelineBatchSize;
        string m_DecryptionKey;
        long m_CounterForCorrelationId;
        IFileProcessor m_FileProcessor = null;

        /// <summary>
        /// Processes the work item. pushes give document files for conversion
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            base.ProcessMessage(message);

            try
            {
                DocumentExtractionMessageEntity documentExtractionMessageEntity = message.Body as DocumentExtractionMessageEntity;
                Debug.Assert(documentExtractionMessageEntity != null, "documentExtractionMessageEntity != null");
                IEnumerable<string> listOfFiles = documentExtractionMessageEntity.FileCollection;

                if (listOfFiles == null || !listOfFiles.Any())
                {
                    throw new EVException().AddErrorCode(ErrorCodes.EDLoaderExtractionWorker_NoFilesToProcess);
                }
                List<FileInfo> files = new List<FileInfo>();
                listOfFiles.SafeForEach(p => files.Add(new FileInfo(p)));

                if (files.Count <= 0)
                {
                    throw new EVException().AddResMsg(ErrorCodes.EDLoaderExtractionWorker_NoFilesToProcess);
                }

                Debug.Assert(m_FileProcessor != null);

                //?? 1. need to make it configurable item
                //?? 2. Need to ensure output.xmls are deleted.
                // ED Loader job should not delete native files ever, hence if delete temporary files is set to true - delete NON native files else delete none
                // Delete none is mostly used for debugging so that EDRM files extracted can be examined.
                //?? need to fix before end of sprint 3
                m_FileProcessor.IsDeleteTemporaryFiles = (false)
                    ? DeleteExtractedFilesOptions.DeleteNonNativeFiles
                    : DeleteExtractedFilesOptions.DeleteNone;

                // if password list (used for password protected archive files extraction) is set, assign it to File Processor after decryption
                if (m_Parameters != null && m_Parameters.PasswordList != null && m_Parameters.PasswordList.Count > 0)
                {
                    List<string> decryptedPasswordList = new List<string>();
                    m_Parameters.PasswordList.SafeForEach(
                        x => decryptedPasswordList.Add(ApplicationConfigurationManager.Decrypt(x, m_DecryptionKey)));
                    m_FileProcessor.Passwords = decryptedPasswordList;
                }

                Debug.Assert(m_Parameters != null, "m_Parameters != null");
                m_FileProcessor.DatasetBeo = m_Parameters.DatasetDetails;
                m_FileProcessor.FilterByMappedFields = m_Parameters.FieldMapping;

                long jobRunId;
                long.TryParse(PipelineId, out jobRunId);

                List<EmailThreadingEntity> rawDocumentRelationships = new List<EmailThreadingEntity>();

                // this call performs document extraction and calls ImportEvDocumentDataEntity as and when extraction is done for each file.
                m_FileProcessor.ProcessDocumentWithCallBack<double>(files,
                    new DirectoryInfo(m_Parameters.DatasetDetails.CompressedFileExtractionLocation),
                    m_OutputBatchSize,
                    jobRunId,
                    ProcessDocumentEntities,
                    OnHandleException,
                    m_PercenatgeCompletion,
                    rawDocumentRelationships);

                if (rawDocumentRelationships.Any())
                {
                    SendRelationshipsInfo(rawDocumentRelationships);
                }

                // if it's the last message send remaining documents
                if ((documentExtractionMessageEntity.IsLastMessageInBatch && m_Documents != null && m_Documents.Any())
                        || (m_OutlookMailStoreDataEntities != null && m_OutlookMailStoreDataEntities.Any()))
                {
                    Send(m_Documents, true);
                    m_Documents.Clear();
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace();
                if (ex.GetErrorCode() == ErrorCodes.ImportDiskFullErrorMessage)
                {
                    // if the disk is full then we throw the exception to stop the job and log the message
                    LogMessage(false, Constants.DiskFullErrorMessage, false);
                    throw;
                }
                LogMessage(false, ex.ToUserString(), false);
                ex.Swallow();
            }
        }

        private void SendRelationshipsInfo(IEnumerable<EmailThreadingEntity> rawDocumentRelationships)
        {
            // For eDocs we ALWAYS send relationships info
            //if (!m_Parameters.IsImportFamilyRelations)
            //{
            //    return;
            //}

            FamiliesInfo familiesInfo = new FamiliesInfo();
            ThreadsInfo threadsInfo = new ThreadsInfo();

            foreach (EmailThreadingEntity emailThreadingEntity in rawDocumentRelationships)
            {
                string docReferenceId = emailThreadingEntity.ChildDocumentID;
                if (String.IsNullOrEmpty(docReferenceId))
                {
                    continue;
                }

                if (emailThreadingEntity.RelationshipType == ThreadRelationshipEntity.RelationshipType.OutlookEmailThread)
                {
                    // Sanitize the value
                    emailThreadingEntity.ConversationIndex = String.IsNullOrEmpty(emailThreadingEntity.ConversationIndex) ? null : emailThreadingEntity.ConversationIndex;

                    // On Append we only calculate relationships between new documents, 
                    // therefore we don't even send standalone documents to threads linker
                    if (emailThreadingEntity.ConversationIndex == null)
                    {
                        continue;
                    }

                    var threadInfo = new ThreadInfo(docReferenceId, emailThreadingEntity.ConversationIndex);
                    threadsInfo.ThreadInfoList.Add(threadInfo);
                }
                else
                {
                    // We don't skip standalone documents for Families, because they always can appear to be topmost parents
                    FamilyInfo familyInfoRecord = new FamilyInfo(docReferenceId);
                    familyInfoRecord.OriginalDocumentId = docReferenceId;
                    familyInfoRecord.OriginalParentId = String.IsNullOrEmpty(emailThreadingEntity.ParentDocumentID) ? null : emailThreadingEntity.ParentDocumentID;

                    //Tracer.Warning("SendRelationshipsInfo: OriginalDocumentId = {0}, OriginalParentId = {1}",
                    //    familyInfoRecord.OriginalDocumentId, familyInfoRecord.OriginalParentId);

                    if (String.Equals(familyInfoRecord.OriginalDocumentId, familyInfoRecord.OriginalParentId, StringComparison.InvariantCulture))
                    {
                        //Tracer.Warning("SendRelationshipsInfo: OriginalDocumentId = {0}, OriginalParentId reset to null", familyInfoRecord.OriginalDocumentId);
                        familyInfoRecord.OriginalParentId = null; // Document must not be its own parent
                    }
                    familiesInfo.FamilyInfoList.Add(familyInfoRecord);
                }

                const int BatchSize = 500;
                if (threadsInfo.ThreadInfoList.Count >= BatchSize)
                {
                    SendThreads(threadsInfo);
                    threadsInfo.ThreadInfoList.Clear();
                }
                if (familiesInfo.FamilyInfoList.Count >= BatchSize)
                {
                    SendFamilies(familiesInfo);
                    familiesInfo.FamilyInfoList.Clear();
                }
            }
            if (threadsInfo.ThreadInfoList.Any())
            {
                SendThreads(threadsInfo);
            }
            if (familiesInfo.FamilyInfoList.Any())
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
        internal void LogMessage(bool status, string information, bool isMessage)
        {
            List<JobWorkerLog<EDocsExtractionLogInfo>> edocsExtarctionLogs = new List<JobWorkerLog<EDocsExtractionLogInfo>>();
            JobWorkerLog<EDocsExtractionLogInfo> extracionLog = new JobWorkerLog<EDocsExtractionLogInfo>()
            {
                JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = 0 /* TaskId*/,
                WorkerRoleType = Constants.EDOCSExtractionRoleType,
                WorkerInstanceId = WorkerId,
                IsMessage = isMessage,
                Success = status,
                CreatedBy = (!string.IsNullOrEmpty(m_Parameters.CreatedBy) ? m_Parameters.CreatedBy : "N/A"),
                LogInfo = new EDocsExtractionLogInfo()
            };
            extracionLog.LogInfo.Information = information;
            extracionLog.LogInfo.Message = information;
            extracionLog.LogInfo.DatasetExtractedPath = m_Parameters.DatasetDetails.CompressedFileExtractionLocation;
            edocsExtarctionLogs.Add(extracionLog);
            SendLog(edocsExtarctionLogs);
        }
        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<EDocsExtractionLogInfo>> log)
        {
            try
            {
                var message = new PipeMessageEnvelope()
                {
                    Body = log
                };
                LogPipe.Send(message);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }
        private void ProcessDocumentEntities(EvDocumentDataEntity documentData, double taskPercentage)
        {
            try
            {
                foreach (var rvwDocumentBEO in documentData.Documents)
                {
                    CreateAndUpdateExternalFileEntityForNativeSet(rvwDocumentBEO);
                    if (File.Exists(rvwDocumentBEO.NativeFilePath))
                    {
                        rvwDocumentBEO.MD5HashValue = DocumentHashHelper.GetMD5HashValue(rvwDocumentBEO.NativeFilePath);
                        rvwDocumentBEO.SHAHashValue = DocumentHashHelper.GetSHAHashValue(rvwDocumentBEO.NativeFilePath);
                    }
                }

                m_Documents.AddRange(documentData.Documents);

                #region Accumilate Outlook MailStoreData Entities - they are flushed and put on queue along with documents being sent.

                // Check if outlook mail stores exist
                if (documentData.OutlookMailStoreDataEntity != null && documentData.OutlookMailStoreDataEntity.PSTFile != null && documentData.OutlookMailStoreDataEntity.EntryIdAndEmailMessagePairs.Count() > 0)
                {
                    if (m_OutlookMailStoreDataEntities == null) m_OutlookMailStoreDataEntities = new List<OutlookMailStoreEntity>();

                    m_OutlookMailStoreDataEntities.Add(documentData.OutlookMailStoreDataEntity);
                }
                #endregion Accumilate Outlook MailStoreData Entities - they are flushed and put on queue along with documents being sent.

                if (m_Documents.Count >= m_PipelineBatchSize)
                {
                    IEnumerable<RVWDocumentBEO> documentBatch = m_Documents.Take(Convert.ToInt16(m_PipelineBatchSize));
                    Send(documentBatch, false);
                    m_Documents.RemoveRange(0, Convert.ToInt16(m_PipelineBatchSize));
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Creates the and update external file entity for native set.
        /// </summary>
        /// <param name="document">The document.</param>
        private static void CreateAndUpdateExternalFileEntityForNativeSet(RVWDocumentBEO document)
        {
            try
            {
                //Insert document binary, if any -- to do: Should we need call this @ this place
                if (document != null && !StringUtility.IsNullOrWhiteSpace(document.NativeFilePath))
                {
                    RVWExternalFileBEO externalFile = new RVWExternalFileBEO();
                    externalFile.Type = "Native";
                    externalFile.Path = document.NativeFilePath;
                    document.DocumentBinary.FileList.Add(externalFile);
                }
                else
                {
                    throw new EVException().AddErrorCode(ErrorCodes.EDLoaderExtractionWorker_NativeFileNotAvailable);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="documentBatch">The document batch.</param>
        /// <param name="isIncludeOutlookMailStoreDataEntityIfAvailable">if set to <c>true</c> [includes outlook mail store data entity in the send list (when available)].</param>
        private void Send(IEnumerable<RVWDocumentBEO> documentBatch, bool isIncludeOutlookMailStoreDataEntityIfAvailable)
        {
            try
            {
                DocumentCollection documentCollection = null;
                List<DocumentDetail> documentDetailList = new List<DocumentDetail>();

                if (documentBatch != null)
                {
                    foreach (RVWDocumentBEO document in documentBatch)
                    {
                        m_CounterForCorrelationId += 1;
                        DocumentDetail documentDetail = new DocumentDetail
                        {
                            CorrelationId = m_CounterForCorrelationId.ToString(),
                            docType = DocumentsetType.NativeSet,
                            document = document,
                            IsNewDocument = true
                        };

                        documentDetailList.Add(documentDetail);
                    }

                    documentCollection = new DocumentCollection
                    {
                        dataset = m_Dataset,
                        documents = documentDetailList
                    };
                }
                if (documentCollection != null)
                {
                    Pipe vaultOutDataPipe = GetOutputDataPipe("Vault");
                    var message = new PipeMessageEnvelope()
                    {
                        Body = documentCollection
                    };
                    if (vaultOutDataPipe != null)
                    {
                        vaultOutDataPipe.Send(message);
                    }
                }
                // All available outlook mail stores are sent to the queue. they shouldn't be sent again. So clear existing list.
                if (isIncludeOutlookMailStoreDataEntityIfAvailable && m_OutlookMailStoreDataEntities != null
                    && m_OutlookMailStoreDataEntities.Count > 0)
                {
                    var message = new PipeMessageEnvelope()
                   {
                       Body = new EDocsDocumentCollection
                       {
                           OutlookMailStoreDataEntity = m_OutlookMailStoreDataEntities
                       }
                   };
                    Pipe eDocsOutlookEmailGeneratorOutputDataPipe = GetOutputDataPipe("EDocsOutlookEmailGenerator");
                    if (eDocsOutlookEmailGeneratorOutputDataPipe != null)
                    {
                        eDocsOutlookEmailGeneratorOutputDataPipe.Send(message);
                    }
                    m_OutlookMailStoreDataEntities.Clear();

                }
                IncreaseProcessedDocumentsCount(documentCollection.documents.Count);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }


        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();

            try
            {
                m_Parameters = DocumentImportHelper.GetProfileBeo((string)BootParameters);

                m_CounterForCorrelationId = 0;

                InitializeConfigurationItems();

                //?? need to percentage completion
                m_PercenatgeCompletion = 100;

                m_Documents = new List<RVWDocumentBEO>();

                #region Get Dataset Details
                if (m_Parameters != null && m_Parameters.DatasetDetails.FolderID > 0)
                {
                    m_FileProcessor = FileProcessorFactory.CreateFileProcessor(
                                             FileProcessorFactory.ExtractionChoices.CompoundFileExtraction);
                    m_Dataset = DataSetBO.GetDataSetDetailForDataSetId(m_Parameters.DatasetDetails.FolderID);

                    if (m_Dataset.Matter != null && m_Dataset.Matter.FolderID > 0)
                    {
                        var matterDetails = MatterDAO.GetMatterDetails(m_Dataset.Matter.FolderID.ToString());
                        if (matterDetails != null)
                        {
                            m_Dataset.Matter = matterDetails;
                            var searchServerDetails = ServerDAO.GetSearchServer(matterDetails.SearchServer.Id);
                            if (searchServerDetails != null)
                            {
                                m_Dataset.Matter.SearchServer = searchServerDetails;
                            }
                        }
                    }
                    else
                        throw new EVException().AddErrorCode(ErrorCodes.EDLoaderExtractionWorker_FailedToObtainMatterDetails); //?? need to set message in resource file
                }
                else
                {
                    throw new EVException().AddErrorCode(ErrorCodes.EDLoaderExtractionWorker_ObtainDatasetDetailsFailure); //?? need to set message in resource file
                }
                #endregion
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Handles the exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        private void OnHandleException(Exception ex)
        {
            ex.Data[Constants.Message] = ex.Message;
            if (ex.GetType() == typeof(EVException))
            {
                ex.Data[Constants.ErrorCode] = (ex as EVException).GetErrorCode();
                ex.Trace();
            }

        }

        /// <summary>
        /// Initializes the configuration items.
        /// </summary>
        private void InitializeConfigurationItems()
        {

            // default values if reading from configuration fails. this avoids job from failing.
            m_OutputBatchSize = 100;
            m_PipelineBatchSize = 100;

            try
            {
                // this can't have a default value.
                m_DecryptionKey = ApplicationConfigurationManager.GetValue("EncryptionKey", "Data Security");
            }
            catch (Exception ex)
            {
                ex.AddErrorCode(ErrorCodes.EDLoaderExtractionWorker_DecryptionKeyUnavailable).Trace().Swallow();
            }

            try
            {
                int.TryParse(ApplicationConfigurationManager.GetValue("EDocsOutputBatchSize", "Imports"), out m_OutputBatchSize);
                uint.TryParse(ApplicationConfigurationManager.GetValue("EDocsPipelineBatchSize", "Imports"), out m_PipelineBatchSize);
            }
            catch (Exception ex)
            {
                ex.AddErrorCode(ErrorCodes.EDLoaderExtractionWorker_ConfigurationUnavailable).Trace().Swallow();
            }

        }

        /// <summary>
        /// Ends the work.
        /// </summary>
        protected override void EndWork()
        {
            base.EndWork();
            m_FileProcessor.Dispose();
            m_FileProcessor = null;

        }


    }
}
