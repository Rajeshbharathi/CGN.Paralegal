# region File Header

//-----------------------------------------------------------------------------------------
// <header>
//      <description>
//          This is a file that contains AnalysisSetController to communicate with services or other web apis
//      </description>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using CGN.Paralegal.UI.Utils;
using Newtonsoft.Json.Linq;
using System.Data;

namespace CGN.Paralegal.UI
{
    using ClientContracts.Analytics;

    public class AnalysisSetController : BaseApiController
    {
        private const string ChunkSizeConfig = "DocumentChunkSize";
        private const string SessionCurrentDocumentContent = "CurrentDocumentContent";
        private const string CodingValueNotCoded = "Not_Coded";
        private const string ExportFileName = "Documents.csv";

        /// <summary>
        /// Get Review Documents
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">projectId</param>
        /// <param name="documentRefId">documentRefId</param>
        /// <param name="analysisset">analysisset</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets/{analysisset}/documents/{documentRefId}")]
        public AnalysisSetDocumentInfo GetDocumentByRefId(long orgId, long matterId,
            long datasetId, long projectId, string analysisset, string documentRefId)
        {
            var client = GetAnalyticsRestClient();
            var document = client.GetDocumentByRefId(orgId, matterId, datasetId,
                projectId, analysisset, documentRefId);

            ProcessDocument(document);
            return document;
        }

        /// <summary>
        /// Get Review Documents
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">projectId</param>
        /// <param name="sequenceId">sequenceId</param>
        /// <param name="searchContext">searchContext</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/uncodedDocument/{sequenceId}")]
        [HttpPost]
        public AnalysisSetDocumentInfo GetUncodedDocument(long orgId, long matterId,
            long datasetId, long projectId, string sequenceId, DocumentQueryContext searchContext)
        {
            var client = GetAnalyticsRestClient();

            var document = client.GetUncodedDocument(orgId, matterId, datasetId,
                projectId, sequenceId, searchContext);

            ProcessDocument(document);
            return document;
        }

        /// <summary>
        /// Gets the review document page.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/matters/datasets/projects/analysissets/{analysisset}/document/{pageIndex}")]
        public DocumentPageContent GetReviewDocumentPage(int pageIndex)
        {
            return GetDocumentPage(pageIndex);
        }

        /// <summary>
        /// Save Coding Value
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">projectId</param>
        /// <param name="documentId">documentId</param>
        /// <param name="codingValue">codingValue</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/documents/{documentId}")]
        [HttpPut]
        public bool PutDocumentCodingValue(long orgId, long matterId, long datasetId, long projectId, 
            string documentId, JObject codingValue)
        {
            var client = GetAnalyticsRestClient();
            var coding = codingValue["CodingValue"].ToString();
            return client.UpdateProjectDocumentCodingValue(orgId, matterId, datasetId, 
                projectId, documentId, coding);
        }


        /// <summary>
        /// Gets the controlset review summary.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/controlset/summary")]
        public ControlSetSummary GetControlSetReviewSummary(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetControlSetSummary(matterId, datasetId, projectId);
        }

        /// <summary>
        /// Gets the training set review summary.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/training/summary")]
        public TrainingSetSummary GetTrainingSetReviewSummary(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetTrainingSetSummary(matterId, datasetId, projectId);
        }

