#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="DataSetDeleteJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Thangakumar</author>
//      <Reviewer>RajkumarPandurangan</Reviewer>
//      <description>
//          Job to delete dataset.
//      </description>
//      <changelog>
//          <date value="02/03/2011">Bug Fix 86335</date>
//          <date value="03/26/2012">Dataset delete job issue fixed</date>
//          <date value="3/30/2014">CNEV 3.0 - Requirement Bug #165088 - Document Delete NFR and functional fix : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespace
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.MatterManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


#endregion


namespace LexisNexis.Evolution.BatchJobs.DataSetDelete
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class DataSetDeleteJob : BaseJob<DeleteDataSetJobBEO, DeleteDataSetTaskBEO>
    {
        #region Private Fields
        private DeleteDataSetJobBEO _deleteDataSetJobBeo; // Job level data
        internal const string AxlFilePrefix = "mk-";
        internal const string AlphabetD = "D";
        internal const string XmlExtension = ".xml";
        private int m_NumberOfTasks;
        private int m_WindowSize;
        private int m_ReadChunkSize;

        #endregion

        #region Instancevariables

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public DataSetDeleteJob()
        {

        }

        #endregion

        #region Job FrameWork Ovveriden Functions
        /// <summary>
        /// Initializes Job BEO 
        /// </summary>
        /// <param name="jobId">DataSet Delete Identifier</param>
        /// <param name="jobRunId">DataSet Delete Run Identifier</param>
        /// <param name="bootParameters">Boot parameters</param>
        /// <param name="createdBy">DataSet Delete created by</param>
        /// <returns>DataSet Delete Job Business Entity</returns>
        protected override DeleteDataSetJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            try
            {
                m_WindowSize = 100;
                m_ReadChunkSize = 1000;
                m_NumberOfTasks = 0;
                //Message that job has been initialized.
                LogMessage(String.Format(Constants.JobInitialized, jobId), false, LogCategory.Job, null);
                EvLog.WriteEntry(String.Format(Constants.JobInitialized, jobId), String.Format(Constants.JobInitialized, jobId));
                // Set Job level properties to DataSet Delete Job business entity object.
                _deleteDataSetJobBeo = new DeleteDataSetJobBEO { JobId = jobId, JobRunId = jobRunId };
                if (bootParameters != null)
                {
                    DeleteDataSetJobBEO deleteDataSetData = GetDataSetDeleteBEO(bootParameters);
                    //Log
                    LogMessage(String.Format(Constants.JobBootParameterParsed, jobId), false, LogCategory.Job, null);
                    EvLog.WriteEntry(String.Format(Constants.JobBootParameterParsed, jobId), String.Format(Constants.JobBootParameterParsed, jobId),
                                        EventLogEntryType.Information);

                    if (deleteDataSetData != null)
                    {
                        _deleteDataSetJobBeo.BootParameters = bootParameters;
                        _deleteDataSetJobBeo.DataSetId = deleteDataSetData.DataSetId;
                        _deleteDataSetJobBeo.DataSetName = deleteDataSetData.DataSetName;
                        _deleteDataSetJobBeo.DeletedBy = deleteDataSetData.DeletedBy;
                        _deleteDataSetJobBeo.CollectionId = deleteDataSetData.CollectionId;
                        _deleteDataSetJobBeo.MatterId = deleteDataSetData.MatterId;
                    }
                    else
                    {
                        LogMessage(String.Format(Constants.JobXMLNotWellFramed, jobId), false, LogCategory.Job, null);
                        EvLog.WriteEntry(String.Format(Constants.JobXMLNotWellFramed, jobId),
                                            String.Format(Constants.JobXMLNotWellFramed, jobId), EventLogEntryType.Error);
                        throw new EVException().AddResMsg(ErrorCodes.ImpXmlFormatErrorId);
                    }
                }
                _deleteDataSetJobBeo.JobScheduleCreatedBy = createdBy;
                _deleteDataSetJobBeo.JobTypeName = Constants.JobName;
                _deleteDataSetJobBeo.JobScheduleCreatedBy = createdBy;
            }
            catch (EVException ex)
            {
                LogToEventLog(ex, GetType(), MethodInfo.GetCurrentMethod().Name, jobId, jobRunId);
                HandleJobException(GetEvExceptionDescription(ex), ex, ErrorCodes.ProblemInJobInitialization);
            }
            catch (Exception ex)
            {
                // Handle exception
                LogMessage(ex, GetType(), MethodInfo.GetCurrentMethod().Name, EventLogEntryType.Error, jobId, jobRunId);
                HandleJobException(String.Format(Constants.JobInitializeException, jobId), ex, ErrorCodes.ProblemInJobInitialization);

            }
            //return DataSet Delete Job Business Entity
            return _deleteDataSetJobBeo;
        }

        /// <summary>
        /// Generates DataSet Delete tasks
        /// </summary>
        /// <param name="jobParameters">DataSet Delete Job BEO</param>
        /// <param name="previouslyCommittedTaskCount">int</param>
        /// <returns>List of DataSet DeleteJob Tasks (BEOs)</returns>
        protected override Tasks<DeleteDataSetTaskBEO> GenerateTasks(DeleteDataSetJobBEO jobParameters, out int previouslyCommittedTaskCount)
        {
            Tasks<DeleteDataSetTaskBEO> tasks = null;
            previouslyCommittedTaskCount = 0;
            try
            {
                // Message that Generate Tasks called.
                LogMessage(String.Format(Constants.JobGenerateTasksInitialized, jobParameters.JobId), false, LogCategory.Job, null);
                EvLog.WriteEntry(String.Format(Constants.JobGenerateTasksInitialized, jobParameters.JobId),
                    String.Format(Constants.JobGenerateTasksInitialized, jobParameters.JobId), EventLogEntryType.Information);


                var docCount = MatterBO.GetDocumentCount(Convert.ToInt64(jobParameters.MatterId), new List<string>() { jobParameters.CollectionId });
                var dataSetDocuments = new List<ReIndexDocumentBEO>();
                if (docCount > 0)
                {
                    var nMessages = (Int32)(Math.Ceiling((double)docCount / m_ReadChunkSize));
                        //Convert.ToInt32(docCount / m_ReadChunkSize) + (docCount % m_ReadChunkSize > 0 ? 1 : 0);
                    var processed = 0;

                    //Loop through and send the request in batches
                    for (var pageIdx = 1; pageIdx <= nMessages; pageIdx++)
                    {
                        var pgSize = 0;
                        //Determine the page size and processed count
                        if (nMessages == 1)
                        {
                            pgSize = (Int32)docCount;
                        }
                        else if (nMessages > 1 && pageIdx == nMessages)
                        {
                            pgSize = (Int32)docCount - processed;
                        }
                        else
                        {
                            pgSize = m_ReadChunkSize;
                        }

                        var batchDocuments = MatterBO.GetCollectionDocuments(Convert.ToInt64(jobParameters.MatterId),
                            pageIdx, m_ReadChunkSize, new List<string>() { jobParameters.CollectionId });
                        if (batchDocuments != null && batchDocuments.Any())
                        {
                            dataSetDocuments.AddRange(batchDocuments);
                        }
                        processed += pgSize;
                    }
                }

                //Get matter details for matter id
                var matterDetail = MatterServices.GetMatterDetails(jobParameters.MatterId);
                var dataSetDetail = DataSetBO.GetDataSetDetailForDataSetId(jobParameters.DataSetId);

                //Get all document sets for dataset id
                var lstDocumentSet = DataSetService.GetAllDocumentSet(jobParameters.DataSetId.ToString());

                tasks = GetTaskList<DeleteDataSetJobBEO, DeleteDataSetTaskBEO>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;
                var documentTaskCount = (Int32)(Math.Ceiling((double)dataSetDocuments.Count / m_WindowSize));


                m_NumberOfTasks = documentTaskCount + lstDocumentSet.Count + 1;
                double taskPercent = (100.0 / m_NumberOfTasks);

                int taskNumber = 0;
                DeleteDataSetTaskBEO deleteDataSetTaskBeo;
                // Create tasks for the documents in group
                for (taskNumber = 0; taskNumber < documentTaskCount; taskNumber++)
                {

                    deleteDataSetTaskBeo = new DeleteDataSetTaskBEO
                    {
                        TaskNumber = taskNumber + 1,
                        TaskComplete = false,
                        TaskPercent = taskPercent,
                        DataSetId = jobParameters.DataSetId,
                        DataSetName = jobParameters.DataSetName,
                        DocumentSetId = jobParameters.CollectionId,
                        DeletedBy = jobParameters.DeletedBy,
                        DocumentId =
                            dataSetDocuments.GetRange(taskNumber * m_WindowSize,
                                Math.Min((dataSetDocuments.Count - (taskNumber * m_WindowSize)), m_WindowSize))
                                .Select(d => d.DocumentReferenceId)
                                .ToList(),
                        MatterDBName = matterDetail.MatterDBName,
                        IsDocumentDelete = true
                    };
                    tasks.Add(deleteDataSetTaskBeo);
                }

                // Create the task for the non-native document sets (production & image sets)
                var nonNativeSets = lstDocumentSet.Where(ds => ds.DocumentSetTypeId != "2");
                foreach (var docset in nonNativeSets)
                {
                    taskNumber += 1;
                    deleteDataSetTaskBeo = new DeleteDataSetTaskBEO
                    {
                        TaskNumber = taskNumber,
                        TaskComplete = false,
                        TaskPercent = taskPercent,
                        DataSetId = jobParameters.DataSetId,
                        DataSetName = jobParameters.DataSetName,
                        DeletedBy = jobParameters.DeletedBy,
                        DocumentId = new List<string>(),
                        MatterDBName = matterDetail.MatterDBName,
                        DocumentSetId = docset.DocumentSetId,
                        DocumentSetTypeId = docset.DocumentSetTypeId,
                        IsDocumentDelete = false
                    };
                    tasks.Add(deleteDataSetTaskBeo);
                }

                // Create the task for only the native document sets
                var nativeSet = lstDocumentSet.Where(ds => ds.DocumentSetTypeId == "2");
                foreach (var docset in nativeSet)
                {
                    taskNumber += 1;
                    deleteDataSetTaskBeo = new DeleteDataSetTaskBEO
                    {
                        TaskNumber = taskNumber,
                        TaskComplete = false,
                        TaskPercent = taskPercent,
                        DataSetId = jobParameters.DataSetId,
                        DataSetName = jobParameters.DataSetName,
                        DeletedBy = jobParameters.DeletedBy,
                        DocumentId = new List<string>(),
                        MatterDBName = matterDetail.MatterDBName,
                        DocumentSetId = docset.DocumentSetId,
                        DocumentSetTypeId = docset.DocumentSetTypeId,
                        IsDocumentDelete = false
                    };
                    tasks.Add(deleteDataSetTaskBeo);
                }

                taskNumber += 1;
                deleteDataSetTaskBeo = new DeleteDataSetTaskBEO
                                           {
                                               TaskNumber = taskNumber,
                                               TaskComplete = false,
                                               TaskPercent = 100,
                                               DataSetId = jobParameters.DataSetId,
                                               DataSetName = jobParameters.DataSetName,
                                               DeletedBy = jobParameters.DeletedBy,
                                               DocumentId = new List<string>(),
                                               DocumentSetId = jobParameters.CollectionId,
                                               MatterDBName = matterDetail.MatterDBName,
                                               IsDocumentDelete = false,
                                               DocumentSets = lstDocumentSet,
                                               ExtractionPath = dataSetDetail.CompressedFileExtractionLocation,
                                               DocumentSetTypeId = string.Empty
                                           };
                tasks.Add(deleteDataSetTaskBeo);

                for (int i = 1; i <= tasks.Count; i++)
                {
                    tasks[i - 1].TaskNumber = i;
                }

            }
            catch (EVException ex)
            {
                LogToEventLog(ex, GetType(), MethodInfo.GetCurrentMethod().Name, jobParameters.JobId, jobParameters.JobRunId);
                HandleJobException(GetEvExceptionDescription(ex), null, ErrorCodes.ProblemInGenerateTasks);

            }
            catch (Exception ex)
            {
                // Handle exception in initialize
                HandleJobException(String.Format(Constants.JobGenerateTasksException, jobParameters.JobId), ex, ErrorCodes.ProblemInGenerateTasks);
                LogMessage(ex, GetType(), MethodInfo.GetCurrentMethod().Name, EventLogEntryType.Error, jobParameters.JobId, jobParameters.JobRunId);
            }
            return tasks;
        }
        /// <summary>
        /// Atomic work 1) Delete Search sub-system Data 2) Delete Vault Data 3) Delete EVMaster Data 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(DeleteDataSetTaskBEO task, DeleteDataSetJobBEO jobParameters)
        {
            const bool bolOutput = false;
            try
            {
                LogMessage(String.Format(Constants.JobDoAtomicWorkInitialized, jobParameters.JobId), false, LogCategory.Job, null);
                EvLog.WriteEntry(String.Format(Constants.JobDoAtomicWorkInitialized, jobParameters.JobId),
                String.Format(Constants.JobDoAtomicWorkInitialized, jobParameters.JobId), EventLogEntryType.Information);
                if (task.IsDocumentDelete)
                {
                    LogMessage(String.Format(Constants.JobDoAtomicWorkDeleteDocument, jobParameters.JobId, String.Join(",", task.DocumentId)), false, LogCategory.Job, null);
                    EvLog.WriteEntry(String.Format(Constants.JobDoAtomicWorkDeleteDocument, jobParameters.JobId, String.Join(",", task.DocumentId)),
                    String.Format(Constants.JobDoAtomicWorkDeleteDocument, jobParameters.JobId, String.Join(",", task.DocumentId)), EventLogEntryType.Information);

                    DeleteDocument(task, jobParameters);
                }
                else if (task.DocumentSetTypeId == Constants.ProductionSet)
                {
                    LogMessage(String.Format(Constants.JobDoAtomicWorkDeleteProductionset, jobParameters.JobId, task.DocumentSetId), false, LogCategory.Job, null);
                    EvLog.WriteEntry(String.Format(Constants.JobDoAtomicWorkDeleteProductionset, jobParameters.JobId, task.DocumentSetId),
                    String.Format(Constants.JobDoAtomicWorkDeleteProductionset, jobParameters.JobId, task.DocumentSetId), EventLogEntryType.Information);

                    DeleteDocumentSetFromVault(task, jobParameters);
                }
                else if (task.DocumentSetTypeId == Constants.NativeSet)
                {
                    LogMessage(String.Format(Constants.JobDoAtomicWorkDeleteNativeset, jobParameters.JobId, task.DocumentSetId), false, LogCategory.Job, null);
                    EvLog.WriteEntry(String.Format(Constants.JobDoAtomicWorkDeleteNativeset, jobParameters.JobId, task.DocumentSetId),
                    String.Format(Constants.JobDoAtomicWorkDeleteNativeset, jobParameters.JobId, task.DocumentSetId), EventLogEntryType.Information);

                    DeleteDatasetFromSearchAndVault(jobParameters);
                }
                else if (task.DocumentSetTypeId == Constants.ImageSet)
                {
                    LogMessage(String.Format(Constants.JobDoAtomicWorkDeleteImageset, jobParameters.JobId, task.DocumentSetId), false, LogCategory.Job, null);
                    EvLog.WriteEntry(String.Format(Constants.JobDoAtomicWorkDeleteImageset, jobParameters.JobId, task.DocumentSetId),
                    String.Format(Constants.JobDoAtomicWorkDeleteImageset, jobParameters.JobId, task.DocumentSetId), EventLogEntryType.Information);

                    DeleteDocumentSetFromVault(task, jobParameters);
                }
                else if (task.DocumentSetTypeId == string.Empty)
                {
                    LogMessage(String.Format(Constants.JobDoAtomicWorkDeleteDataset, jobParameters.JobId), false, LogCategory.Job, null);
                    EvLog.WriteEntry(String.Format(Constants.JobDoAtomicWorkDeleteDataset, jobParameters.JobId),
                    String.Format(Constants.JobDoAtomicWorkDeleteDataset, jobParameters.JobId), EventLogEntryType.Information);

                    DeleteDataSetFromEvMaster(task, jobParameters);
                }
            }
            catch (EVException ex)
            {
                LogToEventLog(ex, GetType(), MethodInfo.GetCurrentMethod().Name, jobParameters.JobId, jobParameters.JobRunId);
                HandleTaskException(GetEvExceptionDescription(ex), null, ErrorCodes.ProblemInDoAtomicWork);

            }
            catch (Exception ex)
            {
                // Handle exception in DoAutomic
                LogMessage(ex, GetType(), MethodInfo.GetCurrentMethod().Name, EventLogEntryType.Error, jobParameters.JobId, jobParameters.JobRunId);
                HandleTaskException(Constants.JobDoAtomicWorksException, ex, ErrorCodes.ProblemInDoAtomicWork);

            }
            return bolOutput;
        }
        #endregion

        #region GetDataSetDeleteBEO
        /// <summary>
        /// This method will return profile BEO out of the passed boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns>Profile Business entity object</returns>
        private DeleteDataSetJobBEO GetDataSetDeleteBEO(String bootParamter)
        {
            //Creating a stringReader stream for the boot parameter
            using (StringReader stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                XmlSerializer xmlStream = new XmlSerializer(typeof(DeleteDataSetJobBEO));

                //De serialization of boot parameter to get profileBEO
                return (DeleteDataSetJobBEO)xmlStream.Deserialize(stream);
            }
        }
        #endregion

        /// <summary>
        /// Delete a single document for document id
        /// </summary>
        /// <param name="task">DeleteDataSetTaskBEO</param>
        /// <param name="jobParameters">DeleteDataSetJobBEO</param>
        private void DeleteDocument(DeleteDataSetTaskBEO task, DeleteDataSetJobBEO jobParameters)
        {
            if (jobParameters != null)
            {
                DocumentBO.BatchDelete(jobParameters.MatterId, jobParameters.DataSetId, task.DocumentId, false, false);
            }
        }

        /// <summary>
        /// Deletes the dataset from vault and search sub-system
        /// </summary>
        /// <param name="jobParameters">DeleteDataSetJobBEO</param>
        private void DeleteDatasetFromSearchAndVault(DeleteDataSetJobBEO jobParameters)
        {
            if (jobParameters != null)
            {
                var datasetUuid = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", Constants.Matter, Constants.Hyphen, jobParameters.MatterId, Constants.Hyphen, Constants.Dataset, Constants.Hyphen, jobParameters.DataSetId, Constants.Hyphen, Constants.Collection, Constants.Hyphen, jobParameters.CollectionId);
                DataSetBO.DeleteDocumentSet(datasetUuid, jobParameters.DeletedBy, true);
            }
        }

        /// <summary>
        /// DeleteDocumentSetFromVault
        /// </summary>
        /// <param name="task">DeleteDataSetTaskBEO</param>
        /// <param name="jobParameters">DeleteDataSetJobBEO</param>
        private void DeleteDocumentSetFromVault(DeleteDataSetTaskBEO task, DeleteDataSetJobBEO jobParameters)
        {
            if (jobParameters != null)
            {
                string datasetUuid = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", Constants.Matter, Constants.Hyphen, jobParameters.MatterId, Constants.Hyphen, Constants.Dataset, Constants.Hyphen, jobParameters.DataSetId, Constants.Hyphen, Constants.Collection, Constants.Hyphen, task.DocumentSetId);
                DataSetBO.DeleteDocumentSet(datasetUuid, jobParameters.DeletedBy, false);
            }
        }

        /// <summary>
        /// Deletes the DataSet From EvMaster
        /// </summary>
        /// <param name="task">DeleteDataSetTaskBEO</param>
        /// <param name="jobParameters">DeleteDataSetJobBEO</param>
        private void DeleteDataSetFromEvMaster(DeleteDataSetTaskBEO task, DeleteDataSetJobBEO jobParameters)
        {
            if (jobParameters != null)
            {
                string datasetUuid = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", Constants.Matter, Constants.Hyphen,
                    jobParameters.MatterId, Constants.Hyphen, Constants.Dataset, Constants.Hyphen, jobParameters.DataSetId,
                    Constants.Hyphen, Constants.Collection, Constants.Hyphen, jobParameters.CollectionId);

                DataSetBO.DeleteDataSetFromEVMaster(datasetUuid);

                var externalizationEnabled = Convert.ToBoolean(CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.ExternalizationConfiguration));
                if (externalizationEnabled)
                {
                    DocumentBO.DeleteExternalization(jobParameters.MatterId, task.ExtractionPath, task.DocumentSets);
                }
            }
        }


        #region Job framework logging and Exception handling

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="isError">if set to <c>true</c> [is error].</param>
        /// <param name="category">The category.</param>
        /// <param name="additionalDetail">The additional detail.</param>
        private void LogMessage(string customMessage, bool isError, LogCategory category, List<KeyValuePair<string, string>> additionalDetail)
        {
            switch (category)
            {
                case LogCategory.Job:
                    JobLogInfo.AddParameters(customMessage);
                    JobLogInfo.IsError = isError;
                    if (additionalDetail != null && additionalDetail.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> keyValue in additionalDetail)
                        {
                            JobLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                        }
                    }
                    break;
                case LogCategory.Task:
                    TaskLogInfo.AddParameters(customMessage);
                    TaskLogInfo.IsError = isError;
                    if (additionalDetail != null && additionalDetail.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> keyValue in additionalDetail)
                        {
                            TaskLogInfo.AddParameters(keyValue.Key, keyValue.Value);
                        }
                    }
                    break;
            }
        }


        /// <summary>
        /// Handles the job exception.
        /// </summary>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="errorCode">Error Code</param>
        private void HandleJobException(string customMessage, Exception ex, string errorCode)
        {
            HandleException(LogCategory.Job, customMessage, ex, errorCode);
        }

        /// <summary>
        /// Handles the task exception.
        /// </summary>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="ex">The ex.</param>
        /// /// <param name="errorCode">Error Code</param>
        private void HandleTaskException(string customMessage, Exception ex, string errorCode)
        {
            HandleException(LogCategory.Task, customMessage, ex, errorCode);
        }

        /// <summary>
        /// Handles the exception.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="errorCode">Error Code</param>
        private void HandleException(LogCategory category, string customMessage, Exception ex, string errorCode)
        {
            if (category == LogCategory.Job)
            {
                JobLogInfo.AddParameters(customMessage);
                EVJobException jobException = new EVJobException(errorCode, ex, JobLogInfo);
                throw (jobException);
            }
            TaskLogInfo.AddParameters(customMessage);
            EVTaskException taskException = new EVTaskException(errorCode, ex, TaskLogInfo);
            throw (taskException);
        }

        /// <summary>
        /// Gets the EV exception description.
        /// </summary>
        /// <param name="evException">The ev exception.</param>
        /// <returns></returns>
        private string GetEvExceptionDescription(EVException evException)
        {
            string message = Msg.FromRes(evException.GetErrorCode()); // Get user friendly message by the error code.              
            return message;
        }

        /// <summary>
        /// EV Exception if thrown use error code for locating message from resource file.
        /// This function logs the message as well...
        /// </summary>
        /// <param name="evException">EV specific application error.</param>
        /// <param name="consumerClass">Type</param>
        /// <param name="location">Location from which message is being logged - normally it's function name</param>
        /// <param name="jobId">int</param>
        /// <param name="jobRunId">int</param>
        /// <returns>Success Status.</returns>
        private void LogToEventLog(EVException evException, Type consumerClass, string location, int jobId, int jobRunId)
        {
            string message = Msg.FromRes(evException.GetErrorCode()); // Get user friendly message by the error code.  
            LogMessage(message, true, LogCategory.Job, null);
            // Log message
            LogMessage(message + Constants.NextLineCharacter + evException.ToUserString(), consumerClass, location, EventLogEntryType.Error, jobId, jobRunId);
        }
        #endregion

        #region Utility Method

        /// <summary>
        /// Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of logging messages.
        /// </summary>
        /// <param name="message"> Message to be logged</param>
        /// <param name="consumerClass"> Import job class type using this function </param>
        /// <param name="messageLocation"> Location from which message is being logged - normally it's function name </param>
        /// <param name="eventLogEntryType"> Error or Message or Audit entry </param>
        /// <param name="jobId"> Job Identifier </param>
        /// <param name="jobRunId"> Job instance identifier </param>
        public static void LogMessage(string message, Type consumerClass, string messageLocation, EventLogEntryType eventLogEntryType, int jobId, int jobRunId)
        {
            try
            {
                //// Errors are always logged, if levels of logging is set to true, events are always logged.
                //if (eventLogEntryType == EventLogEntryType.Error)
                //    shallIlog = true;

                EvLog.WriteEntry(consumerClass.ToString(),
                                 Constants.JobID + jobId
                                 + Constants.NextLineCharacter + Constants.JobRunID + jobRunId
                                 + Constants.NextLineCharacter + Constants.JobLocation + messageLocation
                                 + Constants.NextLineCharacter + ((message.Equals(string.Empty)) ? string.Empty : "Details: " + message), eventLogEntryType);
            }
            catch (Exception)
            { } // No error logging a message can be captured and handled
        }

        /// <summary>
        /// Logs messages as required by ED Loader Job. Created as a separate function so that the job has a consistent way of logging messages.
        /// </summary>
        /// <param name="exception"> Exception details to be logged </param>
        /// <param name="consumerClass"> Import job class type using this function </param>
        /// <param name="messageLocation">string</param>
        /// <param name="eventLogEntryType"> Error or Message or Audit entry </param>
        /// <param name="jobId"> Job Identifier </param>
        /// <param name="jobRunId"> Job instance identifier </param>
        public static void LogMessage(Exception exception, Type consumerClass, string messageLocation, EventLogEntryType eventLogEntryType, int jobId, int jobRunId)
        {
            try
            {
                // Create Message from the exception
                StringBuilder message = new StringBuilder();
                message.Append((exception != null) ? Constants.JobErrorMessage + exception.Message + Constants.NextLineCharacter : string.Empty);

                const int levelOfInnerException = 1;
                if (exception != null && exception.InnerException != null) GetMessagesFromInnerException(exception, message, levelOfInnerException);

                message.Append(exception != null && (exception.StackTrace != null) ? Constants.JobStackTrace + exception.StackTrace + Constants.NextLineCharacter : string.Empty);

                // Log message.
                LogMessage(message.ToString(), consumerClass, messageLocation, eventLogEntryType, jobId, jobRunId);
            }
            catch { } // No error logging a message can be captured and handled

        }

        /// <summary>
        /// Obtains exception detail and stack trace
        /// </summary>
        /// <param name="exception"> Exception object for which inner exception details need to be obtained </param>
        /// <param name="message"> message object to which details are appended </param>
        /// <param name="levelOfException"> auto incremented number that depicts level of inner exception </param>
        /// <returns> recursive function - hence the bool return type </returns>
        public static bool GetMessagesFromInnerException(Exception exception, StringBuilder message, int levelOfException)
        {
            checked
            {
                levelOfException += 1;
            }

            message.Append(string.Format(Constants.JobInnerException1 + exception.InnerException + Constants.NextLineCharacter, levelOfException));

            if (exception.InnerException.StackTrace != null)
            {
                message.Append(Constants.JobInnerException2 + exception.InnerException.StackTrace + Constants.JobInnerException3 + Constants.NextLineCharacter);
            }

            message.Append(string.Format(Constants.JobInnerException4, levelOfException));

            if (exception.InnerException.InnerException != null) return GetMessagesFromInnerException(exception.InnerException, message, levelOfException);
            return false;
        }


        /// <summary>
        /// Encapsulates resource manager creation and retrieval of resource values
        /// </summary>
        [Serializable]
        public class ResourceManagerHelper
        {
            readonly ResourceManager _resourceManager;

            /// <summary>
            /// When resource is unavailable, getResourceValue function shall return this value.
            /// </summary>
            public string ResourceUnavailableString
            {
                get;
                set;
            }


            /// <summary>
            /// Creates file based resource manager object.
            /// </summary>
            /// <param name="resouceBaseName"></param>
            /// <param name="resourceDirectory"></param>
            public ResourceManagerHelper(string resouceBaseName, string resourceDirectory)
            {
                _resourceManager = ResourceManager.CreateFileBasedResourceManager(resouceBaseName, resourceDirectory, null);
                ResourceUnavailableString = string.Empty;
            }

            /// <summary>
            /// Return resource value if available, if not returns empty string
            /// </summary>
            /// <param name="resourceName">Resource for which value need to be obtained</param>
            /// <returns> Resource value or empty string </returns>
            public string GetResourceValue(string resourceName)
            {
                try
                {
                    return _resourceManager.GetString(resourceName);
                }
                catch
                {
                    return ResourceUnavailableString;
                }

            }
        }
        #endregion
    }
}
