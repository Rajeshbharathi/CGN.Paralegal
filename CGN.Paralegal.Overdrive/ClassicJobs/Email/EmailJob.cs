#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="EmailJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Suneeth Senthil Jayapalan/Anandhi Rengasamy</author>
//      <description>
//          Actual backend Backend process which does the Email. The assembly,
//                      containing this code will be invoked by the JobManagement services
//                      based on the Email schedule
//      </description>
//      <changelog>
//          <date value="08/18/2010">created</date>
//          <date value="04/11/2010">Bug fix 98739</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
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
using LexisNexis.Evolution.Business.SendEmail;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;

#endregion

namespace LexisNexis.Evolution.BatchJobs.Email
{
    [Serializable]
    public class EmailJob : BaseJob<BaseJobBEO, BaseJobTaskBusinessEntity>
    {
        #region Private Fields

        private readonly BaseJobBEO m_Job; // Job level data 
        private UserBusinessEntity m_UserBusinessEntity;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public EmailJob()
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
        /// <param name="createdBy">Created By Guid</param>
        /// <returns>jobParameters</returns>
        protected override BaseJobBEO Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            m_UserBusinessEntity = null;
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_Initialize_Start, EventLogEntryType.Information);
                //Create Folders for Temporary Storage
                this.CreateFoldersForTemperoryStorage();
                //Clean Source Directory
                this.CleanSharedSpaceForSourceDirectory();
                //Clean Zip Files
                this.CleanZipFilesInSharedSpace();
                //filling properties of the job parameter
                this.m_Job.JobId = jobId;
                this.m_Job.JobRunId = jobRunId;
                this.m_Job.JobName = Constants.EmailJob;
                this.m_UserBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
                this.m_Job.JobScheduleCreatedBy = (this.m_UserBusinessEntity.DomainName.Equals(Constants.NotApplicable)) ? this.m_UserBusinessEntity.UserId : this.m_UserBusinessEntity.DomainName + Constants.PathSeperator + this.m_UserBusinessEntity.UserId;
                this.m_Job.JobTypeName = Constants.EmailDocuments;
                // Default settings
                this.m_Job.StatusBrokerType = BrokerType.Database;
                this.m_Job.CommitIntervalBrokerType = BrokerType.ConfigFile;
                this.m_Job.CommitIntervalSettingType = SettingType.CommonSetting;

