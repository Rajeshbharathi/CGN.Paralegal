using CGN.Paralegal.UI.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace CGN.Paralegal.UI.Tests.QcSet
{
    [TestClass]
    public class QcSetReviewTests : TestBase
    {
        private AnalysisSetReviewActions analysisSetReviewActions;

        const string WorkflowStateQcSetReviewNotStarted =
    @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":true,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        const string WorkflowStateQcSetReviewInProgress =
          @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""InProgress"",""IsCurrent"":true,""Order"":5},
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
        ///  This web test scenario tests document review navigation and verify document text loaded or not
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_NavigateToFirstDocument_ShouldSeeDocumentText()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);
            this.analysisSetReviewActions.ClickVerificationSetStartReview();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            
            
            this.analysisSetReviewActions.GoToDocument(1);
            this.analysisSetReviewActions.WaitForLoadPage();
            
            this.analysisSetReviewActions.VerifyDocumentTextVisible();
        }

     
        /// <summary>
        ///  This web test scenario tests review qc document and mark as relevant
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_MarkDocumentAsRelevant_ShouldSucceed()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);
            
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);
            this.analysisSetReviewActions.ClickVerificationSetStartReview();

            this.analysisSetReviewActions.WaitForLoadPage();
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
        ///  This web test scenario tests review qc document and mark as not relevant
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_MarkDocumentAsNotRelevant_ShouldSucceed()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);

            this.analysisSetReviewActions.ClickVerificationSetStartReview();

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            
            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.MarkDocumentAsNotRelevant();

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(2);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.GoToDocument(1);

            this.analysisSetReviewActions.WaitForLoadPage();

            this.analysisSetReviewActions.VerifyDocumentIsMarkedAsNotRelevant();
        }

        /// <summary>
        ///  This web test scenario tests change coding value from relevant to not relevant after move back from one document to another document
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_ChangeCodeValue_ShouldSucceed()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            this.analysisSetReviewActions.WaitForLoadPage();

            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);
            
            this.analysisSetReviewActions.ClickVerificationSetStartReview();

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
        ///  This web test scenario tests enter out of range in navigation text box and verify the expected 
        /// system behaviour
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_EnterDocumentNumberOutOfRange_ShouldDisplayErrorMessage()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);

            this.analysisSetReviewActions.ClickVerificationSetStartReview();

            this.analysisSetReviewActions.WaitForLoadPage();
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
        ///  This web test scenario tests review all qc documents and complete the review
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_ReviewAllDocuments_ShouldDisplayConfirmationModal()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);

            this.analysisSetReviewActions.ClickVerificationSetStartReview();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();
            
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.CheckQcSetCompleteConfirmationModalWindow();
        }


        /// <summary>
        ///  This web test scenario tests review all qc documents and click finish in qcset complete popup
        /// should redirect to reviewer dashboard
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_ClickFinishInQcSetReviewCompletePopUp_ShouldRedirectToReviewerDashboardPage()
        {

            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);

            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);

            this.analysisSetReviewActions.ClickVerificationSetStartReview();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickPrimaryButtonOnQcSetPopup();
            this.analysisSetReviewActions.IsAtReviewerDashboardPage();
        }


        
        
        /// <summary>
        /// This web test scenario tests review all qc documents and  clicking cancel button in qc set complete modal should
        /// stay in review page
        /// </summary>
        [TestMethod]
        public void PRC_QcSetReview_ClickCancelInQcSetReviewCompletePopUp_ShouldStayInReviewPage()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewNotStarted);            
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);
            CommonActions.ClickTab(driver, CommonActions.VerificationTabId);

            this.analysisSetReviewActions.ClickVerificationSetStartReview();
            this.analysisSetReviewActions.DisableAutoAdvance();
            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickCancelOnFinishPopup();
            this.analysisSetReviewActions.IsAtReviewPage();
        }
    }
}





