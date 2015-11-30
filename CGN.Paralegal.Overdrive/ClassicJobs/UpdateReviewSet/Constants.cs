#region Header
//-----------------------------------------------------------------------------------------
// <copyright file=""Constants.cs"" company=""Lexis Nexis"">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Ram Sundar</author>
//      <description>
//          This file contains the Constants class for Updating the reviewset
//      </description>
//      <changelog>
//          <date value="09/03/2011"></date>
//          <date value="31/05/2011">Added new Constants</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces

#endregion

namespace LexisNexis.Evolution.BatchJobs.UpdateReviewSet
{
    class Constants
    {
        private Constants()
        {
        }

        #region General constants

        public const string JobLogName = "Update ReviewSet Log";
        public const string InitMessage = "Update Reviewset Job Initialization";
        public const string DoAtomicWorkMethod = "In Do Atomic Work Method";
        public const string JobStartMessage = "In Initialization method - Start ";
        public const string JobEndMessage = "In Initialization method - End ";
        public const string JobName = "Update ReviewSet";
        public const string JobError = "Update Reviewset error";
        public const string GenerateTasks = "GenerateTasks";
        public const string JobParamND = "Job Parameters Is Not Defined";
        internal const string Relevance = "Relevance";
        internal const string DocumentsChunkSize = "DocumentsChunkSize";

        #region Services Names
        internal const string Add = "Add";
        internal const string Remove = "Remove";
        internal const string Archive = "Archive";
        #endregion
        #endregion
    }
}
