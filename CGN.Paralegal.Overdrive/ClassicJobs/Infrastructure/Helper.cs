//---------------------------------------------------------------------------------------------------
// <copyright file="Helper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the Helper class for Jobs infrastructure projects.
//      </description>
//      <changelog>
//          <date value=""></date>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Infrastructure.Jobs
{
    #region Namespaces
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using BusinessEntities;
    using Infrastructure;
    using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

    #endregion

    /// <summary>
    /// Helper class for the Job infrastructure.
    /// </summary>
    /// <remarks></remarks>
    public static class Helper
    {
        #region Base Job Helper Methods

        /// <summary>
        /// Method to PersistStatus 
        /// </summary>
        /// <typeparam name="JobParametersType">job parameters type</typeparam>
        /// <typeparam name="TaskType"> task type</typeparam>
        /// <param name="jobParameters">job parameters</param>
        /// <param name="taskId"> task id</param>
        /// <param name="brokerType">broker type</param>
        /// <param name="issuedCommandId"> command issued</param>
        /// <param name="taskStartTime"> task start time</param>
        /// <param name="isCustomProgressPercentage"> is custom progress</param>
        /// <returns></returns>
        internal static bool PersistStatus<JobParametersType, TaskType>(JobParametersType jobParameters, int taskId, Constants.BrokerType brokerType, out int issuedCommandId, DateTime? taskStartTime, bool isCustomProgressPercentage)
        {
            // Declare a local output boolean variable.
            bool output = false;

            // Get the job run id from the job parameters / settings.
            int jobRunId = Convert.ToInt32(GetParameterValue(jobParameters, Constants.PropertyNameJobRunId), CultureInfo.CurrentCulture);

            // Get the progress percentage from the job parameters / settings.
            double progressPercent = Convert.ToDouble(GetParameterValue<JobParametersType>(jobParameters, Constants.PropertyNameJobProgressPercent), CultureInfo.CurrentCulture);

            // Initialize the issued command id to 0.
            issuedCommandId = Constants.None;

            // Branch out based on the type of broker specified.
            switch (brokerType)
            {
                // If the broker is Database then call the UdpateJobStatus() in DatabaseBroker.
                case Constants.BrokerType.Database:
                    {
                        // Update the job status in the database by persisting the tasks and progress percent. Obtain the command id issued for the job.
                        output = DatabaseBroker.UpdateJobStatus<TaskType>(jobRunId, progressPercent, taskId, out issuedCommandId, taskStartTime, isCustomProgressPercentage);
                        break;
                    } // End case Database broker type

                // If the broker is ConfigFile then call the respective method in ConfigFileBroker if implemented.
                case Constants.BrokerType.ConfigFile:
                    {
                        // Config file cannot be used as a broker to store the job status.
                        throw new NotSupportedException();
                    } // End case Config file broker

                // If the broker is Queue then call the respective method in QueueBroker if implemented.
                case Constants.BrokerType.Queue:
                    {
                        // Queue cannot be used as a broker to store the job status.
                        throw new NotSupportedException();
                    } // End case Queue broker
            } // End Switch

            // Return the output of the operation.
            return output;
        } // End PersistStatus()



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
        } // End SetParameterValue()

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
        } // End GetParameterValue()


        /// <summary>
        /// This method updates the job execution status. 
        /// </summary>
        /// <remarks>
        /// Execution statuses include: Loaded, Running, Stopped, Paused.
        /// </remarks>
        /// <typeparam name="JobParametersType">The business entity type for a job.</typeparam>
        /// <typeparam name="JobStatusType">The task business entity type for a job.</typeparam>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        /// <param name="jobExecutionStatus">Job execution status - loaded, running, stopped, paused.</param>
        /// <param name="brokerType">Indicates the broker to be used.</param>
        /// <returns>Status of the update operation.</returns>
        internal static bool UpdateJobExecutionStatus<JobParametersType, JobStatusType>(JobParametersType jobParameters, JobStatusType jobExecutionStatus, Constants.BrokerType brokerType)
        {
            int issuedCommandId = 0;
            return UpdateJobExecutionStatus<JobParametersType, JobStatusType>(jobParameters, jobExecutionStatus, brokerType, out issuedCommandId);
        } // End UpdateJobExecutionStatus()

        /// <summary>
        /// This method updates the job execution status. 
        /// </summary>
        /// <remarks>
        /// Execution statuses include: Loaded, Running, Stopped, Paused.
        /// </remarks>
        /// <typeparam name="JobParametersType">The business entity type for a job.</typeparam>
        /// <typeparam name="JobStatusType">The task business entity type for a job.</typeparam>
        /// <param name="jobParameters">Job input parameters / settings obtained during Initialize()</param>
        /// <param name="jobExecutionStatus">Job execution status - loaded, running, stopped, paused.</param>
        /// <param name="brokerType">Indicates the broker to be used.</param>
        /// <param name="issuedCommandId">Indicates the command id issued for the job.</param>
        /// <returns>Status of the update operation.</returns>
        internal static bool UpdateJobExecutionStatus<JobParametersType, JobStatusType>(JobParametersType jobParameters, JobStatusType jobExecutionStatus, Constants.BrokerType brokerType, out int issuedCommandId)
        {
            // Declare a local output boolean variable.
            bool output = true;

            // Initialize the issued command id to 0.
            issuedCommandId = 0;

            // Get the job run id from the job parameters / settings.
            int jobRunId = Convert.ToInt32(GetParameterValue(jobParameters, Constants.PropertyNameJobRunId), CultureInfo.CurrentCulture);

            // Get the execution status id.
            int jobExecutionStatusId = Convert.ToInt32(jobExecutionStatus, CultureInfo.CurrentCulture);

            // Branch out based on the type of broker specified.
            switch (brokerType)
            {
                // If the broker is Database then call the UpdateJobExecutionStatus() in DatabaseBroker.
                case Constants.BrokerType.Database:
                    {
                        output = DatabaseBroker.UpdateJobExecutionStatus(jobRunId, jobExecutionStatusId, out issuedCommandId);
                        break;
                    } // End case Database

                // If the broker is ConfigFile then call the respective method in ConfigFileBroker if implemented.
                case Constants.BrokerType.ConfigFile:
                    {
                        // Config file cannot be used as a broker to store the job status.
                        throw new NotSupportedException();
                    } // End case ConfigFile

                // If the broker is Queue then call the respective method in QueueBroker if implemented.
                case Constants.BrokerType.Queue:
                    {
                        // Queue cannot be used as a broker to store the job status.
                        throw new NotSupportedException();
                    } // End case Queue
            } // End case

            // Return the output of the operation.
            return output;
        } // End UpdateJobExecutionStatus()

        /// <summary>
        /// This method updates the job final status. 
        /// </summary>
        /// <param name="jobId">Job Identifier.</param>
        /// <param name="jobRunId">Job Instance Identifier.</param>
        /// <param name="jobStatus">Job status.</param>
        /// <returns>Status of the update operation.</returns>
        internal static bool UpdateJobFinalStatus(int jobId, int jobRunId, int jobStatus)
        {
            bool output = true;
            output = DatabaseBroker.UpdateJobFinalStatus(jobId, jobRunId, jobStatus);
            return output;
        }
        #endregion


        #region ScheduleController Helper Methods

        /// <summary>
        /// This method is used to fetch the Log Details.
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="jobRunId">Job Run Identifier</param>
        /// <param name="taskId">Task Identifier</param>
        /// <returns>LogInfo object</returns>
        internal static LogInfo GetLogDetails(int jobId, int jobRunId, int taskId)
        {
            try
            {
                string logInfo = DatabaseBroker.GetLogDetails(jobId, jobRunId, taskId);
                return string.IsNullOrEmpty(logInfo) ? new LogInfo() : (DeserializeObject<LogInfo>(logInfo));
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                return new LogInfo();
            }
        }


        #endregion
        #region Other Methods
        /// <summary>
        ///  This method is used to fetch Job Details.
        /// </summary>
        /// <param name="jobID">Job Unique Identifier</param>
        /// <returns>Return Job Details</returns>
        internal static JobBusinessEntity GetJobDetails(string jobID)
        {
            return DatabaseBroker.GetJobSubScriptionDetails(jobID);
        }

        /// <summary>
        ///  This method is used to fetch Job Details.
        /// </summary>
        /// <param name="jobID">Job Unique Identifier</param>
        /// <returns>Return Job Details</returns>
        internal static int GetJobStatus(string jobID)
        {
            return DatabaseBroker.GetJobStatus(jobID);
        }

        /// <summary>
        /// This method will update the status of the whole Job
        /// </summary>
        /// <param name="jobId">Unique Identifier</param>
        /// <param name="status">StatusId</param>
        /// <returns></returns>
        internal static bool UpdateJobMasterStatus(int jobId, int status)
        {
            return (DatabaseBroker.UpdateJobStatus(jobId, status));
        }

        /// <summary>
        /// This Method is used to insert the Task Details in database
        /// </summary>
        /// <typeparam name="TaskType">TaskType Specific to a Job</typeparam>
        /// <param name="taskList">List of tasks</param>
        /// <param name="jobRunId">Job Run Id</param>
        /// <returns>return True, successful insertion of task details</returns>
        internal static bool InsertTaskDetails<TaskType>(Tasks<TaskType> taskList, int jobRunId)
        {
            bool result = false;
            DateTime startTime = DateTime.UtcNow;

            foreach (TaskType task in taskList)
            {
                int taskNumber = Convert.ToInt32(GetParameterValue<TaskType>(task, Constants.PropertyNameTaskNumber));
                byte[] binaryData = DatabaseBroker.SerializeObjectBinary<TaskType>(task);
                result = DatabaseBroker.InsertTaskDetails(jobRunId, taskNumber, binaryData);
            }

            EvLog.WriteEntry(Constants.TaskInsertionTime, Convert.ToString((DateTime.UtcNow - startTime)), EventLogEntryType.Information);
            return result;
        }


        /// <summary>
        /// Thsi method is used to fetch task details.
        /// </summary>
        /// <typeparam name="JobParameterType"></typeparam>
        /// <typeparam name="TaskType"></typeparam>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        internal static Tasks<TaskType> GetJobTaskDetails<JobParameterType, TaskType>(JobParameterType jobParameters) where TaskType : class, new()
        {
            return DatabaseBroker.GetJobTaskDetails<JobParameterType, TaskType>(jobParameters);
        }

        /// <summary>
        ///  This method is used to log the Error messages along with the properties of the task.
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="JobRunId">Job Run Identifier</param>
        /// <param name="logInfoXML">Log xml</param>
        /// <param name="userGuid">user id</param>
        /// <param name="messageType">Message Type</param>
        /// <param name="isXmlLog">determines if log is in xml format</param>
        public static void JobLog(int jobId, int jobRunId, string errorCode, string logInfo, string userGuid, bool isError, bool isXmlLog)
        {
            DatabaseBroker.JobLog(jobId, jobRunId, logInfo, userGuid, isError, errorCode, isXmlLog);
        }

        /// <summary>
        /// Updates the task log.
        /// </summary>
        /// <param name="jobId">The job id.</param>
        /// <param name="JobRunId">The job run id.</param>
        /// <param name="taskId">The task id.</param>
        /// <param name="taskKey">The task key.</param>
        /// <param name="logInfo">The log info.</param>
        /// <param name="isError">if set to <c>true</c> [is error].</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="errorCode">Error Code</param>
        /// <remarks></remarks>
        public static void UpdateTaskLog(int jobId, int JobRunId, int taskId, string taskKey, string logInfo, bool isError, DateTime? startTime, DateTime? endTime, string errorCode)
        {
            DatabaseBroker.UpdateTaskLog(jobId, JobRunId, taskId, taskKey, logInfo, isError, startTime, endTime, errorCode);
        }

        ///// <summary>
        ///// This method serializes the object into a XML string.
        ///// </summary>
        ///// <param name="collection">Collection to be serialized.</param>
        ///// <returns>Serialized XML string.</returns>
        public static string SerializeObject<T>(T sourceObject)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode))
                {
                    xmlTextWriter.Formatting = Formatting.None;
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    xmlSerializer.Serialize(xmlTextWriter, sourceObject);
                    return Encoding.Unicode.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                return Constants.SerializationStartMessage + ex.Message + Constants.SerializationEndMessage;
            }
        }


        /// <summary>
        /// This method deserilizes a XML file into a class of type Tasks.
        /// </summary>
        /// <param name="xml">XML string to be deserialized.</param>
        /// <returns>Instance of type Tasks.</returns>
        public static T DeserializeObject<T>(string xml)
        {
            T logInfoObjet = default(T);
            try
            {
                Encoding utf16 = new UnicodeEncoding();
                using (MemoryStream memoryStream = new MemoryStream(utf16.GetBytes(xml)))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    logInfoObjet = (T)xmlSerializer.Deserialize(memoryStream);
                }
            }
            catch (Exception)
            {
                //This is left empty as to catch the exception and proceed further as we are handling this later.
            }
            return logInfoObjet;
        }


        #endregion

    } // End class Helper
} // End namespace
