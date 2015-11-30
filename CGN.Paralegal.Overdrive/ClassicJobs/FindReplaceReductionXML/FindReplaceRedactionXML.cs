# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file=" FindandReplaceJob.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Bharani</author>
//      <description>
//          Backend process which does the global find and replace redaction reason in redactionxml,
//                      The assembly, containing this code will be invoked by the JObManagement services
//                      based on schedule
//      </description>
//      <changelog>
//          <date value="28-Jul-2010">created</date>
//          <date value="6/5/2012">Fix for bug 100692 & 100624 - babugx</date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------

# endregion

using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Business.ReviewerSearch;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace LexisNexis.Evolution.BatchJobs.FindReplaceRedactionXML
{
    [Serializable]
    public class FindReplaceRedactionXML : BaseJob<GlobalReplaceJobBEO, GlobalReplaceTaskBEO>
    {
        /// <summary>
        /// Initializes the job parameters
        /// </summary>
        /// <param name="jobId">Job Id</param>
        /// <param name="jobRunId">Run Id</param>
        /// <param name="bootParameters">bootParameters</param>
        /// <param name="createdBy">createdByGuid</param>
        /// <returns>GlobalReplaceJobBEO</returns>
        protected override GlobalReplaceJobBEO Initialize(int jobId, int jobRunId, string bootParameters,
            string createdBy)
        {
            GlobalReplaceJobBEO jobBeo;
            try
            {
                // Initialize the JobBEO
                jobBeo = new GlobalReplaceJobBEO
                {
                    JobId = jobId,
                    JobRunId = jobRunId,
                    JobScheduleCreatedBy = createdBy,
                    JobTypeName = Constants.Job_TYPE_NAME,
                    BootParameters = bootParameters,
                    JobName = Constants.JOB_NAME
                };
                //filling properties of the job parameter
                EvLog.WriteEntry(jobId + Constants.JOB_INITIALIZATION_KEY, Constants.JOB_INITIALIZATION_VALUE,
                    EventLogEntryType.Information);
                // Default settings
                jobBeo.StatusBrokerType = BrokerType.Database;
                jobBeo.CommitIntervalBrokerType = BrokerType.ConfigFile;
                jobBeo.CommitIntervalSettingType = SettingType.CommonSetting;
                //constructing GlobalReplaceBEO from boot parameter by de serializing
                var globalReplaceContextBeo = GetGlobalReplaceBEO(bootParameters);
                globalReplaceContextBeo.CreatedBy = createdBy;
                EvLog.WriteEntry(jobId + Constants.AUDIT_BOOT_PARAMETER_KEY, Constants.AUDIT_BOOT_PARAMETER_VALUE,
                    EventLogEntryType.Information);
                jobBeo.SearchContext = globalReplaceContextBeo.SearchContext;
                jobBeo.ActualString = globalReplaceContextBeo.ActualString;
                jobBeo.ReplaceString = globalReplaceContextBeo.ReplaceString;
            }
            catch (Exception exp)
            {
                exp.AddUsrMsg(jobId + Constants.EVENT_INITIALIZATION_EXCEPTION_VALUE);
                throw;
            }
            return jobBeo;
        }


        /// <summary>
        /// This is the overridden GenerateTasks() method. 
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount">int</param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<GlobalReplaceTaskBEO> GenerateTasks(GlobalReplaceJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            previouslyCommittedTaskCount = 0;
            try
            {
                EvLog.WriteEntry(Constants.JOB_NAME + " - " + jobParameters.JobId, Constants.AUDIT_GENERATE_TASK_VALUE,
                    EventLogEntryType.Information);
                var tasks = GetTaskList<GlobalReplaceJobBEO, GlobalReplaceTaskBEO>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;

                if (tasks.Count <= 0)
                {
                    var globalReplaceTaskBeo = new GlobalReplaceTaskBEO();
                    const int taskNumber = 1;
                    globalReplaceTaskBeo.TaskNumber = taskNumber;
                    globalReplaceTaskBeo.TaskComplete = false;
                    globalReplaceTaskBeo.TaskPercent = 100.00;
                    globalReplaceTaskBeo.SearchContext = jobParameters.SearchContext;
                    globalReplaceTaskBeo.ActualString = jobParameters.ActualString;
                    globalReplaceTaskBeo.ReplaceString = jobParameters.ReplaceString;
                    tasks.Add(globalReplaceTaskBeo);
                }
                return tasks;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + " : " + jobParameters.JobId + Constants.AUDIT_GENERATE_TASK_VALUE,
                    exp.Message, EventLogEntryType.Error);
                throw new EVException().AddUsrMsg(exp.ToUserString());
            }
        }


        /// <summary>
        /// This is the overridden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <returns>Status of the operation.</returns>
        protected override bool DoAtomicWork(GlobalReplaceTaskBEO task, GlobalReplaceJobBEO jobParameters)
        {
            var isSuccess = false;
            
            var redactionDataList = new List<KeyValuePair<string, string>>();
            try
            {
                EvLog.WriteEntry(Constants.JOB_NAME + " : " + jobParameters.JobId,
                    Constants.AUDIT_IN_DO_ATOMIC_WORK_VALUE, EventLogEntryType.Information);
                var baseReviewerSearchResultsBeo = RVWSearchBO.Search(task.SearchContext);
                if (baseReviewerSearchResultsBeo != null && baseReviewerSearchResultsBeo.ResultDocuments != null)
                {
                    var searchContext = task.SearchContext;
                    searchContext.PageSize = baseReviewerSearchResultsBeo.TotalRecordCount;
                    var reviewerSearchResultsBeo = RVWSearchBO.Search(task.SearchContext);
                    if (reviewerSearchResultsBeo != null && reviewerSearchResultsBeo.ResultDocuments != null)
                    {
                        foreach (var documentBeo in reviewerSearchResultsBeo.ResultDocuments)
                        {
                            var documentVault = new DocumentVaultManager();
                            try
                            {
                                documentVault.GetBinaryReferenceId(
                                    documentBeo.MatterID.ToString(CultureInfo.InvariantCulture),
                                    documentBeo.CollectionID, documentBeo.DocumentID, "4");
                            }
                            catch
                            {
                                continue;
                            }
                            var metaData = new DocumentMetaDataBEO
                            {
                                CollectionId = documentBeo.CollectionID,
                                DocumentReferenceId = documentBeo.DocumentID
                            };
                            var documentData =
                                documentVault.GetRedactionXmlFromVault(
                                    documentBeo.MatterID.ToString(CultureInfo.InvariantCulture), metaData.CollectionId,
                                    metaData.DocumentReferenceId);
                            var markupXml = new XmlDocument();
                            if (!string.IsNullOrEmpty(documentData.MarkupXml))
                            {
                                markupXml.LoadXml(documentData.MarkupXml);
                                var nodes = markupXml.GetElementsByTagName(Constants.MarkUpBlockout);
                                for (var i = 0; i < nodes.Count; i++)
                                {
                                    if (nodes[i].Attributes[Constants.MarkupRedactionComment].Value.Length > 0)
                                    {
                                        nodes[i].Attributes[Constants.MarkupRedactionComment].Value =
                                            nodes[i].Attributes[Constants.MarkupRedactionComment].Value.Replace(
                                                task.ActualString, task.ReplaceString);
                                    }
                                }
                                if (documentData.MarkupXml == markupXml.OuterXml) continue;
                                documentData.MarkupXml = markupXml.OuterXml;
                                documentVault.SaveMarkupInVault(documentData);

                                redactionDataList.Add(
                                    new KeyValuePair<string, string>(Constants.MarkupRedactionComment,
                                        task.ReplaceString));
                                
                                IEnumerable<DocumentBeo> documentBeos = new List<DocumentBeo>() {DocumentBO.ToDocumentBeo(documentData.DocumentId,redactionDataList)};
                                var indexManagerProxy = new IndexManagerProxy(documentBeo.MatterID, documentBeo.CollectionID);
                                indexManagerProxy.BulkUpdateDocumentsAsync((List<DocumentBeo>)documentBeos);

                                if (!isSuccess)
                                {
                                    break;
                                }

                               
                            }
                        }
                    }
                }
                EvLog.WriteEntry(Constants.JOB_NAME + " : " + jobParameters.JobId,
                    Constants.JobEndMessage + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                    EventLogEntryType.Information);
                return isSuccess;
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JOB_NAME + Constants.AUDIT_IN_DO_ATOMIC_WORK_VALUE, exp.Message,
                    EventLogEntryType.Error);
                throw new EVException().AddUsrMsg(exp.ToUserString());
            }
        }

        /// <summary>
        /// Method will call the Shutdown
        /// </summary>
        /// <param name="jobParameters">jobParameters</param>
        protected override void Shutdown(GlobalReplaceJobBEO jobParameters)
        {
            var globalReplaceContextBeo = GetGlobalReplaceBEO(jobParameters.BootParameters);
        }

        #region Helper Methods

        /// <summary>
        /// This method will return GetGlobalReplaceBEO out of the passed bootparamter
        /// </summary>
        /// <param name="bootParamter">String</param>
        /// <returns>GlobalReplaceBEO</returns>
        private GlobalReplaceBEO GetGlobalReplaceBEO(String bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            using (var stream = new StringReader(bootParamter))
            {
                //Creating xmlStream for xml serialization
                var xmlStream = new XmlSerializer(typeof (GlobalReplaceBEO));

                //De serialization of boot parameter to get profileBEO
                return (GlobalReplaceBEO) xmlStream.Deserialize(stream);
            }
        }

        #endregion
    }
}