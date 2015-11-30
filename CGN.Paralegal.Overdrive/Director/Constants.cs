using System;

namespace LexisNexis.Evolution.Overdrive
{
    public static class Constants
    {
        #region Configuration Key Constants

        /// <summary>
        /// Constant representing the key PoCDB.
        /// </summary>
        public const string ConfigKeyDatabaseToUse = "ConcordanceEVConnection";

        #endregion

        /// <summary>
        /// Constant representing month indicator - "M".
        /// </summary>
        public const string Month = "M";

        /// <summary>
        /// Constant representing week indicator - "W".
        /// </summary>
        public const string Week = "W";

        /// <summary>
        /// Constant represnting day indicator - "DY".
        /// </summary>
        public const string Day = "DY";

        /// <summary>
        /// Constant represnting date indicator - "DT".
        /// </summary>
        public const string Date = "DT";
        /// <summary>
        /// Constant representing MinDate 
        /// </summary>
        public static DateTime MinDate = new DateTime(1900, 1, 1);

        #region Property Name Constants
        /// <summary>
        /// Constant representing the Property name CurrentStatusId.
        /// </summary>
        public const string PropertyNameCurrentStatusId = "CurrentStatusId";
        /// <summary>
        /// Constant representing the property name JobProgressPercent.
        /// </summary>
        public const string PropertyNameJobProgressPercent = "ProgressPercent";
        /// <summary>
        /// Constant representing the Property name IssuedCommandId.
        /// </summary>
        public const string PropertyNameIssuedCommandId = "IssuedCommandId";
        /// <summary>
        /// Constant represents Job Visibility
        /// </summary>
        public const string PropertyNameVisibility = "Visibility";
        /// <summary>
        /// Constant representing the Property name JobName.
        /// </summary>
        public const string PropertyNameJobName = "JobName";
        /// <summary>
        /// Constant represents Job Priority column of EV_JOb_JobMaster table.
        /// </summary>
        public const string PropertyNamePriority = "JobPriority";
        /// <summary>
        /// Constatnt represents property Name
        /// </summary>
        public const string PropertyNameSubscriptionTypeId = "SubscriptionTypeId";
        /// <summary>
        /// Constatnt represents property Name
        /// </summary>
        public const string PropertyNameSubscriptionTypeName = "SubscriptionTypeName";
        /// <summary>
        /// Constatnt represents property Name
        /// </summary>
        public const string PropertyNameFolderId = "FolderId";
        /// <summary>
        /// Constatnt represents property Name
        /// </summary>
        public const string PropertyNameFolderName = "FolderName";

        #endregion

        #region Stored Procedure Name Constants

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_Update_Openjobs_IssueCmd.
        /// </summary>
        public const string StoredProcedureUpdateIssueCommandOpenJobs = "EV_JOB_Update_IssueCmd_OpenJobs";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_Update_OpenJobs.
        /// </summary>
        public const string StoredProcedureUpdateOpenJobs = "EV_JOB_Update_OpenJobs";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_GetNext_JobFromQueue.
        /// </summary>
        public const string StoredProcedureGetNextJobFromLoadQueue = "EV_JOB_GetNext_JobFromQueue";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_GetFrom_JobMaster.
        /// </summary>
        public const string StoredProcedureGetFromJobMaster = "EV_JOB_GetFrom_JobMaster";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_GetFrom_OpenJobs.
        /// </summary>
        public const string StoredProcedureGetJobStatus = "EV_JOB_GetFrom_OpenJobs";

        /// <summary>
        /// Constatnt represents UpdateJobMasterStatus Stored Procedure
        /// </summary>
        internal const string UpdateJobStatusProcedure = "dbo.EV_JOB_UpdateJobMasterStatus";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_UpdateFinalStatus.
        /// </summary>
        public const string StoredProcedureUpdateJobFinalStatus = "EV_JOB_UpdateFinalStatus";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_UpdateJobPercentage.
        /// </summary>
        public const string StoredProcedureUpdateJobPercentage = "EV_JOB_UpdateJobPercentage";

        /// <summary>
        /// Constant representing a SP Name
        /// </summary>
        public const string GetJobSubscriptionDetails = "EV_JOB_GetJobSubscriptionDetails";
        #endregion

        #region Stored Procedure Input / output Parameter Constants

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobTypeId.
        /// </summary>
        public const string InputParameterJobTypeId = "@in_iJobTypeID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobRunId.
        /// </summary>
        public const string InputParameterJobRunId = "@in_iJobRunID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobRunId.
        /// </summary>
        public const string InputParameterJobRunId1 = "@in_iJobRunId";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_uJobServerID.
        /// </summary>
        public const string InputParameterJobServerId = "@in_uJobServerID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobID.
        /// </summary>
        public const string InputParameterJobId = "@in_iJobID";

