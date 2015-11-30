using CGN.Paralegal.UI.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace CGN.Paralegal.UI.Tests.ControlSet
{
    /// <summary>
    /// Tests for control set review
    /// </summary>
    [TestClass]
    public class ControlSetReviewTests : TestBase
    {

        private ControlSetDashboardActions controlSetDashboardActions;
        private AnalysisSetReviewActions analysisSetReviewActions;

        private const string WorkflowStateControlSetReviewNotStarted =
    @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";


        [TestInitialize]
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            this.analysisSetReviewActions = new AnalysisSetReviewActions(driver);
            this.controlSetDashboardActions = new ControlSetDashboardActions(driver);
            TestHelper.UpdateWorkflowState(WorkflowStateControlSetReviewNotStarted);
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }
        /// <summary>
        /// Control set review should display text
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_GoToFirstDocument_ShouldSeeDocumentText()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(1);
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentTextVisible();
        }
        /// <summary>
        /// Test marking document as releveant or not relevant
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_SetDocumentToRelevantNotRelevant_ShouldSucceed()
        {

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsNotRelevant();
        }
        /// <summary>
        /// Test changing coding values for documents
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ChangeCodeValue_ShouldSucceed()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsRelevant();
        }
        /// <summary>
        /// Test going to a particular document
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_GotoDocumentNumber_ShouldGoToCorrectDocument()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.IsAtDocument(2);
        }

        /// <summary>
        /// Test going to a particular document
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickNextDocumentArrow_ShouldGoToCorrectDocument()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickNextDocumentButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.IsAtDocument(3);
        }


        /// <summary>
        /// Test entering out of range document numbers in navigator
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_GotoDocumentNumberOutOfRange_ShouldDisplayErrorMessage()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(0);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyOutOfRangeErrorMessageIsDisplayed();

            var maxDocument = this.analysisSetReviewActions.GetMaxDocumentNumber();
            this.analysisSetReviewActions.GoToDocument(maxDocument + 1);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.VerifyOutOfRangeErrorMessageIsDisplayed();
        }
        /// <summary>
        /// Test clicking first document button
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickFirstDocButton_ShouldGoToFirstDocument()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(5);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickFirstDocumentButton();

            this.analysisSetReviewActions.IsAtDocument(1);
        }
        /// <summary>
        /// Test clicking last document button
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickLastDocumentButton_ShouldGoToLastDocument()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.GoToDocument(5);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.ClickLastDocumentButton();
            var maxDocument = this.analysisSetReviewActions.GetMaxDocumentNumber();
            this.analysisSetReviewActions.IsAtDocument(maxDocument);
        }

        /// <summary>
        /// Test clicking on View Set button
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickStartViewSet_ShouldGoToDocListingPage()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickViewSetsLink();
            this.controlSetDashboardActions.WaitForLoadPage();

            this.analysisSetReviewActions.IsAtDocListPage();
        }

        //
        //Finish Control Set Review test cases
        //
        /// <summary>
        /// Test finishing control set review and its popup window
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_FinishReview_ShouldDisplayFinishPopup()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.VerifyFinishPopup();
        }
        /// <summary>
        /// Test clicking cancel button on finish review popup
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickCloseOnFinishPopup_ShouldStayInPage()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickCloseOnFinishPopup();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.IsAtReviewPage();

        }
        /// <summary>
        /// Test clicking project dashboard on finish review popup
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickOkOnFinishPopup_ShouldGoToProjectDashboard()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickOkOnFinishPopup();

            this.controlSetDashboardActions.WaitForLoadPage();
            this.controlSetDashboardActions.IsAtProjectDashboardPage();

        }

        /// <summary>
        /// Test clicking next uncoded document button
        /// </summary>
        [TestMethod]
        public void PRC_ControlSetReview_ClickNextUncodedDocumentButtonAtTheLastDocument_ShouldGoToFirstUncodedDocument()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ClickLastDocumentButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.MarkDocumentAsRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickNextUncodedDocumentButton();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.IsAtDocument(1);
        }
    }
}
