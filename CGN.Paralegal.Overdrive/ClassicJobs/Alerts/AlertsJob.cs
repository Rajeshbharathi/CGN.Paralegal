//-----------------------------------------------------------------------------------------
// <copyright file="AlertsJob.cs">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>V Keerti Kotaru</author>
//      <description>
//          This file contains Alert Job implementation.
//      </description>
//      <changelog>
//          <date value="15-Aug-2010"></date>
//          <date value="9-Dec-2011">Change in Query part, removing imported date</date>
//          <date value="9-April-2012">Bug fix 98774</date>
//          <date value="3-Jan-2013">Bug fix 112430</date>
//          <date value="3-15-2013">Bug fix 131769 </date>
//          <date value="5-20-2013">Bug fix 142839 </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using LexisNexis.Evolution.BatchJobs.Utilities;
using LexisNexis.Evolution.Business.FolderManagement;
using LexisNexis.Evolution.Business.NotificationManagement;
using LexisNexis.Evolution.Business.ReviewSet;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.Infrastructure.SessionManagement;
using Moq;

#endregion Namespaces

namespace LexisNexis.Evolution.BatchJobs.Alerts
{
    /// <summary>
    /// Alert Job implementation class.
    /// </summary>
    [Serializable]
    public class AlertsJob : BaseJob<SearchAlertsJobBEO, SearchAlertsTaskBEO>
    {
        private readonly SearchAlertsJobBEO m_SearchAlertsJobBeo; // Job level data
        private readonly Helper m_Helper; // helper object ensures one DB object/connection per job.
        private UserBusinessEntity m_UserBusinessEntity;
        private long m_NewResultCount;
        private long m_AllResultCount;
        private DateTime m_ManualAlertNextRunDate;
        private int m_LastResultCount;
        private DateTime m_AlertLastRunTimestamp;
        private string m_PrevSearchQuery;

        /// <summary>
        ///     Format in our application for all date fields
        /// </summary>
        private const string EvDateTimeFormat = "yyyyMMddHHmmss";

        /// <summary>
        /// Constructor - Initialize private objects.
        /// </summary>
        public AlertsJob()
        {
            try
            {
                m_Helper = new Helper();
                m_SearchAlertsJobBeo = new SearchAlertsJobBEO();
                m_NewResultCount = 0;
                m_AllResultCount = 0;
                m_LastResultCount = 0;
                m_ManualAlertNextRunDate = DateTime.MinValue;
                m_AlertLastRunTimestamp = DateTime.MinValue;
                m_PrevSearchQuery = string.Empty;
            }
            catch (EVException ex)
            {
                HandleEVException(ex, MethodBase.GetCurrentMethod().Name, null);
            }
            catch (Exception ex)
            {
                // Handle exception in constructor
                LogMessage(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error, null);
            }
        }

        #region Job Framework functions

