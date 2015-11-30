#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Thangakumar</author>
//      <Reviewer>RajkumarPandurangan</Reviewer>
//      <description>
//          Job to delete dataset.
//      </description>
//      <changelog>
//          <date value="01/18/2011">Created</date>
//          <date value="06/15/2011">Unwanted constants are removed</date>
//          <date value="03/26/2012">Dataset delete job issue fixed</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion


namespace LexisNexis.Evolution.BatchJobs.DataSetDelete
{
    class Constants
    {
        private Constants()
        {
        }

        internal const string JobInnerException1 = " Inner Exception (level {0}): ";
        internal const string JobInnerException2 = " Inner Exception Stack Trace: ";
        internal const string JobInnerException3 = ">>> END INNER EXCEPTION STACK TRACE <<< ";
        internal const string JobInnerException4 = " >>> END INNER EXCEPTION level {0}";
        /// <summary>
        /// Represents Next Line Character.
        /// </summary>
        internal const string NextLineCharacter = "\n";

        /// <summary>
        /// Represents key used by Audit Log Info object for timestamp
        /// </summary>
        internal const string MatterService = "MatterService";
        internal const string DatasetService = "DatasetService";
        internal const string JobRunID = "Job Run ID: ";
        internal const string JobID = "Job ID: ";
        internal const string JobLocation = "Location: ";
        internal const string JobErrorMessage = " Error Message: ";
        internal const string JobStackTrace = " Stack Trace: ";
        internal const string JobName = "DataSet Delete";
        internal const string JobInitialized = "Delete Dataset Job - Job ID:{0} - Initialization starts...";
        internal const string JobBootParameterParsed = "Delete Dataset Job - Job ID:{0}- Boot parameter parsed successfully.";
        internal const string JobXMLNotWellFramed = "Delete Dataset Job - Job ID:{0}- Job Initialization: Xml string is not well formed.";
        internal const string JobInitializeException = "Delete Dataset Job - Job ID:{0}- Exception in Initialize Method";
        internal const string JobDoAtomicWorkInitialized = "Delete Dataset Job - Job ID:{0} - DoAtomicWork starts...";
        internal const string JobDoAtomicWorksException = "Delete Dataset Job - Job ID:{0}- Exception in DoAtomicWork Method";
        internal const string JobGenerateTasksInitialized = "Delete Dataset Job - Job ID:{0} - GenerateTasks starts...";
        internal const string JobGenerateTasksException = "Delete Dataset Job - Job ID:{0}- Exception in GenerateTasks Method";
        internal const string JobDoAtomicWorkDeleteDocument = "Delete Dataset Job-Job ID:{0}-Deletes Document.Doc Id:{1}";
        internal const string JobDoAtomicWorkDeleteNativeset = "Delete Dataset Job-Job ID:{0}-Deletes Nativeset.DocSet Id:{1}";
        internal const string JobDoAtomicWorkDeleteProductionset = "Delete Dataset Job-Job ID:{0}-Deletes Productionset.DocSet Id:{1}";
        internal const string JobDoAtomicWorkDeleteImageset = "Delete Dataset Job-Job ID:{0}-Deletes Imageset.DocSet Id:{1}";
        internal const string JobDoAtomicWorkDeleteDataset = "Delete Dataset Job-Job ID:{0}-Deletes dataset from EVMaster";
        internal const string Dataset = "Dataset";
        internal const string Matter = "Matter";
        internal const string ExternalizationConfiguration = "Externalization";
        internal const string Collection = "Collection";
        internal const string Document = "Document";
        internal const string Hyphen = "-";
        internal const string Relevance = "Relevance";

        internal const string ProductionSet = "1";
        internal const string NativeSet = "2";
        internal const string ImageSet = "3";

        /// <summary>
        /// Represents key used by Audit Log Info object for User Name
        /// </summary>

        internal const string JobLogDeleteDocumentField = "Audit Logging Delete Document Field job -";

        /// <summary>
        /// Constant for : "matter/{matterId}"
        /// </summary>
        internal const string GetMatterDetailForMatterId = "matter/{0}";
        /// <summary>
        /// Constant for : dataset/{documentuuid}/{matterdbname}/{deletedby}/document
        /// </summary>
        internal const string DeleteDocument = "dataset/{0}/{1}/{2}/document";
        /// <summary>
        /// Constant for :dataset/deletedataset/{datasetuuid}/{deletedby}
        /// </summary>
        internal const string DeleteDatasetFromSearchAndVault = "dataset/deletedataset/{0}/{1}";

        /// <summary>
        /// Constant for :dataset/documentset/{datasetuuid}/{deletedby}
        /// </summary>
        internal const string DeleteDocumentSetFromVault = "dataset/documentset/{0}/{1}";

        /// <summary>
        /// Constant for :  "dataset/{datasetuuid}/EVMaster"
        /// </summary>
        internal const string DeleteDataSetFromEVMaster = "dataset/{0}/EVMaster";

        /// <summary>
        /// Constant for : "documentset/{datasetid}"
        /// </summary>
        internal const string GetAllDocumentSet = "documentset/{0}";

        /// <summary>
        /// Represents true
        /// </summary>
        internal const bool SUCCESS = true;

        #region RVWReviewerSearchService

        internal const int MaxDocChunkSize = 500;
        internal const int MaxBinStatesSize = 100;
        internal const string SearchMaxChunkSize = "SEARCH_MAX_CHUNKSIZE";
        internal const string SearchMaxBinStateSize = "SEARCH_MAX_BINSTATESIZE";

        #endregion
    }
}
