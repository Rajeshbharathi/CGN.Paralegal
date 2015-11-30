using System;
using System.Collections.Generic;
using System.Configuration;
using CGN.Paralegal.ClientContracts.Analytics;
using CGN.Paralegal.Mocks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using NLog;

namespace CGN.Paralegal.UI.App.common.notifications
{
    using ClientContracts.AppState;
    using System.Globalization;

    public class WorkflowStateHub : Hub
    {
        private const string AnalyticsNotifications = "AnalyticsNotifications";
        private static readonly Logger Nlog = LogManager.GetLogger("PCWeb");

        /// <summary>
        /// Registers the notification.
        /// </summary>
        /// <param name="appState">State of the application.</param>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">AnalyticsNotifications Service URL setting is null or empty</exception>
        public void RegisterNotification(AppState appState)
        {
            try
            {
                //Adding to group by projectId
                Nlog.Log(LogLevel.Trace, "PC Web - Start addining connection into group");
                Groups.Add(Context.ConnectionId, appState.ProjectId.ToString(CultureInfo.InvariantCulture));
                Nlog.Log(LogLevel.Trace, "PC Web - Connection added to group");

                if (BaseApiController.IsMockMode())
                {
                    MockWorkflowState.WorkflowStateChanged += OnMockWorkflowStateChanged;
                }
                else
                {
                    Nlog.Log(LogLevel.Trace, "PC Web - Fired RegisterNotification");
                    var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsNotifications);
                    if (String.IsNullOrEmpty(serviceUri))
                    {
                        throw new ConfigurationErrorsException("AnalyticsNotifications Service URL setting is null or empty");
                    }
                    var hubConnection = new HubConnection(serviceUri);
                    var serviceHubProxy = hubConnection.CreateHubProxy("WorkflowStateServiceHub");

                    //Handle incoming calls from service
                    //BroadcastWorkflowState for updated project
                    serviceHubProxy.On<long, List<AnalyticsWorkflowState>>("UpdateWorkflowState", BroadcastWorkflowState);

                    //Signalr service connection 
                    Nlog.Log(LogLevel.Trace, "PC Web - Starting service signalr connection");
                    hubConnection.Start().ContinueWith(task => {}).Wait();
                    Nlog.Log(LogLevel.Trace, "PC Web - Service signalr connection success");

                    //Register for notifications
                    Nlog.Log(LogLevel.Trace, "PC Web - Invoking service signalr RegisterNotification");
                    serviceHubProxy.Invoke("RegisterNotification", appState.MatterId, appState.ProjectId)
                        .ContinueWith(task =>{}).Wait();
                    Nlog.Log(LogLevel.Trace, "PC Web - Invoked service signalr RegisterNotification");
                }
            }
            catch (Exception ex)
            {
                Nlog.Log(LogLevel.Error, ex.GetBaseException());
            }
        }

        /// <summary>
        /// Broadcasts the changed workflow state.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="workflowState">State of the workflow.</param>
        private void BroadcastWorkflowState(long projectId, List<AnalyticsWorkflowState> workflowState)
        {
            Nlog.Log(LogLevel.Trace, string.Format(CultureInfo.InvariantCulture, "PC Web - Start broadcasting for ProjectId: {0}", projectId));
            Clients.Group(projectId.ToString(CultureInfo.InvariantCulture)).updateWorkflowState(projectId, workflowState);
            Nlog.Log(LogLevel.Trace, string.Format(CultureInfo.InvariantCulture, "PC Web - End broadcasting for ProjectId: {0}", projectId));
        }

        /// <summary>
        /// Called when [mock workflow state changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkflowStateChangedEventArgs"/> instance containing the event data.</param>
        private void OnMockWorkflowStateChanged(object sender, WorkflowStateChangedEventArgs e)
        {
            BroadcastWorkflowState(e.ProjectId, e.WorkflowState);
        }
    }
}