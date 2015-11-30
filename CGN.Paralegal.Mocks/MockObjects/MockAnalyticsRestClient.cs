using System.Diagnostics;

namespace CGN.Paralegal.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Linq;

    using ClientContracts.Analytics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class MockAnalyticsRestClient : IAnalyticsRestClient
    {
        private const string MockDataNameSpace = "CGN.Paralegal.Mocks.MockData";

        private const int ControlDocumentIndex = 0;
        private const int TrainingDocumentIndex = 1;
        private const int QualityDocumentIndex = 2;
        private const int ReviewerCategoryIndex = 3;
        private const int PredictedCategoryIndex = 4;

        private const string Relevant = "Relevant";

        private const string NotRelevant = "Not_Relevant";

        private const string NotCoded = "Not_Coded";

        private const string Skipped = "Skipped";

        private static DocumentList mockDocuments;
        private static List<AnalysisSet> analysisSets;
        private static String currentAnalyticSetName = "Training Set 001";
        private static TrainingSetSummary resultTrainingSetSummary = new TrainingSetSummary();
        private static int trainingSetCount = 0;


        private static List<List<int>> mockDiscrepancies;

        private static List<QcSet> mockQcSts;

        private static PredictAllSummary mockPredictAllSummaryInfo;

        private static AnalyticsProjectInfo mockAnalyticsProjectInfo;        

        private static DocumentList mockTrainingSetAdditionalDocuments;

        private static int addTrainingDocumentIndex = 0;

        private static string GetEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        public List<AnalyticsWorkflowState> GetAnalyticWorkflowState(long matterId, long dataSetId, long projectId)
        {

            return MockWorkflowState.WorkflowState;
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
        /// Updated State
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public List<AnalyticsWorkflowState> UpdateAnalyticWorkflowState(long matterId, long datasetId, long projectId, string binderId, List<AnalyticsWorkflowState> workflowState)
        {
            if(workflowState.Count > 1)
            {
                mockDocuments = null;
                mockAnalyticsProjectInfo = null;
                mockQcSts = null;
                mockTrainingSetAdditionalDocuments = null;
                analysisSets = null;                
                addTrainingDocumentIndex = 0;
                trainingSetCount = 0;
                mockPredictAllSummaryInfo = null;
                resultTrainingSetSummary = new TrainingSetSummary();
                resultTrainingSetSummary.CompletedRoundsSummary = new AnalysisSet();
                resultTrainingSetSummary.CompletedRoundDetails = new List<AnalysisSet>();
                resultTrainingSetSummary.CurrentRoundProgress = new AnalysisSet();
                MockWorkflowState.Initialize();
                MockWorkflowState.ChangeToState = new AnalyticsWorkflowState();
                //Ensure data is initialized as expected for the updated workflow state

                var random = new Random();

                //ControlSet
                AnalyticsWorkflowState state = workflowState.Find(p => p.Name == State.ControlSet);
                if (state.ReviewStatus == Status.Inprogress)
                {
                    var controlSetDocs = this.GetDocuments(matterId, datasetId, projectId, "controlset", null);

                    //ReviewStatus inprogress means some controlset documents should be coded
                    if (controlSetDocs.Documents.Find(d => d.Fields[ReviewerCategoryIndex].Value == NotCoded) != null)
                    {
                        foreach (var doc in controlSetDocs.Documents)
                        {
                            doc.Fields[ReviewerCategoryIndex].Value = (random.Next(2) == 0) ? Relevant : NotCoded;
                        }
                    }
                    UpdateControlSetSummary(this.GetAllAnalysisSets(matterId, datasetId, projectId));
                }

                if (state.ReviewStatus == Status.Completed)
                {
                    var controlSetDocs = this.GetDocuments(matterId, datasetId, projectId, "controlset", null);

                    //ReviewStatus Completed means all controlset documents should be coded
                    if (controlSetDocs.Documents.Find(d => d.Fields[ReviewerCategoryIndex].Value == NotCoded) != null)
                    {
                        foreach (var doc in controlSetDocs.Documents)
                        {
                            doc.Fields[ReviewerCategoryIndex].Value = (random.Next(2) == 0)? NotRelevant : Relevant;
                        }
                    }
                    
                    UpdateControlSetSummary(this.GetAllAnalysisSets(matterId, datasetId, projectId));
                }

                //TrainingSet
                state = workflowState.Find(p => p.Name == State.TrainingSet);
                if (state.CreateStatus == Status.Completed)
                {
                    this.CreateTrainingset(matterId.ToString(CultureInfo.InvariantCulture), datasetId.ToString(CultureInfo.InvariantCulture), projectId.ToString(CultureInfo.InvariantCulture));

                    //MockWorkflowState.ChangeToState = workflowState.Find(p => p.Name == State.ControlSet);
                    //resultTrainingSetSummary.CompletedRoundsSummary = new AnalysisSet();
                    //resultTrainingSetSummary.CompletedRoundDetails = new List<AnalysisSet>();
                    //resultTrainingSetSummary.CompletedRoundsSummary.Type = AnalysisSetType.TrainingSet;
                    //resultTrainingSetSummary.RoundsCompleted = 0;
                    //resultTrainingSetSummary.CurrentRound = 1;
                }
                if (state.ReviewStatus == Status.Inprogress)
                {
                    var trainingSetDocs = this.GetDocuments(matterId, datasetId, projectId, "trainingset", CreateQueryContext(AnalysisSetType.TrainingSet, "Training Set 001"));
                    trainingSetDocs.Documents[0].Fields[ReviewerCategoryIndex].Value = Relevant;
                    trainingSetDocs.Documents[1].Fields[ReviewerCategoryIndex].Value = NotRelevant;
                    trainingSetDocs.Documents[2].Fields[ReviewerCategoryIndex].Value = Skipped;
                    UpdateTrainingSetSummary(this.GetAllAnalysisSets(matterId, datasetId, projectId), "current");
                    resultTrainingSetSummary.CompletedRoundsSummary = new AnalysisSet();
                    resultTrainingSetSummary.RoundsCompleted = 0;
                    resultTrainingSetSummary.CurrentRound = 1;

                }
                if (state.ReviewStatus == Status.Completed)
                {
                    var queryContext = CreateQueryContext(AnalysisSetType.TrainingSet, "Training Set 001");
                    var trainingSetDocs = this.GetDocuments(matterId, datasetId, projectId, "trainingset",queryContext);
                    foreach (var doc in trainingSetDocs.Documents)
                    {
                        doc.Fields[ReviewerCategoryIndex].Value = (random.Next(2) == 0) ? NotRelevant : Relevant;
                    }
                    this.CreateTrainingset(matterId.ToString(CultureInfo.InvariantCulture), datasetId.ToString(CultureInfo.InvariantCulture), projectId.ToString(CultureInfo.InvariantCulture));
                    queryContext.AnalysisSet.Name = "Training Set 002";
                    trainingSetDocs = this.GetDocuments(matterId, datasetId, projectId, "trainingset", queryContext);
                    foreach (var doc in trainingSetDocs.Documents)
                    {
                        doc.Fields[ReviewerCategoryIndex].Value = (random.Next(2) == 0) ? NotRelevant : Relevant;
                    }

                    this.CreateTrainingset(matterId.ToString(CultureInfo.InvariantCulture), datasetId.ToString(CultureInfo.InvariantCulture), projectId.ToString(CultureInfo.InvariantCulture));
                }

                //QcSet
                state = workflowState.Find(p => p.Name == State.QcSet);
                if (state.CreateStatus == Status.Completed)
                {
                    this.CreateQcSet(matterId, datasetId, projectId, new QcSet());                    
                }
                if (state.ReviewStatus == Status.Inprogress)
                {
                    var queryContext = CreateQueryContext(AnalysisSetType.QcSet, "QCSet01");
                    var qcSetDocs = this.GetDocuments(matterId, datasetId, projectId, "qcset", queryContext);
                    foreach (var doc in qcSetDocs.Documents)
                    {
                        doc.Fields[ReviewerCategoryIndex].Value = (random.Next(2) == 0) ? Relevant : NotCoded;
                    }
                }

                if (state.ReviewStatus == Status.Completed)
                {
                    var queryContext = CreateQueryContext(AnalysisSetType.QcSet, "QCSet01");
                    var qcSetDocs = this.GetDocuments(matterId, datasetId, projectId, "qcset", queryContext);
                    foreach (var doc in qcSetDocs.Documents)
                    {
                        doc.Fields[ReviewerCategoryIndex].Value = (random.Next(2) == 0) ? NotRelevant : Relevant;
                    }
                }
                //PredictSet
                state = workflowState.Find(p => p.Name == State.PredictSet);
                if (state.ReviewStatus == Status.Completed)
                {
                    if (mockDocuments != null)
                    {
                        foreach (var doc in mockDocuments.Documents)
                        {
                            doc.Fields[PredictedCategoryIndex].Value = doc.Fields[PredictedCategoryIndex].Value == NotCoded ? Relevant : doc.Fields[PredictedCategoryIndex].Value;
                        }
                        
                    }

                }
            }
            
            return MockWorkflowState.UpdateStates(workflowState);
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
            return 1000;
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
            return MockWorkflowState.ChangeToState;
        }
        
        private static DocumentQueryContext CreateQueryContext(AnalysisSetType analysisSetType, string analysisSetName)
        {
            var queryContext = new DocumentQueryContext();
            queryContext.AnalysisSet = new AnalysisSet();
            queryContext.AnalysisSet.Name = analysisSetName;
            queryContext.AnalysisSet.Type = analysisSetType;
            queryContext.KeyWord = "";
            queryContext.Filters = new List<Field>();
            queryContext.Sort = new List<Sort>();
            queryContext.MatterId = 1;
            queryContext.DatasetId = 1;
            queryContext.ProjectId = 1;
            queryContext.PageIndex = 1;
            queryContext.PageSize = 500;
            return queryContext;
        }
        
      public AnalyticsProjectInfo CreateAnalyticProject(string matterId, string dataSetId, AnalyticsProjectInfo project)
      {
            UpdateAnalyticProjectState(matterId, dataSetId);
                
            if (MockWorkflowState.ProjectSetup.CreateStatus == Status.NotStarted)
            {
                MockWorkflowState.UpdateState(name: State.ProjectSetup, createStatus: Status.Completed, reviewStatus: Status.Completed, isCurrent: true);
            }
            else
            {
                var projectId = 1;
                this.GetDocuments(Convert.ToInt64(matterId, CultureInfo.CurrentCulture), Convert.ToInt64(dataSetId, CultureInfo.CurrentCulture), projectId, "projectsetup", null);
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-additionaldocuments.json",
                             MockDataNameSpace, matterId, dataSetId, projectId);
                var mockData = GetEmbeddedResource(resourceName);
                var mockAddtionalDocuments = JsonConvert.DeserializeObject<DocumentList>(mockData);
                for (int i = 1; i <= 5; i++)
                {
                      var random = new Random();
                      mockDocuments.Documents.Add(mockAddtionalDocuments.Documents[random.Next(1, 14)]);
                }
                mockDocuments.Total = mockDocuments.Total + 5;
                mockAnalyticsProjectInfo.TotalDocumentCount = mockAnalyticsProjectInfo.TotalDocumentCount + 5;
            }
            return mockAnalyticsProjectInfo;
       }

      /// <summary>
      /// Get AnalyticsProjectInfo data
      /// </summary>
      /// <param name="matterId">The matter identifier.</param>
      /// <param name="dataSetId">The data set identifier.</param>
      /// <returns></returns>
        private static void UpdateAnalyticProjectState(string matterId, string dataSetId)
        {
            if (mockAnalyticsProjectInfo == null)
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-create-analytic-project.json",
                           MockDataNameSpace, matterId, dataSetId);

                var mockData = GetEmbeddedResource(resourceName);

                mockAnalyticsProjectInfo = JsonConvert.DeserializeObject<AnalyticsProjectInfo>(mockData);
            }         
        }
        /// <summary>
        /// Deletes the analytic project.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="dataSetId">The data set identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        public void DeleteAnalyticProject(long matterId, long dataSetId, long projectId)
        {
            mockDocuments = null;
            analysisSets = null;
            currentAnalyticSetName = "Training Set 001";
            trainingSetCount = 0;
            resultTrainingSetSummary = new TrainingSetSummary();

            MockWorkflowState.Initialize();
        }

        public QcSet CreateQcSet(long matterId, long dataSetId, long projectId, QcSet qcSet)
        {
            //Reset next workflow state
            MockWorkflowState.ChangeToState = new AnalyticsWorkflowState();

            var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-qcSetsInfo.json",
                MockDataNameSpace, matterId, dataSetId, projectId);
            var mockData = GetEmbeddedResource(resourceName);
            List<QcSet> tempQcStsInfo = JsonConvert.DeserializeObject<List<QcSet>>(mockData);

            if (mockQcSts == null)
            {
                mockQcSts = new List<QcSet>();
            }

            mockQcSts.Add(tempQcStsInfo[mockQcSts.Count % 2]);

            MockWorkflowState.UpdateState(name: State.QcSet, createStatus: Status.Completed, reviewStatus: Status.NotStarted, isCurrent: true);

            foreach (var qcSt in mockQcSts)
            {
                qcSt.IsCurrent = false;
            }

            var lastQcSet = mockQcSts.Last();
            lastQcSet.IsCurrent = true;

            return lastQcSet;
        }

        public long GetAvailableDocumentCount(long orgId, long matterId, long datasetId, long projectId)
        {
            return 1000;
        }

        public AnalyticsProjectInfo GetAnalyticProject(string matterId, string dataSetId, string projectId)
        {
           UpdateAnalyticProjectState(matterId, dataSetId);
           return mockAnalyticsProjectInfo;
        }

        public List<SavedSearch> GetSavedSearches(string orgId, string matterId, string dataSetId)
        {
            var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-saved-searches.json",
                  MockDataNameSpace, matterId, dataSetId);

            var mockData = GetEmbeddedResource(resourceName);

            return JsonConvert.DeserializeObject<List<SavedSearch>>(mockData);
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

            var docList = new DocumentList();
           
            if(mockDocuments == null )
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-documents.json",
                             MockDataNameSpace, matterId, dataSetId, projectId);

                var mockData = GetEmbeddedResource(resourceName);
                mockDocuments = JsonConvert.DeserializeObject<DocumentList>(mockData);
             
            }
            if(analysisSet == "controlset")
            {
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[ControlDocumentIndex].Value == "1").ToList();
                docList.Total = docList.Documents.Count;
            }
            if(analysisSet ==  "trainingset")
            {
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[TrainingDocumentIndex].Name == queryContext.AnalysisSet.Name).ToList();
                docList.Total = docList.Documents.Count;
            }
            if (analysisSet == "qcset")
            {
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[QualityDocumentIndex].Name == queryContext.AnalysisSet.Name).ToList();
                docList.Total = docList.Documents.Count;
            }
            if (analysisSet == "alldocuments")
            {
                docList.Documents = mockDocuments.Documents;
                docList.Total = docList.Documents.Count;
            }
            
            docList = FilterDocumentList(docList, queryContext);

            var pageDocuments = PageNavigationDocumentList(docList, queryContext);
            return pageDocuments;           
          
        }

        private static DocumentList FilterDocumentList(DocumentList docList, DocumentQueryContext queryContext)
        {
            if (queryContext != null)
            { 
                if (queryContext.KeyWord.Length > 0)
                {
                    docList.Documents = docList.Documents.Where(d => d.Fields[ReviewerCategoryIndex].Value.Contains(queryContext.KeyWord)).ToList();
                    docList.Total = docList.Documents.Count;
                }

                if (queryContext.Filters.Count > 0)
                {
                    docList.Documents = docList.Documents.Where(d => d.Fields[ReviewerCategoryIndex].Value == queryContext.Filters[0].Value).ToList();
                    docList.Total = docList.Documents.Count;

                    docList=UpdatePredictedCategoryValue(docList, queryContext);
                 
                }

                if (queryContext.Sort.Count > 0 && docList.Documents.Count > 0)
                {
                    var index = docList.Documents[0].Fields.FindIndex(f => f.Name == queryContext.Sort[0].Name);
                    if (queryContext.Sort[0].Order == SortOrder.Ascending)
                    {
                        docList.Documents = docList.Documents.Where(d => d.Fields[index].Name == queryContext.Sort[0].Name).OrderBy(o => o.Fields[index].Value).ToList();
                    }
                    if (queryContext.Sort[0].Order == SortOrder.Descending)
                    {
                        docList.Documents = docList.Documents.Where(d => d.Fields[index].Name == queryContext.Sort[0].Name).OrderByDescending(o => o.Fields[index].Value).ToList();
                    }
                }
                
            }

            return docList;
        }

        private static DocumentList UpdatePredictedCategoryValue(DocumentList docList, DocumentQueryContext queryContext)
        {
            if (queryContext.Filters.Count == 2 && docList.Total > 0)
            {
                var totalDocs = docList.Total;
                var count = queryContext.Filters[0].Value == Relevant ? queryContext.Filters[1].Value == Relevant ? mockDiscrepancies[0][0] : mockDiscrepancies[0][1] : queryContext.Filters[1].Value == Relevant ? mockDiscrepancies[1][0] : mockDiscrepancies[1][1];
                if (count > totalDocs)
                    count = totalDocs;
                docList.Documents = docList.Documents.GetRange(0, count);
                docList.Total = docList.Documents.Count;
                foreach (var doc in docList.Documents)
                {
                    doc.Fields[PredictedCategoryIndex].Value = queryContext.Filters[1].Value;
                }

            }

            return docList;
        }




        /// <summary>
        /// Gets document list for a page.
        /// </summary>
        /// <param name="docList">The sorted or filtered document list.</param>
        /// <param name="queryContext">The query context.</param>
        /// <returns>The page documents list</returns>
        private static DocumentList PageNavigationDocumentList(DocumentList docList, DocumentQueryContext queryContext)
        {
            if (queryContext != null)
            {
                var start = ((queryContext.PageIndex - 1) * queryContext.PageSize);
                var end = start + queryContext.PageSize;
                var count = queryContext.PageSize;


                if (end > docList.Total)
                {
                    count = docList.Total - start;
                }

                docList.Documents = docList.Documents.GetRange(start, count);
                docList.Total = docList.Documents.Count;
            }

            return docList;
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
            if(analysisSets == null)
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-analysisSets.json",
                      MockDataNameSpace, matterId, dataSetId, projectId);

                var mockData = GetEmbeddedResource(resourceName);

                analysisSets = JsonConvert.DeserializeObject<List<AnalysisSet>>(mockData);

                UpdateControlSetSummary(analysisSets);
            }
            
            return analysisSets;
        }

        private static void UpdateControlSetSummary(List<AnalysisSet> analysisSets)
        {
            if (mockDocuments != null)
            {
                var controlSetDocs = mockDocuments.Documents.Where(d => d.Fields[ControlDocumentIndex].Value == "1").ToList();
                var controlSetSummary = analysisSets.Find(a => a.Type == AnalysisSetType.ControlSet);
                if (controlSetSummary != null)
                {
                    controlSetSummary.NumberOfNotCodedDocuments = controlSetDocs.Where(d => d.Fields[ReviewerCategoryIndex].Value == NotCoded).ToList().Count;
                    controlSetSummary.NumberOfNotRelevantDocuments = controlSetDocs.Where(d => d.Fields[ReviewerCategoryIndex].Value == NotRelevant).ToList().Count;
                    controlSetSummary.NumberOfRelevantDocuments = controlSetDocs.Where(d => d.Fields[ReviewerCategoryIndex].Value == Relevant).ToList().Count;
                }
            }
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
            if (mockDocuments != null)
            {
                var analysisSets = GetAllAnalysisSets(Convert.ToInt64(matterId, CultureInfo.CurrentCulture), Convert.ToInt64(dataSetId, CultureInfo.CurrentCulture), Convert.ToInt64(projectId, CultureInfo.CurrentCulture));
                if(resultTrainingSetSummary.RoundsCompleted == 0)
                {
                    resultTrainingSetSummary.CompletedRoundsSummary = new AnalysisSet();
                    resultTrainingSetSummary.CompletedRoundsSummary.Type = AnalysisSetType.TrainingSet;
                }
                resultTrainingSetSummary.CurrentRoundProgress = UpdateTrainingSetSummary(analysisSets, "current");

            }
            else
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-trainingSummary.json",
                    MockDataNameSpace, matterId, dataSetId, projectId);

                var mockData = GetEmbeddedResource(resourceName);

                resultTrainingSetSummary = JsonConvert.DeserializeObject<TrainingSetSummary>(mockData);

            }

            return resultTrainingSetSummary;
            }
           
        private static AnalysisSet UpdateTrainingSetSummary(List<AnalysisSet> analysisSets, string roundStatus)
        {
                AnalysisSet roundSummary = new AnalysisSet();           
                roundSummary.Name = currentAnalyticSetName;
                roundSummary.Type = AnalysisSetType.TrainingSet;
                roundSummary.BinderId = analysisSets.FirstOrDefault(s => s.Name == currentAnalyticSetName).BinderId;
                var docList = new DocumentList();
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[1].Name == currentAnalyticSetName).ToList();

                if (roundStatus == "completed")
                {
                    roundSummary = resultTrainingSetSummary.CompletedRoundsSummary;
                }               
                roundSummary.TotalDocuments = roundSummary.TotalDocuments + docList.Documents.Count;
                roundSummary.NumberOfRelevantDocuments = roundSummary.NumberOfRelevantDocuments + docList.Documents.Where(d => d.Fields[ReviewerCategoryIndex].Value == Relevant).ToList().Count;
                roundSummary.NumberOfNotRelevantDocuments = roundSummary.NumberOfNotRelevantDocuments + docList.Documents.Where(d => d.Fields[ReviewerCategoryIndex].Value == NotRelevant).ToList().Count;
                roundSummary.NumberOfNotCodedDocuments = roundSummary.NumberOfNotCodedDocuments + docList.Documents.Where(d => d.Fields[ReviewerCategoryIndex].Value == NotCoded).ToList().Count;
                roundSummary.NumberOfSkippedDocuments = roundSummary.NumberOfSkippedDocuments + docList.Documents.Where(d => d.Fields[ReviewerCategoryIndex].Value == Skipped).ToList().Count;
                return roundSummary;           
        }

        public ControlSetSummary GetControlSetSummary(long matterId, long dataSetId, long projectId)
        {
            var result = new ControlSetSummary();
            if (mockDocuments != null)
            {
                var controlSetMockDocuments = new DocumentList();

                controlSetMockDocuments.Documents = mockDocuments.Documents.Where(d => d.Fields[ControlDocumentIndex].Value == "1").ToList();
                result.TotalDocuments = controlSetMockDocuments.Documents.Count;
                controlSetMockDocuments.Documents.ForEach(d =>
                {
                    var reviewerCategoryValue = d.Fields.Find(f => (f.DisplayName == "Reviewer Category")).Value;
                    if (reviewerCategoryValue == Relevant)
                    {
                        result.NumberOfRelevantDocuments++;
                    }
                    else if (reviewerCategoryValue == NotRelevant)
                    {
                        result.NumberOfNotRelevantDocuments++;
                    }
                    else if (reviewerCategoryValue == NotCoded)
                    {
                        result.NumberOfNotCodedDocuments++;
                    }
                    else
                    {
                        result.NumberOfSkippedDocuments++;
                    }
                });
                result.PercentageOfTotalPopulation = result.NumberOfRelevantDocuments/(float)result.TotalDocuments;
                result.EstimatedTotalDocuments = (long)(result.PercentageOfTotalPopulation * mockDocuments.Documents.Count);
                result.PercentageOfTotalPopulation *= 100;
            }
            else
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-controlset-summary.json",
                      MockDataNameSpace, matterId, dataSetId, projectId);

                var mockData = GetEmbeddedResource(resourceName);

                result = JsonConvert.DeserializeObject<ControlSetSummary>(mockData);

            }
            return result;
        }


        public List<Tag> GetAnalyticProjectTags(string orgId, string matterId, string dataSetId)
        {
            var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-analytic-project-tags.json",
                  MockDataNameSpace, matterId, dataSetId);

            var mockData = GetEmbeddedResource(resourceName);

            return JsonConvert.DeserializeObject<List<Tag>>(mockData); 
        }

        public long GetSearchCount(string orgId, string matterId, string dataSetId, string projectId, JObject documentSelection)
        {
            if (MockWorkflowState.ProjectSetup.CreateStatus == Status.Completed)
            {
                return 5;
            }
            return 30;
        }

        public int GetControlsetSampleSize(
            string matterId,
            string dataSetId,
            string confidenceLevel,
            string marginOfError)
        {
            var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-analytic-project-samplesize.json",
                MockDataNameSpace, matterId, dataSetId);

            var mockData = GetEmbeddedResource(resourceName);

            return Convert.ToInt32(mockData, CultureInfo.InvariantCulture);
        }

        public ControlSet CreateControlset(string matterId, string dataSetId, string projectId, ControlSet project)
        {
            var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-analytic-project_{3}-create-controlset.json",
                MockDataNameSpace, matterId, dataSetId, projectId);

            var mockData = GetEmbeddedResource(resourceName);

            var result = JsonConvert.DeserializeObject<ControlSet>(mockData);

            //Reset Mock Documents
            mockDocuments = null;

            MockWorkflowState.UpdateState(name: State.ControlSet, createStatus: Status.Completed, reviewStatus: Status.NotStarted, isCurrent: true);

            return result;
        }

        public string CreateTrainingset(string matterId, string dataSetId, string projectId)
        {
            resultTrainingSetSummary.RoundsCompleted = trainingSetCount;
            var allAnalysisSets = GetAllAnalysisSets(Convert.ToInt64(matterId, CultureInfo.CurrentCulture), Convert.ToInt64(dataSetId, CultureInfo.CurrentCulture), Convert.ToInt64(projectId, CultureInfo.CurrentCulture));
            Status reviewStatus;
            if(trainingSetCount > 0)
            {
                reviewStatus = Status.Inprogress;
                resultTrainingSetSummary.CompletedRoundsSummary = UpdateTrainingSetSummary(allAnalysisSets, "completed");
                resultTrainingSetSummary.CompletedRoundDetails.Add(UpdateTrainingSetSummary(allAnalysisSets, "current"));
            }
            else
            {
                reviewStatus = Status.NotStarted;
                resultTrainingSetSummary.CompletedRoundsSummary = new AnalysisSet();
                resultTrainingSetSummary.CompletedRoundDetails = new List<AnalysisSet>();
                resultTrainingSetSummary.CompletedRoundsSummary.Type = AnalysisSetType.TrainingSet;
            }

            if (MockWorkflowState.ChangeToState.Name == State.PredictSet)
            {
                return string.Empty;
            }

            MockWorkflowState.UpdateState(State.TrainingSet, Status.Completed, reviewStatus, true);

            trainingSetCount++;
            resultTrainingSetSummary.CurrentRound = trainingSetCount;
            if (trainingSetCount >= 2)
            {
                // Mock that training is stable
                var states = new List<AnalyticsWorkflowState> { new AnalyticsWorkflowState { Name = State.PredictSet, CreateStatus = Status.NotStarted, ReviewStatus = Status.NotStarted, IsCurrent = false, Order = 4 } };
                MockWorkflowState.UpdateStates(states);
            }

            currentAnalyticSetName = trainingSetCount % 2 != 0 ? "Training Set 001" : "Training Set 002";
            
            var trainingSet = allAnalysisSets.FirstOrDefault(s => s.Name == currentAnalyticSetName);
            return trainingSet != null ? trainingSet.BinderId : Guid.NewGuid().ToString();
        }

        public bool UpdateProjectDocumentCodingValue(
            long orgId,
            long matterId,
            long dataSetId,
            long projectId,
            string documentId,
            string codingValue)
        {
            var doc = mockDocuments.Documents.Find(d => documentId == d.ReferenceId);
            if (doc != null)
            {
                var field = doc.Fields.Find(f => f.DisplayName == "Reviewer Category");
                if (field != null)
                {
                    field.Value = codingValue;
                    return true;
                }
            }

            return true;
        }
 
       
        public AnalysisSetDocumentInfo GetDocumentByRefId(long orgId, long matterId, long dataSetId,
            long projectId, string analysisset, string documentRefId)
        {
            AnalysisSetDocumentInfo result = new AnalysisSetDocumentInfo();
            var doc = mockDocuments.Documents.Find(d => documentRefId == d.ReferenceId);
            if (doc != null)
            {
                result.DocumentReferenceId = doc.ReferenceId;
                result.ProjectName = "Predictive Coding Project";
                result.DocumentText = "Mock Document " + doc.Id + " - Concordance® Evolution feeds your need for speed during document review. Litigation support professionals and document reviewers told us what they need most in an e-discovery review engine: outstanding speed, capacity and ease of use. Get it all with LexisNexis® Concordance® Evolution. Concordance® puts you in the driver’s seat, maintaining control and accelerating the review process to enhance client service and improve efficiency.";
                var field = doc.Fields.Find(f => f.DisplayName == "Reviewer Category");
                var dcnField = doc.Fields.Find(f => f.DisplayName == "DCN");
                if (field != null)
                {
                    result.ReviewerCategory = field.Value;           
                }
                if (dcnField != null)
                {
                    result.DocumentDcn = dcnField.Value;
                }
            }

            return result;
        }

       
        public AnalysisSetDocumentInfo GetUncodedDocument(long orgId, long matterId, long dataSetId,
            long projectId, string documentRefId, DocumentQueryContext searchContext)
        {
            var result = new AnalysisSetDocumentInfo();
            var docList = new DocumentList();
            if(searchContext.AnalysisSet.Type == AnalysisSetType.ControlSet)
            {
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[ControlDocumentIndex].Value == "1").ToList();
                docList.Total = docList.Documents.Count;
            }
            if (searchContext.AnalysisSet.Type == AnalysisSetType.TrainingSet)
            {
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[TrainingDocumentIndex].Name == searchContext.AnalysisSet.Name).ToList();
                docList.Total = docList.Documents.Count;
            }
            if (searchContext.AnalysisSet.Type == AnalysisSetType.QcSet)
            {
                docList.Documents = mockDocuments.Documents.Where(d => d.Fields[QualityDocumentIndex].Name == searchContext.AnalysisSet.Name).ToList();
                docList.Total = docList.Documents.Count;
            }
            result.TotalDocumentCount = docList.Documents.Count;

            var doc =
                docList.Documents.Find(
                    d => d.Fields.Exists(f => (f.DisplayName == "Reviewer Category" && f.Value == NotCoded)));

            if (doc != null)
            {
                result.DocumentReferenceId = doc.ReferenceId;
                result.ProjectName = "Predictive Coding Project";
                result.DocumentText = "Mock Document " + doc.Id + " - Concordance® Evolution feeds your need for speed during document review. Litigation support professionals and document reviewers told us what they need most in an e-discovery review engine: outstanding speed, capacity and ease of use. Get it all with LexisNexis® Concordance® Evolution. Concordance® puts you in the driver’s seat, maintaining control and accelerating the review process to enhance client service and improve efficiency.";
                result.DocumentIndexId = Convert.ToInt32(doc.Id);
                result.ReviewerCategory = NotCoded;
                var dcnField = doc.Fields.Find(f => (f.DisplayName == "DCN"));
                if (dcnField != null)
                {
                    result.DocumentDcn = dcnField.Value;
                }
            }

            var reviewStatus = doc != null ? Status.Inprogress : Status.Completed;
            MockWorkflowState.SetReviewStatus(searchContext.AnalysisSet.Type, reviewStatus);

            return result;
        }
 
        public int ScheduleJobForExportDocuments(long matterId, long dataSetId, long projectId, DocumentQueryContext queryContext)
        {
            return 10;
        }

        public int CreateJobForCategorizeControlset(string matterId, string dataSetId, string projectId,
            string trainingRound)
        {
            return 100; //Job Id
        }

        /// <summary>
        ///  Create Job for categorize analysisset
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="analysisSetType">Analysis Set Type</param>
        /// <param name="trainingRound">Training Round</param>
        /// <returns></returns>
        public int CreateJobForCategorizeAnalysisset(string matterId, string dataSetId, string projectId, string analysisSetType,string binderId,
            string trainingRound)
        {
            return 102; //TODO: Update Job Id
        }

        /// <summary>
        ///  Create Job for categorize All documents
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="jobScheduleInfo">JobScheduleInfo</param>
        /// <returns></returns>
        public int CreateJobForCategorizeAll(string matterId, string dataSetId, string projectId,
           JobScheduleInfo jobScheduleInfo)
        {
            MockWorkflowState.UpdateState(name: State.PredictSet, createStatus: Status.Completed, reviewStatus: Status.Completed, isCurrent: true);

            return 101; //Job Id
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
            return true;
        }

        /// <summary>
        /// Get the categorize status
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="dataSetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <returns></returns>
        public bool ValidateQcSetCreationPrerequisite(long orgId, long matterId, long dataSetId, long projectId)
        {

            if (mockDocuments != null)
            {
                foreach (var doc in mockDocuments.Documents)
                {
                    if (doc.Fields[ControlDocumentIndex].Value == null &&
                        doc.Fields[TrainingDocumentIndex].Value == null &&
                        doc.Fields[ReviewerCategoryIndex].Value == null &&
                        doc.Fields[PredictedCategoryIndex].Value == null &&
                        doc.Fields[QualityDocumentIndex].Value == null)
                        return false;
                }

            }
            return true;
        }

        /// <summary>
        /// Return list of all prediction scores
        /// </summary>
        /// <param name="orgId">org Id</param>
        /// <param name="matterId">matter Id</param>
        /// <param name="dataSetId">dataSet Id</param>
        /// <param name="projectId">project Id</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        public List<PredictionScore> GetPredictionScores(long orgId, long matterId, long dataSetId, long projectId, string setType, string setId)
        {
            var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-predictionScores.json",
    MockDataNameSpace, matterId, dataSetId, projectId);
            var mockData = GetEmbeddedResource(resourceName);

            var scores = JsonConvert.DeserializeObject<List<PredictionScore>>(mockData);

            return scores;
        }

        /// <summary>
        /// Return categoriztion discrepancies
        /// </summary>
        /// <param name="orgId">org Id</param>
        /// <param name="matterId">matter Id</param>
        /// <param name="dataSetId">dataSet Id</param>
        /// <param name="projectId">project Id</param>
        /// <param name="setType">The set Type</param>
        /// <param name="setId">The set Id </param>
        /// <returns></returns>
        public List<List<int>> GetDiscrepancies(long orgId, long matterId, long dataSetId, long projectId, string setType, string setId)
        {
            if(mockDiscrepancies==null)
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-discrepancies.json",
    MockDataNameSpace, matterId, dataSetId, projectId);
                var mockData = GetEmbeddedResource(resourceName);

                mockDiscrepancies = JsonConvert.DeserializeObject<List<List<int>>>(mockData);
            }

            return mockDiscrepancies;
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
            if (mockQcSts == null)
            {
                mockQcSts = new List<QcSet>();
            }
            else
            {
                foreach (var qcSet in mockQcSts)
                {
                    var qcSetMockDocuments = mockDocuments.Documents.Where(d => d.Fields[QualityDocumentIndex].Name == qcSet.Name).ToList();
                    qcSet.NumberOfRelevantDocuments = qcSetMockDocuments.Where(d => d.Fields[ReviewerCategoryIndex].Value == Relevant).ToList().Count;
                    qcSet.NumberOfNotRelevantDocuments = qcSetMockDocuments.Where(d => d.Fields[ReviewerCategoryIndex].Value == NotRelevant).ToList().Count;
                    qcSet.NumberOfNotCodedDocuments = qcSetMockDocuments.Where(d => d.Fields[ReviewerCategoryIndex].Value == NotCoded).ToList().Count;
                }
            }

            return mockQcSts;
        }

        /// <summary>
        /// Gets predict all summary information
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <returns>predict all summary</returns>
        public PredictAllSummary GetPredictAllSummary(long orgId, long matterId, long datasetId, long projectId)
        {
            if (mockPredictAllSummaryInfo == null)
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-predictAllSummaryInfo.json",
    MockDataNameSpace, matterId, datasetId, projectId);
                var mockData = GetEmbeddedResource(resourceName);

                mockPredictAllSummaryInfo = JsonConvert.DeserializeObject<PredictAllSummary>(mockData);
            }

            return mockPredictAllSummaryInfo;
        }

        private List<Discrepancy> mockPredictionDiscrepanciesInfo;
        /// <summary>
        /// Gets prediction discrepancies
        /// </summary>
        /// <param name="orgId">The org identifier.</param>
        /// <param name="matterId">The matter identifier.</param>
        /// <param name="datasetId">The dataset identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="setType">The set type.</param>
        /// <returns></returns>
        public List<Discrepancy> GetPredictionDiscrepancies(long orgId, long matterId, long datasetId, long projectId,
            string setType)
        {
            if (mockPredictionDiscrepanciesInfo == null)
            {
                var resourceName = string.Format(CultureInfo.InvariantCulture, "{0}.matter_{1}-dataset_{2}-project_{3}-{4}-predictionDiscrepanciesInfo.json",
    MockDataNameSpace, matterId, datasetId, projectId, setType);
                var mockData = GetEmbeddedResource(resourceName);

                mockPredictionDiscrepanciesInfo = JsonConvert.DeserializeObject<List<Discrepancy>>(mockData);
            }

            return mockPredictionDiscrepanciesInfo;
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
            project.IsValidProjectName = (project.Name != "Test");
            project.IsValidFieldPrefix = (project.FieldPreFix != "PC");
            return project;
        }

        public void AutoCodeTruthSet(string matterId, string dataSetId, string projectId,
            string analysisSet, DocumentQueryContext documentQuery, string truthsetFieldName, string relevantFieldValue)
        {
            //No need of mock implementation for this method
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
            var currentTrianingRoundSummary = UpdateTrainingSetSummary(this.GetAllAnalysisSets(matterId, dataSetId, projectId), "current");
            if(currentTrianingRoundSummary.NumberOfRelevantDocuments == 0 || currentTrianingRoundSummary.NumberOfNotRelevantDocuments == 0 )
            {
                if(mockTrainingSetAdditionalDocuments == null)
                {
                    var resourceName = string.Format(CultureInfo.InvariantCulture,
                    "{0}.matter_{1}-dataset_{2}-project_{3}-additionaltrainingsetdocuments.json",
                    MockDataNameSpace, matterId, dataSetId, projectId);
                    var mockData = GetEmbeddedResource(resourceName);
                    mockTrainingSetAdditionalDocuments = JsonConvert.DeserializeObject<DocumentList>(mockData);
                }
                for (int i = addTrainingDocumentIndex; i < addTrainingDocumentIndex + 2; i++)
                {
                    mockDocuments.Documents.Add(mockTrainingSetAdditionalDocuments.Documents[i]);
                }
                addTrainingDocumentIndex += 2;
                mockDocuments.Total = mockDocuments.Total + 2 ;
            return 50;
        }
            return 0;
        }
    }
}