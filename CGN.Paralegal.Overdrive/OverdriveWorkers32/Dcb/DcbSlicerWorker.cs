# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="DcbSlicerWorker.cs" company="Lexis Nexis">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <author>nagaraju/raj kumar/ganesh</author>
//      <description>
//          This is a file that contains DcbSlicerWorker class
//      </description>
//      <changelog>
//          <date value="03/02/2012">Bug Fix 86335</date>
//          <date value="06/18/2013">Bug Fix 86335</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using ClassicServicesLibrary;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Worker
{
    using System.Runtime.CompilerServices;
    using Infrastructure.ExceptionManagement;

    public class DcbSlicerWorker : WorkerBase
    {
        protected override void BeginWork()
        {
            try
            {
                base.BeginWork();
                ProfileBEO = Utils.SmartXmlDeserializer(BootParameters) as ProfileBEO;
                DcbOpticonJobBEO = PopulateImportRequest(ProfileBEO);

                if (DcbOpticonJobBEO.ImportImages)
                {
                    ImageSetId = GetImageSetId();
                }

                OpenDCB();


                numberOfDocumentsInDcb = DcbFacade.GetNumberOfDocuments();
                Tracer.Info("DCB Slicer: BeginWork found {0} documents to fetch", numberOfDocumentsInDcb);
            }
            catch (Exception ex)
            {
                LogMessage(false, "DcbSlicer failed to initialize. Error: " + ex.Message);
                throw;
            }
        }

        protected override bool GenerateMessage()
        {
            try
            {
                var dcbSlice = new DcbSlice
                {
                    FirstDocument = currentDocumentNumber,
                    DcbCredentials = DcbCredentials,
                    ImageSetId = ImageSetId
                };

                var currentBatchSize = BatchSize;
                var documentsRemaining = numberOfDocumentsInDcb - currentDocumentNumber;
                if (documentsRemaining < BatchSize)
                {
                    currentBatchSize = documentsRemaining;
                }
                dcbSlice.NumberOfDocuments = currentBatchSize;
                currentDocumentNumber += currentBatchSize;

                Send(dcbSlice);

                if (currentDocumentNumber >= numberOfDocumentsInDcb)
                {
                    CreateDatabaseLevelTags();
                    return true;
                }
                LogMessage(true, "DCB documents sliced successfully");
                return false;
            }
            catch (ArgumentOutOfRangeException exRange)
            {
                Tracer.Debug("DcbParser: Opening unsecured DCB database {0}", exRange.Message);
                LogMessage(false, "DcbParser failed to process document. Error: " + Constants.ExportPathFull);
                throw;
            }
            catch (Exception ex)
            {
                LogMessage(false, "DcbSlicer failed to process document. Error: " + ex.Message);
                throw;
            }
        }
      

        private void CreateDatabaseLevelTags()
        {
            // TODO: here is the right place to handle it, but currently we let DcbParcer who handles first document handle DB level tags as well
        }

        private void Send(DcbSlice dcbSlice)
        {
            var message = new PipeMessageEnvelope
            {
                Body = dcbSlice
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(dcbSlice.NumberOfDocuments);
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

        private static DcbOpticonJobBEO PopulateImportRequest(ProfileBEO profiledata)
        {
            var request = new DcbOpticonJobBEO();

            request.JobTypeName = profiledata.ImportTypeName;
            request.JobName = profiledata.ImportJobName;
            request.SysDocId = profiledata.SysDocID;
            request.SysImportType = profiledata.SysImportTypeID;
            // Default settings
            request.StatusBrokerType = BrokerType.Database;
            request.CommitIntervalBrokerType = BrokerType.ConfigFile;
            request.CommitIntervalSettingType = SettingType.CommonSetting;
            //MatterName
            request.MatterName = profiledata.DatasetDetails.Matter.FolderName;
            //Source Path
            request.DcbSourcePath = profiledata.Locations[0];

            //For log.
            // TODO
            //JobLogInfo.AddParameters(Constants.SourcePath, profiledata.Locations[0].ToString());

            //Target DatasetId
            request.TargetDatasetId = profiledata.DatasetDetails.CollectionId;

            //DatasetFolderId
            request.DatasetFolderId = profiledata.DatasetDetails.FolderID;
            //fieldMappinga
            request.FieldMappings = profiledata.FieldMapping;
            //ContentFieldMappings
            request.ContentFields = profiledata.ContentFields;
            request.MatterId = profiledata.DatasetDetails.Matter.FolderID;
            request.IncludeTags = profiledata.IncludeAssociatedTags;
            request.IncludeNotes = profiledata.IncludeNotes;
            request.DcbCredentialList = profiledata.DcbUNPWs;
            request.NativeFilePath = profiledata.NativeFilePathField;

            request.ImageSetName = profiledata.ImageSetName;
            request.ImportImages = profiledata.IsImportImages;
            request.NewImageset = profiledata.IsNewImageSet;
            request.JobName = profiledata.ImportJobName;

            //Populate Family Info
            request.IsImportFamilies = profiledata.IsImportFamilyRelations;
            request.FamilyRelations = profiledata.FamilyRelations;

            return request;
        }

        private void OpenDCB()
        {
            DcbFacade = new DcbFacade();
            if (DcbOpticonJobBEO.DcbCredentialList == null || 0 == DcbOpticonJobBEO.DcbCredentialList.Count)
            {
                OpenDCBWithoutCreds();
            }
            else
            {
                OpenDCBWithCreds();
            }
        }

        private void OpenDCBWithoutCreds()
        {
            Tracer.Debug("DcbSlicer: Opening unsecured DCB database {0}", DcbOpticonJobBEO.DcbSourcePath);
            DcbFacade.OpenDCB(DcbOpticonJobBEO.DcbSourcePath, null, null);
        }

        private void OpenDCBWithCreds()
        {
            //Adding one more empty pair to handle the case where user passes credentials for the 
            //unsecured dcbs
            DcbOpticonJobBEO.DcbCredentialList.Add(Convert.ToString((char) 174));

            //If it is a secured dcb
            foreach (var usernamepasswordpair in DcbOpticonJobBEO.DcbCredentialList)
            {
                if (usernamepasswordpair == null) continue;
                var uidpwd = usernamepasswordpair.Split(new[] {(char) 174});
                var login = uidpwd[0];
                var password = (uidpwd.Length > 1) ? uidpwd[1] : "";
                if (!string.IsNullOrEmpty(password))
                {
                    password = ApplicationConfigurationManager.Decrypt(password,
                        ApplicationConfigurationManager.GetValue(UNPWEncryption, UNPWDataSecurity));
                }

                try
                {
                    Tracer.Debug("DcbSlicer: Opening secured DCB database {0}", DcbOpticonJobBEO.DcbSourcePath);
                    DcbFacade.OpenDCB(DcbOpticonJobBEO.DcbSourcePath, login, password);
                    DcbCredentials = new DcbCredentials {Login = login, Password = password};
                    return;
                }
                catch (Dcb2EvException ex)
                {
                    if (ex.ErrorCode != (int) DcbFacadeErrorCodes.AccessDenied)
                    {
                        throw;
                    }
                    ex.Trace().Swallow();
                }
            }
            var message = String.Format("Tried all login/password pairs for {0} and none worked",
                DcbOpticonJobBEO.DcbSourcePath);
            throw new Dcb2EvException(message, (int) DcbFacadeErrorCodes.AccessDenied);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private string GetImageSetId()
        {
            if (String.IsNullOrEmpty(DcbOpticonJobBEO.ImageSetName))
            {
                return null;
            }

            if (DcbOpticonJobBEO.NewImageset)
            {
                return CreateImageSet();
            }

            var doumentSetBEOList =
                DataSetBO.GetAllDocumentSet(Convert.ToString(DcbOpticonJobBEO.DatasetFolderId));
            if (null == doumentSetBEOList)
            {
                return CreateImageSet();
            }

            var documentSetBEO =
                doumentSetBEOList.Find(o => o.DocumentSetName == DcbOpticonJobBEO.ImageSetName);
            if (null == documentSetBEO)
            {
                return CreateImageSet();
            }

            return documentSetBEO.DocumentSetId;
        }

        private string CreateImageSet()
        {
            var documentSetBEO = new DocumentSetBEO();
            documentSetBEO.DocumentSetId = Guid.NewGuid().ToString();
            documentSetBEO.DocumentSetDescription = DcbOpticonJobBEO.DcbSourcePath;
            documentSetBEO.DatasetId = DcbOpticonJobBEO.DatasetFolderId;
            documentSetBEO.DocumentSetName = DcbOpticonJobBEO.ImageSetName;
            documentSetBEO.DocumentSetTypeId = ImageType;

            var createdByGuid = String.Empty;
            if (null != ProfileBEO && null != ProfileBEO.CreatedBy)
            {
                createdByGuid = ProfileBEO.CreatedBy;
            }

            documentSetBEO.CreatedBy = createdByGuid;
            return CreateDocumentSet(documentSetBEO);
        }

        private string CreateDocumentSet(DocumentSetBEO documentSet)
        {
            string imagesetid;
            try
            {
                documentSet.DocumentSetTypeId = Constants.ImageType;
                documentSet.ParentId = ProfileBEO.DatasetDetails.CollectionId;
                imagesetid = DataSetBO.CreateDocumentSet(documentSet);
            }
            catch (Exception ex)
            {
                Tracer.Error("DcbSlicer: Failed to create image set. {0}", ex);
                imagesetid = String.Empty;
            }
            return imagesetid;
        }

        #region Log

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(bool status, string information)
        {
            var log = new List<JobWorkerLog<DcbParserLogInfo>>();
            var parserLog = new JobWorkerLog<DcbParserLogInfo>();
            parserLog.JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0;
            parserLog.CorrelationId = 0; // TaskId
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

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<DcbParserLogInfo>> log)
        {
            LogPipe.Open();
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        #endregion

        private int numberOfDocumentsInDcb;
        private int currentDocumentNumber;

        private const int BatchSize = 10;

        private ProfileBEO ProfileBEO { get; set; }
        private DcbOpticonJobBEO DcbOpticonJobBEO { get; set; }
        private DcbFacade DcbFacade { get; set; }

        private const string UNPWEncryption = "EncryptionKey";
        private const string UNPWDataSecurity = "Data Security";
        private const string ImageType = "3";

        private DcbCredentials DcbCredentials { get; set; }
        private string ImageSetId { get; set; }
    }
}