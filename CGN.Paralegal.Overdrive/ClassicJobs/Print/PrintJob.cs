#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="PrintJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Suneeth senthil Jayapalan/Anandhi Rengasamy</author>
//      <description>
//          Actual backend Backend process which does the Print. The assembly,
//                      containing this code will be invoked by the JobManagement services
//                      based on the Print schedule
//      </description>
//      <changelog>
//          <date value="08/18/2010">created</date>
//          <date value="02/10/2012">Fix for bug 95956</date>
//          <date value="03/07/2012">Fix for bug 97614</date>
//          <date value="21/Mar/2012">98210 bug fix</date>
//          <date value="04/19/2012">Bug Fix 98566</date>
//          <date value="30/Jan/2013">CCB bug fix 112466</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion

#region Namespaces
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Business.DeliveryOptions;
using LexisNexis.Evolution.Business.PrintToFile;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.DataAccess.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.TraceServices;

#endregion

namespace LexisNexis.Evolution.BatchJobs.Print
{
    [Serializable]
    public class PrintJob : BaseJob<BaseJobBEO, BaseJobTaskBusinessEntity>
    {
        #region Private Fields

        private readonly BaseJobBEO m_Job; // Job level data 
        private UserBusinessEntity m_UserBusinessEntity;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public PrintJob()
        {
            m_Job = new BaseJobBEO();
        }
        #endregion

        #region Job Management Related functions

