#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="DeduplicationJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Deepthi Bitra</author>
//      <description>
//          This file is used to handle Deduplication related batch jobs 
//          which can be used to delete and group duplicate documents
//      </description>
//      <changelog>
//          <date value="1/4/2011">Created</date>
//          <date value="2/8/2012">Fix for bug 93868</date>
//          <date value="6/5/2012">Fix for bug 100692 & 100624 - babugx</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/11/2012">TaskFix#102268</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//          <date value="3/30/2014">CNEV 3.0 - Requirement Bug #165088 - Document Delete NFR and functional fix : babugx</date>
//          <date value="5/2/2014">CNEV 3.0 - Bug# 168471,168515 - Deduplication and Billing report fix : babugx</date>
//          <date value="5/2/2014">CNEV 3.0 - Bug# 168471 - The deleted document count is displaying wrongly in deduplication job log : babugx</date>
//          <date value="6/12/2014">CNEV 3.0 - Bug# 168471 - Rework for dedup functional & perf fix : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Vault;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LexisNexis.Evolution.BatchJobs.Deduplication
{
    [Serializable]
    public class DeduplicationJob : BaseJob<DeduplicationJobBEO, DeduplicationJobTaskBEO>
    {
        #region Private Variables

        private DeduplicationJobTaskBEO _task;
        private Tasks<DeduplicationJobTaskBEO> _tasks;
        private int _taskNumber;

        #region For Log

        private string _tempHashInfo = string.Empty;
        private List<string> _lsBatesNo;
        private int _intCount = 1;
        private bool _isNew;
        private readonly List<AffectedDocument> _lsAffectedDocument = new List<AffectedDocument>();
        private AffectedDocument _objAffectedDocument;
        private IVault _vault;
        private string _matterId = string.Empty;
        private string _jobName = string.Empty;
        private int _jobid;
        private string _jobrunId = string.Empty;
        private string _actionType = string.Empty;
        private List<AffectedDocument> _affectedDocList;
        private string _createdByUserName = string.Empty;
        private int _successCount;
        private int _failureCount;

        #endregion

        #endregion

        #region Public Variables

        /// <summary>
        /// This property helps to log duplicate documents related data.
        /// </summary>
        public DeduplicationJobLogBEO LogDetails { get; set; }

        /// <summary>
        /// This property holds Dataset collection Id
        /// </summary>
        public Guid DataSetCollectionId { get; set; }

        /// <summary>
        /// This property holds tasks count
        /// </summary>
        public int TaskCount { get; set; }

        /// <summary>
        /// This property holds failed task count
        /// </summary>
        public int FailedTask { get; set; }

        /// <summary>
        /// This property holds duplicate documents count
        /// </summary>
        public int NoOfDuplicate { get; set; }

        /// <summary>
        /// This property holds total documents inside a dataset
        /// </summary>
        public int NoOfDocumentProcessed { get; set; }

        #endregion

        #region Initialize

        /// <summary>
        ///  Override version of Initialize
        /// </summary>
        /// <param name="jobId">Job Id</param>
        /// <param name="jobRunId">Run Id</param>
        /// <param name="bootParameters">Parameters xml</param>
        /// <param name="createdByGuid">User Guid</param>
        /// <returns>Job Business Object</returns>
        protected override DeduplicationJobBEO Initialize(int jobId, int jobRunId, string bootParameters,
            string createdByGuid)
        {
            DeduplicationJobBEO jobBeo = null;
            try
            {
                jobBeo = new DeduplicationJobBEO {JobId = jobId, JobName = Constants.JobName, JobRunId = jobRunId};
                var userEntityOfJobOwner = UserBO.GetUserUsingGuid(createdByGuid);
                jobBeo.JobScheduleCreatedBy = (userEntityOfJobOwner.DomainName.Equals("N/A"))
                    ? userEntityOfJobOwner.UserId
                    : userEntityOfJobOwner.DomainName + "\\" + userEntityOfJobOwner.UserId;

                jobBeo.JobTypeName = Constants.JobTypeName;
                _createdByUserName = jobBeo.JobScheduleCreatedBy;
                EvLog.WriteEntry(jobId.ToString(CultureInfo.InvariantCulture), Constants.EVENT_JOB_INITIALIZATION_VALUE,
                    EventLogEntryType.Information);
                jobBeo.StatusBrokerType = BrokerType.Database;
                jobBeo.CommitIntervalBrokerType = BrokerType.ConfigFile;
                jobBeo.CommitIntervalSettingType = SettingType.CommonSetting;
                var deduplicationProfileBeo = GetDeduplicationProfileBEO(bootParameters);
                if (deduplicationProfileBeo != null)
                {
                    jobBeo.DatasetInfo.Clear();
                    deduplicationProfileBeo.DatasetInfo.SafeForEach(o => jobBeo.DatasetInfo.Add(o));
                    jobBeo.CompareType = deduplicationProfileBeo.CompareType;
                    jobBeo.Algorithm = deduplicationProfileBeo.Algorithm;
                    jobBeo.IsDelete = deduplicationProfileBeo.IsDelete && deduplicationProfileBeo.IsDelete;

                    jobBeo.IsGroup = deduplicationProfileBeo.IsGroup && deduplicationProfileBeo.IsGroup;
                }
                else
                {
                    EvLog.WriteEntry(jobId.ToString(CultureInfo.InvariantCulture),
                        Constants.EVENT_JOB_INITIALIZATION_KEY, EventLogEntryType.Information);
                    JobLogInfo.AddParameters(Constants.EVENT_JOB_INITIALIZATION_KEY);
                }
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(jobId + Constants.EVENT_INITIALIZATION_EXCEPTION_VALUE, exp.Message,
                    EventLogEntryType.Error);
                LogException(jobId, exp, Constants.EVENT_INITIALIZATION_EXCEPTION_VALUE, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInJobInitialization);
            }
            return jobBeo;
        }

        #endregion

        #region GenerateTasks

        /// <summary>
        /// This method helps to create multiple tasks and task generation is based on no. of duplicate documents in the collection 
        /// </summary>
        protected override Tasks<DeduplicationJobTaskBEO> GenerateTasks(DeduplicationJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            #region Pre-condition asserts

            jobParameters.ShouldNotBe(null);

            #endregion

            previouslyCommittedTaskCount = 0;
            try
            {
                _tasks = GetTaskList<DeduplicationJobBEO, DeduplicationJobTaskBEO>(jobParameters);
                previouslyCommittedTaskCount = _tasks.Count;
                EvLog.WriteEntry(jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                    Constants.EVENT_JOB_GENERATETASK_VALUE, EventLogEntryType.Information);
                _jobid = jobParameters.JobId;
                _jobName = jobParameters.JobName;
                _jobrunId = jobParameters.JobRunId.ToString(CultureInfo.InvariantCulture);
                _jobid.ShouldBeGreaterThan(0);
                _jobName.ShouldNotBe(null);
                _jobrunId.ShouldNotBe(null);
                _jobrunId.ShouldNotBe(string.Empty);
                //Get DocumentsHashValues count>0
                var lsDocumentHash = GetDocumentHashValues(jobParameters.DatasetInfo, jobParameters.Algorithm,
                    jobParameters.CompareType);
                if (lsDocumentHash != null && lsDocumentHash.Count > 0)
                {
                    lsDocumentHash.RemoveAll(doc => string.IsNullOrEmpty(doc.HashValue));
                }
                //Get Duplicate docs excluding original documents
                var lsdupDoc = GetDuplicatesDocuments(lsDocumentHash);

                if (_tasks.Count <= 0)
                {
                    if (lsDocumentHash != null && lsDocumentHash.Count > 0)
                    {
                        if (jobParameters.IsDelete)
                        {
                            GeneratetasksForDeleteOperation(lsDocumentHash, lsdupDoc);
                        }
                        if (jobParameters.IsGroup)
                        {
                            var strDuplicateFiledVal = GetDuplicateFieldValue(jobParameters.Algorithm);
                            GeneratetasksForGroupOperation(lsDocumentHash, strDuplicateFiledVal);
                        }
                    }
                    else
                    {
                        NoOfDuplicate = 0;
                        EvLog.WriteEntry(jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                            Constants.GenerateTaskNoDuplicates, EventLogEntryType.Information);
                        JobLogInfo.AddParameters(Constants.GenerateTaskNoDuplicates);
                    }
                    LogDetails = CommonLogInfo();
                }
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(jobParameters.JobId + Constants.EVENT_GENERATE_TASKS_EXCEPTION_VALUE, exp.Message,
                    EventLogEntryType.Error);
                LogException(jobParameters.JobId, exp, Constants.EVENT_GENERATE_TASKS_EXCEPTION_VALUE, LogCategory.Job,
                    string.Empty, ErrorCodes.ProblemInGenerateTasks);
            }

            #region Post-condition asserts

            jobParameters.ShouldNotBe(null);

            #endregion

            return _tasks;
        }

        #endregion

        #region GeneratetasksForDeleteOperation

        /// <summary>
        /// This method helps to generate tasks for performing delete duplicate documents from vault and search sub-system
        /// </summary>
        private void GeneratetasksForDeleteOperation(IEnumerable<DocumentHashMapEntity> lsDocumentHash,
            List<string> lsdupDoc)
        {
            var dupDocToDelete = (from docHash in lsDocumentHash
                where lsdupDoc.Contains(docHash.DocumentReferenceId + docHash.CollectionId)
                select docHash).GroupBy(d => d.CollectionId);

            if (dupDocToDelete != null && dupDocToDelete.Any())
            {
                foreach (var docGroup in dupDocToDelete)
                {
                    var duplicateBatches = docGroup.Batch(100);
                    foreach (var batch in duplicateBatches)
                    {
                        _task = new DeduplicationJobTaskBEO {IsDelete = true};
                        _taskNumber++;
                        _task.TaskNumber = _taskNumber;
                        _task.MatterId = _matterId;
                        _task.CollectionId = docGroup.Key.ToString();
                        _task.DeleteDocumentList.AddRange(batch.Select(d => d.DocumentReferenceId));
                        //_task.DocumentReferenceId = documentHash.DocumentReferenceId;
                        //_task.HashValue = documentHash.HashValue;
                        _task.TaskComplete = false;
                        //_task.TaskPercent = 100.0 / lsdupDoc.Count;
                        _task.TaskKey = string.Format("Task # {0} - JobRunId : {1}", _task.TaskNumber, _jobrunId);
                        _tasks.Add(_task);
                    }
                }
            }

            _tasks.SafeForEach(t => t.TaskPercent = (100.0/_tasks.Count));

            var nonDupDocs = (from docHash in lsDocumentHash
                where !lsdupDoc.Contains(docHash.DocumentReferenceId + docHash.CollectionId)
                select docHash);
            if (nonDupDocs != null && nonDupDocs.Any())
            {
                foreach (var docHash in nonDupDocs)
                {
                    OriginalDocumentsLogInfo(docHash.CollectionId, docHash.DocumentReferenceId);
                }
            }

            TaskCount = _tasks.Count;
            _affectedDocList = _lsAffectedDocument;
            EvLog.WriteEntry(_jobid.ToString(CultureInfo.InvariantCulture), Constants.GenerateTaskDelete,
                EventLogEntryType.Information);
        }

        #endregion

        #region GeneratetasksForGroupOperation

        /// <summary>
        /// This method helps to get DuplicateValue set in constants based on action type(delete/group)
        /// </summary>
        private string GetDuplicateFieldValue(string algorithm)
        {
            var strDuplicateFiledVal = string.Empty;

            switch (algorithm)
            {
                case Constants.Algorithm_MD5:
                    strDuplicateFiledVal = Constants.DUP_GROUP_ORGDOC_MD5;
                    break;
                case Constants.Algorithm_SHA1:
                    strDuplicateFiledVal = Constants.DUP_GROUP_ORGDOC_SHA1;
                    break;
            }

            _actionType = Constants.Action_GROUP_Type;
            return strDuplicateFiledVal;
        }

        #endregion

        #region GeneratetasksForGroupOperation

        /// <summary>
        /// This method helps to generate tasks to group duplicate documents from vault and  search sub-system
        /// </summary>
        private void GeneratetasksForGroupOperation(List<DocumentHashMapEntity> lsDocumentHash,
            string strDuplicateFiledVal)
        {
            if (lsDocumentHash.Count > 0)
            {
                foreach (var docHasMap in lsDocumentHash)
                {
                    _task = new DeduplicationJobTaskBEO
                    {
                        IsGroup = true,
                        CollectionId = docHasMap.CollectionId.ToString(),
                        MatterId = _matterId,
                        DocumentReferenceId = docHasMap.DocumentReferenceId,
                        HashValue = docHasMap.HashValue,
                        DuplicateField = strDuplicateFiledVal,
                        DuplicateDocumentCount = lsDocumentHash.Count
                    };
                    _taskNumber++;
                    _task.TaskNumber = _taskNumber;
                    _task.TaskComplete = false;
                    _task.TaskPercent = 100.0/lsDocumentHash.Count;
                    _task.TaskKey = DocumentBO.GetDCNNumber(_matterId, docHasMap.CollectionId.ToString(),
                        docHasMap.DocumentReferenceId, _createdByUserName);
                    _tasks.Add(_task);

                    var document = _vault.GetDocumentMasterData(new Guid(_task.CollectionId), _task.DocumentReferenceId);
                    GroupingLogInfo(document, docHasMap, lsDocumentHash);
                }
                TaskCount = _tasks.Count;
                _affectedDocList = _lsAffectedDocument;
                EvLog.WriteEntry(_jobid + Constants.GenerateTaskGrouping, string.Empty, EventLogEntryType.Information);
            }
        }

        #endregion

        #region GroupingLogInfo

        /// <summary>
        /// This method helps to Log Info which groups duplicates and there original document in turn docs whose hash values count >0 
        /// and groups them into a logical group say IsDuplicate replicate of each other.This helps the user to identify the docs in 
        /// Document Data Viewer with the help of Red color identification.
        /// </summary>
        private void GroupingLogInfo(DocumentMasterEntity document, DocumentHashMapEntity docHasMap,
            List<DocumentHashMapEntity> lsDocumentHash)
        {
            try
            {
                if (_tempHashInfo == string.Empty)
                {
                    _tempHashInfo = docHasMap.HashValue;
                    _objAffectedDocument = new AffectedDocument
                    {
                        CollectionId = document.CollectionId.ToString(),
                        DocId = document.DocumentReferenceId
                    };
                    if (!String.IsNullOrEmpty(document.NativeFilePath))
                    {
                        var strFileInfo = document.NativeFilePath;
                        var strFile = strFileInfo.Split(Convert.ToChar(@"\"));
                        _objAffectedDocument.Name = strFile[strFile.Length - 1];
                    }
                    else
                    {
                        _objAffectedDocument.Name = document.DocumentTitle;
                    }
                    if (document.DocumentTitle != null)
                    {
                        _objAffectedDocument.BatesNumber = document.DocumentTitle;
                    }
                    _lsBatesNo = new List<string>();
                    _isNew = true;
                }
                if (_tempHashInfo != string.Empty && _isNew == false)
                {
                    if (_tempHashInfo == docHasMap.HashValue)
                    {
                        _lsBatesNo.Add(document.DocumentTitle);
                        if (_intCount == lsDocumentHash.Count) //task.DuplicateDocumentCount
                        {
                            _objAffectedDocument.DuplicateList.Clear();
                            _lsBatesNo.SafeForEach(o => _objAffectedDocument.DuplicateList.Add(o));
                            _lsAffectedDocument.Add(_objAffectedDocument);
                        }
                    }
                    else
                    {
                        _objAffectedDocument.DuplicateList.Clear();
                        _lsBatesNo.SafeForEach(o => _objAffectedDocument.DuplicateList.Add(o));
                        _lsAffectedDocument.Add(_objAffectedDocument);

                        _objAffectedDocument = null;
                        _lsBatesNo = null;
                        _lsBatesNo = new List<string>();
                        _objAffectedDocument = new AffectedDocument
                        {
                            CollectionId = document.CollectionId.ToString(),
                            DocId = document.DocumentReferenceId
                        };
                        if (!String.IsNullOrEmpty(document.NativeFilePath))
                        {
                            var strFileInfo = document.NativeFilePath;
                            var strFile = strFileInfo.Split(Convert.ToChar(@"\"));
                            _objAffectedDocument.Name = strFile[strFile.Length - 1];
                        }
                        else
                        {
                            _objAffectedDocument.Name = document.DocumentTitle;
                        }
                        if (document.DocumentTitle != null)
                        {
                            _objAffectedDocument.BatesNumber = document.DocumentTitle;
                        }
                        _tempHashInfo = docHasMap.HashValue;
                    }
                }
                _isNew = false;
                _intCount++;
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(_jobid + Constants.ErrorForBatesNumber, ex.Message, EventLogEntryType.Error);
                LogException(_jobid, ex, Constants.ErrorForBatesNumber, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
        }

        #endregion

        #region CommonLogInfo

        /// <summary>
        /// This method helps to Log Info regarding Job name, Run date,ActionType and No.of Duplicates and original documents
        /// </summary>
        private DeduplicationJobLogBEO CommonLogInfo()
        {
            var logInfo = new DeduplicationJobLogBEO();
            try
            {
                logInfo.JobName = _jobName;
                logInfo.JobRunId = _jobrunId;
                logInfo.RunDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                logInfo.NoOfDuplicate = NoOfDuplicate;
                logInfo.ActionType = _actionType;
                _affectedDocList.SafeForEach(o => logInfo.AffectedDocList.Add(o));
                logInfo.NoOfDocumentProcessed = NoOfDocumentProcessed;
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(_jobid + Constants.ErrorForOriginalDocument, ex.Message, EventLogEntryType.Error);
                LogException(_jobid, ex, Constants.ErrorForOriginalDocument, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
            return logInfo;
        }

        #endregion

        #region OriginalDocumentsLogInfo

        /// <summary>
        /// This method helps to Log Info for which filler outs original documents from duplicate 
        /// documents and collects there bates no. to be shown to user.This is called for Action Type-Delete
        /// </summary>
        private void OriginalDocumentsLogInfo(Guid collectionId, string documentReferenceId)
        {
            try
            {
                //  List<AffectedDocument> lsAffectedDocument1 = new List<AffectedDocument>();
                var document = _vault.GetDocumentMasterData(collectionId, documentReferenceId);
                if (document != null)
                {
                    var objAffectedDocuments = new AffectedDocument
                    {
                        CollectionId = document.CollectionId.ToString(),
                        DocId = document.DocumentReferenceId
                    };
                    if (!String.IsNullOrEmpty(document.NativeFilePath))
                    {
                        var strFileInfo = document.NativeFilePath;
                        var strFile = strFileInfo.Split(Convert.ToChar(@"\"));
                        objAffectedDocuments.Name = strFile[strFile.Length - 1];
                    }
                    else
                    {
                        objAffectedDocuments.Name = document.DocumentTitle;
                    }

                    if (document.DocumentTitle != null)
                    {
                        objAffectedDocuments.BatesNumber = document.DocumentTitle;
                    }

                    _lsAffectedDocument.Add(objAffectedDocuments);
                }
                _actionType = Constants.Action_Delete_Type;
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(_jobid + Constants.ErrorForOriginalDocumentForDelete, ex.Message,
                    EventLogEntryType.Error); //Constants.EVENT_GENERATE_TASK_KEY,
                LogException(_jobid, ex, Constants.ErrorForOriginalDocument, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
        }

        #endregion

        #region GetDuplicatesDocuments

        /// <summary>
        /// This method helps to get all duplicate documents for a collection whose hash value count>0
        /// </summary>
        private List<string> GetDuplicatesDocuments(List<DocumentHashMapEntity> lsDocumentHash)
        {
            #region Pre-condition asserts

            lsDocumentHash.ShouldNotBe(null);

            #endregion

            var lsdupDoc = new List<string>();
            try
            {
                if (lsDocumentHash.Count > 0)
                {
                    var tempHash = lsDocumentHash[0].HashValue;
                    for (var i = 1; i < lsDocumentHash.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(tempHash) && tempHash == lsDocumentHash[i].HashValue)
                        {
                            lsdupDoc.Add(lsDocumentHash[i].DocumentReferenceId + lsDocumentHash[i].CollectionId);
                        }
                        else
                        {
                            tempHash = lsDocumentHash[i].HashValue;
                        }
                    }
                    NoOfDuplicate = lsdupDoc.Count;
                }
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(_jobid + Constants.ErrorForDuplicateDocumentsCount, ex.Message, EventLogEntryType.Error);
                LogException(_jobid, ex, Constants.ErrorForDuplicateDocumentsCount, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }

            #region Post-condition asserts

            lsDocumentHash.ShouldNotBe(null);

            #endregion

            return lsdupDoc;
        }

        #endregion

        #region GetOriginalDocuments

        /// <summary>
        /// This method helps to get all original documents under a collection
        /// </summary>
        private void GetOriginalDocuments(IEnumerable<string> lsCollectionId)
        {
            try
            {
                var docCount =
                    lsCollectionId.Select(collection => _vault.GetDocumentCountForCollection(new Guid(collection)))
                        .Aggregate(0, (current, count) => current + count);
                NoOfDocumentProcessed = docCount;
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(_jobid + Constants.ErrorForOriginalDocuments, ex.Message, EventLogEntryType.Error);
                LogException(_jobid, ex, Constants.ErrorForOriginalDocuments, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
        }

        #endregion

        #region GetDocumentHashValues

        /// <summary>
        /// This method helps to get all the Duplicate counts in a collection whose hash value count>0 (which is combination of original docs+ duplicate docs)
        /// </summary>
        private List<DocumentHashMapEntity> GetDocumentHashValues(IEnumerable<DeduplicationDatasetBEO> lsDataset,
            string algorithm, string compareType)
        {
            List<DocumentHashMapEntity> lsDocumentHash = null;
            try
            {
                var lsCollectionId = new List<string>();

                foreach (var datasetInfo in lsDataset)
                {
                    lsCollectionId.Add(datasetInfo.CollectionId);
                    if (_matterId == string.Empty)
                    {
                        _matterId = datasetInfo.MatterId.ToString(CultureInfo.InvariantCulture);
                    }
                }
                var hashTypeId = GetHashTypeId(algorithm, compareType);
                _vault = VaultRepository.CreateRepository(Convert.ToInt64(_matterId));

                var lsCollectionTemp = lsCollectionId.Select(collection => new Guid(collection)).ToList();
                if (lsCollectionId.Count > 1)
                {
                    lsDocumentHash = _vault.GetDocumentDuplicatesAcrossCollections(lsCollectionTemp, hashTypeId);
                }
                else
                {
                    DataSetCollectionId = lsCollectionTemp[0];
                    lsDocumentHash = _vault.GetDocumentDuplicatesByHashValue(lsCollectionTemp[0], hashTypeId,
                        string.Empty);
                }
                GetOriginalDocuments(lsCollectionId);
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry(_jobid + Constants.EVENT_GENERATE_TASK_KEY,
                    Constants.ErrorForDocumentHashValues + ex.Message);
                LogException(_jobid, ex, Constants.ErrorForOriginalDocuments, LogCategory.Job, string.Empty,
                    ErrorCodes.ProblemInGenerateTasks);
            }
            return lsDocumentHash;
        }

        #endregion

        #region DoAtomicWork

        /// <summary>
        /// This method helps to perform each task generated from GenerateTask method
        /// </summary>
        protected override bool DoAtomicWork(DeduplicationJobTaskBEO doAtomicTask, DeduplicationJobBEO jobParameters)
        {
            var isFailed = false;
          
            List<CollectionFieldEntity> lsColFieldEntity = null;

            try
            {
                if (doAtomicTask.IsDelete)
                {
                    try
                    {
                        DocumentBO.BatchDelete(doAtomicTask.MatterId, doAtomicTask.CollectionId,
                            doAtomicTask.DeleteDocumentList);
                        _successCount += doAtomicTask.DeleteDocumentList.Count;
                    }
                    catch (Exception ex)
                    {
                        isFailed = true;
                        _failureCount += doAtomicTask.DeleteDocumentList.Count;
                        EvLog.WriteEntry(
                            jobParameters.JobId + Constants.DO_ATOMIC_TASK + ":" +
                            Constants.DO_ATOMIC_ERR_DEL_DUP_DOC_VAULT, ex.Message);
                        LogException(_jobid, ex,
                            Constants.DO_ATOMIC_TASK + ":" + Constants.DO_ATOMIC_ERR_DEL_DUP_DOC_VAULT, LogCategory.Task,
                            _task.TaskKey, ErrorCodes.ProblemInDoAtomicWork);
                    }
                }
                else if (doAtomicTask.IsGroup)
                {
                    try
                    {
                        lsColFieldEntity = _vault.GetCollectionFields(DataSetCollectionId, EVSystemFields.Duplicate);
                    }
                    catch (Exception ex)
                    {
                        isFailed = true;
                        EvLog.WriteEntry(
                            jobParameters.JobId + Constants.DO_ATOMIC_TASK + ":" + Constants.DO_ATOMIC_ERR_GET_COL_FIELD,
                            ex.Message);
                        LogException(_jobid, ex, Constants.DO_ATOMIC_TASK + ":" + Constants.DO_ATOMIC_ERR_GET_COL_FIELD,
                            LogCategory.Task, _task.TaskKey, ErrorCodes.ProblemInDoAtomicWork);
                    }
                    var document = _vault.GetDocumentMasterData(new Guid(doAtomicTask.CollectionId),
                        doAtomicTask.DocumentReferenceId);

                    #region "Insert Fields for Document in vault"

                    if (lsColFieldEntity != null)
                    {
                        var documentFieldEntity = new DocumentFieldEntity
                        {
                            CreatedBy = document.CreatedBy,
                            DocumentReferenceId = document.DocumentReferenceId,
                            CollectionId = document.CollectionId,
                            FieldId = lsColFieldEntity[0].FieldId,
                            FieldValue = doAtomicTask.DuplicateField
                        };
                        try
                        {
                            _vault.CreateDocumentField(documentFieldEntity);
                        }
                        catch (Exception ex)
                        {
                            EvLog.WriteEntry(jobParameters.JobId + Constants.DO_ATOMIC_TASK,
                                Constants.DO_ATOMIC_ERR_INS_DUP_FIELD_VAULT + ex.Message);
                        }
                    }

                    #endregion

                    document.CollectionId.ShouldNotBe(Guid.Empty);
                    var rvwDocumentBeo = new RVWDocumentBEO {DocumentId = document.DocumentReferenceId};
                    
                    rvwDocumentBeo.CollectionId=document.CollectionId.ToString();
                    rvwDocumentBeo.DuplicateId = doAtomicTask.HashValue;
                    rvwDocumentBeo.MatterId = Convert.ToInt64(doAtomicTask.MatterId);
                    DocumentBO.UpdateDuplicateId(rvwDocumentBeo);

                    #region "Insert Fields for Document into search sub-system"

                    var fieldValues = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>(EVSystemFields.Duplicate.ToLower(), doAtomicTask.DuplicateField),
                        new KeyValuePair<string, string>(EVSystemFields.DuplicateId.ToLower(), doAtomicTask.HashValue)
                    };


                    var indexManagerProxy = new IndexManagerProxy(Convert.ToInt64(doAtomicTask.MatterId),doAtomicTask.CollectionId);
                        var documentBeos=new List<DocumentBeo>
                        {
                            DocumentBO.ToDocumentBeo(document.DocumentReferenceId, fieldValues)
                        };
                    indexManagerProxy.BulkUpdateDocumentsAsync(documentBeos);
                  
                    #endregion
                }
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(jobParameters.JobId + Constants.EVENT_DO_ATOMIC_WORK_EXCEPTION_VALUE, exp.Message,
                    EventLogEntryType.Error);
                LogException(_jobid, exp, Constants.EVENT_DO_ATOMIC_WORK_EXCEPTION_VALUE, LogCategory.Task,
                    _task.TaskKey, ErrorCodes.ProblemInDoAtomicWork);
                isFailed = true;
            }
            return (!isFailed);
        }

        #endregion

        #region HandleFailedTasks

        /// <summary>
        /// Handle the failed tasks specific to a job.
        /// </summary>
        /// <param name="failedTasks">List of tasks that failed.</param>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected override void HandleFailedTasks(Tasks<DeduplicationJobTaskBEO> failedTasks,
            DeduplicationJobBEO jobParameters)
        {
            FailedTask = failedTasks.Count;
            EvLog.WriteEntry(Constants.FAILED_TASK, Constants.AUDIT_FAILED_TASK_HANDLER_VALUE);
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// Perform shutdown activities for a job if any.
        /// </summary>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        protected override void Shutdown(DeduplicationJobBEO jobParameters)
        {
            EvLog.WriteEntry(jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                Constants.EVENT_JOB_SHUTDOWN_KEY, EventLogEntryType.Information);

            #region LogDetails

            if (LogDetails != null)
            {
                LogDetails.NoOfDocAffected = (TaskCount - FailedTask);
                var strLogXml = CreateXMLPropertyString(LogDetails);
                var info = new LogInfo {CustomMessage = strLogXml};
                LogJobMessage(info, true);
            }
            // Update Job master with the success and failure count
            JobMgmtBO.UpdateJobResult(_jobid, _successCount, _failureCount,
                null);

            #endregion
        }

        #endregion

        #region GetDeduplicationProfileBEO

        /// <summary>
        /// This method will return De duplication Profile BEO out of the passed boot paramter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns>Profile Business entity object</returns>
        private DeduplicationProfileBEO GetDeduplicationProfileBEO(String bootParamter)
        {
            //Creating a stringReader stream for the boot parameter
            using (var stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                var xmlStream = new XmlSerializer(typeof (DeduplicationProfileBEO));
                //De serialization of boot parameter to get profileBEO
                return (DeduplicationProfileBEO) xmlStream.Deserialize(stream);
            }
        }

        #endregion

        #region Hash Type Id

        private short GetHashTypeId(string algorithm, string compareType)
        {
            short hashTypeId = 0;
            if (compareType.Equals(Constants.CompareType_Fields))
            {
                hashTypeId = 5; //For field, MD5 default
            }
            else if (algorithm.Equals(Constants.Algorithm_MD5) &&
                     compareType.Equals(Constants.CompareType_OriginalDocuments))
            {
                hashTypeId = 3;
            }
            else if (algorithm.Equals(Constants.Algorithm_SHA1) &&
                     compareType.Equals(Constants.CompareType_OriginalDocuments))
            {
                hashTypeId = 4;
            }
            return hashTypeId;
        }

        #endregion

        #region Create XML for DeduplicationJobLogBE

        /// <summary>
        /// Serializes the ProfileBEO object and returns the xml as string
        /// </summary>
        /// <param name="logBeo"></param>
        /// <returns></returns>
        private static string CreateXMLPropertyString(DeduplicationJobLogBEO logBeo)
        {
            string logData;
            using (var xmlStream = new StringWriter(System.Threading.Thread.CurrentThread.CurrentCulture))
            {
                var xmlSerializer = new XmlSerializer(typeof (DeduplicationJobLogBEO));
                xmlSerializer.Serialize(xmlStream, logBeo);
                logData = xmlStream.ToString();
            }
            return logData;
        }

        #endregion

        #region Helper Method

        /// <summary>
        /// To separate list of fields to comma separated form
        /// </summary>
        /// <param name="datasets"></param>
        /// <returns></returns>
        private static string CommaSeparator(List<DeduplicationDatasetBEO> datasets)
        {
            var fieldsList = new StringBuilder();
            var index = 0;
            foreach (var dataset in datasets)
            {
                if (index < datasets.Count - 1)
                {
                    fieldsList.Append(dataset.DatasetName + Constants.EV_COMMA);
                }
                else
                {
                    fieldsList.Append(dataset.DatasetName);
                }
                index++;
            }
            return fieldsList.ToString();
        }

        #endregion

        #region LogException

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="exp">exception received</param>
        /// <param name="msg">message to be logged</param>
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key to identify the Task, need for task log only</param>
        /// <param name="errorCode">Key to identify the error code based on which appropriate error message is displayed</param>
        private void LogException(int jobId, Exception exp, string msg, LogCategory category, string taskKey,
            string errorCode)
        {
            exp.Trace();
            if (category == LogCategory.Job)
            {
                JobLogInfo.AddParameters(jobId + msg);
                var jobException = new EVJobException(errorCode, exp, JobLogInfo);
                throw (jobException);
            }
            TaskLogInfo.AddParameters(jobId + msg);
            TaskLogInfo.TaskKey = taskKey;
            var taskException = new EVTaskException(errorCode, exp, TaskLogInfo);
            throw (taskException);
        }

        #endregion
    }
}