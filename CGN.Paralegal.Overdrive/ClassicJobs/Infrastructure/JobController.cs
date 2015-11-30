//---------------------------------------------------------------------------------------------------
// <copyright file="JobController.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the JobController class to control a specified job.
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Infrastructure.Jobs
{
    #region Namespaces


    #endregion

    /// <summary>
    /// This class represents the Job Controller API.
    /// </summary>
    public static class JobController
    {
        #region Enumerations
        /// <summary>
        /// Enumeration for the job commands.
        /// </summary>
        public enum JobCommand
        {
            /// <summary>
            /// Use this to start a job.
            /// </summary>
            Start = 2,

            /// <summary>
            /// Use this to stop a job.
            /// </summary>
            Stop = 3,

            /// <summary>
            /// Use this to pause a job.
            /// </summary>
            Pause = 4,

            /// <summary>
            /// Use this to Resume a job.
            /// </summary>
            Resume = 5,
        } // End JobCommand

        /// <summary>
        /// Enumeration for the job statues.
        /// </summary>
        public enum JobStatus
        {
            /// <summary>
            /// Indicates a job is loaded.
            /// </summary>
            Loaded = 1,

            /// <summary>
            /// Indicates a job is running.
            /// </summary>
            Running = 2,

            /// <summary>
            /// Indicates a job is stopped.
            /// </summary>
            Stopped = 3,

            /// <summary>
            /// Indicates a job is paused.
            /// </summary>
            Paused = 4,


        } // End JobStatus
        #endregion
    } // End JobController
} // End Namespace
