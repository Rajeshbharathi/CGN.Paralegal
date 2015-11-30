//---------------------------------------------------------------------------------------------------
// <copyright file="NotifiedDataCache.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ningjun</author>
//      <description>
//          It cache the result of SQL.
//      </description>
//      <changelog>
//          <date value="6/18/2013"></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading;

namespace CGN.Paralegal.Infrastructure.Caching
{
    /// <summary>
    /// Manage cached data from SQL query.
    /// </summary>
    public sealed class NotifiedDataCache : IDisposable
    {
        private static NamedTracer _TRACER = new NamedTracer("Infrastructure.Caching");

        private const string EVENT_SOURCE = "ConcordanceEV";
        private static NotifiedDataCache _instance = new NotifiedDataCache();

        public static NotifiedDataCache Instance
        {
            get { return _instance; }
        }
        private ConcurrentDictionary<string, string> _AllConnectionStrs = new ConcurrentDictionary<string, string>(); // store all connection strings, map connectionStr.ToUpperCase() => connectionStr
        private object _StartSqlDependencyLock = new object(); // Lock to prevent running SqlDependency.Start() from multiple thread
        private object _StopAllSqlDependencyLock = new object(); // Lock to prevent running SqlDependency.Stop() from multiple thread

        private bool _disposed = false;


        private NotifiedDataCache()
        {
            //if (!EventLog.SourceExists(EVENT_SOURCE))
            //{
            //    EventLog.CreateEventSource(EVENT_SOURCE, "Application");
            //}
            _TRACER.Info("Entering constructor of NotifiedDataCache");

            Utils.RegisterProcessExitHandlerAndMakeItFirstInLineToExecute(
                (sender, e) =>
                    {
                        _TRACER.Info("Entering AppDomain.CurrentDomain.ProcessExit");
                        StopAllSqlDependency();
                    }
                );



        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopAllSqlDependency();
                _disposed = true;
            }

        }

        /// <summary>
        /// Run SqlDependency.Start() once and only once for a give connection string. 
        /// </summary>
        /// <param name="connStr">Connection string </param>
        private void StartSqlDependency(string connStr)
        {
            if (_AllConnectionStrs.ContainsKey(connStr.ToUpper()))
            {
                // SqlDependency.Start() has already been called for this connection string
                return;
            }
            // SqlDependency.Start() has not been called for this connection string yet, call it now
            lock (_StartSqlDependencyLock) // Prevent running this from multi-thread 
            {
                // doublbe check because other thread may already invoke SqlDependency.Start() for this connection string
                if (_AllConnectionStrs.ContainsKey(connStr.ToUpper()))
                {
                    // SqlDependency.Start() has already been called for this connection string
                    return;
                }
                // SqlDependency.Start() has not been called for this connection string yet, call it now
                _TRACER.Info("Run SqlDependency.Start()");
                SqlDependency.Start(connStr);
                _AllConnectionStrs[connStr.ToUpper()] = connStr;
            }
        }

        /// <summary>
        /// Run SqlDependency.Stop(connStr) once and only once for each connection string
        /// </summary>
        private void StopAllSqlDependency()
        {
            _TRACER.Info("Entering NotifiedDataCache.StopAllSqlDependency()");
            lock (_StopAllSqlDependencyLock)
            {
                if (!_AllConnectionStrs.IsEmpty)
                {
                    foreach (string key in _AllConnectionStrs.Keys)
                    {
                        string connStr = _AllConnectionStrs[key];
                        string msg = "Run SqlDependency.Stop()";
                        _TRACER.Info(msg);
                        SqlDependency.Stop(connStr);
                    }
                    var dict = new ConcurrentDictionary<string, string>();
                    Interlocked.Exchange(ref _AllConnectionStrs, dict); // Change _AllConnectionStrsto point to the empty dictionary in atomic operation
                }
            }
        }

        
        /// <summary>
        /// Get DataTable from memory cache. If it is not in the cache, retrieve it from database and save to cache. The cached data will be automatically removed if the underlying data in the database change.
        /// </summary>
        /// <param name="cacheKey">key to the memory cache for retrieving data</param>
        /// <param name="connStr">Database connection string</param>
        /// <param name="sql">SQL statement</param>
        /// <returns></returns>
        public DataTable GetCachedData(string cacheKey, string connStr, string sql)
        {
            DataTable dt = MemoryCache.Default.Get(cacheKey) as DataTable;
            if (dt == null)
            {
                // Data is not in the memory cach, retrieve it from database and save to the cache
                dt = ReloadCachedData(cacheKey, connStr, sql);
            }
            Debug.Assert(dt != null, "Cached data cannot be null.");
            return dt;
        }

        /// <summary>
        /// Get the data from database and save it in the memory cache.
        /// </summary>
        /// <param name="cacheKey">key to the memory cache for storing data</param>
        /// <param name="connStr">Database connection string</param>
        /// <param name="sql">SQL statement</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private DataTable ReloadCachedData(string cacheKey, string connStr, string sql)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connStr), "Connection string is mandatory");
            _TRACER.Info("Reload cache data from database, cacheKey = {0}", cacheKey);
            Stopwatch stopWatch = Stopwatch.StartNew();

            // Can be called many times without issue
            StartSqlDependency(connStr);

            using (var conn = new SqlConnection(connStr))
            using (var command = new SqlCommand(sql, conn))
            {
                command.Notification = null;
                var dep = new SqlDependency(command);

                var dt = new DataTable();
                conn.Open();
                dt.Load(command.ExecuteReader(CommandBehavior.CloseConnection));

                // Create a monitor for changes in Sql Server
                var monitor = new SqlChangeMonitor(dep);

                // Create a monitor cache policy (more advanced than sliding expire)
                var policy = new CacheItemPolicy();
                policy.ChangeMonitors.Add(monitor);
               
                 policy.RemovedCallback = 
                    (o) =>
                        {
                            String strLog = String.Format("CacheItem removed, Reason: {0}, CacheItemKey: {1}, CacheItemValue: {2}",
                                                          o.RemovedReason.ToString(), o.CacheItem.Key,
                                                          o.CacheItem.Value.ToString());
                            _TRACER.Info(strLog);
                        };

                // Add config data to in-memory cache which will reload if data changes in SQL
                MemoryCache.Default.Set(cacheKey, dt, policy);
                stopWatch.Stop();
                _TRACER.Info("Time for ReloadCachedData(): {0} milliseconds", stopWatch.ElapsedMilliseconds);
                return dt;
            }
        }
    }
}