        /// <summary>
        /// Gets the documents.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSet">The analysis set.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), 
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets/{analysisset}/documents")]
        [HttpPost]
        public DocumentList GetDocuments(long orgId, long matterId, long datasetId, long projectId, 
            string analysisSet, DocumentQueryContext queryContext)
        {
            var client = GetAnalyticsRestClient();
            var documentList = client.GetDocuments(matterId, datasetId, projectId, analysisSet, queryContext);
            var rowNumber = 1 + ((queryContext.PageIndex - 1) * queryContext.PageSize);
            foreach (var document in documentList.Documents)
            {
                document.Id = rowNumber;
                rowNumber++;
            }
            return documentList;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets")]
        public List<AnalysisSet> GetAllAnalysisSets(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            return client.GetAllAnalysisSets(matterId, datasetId, projectId);
        }

        /// <summary>
        /// Exports the documents.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSet">The analysis set.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets/{analysisset}/documents/export")]
        [HttpPost]
        public HttpResponseMessage ExportDocuments(long orgId, long matterId, long datasetId, long projectId,
            string analysisSet, DocumentQueryContext queryContext)
        {
            var documentList = GetDocuments(orgId, matterId, datasetId, projectId, analysisSet, queryContext);
            var table = ConvertDocListToTable(documentList.Documents, queryContext);
            var csvContent = ExportHelper.WriteToCsv(table, true);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(csvContent, Encoding.UTF8);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = ExportFileName };
            return response;
        }

        /// <summary>
        /// Converts the document list to table.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <returns></returns>
        private static DataTable ConvertDocListToTable(IReadOnlyList<Document> documents, DocumentQueryContext queryContext)
        {
            var table = new DataTable {Locale = CultureInfo.InvariantCulture};
            if (!documents.Any()) return table;
            var fisrtDocument = documents[0];
            if (!fisrtDocument.Fields.Any()) return table;
            var rowFlag = false;
            if (queryContext.ExportFilters.Count > 0)
            {
                foreach (var dc in queryContext.ExportFilters)
                {
                    table.Columns.Add(new DataColumn(dc));
                    if (dc.Equals("Row"))
                    {
                        rowFlag = true;
                    }
                }
                foreach (var document in documents)
                {
                    var dr = table.NewRow();
                    if (rowFlag)
                    {
                        dr["Row"] = document.Id;
                    }
                    foreach (DataColumn column in table.Columns)
                    {
                        if (!column.ColumnName.Equals("Row"))
                        {
                            var field = document.Fields.Find(f => f.DisplayName.Equals(column.ColumnName));
                            dr[column.ColumnName] = field != null ? field.Value : string.Empty;
                        }
                    }
                    table.Rows.Add(dr);
                }
            }
            return table;
        }

        /// <summary>
        /// Schedules the job for export documents.
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="analysisSet">The analysis set.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets/{analysisset}/documents/export/job")]
        [HttpPost]
        public int ScheduleJobForExportDocuments(long orgId, long matterId, long datasetId, long projectId,
            string analysisSet, DocumentQueryContext queryContext)
        {
            var client = GetAnalyticsRestClient();
            return client.ScheduleJobForExportDocuments(matterId, datasetId, projectId, queryContext);
        }

        /// <summary>
        /// Get Predict All Summary Information
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
         Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/predictAllSummary")]
        public PredictAllSummary GetPredictAllSummary(long orgId, long matterId, long datasetId, long projectId)
        {
            var client = GetAnalyticsRestClient();
            var summary = client.GetPredictAllSummary(orgId, matterId, datasetId, projectId);

            return summary;
        }

        /// <summary>
        /// Get All Training Sets Discrepancies
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The analysis set type.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
         Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/discrepancies/{setType}")]
        public List<Discrepancy> GetDiscrepancies(long orgId, long matterId, long datasetId, long projectId, string setType)
        {
            var client = GetAnalyticsRestClient();
            var discrepancies = client.GetPredictionDiscrepancies(orgId, matterId, datasetId, projectId, setType);
            return discrepancies.OrderBy(d=>d.SetName).ToList();
        }

