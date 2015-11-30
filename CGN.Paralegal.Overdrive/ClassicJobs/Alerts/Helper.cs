//-----------------------------------------------------------------------------------------
// <copyright file="Helper.cs">
//      Copyright (c) Cognizant. All rights reserved.
// </copyright>
// <header>
//      <author>V Keerti Kotaru</author>
//      <description>
//          Helper functions (includes DB calls) for Alerts Job
//      </description>
//      <changelog>
//          <date value="4-october-2011">Bug fix 88021</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataContracts;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.DBManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
namespace LexisNexis.Evolution.BatchJobs.Alerts
{
    /// <summary>
    /// Class includes Helper functions (includes DB calls) for Alerts Job
    /// </summary>
    [Serializable]
    internal class Helper
    {
        [NonSerialized]
        EVDbManager _evDbManager;

        /// <summary>
        /// Property is created for null check on _evDBManager
        /// null check - one helper class object is expected to use on object of EV DB Manager.
        /// </summary>
        private EVDbManager AlertsDbManager
        {
            get
            {
                // null check - one helper class object is expected to use on object of EV DB Manager.
                return _evDbManager ?? (_evDbManager = new EVDbManager());
            }
        }


        #region Helper functions for Alerts Job.

        /// <summary>
        /// Calls stored procedure to obtain list of alerts to be notified.
        /// </summary>
        /// <param name="toTime">Alerts scheduled till this time</param>
        /// <param name="fromTime">Alerts scheduled to run beginning this time</param>
        /// <returns>List of tasks</returns>
        internal Tasks<SearchAlertsTaskBEO> GetActiveAlerts(DateTime toTime, DateTime fromTime)
        {
            Tasks<SearchAlertsTaskBEO> tasks = new Tasks<SearchAlertsTaskBEO>();

            // create command object
            DbCommand dbCommand = AlertsDbManager.GetStoredProcCommand(Constants.STOREDPROCEDURE_GET_ALERTS_TOBE_NOTIFIED);

            // add in parameters
            // param - to time, alerts till what time to be retrieved.
            AlertsDbManager.AddInParameter(dbCommand, Constants.GA_TO_TIMESTAMP, DbType.DateTime, toTime);
            AlertsDbManager.AddInParameter(dbCommand, Constants.GA_FROM_TIMESTAMP, DbType.DateTime, fromTime);

            // call stored procedure
            DataSet dataset = AlertsDbManager.ExecuteDataSet(dbCommand);

            // Create tasks - each search alert to be notified is a task.
            // Get all Alerts and add them to tasks.
            if (dataset != null)
            {
                foreach (SearchAlertsTaskBEO tmpSearchAlertsTask in (from DataTable dataTable in dataset.Tables from DataRow dataRow in dataTable.Rows select dataRow).Select(ConvertToTask).Where(tmpSearchAlertsTask => tmpSearchAlertsTask != null))
                {
                    tasks.Add(tmpSearchAlertsTask);
                }
            }

            // return list of active alerts objects.
            return tasks;
        }

        /// <summary>
        /// Job shall update next start time (based on next alert to be run)
        /// </summary>
        /// <param name="jobId">Alert Job's ID</param>
        /// <param name="lastAlertInCurrentRun">Next run start time is calculate based on last alert in current run</param>
        /// <returns></returns>
        internal void UpdateAlertJobNextRunDateTime(Int64 jobId, DateTime lastAlertInCurrentRun)
        {
            // Create Command object
            DbCommand dbCommand = AlertsDbManager.GetStoredProcCommand(Constants.STORED_PROCEDURE_UPDATE_ALERT_NEXT_RUN_DATE);

            // Add In Parameters
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAJN_JOB_ID, DbType.Int64, jobId);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAJN_NEXT_RUN_DATE, DbType.DateTime, lastAlertInCurrentRun);

