//-----------------------------------------------------------------------------------------
// <copyright file="constants.cs">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>V Keerti Kotaru</author>
//      <description>
//          Constants for Alerts Job
//      </description>
//      <changelog>
//          <date value="15-Aug-2010"></date>
//          <date value="4-october-2011">Bug fix 88021</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="5-20-2013">Bug fix 142839 </date>
//          <date value="9-26-2013">Bug Fix for 176294 - Changed the Notification Name. </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

using LexisNexis.Evolution.Infrastructure.Common;
namespace LexisNexis.Evolution.BatchJobs.Alerts
{
    /// <summary>
    /// Creating Alerts jobs constants class
    /// </summary>
    class Constants
    {
        /// <summary>
        /// Constructor
        /// </summary>
        private Constants()
        {
        }

        #region Constants specific to Stored Procedure EV_SEA_GetAlerts

        /// <summary>
        /// Represents stored procedure name EV_SEA_GetAlerts
        /// </summary>
        internal const string STOREDPROCEDURE_GET_ALERTS_TOBE_NOTIFIED = "EV_SEA_GetAlerts";

        /// <summary>
        /// Represents IN parameter @in_dFromTime
        /// </summary>
        internal const string GA_FROM_TIMESTAMP = "@in_dFromTime";

        /// <summary>
        /// Represents IN parameter @in_dToTime
        /// </summary>
        internal const string GA_TO_TIMESTAMP = "@in_dToTime";

        /// <summary>
        /// represents column SearchAlertID
        /// </summary>
        internal const string GA_SEARCH_ALERT_ID = "SearchAlertID";

        /// <summary>
        /// represents column SearchAlertName
        /// </summary>
        internal const string GA_SEARCH_ALERT_NAME = "SearchAlertName";

        /// <summary>
        /// represents column SearchQuery
        /// </summary>
        internal const string GA_SEARCH_QUERY = "SearchQuery";

        /// <summary>
        /// represents column ActualOccuranceCount
        /// </summary>
        internal const string GA_ACUAL_OCCURANCE_COUNT = "ActualOccuranceCount";

        /// <summary>
        /// represents column OwnerID
        /// </summary>
        internal const string GA_OWNER_ID = "OwnerID";

        /// <summary>
        /// represents column NotificationID
        /// </summary>
        internal const string GA_NOTIFICATION_ID = "NotificationID";

        /// <summary>
        /// represents column LastRunDate
        /// </summary>
        internal const string GA_LAST_RUN_DATE = "LastRunDate";



        /// <summary>
        /// represents column NextRunDate
        /// </summary>
        internal const string GA_NEXT_RUN_DATE = "NextRunDate";

        /// <summary>
        /// represents column DurationInMinutes
        /// </summary>
        internal const string GA_DURATION_IN_MINUTES = "DurationInMinutes";

        /// <summary>
        /// represents column CreatedBy
        /// </summary>
        internal const string GA_CREATED_BY = "CreatedBy";

        /// <summary>
        /// represents column IsActive
        /// </summary>
        internal const string IsActive = "IsActive";

        #endregion

        #region Constants specific to Stored Procedure EV_SEA_UpdateAlertPostNotification

        /// <summary>
        /// Represents stored procedure name EV_SEA_UpdateAlertPostNotification
        /// </summary>
        internal const string STORED_PROCEDURE_UPDATE_ALERT_POST_NOTIFICATION = "EV_SEA_UpdateAlertPostNotification";

        /// <summary>
        /// represents column SearchAlertID
        /// </summary>
        internal const string UAPN_SEARCH_ALERT_ID = "@in_iSearchalertid";

        /// <summary>
        /// represents column ActualOccuranceCount
        /// </summary>
        internal const string UAPN_ACUAL_OCCURANCE_COUNT = "@in_iActualoccurancecount";


        /// <summary>
        /// represents column LastRunDate
        /// </summary>
        internal const string UAPN_LAST_RUN_DATE = "@in_dLastrundate";