        /// <summary>
        /// Initializes Job BEO
        /// </summary>
        /// <param name="jobId">Alert Job Identifier</param>
        /// <param name="jobRunId">Alert Job Run Identifier</param>
        /// <param name="bootParameters">Boot parameters</param>
        /// <param name="createdByGuid">Alert Job created by Guid</param>
        /// <returns>Alert Job Business Entity</returns>
        protected override SearchAlertsJobBEO Initialize(int jobId, int jobRunId, string bootParameters,
            string createdByGuid)
        {
            try
            {
                // Set Job level properties to Alert Job business entity object.
                m_SearchAlertsJobBeo.JobId = jobId;
                m_SearchAlertsJobBeo.JobRunId = jobRunId;
                m_SearchAlertsJobBeo.JobTypeName = Constants.LOG_SOURCE;
                m_SearchAlertsJobBeo.BootParameters = bootParameters;
                m_SearchAlertsJobBeo.JobName = Constants.LOG_SOURCE;
                m_UserBusinessEntity = UserBO.GetUserUsingGuid(createdByGuid);
                m_SearchAlertsJobBeo.JobScheduleCreatedBy = (m_UserBusinessEntity.DomainName.Equals(Constants.NA))
                    ? m_UserBusinessEntity.UserId
                    : m_UserBusinessEntity.DomainName + Constants.BackSlash + m_UserBusinessEntity.UserId;
                // job start time need to be reduced by an offset to calculate start time
                // That's because at the beginning, Scheduler increments the start time by an offset for next run.
                var jobStartTimeoffsetFinal = Constants.INTEGER_INITIALIZE_VALUE;
                var jobStartTimeoffset = ApplicationConfigurationManager.GetValue(Constants.JOB_START_TIME_OFFSET);
                Int32.TryParse(jobStartTimeoffset, out jobStartTimeoffsetFinal);
                m_SearchAlertsJobBeo.JobScheduleNextRunDate =
                    m_Helper.GetSearchAlertJobRunStartTime(m_SearchAlertsJobBeo.JobId).AddHours(jobStartTimeoffsetFinal);
            }
            catch (EVException ex)
            {
                HandleEVException(ex, MethodBase.GetCurrentMethod().Name, null);
                var jobException = new EVJobException(ex.GetErrorCode(), ex, JobLogInfo);
                throw (jobException);
            }
            catch (Exception ex)
            {
                // Handle exception in initialize
                LogMessage(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error, null);
                var jobException = new EVJobException(ErrorCodes.ProblemInJobInitialization, ex, JobLogInfo);
                throw (jobException);
            }
            // return Alert Job Business Entity
            return m_SearchAlertsJobBeo;
        }

        /// <summary>
        /// Generate Search Alerts Tasks
        /// </summary>
        /// <param name="jobParameters"></param>
        /// <param name="previouslyCommittedTaskCount"></param>
        /// <returns></returns>
        protected override Tasks<SearchAlertsTaskBEO> GenerateTasks(SearchAlertsJobBEO jobParameters,
            out int previouslyCommittedTaskCount)
        {
            previouslyCommittedTaskCount = 0;
            try
            {
                //It is always try because GetTaskList is not calling.
                // Fix un notified errors - this is self correction of alerts if previous runs failed to update next run date.
                // If not fixed, one failure in chain can stop that specific alert for ever.
                try
                {
                    FixUnnotifiedAlerts();
                }
                catch (Exception ex)
                {
                    LogMessage(Constants.UNNOTIFIED_MESSAGE + ex.Message.Trim(),
                        MethodBase.GetCurrentMethod().Name, EventLogEntryType.FailureAudit, null);
                }
                // Not calling GetTaskList() as no existing tasks are expected.
                // Job can't be paused as it's not seen in job schedule screen.
                // Get tasks from helper function - each Alert is a task
                var searchAlertTasks = m_Helper.GetActiveAlerts
                    (DateTime.UtcNow.AddSeconds(
                        Convert.ToInt32(
                            ApplicationConfigurationManager.GetValue(Constants.OFFSET_DURATION_IN_SECONDS).Trim())),
                        m_SearchAlertsJobBeo.JobScheduleNextRunDate);

                var taskCounter = 0;
                foreach (var alertTask in searchAlertTasks)
                {
                    alertTask.TaskNumber = taskCounter++;
                }

                #region Find last alert's timestamp.

                // Find last date in the current job's iteration (in all alerts).
                // Please note alerts in specified time window are the tasks.
                // Check to verify if there are any tasks/alerts.
                if (searchAlertTasks.Count > Constants.INTEGER_INITIALIZE_VALUE)
                {
                    // To sort Tasks list can't be used. So convert to List
                    var tasks = searchAlertTasks.ToList();

                    // Sort
                    var orderedTasks = from p in tasks
                        orderby p.SearchAlert.NextRunDate descending
                        select p;

                    // Check if there are any elements
                    if (orderedTasks.Count() > Constants.INTEGER_INITIALIZE_VALUE)
                    {
                        // First item is the last alert in the iteration.Set it to Job BEO.
                        m_SearchAlertsJobBeo.HighestDateInIteration = orderedTasks.First().SearchAlert.NextRunDate;
                    }
                }
                return searchAlertTasks;
            }
            catch (EVException ex)
            {
                HandleEVException(ex, MethodBase.GetCurrentMethod().Name, null);
                var jobException = new EVJobException(ex.GetErrorCode(), ex, JobLogInfo);
                throw (jobException);
            }
            catch (Exception ex)
            {
                // Handle exception in Generate Tasks
                LogMessage(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error, null);
                var jobException = new EVJobException(ErrorCodes.ProblemInJobInitialization, ex, JobLogInfo);
                throw (jobException);
            }

            #endregion Find last alert's timestamp.
        }

