using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace CGN.Paralegal.UI.Tests.Controlset
{
    using CGN.Paralegal.UI.Tests.Common;
    using CGN.Paralegal.UI.Tests.ControlSet;
    [TestClass]
    public class ControlSetDashboardTests : TestBase
    {
        private ControlSetDashboardActions controlSetDashboardActions;
        private AnalysisSetReviewActions analysisSetReviewActions;

        private const string WorkflowStateControlSetNotCreated =
           @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateControlSetReviewNotStarted =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateControlSetReviewInProgress =
  @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""InProgress"",""IsCurrent"":true,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateControlSetReviewCompleted =
  @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateTrainingSetReviewNotStarted =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateTrainingSetReviewInProgress =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""InProgress"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string WorkflowStateTrainingSetReviewCompleted =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        [TestInitialize]
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            this.controlSetDashboardActions = new ControlSetDashboardActions(driver);
            this.analysisSetReviewActions = new AnalysisSetReviewActions(driver);
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            this.controlSetDashboardActions = null;
            TestBaseCleanup();
        }

        /// <summary>
        /// This web test scenario tests verify that the page is in control-set-not-created state 
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_BeforeControlSetCreated_VerifyControlSetNotCreatedState_ShouldPass()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            this.controlSetDashboardActions.WaitForAngularShellPageLoad();

            this.controlSetDashboardActions.VerifyControlSetNotCreatedState();
        }

        /// <summary>
        /// This web test scenario tests the page is in ready to start state after control set is created
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_AfterControlSetCreated_VerifyActionStatus_ShouldPass()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetNotCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            this.controlSetDashboardActions.WaitForAngularShellPageLoad();

            CreateControlSet();
            
            this.controlSetDashboardActions.VerifyControlsetCreatedActionStatus();
        }

        /// <summary>
        /// This web test scenario tests the page is in correct in-progress state after review started in reviewer dashboard
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_ControSetReviewerDashBoard_AfterControlSetReviewStarted_ShouldDisplayStatusAsInProgress()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.controlSetDashboardActions.WaitForAngularShellPageLoad();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickProjectDashboardLink();

            this.controlSetDashboardActions.WaitForLoadPage();
            this.controlSetDashboardActions.VerifyInProgressState();
        }

        /// <summary>
        /// This web test scenario tests the page is in correct completed state after control set review completed in reviewer dashboard
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_ControSetReviewerDashBoard_AfterControlSetReviewCompleted_SholudDisplayStatusAsCompleted()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.controlSetDashboardActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();

            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();

            this.analysisSetReviewActions.ClickOkOnFinishPopup();
            this.controlSetDashboardActions.WaitForLoadPage();

            this.controlSetDashboardActions.VerifyCompletedState();
        }

        /// <summary>
        /// This web test scenario tests the page is in correct in-progress state after review started in admin dashboard 
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_ControSetAdminDashBoard_AfterControlSetReviewStarted_ShouldDisplayStatusAsInProgress()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewInProgress);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            this.controlSetDashboardActions.VerifyInProgressState();
        }

        /// <summary>
        /// This web test scenario tests the page is in correct completed state after control set review completed in admin dashboard 
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_ControSetAdminDashBoard_AfterControlSetReviewCompleted_ShouldDisplayStatusAsCompleted()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            this.controlSetDashboardActions.VerifyCompletedState();
        }

        /// <summary>
        /// This web test scenario tests the page is in correct completed state after control set review completed in admin dashboard 
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_ControSetAdminDashBoard_AfterControlSetReviewCompleted_ShouldDisplayCreateTrainingSet()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            this.controlSetDashboardActions.VerifyCreateTrainingSet();
        }

        /// <summary>
        /// Test clicking on start review button on dashboard page
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickStartReview_ShouldGoToFirstDocument()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.IsAtDocument(1);
        }
        /// <summary>
        /// Test click on continue review button on dashboard page
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickContinueReview_ShouldGoToFirstUncodedDocument()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.MarkDocumentAsRelevant();
            this.analysisSetReviewActions.ClickProjectDashboardLink();
            this.analysisSetReviewActions.ClickContinueReviewButton();
            this.analysisSetReviewActions.IsAtDocument(2);
        }
        /// <summary>
        /// Functin to create control  set
        /// </summary>
        private static void CreateControlSet()
        {
            CreateControlsetActions.ClickStartButtonToOpenCreateControlsetPopup(driver);
            CommonActions.WaitForLoadPage(driver);

            CreateControlsetActions.CheckControlSetPopup(driver);

            CreateControlsetActions.ClickOkButtonToCreateControlset(driver);
            CommonActions.WaitForLoadPage(driver);

            CommonActions.RefreshMainWindow(driver);
            CommonActions.WaitForLoadPage(driver);

            CreateControlsetActions.CheckNextStatusIsReviewControlset(driver);
        }

      
        /// <summary>
        /// Check predication discrepancies after trainingset review has completed
        /// </summary>
        //[TestMethod]
        public void PRC_ControlSet_CheckDiscrepaniesAfterTrainingSetRoundCompleted_ShouldDisplayPredictionDiscrepanciesWidget()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingSetReviewCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            this.controlSetDashboardActions.WaitForLoadPage();

            this.controlSetDashboardActions.ClickOnControlSetTab(driver);

            this.controlSetDashboardActions.VerifyPredictionDiscrepanciesWidget(driver);

        }

        

    }
}