        /// <summary>
        /// represents column LastRunResultCount
        /// </summary>
        internal const string UAPN_LAST_RUN_RESULT_COUNT = "@in_iLastrunresultcount";

        /// <summary>
        /// represents column AllResultCount
        /// </summary>
        internal const string UAPN_ALL_RUN_RESULT_COUNT = "@in_iAllrunresultcount";

        /// <summary>
        /// represents column NextRunDate
        /// </summary>
        internal const string UAPN_NEXT_RUN_DATE = "@in_dNextrundate";

        /// <summary>
        /// Represents IN parameter Notification ID
        /// </summary>
        internal const string UAPN_NOTIFICATION_ID = "@in_iNotificationId";

        /// <summary>
        /// Represents IN parameter Job Run ID
        /// </summary>            
        internal const string UAPN_JOB_RUN_ID = "@in_iJobRunID";

        /// <summary>
        /// Represents IN parameter Search Query
        /// </summary>
        internal const string UAPN_SEARCH_QUERY = "@in_sSearchQuery";

        /// <summary>
        /// Represents IN parameter ManualAlertRunDate update flag
        /// </summary>
        internal const string UAPN_MANUALALERT_RUNDATE = "@in_dManualRunUpdate";

        /// <summary>
        /// Represents error returned by the stored procedure updating alert record.
        /// </summary>
        internal const int UAPN_ERROR_NO_ALERT_HISTORY_RECORD_INSERTED = 0;

        /// <summary>
        /// Represents error returned by the stored procedure updating alert record.
        /// </summary>
        internal const int UAPN_ERROR_UPDATING_ALERT_RECORD = -1;

        /// <summary>
        /// Represents error returned by the stored procedure - no alert record updated.
        /// </summary>
        internal const int UAPN_ERROR_NO_ALERT_RECORD_UPDATED = -2;

        /// <summary>
        /// Represents error returned by the stored procedure - couldn't update history
        /// </summary>
        internal const int UAPN_ERROR_UPDATING_ALERT_HISTORY = -3;

        #endregion

        #region Constants Specific to Stored Procedure EV_SEA_UpdateAlertJobNextRun

        /// <summary>
        /// Represents stored procedure EV_SEA_UpdateAlertJobNextRun
        /// </summary>
        internal const string STORED_PROCEDURE_UPDATE_ALERT_NEXT_RUN_DATE = "EV_SEA_UpdateAlertJobNextRun";

        /// <summary>
        /// Represents column in_dNextRunOffset
        /// </summary>
        internal const string UAJN_NEXT_RUN_DATE = "@in_dNextRunOffset";

        /// <summary>
        /// Represents column in in_iJobId
        /// </summary>
        internal const string UAJN_JOB_ID = "@in_iJobId";

        /// <summary>
        /// No future alerts found. Hence this job won't run
        /// </summary>
        internal const int UAJN_ERROR_NO_FUTURE_ALERTS = -1;

        #endregion

        #region Miscellaneous Constants
        internal const string COL_LASTRUNDATE = "LastRunDate";
        internal const string COL_LASTRUNRESULTCOUNT = "LastRunResultCount";
        internal const string COL_MANUALNEXTRUNDATE = "ManualNextRunDate";
        internal const string COL_SEARCHQUERY = "SearchQuery";
        internal const string COL_NEWRESULTCOUNT = "NewResultCount";
        internal const string COL_CREATEDBY = "CreatedBy";
        internal const string COL_NEXTRUNDATE = "NextRunDate";
        internal const string NewLine = "\n";
        internal const string JobId = "Job ID: ";
        internal const string JobRunId = "Job Run ID";
        internal const string NA = "N/A";
        internal const string BackSlash = "\\";
        internal const string ThreeTilde = "~~~";
        internal const string Hyphen = " - ";
        internal const string FOR = " for ";
        internal const string DATASET = " Dataset: ";
        internal const string INREVIEWSET = " in Review Set: ";

        #endregion

