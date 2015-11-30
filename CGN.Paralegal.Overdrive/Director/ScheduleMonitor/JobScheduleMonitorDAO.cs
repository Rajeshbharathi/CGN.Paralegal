//---------------------------------------------------------------------------------------------------
// <copyright file="JobScheduleMonitorDAO.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the JobScheduleMonitorDAO class.
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Overdrive.ScheduleMonitor
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using Infrastructure.DBManagement;

    #endregion

    /// <summary>
    /// This class is used by the Job Schedule Monitor to query database for job schedules.
    /// </summary>
    internal static class JobScheduleMonitorDAO
    {
        /// <summary>
        /// This method can be used to retrieve the list jobs to be loaded based on their schedule.
        /// </summary>
        /// <returns>List of jobs to be loaded.</returns>
        internal static List<JobSchedule> GetJobsToLoad()
        {
            // Create an instance of the JobScheduleMonitorBEO.
            List<JobSchedule> jobsToLoad = null;

            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager(Constants.ConfigKeyDatabaseToUse);

            // Instantiate the stored procedure to obtain the jobs from load queue.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureGetJobsToLoad);
            // Add input parameters. 
            // RK - Changed the sp to work for both the functions. Need to send DBNull.Value for JobID.
            // Arun - This is incorrect. I've no idea why this SP needs JobId!
            db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, DBNull.Value);

            // Execute the stored procedure and obtain the result set from the database into a dataset.
            DataSet dsResult = db.ExecuteDataSet(dbCommand);

            // If there are jobs to load then obtain the schedules.
            if (dsResult != null && dsResult.Tables.Count > Constants.None)
            {

                // Create an instance of job schedules list.
                jobsToLoad = new List<JobSchedule>();

                // Declare a job schedule object to represent a current job schedule.
                JobSchedule currentJob;

                // Declare a job schedule object to represent the previous job schedule while processing in a loop.
                JobSchedule previousJob = null;

                // Declare an integer to store the current job id.
                int currentJobId = Constants.None;

                // For each job that has to be loaded process the schedule information.
                foreach (DataRow drResult in dsResult.Tables[Constants.First].Rows)
                {
                    // Create an instance of the current job schedule.
                    currentJob = new JobSchedule();

                    // Get the current job id.
                    currentJobId = Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnJobId]);

                    // If this is the first job or if this is a different job from the previous job then create a new instance for the current job.
                    if ((previousJob == null) || (previousJob.JobId != currentJobId))
                    {
                        // Set the job id.
                        currentJob.JobId = currentJobId;

                        // Set the job name.
                        currentJob.JobName = Convert.ToString(drResult[Constants.TableJobMasterColumnJobName]);

                        // Set the job server id.
                        currentJob.JobServerId = !Convert.IsDBNull(drResult[Constants.TableJobMasterColumnJobServerId]) ? new Guid(Convert.ToString(drResult[Constants.TableJobMasterColumnJobServerId])) : Guid.Empty;

                        // Set the job server id.
                        currentJob.BootParameters = !Convert.IsDBNull(drResult[Constants.TableJobMasterColumnJobParameters]) ? Convert.ToString(drResult[Constants.TableJobMasterColumnJobParameters]) : string.Empty;

                        // Set the job type id.
                        currentJob.JobTypeId = Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnJobTypeId]);

                        // Set the job type name.
                        currentJob.JobTypeName = Convert.ToString(drResult[Constants.TableJobTypeMasterColumnJobRunId]);

                        // Set the job start date.
                        currentJob.JobStartDate = Convert.ToDateTime(drResult[Constants.TableJobScheduleMasterColumnJobStartDate]);

                        // Set the hourly repeat interval. If the value is null set it to 0.
                        currentJob.Hourly = !Convert.IsDBNull(drResult[Constants.TableJobScheduleMasterColumnHourly]) ? Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnHourly]) : Constants.None;

                        // Set the daily repeat interval. If the value is null set it to 0.
                        currentJob.Daily = !Convert.IsDBNull(drResult[Constants.TableJobScheduleMasterColumnDaily]) ? Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnDaily]) : Constants.None;

                        // Set the requested recurrence count for the job. If the value is null set it to 0.
                        currentJob.RequestedRecurrenceCount = !Convert.IsDBNull(drResult[Constants.TableJobScheduleMasterColumnRequestedRecurrenceCount]) ? Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnRequestedRecurrenceCount]) : Constants.None;

                        // Set the requested occurence count for the job. If the value is null set it to 0.
                        currentJob.ActualOccurenceCount = !Convert.IsDBNull(drResult[Constants.TableJobScheduleMasterColumnActualOccurrenceCount]) ? Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnActualOccurrenceCount]) : 0;

                        // Set the job next run date.
                        currentJob.NextRunDate = !Convert.IsDBNull(drResult[Constants.TableJobScheduleMasterColumnNextRunDate]) ? Convert.ToDateTime(drResult[Constants.TableJobScheduleMasterColumnNextRunDate]) : Constants.MinDate;

                        // Set the job run duration.
                        currentJob.JobRunDuration = !Convert.IsDBNull(drResult[Constants.TableJobScheduleMasterColumnDurationMinutes]) ? Convert.ToInt32(drResult[Constants.TableJobScheduleMasterColumnDurationMinutes]) : Constants.MinutesInOneYear;

                        // Set the previous job instance to current job.
                        previousJob = currentJob;
                    }
                    else
                    {
                        // If this is not the first job and if this is the same job as the previous job set the current job instance to the previous job.
                        currentJob = previousJob;
                    } // End if

                    // Create an instance of the job schedule details.
                    JobScheduleDetails jobScheduleDetails = new JobScheduleDetails
                                                                {
                                                                    WeekMonthIndicator =
                                                                        Convert.ToString(
                                                                            drResult[
                                                                                Constants.
                                                                                    TableJobScheduleDetailsColumnWeekMonthIndicator
                                                                                ]),
                                                                    DayDateIndicator =
                                                                        Convert.ToString(
                                                                            drResult[
                                                                                Constants.
                                                                                    TableJobScheduleDetailsColumnDayDateIndicator
                                                                                ])
                                                                };

                    // Set the week / month indicator.

                    // Set the day / date indicator.

                    // Set the date value.
                    if (!Convert.IsDBNull(drResult[Constants.TableJobScheduleDetailsColumnDate]))
                        jobScheduleDetails.DateValue = Convert.ToDateTime(drResult[Constants.TableJobScheduleDetailsColumnDate]);

                    // Set the repeat every interval.
                    jobScheduleDetails.RepeatEvery = !Convert.IsDBNull(drResult[Constants.TableJobScheduleDetailsColumnRepeatEvery]) ? Convert.ToInt32(drResult[Constants.TableJobScheduleDetailsColumnRepeatEvery]) : 0;


                    // Add the schedule details to the current job.
                    if (currentJob.ScheduleDetails == null)
                    {
                        currentJob.ScheduleDetails = new List<JobScheduleDetails>();
                    }

                    // Add the current schedule information to the current job.
                    currentJob.ScheduleDetails.Add(jobScheduleDetails);

                    // Add the current job to the jobs to load list.
                    jobsToLoad.Add(currentJob);
                } // End for each
            } // End if

            // Return the list of jobs to load.
            return jobsToLoad;
        } // End GetJobsToLoad()

        /// <summary>
        /// This method can be used to update the next run date for a job with recurring schedule.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="nextRunDate">Next run date for a job.</param>
        /// <returns>Status of the update operation.</returns>
        internal static bool UpdateJobNextRun(int jobId, DateTime nextRunDate)
        {
            // Declare a local output variable.
            bool output = Constants.Success;

            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager(Constants.ConfigKeyDatabaseToUse);

            // Instantiate the stored procedure to obtain the jobs from load queue.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureUpdateJobNextRun);

            // Add input parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobId);
            db.AddInParameter(dbCommand, Constants.InputParameterNextRunDate, DbType.DateTime, nextRunDate);

            // Execute the stored procedure to update the next run date for the specified job.
            if (db.ExecuteNonQuery(dbCommand) <= Constants.None)
            {
                output = Constants.Failure;
            }

            // Return the output of the operation.
            return output;
        } // End UpdateJobNextRun()

        /// <summary>
        /// This method adds a job to the load queue to be picked up by the job loader.
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobTypeId">Job Type Identifier.</param>
        /// <param name="jobRunDuration">Job Run Duration.</param>
        /// <param name="serverId">Server Identifier.</param>
        /// <param name="jobRunId">Job Run Identifier.</param>
        /// <param name="jobLoadTime"> Job Load Time</param>
        /// <param name="jobParameters"> Job Parameters</param>
        /// <returns>Status of the operation.</returns>
        internal static bool AddJobToLoadQueue(int jobId, int jobTypeId, int jobRunDuration, Guid serverId, out int jobRunId, string jobParameters, DateTime jobLoadTime)
        {
            // Declare a local output variable.
            bool output = Constants.Success;

            // Initialize job run id to 0.
            jobRunId = 0;

            // Create a new instance of the DB manager.
            EVDbManager db = new EVDbManager(Constants.ConfigKeyDatabaseToUse);

            // Instantiate the stored procedure to obtain the jobs from load queue.
            DbCommand dbCommand = db.GetStoredProcCommand(Constants.StoredProcedureInsertIntoLoadQueue);

            // Add input and output parameters.
            db.AddInParameter(dbCommand, Constants.InputParameterJobId, DbType.Int32, jobId);
            db.AddInParameter(dbCommand, Constants.InputParameterJobTypeId, DbType.Int32, jobTypeId);
            db.AddInParameter(dbCommand, Constants.InputParameterJobDurationMinutes, DbType.Int32, jobRunDuration);
            db.AddInParameter(dbCommand, Constants.InputParameterJobServerId, DbType.Guid, serverId);
            db.AddInParameter(dbCommand, Constants.InputJobParameters, DbType.Xml, jobParameters);
            db.AddInParameter(dbCommand, Constants.InputJobLoadTime, DbType.DateTime, jobLoadTime);
            db.AddOutParameter(dbCommand, Constants.OutputParameterJobRunId, DbType.Int32, 4);

            if (db.ExecuteNonQuery(dbCommand) <= Constants.None)
            {
                output = Constants.Failure;
            }

            // Get the job run id returned.
            jobRunId = Convert.ToInt32(db.GetParameterValue(dbCommand, Constants.OutputParameterJobRunId));

            // Return the output of the operation.
            return output;
        } // End AddJobToLoadQueue()

    } // End JobScheduleMonitorDAO
} // End namespace
