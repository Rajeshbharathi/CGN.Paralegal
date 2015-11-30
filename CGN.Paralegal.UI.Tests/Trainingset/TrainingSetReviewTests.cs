using CGN.Paralegal.UI.Tests.Common;
using CGN.Paralegal.UI.Tests.Controlset;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CGN.Paralegal.UI.Tests.Trainingset
{
    using CGN.Paralegal.UI.Tests.ProjectDashboard;

    [TestClass]
    public class TrainingSetReviewTests : TestBase
    {
        private AnalysisSetReviewActions analysisSetReviewActions;

        const string WorkflowStateTrainingNotStarted =
    @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":3},
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
        const string WorkflowStateTrainingCompleted =
@"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":3},
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
        ///  This web test scenario tests document review navigation and verify document text loaded or not
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_NavigateToFirstDocument_ShouldSeeDocumentText()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();


            this.analysisSetReviewActions.GoToDocument(1);
            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyDocumentTextVisible();
        }


        /// <summary>
        ///  This web test scenario tests review training document and mark as relevant
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_MarkDocumentAsRelevant_ShouldSucceed()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();

            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.MarkDocumentAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsRelevant();
        }

        /// <summary>
        ///  This web test scenario tests review training document and mark as not relevant
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_MarkDocumentAsNotRelevant_ShouldSucceed()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();


            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsNotRelevant();


        }


        /// <summary>
        ///  This web test scenario tests review training document and mark as skipped
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_MarkDocumentAsSkipped_ShouldSucceed()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();

            this.analysisSetReviewActions.DisableAutoAdvance();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.MarkDocumentAsSkipped();

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsskipped();


        }



        /// <summary>
        ///  This web test scenario tests chang coding value from relevant to not relevant after move back from one document to another document
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_ChangeCodeValue_ShouldSucceed()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();


            this.analysisSetReviewActions.GoToDocument(1);
            this.analysisSetReviewActions.MarkDocumentAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(1);
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsRelevant();
            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(1);
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsNotRelevant();

        }


        /// <summary>
        ///  This web test scenario tests enter out of range in navigation text box and verify the expected system behaviour
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_EnterDocumentNumberOutOfRange_ShouldDisplayErrorMessage()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();

            this.analysisSetReviewActions.DisableAutoAdvance();

            this.analysisSetReviewActions.GoToDocument(0);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyOutOfRangeErrorMessageIsDisplayed();

            this.analysisSetReviewActions.WaitForLoadPage();

            var maxDocument = this.analysisSetReviewActions.GetMaxDocumentNumber();
            this.analysisSetReviewActions.GoToDocument(maxDocument + 1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyOutOfRangeErrorMessageIsDisplayed();
        }


        /// <summary>
        ///  This web test scenario tests review all training documents and complete the review
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_ReviewAllDocuments_ShouldDisplayConfirmationModal()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.WaitForElement(driver);
            this.analysisSetReviewActions.CheckConfirmationModalWindow();

        }

        /// <summary>
        /// Test clicking project dashboard on finish review popup
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_ClickProjectDashboardOnFinishPopup_ShouldGoToProjectDashboard()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.ClickProjectDashboardOnFinishPopup();

            CommonActions.WaitForLoadPcDashboardPage(driver);
        }








        /// <summary>
        /// This web test scenario tests review all training documents by clicking cancel should
        /// redirect to training set review
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_ClickCancelInFinishTrainingSetReview_ShouldStayInPage()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);

            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.ReviewAllDocumentsAutoAdvanceToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.ClickCancelButtonInFinishTrainingSetReview();
        }







        /// <summary>
        ///  This web test scenario tests review all training document and mark as relevant and display continue training popup 
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_MarkAllDocumentsAsRelevant_ShouldDisplayContinueTrainingModalWindow()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsAsRelevant();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.CheckContinueTrainingConfirmationModalWindow();
        }

        /// <summary>
        ///  This web test scenario tests review all training document and mark as not relevant and display continue training popup 
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_MarkAllDocumentsAsNotRelevant_ShouldDisplayContinueTrainingModalWindow()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsAsNotRelevant();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.CheckContinueTrainingConfirmationModalWindow();
        }

        /// <summary>
        ///  This web test scenario tests review all training document and mark as skipped and display continue training popup 
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_MarkAllDocumentsAsSkipped_ShouldDisplayContinueTrainingModalWindow()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsAsSkipped();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.CheckContinueTrainingConfirmationModalWindow();
        }

        /// <summary>
        ///  This web test scenario tests review all training document as not relevant after clicking close in continue training popup
        /// and display confirmation modal
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_AfterContinueTrainingMarkAllDocumentsAsNotRelevant_ShouldDisplayConfirmationModalWindow()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.ClickTrainingSetStartReviewButton();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsAsRelevant();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickCloseButtonInContinueTrainingPopup();
            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();
            this.analysisSetReviewActions.ClickNextUncodedDocumentButton();
            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();
            this.analysisSetReviewActions.ClickNextUncodedDocumentButton();
            this.analysisSetReviewActions.CheckConfirmationModalWindow();

        }
        /// <summary>
        /// This web test scenario continues the document review  and display continue training popup 
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_ContinueReviewDocuments_ShouldDisplayFinishPopup()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingInProgress);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            TrainingSetDashboardActions.ClickOnContinueReview(driver);
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            TrainingSetDashboardActions.ClickProjectDashboard(driver);

        }
        /// <summary>
        /// This web test scenario displays list of relevant,not relevant,skipped and not coded documents
        /// </summary>
        [TestMethod]
        public void PRC_TrainingSetReview_VerifyCodingSummary_ShouldDisplayDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateTrainingInProgress);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            this.analysisSetReviewActions.WaitForLoadPage();
            TrainingSetDashboardActions.ClickRelevantCodingSummary(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl); ;
            this.analysisSetReviewActions.ActivateTrainingTab();
            TrainingSetDashboardActions.ClickNotRelevantCodingSummary(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            TrainingSetDashboardActions.ClickSkippedCodingSummary(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();
            TrainingSetDashboardActions.ClickNotCodedCodingSummary(driver);
            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.ActivateTrainingTab();

        }



    }
}
