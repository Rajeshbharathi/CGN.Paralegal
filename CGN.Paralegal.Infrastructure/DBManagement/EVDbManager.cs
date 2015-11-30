//---------------------------------------------------------------------------------------------------
// <copyright file="EVDbManager.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Srinivasan Sivasubramanian</author>
//      <description>
//          This file contains the EVDbManager class.
//      </description>
//      <changelog>
//          <date value="28April2011">Modified exception management</date>
//          <date value="20Jun2012">Fix for bug# 102575</date>
//          <date value="04/23/2013">Task # 136410 - ADM -ADMIN-003 -  Moving conversion log to vault database </date>
//          <date value="08/06/2013">Binary Externalization Implementation</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using CGN.Paralegal.TraceServices;

namespace CGN.Paralegal.Infrastructure.DBManagement
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Transactions;

    using ExceptionManagement;

    public class EVDbManager : IEVDbManager
    {
        public string ConnString { get; private set; }

        private const int NumOfRetries = 3;

        public long MatterIdentifier;
        private EVDbManager(string connString, bool unused)
        {
            ConnString = connString;
        }

        /// <summary>
        /// Initializes a new instance of the EVDbManager class.
        /// Created EVDBManager object based on Connection String read from Configuration file.
        /// </summary>
        /// <param name="keyToConnectionStringInConfigFile">Key to connection string in configuration file.</param>
        public EVDbManager(string keyToConnectionStringInConfigFile)
            : this(ConfigurationManager.ConnectionStrings[keyToConnectionStringInConfigFile].ToString(), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the EVDbManager class.
        /// Uses default connection string key from configuration file.
        /// </summary>
        public EVDbManager()
            : this(Constants.DB_CON_STR)
        {
        }

        /// <summary>
        /// Initializes a new instance of the EVDbManager class.
        /// Create the manager object for "Default" database based on server id.
        /// </summary>
        /// <param name="serverId">SQL Server ID to uniquely identify the SQL server.</param>
        public EVDbManager(Guid serverId)
            : this(ServerIdToConnectionString(serverId), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the EVDbManager class.
        /// Creates EVDbManager based on connection Properties
        /// </summary>
        /// <param name="hostId">IP address of the DB server</param>
        /// <param name="userId">User ID for the database</param>
        /// <param name="password">Password for the database</param>
        /// <param name="databaseName">Database Name</param>
        public EVDbManager(string hostId, string userId, string password, string databaseName)
            : this(String.Format(
                            CultureInfo.InvariantCulture,
                            "Data Source={0};User ID={1};Password={2};Initial Catalog={3}",
                            hostId,
                            userId,
                            password,
                            databaseName), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the EVDbManager class.
        /// Create the manager object for "Matter" database based on server id.
        /// </summary>
        /// <param name="matterId">Matter ID for which database object need to be created.</param>
        public EVDbManager(long matterId)
            : this(MatterIdToConnectionString(matterId), false)
        {
            MatterIdentifier = matterId;
        }

        /// <summary>
        /// Retrieves the connection string url for the specified matter identifier
        /// </summary>
        /// <returns>string</returns>
        public string GetSearchServerUrl()
        {
            return GetSearchServerUrlForMatterIdStatic(MatterIdentifier);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static string GetSearchServerUrlForMatterIdStatic(long matterId)
        {
            var hostId = string.Empty;

            lock (MatterIdToSearchServerUrlCache)
            {
                string connString;
                if (MatterIdToSearchServerUrlCache.TryGetValue(matterId, out connString))
                {
                    return connString;
                }

                using (DbConnection masterConnection = OpenConnectionInternal
                    (ConfigurationManager.ConnectionStrings[Constants.DB_CON_STR].ToString()))
                using (DbCommand dbCommand = Factory.CreateCommand())
                {
                    Debug.Assert(dbCommand != null, "dbCommand != null");
                    dbCommand.CommandText = Constants.GetCmgSearchServerDetails;
                    dbCommand.CommandType = CommandType.StoredProcedure;
                    dbCommand.Connection = masterConnection;

                    DbParameter dbParameter = Factory.CreateParameter();
                    Debug.Assert(dbParameter != null, "dbParameter != null");
                    dbParameter.ParameterName = Constants.InputParamGetMatterDBDetails;
                    dbParameter.DbType = DbType.Int64;
                    dbParameter.Value = matterId;
                    dbCommand.Parameters.Add(dbParameter);

                    var objDr = dbCommand.ExecuteReader();
                    // Read the first record the record set.
                    if (objDr.Read())
                    {
                        // Scan through all columns - look for columns we need to create connection string.
                        for (var i = 0; i < objDr.FieldCount; i++)
                        {
                            if (!objDr.GetName(i)
                                .Equals("HostId", StringComparison.OrdinalIgnoreCase)) continue;
                            hostId = objDr.GetString(i);
                            break;
                        }
                    }
                }
                connString = GetSearchSubSystemUrl(hostId);
                MatterIdToSearchServerUrlCache.Add(matterId, connString);
                return connString;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static string GetSearchSubSystemUrl(string hostId)
        {
            var urlString = string.Empty;
            using (var masterConnection = OpenConnectionInternal
                    (ConfigurationManager.ConnectionStrings[Constants.DB_CON_STR].ToString()))
            using (var dbCommand = Factory.CreateCommand())
            {
                Debug.Assert(dbCommand != null, "dbCommand != null");
                dbCommand.CommandText = Constants.GetCmgInstanceConfig;
                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.Connection = masterConnection;

                var dbParameter = Factory.CreateParameter();
                Debug.Assert(dbParameter != null, "dbParameter != null");
                dbParameter.ParameterName = "@in_sHostID";
                dbParameter.DbType = DbType.String;
                dbParameter.Value = hostId;
                dbCommand.Parameters.Add(dbParameter);

                dbParameter = Factory.CreateParameter();
                Debug.Assert(dbParameter != null, "dbParameter != null");
                dbParameter.ParameterName = "@in_sServiceName";
                dbParameter.DbType = DbType.String;
                dbParameter.Value = "Search Service";
                dbCommand.Parameters.Add(dbParameter);

                dbParameter = Factory.CreateParameter();
                Debug.Assert(dbParameter != null, "dbParameter != null");
                dbParameter.ParameterName = "@in_sConfigName";
                dbParameter.DbType = DbType.String;
                dbParameter.Value = "SearchServerUrl";
                dbCommand.Parameters.Add(dbParameter);

                var objDr = dbCommand.ExecuteReader();
                // Read the first record the record set.
                if (!objDr.Read()) return string.Empty;
                // Scan through all columns - look for columns we need to create connection string.
                for (var i = 0; i < objDr.FieldCount; i++)
                {
                    // Add Data Source to the connection string

                    if (!objDr.GetName(i)
                        .Equals("InstanceValue", StringComparison.OrdinalIgnoreCase)) continue;
                    urlString = objDr.GetString(i);
                    break;
                }
            }
            return urlString;
        }


        private static string MatterIdToConnectionString(long matterId)
        {
            string connectionString = GetConnectionStringForMatterIdStatic(matterId);

            if (!connectionString.Contains("Asynchronous Processing"))
            {
                connectionString += ";Asynchronous Processing=true";
            }
            return connectionString;
        }

        /// <summary>
        /// Used to add input parameters to the command object
        /// </summary>
        /// <param name="command">Dbcommand to which the parameter needs to be added</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="dbType">DB Type of the parameter</param>
        /// <param name="value">Value of the parameter passed</param>
        public void AddInParameter(DbCommand command, string name, DbType dbType, object value)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            DbParameter dbParameter = Factory.CreateParameter();
            Debug.Assert(dbParameter != null, "dblParameter != null");
            dbParameter.ParameterName = name;
            dbParameter.DbType = dbType;
            dbParameter.IsNullable = true;
            dbParameter.Direction = ParameterDirection.Input;
            dbParameter.Value = value ?? DBNull.Value;

            command.Parameters.Add(dbParameter);
        }

        /// <summary>
        /// Used to add input parameters to the command object
        /// </summary>
        /// <param name="command">Dbcommand to which the parameter needs to be added</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter passed</param>
        public void AddInputTableParameter(DbCommand command, string name, object value)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            SqlParameter sqlParameter = new SqlParameter
                {
                    ParameterName = name,
                    SqlDbType = SqlDbType.Structured,
                    Direction = ParameterDirection.Input,
                    Value = value
                };

            command.Parameters.Add(sqlParameter);
        }

        public void AddInputTableParameter(DbCommand command, string name, object value, string typeName)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            SqlParameter sqlParameter = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                TypeName = typeName,
                Direction = ParameterDirection.Input,
                Value = value
            };

            command.Parameters.Add(sqlParameter);
        }

        /// <summary>
        /// Execute bulk copy 
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="data">Data Table</param>
        /// <param name="tableName">Table Name</param>
        public bool ExecuteBulkCopy(string connectionString,DataTable data,string tableName)
        {
            var numberofColumns=   data.Columns.Count;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = tableName;
                    for (var i = 0; i < numberofColumns; i++)
                    {
                        bulkCopy.ColumnMappings.Add(i, i);
                    }
                    bulkCopy.WriteToServer(data);
                }
            }
            return true;
        }

        /// <summary>
        /// Used to add output parameters to the commandobject
        /// </summary>
        /// <param name="command">The command to add the out parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="dbType">One of the System.Data.DbType values</param>
        /// <param name="size">The value of the parameter</param>
        public void AddOutParameter(DbCommand command, string name, DbType dbType, int size)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            DbParameter dbParameter = Factory.CreateParameter();
            Debug.Assert(dbParameter != null, "dblParameter != null");
            dbParameter.ParameterName = name;
            dbParameter.DbType = dbType;
            dbParameter.Direction = ParameterDirection.Output;
            dbParameter.Size = size;

            command.Parameters.Add(dbParameter);
        }

        /// <summary>
        /// Create the manager object for "Default" database based on server id.
        /// </summary>
        /// <param name="serverId">SQL Server ID to uniquely identify the SQL server.</param>
        public void CreateConnection(Guid serverId)
        {
            ConnString = ServerIdToConnectionString(serverId);
        }

        private const string Provider = "System.Data.SqlClient";
        private static DbProviderFactory Factory
        {
            get
            {
                return DbProviderFactories.GetFactory(Provider);
            }
        }

        private DbConnection OpenConnectionInternal()
        {
            return OpenConnectionInternal(ConnString);
        }

        private static DbConnection OpenConnectionInternal(string connString)
        {
            if (DirtyHacksForConfigurationServices.RunByUnitTest)
            {
                //string entry = Environment.StackTrace;
                //File.AppendAllText(@"C:\DBaccess005.txt", entry + Environment.NewLine + Environment.NewLine + Environment.NewLine);
                throw new EVException().AddDbgMsg("Unit test tries to access DB!");
            }

            DbConnection dbConnection = Factory.CreateConnection();
            Debug.Assert(dbConnection != null, "dbConnection != null");
            dbConnection.ConnectionString = connString;

            dbConnection.Open();

            return dbConnection;
        }

        /// <summary>
        /// Executes the command and returns the results in a new System.Data.Dataset.
        /// </summary>
        /// <param name="dbCommand">The command to execute</param>
        /// <returns>A System.Data.DataSet with the results of the command.</returns>
        public DataSet ExecuteDataSet(DbCommand dbCommand)
        {
            DbDataAdapter dbDataAdapter = Factory.CreateDataAdapter();
            Debug.Assert(dbDataAdapter != null, "dbDataAdapter != null");
            dbDataAdapter.SelectCommand = dbCommand;
            DataSet dataSet = new DataSet();
            Retry(() => dbDataAdapter.Fill(dataSet), dbCommand);
            return dataSet;
        }
        /// <summary>
        /// Takes Db Command And  returns DbDataReader
        /// </summary>
        /// <param name="dbCommand"></param>
        /// <returns>Db DataReader</returns>
        public DbDataReader ExecuteDataReader(DbCommand dbCommand)
        {
            dbCommand.Connection = OpenConnectionInternal();
            return dbCommand.ExecuteReader(CommandBehavior.CloseConnection);
            //return RetryKeepConnection(dbCommand.ExecuteReader, dbCommand);
        }



        /// <summary>
        /// Executes the command and returns the number of rows affected
        /// </summary>
        /// <param name="dbCommand">The command that contains the query to execute</param>
        /// <returns>The number of rows affected.</returns>
        public int ExecuteNonQuery(DbCommand dbCommand)
        {
            return Retry(dbCommand.ExecuteNonQuery, dbCommand);
        }

        /// <summary>
        /// Executes the command that returns the first column of the first row in the resuktset returned by the query.
        /// Extra columns or rows are ignored
        /// </summary>
        /// <param name="dbCommand">The command that contains the query to execute</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar(DbCommand dbCommand)
        {
            return Retry(dbCommand.ExecuteScalar, dbCommand);
        }

        public T Retry<T>(Func<T> action, DbCommand dbCommand)
        {
            using (DbConnection dbConnection = OpenConnectionInternal())
            {
                dbCommand.Connection = dbConnection;
                return RetryKeepConnection(action, dbCommand);
            } // using
        }

        public T RetryKeepConnection<T>(Func<T> action, DbCommand dbCommand)
        {
            for (int attempt = 0; attempt < NumOfRetries; attempt++)
            {
                try
                {
                    return action();
                }
                catch (SqlException ex)
                {
                    string message = ex.Message;
                    if (message.Length == 4) // Stored proc returned four digit ErrorCode in the Message
                    {
                        ex.AddResMsg(message);
                        ex.AddDbgMsg(BuildSQL(dbCommand));
                        ex.AddDbgMsg(GetErrorDetails(ex));
                        throw;
                    }

                    if (/*ex.ErrorCode != 10054 || */ attempt >= NumOfRetries - 1)
                    {
                        ex.AddDbgMsg(BuildSQL(dbCommand));
                        ex.AddDbgMsg(GetErrorDetails(ex));
                        throw;
                    }

                    ex.AddDbgMsg("DB operation attempt #{0} failed and will be retried.", attempt);
                    ex.AddDbgMsg(BuildSQL(dbCommand));
                    ex.AddDbgMsg(GetErrorDetails(ex));
                    ex.Trace().Swallow();
                    Thread.Sleep(100);
                } // catch
            } // for
            throw new EVException().AddDbgMsg("Unreachable code");
        }

        /// <summary>
        /// Gets the connectionstring for the matter from the master database.
        /// </summary>
        /// <param name="matterId">Matter Id for which the connection string is requested for.</param>
        /// <returns>A connection string for the matter id passed</returns>
        public string GetConnectionStringForMatterId(long matterId)
        {
            return GetConnectionStringForMatterIdStatic(matterId);
        }

        /// <summary>
        /// Gets the database name for the matter from the master database.
        /// </summary>
        /// <returns>datanbase name</returns>
        public string GetDatabaseNameForMatterId()
        {
            return GetDatabaseNameForMatterIdStatic(MatterIdentifier);
        }


        /// <summary>
        /// Get connection string for EVMaster database.
        /// </summary>
        /// <returns></returns>
        public string GetConnectionStringForEvMaster()
        {
            return ConnString;
        }

        private static readonly Dictionary<Guid, string> ServerIdToConnectionStringCache = new Dictionary<Guid, string>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static string ServerIdToConnectionString(Guid serverId)
        {
            lock (ServerIdToConnectionStringCache)
            {
                string connString;
                if (ServerIdToConnectionStringCache.TryGetValue(serverId, out connString))
                {
                    return connString;
                }

                using (DbConnection masterConnection = OpenConnectionInternal(ConfigurationManager.ConnectionStrings[Constants.DB_CON_STR].ToString()))
                using (DbCommand dbCommand = Factory.CreateCommand())
                {
                    Debug.Assert(dbCommand != null, "dbCommand != null");
                    dbCommand.CommandText = Constants.GetCmgSqlServerDetails;
                    dbCommand.CommandType = CommandType.StoredProcedure;
                    dbCommand.Connection = masterConnection;

                    DbParameter dbParameter = Factory.CreateParameter();
                    Debug.Assert(dbParameter != null, "dbParameter != null");
                    dbParameter.ParameterName = Constants.InputParamGetServerIdDetails;
                    dbParameter.DbType = DbType.Guid;
                    dbParameter.Value = serverId;
                    dbCommand.Parameters.Add(dbParameter);

                    IDataReader reader = dbCommand.ExecuteReader();
                    connString = CreateConnectionString(reader);
                }

                ServerIdToConnectionStringCache.Add(serverId, connString);
                return connString;
            }
        }

        private static readonly Dictionary<long, string> MatterIdToConnectionStringCache = new Dictionary<long, string>();
        private static readonly Dictionary<long, string> MatterIdToDatabaseNameCache = new Dictionary<long, string>();
        private static readonly Dictionary<long, string> MatterIdToSearchServerUrlCache = new Dictionary<long, string>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static string GetConnectionStringForMatterIdStatic(long matterId)
        {
            lock (MatterIdToConnectionStringCache)
            {
                string connString;
                if (MatterIdToConnectionStringCache.TryGetValue(matterId, out connString))
                {
                    return connString;
                }

                using (DbConnection masterConnection = OpenConnectionInternal(ConfigurationManager.ConnectionStrings[Constants.DB_CON_STR].ToString()))
                using (DbCommand dbCommand = Factory.CreateCommand())
                {
                    Debug.Assert(dbCommand != null, "dbCommand != null");
                    dbCommand.CommandText = Constants.GetMatterDBDetails;
                    dbCommand.CommandType = CommandType.StoredProcedure;
                    dbCommand.Connection = masterConnection;

                    DbParameter dbParameter = Factory.CreateParameter();
                    Debug.Assert(dbParameter != null, "dbParameter != null");
                    dbParameter.ParameterName = Constants.InputParamGetMatterDBDetails;
                    dbParameter.DbType = DbType.Int64;
                    dbParameter.Value = matterId;
                    dbCommand.Parameters.Add(dbParameter);

                    IDataReader reader = dbCommand.ExecuteReader();
                    connString = CreateConnectionString(reader,matterId);
                }

                MatterIdToConnectionStringCache.Add(matterId, connString);
                return connString;
            }
        }


        /// <summary>
        /// Method retrieves the database name for specified matter identifier
        /// </summary>
        /// <param name="matterId">long</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static string GetDatabaseNameForMatterIdStatic(long matterId)
        {
            matterId.ShouldBeGreaterThan(0);
            var databaseName= (MatterIdToDatabaseNameCache==null 
                              ||!MatterIdToDatabaseNameCache.ContainsKey(matterId))
                              ? null : MatterIdToDatabaseNameCache[matterId];
            databaseName.ShouldNotBeEmpty();
            return databaseName;
        }

       

    

     

        /// <summary>
        /// Gets the value of a parameter
        /// </summary>
        /// <param name="dbCommand">The command that contains the parameter</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns> The value of the parameter.</returns>
        public object GetParameterValue(DbCommand dbCommand, string parameterName)
        {
            return dbCommand.Parameters[parameterName].Value;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public DbCommand CreateTextCommand(string commandText)
        {
            DbCommand dbCommand = Factory.CreateCommand();
            Debug.Assert(dbCommand != null, "dbCommand != null");
            dbCommand.CommandText = commandText;
            dbCommand.CommandType = CommandType.Text;
            return dbCommand;
        }

        /// <summary>
        /// Used to gets the stored procedure command
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <returns>The System.Data.Common.DbCommand for the stored procedure.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public DbCommand GetStoredProcCommand(string storedProcedureName)
        {
            DbCommand dbCommand = Factory.CreateCommand();
            Debug.Assert(dbCommand != null, "dbCommand != null");
            dbCommand.CommandText = storedProcedureName;
            dbCommand.CommandType = CommandType.StoredProcedure;
            return dbCommand;
        }

        /// <summary>
        /// Used to gets the stored procedure command
        /// </summary>
        /// <param name="storedProcedureName">Name of the stored procedure</param>
        /// <param name="commandTimeOut">Database command time out</param>
        /// <returns>The System.Data.Common.DbCommand for the stored procedure.</returns>
        public DbCommand GetStoredProcCommand(string storedProcedureName, int commandTimeOut)
        {
            var dbCommand = GetStoredProcCommand(storedProcedureName);
            dbCommand.CommandTimeout = commandTimeOut;
            return dbCommand;
        }

        /// <summary>
        /// Create connection string using Data Reader.
        /// </summary>
        /// <param name="objDr">Data reader holding connection string information.</param>
        /// <param name="matterId"></param>
        /// <returns>A connection string for the datareader passed</returns>
        private static string CreateConnectionString(IDataReader objDr,long matterId=0)
        {
            string databaseName = null;
            //string connectionstring = string.Empty;
            var connectionString = new DbConnectionStringBuilder();

            
            // Read the first record the record set.
            if (objDr.Read())
            {
                // Scan through all columns - look for columns we need to create connection string.
                for (int i = 0; i < objDr.FieldCount; i++)
                {
                    // Add Data Source to the connection string

                    if (
                        objDr.GetName(i).ToLower(CultureInfo.CurrentCulture).Equals(
                            Constants.DataBaseDataSourceColumn.ToLower(CultureInfo.CurrentCulture)))
                    {
                        connectionString[Constants.DataBaseDataSource] = objDr.GetString(i);
                    }
                    else if (
                        objDr.GetName(i).ToLower(CultureInfo.CurrentCulture).Equals(
                            Constants.DataBaseInitialCatalogColumn.ToLower(CultureInfo.CurrentCulture)))
                    {
                          connectionString[Constants.DataBaseInitialCatalog] = databaseName=objDr.GetString(i);
                    }
                    else if (
                        objDr.GetName(i).ToLower(CultureInfo.CurrentCulture).Equals(
                            Constants.DataBaseUserIdColumn.ToLower(CultureInfo.CurrentCulture)))
                    {
                        connectionString[Constants.DataBaseUserId] = objDr.GetString(i);
                    }
                    else if (
                        objDr.GetName(i).ToLower(CultureInfo.CurrentCulture).Equals(
                            Constants.DataBasePasswordColumn.ToLower(CultureInfo.CurrentCulture)))
                    {
                        connectionString[Constants.DataBasePassword] = objDr.GetString(i);
                    }
                }
            }
            if(matterId>0&&!MatterIdToDatabaseNameCache.ContainsKey(matterId))
              MatterIdToDatabaseNameCache.Add(matterId, databaseName);
            //DEVBug 130146 - Failover issue - adding network library (TCP/IP protocol since we suspect it is using named pipes)
            connectionString.Add("Network Library", "dbmssocn");
            return connectionString.ToString();
        }

        protected string BuildSQL(DbCommand dbCommand)
        {
            if (dbCommand == null)
            {
                return "dbCommand is null";
            }

            SqlCommand sqlCommand = dbCommand as SqlCommand;
            if (sqlCommand == null)
            {
                return "dbCommand is not an SqlCommand, but is " + dbCommand.GetType().Name;
            }

            try
            {
                return BuildSQL(sqlCommand).Trim(new char[] {'\r', '\n'});
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("BuildSQL failed to convert sqlCommand to text").Trace().Swallow();
                return ex.ToDebugString();
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///   Writes out the SQL to execute a stored procedure
        ///   Based on https://www.simple-talk.com/dotnet/.net-framework/.net-application-architecture-logging-sql-exceptions-in-the-data-access-layer/
        /// </summary>
        /// <param name="cmd">Command for which the SQL should be built</param>
        ////////////////////////////////////////////////////////////////////////
        protected string BuildSQL(SqlCommand cmd)
        {
            StringBuilder errorSQL = new StringBuilder();

            //Create Parameter Declaration Statements
            for (int index = 0; index < cmd.Parameters.Count; index++)
            {
                if (cmd.Parameters[index].Direction != ParameterDirection.Input)
                {
                    errorSQL.Append("DECLARE ");
                    errorSQL.Append(cmd.Parameters[index].ParameterName);
                    errorSQL.Append(" ");
                    errorSQL.Append(cmd.Parameters[index].SqlDbType.ToString().ToLower());

                    //Check to see if the size and precision needs to be included
                    if (cmd.Parameters[index].Size != 0)
                    {
                        if (cmd.Parameters[index].Precision != 0)
                        {
                            errorSQL.Append("(");
                            errorSQL.Append(cmd.Parameters[index].Size.ToString());
                            errorSQL.Append(",");
                            errorSQL.Append(cmd.Parameters[index].Precision.ToString());
                            errorSQL.Append(")");
                        }
                        else
                        {
                            errorSQL.Append("(");
                            errorSQL.Append(cmd.Parameters[index].Size.ToString());
                            errorSQL.Append(")");
                        }
                    }

                    //Output the direction just for kicks
                    errorSQL.Append(";  --");
                    errorSQL.Append(cmd.Parameters[index].Direction.ToString());
                    errorSQL.AppendLine();

                    //Set the Default Value for the Parameter if it's an InputOutput
                    if (cmd.Parameters[index].Direction == ParameterDirection.InputOutput)
                    {
                        errorSQL.Append("SET ");
                        errorSQL.Append(cmd.Parameters[index].ParameterName);
                        errorSQL.Append(" = ");
                        if (cmd.Parameters[index].Value == DBNull.Value
                             || cmd.Parameters[index].Value == null)
                        {
                            errorSQL.AppendLine("NULL");
                        }
                        else
                        {
                            errorSQL.Append("'");
                            errorSQL.Append(cmd.Parameters[index].Value.ToString());
                            errorSQL.Append("'");
                        }
                        errorSQL.AppendLine(";");
                    }
                }
            }

            //Create a break line before outputting the EXEC statement
            errorSQL.AppendLine();

            //Output the exec statement
            errorSQL.Append("EXEC ");

            //See if you need to capture the return value
            for (int index = 0; index < cmd.Parameters.Count; index++)
            {
                if (cmd.Parameters[index].Direction == ParameterDirection.ReturnValue)
                {
                    errorSQL.Append(cmd.Parameters[index].ParameterName);
                    errorSQL.Append(" = ");
                    break;
                }
            }

            //Output the name of the command
            errorSQL.Append(cmd.CommandText);

            //Output the parameters and their values
            for (int index = 0; index < cmd.Parameters.Count; index++)
            {
                if (cmd.Parameters[index].Direction != ParameterDirection.ReturnValue)
                {
                    //Append comma seperator (or space if it's the first item)
                    if (index == 0)
                    {
                        errorSQL.Append(" ");
                    }
                    else
                    {
                        errorSQL.Append(", ");
                        errorSQL.AppendLine();
                        errorSQL.Append("\t\t");
                    }

                    errorSQL.Append(cmd.Parameters[index].ParameterName);
                    switch (cmd.Parameters[index].Direction)
                    {
                        case ParameterDirection.Input:
                            errorSQL.Append(" = ");
                            if (cmd.Parameters[index].Value == DBNull.Value
                                 || cmd.Parameters[index].Value == null)
                            {
                                errorSQL.AppendLine("NULL");
                            }
                            else
                            {
                                errorSQL.Append("'");
                                errorSQL.Append(cmd.Parameters[index].Value.ToString());
                                errorSQL.Append("'");
                            }
                            break;

                        case ParameterDirection.InputOutput:
                        case ParameterDirection.Output:

                            errorSQL.Append(" OUTPUT");
                            break;
                    }

                }
            }

            return errorSQL.ToString();
        }

        private static string GetErrorDetails(SqlException ex)
        {
            StringBuilder sb = new StringBuilder();
            foreach (SqlError error in ex.Errors)
            {
                sb.AppendFormat("\r\n    Message = {0}", error.Message);
                sb.AppendFormat("\r\n    Procedure = {0}", error.Procedure);
                sb.AppendFormat("\r\n    LineNumber = {0}", error.LineNumber);
            }

            Transaction ambientTransaction = Transaction.Current;
            if (ambientTransaction == null)
            {
                sb.Append("\r\n    Ambient transaction is not detected.");
            }
            else
            {
                TransactionInformation transactionInformation = ambientTransaction.TransactionInformation;
                sb.AppendFormat("\r\n    Ambient transaction detected. CreationTime = {0}, DistributedIdentifier = {1}, LocalIdentifier = {2}, Status = {3}",
                    transactionInformation.CreationTime, transactionInformation.DistributedIdentifier, transactionInformation.LocalIdentifier,
                    transactionInformation.Status);
            }

            return sb.ToString();
        }
    }
}