        /// <summary>
        /// Atomic work 1) Search 2) Notify 3) Update task details back to database.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="jobParameters"></param>
        /// <returns></returns>
        protected override bool DoAtomicWork(SearchAlertsTaskBEO task, SearchAlertsJobBEO jobParameters)
        {
            var statusFlag = Constants.FAILURE; // Function return status.
            var queryTostore = string.Empty;
            DocumentQueryEntity searchBizEntityObj = null;
            try
            {
                //Get Previous Run Details
                var prevAlertRunDetails = m_Helper.GetPreviousAlertRunDetails(task.SearchAlert.AlertId);
                if (prevAlertRunDetails.Count > 0 && prevAlertRunDetails.Count == 7)
                {
                    m_AlertLastRunTimestamp = Convert.ToDateTime(prevAlertRunDetails[0]);
                    m_LastResultCount = Convert.ToInt32(prevAlertRunDetails[1]);
                    m_ManualAlertNextRunDate = Convert.ToDateTime(prevAlertRunDetails[2]);
                    var searchQuery = prevAlertRunDetails[3];
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        searchBizEntityObj =
                            (DocumentQueryEntity)
                                XmlUtility.DeserializeObject(searchQuery, typeof (DocumentQueryEntity));
                        if (searchBizEntityObj != null)
                        {
                            m_PrevSearchQuery =
                                HttpUtility.HtmlDecode(
                                    (searchBizEntityObj.QueryObject.DisplayQuery.Split(new[] {Constants.ThreeTilde},
                                        StringSplitOptions.None))[0]);
                        }
                    }
                    var createdBy = prevAlertRunDetails[6];

                    //the below block of code is required to pass the user details who has actually created or updated the alert so that search can use the same
                    // to check various business rules.
                    var adminUserSession = EVSessionManager.Get<UserSessionBEO>(Constants.UserSessionInfo);
                    var currentAlertUser = UserBO.GetUserUsingGuid(createdBy);
                    if (currentAlertUser != null)
                    {
                        SetContext(adminUserSession, currentAlertUser, true);
                    }
                    var originalQuery = HttpUtility.HtmlDecode(task.SearchAlert.DocumentQuery.QueryObject.DisplayQuery);
                    var generatedQuery = PopulateQuery(task, originalQuery, currentAlertUser.Timezone);

                    //now modify the query in order to distinguish between all results and new results. This will be used in the UI.
                    queryTostore = originalQuery + Constants.QUERY_DELIMETER + generatedQuery.Item3;


                    // 1st time both will be the same so execute search only once.
                    if (generatedQuery.Item1)
                    {
                        // Get only the all doc result count. This will be executed only for 1st time.
                        m_AllResultCount = GetAlertResultCount(searchBizEntityObj, originalQuery);
                        m_NewResultCount = m_AllResultCount;
                    }
                    else
                    {
                        // Get the new result count
                        m_NewResultCount = GetAlertResultCount(searchBizEntityObj, generatedQuery.Item2);
                        // Get the all docs result count
                        m_AllResultCount = GetAlertResultCount(searchBizEntityObj, originalQuery);
                    }

                    ConstructNotificationMessage(task, originalQuery, searchBizEntityObj);
                    statusFlag = Constants.SUCCESS;
                }
            }
            catch (EVException ex)
            {
                HandleEVException(ex, MethodBase.GetCurrentMethod().Name, task);
                statusFlag = Constants.FAILURE;
                var taskException = new EVTaskException(ex.GetErrorCode(), ex, TaskLogInfo);
                throw (taskException);
            }
            catch (Exception ex)
            {
                // Handle exception in initialize
                LogMessage(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error, task);
                statusFlag = Constants.FAILURE;
                var taskException = new EVTaskException(ErrorCodes.ProblemInDoAtomicWork, ex, TaskLogInfo);
                throw (taskException);
            }
            finally
            {
                try
                {
                    task.SearchAlert.ActualOccurrenceCount = task.SearchAlert.ActualOccurrenceCount + 1;
                    task.SearchAlert.DocumentQuery.QueryObject.QueryList.Clear();
                    task.SearchAlert.DocumentQuery.QueryObject.QueryList.AddRange(new List<Query>
                    {
                        new Query
                        {
                            SearchQuery = queryTostore,
                            Precedence = 1
                        }
                    });

                    // 3. Update alert details back to database
                    m_Helper.UpdateAlertPostNotification(task.SearchAlert.ActualOccurrenceCount,
                        DateTime.UtcNow,
                        m_NewResultCount,
                        m_AllResultCount,
                        m_ManualAlertNextRunDate != DateTime.MinValue
                            ? m_ManualAlertNextRunDate
                            : m_Helper.CalculateNextRunTimeStampForAlert(
                                task.SearchAlert.Duration, task.SearchAlert.NextRunDate),
                        task.SearchAlert.AlertId,
                        task.SearchAlert.NotificationId,
                        jobParameters.JobRunId,
                        XmlUtility.SerializeObject(task.SearchAlert.DocumentQuery));
                }
                catch (EVException ex)
                {
                    HandleEVException(ex, MethodBase.GetCurrentMethod().Name, task);
                    statusFlag = Constants.FAILURE;
                }
                catch (Exception ex)
                {
                    // Handle exception in initialize
                    LogMessage(Constants.FAILED_MESSAGE + ex.Message, MethodBase.GetCurrentMethod().Name,
                        EventLogEntryType.Error, task);
                    statusFlag = Constants.FAILURE;
                }
            }
            return statusFlag; // function return status.
        }

