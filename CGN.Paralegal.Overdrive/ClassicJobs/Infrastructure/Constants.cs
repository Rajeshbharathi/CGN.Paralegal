//---------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the Constants class used by the Jobs infrastructure project classes.
//      </description>
//      <changelog>
//          <date value="07/27/2011">Constant added for Bug# 88625</date>
//          <date value="05/11/2012">Fix for bug 100606</date>
//          <date value="05/28/2012">Fix for bug 100606</date>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Infrastructure.Jobs
{
    #region Namespaces

    #endregion

    /// <summary>
    /// This class contains the Constants for the Jobs infrastructure projects.
    /// </summary>
    /// <remarks></remarks>
    public sealed class Constants
    {
        private Constants()
        {
        }
        #region Property Name Constants


        /// <summary>
        /// Constant representing the Property name JobName.
        /// </summary>
        public const string PropertyNameJobName = "JobName";

        /// <summary>
        /// Constant representing the property name JobRunId.
        /// </summary>
        public const string PropertyNameJobRunId = "JobRunId";

        /// <summary>
        /// Constant representing the Property name CurrentStatus
        /// </summary>
        public const string PropertyNameCurrentStatus = "CurrentStatus";


        /// <summary>
        /// Constant representing the property name StatusBrokerType.
        /// </summary>
        public const string PropertyNameStatusBrokerType = "StatusBrokerType";

        /// <summary>
        /// Constant representing the property name LastExecutedCommand.
        /// </summary>
        public const string PropertyNameLastExecutedCommand = "LastExecutedCommand";

        /// <summary>
        /// Constant representing the property name TaskNumber.
        /// </summary>
        public const string PropertyNameTaskNumber = "TaskNumber";

        /// <summary>
        /// Constant representing the property name TaskInsertionTime.
        /// </summary>
        public const string TaskInsertionTime = "Task Details Insertion Time ";

        /// <summary>
        /// Constant representing the property name SerializationStartMessage.
        /// </summary>
        public const string SerializationStartMessage = "<SerializationError>";

        /// <summary>
        /// Constant representing the property name SerializationStartMessage.
        /// </summary>
        public const string SerializationEndMessage = "</SerializationError>";


        /// <summary>
        /// Constant representing the property name TaskPercent.
        /// </summary>
        public const string PropertyNameTaskPercent = "TaskPercent";

        /// <summary>
        /// Constant representing the property name TaskComplete.
        /// </summary>
        public const string PropertyNameTaskComplete = "TaskComplete";

        /// <summary>
        /// Constant representing the property name IsError.
        /// </summary>
        public const string PropertyNameIsTaskError = "IsError";

        /// <summary>
        /// Constant representing the property name JobProgressPercent.
        /// </summary>
        public const string PropertyNameJobProgressPercent = "ProgressPercent";

        public const string ProductionStartupRoleId = "prod0fc6-113e-4217-9863-ec58c3f7sw89";
        #endregion

        #region Job Status Constants

        /// <summary>
        /// Constant representing Running job.
        /// </summary>
        public const int Running = 2;

        /// <summary>
        /// Constant representing Stopped job.
        /// </summary>
        public const int Stopped = 3;

        /// <summary>
        /// Constant representing Paused job.
        /// </summary>
        public const int Paused = 4;

        /// <summary>
        /// Constant representing Completed job.
        /// </summary>
        public const int Completed = 5;

        /// <summary>
        /// Constant representing Failed job.
        /// </summary>
        public const int Failed = 6;

        #endregion
        #region Boolean Constants

        /// <summary>
        /// Constant representing the boolean value true.
        /// </summary>
        public const bool Yes = true;

        /// <summary>
        /// Constant representing the boolean value false.
        /// </summary>
        public const bool No = false;

        /// <summary>
        /// Constant representing the boolean value true.
        /// </summary>
        public const bool Success = true;

        /// <summary>
        /// Constant representing the boolean value false.
        /// </summary>
        public const bool Failure = false;
        #endregion
        #region Count Constansts
        /// <summary>
        /// Constant representing the value 0.
        /// </summary>
        public const int NoTask = 0;

        /// <summary>
        /// Constant representing the value 0.
        /// </summary>
        public const int None = 0;

        /// <summary>
        /// Constant representing the value 0.
        /// </summary>
        public const int First = 0;

        /// <summary>
        /// Constant representing the value 0.0.
        /// </summary>
        public const int Now = 0;
        #endregion
        #region Stored Procedure Name Constants
        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_Update_OpenJobs.
        /// </summary>
        public const string StoredProcedureUpdateOpenJobs = "EV_JOB_Update_OpenJobs";

        public const string StoredProcedureUpdateTaskCompletionStatus = "EV_JOB_Update_TaskCompleteStatus";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_Update_OpenJobs.
        /// </summary>
        public const string StoredProcedureInsertIntoTaskDetails = "EV_JOB_InsertInto_TaskDetails";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_Update_Openjobs_IssueCmd.
        /// </summary>
        public const string StoredProcedureUpdateIssueCommandOpenJobs = "EV_JOB_Update_IssueCmd_OpenJobs";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_UpdateFinalStatus.
        /// </summary>
        public const string StoredProcedureUpdateJobFinalStatus = "EV_JOB_UpdateFinalStatus";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_GetTaskDetails.
        /// </summary>
        public const string StoredProcedureGetTaskDetails = "EV_JOB_GetTaskDetails";

        #endregion
        #region Stored Procedure Input / Output Parameter Constants
        #region Input Parameters
        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobRunId.
        /// </summary>
        public const string InputParameterJobRunId = "@in_iJobRunId";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_bTaskDetailsRequired.
        /// </summary>
        public const string InputParamertTaskDetailsRequired = "@in_bTaskDetailsRequired";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobId.
        /// </summary>
        public const string InputParameterJobId = "@in_iJobId";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iTaskid.
        /// </summary>
        public const string InputParameterTaskId = "@in_iTaskid";

        /// <summary>
        /// Constant representing TaskKey
        /// </summary>
        public const string InputParameterTaskKey = "@in_sTaskKey";

        /// <summary>
        /// Constant representing TaskLog
        /// </summary>
        public const string InputParameterTaskLog = "@in_sTaskLog";

        /// <summary>
        /// Represents IsError.
        /// </summary>
        public const string InputParameterIsError = "@in_bIsError";

        /// <summary>
        /// Constant represents TaskStartTime
        /// </summary>
        public const string InputParameterTaskStartTime = "@in_dTaskStartTime";

        /// <summary>
        /// Represents TaskStartTime
        /// </summary>
        public const string InputParameterTaskEndTime = "@in_dTaskEndTime";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @bTaskdetails.
        /// </summary>
        public const string InputParameterTaskDetails = "@in_bTaskdetails";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobId.
        /// </summary>
        public const string InputParameterIssuedCommandId = "@in_iIssuedCommandId";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iCurrentStatusId.
        /// </summary>
        public const string InputParameterCurrentStatusId = "@in_iCurrentStatusId";


        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iCurrentStatusId.
        /// </summary>
        public const string InputParameterProgressUpdateSource = "@in_sProgressUpdateSource";

        /// <summary>
        /// Constant represents Task Start Time.
        /// </summary>
        public const string InputparamTaskStartTime = "@in_TaskStartTime";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iProgressPercent.
        /// </summary>
        public const string InputParameterProgressPercent = "@in_fProgressPercent";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iStatusID.
        /// </summary>
        public const string InputParameterStatusId = "@in_iStatusID";

        #endregion
        #region Output Parameters
        /// <summary>
        /// Constant representing the database stored procedure output parameter @out_iIssuedCommandID.
        /// </summary>
        public const string OutputParameterIssuedCommandId = "@out_iIssuedCommandID";

        /// <summary>
        /// Constant representing the database stored procedure output parameter @out_iFlag.
        /// </summary>
        public const string OutputParameterResultFlag = "@out_iFlag";
        /// <summary>
        /// Constant representing the database stroed procedure output parameter @out_iRowsUpdated.
        /// </summary>
        public const string OutputParameterRowsUpdated = "@out_iRowsUpdated";


        #endregion
        #endregion
        #region Table Column Name Constants
        #region OpenJobs
        /// <summary>
        /// Constant representing the OpenJobs table column ProgressPercent.
        /// </summary>
        public const string TableOpenJobsColumnProgressPercent = "ProgressPercent";

        /// <summary>
        /// Constant representing the OpenJobs table column TaskId.
        /// </summary>
        public const string TableOpenJobsColumnTaskId = "TaskId";

        /// <summary>
        /// Constant representing the OpenJobs table column IsError.
        /// </summary>
        public const string TableOpenJobsColumnIsError = "IsError";

        /// <summary>
        /// Constant representing the OpenJobs table column TaskDetails.
        /// </summary>
        public const string TableOpenJobsColumnTaskDetails = "TaskDetails";

        /// <summary>
        /// Constant representing the OpenJobs table column IsComplete.
        /// </summary>
        public const string TableOpenJobsColumnIsComplete = "IsComplete";

        #endregion

        #region JobTypeMaster
        /// <summary>
        /// Constant representing the JobTypeMaster table column JobTypeName.
        /// </summary>
        public const string TableJobTypeMasterColumnJobTypeName = "JobTypeName";

        #endregion


        #endregion

        #region Misc. Constants

        /// <summary>
        /// Constant representing a SP Name
        /// </summary>
        public const string GetJobSubscriptionDetails = "EV_JOB_GetJobSubscriptionDetails";

        /// <summary>
        /// Constant represents JobReturnFlag Output parameter
        /// </summary>
        public const string JobReturnFlagOutParameter = "@out_iFlag";
        /// <summary>
        /// Constant represents property Name
        /// </summary>
        public const string PropertyNameSubscriptionTypeId = "SubscriptionTypeId";
        /// <summary>
        /// Constant represents property Name
        /// </summary>
        public const string PropertyNameSubscriptionTypeName = "SubscriptionTypeName";
        /// <summary>
        /// Constant represents property Name
        /// </summary>
        public const string PropertyNameFolderId = "FolderId";
        /// <summary>
        /// Constant represents property Name
        /// </summary>
        public const string PropertyNameFolderName = "FolderName";

        /// <summary>
        /// Constant represents UpdateJobMasterStatus Stored Procedure
        /// </summary>
        internal const string UpdateJobStatusProcedure = "dbo.EV_JOB_UpdateJobMasterStatus";

        /// <summary>
        /// Constant represents Job Visibility
        /// </summary>
        public const string PropertyNameVisibility = "Visibility";

        /// <summary>
        /// Constant represents Job Priority column of EV_JOb_JobMaster table.
        /// </summary>
        public const string PropertyNamePriority = "JobPriority";

        /// <summary>
        /// Constant Represents an Input parameter JobId
        /// </summary>
        internal const string ParameterInputJobId = "@in_iJobID";

        /// <summary>
        /// Constants represents an input parameter StatusId
        /// </summary>
        internal const string InputParameterJobStatus = "@in_iStatusid";

        /// <summary>
        /// Constant represents parameter Output Flag 
        /// </summary>
        internal const string OutputParameterReturnFlag = "@out_iFlag";

        /// <summary>
        /// Constant represents the JobBehaviour Configuration.
        /// </summary>
        internal const string ConfigurationKeyJobBehaviour = "JobBehaviour";

        /// <summary>
        /// Constant represents the OneTimeJob
        /// </summary>
        internal const string OneTimeJob = "1";

        /// <summary>
        /// Constant represent the Recurrence Type OneTime
        /// </summary>
        internal const string RecurrenceTypeOnetime = "0";

        /// <summary>
        /// Constant represent the Recurrence Type Now
        /// </summary>
        internal const string RecurrenceTypeNow = "5";
        /// <summary>
        /// Constant representing the backslash character.
        /// </summary>
        internal const string BackSlash = @"\\";

        /// <summary>
        /// Constant represent the UserServiceUri
        /// </summary>
        internal const string UserServiceUri = "UserService";
        /// <summary>
        /// Constant represents user literal. 
        /// </summary>
        internal const string User = "user";

        /// <summary>
        /// Constant represent the Medium priority job
        /// </summary>
        internal const int JobPriorityNormal = 2;

        /// <summary>
        /// Constant represent the Low priority job
        /// </summary>
        internal const int JobPriorityLow = 3;

        /// <summary>
        /// Constant represent the High priority job
        /// </summary>
        internal const int JobPriorityHigh = 1;

        /// <summary>
        /// Constant representing the jobId
        /// </summary>
        internal const string ParamInJobId = "@in_iJobId";
        /// <summary>
        /// Constant representing the JobRunID
        /// </summary>
        internal const string ParamInJobRunId = "@in_iJobRunID";

        /// <summary>
        /// Constant representing the JobLog
        /// </summary>
        internal const string ParamInJobLog = "@in_sJobLog";

        /// <summary>
        /// Constant representing the CreatedBy
        /// </summary>
        internal const string ImportsParamInCreatedBy = "@in_sCreatedBy";

        /// <summary>
        /// Constant represents the iReturnFlag
        /// </summary>
        internal const string ParamOutReturnFlag = "@out_iReturnFlag";

        /// <summary>
        ///  Constant represents the @in_bIsError
        /// </summary>
        internal const string ParamInIsError = "@in_bIsError";

        /// <summary>
        /// Constant represents the @in_bIsXml
        /// </summary>
        internal const string ParamInIsxml = "@in_bIsXml";

        /// <summary>
        /// Constant representing the ErrorCode
        /// </summary>
        internal const string ImportsParamInErrorCode = "@in_sErrorCode";

        /// <summary>
        /// Constant represent the database stored procedure EV_JOB_InsertInto_JobLogs
        /// </summary>
        internal const string EvImDalSpSaveJobLog = "EV_JOB_InsertInto_JobLogs";

        /// <summary>
        /// Constant represent the database stored procedure EV_JOB_Update_TaskDetails
        /// </summary>
        internal const string EvJobUpdateTaskDetails = "EV_JOB_Update_TaskDetails";

        /// <summary>
        /// Constant represents the Notification message of a job cancelled/Stopped.
        /// </summary>
        internal const string CancelledStopped = "Cancelled/Stopped Successfully";

        /// <summary>
        /// Constant represents the Notification message of a job Paused.
        /// </summary>
        internal const string PausedSuccess = "Paused Successfully";

        /// <summary>
        ///  Represents space character
        /// </summary>
        internal const string StrSpace = " ";

        /// <summary>
        /// Represents the Task ErrorCode
        /// </summary>
        internal const string ErrorCodeTask = "1640";

        /// <summary>
        /// Represents the Job ErrorCode
        /// </summary>
        internal const string ErrorCodeJob = "1643";
        internal const string GetLogDetailsProcedure = "EV_JOB_GetLogDetails";
        internal const string JobInputParameterJobId = "@in_iJobid";
        internal const string JobInputParameterJobRunId = "@in_iJobRunId";
        internal const string JobInputParameterTaskId = "@in_iTaskId";
        internal const string ParamOutTotalNoOfRecord = "@out_iTotalNoOfRecords";
        internal const string LogInformation = "LogInfo";
        internal const string MessageTaskNumber = "Task Number :";
        internal const string ErrorInJob = "ErrorInJob";
        internal const string JobTypeNameBulkPrintJobs = "Bulk Print Job";
        internal const int BulkPrintJobType = 27;
        internal const string JobTypeNameClusterJob = "Clustering Job";
        internal const int DeduplicationJobTypeId = 6;
        internal const int ClusteringJob = 32;
        internal const string AudDatasetName = "Dataset Name";
        internal const string NA = "N/A";
        #endregion
        #region Enumerations
        /// <summary>
        /// Enumeration for the types of broker - Database, ConfigFile, Queue.
        /// A broker is means for communication i.e., a means to pass data between entities.
        /// </summary>
        public enum BrokerType
        {
            /// <summary>
            /// Use this if information will be stored / persisted and passed through Database.
            /// </summary>
            Database = 1,

            /// <summary>
            /// Use this if information will be stored / persisted and passed through Configuration file.
            /// </summary>
            ConfigFile = 2,

            /// <summary>
            /// Use this if information will be stored / persisted and passed through Queues.
            /// </summary>
            Queue = 3,
        }

        #endregion
        #region ErrorMessages
        public const string JobFailed = "One or more tasks failed";
        /// <summary>
        /// Constant representing successful message
        /// </summary>
        public const string JobSuccess = "Completed Successfully";
        /// <summary>
        /// Constant represents Notification Message " No Tasks Message"
        /// </summary>
        public const string JobMsgNoTasks = "No Tasks are available to process! ";

        /// <summary>
        /// Constant representing unsuccessful message
        /// </summary>
        public const string JobFailure = "An unexpected error has occurred. Job Terminated";

        #endregion
        #region Audit related constants
        /// <summary>
        /// Constant representing JobType  for audit
        /// </summary>
        internal const string AU_EVENT_JOBTYPE = "Job Type";

        /// <summary>
        /// Constant representing JobName   for audit
        /// </summary>
        internal const string AuEventJobName = "JobName";

        #endregion
        #region Notification message constants
        internal const string Instance = " Instance: ";
        internal const string Type = " Type: ";
        internal const string HtmlBreak = "<br/>";
        internal const string StatusColon = "Status: ";
        internal const string Job = "Job";
        internal const string Task = "Task";
        #endregion
    } // End Constants
}