        /// <summary>
        /// This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy">string</param>
        /// <returns>jobParameters</returns>
        protected override BaseJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobInitializeStart, EventLogEntryType.Information);
                //Create Folders for Temporary storage
                this.CreateFoldersForTemperoryStorage();
                //Clean shared Space
                this.CleanSharedSpace();
                //filling properties of the job parameter
                this.m_Job.JobId = jobId;
                this.m_Job.JobRunId = jobRunId;
                this.m_Job.JobName = Constants.PrintJobName;
                this.m_UserBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                this.m_Job.JobScheduleCreatedBy = (this.m_UserBusinessEntity.DomainName.Equals(Constants.NA)) ? this.m_UserBusinessEntity.UserId : this.m_UserBusinessEntity.DomainName.ConcatStrings(new List<string> { Constants.PathSeperator, this.m_UserBusinessEntity.UserId });
                this.m_Job.JobTypeName = Constants.PrintJobTypeName;
                // Default settings
                this.m_Job.StatusBrokerType = BrokerType.Database;
                this.m_Job.CommitIntervalBrokerType = BrokerType.ConfigFile;
                this.m_Job.CommitIntervalSettingType = SettingType.CommonSetting;
                if (bootParameters != null)
                {
                    this.m_Job.BootParameters = bootParameters;
                    //Update the delivery status to "running" state
                    DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Running);
                    EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobInitializeSuccess, EventLogEntryType.Information);
                }
                else
                {
                    DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                    EvLog.WriteEntry(jobId + Constants.Colon + Constants.PrintJobInitialisation, Constants.JobXmlNotWellFormed, EventLogEntryType.Information);
                    throw new EVException().AddResMsg(ErrorCodes.XmlStringNotWellFormed);
                }
            }
            catch (EVException ex)
            {
                DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobInitializeFailed + " : " + ex.ToUserString() + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Information);
                LogException(this.JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInJobInitialization);
            }
            catch (Exception ex)
            {
                DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                //Handle exception in initialize
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobInitializeFailed + " : " + ex.Message, EventLogEntryType.Information);
                LogException(this.JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInJobInitialization);
            }
            return m_Job;
        }

        /// <summary>
        /// This is the overridden GenerateTasks() method. 
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount">integer</param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<BaseJobTaskBusinessEntity> GenerateTasks(BaseJobBEO jobParameters, out int previouslyCommittedTaskCount)
        {
            Tasks<BaseJobTaskBusinessEntity> tasks = new Tasks<BaseJobTaskBusinessEntity>();
            previouslyCommittedTaskCount = 0;
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobGenerateTaskStart, EventLogEntryType.Information);
                //De serialize the Boot parameters
                PrintToFileServiceRequestBEO request = (PrintToFileServiceRequestBEO)XmlUtility.DeserializeObject(jobParameters.BootParameters, typeof(PrintToFileServiceRequestBEO));
                int i = 0;
                //Construct The Task
                if (tasks.Count <= 0)
                {
                    foreach (BaseJobTaskBusinessEntity task in request.Documents.Select(t => new BaseJobTaskBusinessEntity()))
                    {
                        //Construct The Task
                        task.TaskNumber = ++i;
                        task.TaskComplete = false;
                        task.TaskPercent = 99.0 / request.Documents.Count;
                        tasks.Add(task);
                    }
                }
            }
            catch (EVException ex)
            {
                DeliveryOptionsBO.UpdateDeliveryStatus(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobGenerateTaskFailed + ":" + ex.ToUserString() + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Error);
                LogException(this.JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInGenerateTasks);
            }
            catch (Exception ex)
            {
                DeliveryOptionsBO.UpdateDeliveryStatus(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobGenerateTaskFailed + ":" + ex.Message + ":" + ex.InnerException + ":" + ex.StackTrace, EventLogEntryType.Error);
                LogException(this.JobLogInfo, ex, LogCategory.Job, string.Empty, ErrorCodes.ProblemInGenerateTasks);
                throw;
            }
            return tasks;
        }

        /// <summary>
        /// This is the overridden DoAtomicWork method.
        /// </summary>
        /// <param name="task">print Job Task</param>
        /// <param name="jobParameters">print Job Parameters</param>
        /// <returns>If Atomic work was successful</returns>
        protected override bool DoAtomicWork(BaseJobTaskBusinessEntity task, BaseJobBEO jobParameters)
        {
            #region Pre-condition asserts
            task.ShouldNotBe(null);
            jobParameters.ShouldNotBe(null);
            #endregion
            PrintToFileServiceRequestBEO request = null;
            bool success = true;
            if (task != null)
            {
                try
                {
                    request = (PrintToFileServiceRequestBEO)XmlUtility.DeserializeObject(jobParameters.BootParameters, typeof(PrintToFileServiceRequestBEO));
                    request.ShouldNotBe(null);
                    string printDocumentConfigurations = GetPrintDocumentConfigurations();
                    request.Documents.ShouldNotBe(null);
                    request.Documents.Count.ShouldBeGreaterThan(0);
                    success = PrintToFileBO.CreateTemperaryFolder(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), request.Documents[task.TaskNumber - 1],
                        request.Options.IncludeSeperatorSheet, task.TaskNumber, printDocumentConfigurations, request.RequestedBy.UserId, request.Options.DocumentSetId);
                    if (task.TaskNumber == request.Documents.Count)
                    {
                        bool isPDFCreated =
                            PrintToFileBO.MergePrintDocument(jobParameters.JobId.ToString(CultureInfo.InvariantCulture),
                                request.Options.IncludeSeperatorSheet, request.Options.TargetDocumentMimeType, printDocumentConfigurations, request.Options.PrinterId);
                        if (!isPDFCreated)
                        {
                            //Send Failure Notification
                            PrintToFileBO.SendPrintNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), string.Empty, false, string.Empty, string.Empty);
                        }
                    }
                    EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobDoAtomicWorkSuccess + Constants.ForTaskNumber + Constants.Colon + task.TaskNumber, EventLogEntryType.Information);
                }
                catch (EVException exp)
                {
                    //Send Failure Notification
                    string errDescription = exp.ToUserString();
                    DocumentIdentifierBEO documentData = null;
                    if (exp.GetErrorCode().Equals(ErrorCodes.RedactItPublishError))
                    {
                        // If the file is not supported by the conversion server, we will extract the file path from the 
                        //error message returned from the conversion server and will log the DCN for that failed document(s).
                        string fileName = exp.InnerException.Message.ToLower(CultureInfo.CurrentCulture).Replace(Constants.ErrorInPublishType, string.Empty).Replace(Constants.ErrorInExtension, string.Empty);
                        documentData = request.Documents.FirstOrDefault(x => x.NativeFilePath.ToLower(CultureInfo.CurrentCulture).Equals(fileName));
                        if (documentData != null && !string.IsNullOrWhiteSpace(documentData.DCN))
                        {
                            PrintToFileBO.SendPrintNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), string.Empty, false, string.Format(Constants.ErrorUnsupportedFormat, documentData.DCN), string.Empty);
                        }
                        else
                        {
                            //If conversion server is inaccessible then we will change the notification message accordingly
                            if (exp.InnerException.Message.ToLower(CultureInfo.CurrentCulture).Contains(Constants.ErrorIPCPort))
                            {
                                PrintToFileBO.SendPrintNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), string.Empty, false, Constants.ConversionServerDown, string.Empty);
                            }
                            else
                            {
                                //If some other exception came from the conversion server, we will log that message as in error message
                                PrintToFileBO.SendPrintNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), string.Empty, false, exp.InnerException.Message, string.Empty);
                            }
                        }
                    }
                    else
                    {
                        //If no native file(s) found for that document and the total number 
                        // of documents selected for this job is only one then we will log the DCN
                        errDescription = exp.GetErrorCode().Equals(ErrorCodes.NoNativeFilesFound) ? Constants.NoConversion : errDescription;
                        PrintToFileBO.SendPrintNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), string.Empty, false, errDescription, string.Empty);
                    }
                    DeliveryOptionsBO.UpdateDeliveryStatus(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                    EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobDoAtomicWorkFailed + " for Task Number -  : " + task.TaskNumber + exp.ToUserString() + ":" + exp.StackTrace, EventLogEntryType.Information);
                    if (request != null && request.Documents.Count > 0)
                    {
                        documentData = request.Documents[task.TaskNumber - 1];
                        this.TaskLogInfo.TaskKey = documentData.DCN;
                    }
                    LogException(this.TaskLogInfo, exp, LogCategory.Task, string.Empty, ErrorCodes.ProblemInDoAtomicWork);
                }
                catch (Exception exp)
                {
                    //Send Failure Notification
                    PrintToFileBO.SendPrintNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), string.Empty, false, string.Empty, string.Empty);
                    DeliveryOptionsBO.UpdateDeliveryStatus(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                    EvLog.WriteEntry(Constants.JobTypeName + Constants.Hypen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.EventJobDoAtomicWorkFailed + " for Task Number -  : " + task.TaskNumber + exp.Message + ":" + exp.StackTrace, EventLogEntryType.Information);
                    if (request != null && request.Documents.Count > 0)
                    {
                        DocumentIdentifierBEO documentIdentifier = request.Documents[task.TaskNumber - 1];
                        this.TaskLogInfo.TaskKey = documentIdentifier.DCN;
                    }
                    LogException(this.TaskLogInfo, exp, LogCategory.Task, string.Empty, ErrorCodes.ProblemInDoAtomicWork);
                }
            }
            #region Post-condition asserts
            task.ShouldNotBe(null);
            jobParameters.ShouldNotBe(null);
            #endregion
            return success;
        }

        /// <summary>
        /// Create Folders for Temporary storage of files for delivery options
        /// </summary>
        private void CreateFoldersForTemperoryStorage()
        {
            string sharedLocation = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.BaseSharedPath);

            if (Directory.Exists(sharedLocation))
            {
                string deliveryOptionsPath = Path.Combine(sharedLocation, Constants.DeliveryOptions);
                if (!Directory.Exists(deliveryOptionsPath))
                {
                    try
                    {
                        Directory.CreateDirectory(deliveryOptionsPath);
                    }
                    catch (Exception ex)
                    {
                        ex.AddResMsg(ErrorCodes.WriteAccessForSharePathIsDenied);
                        throw;
                    }
                }

                string sourceDirectoryPath = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.SourceDirectoryPath);
                if (!Directory.Exists(sourceDirectoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(sourceDirectoryPath);
                    }
                    catch (Exception ex)
                    {
                        ex.AddResMsg(ErrorCodes.WriteAccessForSharePathIsDenied);
                        throw;
                    }
                }

                string targetDirectoryPath = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.TargetDirectoryPath);
                if (!Directory.Exists(targetDirectoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(targetDirectoryPath);
                    }
                    catch (Exception ex)
                    {
                        ex.AddResMsg(ErrorCodes.WriteAccessForSharePathIsDenied);
                        throw;
                    }
                }
            }
            else
            {
                throw new EVException().AddResMsg(ErrorCodes.SourceDirectoryNotExists);
            }
        }

        /// <summary>
        /// Clean Shared Space
        /// </summary>
        private void CleanSharedSpace()
        {
            string sharedLocation = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.BaseSharedPath);
            sharedLocation = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.SourceDirectoryPath);
            DirectoryInfo dir = new DirectoryInfo(sharedLocation);
            DirectoryInfo[] directories = dir.GetDirectories();
            if (directories.Length > 0)
            {
                int interval = int.Parse(ConfigurationManager.AppSettings[Constants.CleanFolderInHours]);
                List<DirectoryInfo> directoriesToDelete = directories.Where(o => o.CreationTime <= DateTime.Now.AddHours(-interval)).ToList();
                if (directoriesToDelete.Count > 0)
                {
                    foreach (DirectoryInfo t in directoriesToDelete)
                    {
                        try
                        {
                            t.Delete(true);
                        }
                        catch (Exception ex)
                        {
                            EvLog.WriteEntry(Constants.ErrorInDeletingDirectory + t.FullName, Constants.ErrorInDeletingDirectory + t.FullName + Constants.DueTo + ex.Message, EventLogEntryType.Error);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Before job shuts down, Update the log.
        /// </summary>
        /// <param name="jobParameters">Job Business Object</param>
        protected override void Shutdown(BaseJobBEO jobParameters)
        {
            try
            {
                CleanSharedSpace();
            }
            catch (EVJobException ex)
            {
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                throw;
            }
            catch (Exception ex)
            {
                // Handle exception in Generate Tasks
                EvLog.WriteEntry(Constants.JobTypeName + MethodInfo.GetCurrentMethod().Name, ex.Message, EventLogEntryType.Error);
                EVJobException jobException = new EVJobException(ErrorCodes.ProblemInJobExecution, ex, JobLogInfo);
                throw (jobException);
            }
        }

        /// <summary>
        /// Get the configuration parameters used for print document
        /// </summary>
        /// <returns>string</returns>
        private static string GetPrintDocumentConfigurations()
        {
            string sharedLocation = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.BaseSharedPath);
            string sourceDirectoryPath = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.SourceDirectoryPath);
            string targetDirectoryPath = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.TargetDirectoryPath);
            string toReturn = string.Empty;
            try
            {
                toReturn = Constants.TargetDirectory.ConcatStrings(new List<string>
                                                                       {
                    Constants.PipeSeperator,
                    targetDirectoryPath,
                    Constants.Comma,
                    Constants.SourceDirectory,
                    Constants.PipeSeperator,
                    sourceDirectoryPath,
                    Constants.Comma,
                    Constants.RedactitUri,
                    Constants.PipeSeperator,
                    CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.QueueServerUrl),
                    Constants.Comma,
                    Constants.CallBackUri,
                    Constants.PipeSeperator,
                    CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.PrintCallBackUri),
                    Constants.Comma,
                    Constants.WaitAttemptsCount,
                    Constants.PipeSeperator,
                    ConfigurationManager.AppSettings[Constants.WaitAttemptsCount],
                    Constants.Comma,
                    Constants.DeleteSourceFiles,
                    Constants.PipeSeperator,
                    ConfigurationManager.AppSettings[Constants.DeleteSourceFiles],
                    Constants.Comma,
                    Constants.RedactItPostSupported,
                    Constants.PipeSeperator,
                    ConfigurationManager.AppSettings[Constants.RedactItPostSupported]                  
                });
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.GetPrintDocumentConfigurations, exp.Message + Constants.Colon + exp.StackTrace, EventLogEntryType.Error);
            }
            return toReturn;
        }


        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="logMsg">LogInfo</param>
        /// <param name="exp">exception received</param>
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="taskKey">Key to identify the Task, need for task log only</param>
        /// <param name="errorCode">string</param>
        private static void LogException(LogInfo logMsg, Exception exp, LogCategory category, string taskKey, string errorCode)
        {
            if (category == LogCategory.Job)
            {
                EVJobException jobException = new EVJobException(errorCode, exp, logMsg);
                throw (jobException);
            }
            else
            {
                logMsg.TaskKey = taskKey;
                EVTaskException jobException = new EVTaskException(errorCode, exp, logMsg);
                throw (jobException);
            }
        }

        #endregion
    }
}
