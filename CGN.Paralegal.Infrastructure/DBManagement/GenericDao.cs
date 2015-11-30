//---------------------------------------------------------------------------------------------------
// <copyright file="GenericDao.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ningjun Wang</author>
//      <description>
//          This file contains the GenericDao class.
//      </description>
//      <changelog>
//          <date value="04/24/2013">Creation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlClient;



namespace CGN.Paralegal.Infrastructure.DBManagement
{
    public static class GenericDao
    {
        /// <summary>
        /// This class represent the return value of query execution.
        /// </summary>

        private class ExecCommandReturn
        {
            public DataTable DataTable;
            public int RowAffected;

        }

        /// <summary>
        /// Create a SqlParameter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SqlParameter CreateSqlParameter(string name, byte[] value)
        {
            SqlParameter p = new SqlParameter(name, SqlDbType.VarBinary);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = value;
            }
            return p;
        }

        /// <summary>
        ///  Create a SqlParameter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SqlParameter CreateSqlParameter(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new SqlParameter(name, DBNull.Value);
            }
            return new SqlParameter(name, value);
        }

        /// <summary>
        ///  Create a SqlParameter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SqlParameter CreateSqlParameter(string name, int? value)
        {
            if (value == null)
            {
                return new SqlParameter(name, DBNull.Value);
            }
            return new SqlParameter(name, value);
        }

        /// <summary>
        ///  Create a SqlParameter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SqlParameter CreateSqlParameter(string name, bool value)
        {
            return new SqlParameter(name, value);
        }

        /// <summary>
        ///  Create a SqlParameter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SqlParameter CreateSqlParameter(string name, DateTime? value)
        {
            SqlParameter p = new SqlParameter(name, SqlDbType.DateTime);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = value;
            }
            return p;
        }

        /// <summary>
        ///  Create a SqlParameter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static SqlParameter CreateSqlOutputParameter(string name, SqlDbType dbType)
        {
            SqlParameter p = new SqlParameter(name, dbType);
            p.Direction = ParameterDirection.Output;
            return p;
        }

        /// <summary>
        /// Execute a stored procedure or Sql commadn that return query result.
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="commandText"></param>
        /// <param name="isStoredProcedure"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        public static DataTable ExecQuery(string connStr, string commandText, bool isStoredProcedure, params SqlParameter[] sqlParams)
        {
            ExecCommandReturn ret = ExecCommand0(connStr, commandText, isStoredProcedure, true, sqlParams);
            return ret.DataTable;
        }

        /// <summary>
        /// Execute a stored procedure or a Sql command that does not return query result.
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="commandText"></param>
        /// <param name="isStoredProcedure"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        public static int ExecNonQuery(string connStr, string commandText, bool isStoredProcedure, params SqlParameter[] sqlParams)
        {
            ExecCommandReturn ret = ExecCommand0(connStr, commandText, isStoredProcedure, false, sqlParams);
            return ret.RowAffected;
        }

        /// <summary>
        /// Execute a stored procedure or Sql command.
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="commandText"></param>
        /// <param name="isStoredProcedure"></param>
        /// <param name="isQuery"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static ExecCommandReturn ExecCommand0(string connStr, string commandText, bool isStoredProcedure, bool isQuery, params SqlParameter[] sqlParams)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    if (isStoredProcedure)
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                    }

                    if (sqlParams != null && sqlParams.Length > 0)
                    {
                        cmd.Parameters.AddRange(sqlParams);
                    }

                    if (isQuery)
                    { // query command, return a DataTable
                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = cmd;
                            using (DataSet ds = new DataSet())
                            {
                                da.Fill(ds);
                                return new ExecCommandReturn { DataTable = ds.Tables[0], RowAffected = 0 };
                            }
                        }
                    }
                    // non query command
                    int rowAffected = cmd.ExecuteNonQuery();
                    return new ExecCommandReturn { DataTable = null, RowAffected = rowAffected };
                }
            }
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colName"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static string GetString(DataRow row, string colName, string nullValue)
        {
            if (row.IsNull(colName))
            {
                return nullValue;
            }
            return (string)row[colName];
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static string GetString(DataRow row, int col, string nullValue)
        {
            if (row.IsNull(col))
            {
                return nullValue;
            }
            return (string)row[col];
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colName"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static Guid GetGuid(DataRow row, string colName, Guid nullValue)
        {
            if (row.IsNull(colName))
            {
                return nullValue;
            }
            return (Guid)row[colName];
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static Guid GetGuid(DataRow row, int col, Guid nullValue)
        {
            if (row.IsNull(col))
            {
                return nullValue;
            }
            return (Guid)row[col];
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colName"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static int GetInt(DataRow row, string colName, int nullValue)
        {
            if (row.IsNull(colName))
            {
                return nullValue;
            }
            return (int)row[colName];
        }

        public static long GetLong(DataRow row, string colName, long nullValue)
        {
            if (row.IsNull(colName))
            {
                return nullValue;
            }
            return (long)row[colName];
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colName"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static bool GetBool(DataRow row, string colName, bool nullValue)
        {
            if (row.IsNull(colName))
            {
                return nullValue;
            }
            return (bool)row[colName];
        }

        /// <summary>
        /// Get column value from a datarow.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colName"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static DateTime GetDateTime(DataRow row, string colName, DateTime nullValue)
        {
            if (row.IsNull(colName))
            {
                return nullValue;
            }
            return (DateTime)row[colName];
        }
    }
}
