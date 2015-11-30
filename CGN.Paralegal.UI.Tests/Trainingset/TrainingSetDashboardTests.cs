using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CGN.Paralegal.UI.Tests.Trainingset
{
    using CGN.Paralegal.UI.Tests.Common;
    using CGN.Paralegal.UI.Tests.ProjectDashboard;
    [TestClass]
    public class TrainingSetDashboardTests : TestBase
    {
        private AnalysisSetReviewActions analysisSetReviewActions;

        const string WorkflowStateTrainingCompleted =
          @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        const string WorkflowStateTrainingInProgress =
         @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""InProgress"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";
        const string WorkflowStateTrainingNotStarted =
    @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";
        const string WorkflowStatePredictAllNotStarted =
   @"[{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4}]";
        private const string workflowStateQCSetNotCreated =
           @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";
        const string WorkflowStateQcSetReviewNotStarted =
     @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";
        [TestInitialize]
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            this.analysisSetReviewActions = new AnalysisSetReviewActions(driver);
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
           TestBaseCleanup();
        }

        /// <summary>
        /// 
        /// This web test scenario tests the table view in training summary widget
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_ClickOnTableViewInTrainingSummary_ShouldDisplayTable()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            TrainingSetDashboardActions.ClickOnTrainingTab(driver);

            TrainingSetDashboardActions.ClickOnTableView(driver);

            TrainingSetDashboardActions.ClickOnRoundLink(driver);

            this.analysisSetReviewActions.IsAtDocListPage();

            TrainingSetDashboardActions.ClickOnProjectDashboardLink(driver);

            TrainingSetDashboardActions.ClickOnTrainingTab(driver);

            TrainingSetDashboardActions.ClickOnTableView(driver);

            TrainingSetDashboardActions.ClickOnRelevantLink(driver);

            this.analysisSetReviewActions.IsAtDocListPage();

            TrainingSetDashboardActions.ClickOnProjectDashboardLink(driver);

            TrainingSetDashboardActions.ClickOnTrainingTab(driver);

            TrainingSetDashboardActions.ClickOnTableView(driver);

            TrainingSetDashboardActions.ClickOnNotRelevantLink(driver);

            this.analysisSetReviewActions.IsAtDocListPage();

            TrainingSetDashboardActions.VerifyNotRelevantDocuments(driver);
       }


        /// <summary>
        ///  Test that training trendsXL are displayed after training is completed
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_VerifyChartViewinTrainngTrend_ShouldDisplayChart()
        {
           TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.ClickTrainingTab(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.VerifyTrainingTrendsChart(driver);
        }


        /// <summary>
        ///  Test that training trend status  are displayed if the training is In Progress
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_VerifyTrainingTrendInProgressStatus_ShouldDisplayTrainingTrendStatus()
        {           
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingInProgress);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.ClickTrainingTab(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.VerifyTrainingTrendsInProgressStatus(driver);
        }


        /// <summary>
        ///  Test that training trend grid and chart  is displayed based on the button clicks 
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_ClickonChartviewAndGridViewTrainingTrend_ShouldDisplayTrainingTrendGridAndChart()
        {            
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.ClickTrainingTab(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.ClickTrainingTrendTableView(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.VerifyTrainingTrendsGridView(driver);
            TrainingSetDashboardActions.ClickTrainingTrendGraphView(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.VerifyTrainingTrendsChart(driver);

        }

        /// <summary>
        ///  This web test scenario tests review all training documents and finish the review
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_ReviewAllDocuments_ShouldFinishTrainingSetReview()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            
            this.analysisSetReviewActions.ReviewAllDocumentsAutoAdvanceToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();

            TrainingSetDashboardActions.ClickContinueTrainingReviewButton(driver);
            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.ReviewAllDocumentsAutoAdvanceToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();
          
            TrainingSetDashboardActions.ClickFinishTrainingReviewButton(driver);
            this.analysisSetReviewActions.WaitForLoadPage();

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            TrainingSetDashboardActions.VerifyRunPredictionsEnabled(driver);            
        }
        /// <summary>
        ///This web test scenario schedules Predict All now and completes it
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_SchedulePredictAllNow_ShouldDisplayPredictAllAsCompleted()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            TrainingSetDashboardActions.ClickRunPredictionsButton(driver);
            TrainingSetDashboardActions.ClickOnCancelButtonInPredictAllDocumentsPopup(driver);
            TrainingSetDashboardActions.ClickRunPredictionsButton(driver);
            TrainingSetDashboardActions.ClickOnConfirmButtonInPredictAllDocumentsPopup(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            TestHelper.UpdateWorkflowState(workflowStateQCSetNotCreated);
            TrainingSetDashboardActions.VerifyPredictAllIsCompleted(driver);
        }
        /// <summary>
        /// This web test scenario tests confirms the triggering and scheduling of predictall later
        /// </summary>
         [TestMethod]
        [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_SchedulePredictAllLater_ShouldDisplayPredictAllAsCompleted()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);
            this.analysisSetReviewActions.ActivateTrainingTab();
            TrainingSetDashboardActions.ClickRunPredictionsButton(driver);
            TrainingSetDashboardActions.SelectLaterOptionInPopup(driver);
            TestHelper.UpdateWorkflowState(workflowStateQCSetNotCreated);
            TrainingSetDashboardActions.ClickOnConfirmButtonInPredictAllDocumentsPopup(driver);
            TrainingSetDashboardActions.VerifyPredictAllIsCompleted(driver);

        }
        /// <summary>
         /// This web test scenario selects statisticalsample and creates verification set
        /// </summary>
        [TestMethod]
        [TestCategory("Training Set")]
         public void PRC_TrainingAdminDashboard_AfterPredictAll_ShouldSelectStatisticalSampleAndCreateVerificationSet()
         {
             TestHelper.UpdateWorkflowState(workflowStateQCSetNotCreated);
             CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
             this.analysisSetReviewActions.ActivateTrainingTab();
             TrainingSetDashboardActions.ClickOnCreateVerificationSet(driver);
             TrainingSetDashboardActions.CheckVerificationSetPopup(driver);
             TrainingSetDashboardActions.ClickCancelButtonInCreateVerificationSetPopup(driver);
             TrainingSetDashboardActions.VerifyCreateVerificationSetIsEnabled(driver);
             TrainingSetDashboardActions.ClickOnCreateVerificationSet(driver);
             TrainingSetDashboardActions.CheckVerificationSetPopup(driver);
             TrainingSetDashboardActions.EnterVerificationSetName(driver);
             TrainingSetDashboardActions.SelectStatisticalSampleOption(driver);
             TrainingSetDashboardActions.ClickCreateButtonInCreateVerificationSetPopup(driver);
             TrainingSetDashboardActions.VerifyVerificationSetIsCreated(driver);

         }
        /// <summary>
        /// This web test scenario selects fixed size and creates verification set
        /// </summary>
         [TestMethod]
         [TestCategory("Training Set")]
        public void PRC_TrainingAdminDashboard_AfterPredictAll_ShouldSelectFixedSizeAndCreateVerificationSet()
         {
             TestHelper.UpdateWorkflowState(workflowStateQCSetNotCreated);
             CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
             this.analysisSetReviewActions.ActivateTrainingTab();
             TrainingSetDashboardActions.ClickOnCreateVerificationSet(driver);
             TrainingSetDashboardActions.CheckVerificationSetPopup(driver);
             TrainingSetDashboardActions.EnterVerificationSetName(driver);
             TrainingSetDashboardActions.SelectFixedSizeOption(driver);
             TrainingSetDashboardActions.ClickCreateButtonInCreateVerificationSetPopup(driver);
             TrainingSetDashboardActions.VerifyVerificationSetIsCreated(driver);
             TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);
             CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
             this.analysisSetReviewActions.ActivateVerificationTab();
             TrainingSetDashboardActions.ClickOnStartReview(driver);
             this.analysisSetReviewActions.WaitForLoadPage();
             this.analysisSetReviewActions.DisableAutoAdvance();
             this.analysisSetReviewActions.ClickProjectDashboardLink();
             this.analysisSetReviewActions.WaitForLoadPage();
             CommonActions.ClickTab(driver, CommonActions.VerificationTabId);
             TrainingSetDashboardActions.VerifyVerificationsetReviewInProgressState(driver);

           
         }

         /// <summary>
         /// This web test scenario tests review all training documents and checks if rolling average chart and grid are displayed
         /// </summary>
         [TestMethod]
         [TestCategory("Training Set")]
         public void PRC_TrainingAdminDashboard_SecondRoundCompleted_ShouldDisplayPredictAhead()
         {
             TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);
            
             CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            
             TrainingSetDashboardActions.VerifyRollingAverageChart(driver);

             CommonActions.WaitForElement(driver);
             TrainingSetDashboardActions.ClickTrainingTableView(driver);
             CommonActions.WaitForElement(driver);
             
             TrainingSetDashboardActions.VerifyRollingAverageTable(driver);
         }
        /// <summary>
        /// display predict discrepancies widget
        /// </summary>
         [TestMethod]
         [TestCategory("Training Set")]
         public void PRC_TrainingAdminDashboard_SecondRoundCompleted_DisplayPredictDiscrepanciesAhead()
         {   
             TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);
             CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
             this.analysisSetReviewActions.WaitForLoadPage();
             TrainingSetDashboardActions.VerifyPredictDiscrepanciesAhead(driver);
         }

        /// <summary>
        ///Display Review in Progress status
        /// </summary>

         [TestMethod]
         [TestCategory("Training Set")]
         public void PRC_TrainingAdminDashboard_CheckDiscrepaniesWhenTrainingSetReviewStarted_ShouldDisplayTrainingSetReviewInProgressText()
         {
             TestHelper.UpdateWorkflowState(WorkflowStateTrainingInProgress);

             CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
             this.analysisSetReviewActions.WaitForLoadPage();
             TrainingSetDashboardActions.ClickTrainingTab(driver);
             this.analysisSetReviewActions.WaitForLoadPage();
             TrainingSetDashboardActions.VerifyPredictDiscrepanciesInProgressStatus(driver);
         }
         /// <summary>
         /// Click doc links in prediction discrepancies widget
         /// </summary>
         [TestMethod]
         [TestCategory("Training Set")]
         public void PRC_TrainingAdminDashboard_ClickDocLinksInPredictionDiscrepanciesWidget_ShouldDisplayDocsInDocList()
         {
             TestHelper.UpdateWorkflowState(WorkflowStateTrainingCompleted);

             CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

             this.analysisSetReviewActions.WaitForLoadPage();

             this.analysisSetReviewActions.ActivateTrainingTab();

             TrainingSetDashboardActions.ClickOnTruePositivesLink(driver);

             TrainingSetDashboardActions.ClickOnProjectDashboardLink(driver);

             this.analysisSetReviewActions.ActivateTrainingTab();

             TrainingSetDashboardActions.ClickOnFalseNegativesLink(driver);

             TrainingSetDashboardActions.ClickOnProjectDashboardLink(driver);

             this.analysisSetReviewActions.ActivateTrainingTab();

             TrainingSetDashboardActions.ClickOnFalsePositivesLink(driver);

             TrainingSetDashboardActions.ClickOnProjectDashboardLink(driver);
             this.analysisSetReviewActions.ActivateTrainingTab();

             TrainingSetDashboardActions.ClickOnTrueNegativesLink(driver);

             TrainingSetDashboardActions.ClickOnProjectDashboardLink(driver);
             this.analysisSetReviewActions.ActivateTrainingTab();

             TrainingSetDashboardActions.VerifyPredictionDiscrepanciesWidget(driver);

         }

        
        
   }
}
