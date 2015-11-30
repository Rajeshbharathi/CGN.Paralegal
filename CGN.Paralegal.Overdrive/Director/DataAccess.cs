using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.DBManagement;

namespace LexisNexis.Evolution.Overdrive
{
    internal class DataAccess : IDisposable
    {
        EVDbManager _db = new EVDbManager(Constants.ConfigKeyDatabaseToUse);

        #region Get next job from load queue

        /// <summary>
        /// This method can be used to get the next job from queue.
        /// </summary>
        /// <param name="jobTypeId">Job Type Id.</param>
        /// <param name="serverId">Job Server Id.</param>
        /// <returns>Next job to run.</returns>
        internal BaseJobBEO GetNextJobFromQueue(int jobTypeId, Guid serverId)
        {
            try
            {
                DataSet dsResult;
                lock (_db)
                {
                    // Instantiate the stored procedure to obtain the jobs from load queue.
                    var dbCommand = _db.GetStoredProcCommand(Constants.StoredProcedureGetNextJobFromLoadQueue);
                    // Add input parameters.
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobTypeId, DbType.Int32, jobTypeId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobServerId, DbType.Guid, serverId);
                    // Execute the stored procedure and obtain the result set from the database into a dataset.
                    dsResult = _db.ExecuteDataSet(dbCommand);
                }
                // If there is a job to run then set the job parameters appropriately.
                BaseJobBEO jobParameters = null;
                if (null != dsResult)
                {
                    if (dsResult.Tables.Count > 0 && dsResult.Tables[Constants.First].Rows.Count > 0)
                    {
                        // Create an instance of the job parameters.
                        jobParameters = new BaseJobBEO();
                        // Set the job parameters.
                        var drResult = dsResult.Tables[Constants.First].Rows[Constants.First];
                        jobParameters.JobId = Convert.ToInt32(drResult[Constants.TableLoadJobQueueColumnJobId]);
                        jobParameters.JobRunId = Convert.ToInt32(drResult[Constants.TableLoadJobQueueColumnJobRunId]);
                        jobParameters.BootParameters =
                            !Convert.IsDBNull(drResult[Constants.TableLoadJobQueueColumnJobParameters])
                                ? Convert.ToString(drResult[Constants.TableLoadJobQueueColumnJobParameters])
                                : String.Empty;
                        jobParameters.JobScheduleRunDuration =
                            Convert.ToInt32(drResult[Constants.TableLoadJobQueueColumnJobDurationMinutes]);
                        jobParameters.JobTypeId = jobTypeId;
                        jobParameters.JobNotificationId =
                            !DBNull.Value.Equals(drResult[Constants.TableJobMasterNotificationId])
                                ? Convert.ToInt64(drResult[Constants.TableJobMasterNotificationId])
                                : 0;
                        jobParameters = GetJobDetails(jobParameters);
                    }
                }
                // Return the job parameters for the next job to run.
                return jobParameters;
            }
            catch (Exception ex)
            {
                ex.Trace();
                return null; // Calling method handles this gracefully.
            }
        }

        #endregion

        #region Get job details