        /// <summary>
        /// Add additional documents to analysisset
        /// </summary>
        /// <param name="orgId">orgId</param>
        /// <param name="matterId">matterId</param>
        /// <param name="datasetId">datasetId</param>
        /// <param name="projectId">projectId</param>
        /// <param name="analysisset">analysisset</param>
        /// <returns>number of documents added</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets/{analysisset}/documents/add")]
        [HttpPut]
        public int AddDocumentsToAnalysisSet(long orgId, long matterId,
            long datasetId, long projectId, string analysisset)
        {
            var client = GetAnalyticsRestClient();
            var count = client.AddDocumentsToAnalysisSet(orgId, matterId, datasetId,
                projectId, analysisset);

            return count;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"),
        Route("api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/analysissets/{analysisset}/autocode/{truthsetFieldName}/{relevantFieldValue}")]
        [HttpPut]
        public void AutoCodeTruthSet(string matterId, string dataSetId, string projectId,
            string analysisSet, DocumentQueryContext queryContext, string truthsetFieldName, string relevantFieldValue)
        {
            var client = GetAnalyticsRestClient();
            client.AutoCodeTruthSet(matterId, dataSetId, projectId, analysisSet, queryContext, truthsetFieldName, relevantFieldValue);
        }


        #region "Utills"

        /// <summary>
        /// Gets the document page.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        private static DocumentPageContent GetDocumentPage(int index)
        {
            DocumentPageContent pageContent = null;
            var json = HttpContext.Current.Session[SessionCurrentDocumentContent].ToString();
            var jsonPages = JArray.Parse(json);
            var documentPages = jsonPages.ToObject<List<DocumentPageContent>>();
            if (documentPages != null)
            {
                pageContent = documentPages.Find(d => d.Index.Equals(index));
                pageContent.Text = HttpContext.Current.Server.HtmlEncode(pageContent.Text);
            }
            return  pageContent;
        }

        /// <summary>
        /// Chunks the content of the document and return first page of the document.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        private static DocumentPageContent ChunkDocumentContent(string content, int pageIndex)
        {
            var chunkSize = 25600; // Default value 25*1024  - 25K
            var configValue = ConfigurationManager.AppSettings.Get(ChunkSizeConfig);
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                int.TryParse(configValue, out chunkSize);
            }
            var currentPage = new DocumentPageContent { Index = 0, TotalPageCount = 1, Text = content };
            if (content.Length <= chunkSize)
            {
                HttpContext.Current.Session.Remove(SessionCurrentDocumentContent);
                return currentPage;
            }
            var result = content.Select((x, i) => i)
                .Where(i => i%chunkSize == 0)
                .Select(i => content.Substring(i, content.Length - i >= chunkSize ? chunkSize : content.Length - i));
            var documentPages = ConstructDocumentPages(result.ToList());
            HttpContext.Current.Session[SessionCurrentDocumentContent] = JArray.FromObject(documentPages).ToString();
            if (documentPages.Any() && documentPages.Exists(d => d.Index.Equals(pageIndex)))
                currentPage = documentPages.Find(d => d.Index.Equals(pageIndex));
            return currentPage;
        }

        /// <summary>
        /// Constructs the document pages.
        /// </summary>
        /// <param name="pages">The pages.</param>
        /// <returns></returns>
        private static List<DocumentPageContent> ConstructDocumentPages(IReadOnlyCollection<string> pages)
        {
            var documentPages = pages.Select((page, index) => new DocumentPageContent
            {
                Text = page,
                Index = index,
                TotalPageCount = pages.Count
            }).ToList();
            return documentPages;
        }

        private static void ProcessDocument(AnalysisSetDocumentInfo document)
        {
            //chunking documents
            if (document.DocumentReferenceId != null && document.DocumentReferenceId.Trim().Length > 0)
            {
                var pages = ChunkDocumentContent(document.DocumentText, 0);
                pages.Text = HttpContext.Current.Server.HtmlEncode(pages.Text);
                document.Pages = pages;
                var codingValue = document.ReviewerCategory;
                document.Coding = new CodingInfo
                {
                    IsCoded = !codingValue.Equals(CodingValueNotCoded),
                    Value = codingValue
                };
            }

            document.DocumentText = ""; //set to null since data has been chunked and stored in document pages
        }

        #endregion
    }
}