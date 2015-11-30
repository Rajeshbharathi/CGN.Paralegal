namespace CGN.Paralegal.ClientContracts.Analytics
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public interface IAnalyticsRestClient
    {
        /// <summary>
        /// Gets the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        AnalyticsProjectInfo GetAnalyticProject(string matterId, string dataSetId, string projectId);

        /// <summary>
        /// Gets the state of the analytic workflow.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        List<AnalyticsWorkflowState> GetAnalyticWorkflowState(long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Update the state of the analytic workflow.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="binderId">The binder identifier.</param>
        /// <param name="workflowState">Workflow State</param>
        /// <returns>
        /// Updated state
        /// </returns>
        List<AnalyticsWorkflowState> UpdateAnalyticWorkflowState(long matterId, long datasetId, long projectId,
            string binderId, List<AnalyticsWorkflowState> workflowState);

        /// <summary>
        /// Gets the state of the changed workflow.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        AnalyticsWorkflowState GetChangedWorkflowState(long matterId, long datasetId, long projectId);
        
        /// <summary>
        /// Gets the control set summary.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        ControlSetSummary GetControlSetSummary(long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Gets the training set summary.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        TrainingSetSummary GetTrainingSetSummary(long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Gets all analysis sets.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        List<AnalysisSet> GetAllAnalysisSets(long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Create the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="project">Project Info</param>
        /// <returns></returns>
        AnalyticsProjectInfo CreateAnalyticProject(string matterId, string dataSetId, AnalyticsProjectInfo project);

        /// <summary>
        /// Deletes the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        void DeleteAnalyticProject(long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Get controlset sample size
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="confidenceLevel">ConfidenceLevel</param>
        /// <param name="marginOfError">MarginOfError</param>
        /// <returns></returns>
        int GetControlsetSampleSize(string matterId, string dataSetId, string confidenceLevel, string marginOfError);

        /// <summary>
        /// Create Controlset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="project">project</param>
        /// <returns></returns>
        ControlSet CreateControlset(string matterId, string dataSetId, string projectId, ControlSet project);

        /// <summary>
        /// Creates the qc set.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="qcSet">The qc set.</param>
        /// <returns></returns>
        QcSet CreateQcSet(long matterId, long dataSetId, long projectId, QcSet qcSet);

        /// <summary>
        /// Gets the available document count.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        long GetAvailableDocumentCount(long orgId, long matterId, long datasetId, long projectId);

        /// <summary>
        /// Gets the analytic project tags.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId</param>
        /// <param name="documentSelection">documentSelection.</param>
        /// <returns></returns>
        long GetSearchCount(string orgId, string matterId, string dataSetId, string projectId,
                                            JObject documentSelection);

        /// <summary>
        /// Gets saved searches for the project.
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <returns></returns>
        List<SavedSearch> GetSavedSearches(string orgId, string matterId, string dataSetId);

        /// <summary>
        /// Gets the analytic project tags.
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <returns></returns>
        List<Tag> GetAnalyticProjectTags(string orgId, string matterId, string dataSetId);

        /// <summary>
        /// Update the control set document coding value
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="documentId">documentId.</param>
        /// <param name="codingValue">codingValue.</param>
        /// <returns></returns>
        bool UpdateProjectDocumentCodingValue(long orgId, long matterId, long dataSetId, long projectId, string documentId, string codingValue);

        /// <summary>
        /// Get document by document reference id
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="documentRefId"> document Ref Id.</param>
        /// <param name="analysisset"> analysisset</param>
        /// <returns></returns>
        AnalysisSetDocumentInfo GetDocumentByRefId(long orgId, long matterId, long dataSetId,
            long projectId, string analysisset, string documentRefId);

        /// <summary>
        /// Get next uncoded document for the analysis set
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="sequenceId"> document sequence Id.</param>
        /// <param name="searchContext"> searchContext</param>
        /// <returns></returns>
        AnalysisSetDocumentInfo GetUncodedDocument(long orgId, long matterId, long dataSetId,
            long projectId, string sequenceId, DocumentQueryContext searchContext);

        /// <summary>
        /// Gets the documents.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSet">The analysis set.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        DocumentList GetDocuments(long matterId, long dataSetId, long projectId, string analysisSet, DocumentQueryContext queryContext);

        /// <summary>
        /// Schedules the job for export documents.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        int ScheduleJobForExportDocuments(long matterId, long dataSetId, long projectId, DocumentQueryContext queryContext);


        /// <summary>
        /// Create Trainingset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <returns></returns>
        string CreateTrainingset(string matterId, string dataSetId, string projectId);

        /// <summary>
        ///  Create Job for categorize Controlset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="trainingRound">training Round</param>
        /// <returns></returns>
        int CreateJobForCategorizeControlset(string matterId, string dataSetId, string projectId, string trainingRound);

        /// <summary>
        /// Creates the manual job for categorize controlset.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        int CreateManualJobForCategorizeControlset(string matterId, string dataSetId, string projectId);

        /// <summary>
        ///  Create Job for categorize analysisset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="analysisSetType">Analysis Set Type</param>
        /// <param name="trainingRound">training Round</param>
        /// <returns></returns>
        int CreateJobForCategorizeAnalysisset(string matterId, string dataSetId, string projectId, string analysisSetType,string  binderId, string trainingRound);

        /// <summary>
        /// Get prediction scores
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>        
        /// <returns></returns>
        List<PredictionScore> GetPredictionScores(long orgId, long matterId, long dataSetId, long projectId, string setType, string setId);

        /// <summary>
        /// Get categorization discrepancies
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        List<List<int>> GetDiscrepancies(long orgId, long matterId, long dataSetId, long projectId, string setType, string setId);
        
        /// <summary>
        /// Gets qc sets detail info.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns>List of QcSet </returns>
        List<QcSet> GetQcSetsInfo(long orgId, long matterId, long datasetId, long projectId);
        

        /// <summary>
        ///  Create Job for categorize All documents
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="jobScheduleInfo">JobScheduleInfo</param>
        /// <returns></returns>
        int CreateJobForCategorizeAll(string matterId, string dataSetId, string projectId, JobScheduleInfo jobScheduleInfo);

        /// <summary>
        /// Gets Predict all summary information.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns>predict all summary information </returns>
        PredictAllSummary GetPredictAllSummary(long orgId, long matterId, long datasetId, long projectId);


        /// <summary>
        /// Gets prediction discrepancies
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set type.</param>
        /// <returns></returns>
        List<Discrepancy> GetPredictionDiscrepancies(long orgId, long matterId, long datasetId, long projectId,
            string setType);

        /// <summary>
        /// Validate create project info
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="project">Project Info</param>
        /// <returns></returns>
        AnalyticsProjectInfo ValidateCreateProjectInfo(string matterId, string dataSetId, AnalyticsProjectInfo project);

        /// <summary>
        /// Validates the predict controlset job.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        bool ValidatePredictControlsetJob(long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Get the categorize status
        /// </summary>
        /// <param name="orgId">orgId</param>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <returns></returns>
        bool ValidateQcSetCreationPrerequisite(long orgId, long matterId, long dataSetId, long projectId);

        /// <summary>
        /// Add additional documents to analysis set
        /// </summary>
        /// <param name="orgId">orgId</param>
        /// <param name="matterId">matterId</param>
        /// <param name="dataSetId">dataSetId</param>
        /// <param name="projectId">projectId</param>
        /// <param name="analysisset">analysisset</param>
        /// <returns>number of documents added</returns>
        int AddDocumentsToAnalysisSet(long orgId, long matterId, long dataSetId,
            long projectId, string analysisset);

        /// <summary>
        /// Automatics the code truth set.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSet">The analysis set.</param>
        /// <param name="documentQuery">The document query.</param>
        /// <param name="truthsetFieldName">Name of the truthset field.</param>
        /// <param name="relevantFieldValue">The relevant field value.</param>
        void AutoCodeTruthSet(string matterId, string dataSetId, string projectId,
            string analysisSet, DocumentQueryContext documentQuery, string truthsetFieldName, string relevantFieldValue);
    }
}