#region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="DCBOpticonExportJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish Kumar</author>
//      <description>
//       This class created for DCB import job to access job methods
//      </description>
//      <changelog>
//          <date value="5/4/2011"></date>
//          <date value="02-17-2012">DCB export job fix for 96454 </date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//         <date value="03/14/2014">ADM-REPORTS-003  - Included code changes for New Audit Log</date>
//      </changelog>
// </header>
//----------------------------------------------------------------------------------------- 

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using ClassicServicesLibrary;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Vault.AuditLog;
using LexisNexis.Evolution.Vault.Entities;
using LexisNexis.Library.ConcordanceCLR;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.DataContracts;

namespace LexisNexis.Evolution.BatchJobs.DcbOpticonExports
{
    using Moq;

    /// <summary>
    /// This class represents a  job - Export DCB Job.
    /// </summary>
    public class DCBOpticonExportJob : BaseJob<BaseJobBEO, ExportDCBFileJobTaskBEO>, IDisposable
    {
        private readonly Hashtable htTags = new Hashtable();
        private readonly List<DcbField> htDcbFieldDefs = new List<DcbField>();
        private readonly Hashtable htDocIds = new Hashtable();
        private uint nDocRunningCount;
        private LogCategory category;
        private int nMaxParaSize;
        private int nContentFields;
        private readonly Hashtable htEvFieldCodeToDcbFieldCodeMap = new Hashtable();

        //EV Dataset
        private DatasetBEO dataset;

        #region Delegate

        private delegate void WritePrdImgSetDelegate(string docId, string BinaryReferenceId);

        private delegate void WritenativeFilesDelegate(string docId, string BinaryReferenceId);

        #endregion

        #region Private Fields

        // hold DCB file info
        private readonly Dictionary<string, string> dcbFileinfo;
        //hold helper file info
        private readonly Dictionary<string, List<string>> helperFileinfo;
        //DCB file path
        private string dcbFilePath = string.Empty;
        private readonly BaseJobBEO job; // Job level data 
        private ExportDCBJobDetailBEO request;
        private string createdByGuid = string.Empty;
        private RVWDocumentBEO evDocument;
        private int jobIdentifier;
        private string currentDocumentId = string.Empty;
        private int totalNumberOfDocuments;
        private int totalNumberOfDocumentsFailed;
        private readonly VolumeHelper prImgVolumeHelper;
        private readonly VolumeHelper nativeVolumeHelper;

        private DcbFacade _dcbFacade;
        private int imageKeyFieldCode = -1;
        private readonly List<int> contentKeyFieldCodes = new List<int>();
        private int evContentFieldCode = -1;
        private int nativeKeyFieldCode = -1;

        private HttpContextBase userContext;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public DCBOpticonExportJob()
        {
            job = new BaseJobBEO();
            request = new ExportDCBJobDetailBEO();
            dcbFileinfo = new Dictionary<string, string>();
            helperFileinfo = new Dictionary<string, List<string>>();
            prImgVolumeHelper = new VolumeHelper();
            nativeVolumeHelper = new VolumeHelper();
        }

        #endregion

        #region Job Framework Methods

        /// <summary>
        /// This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy">string</param>
        /// <returns>DCBOpticonJobBEO .</returns>
        protected override BaseJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            try
            {
                //filling properties of the jobparameter
                job.JobId = jobId;
                job.JobRunId = jobRunId;
                job.JobTypeName = "DCBFile";
                job.BootParameters = bootParameters;
                jobIdentifier = jobId;
                category = LogCategory.Job;
                WriteEventLog(jobId + ":" + Constants.Event_Job_Initialize_Start, Constants.EventJobInitializationValue,
                    EventLogEntryType.Information);
                // Default settings
                job.StatusBrokerType = BrokerType.Database;
                job.CommitIntervalBrokerType = BrokerType.ConfigFile;
                job.CommitIntervalSettingType = SettingType.CommonSetting;
                //Service call to get user details
                UserBusinessEntity userBusinessEntity;
                if (!GetUserBusinessEntity(createdBy, out userBusinessEntity))
                {
                    throw new EVException().AddUsrMsg(Constants.UserError);
                }
                createdByGuid = createdBy;

                userContext = CreateUserContext();
                EVHttpContext.CurrentContext = userContext;

                job.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A"))
                    ? userBusinessEntity.UserId
                    : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
                //Deserialize the Boot parameters
                request =
                    (ExportDCBJobDetailBEO) XmlUtility.DeserializeObject(bootParameters, typeof (ExportDCBJobDetailBEO));
                job.JobName = request.JobName;
                // Check folder exists if doesn't create one
                CreateFolder(request.ExportDCBFileInfo.FilePath);
                //create load file 
                if (request.ExportDCBFileInfo.FileName == string.Empty)
                {
                    LogJobException(ErrorCodes.LogFileNameMissing, Constants.LogFileNameMissing, true,
                        Constants.LogFileNameMissing);
                    var ex = new EVJobException(ErrorCodes.LogFileNameMissing) {LogMessge = JobLogInfo};
                    throw ex;
                }
                if (request.ExportDCBFileInfo.FileName.EndsWith(".dcb"))
                    dcbFilePath = request.ExportDCBFileInfo.FilePath + @"\" + request.ExportDCBFileInfo.FileName;
                else
                    dcbFilePath = request.ExportDCBFileInfo.FilePath + @"\" + request.ExportDCBFileInfo.FileName +
                                  ".dcb";

                nMaxParaSize = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MAX_PARA_SIZE"));
                if (nMaxParaSize > Constants.MAX_DCB_PARASIZE || nMaxParaSize < 1)
                {
                    nMaxParaSize = Constants.MAX_DCB_PARASIZE;
                }

                nContentFields = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MAX_CONTENT_FIELDS"));
                if (nContentFields > Constants.MAX_CONTENT_FIELDS || nContentFields < 1)
                {
                    nContentFields = Constants.MAX_CONTENT_FIELDS;
                }

                InitializeTargetDCB(dcbFilePath);
                if (request.ExportDCBTagInfo.IncludeTag)
                    InitializeTagDefinitions(request);
                WriteEventLog(jobId + ":" + Constants.Event_Job_Initialize_Success,
                    Constants.EventJobInitializationValue, EventLogEntryType.Information);
                return job;
            }
            catch (EVException ex)
            {
                var jobException = new EVJobException(ErrorCodes.ProblemInJobInitialization, ex, JobLogInfo);
                throw jobException;
            }
            catch (Exception ex)
            {
                var jobException = new EVJobException(ErrorCodes.ProblemInJobInitialization, ex, JobLogInfo);
                WriteEventLog(jobId + ":" + Constants.EventJobInitializationKey,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Error);
                throw jobException;
            }
        }