        /// <summary>
        /// Constant Represents an Input paramenter JobId
        /// </summary>
        internal const string ParameterInputJobId = "@in_iJobID";

        /// <summary>
        /// Constants represents an input parameter StatusId
        /// </summary>
        internal const string InputParameterJobStatus = "@in_iStatusid";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iStatusID.
        /// </summary>
        public const string InputParameterStatusId = "@in_iStatusID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iCurrentStatusId.
        /// </summary>
        public const string InputParameterCurrentStatusId = "@in_iCurrentStatusId";

        /// <summary>
        /// Constant representing the database stored procedure output parameter @out_iIssuedCommandID.
        /// </summary>
        public const string OutputParameterIssuedCommandId = "@out_iIssuedCommandID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobId.
        /// </summary>
        public const string InputParameterIssuedCommandId = "@in_iIssuedCommandId";

        /// <summary>
        /// Constant represents parameter Output Flag 
        /// </summary>
        internal const string OutputParameterReturnFlag = "@out_iFlag";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iProgressPercent.
        /// </summary>
        public const string InputParameterProgressPercent = "@in_fProgressPercent";

        /// <summary>
        /// Constant representing the database stroed procedure output parameter @out_iRowsUpdated.
        /// </summary>
        public const string OutputParameterRowsUpdated = "@out_iRowsUpdated";

        /// <summary>
        /// Constant represents JobReturnFlag Output parameter
        /// </summary>
        public const string JobReturnFlagOutParameter = "@out_iFlag";
        #endregion

        #region Count Constants

        /// <summary>
        /// Constant representing the value 0.
        /// </summary>
        public const int First = 0;

        /// <summary>
        /// Constant representing the value 0.
        /// </summary>
        public const int None = 0;

        #endregion

        #region Table Column Name Constants

        #region OpenJobs

        /// <summary>
        /// Constant representing the OpenJobs table column ProgressPercent.
        /// </summary>
        public const string TableOpenJobsColumnProgressPercent = "ProgressPercent";

        /// <summary>
        /// Constant representing the OpenJobs table column CurrentStatusId.
        /// </summary>
        public const string TableOpenJobsColumnCurrentStatusId = "CurrentStatusId";

        /// <summary>
        /// Constant representing the OpenJobs table column IssuedCommandId.
        /// </summary>
        public const string TableOpenJobsColumnIssuedCommandId = "IssuedCommandId";

        #endregion

        #region LoadQueue

        /// <summary>
        /// Constant representing LoadJobQueue table column JobId.
        /// </summary>
        public const string TableLoadJobQueueColumnJobId = "JobId";

        /// <summary>
        /// Constant representing Job Master table column CreatedBy.
        /// </summary>
        public const string TableJobMasterColumnCreatedBy = "CreatedBy";

        /// <summary>
        /// Constant representing Job Master table column NotificationID.
        /// </summary>
        public const string TableJobMasterColumnNotfnId = "NotificationID";

        /// <summary>
        /// Constant representing LoadJobQueue table column JobRunId.
        /// </summary>
        public const string TableLoadJobQueueColumnJobRunId = "JobRunId";

        /// <summary>
        /// Constant representing LoadJobQueue table column JobParameters.
        /// </summary>
        public const string TableLoadJobQueueColumnJobParameters = "JobParameters";

        /// <summary>
        /// Constant representing LoadJobQueue table column JobDurationMinutes.
        /// </summary>
        public const string TableLoadJobQueueColumnJobDurationMinutes = "JobDurationMinutes";

        /// <summary>
        ///  Constatnt representing Jobmaster table column RecurrenceType
        /// </summary>
        public const string TableJobMasterColumnRecurrenceType = "RecurrenceType";

        /// <summary>
        /// Constant representing NotificationId table coulmn
        /// </summary>
        public const string TableJobMasterNotificationId = "NotificationId";

        #endregion

        #endregion

        #region Property Names

        /// <summary>
        /// Constant representing the Property name JobId.
        /// </summary>
        public const string PropertyNameJobId = "JobId";

        /// <summary>
        /// Constant representing the property name JobRunId.
        /// </summary>
        public const string PropertyNameJobRunId = "JobRunId";

        #endregion

        #region Job Status Constants

        /// <summary>
        /// Constant representing Loaded job.
        /// </summary>
        public const int Loaded = 1;

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
    }
}