        /// <summary>
        /// Before job shuts down, shall update job next run
        /// </summary>
        /// <param name="jobParameters">Job Business Object</param>
        protected override void Shutdown(SearchAlertsJobBEO jobParameters)
        {
            try
            {
                // Log message indicating Shutdown called.
                DateTime dateTimeOffset;

                if (DateTime.Compare(m_SearchAlertsJobBeo.HighestDateInIteration, DateTime.MinValue) ==
                    Constants.INTEGER_INITIALIZE_VALUE ||
                    DateTime.Compare(m_SearchAlertsJobBeo.HighestDateInIteration, Constants.MSSQL2005_MINDATE) ==
                    Constants.INTEGER_INITIALIZE_VALUE)
                {
                    dateTimeOffset = DateTime.UtcNow;
                }
                else
                {
                    dateTimeOffset = m_SearchAlertsJobBeo.HighestDateInIteration;
                }

                // Update Job's next run.
                m_Helper.UpdateAlertJobNextRunDateTime(m_SearchAlertsJobBeo.JobId, dateTimeOffset);
            }
            catch (EVException ex)
            {
                HandleEVException(ex, MethodBase.GetCurrentMethod().Name, null);
            }
            catch (Exception ex)
            {
                // Handle exception
                LogMessage(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error, null);
            }
        }

        #endregion Job Framework functions

        #region Internal/private functions

