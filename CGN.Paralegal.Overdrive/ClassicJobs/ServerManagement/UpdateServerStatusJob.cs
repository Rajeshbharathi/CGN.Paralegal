# region File Header
// <copyright file="UpdateServerStatusJob.cs" company="LexisNexis">
//    Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Swamy</author>
//     <description>
//         This file contain job management overriden function
//      </description>
//      <changelog>
//          <date value="2/15/2013"> Fix for 130654</date>
//      </changelog>
// </header>
# endregion
#region Namespace
using System;
using System.Diagnostics;
using System.Globalization;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
#endregion

namespace LexisNexis.Evolution.BatchJobs.ServerManagement
{
    [Serializable]
    public class UpdateServerStatusJob : BaseJob<ServerStatusJobBusinessEntity, ServerStatusTaskBusinessEntity>
    {
        #region  " Job Management overridden Function"
        #region "Override version of Initialize method"

        /// <summary>
        /// This is the overridden Initialize() method.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="bootParameters">Boot Parameters.</param>
        /// <param name="createdBy"> </param>
        /// <returns>jobParameters</returns>
        protected override ServerStatusJobBusinessEntity Initialize(int jobId, int jobRunId, string bootParameters, string createdBy)
        {
            ServerStatusJobBusinessEntity jobParameters = null;
            try
            {                 
            EvLog.WriteEntry(Constants.JobName + " - " + jobId.ToString(CultureInfo.InvariantCulture), Constants.MethodInitialize, EventLogEntryType.Information);
                jobParameters = new ServerStatusJobBusinessEntity
                                    {
                                        JobId = jobId,
                                        JobRunId = jobRunId,
                                        JobName = Constants.JobName,
                                        BootParameters = bootParameters,
                                        StatusBrokerType = BrokerType.Database,
                                        CommitIntervalBrokerType = BrokerType.ConfigFile,
                                        CommitIntervalSettingType = SettingType.CommonSetting
                                    };
                UserBusinessEntity userBusinessEntity = UserBO.GetUserUsingGuid(createdBy);
            jobParameters.JobScheduleCreatedBy = (userBusinessEntity.DomainName.Equals("N/A")) ? userBusinessEntity.UserId : userBusinessEntity.DomainName + "\\" + userBusinessEntity.UserId;
            userBusinessEntity = null;
            jobParameters.JobTypeName = Constants.JobTypeName;
            EvLog.WriteEntry(Constants.JobName + " - " + jobId.ToString(CultureInfo.InvariantCulture), Constants.MethodInitialize + " - " + bootParameters, EventLogEntryType.Information);
            }
            catch (EVException ex)
            {
                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInJobInitialization, string.Empty);
            }
            catch (Exception ex)
            {

                LogException(JobLogInfo, ex, LogCategory.Job, ErrorCodes.ProblemInJobInitialization, string.Empty);
            }
                
          return jobParameters;
        }
        #endregion
        #region "Override version of GenerateTask method"

        /// <summary>
        /// This is the overridden GenerateTasks() method. 
        /// </summary>
        /// <param name="jobParameters">Input settings / parameters of the job.</param>
        /// <param name="previouslyCommittedTaskCount"> </param>
        /// <returns>List of tasks to be performed.</returns>
        protected override Tasks<ServerStatusTaskBusinessEntity> GenerateTasks(ServerStatusJobBusinessEntity jobParameters, out int previouslyCommittedTaskCount)
        {
            previouslyCommittedTaskCount = 0;
            Tasks<ServerStatusTaskBusinessEntity> tasks = null;
            try
            {
                EvLog.WriteEntry(Constants.JobName + " - " + jobParameters.JobId, Constants.GenerateTasksMethod, EventLogEntryType.Information);
                tasks = GetTaskList<ServerStatusJobBusinessEntity,ServerStatusTaskBusinessEntity>(jobParameters);
                previouslyCommittedTaskCount = tasks.Count;

                //Classic job UpdateServerStatus is not relevant anymore and should be removed from DB scripts which create it.
                if (tasks.Count > 0)
                {
                    ServerStatusTaskBusinessEntity serverStatusTask = new ServerStatusTaskBusinessEntity();
                    int taskNumber = 1;
                    serverStatusTask.TaskNumber = taskNumber;
                    serverStatusTask.TaskComplete = false;
                    serverStatusTask.TaskPercent = 100.00;
                    tasks.Add(serverStatusTask);
                }
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName+" : "+jobParameters.JobId +  Constants.GenerateTasksMethod, exp.Message,EventLogEntryType.Error);
                LogException(JobLogInfo, exp, LogCategory.Job, ErrorCodes.ProblemInGenerateTasks, string.Empty);              
            }
            return tasks;
        }
        #endregion
        #region "Overridden DoAtomicWork method"

        /// <summary>
        /// This is the overridden DoAtomicWork() method.
        /// </summary>
        /// <param name="task">A task to be performed.</param>
        /// <param name="jobParameters"> </param>
        /// <returns>Status of the operation.</returns>
        protected override bool DoAtomicWork(ServerStatusTaskBusinessEntity task, ServerStatusJobBusinessEntity jobParameters)
        {
            bool output = true;
            try
            {              
                EvLog.WriteEntry(Constants.JobName + " : " + jobParameters.JobId, Constants.DoAtomicWorkMethod, EventLogEntryType.Information);

                using (ServerStatusDataAccess dao = new ServerStatusDataAccess())
                {
                    dao.PingServer();
                    EvLog.WriteEntry(Constants.JobName + " : " + jobParameters.JobId, Constants.JobEndMessage + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), EventLogEntryType.Information);
                }
            }
            catch (Exception exp)
            {
                EvLog.WriteEntry(Constants.JobName + Constants.DoAtomicWorkMethod, exp.Message, EventLogEntryType.Error);
                LogException(TaskLogInfo, exp, LogCategory.Task, ErrorCodes.ProblemInDoAtomicWork,string.Empty);    
                output = false;
            }
            return output;
        }
         #endregion
        #endregion

        /// <summary>
        /// Logs the exception message into database..
        /// </summary>
        /// <param name="logInfo">Log information</param>
        /// <param name="exp">exception received</param>        
        /// <param name="category">To identify the job or task to log the message</param>
        /// <param name="errorCode"> </param>
        /// <param name="taskKey">taskKey</param>  
        private static void LogException(LogInfo logInfo, Exception exp, LogCategory category, string errorCode,string taskKey)
        {
            if (category == LogCategory.Job)
            {  
                EVJobException jobException = new EVJobException(errorCode,exp,logInfo);
                throw (jobException);
            }
            else
            {  logInfo.TaskKey = taskKey;
                EVTaskException taskException = new EVTaskException(errorCode, exp, logInfo);
                throw (taskException);
            }
        }
    }
}
