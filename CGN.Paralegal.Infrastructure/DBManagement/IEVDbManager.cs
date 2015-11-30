//---------------------------------------------------------------------------------------------------
// <copyright file="IEVDbManager.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ramanujam Sampath</author>
//      <description>
//          Interface class for the EVDbManager class.
//      </description>
//      <changelog>
//          <date value="04/23/2013">Task # 136410 - ADM -ADMIN-003 -  Moving conversion log to vault database </date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;

namespace CGN.Paralegal.Infrastructure.DBManagement
{
    /// <summary>
    /// Interface class for the EvDbManager which wraps the DatatabaseFactory class.
    /// </summary>
    public interface IEVDbManager
    {
        /// <summary>
        /// Interface method used to add input parameters to the command object
        /// </summary>
        /// <param name="command">The command to add the out parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="databaseType">One of the DbType values</param>
        /// <param name="value">The value of the parameter</param>
        void AddInParameter(DbCommand command, string name, DbType databaseType, object value);

        /// <summary>
        /// Interface method used to add output parameters to the command object
        /// </summary>
        /// <param name="command">Dbcommand to which the parameter needs to be added</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="dataBaseType">DB Type of the parameter</param>
        /// <param name="size">Value of the parameter passed</param>
        void AddOutParameter(DbCommand command, string name, DbType dataBaseType, int size);

        /// <summary>
        /// Interface method used to add input table parameter to the command object
        /// </summary>
        /// <param name="command">The command to add the out parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">collection to be added as a structured parameter</param>
        void AddInputTableParameter(DbCommand command, string name, object value);

        /// <summary>
        /// Execute bulk copy 
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="data">Data Table</param>
        /// <param name="tableName">Table Name</param>
        bool ExecuteBulkCopy(string connectionString, DataTable data, string tableName);

        /// <summary>
        /// Interface member executes the command and returns the results in a new System.Data.Dataset.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>A System.Data.DataSet with the results of the command.</returns>
        DataSet ExecuteDataSet(DbCommand command);

        /// <summary>
        /// Interface member executes the command and returns the number of rows affected
        /// </summary>
        /// <param name="command">The command that contains the query to execute</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteNonQuery(DbCommand command);

        /// <summary>
        /// Interface member executes the command that returns the first column of the first row in the result set returned by the query.
        /// Extra columns or rows are ignored
        /// </summary>
        /// <param name="command">The command that contains the query to execute</param>
        /// <returns>The first column of the first row in the result set.</returns>
        object ExecuteScalar(DbCommand command);

        /// <summary>
        /// Interface member which gets the value of a parameter
        /// </summary>
        /// <param name="command">The command that contains the parameter</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns> The value of the parameter.</returns>
        object GetParameterValue(DbCommand command, string parameterName);

        /// <summary>
        /// Interface member used to get the stored procedure command
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <returns>The DbCommand for the stored procedure.</returns>
        DbCommand GetStoredProcCommand(string storedProcedureName);

        /// <summary>
        /// Interface member used to get the stored procedure command
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <param name="commandTimeOut">Database command time out</param>
        /// <returns>The DbCommand for the stored procedure.</returns>
        DbCommand GetStoredProcCommand(string storedProcedureName,int commandTimeOut);

        /// <summary>
        /// Get connection string for a given Matter ID.
        /// </summary>
        /// <param name="matterId">matter Id</param>
        /// <returns>Connection string.</returns>
        string GetConnectionStringForMatterId(long matterId);


        /// <summary>
        /// Gets the database name for the matter from the master database.
        /// </summary>
        /// <returns>datanbase name</returns>
        string GetDatabaseNameForMatterId();


       
        /// <summary>
        /// Get connection string for EVMaster database
        /// </summary>
        /// <returns></returns>
        string GetConnectionStringForEvMaster();

        /// <summary>
        /// Retrieves the connection string url, of the search-sub-system for the specified matter identifier
        /// </summary>
        /// <returns>string</returns>
        string GetSearchServerUrl();

        /// <summary>
        /// Create the manager object for "Default" database based on server id.
        /// </summary>
        /// <param name="serverId">SQL Server ID to uniquely identify the SQL server.</param>
        void CreateConnection(Guid serverId);

         DbCommand CreateTextCommand(string commandText);
         /// <summary>
         /// Takes Db Command And  returns DbDataReader
         /// </summary>
         /// <param name="dbCommand"></param>
         /// <returns>Db DataReader</returns>
         DbDataReader ExecuteDataReader(DbCommand dbCommand);
    }
}
