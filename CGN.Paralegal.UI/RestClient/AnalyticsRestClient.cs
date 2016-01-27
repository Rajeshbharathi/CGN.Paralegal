# region File Header

//-----------------------------------------------------------------------------------------
// <header>
//      <description>
//          This is a file that contains AnalyticsRestClient to communicate with services or other web apis
//      </description>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using CGN.Paralegal.UI.Log.Instrumentation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CGN.Paralegal.UI.RestClient
{
    using ClientContracts.Analytics;
    using ClientContracts.Search;

    public class AnalyticsRestClient : IAnalyticsRestClient
    {
        const string AnalyticsService = "AnalyticsService";
        const string EvWebApi = "EvWebApi";


        public List<ParaLegalProfile> GetSearchList(string keyWord)
        {
            List<ParaLegalProfile> paralegal = null;
            return paralegal;

        }
        public ParaLegalProfile GetParalegalDetails(int paralegalid)
        {
            ParaLegalProfile paralegal = null;
            return paralegal;
        }

        public List<string> GetReviewList(int paralegalid)
        {
            List<string> review = null;
            return review;
        }

        public List<AreaOfPractise> GetTopTenAOP()
        {
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}search/gettop10aop", serviceUri);

            var response = HttpClientHelper.Execute(uri, Method.Get);
            var json = response.Content.ReadAsStringAsync().Result;
            var jObj = JArray.Parse(json);
            return jObj.ToObject<List<AreaOfPractise>>();            
        }

        public List<Location> GetTopTenCity()
        {
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}search/gettop10cities", serviceUri);

            var response = HttpClientHelper.Execute(uri, Method.Get);
            var json = response.Content.ReadAsStringAsync().Result;
            var jObj = JArray.Parse(json);
            return jObj.ToObject<List<Location>>();    
        }

        public List<PLDetail> GetTopTenParaLegal()
        {
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}search/gettop10pls", serviceUri);

            var response = HttpClientHelper.Execute(uri, Method.Get);
            var json = response.Content.ReadAsStringAsync().Result;
            var jObj = JArray.Parse(json);

            
            return jObj.ToObject<List<PLDetail>>();    

        }
        /// <summary>
        /// Gets the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public AnalyticsProjectInfo GetAnalyticProject(string matterId, string dataSetId, string projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalyticProject);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var projectInfo = JObject.Parse(json);
            return projectInfo.ToObject<AnalyticsProjectInfo>();
        }

        /// <summary>
        /// Gets the documents.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSet">The analysis set.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        public DocumentList GetDocuments(long matterId, long dataSetId, long projectId, string analysisSet, DocumentQueryContext queryContext)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetDocuments);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/analysissets/{4}/documents",
                serviceUri, matterId, dataSetId, projectId, analysisSet);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(queryContext));
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var documents = JObject.Parse(json);
            return documents.ToObject<DocumentList>();
        }

        /// <summary>
        /// Gets all analysis sets.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public List<AnalysisSet> GetAllAnalysisSets(long matterId, long dataSetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAllAnalysisSets);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/analysissets",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var documents = JArray.Parse(json);
            return documents.ToObject<List<AnalysisSet>>();
        }

        /// <summary>
        /// Schedules the job for export documents.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        public int ScheduleJobForExportDocuments(long matterId, long dataSetId, long projectId, DocumentQueryContext queryContext)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetDocuments);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/analysisset/export",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(queryContext));
            inst.End(uri);
            var result = response.Content.ReadAsStringAsync().Result;
            int jobId;
            int.TryParse(result, out jobId);
            return jobId;
        }

        /// <summary>
        /// Gets the state of the analytic workflow.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public List<AnalyticsWorkflowState> GetAnalyticWorkflowState(long matterId, long dataSetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalyticWorkflowState);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/workflow",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var workflowState = JArray.Parse(json);
            return workflowState.ToObject<List<AnalyticsWorkflowState>>();
        }

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
        public List<AnalyticsWorkflowState> UpdateAnalyticWorkflowState(long matterId, long datasetId, long projectId, string binderId,
            List<AnalyticsWorkflowState> workflowState)
        {
            var inst = new Instrumentation(InstrumentationOperations.UpdateAnalyticWorkflowState);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/analysiset/{4}/workflow",
                serviceUri, matterId, datasetId, projectId, binderId);
            var post = JArray.FromObject(workflowState);
            var response = HttpClientHelper.Execute(uri, Method.Put, post);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var state = JArray.Parse(json);
            return state.ToObject<List<AnalyticsWorkflowState>>();
        }

        /// <summary>
        /// Gets the state of the changed workflow.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public AnalyticsWorkflowState GetChangedWorkflowState(long matterId, long datasetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetChangedWorkflowState);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/changed-workflow-state",
                serviceUri, matterId, datasetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var state = JObject.Parse(json);
            return state.ToObject<AnalyticsWorkflowState>();
        }

        /// <summary>
        /// Gets the control set summary.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public ControlSetSummary GetControlSetSummary(long matterId, long dataSetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetControlSetSummary);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/controlset/summary",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var controlsetSummary = JObject.Parse(json);
            return controlsetSummary.ToObject<ControlSetSummary>();
        }

        /// <summary>
        /// Gets the training set summary.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public TrainingSetSummary GetTrainingSetSummary(long matterId, long dataSetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetTrainingSetSummary);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/training/summary",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var controlsetSummary = JObject.Parse(json);
            return controlsetSummary.ToObject<TrainingSetSummary>();
        }

        /// <summary>
        /// Create the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="project">Project Info</param>
        /// <returns></returns>
        public AnalyticsProjectInfo CreateAnalyticProject(string matterId, string dataSetId, AnalyticsProjectInfo project)
        {
            var inst = new Instrumentation(InstrumentationOperations.CreateAnalyticProject);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project",
                serviceUri, matterId, dataSetId);
            var response = HttpClientHelper.Execute(uri, Method.Post,JObject.FromObject(project));               
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}", matterId, dataSetId));
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JObject.Parse(json);
            return result.ToObject<AnalyticsProjectInfo>();
        }

        /// <summary>
        /// Deletes the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public void DeleteAnalyticProject(long matterId, long dataSetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.DeleteAnalyticProject);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}",
                serviceUri, matterId, dataSetId, projectId);
            HttpClientHelper.Execute(uri, Method.Delete);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", 
                matterId, dataSetId, projectId));
        }


        /// <summary>
        /// Get controlset sample size
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="confidenceLevel">ConfidenceLevel</param>
        /// <param name="marginOfError">MarginOfError</param>
        /// <returns></returns>
        public int GetControlsetSampleSize(string matterId, string dataSetId, string confidenceLevel, string marginOfError)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetSamplesizeControlSet);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/samplesize?confidenceLevel={3}&marginOfError={4}",
                serviceUri, matterId, dataSetId, confidenceLevel, marginOfError);
            var response = HttpClientHelper.Execute(uri, Method.Put);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}", matterId, dataSetId));
            var size = response.Content.ReadAsStringAsync().Result;
            return Convert.ToInt32(size, CultureInfo.InvariantCulture);
        }


       /// <summary>
        /// Create Controlset
       /// </summary>
        /// <param name="matterId">Matter Id</param>
       /// <param name="dataSetId">Dataset Id</param>
       /// <param name="projectId">Project Id</param>
        /// <param name="controlSet">controlSet</param>
       /// <returns></returns>
        public ControlSet CreateControlset(string matterId, string dataSetId, string projectId, ControlSet controlSet)
        {
            var inst = new Instrumentation(InstrumentationOperations.CreateAnalyticProject);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/controlset",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(controlSet));
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JObject.Parse(json);
            return result.ToObject<ControlSet>();
        }

        /// <summary>
        /// Creates the qc set.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="qcSet">The qc set.</param>
        /// <returns></returns>
        public QcSet CreateQcSet(long matterId, long dataSetId, long projectId, QcSet qcSet)
        {
            var inst = new Instrumentation(InstrumentationOperations.CreateQcSet);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/qcset",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(qcSet, new JsonSerializer
            {
                DateParseHandling = DateParseHandling.DateTime
            }));
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JObject.Parse(json);
            return result.ToObject<QcSet>();
        }

        /// <summary>
        /// Gets the available document count.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public long GetAvailableDocumentCount(long orgId, long matterId, long datasetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAvailableDocumentCount);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/availabledoccount",
                serviceUri, matterId, datasetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", matterId, datasetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            return long.Parse(json, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the analytic project tags.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId</param>
        /// <param name="documentSelection">documentSelection.</param>
        /// <returns></returns>
        public long GetSearchCount(string orgId, string matterId, string dataSetId, string projectId,
            JObject documentSelection)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalyticProjectSearchCount);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/search/count", 
                serviceUri, orgId, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post, documentSelection);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}", matterId, dataSetId));
            var count = response.Content.ReadAsStringAsync().Result;
            var documentCount = Convert.ToInt64(count, CultureInfo.InvariantCulture);
            return documentCount;
        }

        /// <summary>
        /// Gets saved searches for the project.
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <returns></returns>
        public List<SavedSearch> GetSavedSearches(string orgId, string matterId, string dataSetId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalyticProjectSavedsearches);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}orgs/{1}/matters/{2}/datasets/{3}/search/savedsearches", serviceUri, orgId, matterId, dataSetId);

            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}", matterId, dataSetId));
            var json = response.Content.ReadAsStringAsync().Result;
            var jObj = JArray.Parse(json);
            return jObj.ToObject<List<SavedSearch>>();
        }

        /// <summary>
        /// Gets the analytic project tags.
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <returns></returns>
        public List<Tag> GetAnalyticProjectTags(string orgId, string matterId, string dataSetId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalyticProjectTags);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}orgs/{1}/matters/{2}/datasets/{3}/tags?scope=Document&fillBehaviors=false", serviceUri, orgId, matterId, dataSetId);

            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}", matterId, dataSetId));
            var json = response.Content.ReadAsStringAsync().Result;
            var jObj = JArray.Parse(json);
            return jObj.ToObject<List<Tag>>();
        }


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
        public bool UpdateProjectDocumentCodingValue(long orgId, long matterId, long dataSetId, long projectId, string documentId, string codingValue)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalyticProjectTags);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);

            var uri = string.Format(CultureInfo.InvariantCulture, "{0}orgs/{1}/matters/{2}/dataSets/{3}/projects/{4}/documents/{5}/coding",
            serviceUri, orgId, matterId, dataSetId, projectId, documentId);
            var codingObj = new JObject { { "codingValue", codingValue } };
            HttpClientHelper.Execute(uri, Method.Post, codingObj);

            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}, Document:{3}", matterId, dataSetId, projectId, documentId));
            return true;
        }

        /// <summary>
        /// Get analysis set document by doc ref id
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="documentRefId">documentRefId.</param>
        /// <param name="analysisset">analysisset.</param>
        /// <returns>AnalysisSetDocumentInfo</returns>
        public AnalysisSetDocumentInfo GetDocumentByRefId(long orgId, long matterId, long dataSetId,
            long projectId, string analysisset, string documentRefId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetDocumentByReferenceId);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/analysissets/{5}/documents/{6}",
                serviceUri, orgId, matterId, dataSetId, projectId, analysisset, documentRefId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var analysisSetDocumentInfo = JObject.Parse(json);
            return analysisSetDocumentInfo.ToObject<AnalysisSetDocumentInfo>();
        }


        /// <summary>
        /// Get analysis set document by doc ref id
        /// </summary>
        /// <param name="orgId">orgId.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">projectId.</param>
        /// <param name="sequenceId">document Sequence Id.</param>
        /// <param name="searchContext">searchContext.</param>
        /// <returns>AnalysisSetDocumentInfo</returns>
        public AnalysisSetDocumentInfo GetUncodedDocument(long orgId, long matterId, long dataSetId,
            long projectId, string sequenceId, DocumentQueryContext searchContext)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetUncodedDocument);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/uncodedDocument/{5}",
                serviceUri, orgId, matterId, dataSetId, projectId, sequenceId);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(searchContext));
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var analysisSetDocumentInfo = JObject.Parse(json);
            return analysisSetDocumentInfo.ToObject<AnalysisSetDocumentInfo>();
        }


        /// <summary>
        /// Create Trainingset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <returns></returns>
        public string CreateTrainingset(string matterId, string dataSetId, string projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.CreateTrainingset);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/trainingset",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", matterId, dataSetId, projectId));
            var result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

        /// <summary>
        /// Create Job for categorize Controlset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="trainingRound">Training Round</param>
        /// <returns></returns>
        public int CreateJobForCategorizeControlset(string matterId, string dataSetId, string projectId, string trainingRound)
        {
            var inst = new Instrumentation(InstrumentationOperations.CategorizeControlset);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/controlset/categorize/{4}",
                serviceUri, matterId, dataSetId, projectId, trainingRound);
            var response = HttpClientHelper.Execute(uri, Method.Post);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}, TrainingRound:{3} ", matterId, dataSetId, projectId, trainingRound));
            var result = response.Content.ReadAsStringAsync().Result;
            int jobId;
            int.TryParse(result, out jobId);
            return jobId;
        }

        /// <summary>
        /// Creates the manual job for categorize controlset.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public int CreateManualJobForCategorizeControlset(string matterId, string dataSetId, string projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.CategorizeControlsetManually);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/controlset/categorize/manual",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}", matterId, dataSetId, projectId));
            var result = response.Content.ReadAsStringAsync().Result;
            int jobId;
            int.TryParse(result, out jobId);
            return jobId;
        }

        /// <summary>
        /// Create Job for categorize Trainingset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="analysisSetType">Analysis Set Type</param>
        /// <param name="trainingRound">Training Round</param>
        /// <returns></returns>
        public int CreateJobForCategorizeAnalysisset(string matterId, string dataSetId, string projectId, string analysisSetType, string binderId, string trainingRound)
        {
            var inst = new Instrumentation(InstrumentationOperations.CategorizeAnalysisset);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/analysissettype/{4}/binderid/{5}/categorize/{6}",
                serviceUri, matterId, dataSetId, projectId, analysisSetType,binderId, trainingRound);
            var response = HttpClientHelper.Execute(uri, Method.Post);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2}, Type:{3}, BinderId:{4}, TrainingRound:{5} ", matterId, dataSetId, projectId, analysisSetType, binderId, trainingRound));
            var result = response.Content.ReadAsStringAsync().Result;
            int jobId;
            int.TryParse(result, out jobId);
            return jobId;
        }

        /// <summary>
        /// Return list of all prediction scores
        /// </summary>
        /// <param name="orgId">org Id</param>
        /// <param name="matterId">matter Id</param>
        /// <param name="dataSetId">dataSet Id</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        public List<PredictionScore> GetPredictionScores(long orgId, long matterId, long dataSetId, long projectId, string setType, string setId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetPredictionScores);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/analysisSetTypes/{5}/analysisSets/{6}/categorization/scores",
                serviceUri, orgId, matterId, dataSetId, projectId, setType, setId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var scores = JsonConvert.DeserializeObject<List<PredictionScore>>(json);
            return scores;
        }

        /// <summary>
        /// Return categorization discrepancies
        /// </summary>
        /// <param name="orgId">org Id</param>
        /// <param name="matterId">matter Id</param>
        /// <param name="dataSetId">dataSet Id</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        public List<List<int>> GetDiscrepancies(long orgId, long matterId, long dataSetId, long projectId, string setType, string setId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetPredictionDiscrepancies);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/analysisSetTypes/{5}/analysisSets/{6}/categorization/discrepancies",
                serviceUri, orgId, matterId, dataSetId, projectId, setType, setId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, dataSetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var discrepancies = JsonConvert.DeserializeObject<List<List<int>>>(json);
            return discrepancies;
        }


        /// <summary>
        /// Create Job for categorize Controlset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="jobScheduleInfo">Job Schedule Info</param>
        /// <returns></returns>
        public int CreateJobForCategorizeAll(string matterId, string dataSetId, string projectId, JobScheduleInfo jobScheduleInfo)
        {
            var inst = new Instrumentation(InstrumentationOperations.CategorizeAll);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/categorize",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(jobScheduleInfo));
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, ProjectId:{2} ", matterId, dataSetId, projectId));
            var result = response.Content.ReadAsStringAsync().Result;
            int jobId;
            int.TryParse(result, out jobId);
            return jobId;
        }

        /// <summary>
        /// Gets qc sets detail info.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public List<QcSet> GetQcSetsInfo(long orgId, long matterId, long datasetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetQcSetsInfo);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/qcSets",
                serviceUri, orgId, matterId, datasetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, datasetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var discrepancies = JsonConvert.DeserializeObject<List<QcSet>>(json);
            return discrepancies;
        }

        /// <summary>
        /// Gets predict all summary information
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public PredictAllSummary GetPredictAllSummary(long orgId, long matterId, long datasetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetPredictAllSummary);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/predictAllSummary",
                serviceUri, orgId, matterId, datasetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, datasetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var summary = JsonConvert.DeserializeObject<PredictAllSummary>(json);
            return summary;
        }


        /// <summary>
        /// Gets prediction discrepancies
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set type.</param>
        /// <returns></returns>
        public List<Discrepancy> GetPredictionDiscrepancies(long orgId, long matterId, long datasetId, long projectId, string setType)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAnalysisSetPredictionDiscrepancies);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/discrepancies/{5}",
                serviceUri, orgId, matterId, datasetId, projectId, setType);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, datasetId, projectId));
            var json = response.Content.ReadAsStringAsync().Result;
            var discrepancyList = JsonConvert.DeserializeObject<List<Discrepancy>>(json);
            return discrepancyList;
        }


        /// <summary>
        /// Validate create project info
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="project">Project Info</param>
        /// <returns></returns>
        public AnalyticsProjectInfo ValidateCreateProjectInfo(string matterId, string dataSetId, AnalyticsProjectInfo project)
        {
            var inst = new Instrumentation(InstrumentationOperations.ValidateCreateProjectInfo);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/validate",
                serviceUri, matterId, dataSetId);
            var response = HttpClientHelper.Execute(uri, Method.Post, JObject.FromObject(project));
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}", matterId, dataSetId));
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JObject.Parse(json);
            return result.ToObject<AnalyticsProjectInfo>();
        }

        /// <summary>
        /// Validates the predict controlset job.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public bool ValidatePredictControlsetJob(long matterId, long dataSetId, long projectId)
        {
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/controlset/categorize/validate",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            var json = response.Content.ReadAsStringAsync().Result;
            return Convert.ToBoolean(json, CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Get the categorize status
        /// </summary>
        /// <param name="orgId">orgId</param>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <returns></returns>
        public bool ValidateQcSetCreationPrerequisite(long orgId, long matterId, long dataSetId, long projectId)
        {
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/qcsets/validate",
                serviceUri, orgId, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            var json = response.Content.ReadAsStringAsync().Result;
            return Convert.ToBoolean(json, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Add additional documents to analysis set
        /// </summary>
        /// <param name="orgId">orgId</param>
        /// <param name="matterId">matterId</param>
        /// <param name="dataSetId">dataSetId</param>
        /// <param name="projectId">projectId</param>
        /// <param name="analysisset">analysisset</param>
        /// <returns>number of documents added</returns>
        public int AddDocumentsToAnalysisSet(long orgId, long matterId, long dataSetId,
            long projectId, string analysisset)
        {
            var inst = new Instrumentation(InstrumentationOperations.AddDocumentsToAnalysisSet);
            var serviceUri = ConfigurationManager.AppSettings.Get(EvWebApi);
            var uri = string.Format(CultureInfo.InvariantCulture,
                "{0}orgs/{1}/matters/{2}/datasets/{3}/projects/{4}/analysissets/{5}/addNewdocuments",
                serviceUri, orgId, matterId, dataSetId, projectId, analysisset);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri, string.Format(CultureInfo.InvariantCulture, "MatterId:{0}, DatasetId:{1}, Project:{2}", matterId, dataSetId, projectId));
            var count = response.Content.ReadAsStringAsync().Result;
            return Convert.ToInt32(count, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the autherized user groups.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        public static List<string> GetAutherizedUserGroups(long matterId, long dataSetId, long projectId)
        {
            var inst = new Instrumentation(InstrumentationOperations.GetAutherizedUserGroups);
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/usergroups",
                serviceUri, matterId, dataSetId, projectId);
            var response = HttpClientHelper.Execute(uri, Method.Get);
            inst.End(uri);
            var json = response.Content.ReadAsStringAsync().Result;
            var documents = JArray.Parse(json);
            return documents.ToObject<List<string>>();
        }

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
        public void AutoCodeTruthSet(string matterId, string dataSetId, string projectId,
            string analysisSet, DocumentQueryContext documentQuery, string truthsetFieldName, string relevantFieldValue)
        {
            var serviceUri = ConfigurationManager.AppSettings.Get(AnalyticsService);
            var uri = string.Format(CultureInfo.InvariantCulture, "{0}matter/{1}/dataset/{2}/analytic-project/{3}/analysissets/{4}/autocode/{5}/{6}",
                serviceUri, matterId, dataSetId, projectId, analysisSet, truthsetFieldName, relevantFieldValue);
            HttpClientHelper.Execute(uri, Method.Put, JObject.FromObject(documentQuery));
        }
    }
}