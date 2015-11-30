using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CGN.Paralegal.UI.Tests
{
    using CGN.Paralegal.UI.Tests.Common;


    /// <summary>
    /// Web test for 'Create Project'
    /// </summary>
    [TestClass]
    public class DocListTests : TestBase
    {

        private AnalysisSetReviewActions analysisSetReviewActions;

        private const string WorkflowStateControlSetNotStarted =
            @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
                {""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":2},
                {""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
                {""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
                {""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
                {""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateControlSetCompleted =
          @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
                {""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":2},
                {""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
                {""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
                {""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
                {""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        [TestInitialize]
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            this.analysisSetReviewActions = new AnalysisSetReviewActions(driver);
          //  TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }        
        #region Happy Day Scenarios
        /// <summary>
        /// This web test scenario tests doc list by 
        /// clicking on the start review button should open doc review 
        /// </summary>
        /// 
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickStartReviewFromReviewDashboard_ShouldDoNavigationBtwDocsInDocReview()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickNextDocButton(driver);
            DocListActions.ClickLastDocButton(driver);
            DocListActions.ClickPreviousDocButton(driver);
            DocListActions.ClickFirstDocButton(driver);
            DocListActions.ShouldDoNavigationBtwDocsInDocReview(driver).Should().BeTrue();
        }        
      /// <summary>
      /// This web test scenario tests doc list by
      ///  Clicking start review button from review dashboard should
      ///  open doc review and doc list 
      /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickStartReviewFromReviewDashboard_ShouldOpenDocReviewAndDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickNextDocButton(driver);
            DocListActions.ClickViewSetInDocReview(driver);
            DocListActions.ClickViewDocumentInDocList(driver);
            DocListActions.IsShouldOpenDocReviewAndDocList(driver).Should().BeTrue();
        }
        /// <summary>
        /// This web test scenario tests doc list by
        /// Select count in controlset widget from review dashboard
        /// should open doc list
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")] 
        public void PRC_DocList_ClickDocLinksInControlSetWidgetFromReviewDashboard_ShouldDisplayDocsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetCompleted);      
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickRelevantDocList(driver);
            DocListActions.ClickProjectDashboardReview(driver);
            DocListActions.ClickNotRelevantDocList(driver);
            analysisSetReviewActions.IsAtDocListPage();
            
        }
        /// <summary>
        /// This web test scenario tests doc list by
        /// click export button with less than 500 doc
        /// should export csv file.
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickExportButtonWithLess500Docs_ShouldExportCSV()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickNextDocButton(driver);
            DocListActions.ClickViewSetInDocReview(driver);
            DocListActions.ClickExportCsv(driver);
            DocListActions.ShouldExportCSV(driver).Should().BeTrue();
        }
        /// <summary>
        /// This web test scenario tests doc list by
        /// click export button with more than 500 doc
        /// should show job schedule message.
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickExportButtonWithMore500Docs_ShouldShowJobScheduleMessage()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickNextDocButton(driver);
            DocListActions.ClickViewSetInDocReview(driver);
            DocListActions.ClickExportCsv(driver);            
            DocListActions.ShouldShowJobScheduleMessage(driver).Should().BeTrue();
        }
        /// <summary>
        /// This web test scenario tests doc list by
        /// Select count in controlset widget from admin dashboard
        /// should open doc list
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickDocLinksInControlSetWidgetFromAdminDashboard_ShouldDisplayDocsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetCompleted);      
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);            
            DocListActions.ClickRelevantDocList(driver);            
            DocListActions.ClickProjectDashboardReview(driver);
            DocListActions.ClickNotRelevantDocList(driver);
            analysisSetReviewActions.IsAtDocListPage();
        }

            
        /// <summary>
        /// This web test scenario tests doc list by
        /// click sort and filter in doc list should display
        /// sorted results in doc list
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickFilterInDocList_ShouldDisplayFilteredResultsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickViewSetInDocReview(driver);
            DocListActions.ClickFilterSearch(driver);
            DocListActions.ClickFilterClear(driver);
            DocListActions.ShouldDisplaySortedFilteredResultsInDocList(driver).Should().BeTrue();
        }

        /// <summary>
        /// This web test scenario tests doc list by
        /// click sort and filter in doc list should display
        /// sorted results in doc list
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickSortAscDesc_ShouldDisplaySortedResultsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickViewSetInDocReview(driver);
            DocListActions.ClickSortAscending(driver);
            DocListActions.ClickSortDescending(driver);            
            DocListActions.ShouldDisplaySortedFilteredResultsInDocList(driver).Should().BeTrue();
        }

        /// <summary>
        /// This web test scenario tests doc list by
        ///  click search button in doc list should display
        ///  searched results in doc list
        /// </summary>
        [TestMethod]
        [TestCategory("Doc List")]
        public void PRC_DocList_ClickSearchButtonInDocList_ShouldDisplaySearchedResultsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotStarted);   
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            DocListActions.ClickStartReviewToOpenDocReview(driver);
            DocListActions.DisableAutoAdvance(driver);
            DocListActions.ClickNextDocButton(driver);
            DocListActions.ClickViewSetInDocReview(driver);            
            DocListActions.ClickSearchButton(driver);
            DocListActions.ShouldDisplaySearchedResultsInDocList(driver).Should().BeTrue();
        }
       
        #endregion
    }
}