                if (bootParameters != null)
                {
                    this.m_Job.BootParameters = bootParameters;
                }
                else
                {
                    DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);                    
                    throw new EVException().AddDbgMsg("{0}:{1}:{2}", jobId, Constants.EmailJobInitialisation, Constants.XmlNotWellFormed).AddResMsg(ErrorCodes.XmlStringNotWellFormed);
                }
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_Initialize_Success, EventLogEntryType.Information);
            }
            catch (EVException ex)
            {
                //Send Email Failure notification
                SendEmailBO.SendNotification(jobId.ToString(CultureInfo.InvariantCulture), false);
                DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_Initialize_Failed + Constants.Colon + ex.ToUserString(), EventLogEntryType.Information);
                this.JobLogInfo.AddParameters(Constants.Event_Job_Initialize_Failed + Constants.Colon + ex.ToUserString());
                EVJobException jobException = new EVJobException(ErrorCodes.ProblemInJobInitialization, ex, this.JobLogInfo);
                throw (jobException);

            }
            catch (Exception ex)
            {
                //Send Email Failure notification
                SendEmailBO.SendNotification(jobId.ToString(CultureInfo.InvariantCulture), false);
                //Update Delivery Status to Failed
                DeliveryOptionsBO.UpdateDeliveryStatus(jobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                //Handle exception in initialize
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_Initialize_Failed + Constants.Colon + ex.Message, EventLogEntryType.Information);
                this.JobLogInfo.AddParameters(Constants.Event_Job_Initialize_Failed + Constants.Colon + ex.Message);
                EVJobException jobException = new EVJobException(ErrorCodes.ProblemInJobInitialization, ex, this.JobLogInfo);
                throw (jobException);
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
            previouslyCommittedTaskCount = 0;
            Tasks<BaseJobTaskBusinessEntity> tasks = new Tasks<BaseJobTaskBusinessEntity>();
            try
            {
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_GenerateTask_Start, EventLogEntryType.Information);

                //De serialize the Boot parameters
                SendEmailServiceRequestBEO request = (SendEmailServiceRequestBEO)XmlUtility.DeserializeObject(jobParameters.BootParameters, typeof(SendEmailServiceRequestBEO));
                int i = 0;

                //Construct The Task
                if (tasks.Count <= 0)
                {
                    int docCount = request.Documents.Count;
                    for (int k = 0; k < docCount; k++)
                    {
                        BaseJobTaskBusinessEntity task = new BaseJobTaskBusinessEntity
                                                             {
                                                                 TaskNumber = ++i,
                                                                 TaskComplete = false,
                                                                 TaskPercent = (float)100 / docCount
                                                             };
                        //Construct The Task
                        tasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                //Send Email Failure notification
                SendEmailBO.SendNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), false);
                //Update Delivery Status to Failed
                DeliveryOptionsBO.UpdateDeliveryStatus(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_GenerateTask_Failed + ":" + ex.Message, EventLogEntryType.Error);
                //LogInfo logMsg = new LogInfo();
                JobLogInfo.AddParameters(Constants.Event_Job_GenerateTask_Failed + Constants.Colon + ex.Message);
                EVJobException jobException = new EVJobException(ErrorCodes.ProblemInGenerateTasks, ex, JobLogInfo);
                throw (jobException);
            }
            return tasks;
        }

        /// <summary>
        /// This is the overridden DoAtomicWork method.
        /// </summary>
        /// <param name="task">print Job Task</param>
        /// <param name="jobParameters">Email Job Parameters</param>
        /// <returns>If Atomic work was successful</returns>
        protected override bool DoAtomicWork(BaseJobTaskBusinessEntity task, BaseJobBEO jobParameters)
        {
            bool success = true;
            bool isSendPerformed = false;
            SendEmailServiceRequestBEO request = null;
            if (task != null)
            {
                bool isMailSent = false;
                try
                {
                    request = (SendEmailServiceRequestBEO)XmlUtility.DeserializeObject(jobParameters.BootParameters, typeof(SendEmailServiceRequestBEO));
                    string emailDocumentConfigurations = GetEmailDocumentConfigurations();
                    success = SendEmailBO.CreateTemperaryFolder(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), request.RequestedBy.UserId, request.Documents[task.TaskNumber - 1], emailDocumentConfigurations);
                    if (task.TaskNumber == request.Documents.Count)
                    {
                        isSendPerformed = true;
                        //Compress the temporary folder
                        SendEmailBO.CompressFolder(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), emailDocumentConfigurations);
                        //Construct Zip folder
                        isMailSent = SendEmailBO.ComposeAndSendEmail(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), request, emailDocumentConfigurations);
                        //Send Notification on success/failure of email job
                        SendEmailBO.SendNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), isMailSent);
                    }
                    EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_DoAtomicWork_Success + Constants.ForTaskNumber + task.TaskNumber, EventLogEntryType.Information);
                }
                catch (Exception exp)
                {
                    exp.Trace();
                    isMailSent = false;
                    //Send Notification on success/failure of email job
                    if (isSendPerformed)
                        SendEmailBO.SendNotification(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), isMailSent);
                    
                    //Update Delivery Status to Failed
                    DeliveryOptionsBO.UpdateDeliveryStatus(jobParameters.JobId.ToString(CultureInfo.InvariantCulture), (short)PrintToFileServiceStateBEO.Failed);
                    EvLog.WriteEntry(Constants.JobTypeName + Constants.Hyphen + jobParameters.JobId.ToString(CultureInfo.InvariantCulture), Constants.Event_Job_DoAtomicWork_Failed + Constants.ForTaskNumber + task.TaskNumber + Constants.Colon + exp.Message, EventLogEntryType.Error);

                    if (request != null && request.Documents.Count > 0)
                    {
                        DocumentIdentifierBEO documentIdentifier = request.Documents[task.TaskNumber - 1];
                        TaskLogInfo.TaskKey = SendEmailBO.GetDcnFieldValue(documentIdentifier.MatterId, documentIdentifier.CollectionId, documentIdentifier.DocumentId);
                    }
                    TaskLogInfo.AddParameters(Constants.Event_Job_DoAtomicWork_Failed + Constants.Colon + exp.Message);
                    if (task.TaskNumber == request.Documents.Count && !isSendPerformed)
                    {
                        EVJobException jobException = new EVJobException(ErrorCodes.ProblemInDoAtomicWork, exp, TaskLogInfo);
                        throw (jobException);
                    }
                    else
                    {
                        EVTaskException taskException = new EVTaskException(ErrorCodes.ProblemInDoAtomicWork, exp, TaskLogInfo);
                        throw (taskException);
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Before job shuts down, Update the log.
        /// </summary>
        /// <param name="jobParameters">Job Business Object</param>
        protected override void Shutdown(BaseJobBEO jobParameters)
        {
            try
            {
                //Clean Source Directory
                CleanSharedSpaceForSourceDirectory();
                //Clean Zip Files
                CleanZipFilesInSharedSpace();
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
        /// Clean Shared Space for storing source folder
        /// </summary>
        private void CleanSharedSpaceForSourceDirectory()
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
        /// Clean Zip files in shared space
        /// </summary>
        private void CleanZipFilesInSharedSpace()
        {
            string sharedLocation = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.BaseSharedPath);
            sharedLocation = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.TargetDirectoryPath);
            DirectoryInfo dir = new DirectoryInfo(sharedLocation);
            if (dir.Exists)
            {
                FileInfo[] files = dir.GetFiles();
                if (files.Length > 0)
                {
                    int interval = int.Parse(ConfigurationManager.AppSettings[Constants.CleanFolderInHours]);
                    List<FileInfo> filesToDelete = files.Where(o => o.Extension.Contains(Constants.ZipExtension) && o.CreationTime <= DateTime.Now.AddHours(-interval)).ToList();
                    if (filesToDelete.Count > 0)
                    {
                        foreach (FileInfo t in filesToDelete)
                        {
                            try
                            {
                                t.Delete();
                            }
                            catch (Exception ex)
                            {
                                EvLog.WriteEntry(Constants.ErrorInDeletingDirectory + t.FullName, Constants.ErrorInDeletingDirectory + t.FullName + Constants.DueTo + ex.Message, EventLogEntryType.Error);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Get the configuration parameters used for Email Documents
        /// </summary>
        /// <returns>string</returns>
        private static string GetEmailDocumentConfigurations()
        {
            string sharedLocation = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.BaseSharedPath);
            string sourceDirectoryPath = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.SourceDirectoryPath);
            string targetDirectoryPath = Path.Combine(Path.Combine(sharedLocation, Constants.DeliveryOptions), Constants.TargetDirectoryPath);
            string toReturn = Constants.TargetDirectory.ConcatStrings(new List<string>
                                                                          {
                                                                              Constants.PipeSeperator,
                                                                              targetDirectoryPath,
                                                                              Constants.Comma,
                                                                              Constants.SourceDirectory,
                                                                              Constants.PipeSeperator,
                                                                              sourceDirectoryPath,
                                                                              Constants.Comma,
                                                                              Constants.WaitTimeToCheckCompressedFiles,
                                                                              Constants.PipeSeperator,
                                                                              ConfigurationManager.AppSettings[Constants.WaitTimeToCheckCompressedFiles],
                                                                              Constants.Comma,
                                                                              Constants.DeleteSourceFiles,
                                                                              Constants.PipeSeperator,
                                                                              ConfigurationManager.AppSettings[Constants.DeleteSourceFiles]
                                                                          });
            return toReturn;
        }

        #endregion
    }
}