            // execute the stored procedure and use the return value for identifying errors
            AlertsDbManager.ExecuteScalar(dbCommand);
        }


        /// <summary>
        /// Alert details are updated after Notification is sent.
        /// </summary>
        /// <param name="actualOccuranceCount">Number of times Job ran - Count incremented by one</param>
        /// <param name="runDate">Timestamp at which job ran</param>
        /// <param name="resultCount">Number of results found when job ran</param>
        /// <param name="allResultcount"></param>
        /// <param name="nextAlertRunDate">Future timestamp this alert has to run</param>
        /// <param name="alertId">Identifier for the alert</param>        
        /// <param name="notificationId">Notification Identifier</param>
        /// <param name="jobRunId">Job Run Identifier</param>
        /// <param name="searchQueryXml">Serialized Search Query object</param>
        /// <returns>success of failure status for the update</returns>
        internal bool UpdateAlertPostNotification(long actualOccuranceCount, DateTime runDate, long resultCount, long allResultcount, DateTime nextAlertRunDate, long alertId, Int64 notificationId, Int64 jobRunId, string searchQueryXml)
        {
            // Create Command object
            DbCommand dbCommand = AlertsDbManager.GetStoredProcCommand(Constants.STORED_PROCEDURE_UPDATE_ALERT_POST_NOTIFICATION);

            #region Add in parameters
            // Add in parameters
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_ACUAL_OCCURANCE_COUNT, DbType.Int32, actualOccuranceCount);
            if ((runDate == Constants.MSSQL2005_MINDATE) || (runDate == DateTime.MinValue))
            {
                AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_LAST_RUN_DATE, DbType.DateTime, null);
            }
            else
            {
                AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_LAST_RUN_DATE, DbType.DateTime, runDate);
            }
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_MANUALALERT_RUNDATE, DbType.DateTime, null);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_LAST_RUN_RESULT_COUNT, DbType.Int32, resultCount);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_ALL_RUN_RESULT_COUNT, DbType.Int32, allResultcount);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_NEXT_RUN_DATE, DbType.DateTime, nextAlertRunDate);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_SEARCH_ALERT_ID, DbType.Int64, alertId);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_NOTIFICATION_ID, DbType.Int64, notificationId);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_JOB_RUN_ID, DbType.Int64, jobRunId);
            AlertsDbManager.AddInParameter(dbCommand, Constants.UAPN_SEARCH_QUERY, DbType.Xml, searchQueryXml);
            #endregion

            // execute the stored procedure and use the return value for identifying errors
            object oFlag = AlertsDbManager.ExecuteScalar(dbCommand);
            int flag = (oFlag == null) ? Constants.INTEGER_INITIALIZE_VALUE : Convert.ToInt16(oFlag);

            // success of failure status
            if (flag > Constants.INTEGER_INITIALIZE_VALUE)
            {
                return Constants.SUCCESS;
            }

            #region Error Conditions
            // exception handling
            EVException evException;
            // Check flag returned by the stored procedure to pin point errors.
            switch (flag)
            {
                case (Constants.UAPN_ERROR_NO_ALERT_RECORD_UPDATED):
                    evException = new EVException().AddResMsg(Constants.ERROR_NO_ALERT_RECORD_UPDATED_POST_NOTIFICATION);
                    break;
                case (Constants.UAPN_ERROR_UPDATING_ALERT_RECORD):
                    evException = new EVException().AddResMsg(Constants.ERROR_UPDATING_ALERT_RECORD_POST_NOTIFICATION);
                    break;
                case (Constants.UAPN_ERROR_UPDATING_ALERT_HISTORY):
                    evException = new EVException().AddResMsg(Constants.ERROR_CREATING_ALERT_HISTORY);
                    break;
                case (Constants.UAPN_ERROR_NO_ALERT_HISTORY_RECORD_INSERTED):
                    evException = new EVException().AddResMsg(Constants.ERROR_RECORD_NOT_INSERTED_IN_HISTORY);
                    break;
                default:// Unknown status returned by the stored procedure.
                    evException = new EVException().AddResMsg(Constants.ERROR_UNKNOWN_FROM_STORED_PROCEDURE);                    
                    break;
            }
            throw evException;
            #endregion
        }


        /// <summary>
        /// Method to get previous alert details
        /// </summary>
        /// <param name="alertId">alert Id</param>
        /// <returns></returns>
        internal List<String> GetPreviousAlertRunDetails(Int64 alertId)
        {
            DbCommand dbcommand = AlertsDbManager.GetStoredProcCommand(Constants.STORED_PROCEDURE_GET_PREV_ALERTDETAILS);
            AlertsDbManager.AddInParameter(dbcommand, Constants.GLAT_AlertID, DbType.Int64, alertId);
            DataSet dsSearchAlert = AlertsDbManager.ExecuteDataSet(dbcommand);
            List<string> alertResults = new List<string>();

            if (dsSearchAlert != null && dsSearchAlert.Tables.Count > 0 && dsSearchAlert.Tables[0].Rows.Count > 0)
            {
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_LASTRUNDATE]) ? DateTime.MinValue.ToString(CultureInfo.InvariantCulture) : dsSearchAlert.Tables[0].Rows[0][Constants.COL_LASTRUNDATE].ToString());
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_LASTRUNRESULTCOUNT]) ? "0" : dsSearchAlert.Tables[0].Rows[0][Constants.COL_LASTRUNRESULTCOUNT].ToString());
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_MANUALNEXTRUNDATE]) ? DateTime.MinValue.ToString(CultureInfo.InvariantCulture) : dsSearchAlert.Tables[0].Rows[0][Constants.COL_MANUALNEXTRUNDATE].ToString());
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_SEARCHQUERY]) ? string.Empty : dsSearchAlert.Tables[0].Rows[0][Constants.COL_SEARCHQUERY].ToString());
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_NEXTRUNDATE]) ? DateTime.MinValue.ToString(CultureInfo.InvariantCulture) : dsSearchAlert.Tables[0].Rows[0][Constants.COL_NEXTRUNDATE].ToString());
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_NEWRESULTCOUNT]) ? "0" : dsSearchAlert.Tables[0].Rows[0][Constants.COL_NEWRESULTCOUNT].ToString());
                alertResults.Add(Convert.IsDBNull(dsSearchAlert.Tables[0].Rows[0][Constants.COL_CREATEDBY]) ? string.Empty : dsSearchAlert.Tables[0].Rows[0][Constants.COL_CREATEDBY].ToString());
            }
            return alertResults;
        }


        /// <summary>
        /// Gets current iteration start time for specified job id
        /// </summary>
        /// <param name="searchAlertJobId">Job ID</param>
        /// <returns>Start time stamp</returns>
        internal DateTime GetSearchAlertJobRunStartTime(Int64 searchAlertJobId)
        {
            DbCommand dbcommand = AlertsDbManager.GetStoredProcCommand(Constants.STORED_PROCEDURE_GET_CURRENT_JOB_START_TIME);
            AlertsDbManager.AddInParameter(dbcommand, Constants.GCJST_JobID, DbType.Int64, searchAlertJobId);
            return Convert.ToDateTime(AlertsDbManager.ExecuteScalar(dbcommand));
        }


        /// <summary>
        /// Calculates next timestamp an Alert has to run
        /// </summary>
        /// <param name="duration">Duration to be added to the current run timestamp - that determines next run time</param>
        /// <param name="currentRunDate"></param>
        /// <returns></returns>
        internal DateTime CalculateNextRunTimeStampForAlert(int duration, DateTime currentRunDate)
        {
            // If duration is in minutes, it's the real value to be added to calculate next run timestamp
            if (duration > 0 && duration <= 1440)
            {
                return currentRunDate.Add(new TimeSpan(Constants.INTEGER_INITIALIZE_VALUE, duration, Constants.INTEGER_INITIALIZE_VALUE));
            }

            #region Code to increment by a day or a month or an year
            switch (duration)
            {
                case Constants.DAILY:
                    return currentRunDate.AddDays(Constants.SINGLE_INCREMENT);
                case Constants.WEEKLY:
                    return currentRunDate.AddDays(Constants.WEEKLY_INCREMENT);
                case Constants.MONTHLY:
                    return currentRunDate.AddMonths(Constants.SINGLE_INCREMENT);
                default:
                    return Constants.MSSQL2005_MINDATE;
            }
            #endregion
        }

        #endregion

        /// <summary>
        /// Converts Data row returned by stored procedure to a Task object
        /// </summary>
        /// <param name="datarow">Data row to be converted</param>
        /// <returns>Converted Task object returned</returns>
        private SearchAlertsTaskBEO ConvertToTask(DataRow datarow)
        {
            SearchAlertsTaskBEO searchAlertsTaskBeo;
            try
            {
                DocumentQueryEntity tmpSearchContext = null;
                if (!datarow[Constants.GA_SEARCH_QUERY].Equals(DBNull.Value))
                {
                    tmpSearchContext = (DocumentQueryEntity)XmlUtility.DeserializeObject
                        (datarow[Constants.GA_SEARCH_QUERY].ToString(), typeof(DocumentQueryEntity));
                }

                searchAlertsTaskBeo = new SearchAlertsTaskBEO
                {
                    SearchAlert = new SearchAlertEntity
                    {
                        AlertId = (datarow[Constants.GA_SEARCH_ALERT_ID].Equals(DBNull.Value))
                                ? Constants.INTEGER_INITIALIZE_VALUE
                                : Convert.ToInt64(datarow[Constants.GA_SEARCH_ALERT_ID]),
                        Duration = (datarow[Constants.GA_DURATION_IN_MINUTES].Equals(DBNull.Value))
                                ? Constants.INTEGER_INITIALIZE_VALUE
                                : Convert.ToInt32(datarow[Constants.GA_DURATION_IN_MINUTES]),
                        LastRunDate = (datarow[Constants.GA_LAST_RUN_DATE].Equals(DBNull.Value))
                                    ? Constants.MSSQL2005_MINDATE
                                    : Convert.ToDateTime(datarow[Constants.GA_LAST_RUN_DATE]),
                        Name = (datarow[Constants.GA_SEARCH_ALERT_NAME].Equals(DBNull.Value))
                                ? string.Empty
                                : datarow[Constants.GA_SEARCH_ALERT_NAME].ToString(),
                        NextRunDate = (datarow[Constants.GA_NEXT_RUN_DATE].Equals(DBNull.Value))
                                ? Constants.MSSQL2005_MINDATE
                                : Convert.ToDateTime(datarow[Constants.GA_NEXT_RUN_DATE].ToString().Trim()),
                        NotificationId = (datarow[Constants.GA_NOTIFICATION_ID].Equals(DBNull.Value))
                                ? Constants.INTEGER_INITIALIZE_VALUE
                                : Convert.ToInt64(datarow[Constants.GA_NOTIFICATION_ID]),
                        OwnerId = (datarow[Constants.GA_OWNER_ID].Equals(DBNull.Value))
                                ? string.Empty
                                : datarow[Constants.GA_OWNER_ID].ToString(),
                        ActualOccurrenceCount = (datarow[Constants.GA_ACUAL_OCCURANCE_COUNT].Equals(DBNull.Value))
                                ? Constants.INTEGER_INITIALIZE_VALUE
                                : Convert.ToInt64(datarow[Constants.GA_ACUAL_OCCURANCE_COUNT]),
                        CreatedBy = (datarow[Constants.GA_CREATED_BY].Equals(DBNull.Value))
                                    ? String.Empty
                                    : datarow[Constants.GA_CREATED_BY].ToString(),
                        DocumentQuery = tmpSearchContext,
                        IsActive = Convert.ToBoolean(datarow[Constants.IsActive])
                    }

                };
            }
            catch
            {
                //--RVWSEarchBEO type is deprecated and no longer supported. hence past records can not be processed and returned..
                return null;
            }
            // return the task object
            return searchAlertsTaskBeo;
        }


    }
}
