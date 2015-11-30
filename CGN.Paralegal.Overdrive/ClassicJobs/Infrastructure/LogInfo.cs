//---------------------------------------------------------------------------------------------------
// <copyright file="LogInfo.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Rama Krishna</author>
//      <description>
//          This file contains the LogInfo class for Jobs infrastructure projects.
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#region NameSpaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;


#endregion

namespace LexisNexis.Evolution.Infrastructure.Jobs
{

    /// <summary>
    /// Class represents the parameters
    /// </summary>
    [SerializableAttribute]
    public class Parameter
    {
        /// <summary>
        /// Property to set Parameter Name
        /// </summary>
        [XmlAttribute("name")]
        public string ParameterName { get; set; }
        /// <summary>
        /// Property to set Parameter Value
        /// </summary>
        [XmlAttribute("value")]
        public string ParameterValue { get; set; }
    }

    /// <summary>
    /// Enum to Log
    /// </summary>
    public enum LogCategory { Job = 1, Task = 2 }

    /// <summary>
    /// Used to capture the Log Info 
    /// </summary>
    [SerializableAttribute]
    public class LogInfo
    {
        #region Properties

        /// <summary>
        /// To Get/Set the Custom message of the exception
        /// </summary>
        public string CustomMessage { get; set; }

        /// <summary>
        /// Get/Set the ErrorCode
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Get/Set the TaskNumber
        /// </summary>
        public string TaskKey { get; set; }

        /// <summary>
        /// Property generates the error message to save it in database.
        /// </summary>
        /// <returns></returns>
        public string GetParameters
        {
            get
            {
                string errorMessage = parameters.Aggregate(string.Empty, (current, msg) => current + string.Format("\t{0} : {1}; \n", msg.ParameterName, msg.ParameterValue));
                errorMessage = (!string.IsNullOrEmpty(errorMessage) ? " <B>Parameters... :</B> " : string.Empty) + errorMessage;
                return errorMessage;
            }
        }
        /// <summary>
        /// Get/Set Stack trace value
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Get/Set IsError
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Get Parameter List
        /// </summary>
        public List<Parameter> Parameters
        {
            get
            {
                return parameters;
            }
        }

        #endregion

        /// <summary>
        /// store the parameters
        /// </summary>
        List<Parameter> parameters;

        /// <summary>
        /// Constructor
        /// </summary>
        public LogInfo()
        {
            parameters = new List<Parameter>();
        }

        /// <summary>
        /// This method helps to add Key Value pair of task attributes to Errorlog Info. 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public void AddParameters(string keyName, string keyValue)
        {
            parameters.Add(new Parameter() { ParameterName = keyName, ParameterValue = keyValue });
        }

        /// <summary>
        /// This method helps to add Key Value pair of task attributes to Errorlog Info. 
        /// </summary>
        /// <param name="propertyValue"></param>
        public void AddParameters(string keyValue)
        {
            parameters.Add(new Parameter() { ParameterName = string.Empty, ParameterValue = keyValue });
        }

        /// <summary>
        /// This method will be used to create a LogInfo object to capture the log information, which need to be saved in JobLog
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <returns>LogInfo</returns>
        public static LogInfo CreateJobLogInfo(int jobId, int jobRunId)
        {
            return (Helper.GetLogDetails(jobId, jobRunId, 0));
        }
        /// <summary>
        /// This property will be used to create a LogInfo object to capture the log information, which need to be saved in TaskDetails
        /// </summary>
        /// <param name="jobId">Job Identifier</param>
        /// <param name="jobRunId">Job Run Identifier</param>
        /// <param name="taskId">Task Identifier</param>
        /// <returns>LogInfo</returns>
        public static LogInfo CreateTaskLogInfo(int jobId, int jobRunId, int taskId)
        {
            return (Helper.GetLogDetails(jobId, jobRunId, taskId));
        }

    }
}
