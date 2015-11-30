#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Suneeth senthil/Anandhi</author>
//      <description>
//         This file has Constants
//      </description>
//      <changelog>
//          <date value="08/18/2010">created</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion

#region Namespaces

#endregion

namespace LexisNexis.Evolution.BatchJobs.Email
{
    public sealed class Constants
    {
        #region private Constructor
        private Constants()
        {
        }
        #endregion

        #region Constants

        public const string Colon = ":";

        #region Event Log
        internal const string JobTypeName = "Delivery Options Email Job";
        internal const string Event_Job_Initialize_Start = "Job Initialize Start";
        internal const string Event_Job_Initialize_Success = "Job Initialize successfully";
        internal const string Event_Job_Initialize_Failed = "Job Initialize Failed";
        internal const string Event_Job_GenerateTask_Start = "Generate Task Start";
        internal const string Event_Job_GenerateTask_Failed = "Generate Task Failed";
        internal const string Event_Job_DoAtomicWork_Success = "Do Atomic Work successfully";
        internal const string Event_Job_DoAtomicWork_Failed = "Do Atomic Work Failed";
        #endregion

        internal const string EmailJob = "Email Job";
        internal const string EmailDocuments = "Email Documents";
        internal const string NotApplicable = "N/A";
        internal const string PathSeperator = "\\";
        internal const string Hyphen = " - ";
        internal const string EmailJobInitialisation = "Email JobInitialization";
        internal const string XmlNotWellFormed = "In Initialization job xml not well formed";
        internal const string ForTaskNumber = " for Task Number -  ";
        internal const string TargetDirectory = "TARGET_DIRECTORY";
        internal const string SourceDirectory = "SOURCE_DIRECTORY";
        internal const string PipeSeperator = "|";
        internal const string WaitTimeToCheckCompressedFiles = "WAIT_TIME_TO_CHECK_COMPRESSED_FILES";
        internal const string DeleteSourceFiles = "DELETE_SOURCE_FILES";
        internal const string Comma = ",";
        internal const string CleanFolderInHours = "CLEAN_FOLDER_IN_HOURS";
        internal const string ErrorInDeletingDirectory = "Error in deleting directory :";
        internal const string DueTo = " due to ";
        internal const string BaseSharedPath = "BASE_SHAREPATH";
        internal const string DeliveryOptions = "DeliveryOptions";
        internal const string SourceDirectoryPath = "SourceDirectory";
        internal const string TargetDirectoryPath = "TargetDirectory";
        internal const string ZipExtension = "zip";
        #endregion
    }
}
