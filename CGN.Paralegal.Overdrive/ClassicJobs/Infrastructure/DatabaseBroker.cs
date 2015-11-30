//---------------------------------------------------------------------------------------------------
// <copyright file="DatabaseBroker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the DatabaseBroker class for Jobs infrastructure projects.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Infrastructure.Jobs
{
    #region Namespaces
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using BusinessEntities;
    using DBManagement;
    using ExceptionManagement;
    using Infrastructure;
    #endregion

    /// <summary>
    /// Database access class that is be used by Job infrastructure to communicate with the database, if, database is used as the broker.
    /// </summary>
    public static class DatabaseBroker
    {
        #region Base Job Data Access Methods


        public static bool UpdateTaskCompletionStatus(int jobRunId)
        {
            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager();
            // Instantiate the stored procedure to update the job status.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureUpdateTaskCompletionStatus);
            db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
            db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
            db.ExecuteNonQuery(dbCommand);
            int iRowsUpdated = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated), CultureInfo.CurrentCulture);
            return iRowsUpdated > 0 ? Constants.Success : Constants.Failure;
        }
        /// <summary>
        /// Update the Job Status for all the tasks.
        /// </summary>
        /// <typeparam name="TaskType">TaskType</typeparam>
        /// <param name="jobRunId">jobRunId</param>
        /// <param name="progressPercent">progressPercent</param>
        /// <param name="taskId">taskId</param>
        /// <param name="issuedCommandId">issuedCommandId</param>
        /// <param name="taskStartTime">taskStartTime</param>
        /// <returns></returns>
        internal static bool UpdateJobStatus<TaskType>(int jobRunId, double progressPercent, int taskId, out int issuedCommandId, DateTime? taskStartTime, bool isCustomProgressPercentage)
        {
            // Initialize the issued command id to 0.
            issuedCommandId = Constants.None;

            // Serialize the tasks to XML.
            string statusXml = string.Empty;

            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager();
            // Instantiate the stored procedure to update the job status.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureUpdateOpenJobs);
            // Add input and output parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
            db.AddInParameter(dbCommand, Constants.InputParameterProgressPercent, DbType.Double, progressPercent);
            db.AddInParameter(dbCommand, Constants.InputParameterTaskId, DbType.Int32, taskId);
            db.AddInParameter(dbCommand, Constants.InputParameterCurrentStatusId, DbType.Int32, (int)JobController.JobStatus.Running);
            db.AddInParameter(dbCommand, Constants.InputParameterProgressUpdateSource, DbType.String, isCustomProgressPercentage ? "J" : "F");

            if (taskStartTime != null)
            {
                db.AddInParameter(dbCommand, Constants.InputparamTaskStartTime, DbType.DateTime, taskStartTime);
            }
            db.AddOutParameter(dbCommand, Constants.OutputParameterIssuedCommandId, DbType.Int32, 4);
            db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
            db.ExecuteNonQuery(dbCommand);
            int iRowsUpdated = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated), CultureInfo.CurrentCulture);

            if (0 == iRowsUpdated)
            {
                issuedCommandId = Constants.Running;
                return Constants.Success;
            }

            if (iRowsUpdated < 0)
            {
                issuedCommandId = Constants.Running;
                return Constants.Failure;
            }

            // Get the issued command id returned.
            issuedCommandId = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterIssuedCommandId), CultureInfo.CurrentCulture);

            // Return the output of the operation.
            return Constants.Success;
        }

        /// <summary>
        /// This method updates the job execution status. 
        /// </summary>
        /// <remarks>
        /// Execution statuses include: Loaded, Running, Stopped, Paused. 
        /// </remarks>
        /// <param name="jobRunId">Job Run Id.</param>
        /// <param name="jobExecutionStatusId">Job Execution Status Id.</param>
        /// <param name="issuedCommandId">Indicates the command id issued for the job.</param>
        /// <returns>Status of the update operation.</returns>
        internal static bool UpdateJobExecutionStatus(int jobRunId, int jobExecutionStatusId, out int issuedCommandId)
        {
            // Initialize the issued command id to 0.
            issuedCommandId = 0;

            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager();

            // Create a new instance of the DB manager.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureUpdateOpenJobs);

            // Add input and output parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
            db.AddInParameter(dbCommand, Constants.InputParameterCurrentStatusId, DbType.Int32, jobExecutionStatusId);
            db.AddOutParameter(dbCommand, Constants.OutputParameterIssuedCommandId, DbType.Int32, 4);
            db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
            // Execute the stored procedure.
            db.ExecuteNonQuery(dbCommand);
            int iRowsUpdated = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated), CultureInfo.CurrentCulture);

            if (0 == iRowsUpdated)
            {
                issuedCommandId = Constants.Running;
                return Constants.Success;
            }

            if (iRowsUpdated < 0)
            {
                issuedCommandId = Constants.Running;
                return Constants.Failure;
            }

            // Get the issued command id returned.
            issuedCommandId = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterIssuedCommandId), CultureInfo.CurrentCulture);

            // Return the output of the operation.
            return Constants.Success;
        } // End UpdateJobExecutionStatus()


        /// <summary>
        /// This method updates the job execution status. 
        /// </summary>
        /// <remarks>
        /// Execution statuses include: Loaded, Running, Stopped, Paused.
        /// </remarks>
        /// <param name="jobId">Job Id.</param>
        /// <param name="jobRunId">Job Run Id.</param>
        /// <param name="jobStatus">Job Status.</param>
        /// <returns>Status of the update operation.</returns>
        internal static bool UpdateJobFinalStatus(int jobId, int jobRunId, int jobStatus)
        {
            // Declare a local output variable.
            bool output = Constants.Failure;
            int returnFlag;
            bool result = false;

            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager();

            // Create a new instance of the DB manager.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureUpdateJobFinalStatus);

            // Add input and output parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobId);
            db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
            db.AddInParameter(dbCommand, Constants.InputParameterStatusId, DbType.Int32, jobStatus);
            db.AddInParameter(dbCommand, Constants.OutputParameterResultFlag, DbType.Int32, 4);

            // Execute the stored procedure.
            db.ExecuteNonQuery(dbCommand);
            result = Int32.TryParse(db.GetParameterValue(dbCommand, Constants.OutputParameterResultFlag).ToString(), NumberStyles.Integer, null, out returnFlag);
            if (result)
            {
                if (returnFlag > Constants.None)
                {
                    output = Constants.Success;
                } // End If
            }

            // Return the output of the operation.
            return output;
        } // End UpdateJobExecutionStatus()

        #endregion



        #region OtherMethods
        /// <summary>
        /// This method is used to Get the Details of a Job SubScription Details.
        /// </summary>
        /// <param name="JobID">Unique Identifier for a job</param>
        /// <returns>JobBusinessEntity</returns>
        public static JobBusinessEntity GetJobSubScriptionDetails(string jobID)
        {
            bool result; int returnFlag;
            JobBusinessEntity jobBEO;
            // Create a new instance of the DB manager.
            EVDbManager DBManager = new EVDbManager();

            DbCommand dbCommand = DBManager.GetStoredProcCommand(Constants.GetJobSubscriptionDetails);

            DBManager.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, Convert.ToInt32(jobID));
            DBManager.AddOutParameter(dbCommand, Constants.JobReturnFlagOutParameter, DbType.Int32, 4);

            DataSet dsJobs = DBManager.ExecuteDataSet(dbCommand);
            jobBEO = new JobBusinessEntity();
            result = Int32.TryParse(DBManager.GetParameterValue(dbCommand, Constants.JobReturnFlagOutParameter).ToString(), NumberStyles.Integer, null, out returnFlag);
            if (result)
            {
                if (dsJobs != null && dsJobs.Tables.Count > 0 && dsJobs.Tables[0].Rows.Count > 0)
                {
                    DataRow job = dsJobs.Tables[0].Rows[0];
                    jobBEO.Type = !Convert.IsDBNull(job[Constants.PropertyNameSubscriptionTypeId]) ? Convert.ToInt32(job[Constants.PropertyNameSubscriptionTypeId]) : 0;
                    jobBEO.TypeName = !Convert.IsDBNull(job[Constants.PropertyNameSubscriptionTypeName]) ? job[Constants.PropertyNameSubscriptionTypeName].ToString() : string.Empty;
                    jobBEO.FolderID = Convert.ToInt64(job[Constants.PropertyNameFolderId]);
                    jobBEO.FolderName = job[Constants.PropertyNameFolderName].ToString();
                    jobBEO.Visibility = Convert.ToBoolean(job[Constants.PropertyNameVisibility]);
                    jobBEO.Priority = Convert.ToInt32(job[Constants.PropertyNamePriority]);
                    jobBEO.Name = !Convert.IsDBNull(job[Constants.PropertyNameJobName]) ? job[Constants.PropertyNameJobName].ToString() : string.Empty;
                }
            }
            return jobBEO;
        }


        /// <summary>
        /// This method is used to Get the Status of a Job of the given JobID
        /// </summary>
        /// <param name="jobId">Unique Identifier for a job</param>
        /// <returns>int</returns>
        public static int GetJobStatus(string jobId)
        {
            int status = 0; int returnFlag;
            EVDbManager DBManager = new EVDbManager();

            DbCommand dbCommand = DBManager.GetStoredProcCommand("EV_JOB_Get_Status");

            DBManager.AddInParameter(dbCommand, Constants.JobInputParameterJobId, DbType.Int32, Convert.ToInt32(jobId));
            DBManager.AddOutParameter(dbCommand, "@out_iStatus", DbType.Int32, 4);
            DBManager.AddOutParameter(dbCommand, "@out_iFlag", DbType.Int32, 4);
            DBManager.ExecuteNonQuery(dbCommand);
            bool result = Int32.TryParse((DBManager.GetParameterValue(dbCommand, "@out_iFlag")).ToString(), NumberStyles.Integer, null, out returnFlag);
            if (result)
            {
                if (returnFlag == 1)
                {

                    status = Convert.ToInt32(DBManager.GetParameterValue(dbCommand, "@out_iStatus"));
                }
            }
            return status;
        }


        /// <summary>
        /// This method is used to Issue the command to a running job
        /// </summary>
        /// <param name="jobRunID">Unique Job ID</param>
        /// <param name="statusId">Job Status</param>
        /// <returns>bool</returns>
        public static bool UpdateJobStatus(int jobId, int statusId)
        {
            // Declare a local output variable.
            bool output = Constants.Success;
            int returnFlag;
            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager();
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.UpdateJobStatusProcedure);
            db.AddInParameter(dbCommand, Constants.ParameterInputJobId, DbType.Int32, jobId);
            db.AddInParameter(dbCommand, Constants.InputParameterJobStatus, DbType.Int32, statusId);
            db.AddOutParameter(dbCommand, Constants.OutputParameterReturnFlag, DbType.Int32, 4);
            db.ExecuteNonQuery(dbCommand);
            output = Int32.TryParse(db.GetParameterValue(dbCommand, Constants.OutputParameterReturnFlag).ToString(), NumberStyles.Integer, null, out returnFlag);
            dbCommand = null;
            return output;
        }


        /// <summary>
        ///  Used to insert Task Details in database
        /// </summary>
        /// <typeparam name="TaskType">TaskType</typeparam>
        /// <param name="taskList">List of Tasks</param>
        /// <param name="jobRunId">Job Run Id</param>
        /// <returns></returns>
        public static bool InsertTaskDetails(int jobRunId, int taskNumber, byte[] taskBinary)
        {
            // Declare a local output variable.
            bool output = Constants.Success;
            EVDbManager db = new EVDbManager();
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureInsertIntoTaskDetails);
            // Add input and output parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
            db.AddInParameter(dbCommand, Constants.InputParameterTaskId, DbType.Int32, taskNumber);
            db.AddInParameter(dbCommand, Constants.InputParameterTaskDetails, DbType.Binary, taskBinary);
            db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
            db.ExecuteNonQuery(dbCommand);
            // Get the issued command id returned.
            int iRowsUpdated = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated), CultureInfo.CurrentCulture);
            // Execute the stored procedure.
            if (iRowsUpdated <= Constants.None)
            {
                output = Constants.Failure;
            } // End If
            db = null;
            dbCommand = null;
            return output;
        }


        /// <summary>
        /// Serialize object collection into binary
        /// </summary>
        /// <param name="collection">List of TaskType object</param>
        /// <returns>Binary string</returns>
        public static byte[] SerializeObjectBinary<TaskType>(TaskType task)
        {
            try
            {
                byte[] output = null;
                Encoding utf16 = new UnicodeEncoding();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(memoryStream, task);
                    output = memoryStream.ToArray();
                }
                return output;
            }
            catch (Exception)
            {
                // Do Nothing!
                return null;
            }
        }

        /// <summary>
        /// Method to DeserializeObject to binary
        /// </summary>
        /// <typeparam name="TaskType"></typeparam>
        /// <param name="binaryObject"></param>
        /// <returns></returns>
        public static TaskType DeserializeObjectBinary<TaskType>(byte[] binaryObject)
        {
            TaskType task = default(TaskType);
            try
            {
                if (binaryObject != null)
                {
                    using (MemoryStream memoryStream = new MemoryStream(binaryObject))
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        task = (TaskType)binaryFormatter.Deserialize(memoryStream);

                    }
                }
                return task;
            }
            catch (Exception ex)
            {
                EvLog.WriteEntry("Get Binary Data Exception", ex.Message);
                return task;
            }
        }

        /// <summary>
        /// Get Task Details.
        /// </summary>
        /// <typeparam name="JobParametersType">JobParametersType</typeparam>
        /// <typeparam name="TaskType">TaskType</typeparam>
        /// <param name="jobParameters">jobParameters</param>
        /// <returns>Tasks<TaskType></returns>
        public static Tasks<TaskType> GetJobTaskDetails<JobParametersType, TaskType>(JobParametersType jobParameters) where TaskType : class, new()
        {
            Tasks<TaskType> tasks = new Tasks<TaskType>();
            int jobRunId = Convert.ToInt32(Helper.GetParameterValue(jobParameters, Constants.PropertyNameJobRunId), CultureInfo.CurrentCulture);
            EVDbManager db = new EVDbManager();
            // Instantiate the stored procedure to update the job status.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureGetTaskDetails);

            // Add input parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
            db.AddInParameter(dbCommand, Constants.InputParamertTaskDetailsRequired, DbType.Boolean, false);

            // Execute the stored procedure and obtain the result set from the database into a dataset.
            DataSet dsResult = db.ExecuteDataSet(dbCommand);
            TaskType task;
            if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[Constants.First].Rows.Count > 0)
            {
                Helper.SetParameterValue<JobParametersType, double>(jobParameters, Constants.PropertyNameJobProgressPercent, Convert.ToDouble(dsResult.Tables[Constants.First].Rows[Constants.First][Constants.TableOpenJobsColumnProgressPercent], CultureInfo.CurrentCulture));
                foreach (DataRow drResult in dsResult.Tables[Constants.First].Rows)
                {
                    byte[] binaryData = (byte[])drResult[Constants.TableOpenJobsColumnTaskDetails];
                    task = DeserializeObjectBinary<TaskType>(binaryData);
                    Helper.SetParameterValue<TaskType, int>(task, Constants.PropertyNameTaskNumber, Convert.ToInt32(drResult[Constants.TableOpenJobsColumnTaskId], CultureInfo.CurrentCulture));
                    Helper.SetParameterValue<TaskType, bool>(task, Constants.PropertyNameTaskComplete, Convert.ToBoolean(drResult[Constants.TableOpenJobsColumnIsComplete], CultureInfo.CurrentCulture));
                    Helper.SetParameterValue<TaskType, bool>(task, Constants.PropertyNameIsTaskError, Convert.ToBoolean(drResult[Constants.TableOpenJobsColumnIsError], CultureInfo.CurrentCulture));
                    tasks.Add(task);
                }
            }
            // Return the list of tasks.
            return tasks;
        }

        #region Save Erro Logs
        /// <summary>
        /// This method will be used to save the log information 
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="jobRunId">Job Run Id</param>
        /// <param name="jobLog">JobLog</param>
        /// <param name="user">User Id</param>
        /// <param name="isError">Is Error</param>
        /// <param name="errorCode">Error Code</param>
        /// <param name="isXmlLog">Is Xml</param>
        /// <returns>true if, information saved successfully, otherwise return false.</returns>
        public static bool JobLog(int jobId, int jobRunId, string jobLog, string user, bool isError, string errorCode, bool isXmlLog)
        {
            //Variable Decleration
            DbCommand dbCommand;
            int result;
            bool returnflag;
            EVDbManager DBManager = new EVDbManager();
            // Instantiate the stored procedure to update the job status.
            dbCommand = DBManager.GetStoredProcCommand(Constants.EvImDalSpSaveJobLog);
            // Add parameters to Command object
            DBManager.AddInParameter(dbCommand, Constants.ParamInJobId, DbType.Int64, jobId);
            DBManager.AddInParameter(dbCommand, Constants.ParamInJobRunId, DbType.Int32, jobRunId);
            DBManager.AddInParameter(dbCommand, Constants.ParamInJobLog, DbType.String, jobLog);
            DBManager.AddInParameter(dbCommand, Constants.ImportsParamInCreatedBy, DbType.String, user);
            DBManager.AddInParameter(dbCommand, Constants.ParamInIsError, DbType.Boolean, isError);
            DBManager.AddInParameter(dbCommand, Constants.ImportsParamInErrorCode, DbType.String, errorCode);
            DBManager.AddInParameter(dbCommand, Constants.ParamInIsxml, DbType.Boolean, isXmlLog);
            DBManager.AddOutParameter(dbCommand, Constants.ParamOutReturnFlag, DbType.Int32, 4);

            DBManager.ExecuteNonQuery(dbCommand);
            result = Convert.ToInt32(DBManager.GetParameterValue(dbCommand, Constants.ParamOutReturnFlag), CultureInfo.CurrentCulture);

            returnflag = result >= 0;
            DBManager = null;
            dbCommand = null;
            return returnflag;
        }

        /// <summary>
        /// Updates the task log.
        /// </summary>
        /// <param name="jobId">The job id.</param>
        /// <param name="jobRunId">The job run id.</param>
        /// <param name="taskNumber">The task number.</param>
        /// <param name="taskKey">The task key.</param>
        /// <param name="taskLog">The task log.</param>
        /// <param name="isError">if set to <c>true</c> [is error].</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="erroCode">ErrorCode</param>
        public static bool UpdateTaskLog(int jobId, int jobRunId, int taskNumber, string taskKey,
            string taskLog, bool isError, DateTime? startTime, DateTime? endTime, string errorCode)
        {
            //Variable Decleration
            DbCommand dbCommand;
            int result;
            bool returnflag;
            EVDbManager DBManager = new EVDbManager();
            // Set command object and SP
            dbCommand = DBManager.GetStoredProcCommand(Constants.EvJobUpdateTaskDetails);
            // Add parameters to Command object
            DBManager.AddInParameter(dbCommand, Constants.ParamInJobId, DbType.Int64, jobId);
            DBManager.AddInParameter(dbCommand, Constants.ParamInJobRunId, DbType.Int32, jobRunId);
            DBManager.AddInParameter(dbCommand, Constants.InputParameterTaskId, DbType.Int32, taskNumber);
            DBManager.AddInParameter(dbCommand, Constants.InputParameterTaskKey, DbType.String, taskKey);
            DBManager.AddInParameter(dbCommand, Constants.InputParameterTaskLog, DbType.String, taskLog);
            DBManager.AddInParameter(dbCommand, Constants.InputParameterIsError, DbType.Boolean, isError);
            DBManager.AddInParameter(dbCommand, Constants.InputParameterTaskStartTime, DbType.DateTime, startTime);
            DBManager.AddInParameter(dbCommand, Constants.InputParameterTaskEndTime, DbType.DateTime, endTime);
            DBManager.AddInParameter(dbCommand, Constants.ImportsParamInErrorCode, DbType.String, errorCode);

            DBManager.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
            DBManager.ExecuteNonQuery(dbCommand);
            result = Convert.ToInt32(DBManager.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated), CultureInfo.CurrentCulture);

            returnflag = result >= 0;
            DBManager = null;
            dbCommand = null;
            return returnflag;
        }
        #endregion



        /// <summary>
        /// Get Job Task Log Details.
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="jobRunId">Job Run Identifier</param>
        /// <param name="taskId">Task Identifier</param>
        /// <returns>LogInfo Xml</returns>
        public static string GetLogDetails(int jobId, int jobRunId, int taskId)
        {
            EVDbManager DBManager = new EVDbManager();
            DbCommand dbCommand = DBManager.GetStoredProcCommand(Constants.GetLogDetailsProcedure);
            DBManager.AddInParameter(dbCommand, Constants.JobInputParameterJobId, DbType.Int32, jobId);
            DBManager.AddInParameter(dbCommand, Constants.JobInputParameterJobRunId, DbType.Int32, jobRunId);
            DBManager.AddInParameter(dbCommand, Constants.JobInputParameterTaskId, DbType.Int32, taskId);
            DBManager.AddOutParameter(dbCommand, Constants.ParamOutTotalNoOfRecord, DbType.Int32, 4);
            JobBusinessEntity jobBeo = new JobBusinessEntity();
            DataSet dsJobs = DBManager.ExecuteDataSet(dbCommand);
            int totalNoOfRecords = Convert.ToInt32(DBManager.GetParameterValue(dbCommand, Constants.ParamOutTotalNoOfRecord), CultureInfo.CurrentCulture);

            if (totalNoOfRecords == -1)
            {
                return string.Empty;
            }
            if (dsJobs != null && dsJobs.Tables.Count > 0 && dsJobs.Tables[0].Rows.Count > 0)
            {
                return (Convert.IsDBNull(dsJobs.Tables[0].Rows[0][Constants.LogInformation]) ? string.Empty : Convert.ToString(dsJobs.Tables[0].Rows[0][Constants.LogInformation]));
            }

            return string.Empty;
        }

        #endregion


    } // End class DatabaseBroker
} // End namespace
