#region File Header
		//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish Kumar</author>
//      <description>
//          Constants for SaveSearchResultsJob
//      </description>
//      <changelog>
//          <date value="2/2/2012">Fix for bug 95162</date>
//          <date value="03/22/2012">Fix for Bug# 90386</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespace

#endregion

namespace LexisNexis.Evolution.BatchJobs.SaveSearchResults
{
    /// <summary>
    /// Constant file for save search result job
    /// </summary>
    public class Constants
    {        
        /// <summary>
        /// Represents true
        /// </summary>
        internal const bool Success = true;
        /// <summary>
        /// Represents Next Line Character.
        /// </summary>
        internal const string NextLineCharacter = "\n";

        /// <summary>
        /// Represents the configuration item - page size for the use of no. of document being processed as single task.
        /// Number of documents to be extracted from search results at a point in time.
        /// </summary>
        internal const string ResultsPageSize = "ResultsPageSize";

        /// <summary>
        /// Represents configured value for folder path that has resource file
        /// </summary>
        internal const string ResourceFileLocation = "ResourceFileLocation";

        /// <summary>
        /// Represents configured value for Resource file base (no .resources extension or culture information)
        /// </summary>
        internal const string ResourceFileBaseName = "ResourceFileBaseName";

        /// <summary>
        /// Represents Job Type for logging and additional purposes
        /// </summary>
        internal const string JobTypeName = "Save Search Results Job";

        /// <summary>
        /// Represents Job Type for logging and additional purposes
        /// </summary>
        internal const int JobStatusPaused = 4;

        #region Audit log releted string constant
        internal const string AuditFor = "Name of the Saved Search Result";
        internal const string AuditJobName = "AuditJobName";
        internal const string SaveSearchResultString = "Saved Search Result";
       
        /// <summary>
        /// No. of document element for audit log
        /// </summary>
        internal const string NoOfDocuments = "Number of documents";
        /// <summary>
        /// DCNlist element for auditlog
        /// </summary>
        internal const string DocumentControlNumberList = "DCNs";

        /// <summary>
        /// DCN element for auditlog
        /// </summary>
        internal const string DocumentControlNumber= "DocumentControlNumber";
        /// <summary>
        /// DocumentGUID element for auditlog
        /// </summary>
        internal const string DocumentGuid = "DocumentGUID";
        /// <summary>
        /// Query description
        /// </summary>        
        internal const string SearchDescription = "Query Description";

        /// <summary>
        /// Query Term
        /// </summary>
        internal const string SearchQuery = "Search Query";
        #endregion
        /// <summary>
        /// Search Result service
        /// </summary>
        internal const string SearchResultService = "SearchResultService";

        internal const string TaskKeyStringFormat="Search Query => {0} ,DocumentRange=>{1}-{2}";
        /// <summary>
        /// URI template for search results
        /// </summary>
        internal const string SearchResultsServiceTemplate = "repository/search-results/";

        /// <summary>
        /// URI template for search results document
        /// </summary>
        internal const string SearchResultsServiceTemplateForDocument = "repository/search-results/documents";

        /// <summary>
        /// Slahh string constants
        /// </summary>
        internal const string Slash = "/";

        /// <summary>
        /// Url for saved search result
        /// </summary>
        internal const string SaveSearchResultUrl = "SaveSearchResultUrl";

        internal const string Relevance = "Relevance";

        #region Message Constants
        internal const string InitializationStartMessage = "Initialization start...";
        internal const string InitializationDoneForConfigurableItemMessage = "Configured item initialization done";
        internal const string InitializationDoneForConfigurableItemErrorMessage = "Error while initializing configured item - Default value will be used";
        internal const string InitializationFailMessage = "Job initialization fails";
        internal const string TaskGenerationCompletedMessage = "Tasks generation completed : No. of Task Generated : {0}";
        internal const string TaskGenerationStartedMessage = "Tasks generation started";
        internal const string TaskGenerationFails = "Task Generation Fails";
        internal const string NoTaskToExecuteError = "Tasks generation Error : No Task to execute ";
        internal const string DoAtomicWorkStartMessage = "Doatomic tasks started for task # : {0}";
        internal const string DoAtomicWorkCompletedMessage = "Doatomic tasks completed for task # : {0}";
        internal const string SearchDoneForTask = " Search done... for Task # : {0}";
        internal const string InsertDocumentDetailMessage="Inserting document details";
        internal const string DoAtomicWorkFailMessage = "Doatomic task fail";
        internal const string ShutdownLogMessage="Shutdown method : Writing audit log";
        internal const string ShutdownErrorMessage = "Shutdown fails";
        internal const string FailureNotificationMessage="Your task for saving   {0} (Saved Search Result) could not be executed properly";
        internal const string SuccessNotificationMessage = "Your task for saving   {0} (Saved Search Result) is now ready and available <a href='{1}'> here</a> ";
        internal const string InitializeMethodFullName="LexisNexis.Evolution.BatchJobs.SaveSearchResultsJob.Initialize";
        internal const string GenerateTaskMethodFullName = "LexisNexis.Evolution.BatchJobs.SaveSearchResultsJob.GenerateTasks";
        internal const string DoAtomicMethodFullName="LexisNexis.Evolution.BatchJobs.SaveSearchResultsJob.DoAtomicWork";
        internal const string JobDateSeparator = " - Instance ";
        #endregion


    }
}