        /// <summary>
        /// This method can be used to get the next job from queue.
        /// </summary>
        /// <param name="jobDetails">BaseJobBEO</param>
        /// <returns>BaseJobBEO</returns>
        internal BaseJobBEO GetJobDetails(BaseJobBEO jobDetails)
        {
            try
            {
                if (null != jobDetails)
                {
                    DataSet dsResult;
                    lock (_db)
                    {
                        // Instantiate the stored procedure to obtain the jobs from load queue.
                        var dbCommand = _db.GetStoredProcCommand(Constants.StoredProcedureGetFromJobMaster);
                        // Add input parameters.
                        _db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobDetails.JobId);
                        // Execute the stored procedure and obtain the result set from the database into a dataset.
                        dsResult = _db.ExecuteDataSet(dbCommand);
                    }
                    // If there is a job to run then set the job parameters appropriately.
                    if (null != dsResult)
                    {
                        if (dsResult.Tables.Count > 0 && dsResult.Tables[Constants.First].Rows.Count > 0)
                        {
                            // Set the job parameters.
                            var drResult = dsResult.Tables[Constants.First].Rows[Constants.First];
                            jobDetails.JobScheduleCreatedBy =
                                drResult[Constants.TableJobMasterColumnCreatedBy].ToString();
                            jobDetails.JobNotificationId =
                                !Convert.IsDBNull(drResult[Constants.TableJobMasterColumnNotfnId])
                                    ? Convert.ToInt64(drResult[Constants.TableJobMasterColumnNotfnId])
                                    : 0;
                            jobDetails.JobFrequency =
                                drResult[Constants.TableJobMasterColumnRecurrenceType].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Unable to get the job details.");
                ex.Trace();
            }
            // Return the job parameters for the next job to run.
            return jobDetails;
        }

        #endregion

        #region Get Active Job Info

        /// <summary>
        /// This method gets the active job information.
        /// </summary>
        /// <typeparam name="JobParametersType">The business entity type for a job.</typeparam>
        /// <param name="jobParameters">Job input parameters / settings.</param>
        /// <returns>Active job information.</returns>
        internal JobParametersType GetActiveJobInfo<JobParametersType>(JobParametersType jobParameters) where JobParametersType : class
        {
            try
            {
                if (null != jobParameters)
                {
                    // Get the job id from the job parameters / settings.
                    var jobId = Convert.ToInt32(GetParameterValue(jobParameters, Constants.PropertyNameJobId),
                                                CultureInfo.CurrentCulture);
                    // Get the job run id from the job parameters / settings.
                    var jobRunId = Convert.ToInt32(GetParameterValue(jobParameters, Constants.PropertyNameJobRunId),
                                                   CultureInfo.CurrentCulture);
                    DataSet dsResult;
                    lock (_db)
                    {
                        // Instantiate the stored procedure to update the job status.
                        var dbCommand = _db.GetStoredProcCommand(Constants.StoredProcedureGetJobStatus);
                        // Add input parameters.
                        _db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobId);
                        // Add input parameters.
                        _db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
                        // Execute the stored procedure and obtain the result set from the database into a dataset.
                        dsResult = _db.ExecuteDataSet(dbCommand);
                    }
                    // If dataset contains data, then get the list of tasks from the dataset.
                    if (null != dsResult)
                    {
                        if (dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                        {
                            var drResult = dsResult.Tables[Constants.First].Rows[Constants.First];
                            SetParameterValue(jobParameters, Constants.PropertyNameJobProgressPercent,
                                              Convert.ToDouble(
                                                  drResult[
                                                      Constants.TableOpenJobsColumnProgressPercent],
                                                  CultureInfo.CurrentCulture));
                            SetParameterValue(jobParameters, Constants.PropertyNameCurrentStatusId,
                                              Convert.ToInt32(
                                                  drResult[Constants.TableOpenJobsColumnCurrentStatusId],
                                                  CultureInfo.CurrentCulture));
                            SetParameterValue(jobParameters, Constants.PropertyNameIssuedCommandId,
                                              Convert.ToInt32(
                                                  drResult[Constants.TableOpenJobsColumnIssuedCommandId],
                                                  CultureInfo.CurrentCulture));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to get job info.");
                ex.Trace();
            }
            // Return the job parameters with the active job information.
            return jobParameters;
        }

        #endregion

        #region Get all active jobs info

        /// <summary>
        /// This method gets the job information for the specified list of active jobs.
        /// </summary>
        /// <typeparam name="JobParametersType">The business entity type for a job.</typeparam>
        /// <param name="jobParametersList">Inputs identifying list of jobs. Job Id and Job Run Id.</param>
        /// <returns>Job info of the provided active jobs list.</returns>
        internal List<JobParametersType> GetAllActiveJobsInfo<JobParametersType>(
            List<JobParametersType> jobParametersList) where JobParametersType : class
        {
            var jobParametersInfoList = new List<JobParametersType>();
            if (null != jobParametersList && jobParametersList.Count > 0)
            {
                try
                {
                    jobParametersInfoList.AddRange(jobParametersList.Select(jobParameters => GetActiveJobInfo<JobParametersType>(jobParameters)));
                }
                catch (Exception ex)
                {
                    ex.AddUsrMsg("Unable to get active job info.").Trace().Swallow();
                }
            }
            return jobParametersInfoList;
        }

        // End GetAllActiveJobsInfo()

        #endregion

        #region Update job status

        /// <summary>
        /// This method is used to update the status of a job.
        /// </summary>
        /// <param name="jobId">Job Id</param>
        /// <param name="statusId">Job Status</param>
        /// <returns>bool</returns>
        public bool UpdateJobStatus(int jobId, int statusId)
        {
            try
            {
                lock (_db)
                {
                    int returnFlag;
                    var dbCommand = _db.GetStoredProcCommand(Constants.UpdateJobStatusProcedure);
                    _db.AddInParameter(dbCommand, Constants.ParameterInputJobId, DbType.Int32, jobId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobStatus, DbType.Int32, statusId);
                    _db.AddOutParameter(dbCommand, Constants.OutputParameterReturnFlag, DbType.Int32, 4);
                    _db.ExecuteNonQuery(dbCommand);
                    var output = Int32.TryParse(
                        _db.GetParameterValue(dbCommand, Constants.OutputParameterReturnFlag).ToString(),
                        NumberStyles.Integer,
                        null, out returnFlag);
                    return output;
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to update completion status for Job Id: {0}.", jobId);
                ex.Trace();
                return false;
            }
        }

        #endregion

        #region Update Job Execution Status

        /// <summary>
        /// This method is used to update the job execution status.
        /// </summary>
        /// <param name="jobRunId">Job Run Id.</param>
        /// <param name="jobExecutionStatusId">Job Execution Status Id.</param>
        /// <param name="issuedCommandId">Issued Command Id.</param>
        /// <returns>bool</returns>
        public bool UpdateJobExecutionStatus(int jobRunId, int jobExecutionStatusId, out int issuedCommandId)
        {
            issuedCommandId = 0;
            try
            {
                lock (_db)
                {
                    var dbCommand = _db.GetStoredProcCommand(Constants.StoredProcedureUpdateOpenJobs);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterCurrentStatusId, DbType.Int32,
                                       jobExecutionStatusId);
                    _db.AddOutParameter(dbCommand, Constants.OutputParameterIssuedCommandId, DbType.Int32, 4);
                    _db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
                    _db.ExecuteNonQuery(dbCommand);
                    var iRowsUpdated =
                        Convert.ToInt32(_db.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated),
                                        CultureInfo.CurrentCulture);
                    issuedCommandId =
                        Convert.ToInt32(_db.GetParameterValue(dbCommand, Constants.OutputParameterIssuedCommandId),
                                        CultureInfo.CurrentCulture);
                    return iRowsUpdated >= Constants.None;
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to update execution status for Job Run Id: {0}.", jobRunId);
                ex.Trace();
                return false;
            }
        }

        #endregion

        #region Update Final Job Status

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
        internal bool UpdateJobFinalStatus(int jobId, int jobRunId, int jobStatus)
        {
            try
            {
                lock (_db)
                {
                    var output = false;
                    int returnFlag;
                    var dbCommand = _db.GetStoredProcCommand(Constants.StoredProcedureUpdateJobFinalStatus);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterStatusId, DbType.Int32, jobStatus);
                    _db.AddInParameter(dbCommand, Constants.OutputParameterReturnFlag, DbType.Int32, 4);
                    _db.ExecuteNonQuery(dbCommand);
                    var result = Int32.TryParse(_db.GetParameterValue(dbCommand, Constants.OutputParameterReturnFlag).ToString(), NumberStyles.Integer, null, out returnFlag);
                    if (result && returnFlag > Constants.None)
                        output = true;
                    return output;
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to update completion status for Job Id: {0} | Job Run Id: {1}.", jobId, jobRunId);
                ex.Trace();
                return false;
            }
        }

        #endregion

        #region Update progress percentage

        /// <summary>
        /// This method will update only the progress percentage.
        /// </summary>
        /// <param name="jobRunId">Job Run Id</param>
        /// <param name="progressPercent">Progress Percentage</param>
        /// <returns>true if update successfully, otherwise return false</returns>
        public bool UpdateProgressPercentage(int jobRunId, double progressPercent)
        {

            try
            {
                lock (_db)
                {
                    var dbCommand = _db.GetStoredProcCommand(Constants.StoredProcedureUpdateJobPercentage);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobRunId, DbType.Int32, jobRunId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterProgressPercent, DbType.Double,
                                       progressPercent);
                    _db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated, DbType.Int32, 4);
                    _db.ExecuteNonQuery(dbCommand);
                    var rowsUpdated =
                        Convert.ToInt32(_db.GetParameterValue(dbCommand, Constants.OutputParameterRowsUpdated),
                                        CultureInfo.CurrentCulture);
                    return rowsUpdated > 0;
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to update completion status for Job Run Id: {1}.", jobRunId);
                ex.Trace();
                return false;
            }
        }

        #endregion

        #region Update Job Command

        public bool UpdateJobCommand(int jobRunId, int commandId)
        {
            try
            {
                lock (_db)
                {
                    var dbCommand =
                        _db.GetStoredProcCommand(
                            Constants.StoredProcedureUpdateIssueCommandOpenJobs);
                    _db.AddInParameter(dbCommand, Constants.InputParameterJobRunId1, DbType.Int32,
                                       jobRunId);
                    _db.AddInParameter(dbCommand, Constants.InputParameterIssuedCommandId,
                                        DbType.Int32, commandId);
                    _db.AddOutParameter(dbCommand, Constants.OutputParameterRowsUpdated,
                                        DbType.Int32, 4);
                    _db.ExecuteNonQuery(dbCommand);
                    var iRowsUpdated =
                        Convert.ToInt32(
                            _db.GetParameterValue(dbCommand,
                                                  Constants.OutputParameterRowsUpdated),
                            CultureInfo.CurrentCulture);
                    return iRowsUpdated > 0;
                }
            }
            catch (Exception)
            {
                Tracer.Error("Failed to update the command for Job Run Id: {0}.", jobRunId);
                return false;
            }

        }

        #endregion

        #region Get Job Subscription Details

        public JobBusinessEntity GetJobSubScriptionDetails(int jobId)
        {
            var jobSubScriptionDetails = new JobBusinessEntity();
            try
            {
                lock (_db)
                {
                    int returnFlag;
                    var dbCommand = _db.GetStoredProcCommand(Constants.GetJobSubscriptionDetails);

                    _db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobId);
                    _db.AddOutParameter(dbCommand, Constants.JobReturnFlagOutParameter, DbType.Int32, 4);

                    var dsJobs = _db.ExecuteDataSet(dbCommand);
                    var result =
                        Int32.TryParse(
                            _db.GetParameterValue(dbCommand, Constants.JobReturnFlagOutParameter).ToString(),
                            NumberStyles.Integer, null, out returnFlag);
                    if (result)
                    {
                        if (null != dsJobs &&
                            dsJobs.Tables.Count > 0 &&
                            dsJobs.Tables[0].Rows.Count > 0)
                        {
                            var job = dsJobs.Tables[0].Rows[0];
                            jobSubScriptionDetails.Type =
                                !Convert.IsDBNull(job[Constants.PropertyNameSubscriptionTypeId])
                                    ? Convert.ToInt32(job[Constants.PropertyNameSubscriptionTypeId])
                                    : 0;
                            jobSubScriptionDetails.TypeName =
                                !Convert.IsDBNull(job[Constants.PropertyNameSubscriptionTypeName])
                                    ? job[Constants.PropertyNameSubscriptionTypeName].ToString()
                                    : string.Empty;
                            jobSubScriptionDetails.FolderID = Convert.ToInt64(job[Constants.PropertyNameFolderId]);
                            jobSubScriptionDetails.FolderName = job[Constants.PropertyNameFolderName].ToString();
                            jobSubScriptionDetails.Visibility =
                                Convert.ToBoolean(job[Constants.PropertyNameVisibility]);
                            jobSubScriptionDetails.Priority = Convert.ToInt32(job[Constants.PropertyNamePriority]);
                            jobSubScriptionDetails.Name = !Convert.IsDBNull(job[Constants.PropertyNameJobName])
                                                              ? job[Constants.PropertyNameJobName].ToString()
                                                              : string.Empty;
                        }
                    }
                    return jobSubScriptionDetails;
                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Failed to get the job subscription details for Job Id: {0}.", jobId);
                ex.Trace();
                return jobSubScriptionDetails;
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// This method sets the value of a specified property of a specified type instance.
        /// </summary>
        /// <typeparam name="T">Type of class instance whose property value is being set.</typeparam>
        /// <typeparam name="U">Type of the value being set for a property.</typeparam>
        /// <param name="classInstance">Instance whose property is being set.</param>
        /// <param name="propertyName">Name of the property to be set.</param>
        /// <param name="value">Value of the property to be set.</param>
        internal static void SetParameterValue<T, U>(T classInstance, string propertyName, U value)
        {
            classInstance.GetType().GetProperty(propertyName).SetValue(classInstance, value, null);
        }

        /// <summary>
        /// This method gets the value of a specified property of a specified type instance.
        /// </summary>
        /// <typeparam name="T">Type of class instance whose property value is being read.</typeparam>
        /// <param name="classInstance">Instance whose property is being read.</param>
        /// <param name="propertyName">Name of the property to be read.</param>
        /// <returns>Value of the property.</returns>
        internal static object GetParameterValue<T>(T classInstance, string propertyName)
        {
            return classInstance.GetType().GetProperty(propertyName).GetValue(classInstance, null);
        }

        #endregion

        #region Data access constants



        #endregion

        public void Dispose()
        {
            _db = null;
        }
    }
}