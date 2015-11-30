using System.Configuration;
using FluentAssertions;
using CGN.Paralegal.UI.Tests.Controlset;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CGN.Paralegal.UI.Tests.Common;

namespace CGN.Paralegal.UI.Tests
{
    [TestClass, System.Runtime.InteropServices.GuidAttribute("EFD69779-D9B9-4FFC-BD16-25522B197092")]
    public class QcSetDashboardTests : TestBase
    {
        private AnalysisSetReviewActions analysisSetReviewActions;

        const string WorkflowStateQcSetCreateNotStarted =
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

        const string WorkflowStateQcSetReviewCompleted =
@"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":5},
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
        /// This web test scenario tests verify that the page is in qc-set-not-created state 
        /// </summary>
        [TestMethod]        
        public void PRC_QcsetDashboard_BeforeQcSetCreated_VerifyQcSetNotCreatedState()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetCreateNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            QcSetDashboardActions.VerifyQcSetNotCreatedState(driver);
        }

        /// <summary>
        /// This web test scenario tests all review states(ready to start/inprogress/completed) of qcset
        /// </summary>
        [TestMethod]        
        public void PRC_QcsetDashboard_ClickStartReview_ReviewShouldBeInProgress()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);
            QcSetDashboardActions.VerifyQcsetReviewReadyToStartState(driver);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.ClickVerificationSetStartReview();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();

            this.analysisSetReviewActions.ClickProjectDashboardLink();

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);

            QcSetDashboardActions.VerifyQcsetReviewInProgressState(driver);
        }

        /// <summary>
        /// This web test scenario tests qcset summary doc list by
        /// Select count in qcset widget from review dashboard
        /// should open doc list
        /// </summary>
        [TestMethod]        
        public void PRC_QcsetDashboard_ClickDocLinksInQcSetWidgetFromReviewDashboard_ShouldDisplayDocsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();

            QcSetDashboardActions.VerifyRelevantDocListInQcSetSummary(driver);

            QcSetDashboardActions.ClickOnProjectDashboardLink(driver);

            this.analysisSetReviewActions.WaitForLoadPage();
            
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            
            this.analysisSetReviewActions.WaitForLoadPage();

            QcSetDashboardActions.VerifyNotRelevantDocListInQcSetSummary(driver);

            QcSetDashboardActions.ClickOnProjectDashboardLink(driver);

            this.analysisSetReviewActions.WaitForLoadPage();
            
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();

            QcSetDashboardActions.VerifyNotCodedDocListInQcSetSummary(driver);
        }
      

    }
}
