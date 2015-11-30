#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          Constants for CompareSavedSearchResultsJob
//      </description>
//      <changelog>
//          <date value="10/05/2011"></date>
//          <date value="02/28/2012">Fix for Bug# 95162</date>
//          <date value="03/22/2012">Fix for Bug# 90386</date>
//          <date value="11\29\2012">Fix for bug 112025</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespace

#endregion

namespace LexisNexis.Evolution.BatchJobs.CompareSavedSearchResultsJob
{
    /// <summary>
    /// Constant file for compare save search result job
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Represents Next Line Character.
        /// </summary>
        internal const string NextLineCharacter = "\n";

        /// <summary>
        /// Represents configured value for folder path that has resource file
        /// </summary>
        internal const string ResourceFileLocation = "ResourceFileLocation";

        /// <summary>
        /// Represents configured value for Resource file base (no .resources extension or culture information)
        /// </summary>
        internal const string ResourceFileBaseName = "ResourceFileBaseName";

        /// <summary>
        /// Represents configured value for Resource file base (no .resources extension or culture information)
        /// </summary>
        internal const string ReportHandlerUrl = "ReportHandlerUrl";

        /// <summary>
        /// Represents Job Type for logging and additional purposes
        /// </summary>
        internal const string JobTypeName = "Compare Save Search Results Job";

        /// <summary>
        /// Represents type of file to be generated
        /// </summary>
        internal const string RequiredFileType = "RequiredFileType";

        /// represents the type of encoding used to write report file        
        internal const string RequiredEncoding = "EncodingType";

        /// <summary>
        /// represents the UniCode encoding used to write report file
        /// </summary>
        internal const string UniCode = "UniCode";

        /// <summary>
        /// represents column separator used to write report file
        /// </summary>
        internal const string FileTypeCsv = "csv";
        /// <summary>
        /// represents key entry in config file for XSL file path for report
        /// </summary>
        internal const string XslFilePathForComparisonReport = "XslFilePathForComparisonReport";

        /// Search Result service       
        internal const string SearchResultService = "SearchResultService";

        /// <summary>
        /// URI template for search results document
        /// </summary>
        internal const string SearchResultsServiceTemplateForDocument = "repository/search-results/documents/";

        /// <summary>
        /// Search result UUID pattern
        /// </summary>
        internal const string SearchResultUUIDString = "dataset-{0}-user-{1}-searchresultid-{2}";

        internal const string BackSlash = "\\";

        #region Message Constants
        internal const string CompareReport = "compareReport";
        internal const string CompareReportMessage = "Your task for exporting  the comparison report for {0}(Saved Search Result) is now ready and available <a href={1}?requestType={2}&searchResultId={3}&fileType={4}&reportId={5}&datasetId={6} target='_blank'> here</a>";
        internal const string InitializationStartMessage = "Initialization Start...";
        internal const string InitializationDoneForConfigurableItemMessage = "Configured item initialization done";
        internal const string InitializationDoneForConfigurableItemErrorMessage = "Error while initializing configured item - Default value will be used";
        internal const string TaskGenerationCompletedMessage = "Tasks Generation completed : No. of task generated : {0}";
        internal const string TaskGenerationStartedMessage = "Tasks generation started";
        internal const string DoAtomicWorkStartMessage = "DoAtomic tasks started for task # : {0}";
        internal const string FailureNotificationMessage = "Your task for exporting  the comparison report for {0}(Saved Search Result) could not be executed properly ";
        internal const string TaskKeyStringFormat = "Task Type : {0}";
        internal const string InitializeMethodFullName = "LexisNexis.Evolution.BatchJobs.CompareSavedSearchResultsJob.Initialize";
        internal const string GenerateTaskMethodFullName = "LexisNexis.Evolution.BatchJobs.CompareSavedSearchResultsJob.GenerateTasks";
        internal const string DoAtomicMethodFullName = "LexisNexis.Evolution.BatchJobs.CompareSavedSearchResultsJob.DoAtomicWork";
        #endregion
        #region Audit Log
        internal const string JobName = "Job Name";
        internal const string SearchResultName = "Search Result Name";
        internal const string CompareSearchResultString = "Create Comparison Export";
        #endregion
    }
}