        #region Constants Specific to Stored Procedure EV_SEA_GetCurrentJobStartTime

        /// <summary>
        /// Represents Stored Procedure Name EV_SEA_GetCurrentJobStartTime
        /// </summary>
        internal const string STORED_PROCEDURE_GET_CURRENT_JOB_START_TIME = "EV_SEA_GetCurrentJobStartTime";

        /// <summary>
        /// Represents in parameter Job ID
        /// </summary>
        internal const string GCJST_JobID = "@in_iJobID";

        /// <summary>
        /// Represents Stored Procedure EV_SEA_GetManualNextRunDate
        /// </summary>
        internal const string STORED_PROCEDURE_GET_PREV_ALERTDETAILS = "EV_SEA_GetPrevAlertRunDetails";

        /// <summary>
        /// Represents In Parameter @in_iAlertID (Alert ID for which Last run time need to be retrieved).
        /// </summary>
        internal const string GLAT_AlertID = "@in_iAlertID";

        #endregion

        #region Constants Specific to Notification

        /// <summary>
        /// Static message text used while sending notification. This kind of message is sent irrespective new results are found or not when Search Alert runs.
        /// </summary>
        internal const string ALERT_NOTIFICATION_MESSAGE = " document(s) found for the alert ";

        #endregion

        #region General constants

        /// <summary>
        /// Represents Source of the message being logged. This constant is advised to be used everywhere in Alerts Job.
        /// </summary>
        internal const string LOG_SOURCE = "Alerts Job";

        /// <summary>
        /// Represents Next Line Character.
        /// </summary>
        internal const string NEXT_LINE_CHARACTER = "\n";

        /// <summary>
        /// Represents hardcoded title "Alert: " used while logging messages
        /// </summary>
        internal const string LOG_MESSAGE_TITLE1 = "Alert: ";

        /// <summary>
        /// Represents hardcoded title "Location: " used while logging messages
        /// </summary>
        internal const string LOG_MESSAGE_TITLE2 = "Location: ";

        /// <summary>
        /// Represents hardcoded title "Message" used while logging messages
        /// </summary>
        internal const string LOG_MESSAGE_TITLE3 = "Message: ";

        /// <summary>
        /// Represents hardcoded title "ID" used while logging messages
        /// </summary>
        internal const string LOG_MESSAGE_TITLE4 = "ID: ";

        /// <summary>
        /// Represents hardcoded title "Alert Name" used while logging messages
        /// </summary>
        internal const string LOG_MESSAGE_TITLE5 = "Name: ";

        /// <summary>
        /// Represents message for unnotified message
        /// </summary>
        internal const string UNNOTIFIED_MESSAGE = "Failed to fix unnotified errors ( this error doesn't impact current set of alerts). \n Error Details: ";

        /// <summary>
        /// Represents integer value to be used for first item or element.
        /// </summary>
        internal const int INTEGER_INITIALIZE_VALUE = 0;

        /// <summary>
        /// Represents query delimeter
        /// </summary>
        internal const string QUERY_DELIMETER = "~~~";

        /// <summary>
        /// Represents failed messages
        /// </summary>
        internal const string FAILED_MESSAGE = "Failed to update alert next run post notification. ";

        /// <summary>
        /// Represents dateformat that needs to be used for search
        /// </summary>
        internal const string DATE_FORMAT = "yyyy/MM/dd hh:mm:ss tt";

        /// <summary>
        /// Represents manual alert message
        /// </summary>
        internal const string MANUAL_ALERT_MESSAGE = "as part of manual alert ";

        /// <summary>
        /// Represents search result change message
        /// </summary>
        internal const string SEARCH_ALERT_RESULTS_CHANGED = " Search result has changed.";

        /// <summary>
        /// Represents Configuration item that has number of hours by which job start time need to be reduced.
        /// Scheduler increments the start time by an offset for next run.
        /// </summary>
        internal const string JOB_START_TIME_OFFSET = "JobStartTimeOffset";

