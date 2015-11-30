//---------------------------------------------------------------------------------------------------
// <copyright file="JobSchedule.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the JobSchedule class.
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Overdrive.ScheduleMonitor
{
    #region Namespaces
    using System;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// This class represents the Schedule information.
    /// </summary>
    internal class JobSchedule
    {
        #region Private Fields
        /// <summary>
        /// Job Identifier.
        /// </summary>
        private int _jobId;

        /// <summary>
        /// Name of the job.
        /// </summary>
        private string _jobName;

        /// <summary>
        /// Boot Parameters
        /// </summary>
        private string _bootParameters;

        /// <summary>
        /// Job Server Id.
        /// </summary>
        private Guid _jobServerId;

        /// <summary>
        /// Job Type Id.
        /// </summary>
        private int _jobTypeId;

        /// <summary>
        /// Job Type Name.
        /// </summary>
        private string _jobTypeName;

        /// <summary>
        /// Job Start Date.
        /// </summary>
        private DateTime _jobStartDate;

        /// <summary>
        /// Frequency denoting hourly repetition.
        /// </summary>
        private int _hourly;

        /// <summary>
        /// Frequency denoting daily repetition.
        /// </summary>
        private int _daily;

        /// <summary>
        /// Requested Recurrence Count.
        /// </summary>
        private int _requestedRecurrenceCount;

        /// <summary>
        /// Actual Occurrence Count.
        /// </summary>
        private int _actualOccurenceCount;

        /// <summary>
        /// Next Run Date.
        /// </summary>
        private DateTime _nextRunDate;

        /// <summary>
        /// Job Run Duration.
        /// </summary>
        private int _jobRunDuration;


        /// <summary>
        /// Job Boot parameters.
        /// </summary>
        //private string _jobParameters;

        /// <summary>
        /// Schedule Details.
        /// </summary>
        private List<JobScheduleDetails> _scheduleDetails;
        #endregion
        #region Public Properties
        /// <summary>
        /// Gets or sets the job identifier.
        /// </summary>
        public int JobId
        {
            get { return this._jobId; }
            set { this._jobId = value; }
        }

        /// <summary>
        /// Gets or sets the job name.
        /// </summary>
        public string JobName
        {
            get { return this._jobName; }
            set { this._jobName = value; }
        }

        /// <summary>
        /// Gets or sets the boot parameters.
        /// </summary>
        public string BootParameters
        {
            get { return _bootParameters; }
            set { _bootParameters = value; }
        }

        /// <summary>
        /// Gets or sets the job server id.
        /// </summary>
        public Guid JobServerId
        {
            get { return this._jobServerId; }
            set { this._jobServerId = value; }
        }

        /// <summary>
        /// Gets or sets the job type id.
        /// </summary>
        public int JobTypeId
        {
            get { return this._jobTypeId; }
            set { this._jobTypeId = value; }
        }

        /// <summary>
        /// Gets or sets the job type name.
        /// </summary>
        public string JobTypeName
        {
            get { return this._jobTypeName; }
            set { this._jobTypeName = value; }
        }

        /// <summary>
        /// Gets or sets the date on / after which the job should start occurring.
        /// </summary>
        public DateTime JobStartDate
        {
            get { return this._jobStartDate; }
            set { this._jobStartDate = value; }
        }

        /// <summary>
        /// Gets or sets the hourly repeat interval. For a job that should run on a hourly basis, this value represents the hourly interval.
        /// </summary>
        public int Hourly
        {
            get { return this._hourly; }
            set { this._hourly = value; }
        }

        /// <summary>
        /// Gets or sets the daily repeat interval. For a job that should run on a hourly basis, this value represents the daily interval.
        /// </summary>
        public int Daily
        {
            get { return this._daily; }
            set { this._daily = value; }
        }

        /// <summary>
        /// Gets or sets the requested recurrence count. This value represents how many times the job should recur. 
        /// It can also be set to 0 if the job is set to recur hourly / daily / weekly / monthly.
        /// </summary>
        public int RequestedRecurrenceCount
        {
            get { return this._requestedRecurrenceCount; }
            set { this._requestedRecurrenceCount = value; }
        }

        /// <summary>
        /// Gets or sets the actual occurrence count. This value represent the actual number of times a job has run.
        /// </summary>
        public int ActualOccurenceCount
        {
            get { return this._actualOccurenceCount; }
            set { this._actualOccurenceCount = value; }
        }

        /// <summary>
        /// Gets or sets the next date and time when the job should recur.
        /// </summary>
        public DateTime NextRunDate
        {
            get { return this._nextRunDate; }
            set { this._nextRunDate = value; }
        }

        /// <summary>
        /// Gets or sets the job's run duration.
        /// </summary>
        public int JobRunDuration
        {
            get { return this._jobRunDuration; }
            set { this._jobRunDuration = value; }
        }

        /// <summary>
        /// Gets or sets the schedule details for more complex recurring scenarios.
        /// Supports following scenarios:
        /// 1. Weekly on a specific day with skip interval for number of day of week to skip.
        /// 2. Monthly on a specific day of month with skip interval for number of months to skip.
        /// 3. Monthly on a specific day of week with skip interval for number of day of week to skip.
        /// A lot more complex scenarios can be supported if required.
        /// </summary>
        public List<JobScheduleDetails> ScheduleDetails
        {
            get { return this._scheduleDetails; }
            set { this._scheduleDetails = value; }
        }


        #endregion
    }
}