        /// <summary>
        /// This method is used to construct the notification message and send the notification for the alert
        /// </summary>
        /// <param name="task"></param>
        /// <param name="originalQuery"></param>
        /// <param name="searchBizEntityObj"></param>
        private void ConstructNotificationMessage(SearchAlertsTaskBEO task, string originalQuery,
            DocumentQueryEntity searchBizEntityObj)
        {
            /* send notification only if
                      - All result count between prev alert and current alert is changed
                      - All result count is same but new results found for current alert
                      - if alert is running for the first time
                      - if the prev query and current query is not same */
            if ((m_LastResultCount != m_AllResultCount) ||
                ((m_LastResultCount == m_AllResultCount) && m_NewResultCount > 0) ||
                (m_AlertLastRunTimestamp.Equals(DateTime.MinValue) ||
                 m_AlertLastRunTimestamp.Equals(Constants.MSSQL2005_MINDATE)) ||
                (!String.IsNullOrEmpty(m_PrevSearchQuery) && (!m_PrevSearchQuery.Equals(originalQuery))))
            {
                var dataSetName = (searchBizEntityObj != null && searchBizEntityObj.QueryObject.DatasetId > 0)
                    ? FolderBO.GetFolderDetails(searchBizEntityObj.QueryObject.DatasetId.ToString()).FolderName
                    : String.Empty;
                var reviewSetName = (searchBizEntityObj != null && searchBizEntityObj.QueryObject.MatterId > 0
                                     && !String.IsNullOrEmpty(searchBizEntityObj.QueryObject.ReviewsetId))
                    ? ReviewSetBO.GetReviewSetDetails(searchBizEntityObj.QueryObject.MatterId.ToString(),
                        searchBizEntityObj.QueryObject.ReviewsetId).ReviewSetName
                    : String.Empty;
                // Send notification
                var alertMessage = Constants.ALERT_NOTIFICATION_MESSAGE;
                // modify the notification message if this needs to be sent as part of manual alert
                if (m_ManualAlertNextRunDate != DateTime.MinValue)
                {
                    alertMessage += Constants.MANUAL_ALERT_MESSAGE;
                }
                if (string.IsNullOrEmpty(reviewSetName))
                {
                    alertMessage = m_AllResultCount + alertMessage + Constants.Hyphen +
                                   HttpUtility.HtmlDecode(task.SearchAlert.Name) + Constants.FOR + Constants.DATASET +
                                   HttpUtility.HtmlDecode(dataSetName);
                }
                else
                {
                    alertMessage = m_AllResultCount + alertMessage + Constants.Hyphen +
                                   HttpUtility.HtmlDecode(task.SearchAlert.Name) + Constants.FOR + Constants.DATASET +
                                   HttpUtility.HtmlDecode(dataSetName) + Constants.INREVIEWSET +
                                   HttpUtility.HtmlDecode(reviewSetName);
                }
                if ((((m_LastResultCount == m_AllResultCount) && m_NewResultCount > 0) ||
                     ((m_LastResultCount > m_AllResultCount) && m_NewResultCount == 0)) &&
                    (m_AlertLastRunTimestamp != DateTime.MinValue))
                {
                    alertMessage += Constants.SEARCH_ALERT_RESULTS_CHANGED;
                }
                // this is required to get the details of the person who actually created the alert
                if (!string.IsNullOrEmpty(task.SearchAlert.CreatedBy))
                {
                    m_UserBusinessEntity = UserBO.GetUserUsingGuid(task.SearchAlert.CreatedBy);
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.Append(Constants.Table);
                messageBuilder.Append(Constants.Row);
                messageBuilder.Append(Constants.Column);
                messageBuilder.Append(HttpUtility.HtmlEncode(alertMessage));
                messageBuilder.Append(Constants.CloseColumn);
                messageBuilder.Append(Constants.CloseRow);
                messageBuilder.Append(Constants.Row);
                messageBuilder.Append(Constants.Column);
                messageBuilder.Append(Constants.MessageSearchQuery);
                messageBuilder.Append(HttpUtility.HtmlDecode(originalQuery));
                messageBuilder.Append(Constants.CloseColumn);
                messageBuilder.Append(Constants.CloseRow);
                messageBuilder.Append(Constants.Row);
                messageBuilder.Append(Constants.Column);
                messageBuilder.Append(Constants.MessageDatasetName);
                messageBuilder.Append(HttpUtility.HtmlDecode(dataSetName));
                messageBuilder.Append(Constants.CloseColumn);
                messageBuilder.Append(Constants.CloseRow);
                if (!(string.IsNullOrEmpty(reviewSetName)))
                {
                    messageBuilder.Append(Constants.Row);
                    messageBuilder.Append(Constants.Column);
                    messageBuilder.Append(Constants.MessageReviewsetName);
                    messageBuilder.Append(HttpUtility.HtmlDecode(reviewSetName));
                    messageBuilder.Append(Constants.CloseColumn);
                    messageBuilder.Append(Constants.CloseRow);
                }
                messageBuilder.Append(Constants.CloseTable);
                SendNotification(task.SearchAlert, alertMessage, messageBuilder.ToString());
            }
        }

        /// <summary>
        /// Method to execute the query and get the search results
        /// </summary>
        /// <param name="searchBizEntityObj">Document Query Entity</param>
        /// <param name="query">query to execute</param>
        /// <returns></returns>
        private long GetAlertResultCount(DocumentQueryEntity searchBizEntityObj, string query)
        {
            if (searchBizEntityObj != null)
            {
                searchBizEntityObj.QueryObject.QueryList.Clear();
                searchBizEntityObj.QueryObject.QueryList.AddRange(new List<Query>
                {
                    new Query
                    {
                        SearchQuery = HttpUtility.HtmlDecode(query),
                        Precedence = 1
                    }
                });
                searchBizEntityObj.TransactionName = "AlertsJob - GetAlertResultCount";
                // Do the search to get all results
                return JobSearchHandler.GetSearchResultsCount(searchBizEntityObj.QueryObject);
            }
            return 0;
        }

        /// <summary>
        /// Method to populate the alert query that needs to be executed
        /// </summary>
        /// <param name="task"> Search alert Task</param>
        /// <param name="originalQuery">query to execute</param>
        /// <param name="userTimeZone">user time zone</param>
        /// <returns>Tuple of values</returns>
        private Tuple<bool, string, string> PopulateQuery(SearchAlertsTaskBEO task, string originalQuery,
            string userTimeZone)
        {
            var newQuery = string.Empty;
            var firstTimeSearch = false;
            var queryToStore = string.Empty;
            var queryToStoreDateTime = DateTime.UtcNow;
            var queryDateTime = TimeZoneInfo.ConvertTimeFromUtc(queryToStoreDateTime,
                TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
            var lastAlertRunTimeStamp = TimeZoneInfo.ConvertTimeFromUtc(m_AlertLastRunTimestamp,
                TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));

           

            if (
                !(m_AlertLastRunTimestamp.Equals(DateTime.MinValue) ||
                  m_AlertLastRunTimestamp.Equals(Constants.MSSQL2005_MINDATE)))
            {
                // Modify the query to get only the new results. This will add document created date and modified date conditions.
                // The query gets modified only after 1st run
                // Sample query :- ((object) AND (ImportedDate:["2011/04/26 01:43:10 PM" TO "2011/04/26 01:44:45 PM"] OR
                //                                DocumentModifiedDate:["2011/04/26 01:43:10 PM " TO "2011/04/26 01:44:45 PM"]))
                if (task.SearchAlert.DocumentQuery.QueryObject.QueryList != null &&
                    task.SearchAlert.DocumentQuery.QueryObject.QueryList.Count > 0)
                {
                    newQuery = string.Format("({0}) AND ({1}:[\"{2}\" TO \"{3}\"])",
                        originalQuery, EVSystemFields.DocumentModifiedDate,
                        new EVDateTime(lastAlertRunTimeStamp,EvDateTimeFormat),
                         new EVDateTime(queryDateTime,EvDateTimeFormat));

                    queryToStore = string.Format("({0}) AND ({1}:[\"{2}\" TO \"{3}\"])",
                        originalQuery, EVSystemFields.DocumentModifiedDate,
                        new EVDateTime(m_AlertLastRunTimestamp,EvDateTimeFormat),
                         new EVDateTime(queryToStoreDateTime,EvDateTimeFormat));
                }
            }
            else
            {
                newQuery = string.Format("({0}) AND ({1}:[\"{2}\" TO \"{3}\"])",
                    originalQuery, EVSystemFields.DocumentModifiedDate,
                    new EVDateTime(queryDateTime.AddYears(-10),EvDateTimeFormat),
                     new EVDateTime(queryDateTime,EvDateTimeFormat));

                queryToStore = string.Format("({0}) AND ({1}:[\"{2}\" TO \"{3}\"])",
                   originalQuery, EVSystemFields.DocumentModifiedDate,
                   new EVDateTime(queryToStoreDateTime.AddYears(-10),EvDateTimeFormat),
                    new EVDateTime(queryToStoreDateTime,EvDateTimeFormat));

                firstTimeSearch = true;
            }
            return Tuple.Create(firstTimeSearch, newQuery, queryToStore);
        }

        /// <summary>
        /// method to set the context and pass the user object to search services
        /// </summary>
        /// <param name="adminSession"></param>
        /// <param name="currUser"></param>
        /// <param name="toggle"></param>
        private void SetContext(UserSessionBEO adminSession, UserBusinessEntity currUser, bool toggle)
        {
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            if (toggle)
            {
                var currentSessionUser = new UserSessionBEO
                {
                    UserGUID = currUser.UserGUID,
                    UserId = currUser.UserId,
                    DomainName = currUser.DomainName,
                    Timezone = currUser.Timezone,
                    IsSuperAdmin = currUser.IsSuperAdmin
                };
                mockSession.Setup(ctx => ctx[Constants.UserSessionInfo]).Returns(currentSessionUser);
            }
            else
            {
                mockSession.Setup(ctx => ctx[Constants.UserSessionInfo]).Returns(adminSession);
            }
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
        }

        /// <summary>
        /// This function picks all broken alerts and updates future next run date.
        /// </summary>
        private void FixUnnotifiedAlerts()
        {
            // Get broken alerts
            // That is, old alerts with next run date in past and actual occurrences less than requested occurrences
            // End date is reduced by a second. Otherwise it will include first alert in current job. The hardcoded value -1 is never used again,
            // didn't see a purpose in keeping it in constants,
            var brokenAlerts = m_Helper.GetActiveAlerts(m_SearchAlertsJobBeo.JobScheduleNextRunDate.AddSeconds(-1),
                Constants.MSSQL2005_MINDATE);

            // For each broken alert...
            foreach (var brokenAlert in brokenAlerts)
            {
                var actualOccuranceCount = brokenAlert.SearchAlert.ActualOccurrenceCount;
                var alertNextRunDate = CaliculateFutureAlertRunDate(brokenAlert.SearchAlert.NextRunDate,
                    brokenAlert.SearchAlert.Duration, ref actualOccuranceCount);
                // Calculate next run date till it's a future date and update it back to DB
                // Hit count here is zero as the alert isn't intended to run, rather fix next run date
                m_Helper.UpdateAlertPostNotification(brokenAlert.SearchAlert.ActualOccurrenceCount,
                    brokenAlert.SearchAlert.LastRunDate,
                    Constants.INTEGER_INITIALIZE_VALUE, Constants.INTEGER_INITIALIZE_VALUE, alertNextRunDate,
                    brokenAlert.SearchAlert.AlertId, brokenAlert.SearchAlert.NotificationId,
                    m_SearchAlertsJobBeo.JobRunId,
                    Constants.MESSAGE_ALERT_FIX);
            }
        }

        /// <summary>
        /// For fixing an alert with next run date in past, this function repeatedly adds duration to the alert till next run date is in future.
        /// If 1000 iterations didn't get the alert to future, it would give up and return.
        /// </summary>
        /// <param name="alertNextRunDate">Alert Next run date - supposedly in past and hence it's a broken alert</param>
        /// <param name="duration">Duration by which alert run date need to be incremented.</param>
        /// <param name="actualOccuranceCount"></param>
        /// <returns>alert next run date in future timestamp</returns>
        private DateTime CaliculateFutureAlertRunDate(DateTime alertNextRunDate, int duration,
            ref long actualOccuranceCount)
        {
            var now = DateTime.UtcNow;
            var counter = 0;

            // If alert run date is in future, return the value.
            while (DateTime.Compare(now, alertNextRunDate) > Constants.INTEGER_INITIALIZE_VALUE)
            {
                alertNextRunDate = m_Helper.CalculateNextRunTimeStampForAlert(duration, alertNextRunDate);
                counter = counter + 1;
                actualOccuranceCount = actualOccuranceCount + 1;

                // avoid infinite loop (if it ever happens, after 1000 iterations, comeout...
                if (counter > 1000) break;
            }

            return alertNextRunDate;
        }

        /// <summary>
        /// Logs messages as required by Alerts Job. Created as a separate function so that the job has a consistent way of logging messages.
        /// </summary>
        /// <param name="message">Message to be logged</param>
        /// <param name="messageLocation">Location from which message is being logged - normally it's function name</param>
        /// <param name="eventLogEntryType">Error or Message or Audit entry</param>
        /// <param name="searchAlertTask">Alert Task</param>
        private static void LogMessage(string message, string messageLocation, EventLogEntryType eventLogEntryType,
            SearchAlertsTaskBEO searchAlertTask)
        {
            var msg = ((searchAlertTask != null)
                ? Constants.LOG_MESSAGE_TITLE1 + Constants.LOG_MESSAGE_TITLE4
                  + searchAlertTask.SearchAlert.AlertId.ToString().Trim() + Constants.NEXT_LINE_CHARACTER +
                  Constants.LOG_MESSAGE_TITLE5
                  + searchAlertTask.SearchAlert.Name.Trim() + Constants.NEXT_LINE_CHARACTER
                : string.Empty)
                      + Constants.LOG_MESSAGE_TITLE2 + messageLocation + Constants.NEXT_LINE_CHARACTER
                      + ((message.Equals(string.Empty)) ? string.Empty : Constants.LOG_MESSAGE_TITLE3 + message);

            EvLog.WriteEntry(Constants.LOG_SOURCE, msg, eventLogEntryType);
        }

        /// <summary>
        /// method to send notification for each alert
        /// </summary>
        /// <param name="searchAlertBeo">Search Alert Business Entity</param>
        /// <param name="subject"></param>
        /// <param name="content"></param>
        /// <returns>Success status</returns>
        private void SendNotification(SearchAlertEntity searchAlertBeo, string subject, string content)
        {
            // Log message that notification is being sent.
            var notificationMessageBeo = new NotificationMessageBEO
            {
                CreatedBy = (m_UserBusinessEntity.DomainName.Equals(Constants.NA))
                    ? m_UserBusinessEntity.UserId
                    : m_UserBusinessEntity.DomainName + Constants.NA + m_UserBusinessEntity.UserId,
                NotificationId = searchAlertBeo.NotificationId,
                NotificationSubject = HttpUtility.HtmlEncode(subject),
                NotificationBody = content,
                CreatedByUserGuid = m_UserBusinessEntity.UserGUID,
                CreatedByUserName = (m_UserBusinessEntity.DomainName.Equals(Constants.NA))
                    ? m_UserBusinessEntity.UserId
                    : m_UserBusinessEntity.DomainName + Constants.NA + m_UserBusinessEntity.UserId
            };

            NotificationBO.SendNotificationMessage(notificationMessageBeo);
        }

        /// <summary>
        /// EV Exception if thrown use error code for locating message from resource file.
        /// This function logs the message as well...
        /// </summary>
        /// <param name="ex">EV specific application error.</param>
        /// <param name="location">Location from which message is being logged - normally it's function name</param>
        /// <param name="searchAlertTask"></param>
        /// <returns>Success Status.</returns>
        public static void HandleEVException(Exception ex, string location, SearchAlertsTaskBEO searchAlertTask)
        {
            var errorMessage = Msg.FromRes(ex.GetErrorCode());
            LogMessage(errorMessage + Constants.NEXT_LINE_CHARACTER + ex.ToUserString(), location,
                EventLogEntryType.Error, searchAlertTask);
        }

        #endregion Internal/private functions
    }
}