        private HttpContextBase CreateUserContext()
        {
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userProp = UserBO.GetUserUsingGuid(createdByGuid);
            var userSession = new UserSessionBEO();
            SetUserSession(createdByGuid, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            return mockContext.Object;
        }

        private static void SetUserSession(string createdUserGuid, UserBusinessEntity userProp,
            UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdUserGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
        }

        /// <summary>
        /// This is the overridden GenerateTasks() method. 
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount">int</param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<ExportDCBFileJobTaskBEO> GenerateTasks(BaseJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            Tasks<ExportDCBFileJobTaskBEO> tasks;
            previouslyCommittedTaskCount = 0;
            var taskNumber = 0;
            WriteEventLog(jobParameters.JobId + ":" + Constants.GenerateTask, Constants.EventJobGenerateTaskValue,
                EventLogEntryType.Information);
            category = LogCategory.Job;
            try
            {
                //Getting tasklist if some of the tasks is already being committed
                tasks = GetTaskList<BaseJobBEO, ExportDCBFileJobTaskBEO>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;
                //// NOTE: Although GetTaskList() might get the committed tasks if the job had run earlier, it is highly recommended
                //// to manually generate the tasks and check if there are any new tasks that may have come up due to some changes in main
                //// parameters.                
                //// If job is running for first time i.e., there were no last committed tasks for this job then manually create tasks.
                if (tasks.Count <= 0)
                {
                    List<string> documents; //list of document reference id's
                    if (!GetDocuments(out documents))
                    {
                        throw new EVException().AddUsrMsg(Constants.DocumentError);
                    }
                    //Generate Tasks
                    if (documents != null)
                    {
                        foreach (
                            var task in
                                documents.Select(document => new ExportDCBFileJobTaskBEO {DocumentId = document}))
                        {
                            taskNumber++;
                            task.TaskNumber = taskNumber;
                            task.TaskComplete = false;
                            task.TaskPercent = 100.0/documents.Count;
                            tasks.Add(task);
                        }
                    }
                }
                else
                {
                    var generatedTasks = (IEnumerable<ExportDCBFileJobTaskBEO>) tasks.GetEnumerator();
                    foreach (
                        var genTask in
                            generatedTasks.Where(genTask => genTask.TaskComplete).Where(genTask => genTask.IsError))
                    {
                        totalNumberOfDocumentsFailed++;
                    }
                }
            }
            catch (EVException ex)
            {
                JobLogInfo.AddParameters(ex.ToUserString());
                var jobException = new EVJobException(ErrorCodes.ProblemInGenerateTasks, ex, JobLogInfo);
                throw jobException;
            }
            catch (Exception ex)
            {
                WriteEventLog(jobParameters.JobId + ":" + Constants.EventGenerateTasks,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Error);
                JobLogInfo.AddParameters(ex.Message);
                var jobException = new EVJobException(ErrorCodes.ProblemInGenerateTasks, ex, JobLogInfo);
                throw jobException;
            }
            totalNumberOfDocuments = tasks.Count;
            return tasks;
        }

        private string Base64Decode(string str)
        {
            var decbuff = Convert.FromBase64String(str);
            return System.Text.Encoding.UTF8.GetString(decbuff);
        }