        /// <summary>
        /// Alerts job when runs can pick items in near future and send out notifications. 
        /// This constant represents offset time in seconds.
        /// </summary>
        internal const string OFFSET_DURATION_IN_SECONDS = "OffsetDurationInSeconds";
    
        /// <summary>
        /// table tag
        /// </summary>
        internal const string Table = "<table>";

        /// <summary>
        /// open row tag
        /// </summary>
        internal const string Row = "<tr>";

        /// <summary>
        /// open column tag
        /// </summary>
        internal const string Column = "<td>";

        /// <summary>
        /// closed column tag
        /// </summary>
        internal const string CloseColumn = "</td>";

        /// <summary>
        /// closed ow tag
        /// </summary>
        internal const string CloseRow = "</tr>";

        /// <summary>
        /// closed table tag
        /// </summary>
        internal const string CloseTable = "</table>";

        /// <summary>
        /// Search Query
        /// </summary>
        internal const string MessageSearchQuery = "Search Query: ";

        /// <summary>
        /// Dataset Name
        /// </summary>
        internal const string MessageDatasetName = "Dataset: ";

        /// <summary>
        /// Reviewset Name
        /// </summary>
        internal const string MessageReviewsetName = "Review Set: ";

        /// <summary>
        /// Represents integer value to increment next run time by one.
        /// </summary>
        internal const int SINGLE_INCREMENT = 1;

        /// <summary>
        /// Represents increment by 7 days
        /// </summary>
        internal const int WEEKLY_INCREMENT = 7;

        /// <summary>
        /// Represents duration - Offset Daily.
        /// </summary>
        internal const int DAILY = -1;

        /// <summary>
        /// Represents duration - Offset 7 days.
        /// </summary>
        internal const int WEEKLY = -2;

        /// <summary>
        /// Represents duration - Offset monthly;
        /// </summary>
        internal const int MONTHLY = -3;

        /// <summary>
        /// Represents true
        /// </summary>
        internal const bool SUCCESS = true;

        /// <summary>
        /// Represents false
        /// </summary>
        internal const bool FAILURE = false;

        /// <summary>
        /// Represents user session info
        /// </summary>
        internal const string UserSessionInfo = "UserSessionInfo";

        /// <summary>
        /// Gets min date in MS SQL Server 2005
        /// </summary>
        internal static System.DateTime MSSQL2005_MINDATE
        {
            get
            {
                return new System.DateTime(1753, 1, 1);
            }
        }
        /// <summary>
        /// Represents Fixed alert
        /// </summary>
        internal const string MESSAGE_ALERT_FIX = "<Message> Fixed alert to run in future, did not search for documents </Message>";

        #endregion

        #region Exception codes
        /// <summary>
        /// Error code depicting no future alerts
        /// </summary>
        internal const string ERROR_CODE_NO_FUTURE_ALERTS = "8201";

        /// <summary>
        /// Error code depicting unknown Stored Procedure problem
        /// </summary>
        internal const string ERROR_UNKNOWN_FROM_STORED_PROCEDURE = "8200";

        /// <summary>
        /// Error code depicting Alert record not updated post notification
        /// </summary>
        internal const string ERROR_NO_ALERT_RECORD_UPDATED_POST_NOTIFICATION = "8202";

        /// <summary>
        /// Error code depicting Stored Procedure crashed updating alert record
        /// </summary>
        internal const string ERROR_UPDATING_ALERT_RECORD_POST_NOTIFICATION = "8203";

        /// <summary>
        /// Error code depicting Stored Procedure crashed inserting alert history record
        /// </summary>
        internal const string ERROR_CREATING_ALERT_HISTORY = "8204";

        /// <summary>
        /// Error code depicting Alert record not udpated to history
        /// </summary>
        internal const string ERROR_RECORD_NOT_INSERTED_IN_HISTORY = "8205";

        /// <summary>
        /// Represents error code when ther are no alerts found in the current run
        /// </summary>
        internal const string ERROR_NO_ALERTS_TO_RUN = "8207";

        #endregion

    }
}
