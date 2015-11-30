# region File Header
/// <copyright file="ServerStatusDataAccess.cs" company="Cognizant">
///     Copyright (c) Cognizant. All rights reserved.
/// </copyright>
/// <header>
///      <author>Swamy</author>
///     <description>
///         This file contain ping and update the status of server
///      </description>
///      <changelog>
///          <date value=""></date>
///      </changelog>
/// </header>
# endregion
#region Namespace
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Net.NetworkInformation;
using System.Threading;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.ServerManagement;
using LexisNexis.Evolution.Infrastructure.DBManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
#endregion
namespace LexisNexis.Evolution.BatchJobs.ServerManagement
{
    public class ServerStatusDataAccess : IDisposable
    {
        Dictionary<string, Ping> pingStatus = new Dictionary<string, Ping>();
        AutoResetEvent waiter = new AutoResetEvent(false);
        #region Code
        /// <summary>
        /// ping server 
        /// </summary>
        public void PingServer()
        {
            try
            {
                List<ServerBEO> serverList = ServerDAO.GetAllServers();
                foreach (ServerBEO server in serverList)
                {
                    Ping ping = new Ping();
                    pingStatus.Add(server.Name, ping);
                    ping.PingCompleted += new PingCompletedEventHandler(PingCompleted);
                    ping.SendAsync(server.HostId, server);
                    waiter.WaitOne();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Handle ping completed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">PingCompletedEventArgs</param>

        private void PingCompleted(object sender, PingCompletedEventArgs e)
        {
            bool status = true;
            ServerBEO server = (ServerBEO)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
                status = true;
            else
                status = false;
            UpdateServerStatus(server.Id, server.ServerType, status);
            waiter.Set();
            IDisposable obj = (Ping)pingStatus[server.Name];
            obj.Dispose();
            pingStatus.Remove(server.Name);
        }

        /// <summary>
        /// update server status to DB
        /// </summary>
        /// <param name="serverId">server id</param>
        /// <param name="serverType">server Type</param>
        /// <param name="status"> </param>
        /// <returns>Boolean</returns>
        private bool UpdateServerStatus(string serverId, int serverType, bool status)
        {
            bool returnFlag = true;
            using (EVTransactionScope transaction = new EVTransactionScope())
            {
                int result;
                EVDbManager dbManager = new EVDbManager();
                DbCommand dbCommand = dbManager.GetStoredProcCommand(Constants.SpEvSvrUpdateServerStatus);

                /* Input Parameters for SP : EV_SVR_Update_ServerStatus*/
                dbManager.AddInParameter(dbCommand, Constants.ParamInServerID, DbType.String, serverId);
                dbManager.AddInParameter(dbCommand, Constants.ParamInServerType, DbType.Int32, serverType);
                dbManager.AddInParameter(dbCommand, Constants.ParamInServerStatus, DbType.Boolean, status);
                dbManager.AddOutParameter(dbCommand, Constants.ParaOutStatus, DbType.Int32, 4);
                dbManager.ExecuteNonQuery(dbCommand);
                result = Convert.ToInt32(dbManager.GetParameterValue(dbCommand, Constants.ParaOutStatus));
                returnFlag = result != -1;
                transaction.Complete();
            }
            return returnFlag;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (waiter != null)
            {
                waiter.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