        /// <summary>
        /// This is the overridden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <returns>Status of the operation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")
        ]
        protected override bool DoAtomicWork(ExportDCBFileJobTaskBEO task, BaseJobBEO jobParameters)
        {
            var isFailed = false;
            currentDocumentId = task.DocumentId;
            dcbFileinfo.Clear();
            helperFileinfo.Clear();
            htEvFieldCodeToDcbFieldCodeMap.Clear();
            try
            {
                //Create LogInfo object to capture task details.
                category = LogCategory.Task;
                evDocument = GetDocumentdata(currentDocumentId);
                var dcbDocument = new Document();
                RVWDocumentFieldBEO documentField;

                if (evDocument == null)
                {
                    var exp = new EVException();
                    exp.AddUsrMsg(string.Format("Failed to fetch the document, id - {0}", currentDocumentId));
                    throw new EVTaskException().SetInnerException(exp)
                        .AddUsrMsg(ErrorCodes.ExportErrorGetEVDocumentFailed);
                }
                // -- get the accession number
                dcbDocument.Accession = Convert.ToUInt32(nDocRunningCount++);

                var fields =
                    htDcbFieldDefs.Select(t => new ClassicServicesLibrary.Field {Name = t.Name, Code = t.Code}).ToList();

                for (var fieldcode = 1; fieldcode <= htDcbFieldDefs.Count; fieldcode++)
                {
                    var dcbFieldDef = htDcbFieldDefs[fieldcode - 1];

                    var dcbFieldSelectionBeo =
                        request.ExportDCBFields.ExportDCBFields.Find(o => o.DCBField == dcbFieldDef.Name);

                    if (dcbFieldSelectionBeo == null)
                        // -- for native/image there is no field in rvwdocumentbeo
                        continue;

                    documentField = evDocument.FieldList.Find(o => o.FieldName == dcbFieldSelectionBeo.DataSetFieldName);

                    if (documentField == null)
                        continue;
                    // -- create a new field to be sent to DCB
                    var field = fields[fieldcode - 1];
                    field.Name = dcbFieldDef.Name;

                    if (documentField.FieldTypeId != Constants.ContentFieldType)
                    {
                        field.Value = dcbFieldDef.Type == Convert.ToSByte('D')
                            ? GetDateFieldValue(dcbFieldDef, documentField)
                            : documentField.FieldValue;

                        if (!htEvFieldCodeToDcbFieldCodeMap.ContainsKey(documentField.FieldId))
                            htEvFieldCodeToDcbFieldCodeMap.Add(documentField.FieldId, field.Code);
                    }
                    // -- set the field code which is important for DCB
                    field.Code = dcbFieldDef.Code;
                }

                var imgAsyncResult = CallPrdImgThread();
                var nativeAsyncResult = CallNativeThrad();

                if (null != imgAsyncResult)
                {
                    imgAsyncResult.AsyncWaitHandle.WaitOne();
                }
                if (null != nativeAsyncResult)
                {
                    nativeAsyncResult.AsyncWaitHandle.WaitOne();
                }

                if (request.ExportDCBFileInfo.ExportDCBFileOption.IncludeNativeFiles
                    && (nativeKeyFieldCode >= 1 && nativeKeyFieldCode <= fields.Count))
                {
                    // -- set the native file path
                    var fld = fields[nativeKeyFieldCode - 1];
                    fld.Value = dcbFileinfo[Constants.NativeFilekey];
                }

                if (request.ExportDCBFileInfo.PriImgSelection != SetSelection.Dataset &&
                    (imageKeyFieldCode >= 1 && imageKeyFieldCode <= fields.Count))
                {
                    // -- set the image set key
                    var fld = fields[imageKeyFieldCode - 1];
                    documentField = evDocument.FieldList.Find(o => o.FieldName == fld.Name);
                    if (documentField != null)
                    {
                        fld.Value = documentField.FieldValue;
                        if (!htEvFieldCodeToDcbFieldCodeMap.ContainsKey(documentField.FieldId))
                            htEvFieldCodeToDcbFieldCodeMap.Add(documentField.FieldId, fld.Code);
                    }
                    else
                    {
                        fld.Value = string.Empty;
                    }
                    dcbDocument.DocImageKey = fld.Value;
                }


                //Set the content Fields
                documentField = evDocument.FieldList.Find(o => o.FieldTypeId == Constants.ContentFieldType);
                var contentFieldIndex = 0;
                if (documentField != null)
                {
                    evContentFieldCode = documentField.FieldId;
                    var contentFieldValue = Base64Decode(documentField.FieldValue);

                    foreach (var contentKeyFieldCode in contentKeyFieldCodes)
                    {
                        if (contentKeyFieldCode >= 1 && contentKeyFieldCode <= fields.Count)
                        {
                            // -- set the content
                            var fld = fields[contentKeyFieldCode - 1];
                            fld.Value = GetContentValue(contentFieldValue, contentFieldIndex);
                        }
                        contentFieldIndex++;
                    }

                    if (!htEvFieldCodeToDcbFieldCodeMap.ContainsKey(documentField.FieldId))
                        htEvFieldCodeToDcbFieldCodeMap.Add(evContentFieldCode, contentKeyFieldCodes);
                }

                htDocIds.Add(currentDocumentId, nDocRunningCount);
                var dcbImages = new List<DcbImage>();
                List<string> images = null;

                if (helperFileinfo.Count != 0)
                {
                    if (dcbFileinfo.ContainsKey(Constants.ImageFilekey))
                        images = helperFileinfo[Constants.ImageFilekey];
                    if (dcbFileinfo.ContainsKey(Constants.PrdImgFilekey))
                        images = helperFileinfo[Constants.PrdImgFilekey];
                }

                var bDocBreak = false;
                if (null != images)
                {
                    foreach (var image in images)
                    {
                        var dcbImage = new DcbImage {ImageData = new CI5EntryItem()};
                        if (!bDocBreak)
                        {
                            dcbImage.ImageData.DocBreak = true;
                            bDocBreak = true;
                            dcbImage.ImageData.RawPages = images.Count;
                        }
                        else
                        {
                            dcbImage.ImageData.DocBreak = false;
                            dcbImage.ImageData.RawPages = 0;
                        }

                        dcbImage.ImageData.FullImagePath = image;
                        dcbImage.ImageData.RawImageFile = Path.GetFileName(image);
                        dcbImage.ImageData.Alias = Path.GetFileNameWithoutExtension(image);
                        dcbImages.Add(dcbImage);
                    }
                }

                // -- set the fields
                dcbDocument.FieldItems = fields; //Fields 
                if (request.ExportDCBTagInfo.IncludeTag)
                    dcbDocument.TagItems = GetDocumentTagItems(currentDocumentId); //Tags

                dcbDocument.Notes = GetDocumentComments(currentDocumentId); //Comments
                dcbDocument.Images = dcbImages; //Images

                // -- append the document
                AppendDcbDocument(dcbDocument);
                var datasetBEO = DataSetBO.GetDataSetDetailForDataSetId(long.Parse(evDocument.DataSetId));
                var lstDocumentIdentifierEntities = new List<DocumentIdentifierEntity>();
                var documentIdentifierEntity = new DocumentIdentifierEntity();
                documentIdentifierEntity.CollectionId = datasetBEO.CollectionId;
                documentIdentifierEntity.CollectionName = datasetBEO.FolderName;
                documentIdentifierEntity.Dcn = evDocument.DocumentControlNumber;
                documentIdentifierEntity.DocumentReferenceId = evDocument.DocumentId;
                lstDocumentIdentifierEntities.Add(documentIdentifierEntity);
                AuditLogFacade.LogDocumentsExported(datasetBEO.Matter.FolderID,
                    lstDocumentIdentifierEntities, jobParameters.JobName
                    );
            }
            catch (EVJobException ex)
            {
                TaskLogInfo.AddParameters(Constants.ErrorDoAtomicWork + "<br/>" + ex.Message);
                TaskLogInfo.StackTrace = ex.Source + "<br/>" + ex.Message + "<br/>" + ex.StackTrace;
                TaskLogInfo.IsError = true;
                isFailed = true;
            }
            catch (EVException ex)
            {
                TaskLogInfo.AddParameters(Constants.ErrorDoAtomicWork + "<br/>" + ex.ToUserString());
                TaskLogInfo.StackTrace = ex.Source + "<br/>" + ex.ToUserString() + "<br/>" + ex.StackTrace;
                TaskLogInfo.IsError = true;
                isFailed = true;
            }
            catch (Exception ex)
            {
                TaskLogInfo.AddParameters(Constants.ErrorDoAtomicWork + "<br/>" + ex.Message);
                TaskLogInfo.StackTrace = ex.Source + "<br/>" + ex.Message + "<br/>" + ex.StackTrace;
                TaskLogInfo.IsError = true;
                isFailed = true;
            }
            evDocument = null;
            return (!isFailed);
        }

        /// <summary>
        /// Perform shutdown activities for a job if any.
        /// </summary>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected override void Shutdown(BaseJobBEO jobParameters)
        {
            WriteEventLog(jobParameters.JobId + ":" + Constants.Shutdown, Constants.ShutdownValue,
                EventLogEntryType.Information);
            try
            {
                FinalizeExport();
                if (request.ExportNotification.SendNotification && request.ExportNotification.UserGroup.Count > 0 &&
                    string.IsNullOrEmpty(request.ExportNotification.NotificationCommand))
                {
                    CustomNotificationMessage = Constants.NotifictioninfoDataset + request.DataSetName +
                                                Constants.NotifictioninfoLocation + request.ExportDCBFileInfo.FilePath +
                                                Constants.NotifictioninfoExport + jobParameters.JobId;
                }
                var importJobLogEntity = new ImportJobLogEntity
                {
                    CreatedBy = jobParameters.JobScheduleCreatedBy,
                    DatasetName = request.DatasetCollectionId,
                    JobID = jobParameters.JobId,
                    JobName = jobParameters.JobName,
                    JobRunId = jobParameters.JobRunId,
                    NoOfDocumentImported = (totalNumberOfDocuments - totalNumberOfDocumentsFailed),
                    NoOfDocumentFailed = totalNumberOfDocumentsFailed
                };
                LogJobMessage(new LogInfo {CustomMessage = Serialize(importJobLogEntity)}, true);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ExportErrorShutdown, Constants.ErrorShutdown, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Handle the failed tasks specific to a job.
        /// </summary>
        /// <param name="failedTasks">List of tasks that failed.</param>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected override void HandleFailedTasks(Tasks<ExportDCBFileJobTaskBEO> failedTasks, BaseJobBEO jobParameters)
        {
            totalNumberOfDocumentsFailed++;
        }

        #endregion

        #region Private Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (_dcbFacade != null)
                    {
                        _dcbFacade.Dispose();
                    }
                }

                disposed = true;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dcbFieldDef"></param>
        /// <param name="documentField"></param>
        /// <returns></returns>
        private string GetDateFieldValue(DcbField dcbFieldDef, RVWDocumentFieldBEO documentField)
        {
            if (String.IsNullOrWhiteSpace(documentField.FieldValue))
            {
                return String.Empty;
            }

            CultureInfo culture;
            if (documentField.FieldType.DataFormat.ToLower().Equals(Constants.DateFormatType1))
                culture = new CultureInfo(Constants.USCulture);
            else if (documentField.FieldType.DataFormat.ToLower().Equals(Constants.DateFormatType2))
                culture = new CultureInfo(Constants.RUCulture);
            else if (documentField.FieldType.DataFormat.ToLower().Equals(Constants.DateFormatType3))
                culture = new CultureInfo(Constants.JACulture);
            else culture = new CultureInfo(Constants.USCulture);

            DateTime dt;
            try
            {
                dt = Convert.ToDateTime(documentField.FieldValue, culture);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                return String.Empty;
            }

            switch (dcbFieldDef.Format)
            {
                case 0: //Date  MMDDYYYY
                    return dt.ToString("MMddyyyy");
                case 1: //Date  YYYYMMDD
                    return dt.ToString("yyyyMMdd");
                case 2: //Date  DDMMYYYY                         
                    return dt.ToString("ddMMyyyy");
                default: //Date  DDMMYYYY
                    return string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dcbFile"></param>
        private void InitializeTargetDCB(string dcbFile)
        {
            _dcbFacade = new DcbFacade();
            dataset = DataSetService.GetDataSet(Convert.ToString(request.DatasetId));
            if (!File.Exists(dcbFile))
            {
                _dcbFacade.CreateDCB(dcbFile);
                var dcbFldsCollection = InitializeFields(request);
                _dcbFacade.SetFields(dcbFldsCollection);
            }
            else //Job pause and run scenario
            {
                _dcbFacade.OpenDCB(dcbFile, null, null);
                InitializeFields(request);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dcbDoc"></param>
        private void AppendDcbDocument(Document dcbDoc)
        {
            try
            {
                _dcbFacade.AppendDocument(dcbDoc);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fieldindex"></param>
        /// <returns></returns>
        private string GetContentValue(string content, int fieldindex)
        {
            if (String.IsNullOrEmpty(content)) return string.Empty;

            if (content.Length <= nMaxParaSize)
            {
                if (fieldindex == 0) return content;
                return string.Empty;
            }

            //Calculate Start & End indices
            var startindex = fieldindex*nMaxParaSize;

            if (startindex > content.Length) return String.Empty;

            var endindex = startindex + nMaxParaSize;
            if (endindex <= content.Length)
                return content.Substring(startindex, nMaxParaSize);
            return content.Substring(startindex, content.Length - startindex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exportDCBJobDetailBEO"></param>
        private DcbFieldsCollection InitializeFields(ExportDCBJobDetailBEO exportDCBJobDetailBEO)
        {
            var fieldidCounter = 1;

            // -- get the dcb field selections that came from the profile/job details
            var dcbfieldSelections = exportDCBJobDetailBEO.ExportDCBFields.ExportDCBFields;

            // -- set the field definition collections that needs to be added to DCB
            var _DcbFieldsCollection = new DcbFieldsCollection();

            foreach (
                var dcbField in
                    dcbfieldSelections.Select(selecteddcbfield => GetDcbField(selecteddcbfield, fieldidCounter)))
            {
                // -- add to the DCB (C++) collection
                _DcbFieldsCollection.Items.Add(dcbField);

                // -- add totthe local list for further reference
                htDcbFieldDefs.Add(dcbField);

                // -- increment so that you can set it later
                fieldidCounter++;
            }

            // -- if native file are included
            if (request.ExportDCBFileInfo.ExportDCBFileOption.IncludeNativeFiles)
            {
                // -- create a paragraph DCB field
                var dcbField = new DcbField
                {
                    Name = request.ExportDCBFileInfo.ExportDCBFileOption.NativeFilePath,
                    Code = fieldidCounter,
                    Type = Convert.ToSByte('P')
                };

                // -- add the same to the collections
                _DcbFieldsCollection.Items.Add(dcbField);
                nativeKeyFieldCode = fieldidCounter;
                htDcbFieldDefs.Add(dcbField);
                fieldidCounter++;
            }

            // -- if image key is included then....
            if (exportDCBJobDetailBEO.ExportDCBFileInfo.PriImgSelection != SetSelection.Dataset)
            {
                // -- create a para text file for imageset
                var dcbField = new DcbField
                {
                    Name = request.ExportDCBFileInfo.ExportDCBFileOption.ImageFieldName,
                    Code = fieldidCounter,
                    Type = Convert.ToSByte('T'),
                    Length = 30,
                    Image = true
                };

                // -- add to collection and set the counter
                _DcbFieldsCollection.Items.Add(dcbField);
                imageKeyFieldCode = fieldidCounter;
                htDcbFieldDefs.Add(dcbField);
                fieldidCounter++;
            }

            if (!string.IsNullOrEmpty(request.ExportDCBFileInfo.ExportDCBFileOption.ContentName))
                AddContentFields(_DcbFieldsCollection, fieldidCounter);

            return _DcbFieldsCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_DcbFieldsCollection"></param>
        /// <param name="fieldidCounter"></param>
        private void AddContentFields(DcbFieldsCollection _DcbFieldsCollection, int fieldidCounter)
        {
            for (var i = 1; i <= nContentFields; i++)
            {
                // -- create a content field
                var dcbField = new DcbField
                {
                    Name =
                        String.Format("{0}{1}",
                            request.ExportDCBFileInfo.ExportDCBFileOption.ContentName,
                            i),
                    Code = fieldidCounter,
                    Type = Convert.ToSByte('P')
                };

                // -- add to collection
                _DcbFieldsCollection.Items.Add(dcbField);
                contentKeyFieldCodes.Add(fieldidCounter);
                htDcbFieldDefs.Add(dcbField);
                fieldidCounter++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exportDcbJobDetailBeo"></param>
        private void InitializeTagDefinitions(ExportDCBJobDetailBEO exportDcbJobDetailBeo)
        {
            var rvwAllTagBEOlist =
                RVWTagService.GetTagsDefinitions(
                    Convert.ToString(exportDcbJobDetailBeo.MatterId, CultureInfo.CurrentCulture),
                    exportDcbJobDetailBeo.DatasetCollectionId, "all", "True");

            // rvwAllTagBEOlist - all tags in the dataset
            var rvwTagBEOlist = rvwAllTagBEOlist.FindAll(o => o.Type == TagType.Tag);
            //int tagCounter = rvwTagBEOlist.Count;
            var tagnames = new List<string>();

            foreach (var rvwTag in rvwTagBEOlist)
            {
                var tagNames = new List<string> {rvwTag.Name};
                var parentTagId = rvwTag.ParentTagId;
                while (parentTagId != 0)
                {
                    var parentTag = rvwAllTagBEOlist.Find(o => o.Id == parentTagId);
                    tagNames.Add(parentTag.Name);
                    parentTagId = parentTag.ParentTagId;
                }

                //Construct the tagname
                var tagname = string.Empty;
                for (var i = (tagNames.Count - 1); i >= 0; i--)
                {
                    tagname = string.Concat(tagname, tagNames[i]);
                    if (i != 0)
                        tagname = string.Concat(tagname, Convert.ToString('»'));
                }

                // request.ExportDCBTagInfo.TagList - all tags requested to export
                if (request.ExportDCBTagInfo.TagList.Contains(Convert.ToString(rvwTag.Id)))
                {
                    htTags.Add(rvwTag.Id, tagname);
                    tagnames.Add(tagname);
                }
            }

            _dcbFacade.SetDatabaseTags(tagnames);
        }


        /// <summary>
        /// get dcb fields
        /// </summary>
        /// <param name="exportDCBFieldselectionBEO"></param>
        /// <param name="fieldid"></param>
        /// <returns></returns>
        private DcbField GetDcbField(ExportDCBFieldselectionBEO exportDCBFieldselectionBEO, int fieldid)
        {
            var dcbField = new DcbField {Name = exportDCBFieldselectionBEO.DCBField, Code = fieldid};

            switch (Convert.ToInt32(exportDCBFieldselectionBEO.FieldType))
            {
                case 0: //Text
                    dcbField.Type = Convert.ToSByte('T');
                    dcbField.Length = 60;
                    break;

                case 1: //Numeric
                    dcbField.Type = Convert.ToSByte('N');

                    var evfield =
                        dataset.DatasetFieldList.Find(o => o.Name.Equals(exportDCBFieldselectionBEO.DataSetFieldName));
                    if (evfield.FieldType.DataFormat.Equals("Currency"))
                        dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.NumericFormat.Currency);
                    else if (evfield.FieldType.DataFormat.Equals("Comma"))
                        dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.NumericFormat.Comma);
                    else if (evfield.FieldType.DataFormat.Equals("ZeroFilled"))
                        dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.NumericFormat.ZeroFill);
                    else
                        dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.NumericFormat.Plain);
                    dcbField.Length = evfield.NumericPrecision;
                    dcbField.Places = Convert.ToSByte(evfield.NumericScale);

                    break;

                case 2: //Date  MMDDYYYY                          
                    dcbField.Type = Convert.ToSByte('D');
                    dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.DateFormat.YYYYMMDD);
                    dcbField.Length = 77; //'M'
                    break;
                case 3: //Date  YYYYMMDD                         
                    dcbField.Type = Convert.ToSByte('D');
                    dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.DateFormat.YYYYMMDD);
                    dcbField.Length = 89; //'Y'
                    break;
                case 4: //Date  DDMMYYYY                         
                    dcbField.Type = Convert.ToSByte('D');
                    dcbField.Format = Convert.ToSByte(ClassicServicesLibrary.DateFormat.YYYYMMDD);
                    dcbField.Length = 68; //'D'
                    break;

                case 5: //Content
                    dcbField.Type = Convert.ToSByte('P');
                    break;
            }

            return dcbField;
        }


        private IEnumerable<DocumentCommentBEO> GetTextLevelTagComments(string documentId)
        {
            var documentComments = new List<DocumentCommentBEO>();

            var documentTags = DocumentService.GetDocumentTags(
                Convert.ToString(request.MatterId),
                request.DatasetCollectionId,
                documentId);

            var textLevelTags = documentTags.FindAll(o => o.Scope == TagScope.Text);

            foreach (var docTag in textLevelTags)
            {
                foreach (var tagtext in docTag.TaggedText)
                {
                    var documentComment = new DocumentCommentBEO {MetadataType = MetadataType.TextLevelComments};

                    var jscomment = new JsonComment
                    {
                        SelectedText = tagtext.SelectedText,
                        IndexInDocument = Convert.ToString(tagtext.IndexInDocument),
                        FieldId = Convert.ToString(tagtext.FieldId)
                    };

                    var serializer = new JavaScriptSerializer();
                    documentComment.Comment.SelectedText = serializer.Serialize(jscomment);
                    documentComment.Comment.Comment = String.Empty;
                    documentComments.Add(documentComment);
                }
            }


            return documentComments;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        private List<NoteRecord2> GetDocumentComments(string documentId)
        {
            const string DocumentScope = "document";
            const string TextScope = "text";
            const string AllScope = "all";
            var scope = string.Empty;
            var linklength = 1;
            var linkoffset = 0;
            var linkFieldCode = 1;

            var noteRecords = new List<NoteRecord2>();

            if (request.ExportDCBTagInfo.IncludeTextDocumentComment) scope = DocumentScope;
            if (request.ExportDCBTagInfo.IncludeTextLevelComment) scope = TextScope;
            if (request.ExportDCBTagInfo.IncludeTextDocumentComment && request.ExportDCBTagInfo.IncludeTextLevelComment)
                scope = AllScope;
            if (!String.IsNullOrEmpty(scope))
            {
                EVHttpContext.CurrentContext = userContext;

                var documentComments = DocumentService.GetDocumentComments(
                    Convert.ToString(request.MatterId, CultureInfo.CurrentCulture),
                    request.DatasetCollectionId,
                    documentId,
                    scope);


                documentComments.AddRange(GetTextLevelTagComments(documentId));

                foreach (var documentComment in documentComments)
                {
                    if (documentComment.MetadataType == MetadataType.TextLevelComments)
                    {
                        documentComment.Comment.SelectedText =
                            HttpUtility.HtmlDecode(documentComment.Comment.SelectedText);
                        var jscomment = DcbOpticonUtil.JsDeserialize<JsonComment>(documentComment.Comment.SelectedText);

                        jscomment.SelectedHtml =
                            jscomment.SelectedText.Replace(string.Format("<{0}\"{1}\">", "BR type=", "nr"), " ");
                        jscomment.SelectedHtml =
                            jscomment.SelectedText.Replace(string.Format("<{0}\"{1}\">", "BR type=", "n"), " ");
                        jscomment.SelectedHtml =
                            jscomment.SelectedText.Replace(string.Format("<{0}\"{1}\">", "BR type=", "r"), " ");
                        jscomment.SelectedHtml =
                            jscomment.SelectedText.Replace(string.Format("<{0}\"{1}\">", "BR type=", "rn"), " ");

                        linklength = Convert.ToInt32(jscomment.SelectedHtml.Length);
                        linkoffset = Convert.ToInt32(jscomment.IndexInDocument);
                        var linkEvFieldCode = Convert.ToInt32(jscomment.FieldId);
                        if (htEvFieldCodeToDcbFieldCodeMap.ContainsKey(linkEvFieldCode))
                        {
                            if (evContentFieldCode == Convert.ToInt32(jscomment.FieldId))
                            {
                                var contentFieldCodes = (List<int>) htEvFieldCodeToDcbFieldCodeMap[linkEvFieldCode];

                                //Calculate the Field Code
                                var contentFieldIndex = linkoffset/nMaxParaSize;
                                linkFieldCode = contentFieldCodes[contentFieldIndex];

                                //Calculate the Start Index
                                if (contentFieldIndex > 0)
                                    linkoffset = linkoffset - (contentFieldIndex*nMaxParaSize);

                                //Calculate the Length
                                if (linklength + linkoffset > nMaxParaSize) linklength = nMaxParaSize - linkoffset;
                            }
                            else
                            {
                                linkFieldCode = (int) htEvFieldCodeToDcbFieldCodeMap[linkEvFieldCode];
                            }
                        }
                    }

                    var noterecord = new NoteRecord2
                    {
                        Text = documentComment.Comment.Comment,
                        LinkFieldCode = linkFieldCode,
                        LinkLength = linklength,
                        LinkOffset = linkoffset,
                        Author = "System",
                        Attachment = String.Empty,
                        AttachmentType = -1,
                        AutoAttachment = -1,
                        Parent = String.Empty
                    };
                    noteRecords.Add(noterecord);
                }
            }
            return noteRecords;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        private List<string> GetDocumentTagItems(string documentId)
        {
            EVHttpContext.CurrentContext = userContext;
            var documentTags = DocumentService.GetDocumentTags(
                Convert.ToString(request.MatterId),
                request.DatasetCollectionId,
                documentId);

            return
                documentTags.Select(dcoTag => (string) htTags[dcoTag.TagId])
                    .Where(tagname => !String.IsNullOrEmpty(tagname))
                    .ToList();
        }


        /// <summary>
        /// 
        /// </summary>
        private void FinalizeExport()
        {
            _dcbFacade.FinalizeAddingDocuments();
            _dcbFacade.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        private IAsyncResult CallPrdImgThread()
        {
            //By load Dictionary count we determine the whether thread is over
            //Check condition for prodimg set else update load Dictionary with empty
            if (request.ExportDCBFileInfo.PriImgSelection == SetSelection.ProductionSet ||
                request.ExportDCBFileInfo.PriImgSelection == SetSelection.ImageSet)
            {
                var binaryRefId = request.ExportDCBFileInfo.PriImgSelection == SetSelection.ProductionSet
                    ? Constants.ProductionFileType
                    : Constants.ImagesFileType;
                WritePrdImgSetDelegate writeProductionSetDelegate = WritePrdImgSet;
                return writeProductionSetDelegate.BeginInvoke(currentDocumentId, binaryRefId, null, this);
            }
            dcbFileinfo.Add(Constants.PrdImgFilekey, string.Empty);
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        private IAsyncResult CallNativeThrad()
        {
            //Check condition for native file else update load Dictionary with empty
            if (request.ExportDCBFileInfo.ExportDCBFileOption.IncludeNativeFiles)
            {
                WritenativeFilesDelegate writenativeFilesDelegate = WriteNativeFiles;
                return writenativeFilesDelegate.BeginInvoke(currentDocumentId, Constants.NativeFileType, null, this);
            }
            dcbFileinfo.Add(Constants.NativeFilekey, string.Empty);
            return null;
        }

        #endregion

        #region SERVICE CALL CODE TO FETCH DATA(Rest client)

        /// <summary>
        /// To get the User Business Entity
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="returnObject"></param>
        /// <returns></returns>
        private bool GetUserBusinessEntity(string userGuid, out UserBusinessEntity returnObject)
        {
            returnObject = new UserBusinessEntity();
            try
            {
                returnObject = UserBO.GetUserUsingGuid(userGuid);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingUserDetails, Constants.ErrorGettingUserDetails, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace);
                return false;
            }

            //Check if the user identifier exists
            if (returnObject.UserId == null)
            {
                LogJobException(ErrorCodes.ErrorGettingUserDetails, Constants.ErrorGettingUserDetails, true,
                    string.Empty);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get document binay
        /// </summary>
        /// <param name="matterId">matter id</param>
        /// <param name="collectionId">collection Id</param>
        /// <param name="documentId">document Id</param>
        /// <param name="documentType">document Type</param>
        /// <param name="referenceId">reference Id</param>
        /// <returns>RVWDocumentBEO</returns>
        private RVWDocumentBEO GetDocumentBinary(string matterId, string collectionId, string documentId,
            string documentType, string referenceId)
        {
            RVWDocumentBEO rVWDocumentBEO;
            try
            {
                EVHttpContext.CurrentContext = userContext;
                rVWDocumentBEO = DocumentService.GetDocumentBinary(matterId, collectionId, documentId, documentType,
                    referenceId, "false", string.Empty);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentBinary, Constants.ErrorGettingDocumentBinary, false,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace);
                return null;
            }
            return rVWDocumentBEO;
        }

        /// <summary>
        /// Gets all meta data for a given document id(fileds value and content)
        /// </summary>
        /// <param name="matterId"></param>
        /// <param name="userId"></param>
        /// <param name="collectionId"></param>
        /// <param name="documentId"></param>
        /// <param name="isIncludehiddenField"></param>
        /// <returns>collections of document data </returns>        
        public RVWDocumentBEO GetDocumentData(string matterId, string collectionId, string documentId, string userId,
            string isIncludehiddenField)
        {
            RVWDocumentBEO toREturn = null;
            var uri = string.Empty;
            try
            {
                EVHttpContext.CurrentContext = userContext;
                toREturn = DocumentService.GetDocumentData(matterId, collectionId, documentId, userId,
                    isIncludehiddenField);
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ErrorGettingDocumentMetaData, Constants.ErrorGettingDocumentMetaData, false,
                    uri + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace);
            }
            return toREturn;
        }

        #endregion

        #region  Helper fuctions

        /// <summary>
        /// Log Job Exception
        /// </summary>
        /// <param name="errorCode">errorCode</param>
        /// <param name="userFriendlyError">userFriendlyError</param>
        ///<param name="isJobError">isJobError</param>
        /// <param name="customMessage">customMessage</param>
        /// <returns>success or failure</returns>
        public void LogJobException(string errorCode, string userFriendlyError, bool isJobError, string customMessage)
        {
            WriteEventLog(jobIdentifier + ":" + userFriendlyError, customMessage, EventLogEntryType.Error);

            if (category == LogCategory.Job)
            {
                JobLogInfo.AddParameters(errorCode + ":" + userFriendlyError + ":" + customMessage);
                JobLogInfo.IsError = isJobError;
            }
            else
            {
                TaskLogInfo.AddParameters(errorCode + ":" + userFriendlyError + ":" + customMessage);
                TaskLogInfo.IsError = isJobError;
            }
        }

        #endregion

        #region Get documents and document data(Service call)

        /// <summary>
        /// Get the document metadata info
        /// </summary>
        /// <param name="DocumentId"></param>
        /// <returns>documentBEO</returns>
        private RVWDocumentBEO GetDocumentdata(string DocumentId)
        {
            RVWDocumentBEO documentBEO = null;
            try
            {
                documentBEO = GetDocumentData(request.MatterId, request.DatasetCollectionId, DocumentId, createdByGuid,
                    "false");
                if (documentBEO != null)
                {
                    // get the allowed fields for the user to view and arrange it in the ordinal position
                    var allowedFields = (from field in documentBEO.FieldList
                        where field.IsHiddenField == false && field.FieldTypeId != Constants.ReasonFieldType
                              && field.FieldTypeId != Constants.DescriptionFieldType
                        select field).OrderBy
                        (f => f.OrdinalPosition).ToList();

                    documentBEO.FieldList.Clear();
                    allowedFields.SafeForEach(f => documentBEO.FieldList.Add(f));
                }
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ExportErrorGettingDocumentsMetaData, Constants.ErrorGettingDocumentMetadata,
                    false, ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace);
            }
            return documentBEO;
        }

        /// <summary>
        /// Get the documents that are to be included in the privilege log
        /// </summary>
        /// <param name="documentsSelected"></param>
        /// <returns>success or failure</returns>
        private bool GetDocuments(out List<string> documentsSelected)
        {
            documentsSelected = new List<string>();

            var reviewsetID = string.Empty;
            const bool isIncludeFamily = false;
            var isIncludeConceptSearch = false;
            try
            {
                var searchQuery = string.Empty;

                if (request.ExportDCBFileInfo != null)
                {
                    if (request.ExportDCBFileInfo.PriImgSelection == SetSelection.ProductionSet)
                    {
                        searchQuery = EVSystemFields.ProductionSets.ToLower() + ":" + "\"" +
                                      request.ExportDCBFileInfo.ProdImgCollectionId + "\"";
                    }
                    else if (request.ExportDCBFileInfo.PriImgSelection == SetSelection.ImageSet)
                    {
                        searchQuery = EVSystemFields.ImageSets.ToLower() + ":" + "\"" +
                                      request.ExportDCBFileInfo.ProdImgCollectionId + "\"";
                    }

                    if (request.ExportDCBFileInfo.PriImgSelection != SetSelection.Dataset &&
                        request.ExportDCBFileInfo.DocumentSelection != DocumentSelection.AllDocuments)
                    {
                        searchQuery += Constants.SearchAndKey;
                    }


                    switch (request.ExportDCBFileInfo.DocumentSelection)
                    {
                        case DocumentSelection.SavedQuery:
                            //Code added by sai for saved search query option.
                            var savedQueryText = request.ExportDCBFileInfo.SavedqueryId;
                            EVHttpContext.CurrentContext = userContext;
                            var savedSearchlist = ReviewerSearchService.GetAllSavedSearch
                                (Constants.SSIndex, int.MaxValue.ToString(CultureInfo.InvariantCulture),
                                    Constants.SSColumn, Constants.SavedSearchSortOrder);
                            var matchingSavedSearch =
                                savedSearchlist.FirstOrDefault(x => x.SavedSearchId.ToString().Equals(savedQueryText));
                            if (matchingSavedSearch != null)
                            {
                                searchQuery += matchingSavedSearch.DocumentQuery.QueryObject.DisplayQuery;
                                reviewsetID = matchingSavedSearch.DocumentQuery.QueryObject.ReviewsetId;
                                isIncludeConceptSearch =
                                    matchingSavedSearch.DocumentQuery.QueryObject.IsConceptSearchEnabled;
                            }
                            break;
                        case DocumentSelection.Tag:

                            searchQuery += EVSearchSyntax.Tag + "\"" + request.ExportDCBFileInfo.TagId + "\"";
                            break;
                        default:
                            //This condition will not occur in normal conditions.Either tag or saved search will be selected.
                            searchQuery += string.Empty;
                            break;
                    }

                    var documentQueryEntity = new DocumentQueryEntity
                    {
                        QueryObject = new SearchQueryEntity
                        {
                            ReviewsetId = reviewsetID,
                            MatterId = Convert.ToInt32(request.MatterId),
                            IsConceptSearchEnabled = isIncludeConceptSearch,
                            DatasetId = Convert.ToInt32(request.DatasetId)
                        }
                    };

                    documentQueryEntity.QueryObject.QueryList.Add(new Query(searchQuery));
                    documentQueryEntity.SortFields.Add(new Sort {SortBy = Constants.Relevance});
                    documentQueryEntity.IgnoreDocumentSnippet = true;
                    documentQueryEntity.TransactionName = "DCBOpticonExportJob - GetDocuments";
                    var searchResult = JobSearchHandler.GetAllDocuments(documentQueryEntity, isIncludeFamily);
                    if (searchResult != null &&
                        (true & searchResult.ResultDocuments != null && searchResult.ResultDocuments.Count > 0))
                    {
                        documentsSelected.AddRange(searchResult.ResultDocuments.Select(document => document.DocumentID));
                    }
                }
                else
                {
                    LogJobException(ErrorCodes.ExportErrorGettingDocumentFilterOptionNotSet,
                        Constants.ErrorGettingDocumentFilterOptionNotSet, true,
                        Constants.ErrorGettingDocumentFilterOptionNotSet);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogJobException(ErrorCodes.ExportErrorGettingDocuments, Constants.ErrorGettingDocuments, true,
                    ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace);
                return false;
            }

            return true;
        }

        #endregion GetDocuments

        /// <summary>
        /// Serializes give object to specified type
        /// </summary>
        /// <returns>
        /// Serialized XML as a string
        /// </returns>
        public static string Serialize<T>(T obj) where T : class
        {
            string xmlString = null; // this will hold the serialize string
            if (obj != null)
            {
                var sw = new StringWriter();
                // it will be used to supress xml declaretion from xml string
                var writerSettings = new XmlWriterSettings {OmitXmlDeclaration = true, CloseOutput = true};
                using (var xmlWriter = XmlWriter.Create(sw, writerSettings))
                {
                    var xs = new XmlSerializer(typeof (T)); //Serailzer
                    xs.Serialize(xmlWriter, obj); // Serializing 
                    xmlString = sw.ToString();
                }
            }
            return xmlString;
        }


        /// <summary>  
        /// Function to write Production set or Image set files
        /// </summary>  
        /// <param name="documentId"></param>
        /// <param name="binaryRefId"></param>
        /// <returns></returns>
        private void WritePrdImgSet(string documentId, string binaryRefId)
        {
            WriteHelper(documentId, Constants.PrdImgFilekey, Constants.PrdImg, binaryRefId, prImgVolumeHelper);
        }

        /// <summary>  
        /// Function to write Native set files
        /// </summary>  
        /// <param name="documentId"></param>
        /// <param name="binaryRefId"></param>
        /// <returns></returns>
        private void WriteNativeFiles(string documentId, string binaryRefId)
        {
            WriteHelper(documentId, Constants.NativeFilekey, Constants.Native, binaryRefId, nativeVolumeHelper);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        /// <param name="eventLogEntryType"></param>
        private static void WriteEventLog(string source, string message, EventLogEntryType eventLogEntryType)
        {
            EvLog.WriteEntry(source, message, eventLogEntryType);
        }

        #region Write Helpers

        /// <summary>  
        /// Function to write files in to appropriate folders
        /// </summary>
        /// <param name="key"></param>
        /// <param name="from">productionset or imageset </param>
        /// <param name="documentId"></param> 
        /// <param name="binaryTypeId"></param> 
        /// <param name="volumeHelper"></param> 
        /// <returns></returns>
        private void WriteHelper(string documentId, string key, string from, string binaryTypeId,
            VolumeHelper volumeHelper)
        {
            EVHttpContext.CurrentContext = userContext;

            volumeHelper.From = from;
            var pageCount = 0;
            //int filecounter = 0;
            var multifulFilesFlag = false;
            var listHelper = new List<string>();
            if (from == Constants.PrdImg)
            {
                multifulFilesFlag = true;
            }
            try
            {
                var collcetionId = @from == Constants.Native
                    ? request.DatasetCollectionId
                    : request.ExportDCBFileInfo.ProdImgCollectionId;
                var documentMasterInfo = GetDocumentBinary(request.MatterId, collcetionId, documentId, binaryTypeId,
                    string.Empty);

                if (null == documentMasterInfo)
                {
                    if (!dcbFileinfo.ContainsKey(key))
                        dcbFileinfo.Add(key, String.Empty);
                    return;
                }
                var documentInfo = documentMasterInfo.DocumentBinary;
                foreach (var rVWExternalFile in documentInfo.FileList)
                {
                    if (rVWExternalFile.Path.Contains(Constants.QuestionMark))
                    {
                        rVWExternalFile.Path = rVWExternalFile.Path.Substring(0,
                            rVWExternalFile.Path.LastIndexOf(Constants.QuestionMark));
                    }

                    var fileName = Path.GetFileName(rVWExternalFile.Path);
                    if (from.Equals(Constants.PrdImg))
                    {
                        var listRvw =
                            evDocument.FieldList.Where(
                                o => o.FieldName == request.ExportDCBFileInfo.ExportDCBFileOption.ImageFieldName)
                                .ToList();
                        if (listRvw.Count > 0 && !string.IsNullOrEmpty(listRvw[0].FieldValue))
                        {
                            var regEx = new Regex(Constants.FileSpecialCharactersRegex);
                            var fieldValue = regEx.Replace(listRvw[0].FieldValue, "");

                            if (documentInfo.FileList.Count > 1)
                            {
                                pageCount++;
                                fileName = fieldValue + "_" + pageCount.ToString("D4") +
                                           Path.GetExtension(rVWExternalFile.Path);
                            }
                            else
                            {
                                fileName = fieldValue + Path.GetExtension(rVWExternalFile.Path);
                            }
                        }
                    }
                    var folderPath = FindFolderPath(volumeHelper);
                    var filePath = folderPath + @"\" + fileName;
                    if (!File.Exists(filePath))
                        File.Copy(rVWExternalFile.Path, filePath);
                    if (multifulFilesFlag)
                    {
                        listHelper.Add(filePath);
                    }
                    else
                    {
                        if (!dcbFileinfo.ContainsKey(key))
                            dcbFileinfo.Add(key, filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                multifulFilesFlag = false;
                if (!dcbFileinfo.ContainsKey(key))
                {
                    dcbFileinfo.Add(key, String.Empty);
                }

                LogJobException(ErrorCodes.ErrorWrintingBinaryFile,
                    Constants.WriteHelperError + from + Constants.WriteHelperErrorMsg + ex.Message, false,
                    Constants.ErrorWritingBinaryFile);
            }

            if (multifulFilesFlag)
            {
                helperFileinfo.Add(key, listHelper);
                if (!dcbFileinfo.ContainsKey(key))
                {
                    dcbFileinfo.Add(key, String.Empty);
                }
            }
        }

        /// <summary>  
        /// Function to apply volume condtions inorder to find folder path for writing file
        /// </summary>  
        /// <param name="volHelper">Folder path</param>
        /// <returns>string</returns>
        private string FindFolderPath(VolumeHelper volHelper)
        {
            var filePath = new List<string>();
            filePath.Insert(0, request.ExportDCBFileInfo.FilePath);
            var i = 1;
            double dirSize = 0;
            if (request.ExportVolume.VolumeFolders)
            {
                if (request.ExportVolume.IsIncreaseVolumeFolder)
                {
                    if (volHelper.Fincreament == 0)
                    {
                        filePath.Insert(i++,
                            @"\" + request.ExportVolume.VolumeFoldersName + request.ExportVolume.IncreaseFolderNmae);
                        volHelper.Fincreament = Convert.ToInt32(request.ExportVolume.IncreaseFolderNmae);
                    }
                    else
                    {
                        filePath.Insert(i++, volHelper.VolumeName);
                    }
                    var dirPath = filePath.Aggregate((x, y) => x + "" + y);
                    if (Directory.Exists(dirPath))
                    {
                        var dir = new DirectoryInfo(dirPath);
                        var fileinfo = dir.GetFiles("*.*", SearchOption.AllDirectories);
                        dirSize = fileinfo.Aggregate(dirSize, (current, file) => current + file.Length);
                        if (Convert.ToInt32(request.ExportVolume.MaxNumberFilesInFolder) <= fileinfo.Length ||
                            dirSize >= Convert.ToInt32(request.ExportVolume.MemorySize)*1024*1024)
                        {
                            filePath.RemoveAt(1);
                            volHelper.Fincreament = volHelper.Fincreament +
                                                    Convert.ToInt32(request.ExportVolume.IncreaseFolderNmae);
                            filePath.Insert(1,
                                @"\" + request.ExportVolume.VolumeFoldersName + volHelper.Fincreament.ToString("D3"));
                        }
                    }
                }
                else
                {
                    filePath.Insert(i++,
                        @"\" + request.ExportVolume.VolumeFoldersName + request.ExportVolume.IncreaseFolderNmae);
                }
            }

            if (request.ExportVolume.CreateseparateFolderImageNative)
            {
                filePath.Insert(i++, @"\");
                if (volHelper.From == Constants.PrdImg)
                {
                    filePath.Insert(i,
                        request.ExportVolume.ImageFileName != string.Empty
                            ? request.ExportVolume.ImageFileName
                            : Constants.PrdImg);
                }
                else if (volHelper.From == Constants.Text)
                {
                    filePath.Insert(i,
                        request.ExportVolume.TextFileName != string.Empty
                            ? request.ExportVolume.TextFileName
                            : Constants.Text);
                }
                else if (volHelper.From == Constants.Native)
                {
                    filePath.Insert(i,
                        request.ExportVolume.NativeFileName != string.Empty
                            ? request.ExportVolume.NativeFileName
                            : Constants.Native);
                }
            }
            CreateFolder(filePath.Aggregate((x, y) => x + "" + y));
            if (request.ExportVolume.VolumeFolders)
            {
                volHelper.VolumeName = filePath[1];
            }
            return filePath.Aggregate((x, y) => x + "" + y);
        }

        #endregion

        #region Create File or Folder, Write Into Files Helpers

        /// <summary>  
        /// Function to create folder
        /// </summary>  
        /// <param name="path">Folder path</param>
        /// <returns></returns>
        private void CreateFolder(string path)
        {
            var lockObject = new object();
            // As writing file multi threaded, Locking the foleder creation 
            lock (lockObject)
            {
                // Check foleder exists if doesn't create one
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }
                catch (Exception)
                {
                    LogJobException(ErrorCodes.ExportErrorAccessFolder,
                        Constants.ErrorAccessFolder + Constants.FilePath + path, true, Constants.ErrorAccessFolder);
                    throw;
                }
            }
        }

        #endregion
    }
}