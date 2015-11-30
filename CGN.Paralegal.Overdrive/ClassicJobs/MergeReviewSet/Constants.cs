#region Header
//-----------------------------------------------------------------------------------------
// <copyright file=""Constants.cs"" company=""Lexis Nexis"">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Ram Sundar</author>
//      <description>
//          This file contains the Constants class for Merging the reviewset
//      </description>
//      <changelog>
//          <date value="02/05/2011">New Constants Added</date>
//          <date value="31/05/2011">Added new Constants</date>
//          <date value="03/20/2012">Bug Fix #98018</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces



#endregion

namespace LexisNexis.Evolution.BatchJobs.MergeReviewSet
{
    class Constants
    {
        private Constants()
        {
        }

        #region General constants

        public const string JobLogName = "Merge ReviewSet Log";
        public const string InitMessage = "Merge Reviewset Job Initialization";
        public const string DoAtomicWorkMethod = "In Do Atomic Work Method";
        public const string JobStartMessage = "In Initialaization method - Start ";
        public const string JobEndMessage = "In Initialaization method - End ";
        public const string JobName = "Merge ReviewSet";
        public const string JobError = "Merge Reviewset error";
        public const string GenerateTasks = "GenerateTasks";
        public const string JobParamND = "Job Parameters Is Not Defined";
        internal const string Relevance = "Relevance";
        internal const string DocumentsChunkSize = "DocumentsChunkSize";
           

        #region Services Names
        internal const string Create = "Create";
        internal const string Merge = "Merge";
        internal const string Single = "Single";
        internal const string AuditEventReviewSetName = "ReviewSet Name";
        #endregion
        #endregion
    }
}
