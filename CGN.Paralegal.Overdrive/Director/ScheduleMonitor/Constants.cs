//---------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the Constants class for Job Schedule Monitor service.
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

    #endregion

    /// <summary>
    /// This class represents the Constants class for the Job Loader service.
    /// </summary>
    internal class Constants
    {
        private Constants()
        {
        }
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
        #region Configuration Key Constants
        /// <summary>
        /// Constant representing the key PoCDB.
        /// </summary>
        //public const string ConfigKeyDatabaseToUse = "PoCDB";
        public const string ConfigKeyDatabaseToUse = "ConcordanceEVConnection";

        /// <summary>
        /// Constant representing the key TimerIntervalInSeconds.
        /// </summary>
        public const string ConfigKeyTimeIntervalInSeconds = "TimerIntervalInSeconds";

        /// <summary>
        /// Constant representing the key MonitorJobTypes.
        /// </summary>
        public const string ConfigKeyJobTypesToMonitor = "MonitorJobTypes";

        /// <summary>
        /// Constant representing the key ServiceName.
        /// </summary>
        public const string ConfigKeyServiceName = "ServiceName";

        /// <summary>
        /// Constant representing the key StartStoppedLoaderService.
        /// </summary>
        public const string ConfigKeyStartStoppedLoaderService = "StartStoppedLoaderService";

        /// <summary>
        /// Constant representing the key StartPausedLoaderService.
        /// </summary>
        public const string ConfigKeyStartPausedLoaderService = "StartPausedLoaderService";

        /// <summary>
        /// Constant representing the key SplitCharacter.
        /// </summary>
        public const string ConfigKeySplitCharacter = "SplitCharacter";
        #endregion
        #region Stored Procedure Name Constants
        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_GetJobsToLoad.
        /// </summary>
        public const string StoredProcedureGetJobsToLoad = "EV_JOB_GetJobsToLoad";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_InsertInto_LoadJobQueue.
        /// </summary>
        public const string StoredProcedureInsertIntoLoadQueue = "EV_JOB_InsertInto_LoadJobQueue";

        /// <summary>
        /// Constant representing the database stored procedure EV_JOB_Update_JobNextRun.
        /// </summary>
        //public const string StoredProcedureUpdateJobNextRun = "EV_JOB_Update_JobScheduleMaster";
        public const string StoredProcedureUpdateJobNextRun = "EV_JOB_Update_JobSchMster_NextRun";

        #endregion
        #region Stored Procedure Input / output Parameter Constants
        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobID.
        /// </summary>
        public const string InputParameterJobId = "@in_iJobID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_dNextRunDate.
        /// </summary>
        public const string InputParameterNextRunDate = "@in_dNextRunDate";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobTypeID.
        /// </summary>
        public const string InputParameterJobTypeId = "@in_iJobTypeID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_iJobDurationMinutes.
        /// </summary>
        public const string InputParameterJobDurationMinutes = "@in_iJobDurationMinutes";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_JobParameters.
        /// </summary>
        public const string InputParameterBootParameters = "@in_sJobParameters";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_JobServerID.
        /// </summary>
        public const string InputParameterJobServerId = "@in_uJobServerID";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_JobServerID.
        /// </summary>
        public const string InputJobParameters = "@in_sJobParameters";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_dJobLoaderTime.
        /// </summary>
        public const string InputJobLoadTime = "@in_dJobLoadTime";
        //public const string InputParameterJobServerId = "@in_uJobServerID";

        /// <summary>
        ///  Constant to represent Input parameter UpdatedBy of JobMastertable
        /// </summary>
        public const string InputParameterUpdatedBy = "@in_sUpdatedBy";
        /// <summary>
        /// Constant representing the database stored procedure input parameter @in_dJobLoadTime.
        /// </summary>
        public const string InputParameterJobLoadTime = "@in_dJobLoadTime";

        /// <summary>
        /// Constant representing the database stored procedure input parameter @out_iJobRunID.
        /// </summary>
        public const string OutputParameterJobRunId = "@out_iJobRunID";

        /// <summary>
        ///  Constant to represent Input parameter JobRunServer of JobMastertable
        /// </summary>
        public const string InputParameterJobRunServer = "@in_sJobRunServer";

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

        /// <summary>
        /// Constant representing the number of minutes in 365 days.
        /// </summary>
        public const int MinutesInOneYear = 525600;
        #endregion
        #region Table Column Name Constants
        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobId.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobId = "JobId";

        /// <summary>
        /// Constant representing the JobMaster table column JobName.
        /// </summary>
        public const string TableJobMasterColumnJobName = "JobName";

        /// <summary>
        /// Constant representing the JobMaster table column JobServerId.
        /// </summary>
        public const string TableJobMasterColumnJobServerId = "JobServerId";

        /// <summary>
        /// Constant representing the JobMaster table column JobParameters.
        /// </summary>
        public const string TableJobMasterColumnJobParameters = "JobParameters";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobRunId.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobRunId = "JobRunId";

        /// <summary>
        /// Constant representing the JobTypeMaster table column JobTypeName.
        /// </summary>
        public const string TableJobTypeMasterColumnJobRunId = "JobTypeName";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobTypeId.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobTypeId = "JobTypeId";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobParameters.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobParameters = "JobParameters";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobServerId.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobServerId = "JobServerId";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobStartDate.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobStartDate = "JobStartDate";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column JobEndDate.
        /// </summary>
        public const string TableJobScheduleMasterColumnJobEndDate = "JobEndDate";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column StatusId.
        /// </summary>
        public const string TableJobScheduleMasterColumnStatusId = "StatusId";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column Hourly.
        /// </summary>
        public const string TableJobScheduleMasterColumnHourly = "Hourly";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column Daily.
        /// </summary>
        public const string TableJobScheduleMasterColumnDaily = "Daily";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column RequestedRecurrenceCount.
        /// </summary>
        public const string TableJobScheduleMasterColumnRequestedRecurrenceCount = "RequestedRecurrenceCount";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column ActualOccurenceCount.
        /// </summary>
        public const string TableJobScheduleMasterColumnActualOccurrenceCount = "ActualOccurrenceCount";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column IsRecurring.
        /// </summary>
        public const string TableJobScheduleMasterColumnIsRecurring = "IsRecurring";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column IsScheduled.
        /// </summary>
        public const string TableJobScheduleMasterColumnIsScheduled = "IsScheduled";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column LastRunDate.
        /// </summary>
        public const string TableJobScheduleMasterColumnLastRunDate = "LastRunDate";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column NextRunDate.
        /// </summary>
        public const string TableJobScheduleMasterColumnNextRunDate = "NextRunDate";

        /// <summary>
        /// Constant representing the JobScheduleMaster table column DurationMinutes.
        /// </summary>
        public const string TableJobScheduleMasterColumnDurationMinutes = "DurationMinutes";

        /// <summary>
        /// Constant representing the JobScheduleDetails table column WkMnIndicator.
        /// </summary>
        public const string TableJobScheduleDetailsColumnWeekMonthIndicator = "WkMnIndicator";

        /// <summary>
        /// Constant representing the JobScheduleDetails table column DtDyIndicator.
        /// </summary>
        public const string TableJobScheduleDetailsColumnDayDateIndicator = "DtDyIndicator";

        /// <summary>
        /// Constant representing the JobScheduleDetails table column Alternation.
        /// </summary>
        public const string TableJobScheduleDetailsColumnRepeatEvery = "Alternation";

        /// <summary>
        /// Constant representing the JobScheduleDetails table column Date.
        /// </summary>
        public const string TableJobScheduleDetailsColumnDate = "Date";
        #endregion
        #region Misc. Constants
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

        /// <summary>
        /// Constant contains the KeyNotFound Info
        /// </summary>
        public const string KeyNotFoundInfo = "Settings collection does not contain the requested key: ";

        #endregion
    } // End Constants
} // End namespace
