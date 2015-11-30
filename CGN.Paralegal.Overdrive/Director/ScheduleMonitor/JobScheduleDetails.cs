//---------------------------------------------------------------------------------------------------
// <copyright file="JobScheduleDetails.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Arun Srinivasan</author>
//      <description>
//          This file contains the JobScheduleDetails class.
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
    #endregion

    /// <summary>
    /// This class represents the Schedule Details.
    /// </summary>
    /// <remarks>
    /// ================================================================================
    /// How to use Schedule Details for different Recurrence Scenarios.
    /// ================================================================================
    /// SCENARIO - 1: Repeat every "n" weeks on a specific day of week 
    /// (where n is between 1 and 52)
    /// Example: Repeat Sunday of every week.
    /// WeekMonthIndicator = W (Indicates detail is for a weekly recurrence schedule.)
    /// DayDateIndicator = DY (Indicates that the DateValue represent day of week.)
    /// DateValue = 3/21/2010 9:00PM (3/21/2010 is a Sunday.)
    /// RepeatEvery = 1 (Since recurrence is every week.)
    /// ================================================================================
    /// SCENARIO - 2: Repeat every "n" months on a specific day of month 
    /// (where n is between 1 and 52)
    /// Example: Repeat on the 21st of every month.
    /// WeekMonthIndicator = M (Indicates detail is for a monthly recurrence schedule.)
    /// DayDateIndicator = DT (Indicates that the DateValue represent day of month.)
    /// DateValue = 3/21/2010 9:00PM (Represents 21 of the month.)
    /// RepeatEvery = 1 (Since recurrence is every month.)
    /// ================================================================================
    /// SCENARIO - 3: Repeat every month on a specific day of week skipping "n"
    /// occurrences of the day of week. (where n is between 1 and 4)
    /// Example: Repeat on the 3rd Sunday of every month.
    /// WeekMonthIndicator = M (Indicates detail is for a monthly recurrence schedule.)
    /// DayDateIndicator = DY (Indicates that the DateValue represent day of week.)
    /// DateValue = 3/21/2010 9:00PM (Represents Sunday.)
    /// RepeatEvery = 3 (Since recurrence is every Sunday of every month.)
    /// ================================================================================
    /// </remarks>
    internal class JobScheduleDetails
    {
        #region Private Fields
        /// <summary>
        /// Week Month Indicator.
        /// </summary>
        private string _weekMonthIndicator;

        /// <summary>
        /// Day Date Indicator.
        /// </summary>
        private string _dayDateIndicator;

        /// <summary>
        /// Date Value.
        /// </summary>
        private DateTime _dateValue;

        /// <summary>
        /// Frequency of repetition.
        /// </summary>
        private int _repeatEvery;
        #endregion
        #region Public Properties
        /// <summary>
        /// Gets or sets the Week / Month indicator.
        /// </summary>
        public string WeekMonthIndicator
        {
            get { return this._weekMonthIndicator; }
            set { this._weekMonthIndicator = value; }
        }

        /// <summary>
        /// Gets or sets if the date for schedule details points to the date value or day of week value.
        /// </summary>
        public string DayDateIndicator
        {
            get { return this._dayDateIndicator; }
            set { this._dayDateIndicator = value; }
        }

        /// <summary>
        /// Gets or sets a datetime value for the schedule details.
        /// </summary>
        public DateTime DateValue
        {
            get { return this._dateValue; }
            set { this._dateValue = value; }
        }

        /// <summary>
        /// Gets or sets the skip interval for weekly / monthly scenarios.
        /// </summary>
        public int RepeatEvery
        {
            get { return this._repeatEvery; }
            set { this._repeatEvery = value; }
        }
        #endregion
    }
}
