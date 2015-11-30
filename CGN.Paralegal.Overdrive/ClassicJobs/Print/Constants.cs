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
//          <date value="04/19/2012">Bug Fix 98566</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion

#region Namespaces

#endregion

namespace LexisNexis.Evolution.BatchJobs.Print
{
    public sealed class Constants
    {
        private Constants()
        {
        }
        #region Constants

        #region Event Log
        internal const string JobTypeName = "Delivery Options Print Job";
        internal const string EventJobInitializeStart = "Job Initialize Start";
        internal const string EventJobInitializeSuccess = "Job Initialize successfully";
        internal const string EventJobInitializeFailed = "Job Initialize Failed";
        internal const string EventJobGenerateTaskStart = "Generate Task Start";
        internal const string EventJobGenerateTaskFailed = "Generate Task Failed";
        internal const string EventJobDoAtomicWorkSuccess = "Do Atomic Work successfully";
        internal const string EventJobDoAtomicWorkFailed = "Do Atomic Work Failed";
        #endregion

        /// <summary>
        /// Represents true
        /// </summary>
        internal const string PrintJobName = "Print Job";
        internal const string PrintJobTypeName = "Print Documents";
        internal const string NA = "N/A";
        internal const string PathSeperator = "\\";
        internal const string Hypen = " - ";
        internal const string Colon = ":";
        internal const string JobXmlNotWellFormed = "Job xml not well formed";
        internal const string PrintJobInitialisation = "Print Job Initialization";
        internal const string TargetDirectory = "TARGET_DIRECTORY";
        internal const string SourceDirectory = "SOURCE_DIRECTORY";
        internal const string RedactitUri = "REDACTIT_URI";
        internal const string QueueServerUrl = "QueueServerUrl";
        internal const string CallBackUri = "CALLBACK_URI";
        internal const string PrintCallBackUri = "PrintToFileCallBackURI";        
        internal const string WaitAttemptsCount = "WAIT_ATTEMPTS_COUNT";
        internal const string DeleteSourceFiles = "DELETE_SOURCE_FILES";
        internal const string RedactItPostSupported = "REDACTIT_POST_SUPPORTED";
        internal const string PipeSeperator = "|";
        internal const string Comma = ",";
        internal const string GetPrintDocumentConfigurations = "GetPrintDocumentConfigurations";
        internal const string ForTaskNumber = "for task number - ";
        internal const string CleanFolderInHours = "CLEAN_FOLDER_IN_HOURS";
        internal const string ErrorInDeletingDirectory = "Error in deleting directory :";
        internal const string DueTo = " due to ";
        internal const string BaseSharedPath = "BASE_SHAREPATH";
        internal const string DeliveryOptions = "DeliveryOptions";
        internal const string SourceDirectoryPath = "SourceDirectory";
        internal const string TargetDirectoryPath = "TargetDirectory";
        internal const string ErrorUnsupportedFormat = " the native file format for the document: {0} is not supported by conversion server.";
        internal const string ErrorInPublishType = "unable to find publish type for '";
        internal const string ErrorInExtension = "', extension not mapped to a queue";
        internal const string NoConversion = "There are no converted documents found for the selected document set.";
        internal const string NoDocument = "No converted document found";
        internal const string ErrorIPCPort = "failed to connect to an ipc port:";
        internal const string ConversionServerDown = " the inaccessibility of conversion server";

        #endregion

    }
}
