using System;
using System.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support;
using Protractor;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CGN.Paralegal.UI.Tests.ProjectDashboard
{
    using CGN.Paralegal.UI.Tests.Common;
    using CGN.Paralegal.UI.Tests.Trainingset;   

    [TestClass]
    public class ProjectDashboardTests : TestBase
    {
        private AnalysisSetReviewActions analysisSetReviewActions;

        private const string workflowStateProjectCreated =
    @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string workflowStateControSetReviewNotStarted =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string workflowStateTrainingSetReviewInProgress =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""InProgress"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        private const string workflowStateTrainingSetCompleted =
   @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
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
            this.analysisSetReviewActions = null;
           TestBaseCleanup();
        }
    
              
        /// <summary>
        /// This web test scenario tests confirms the initial state of 
        /// control set summary tab.
        /// </summary>
        [TestMethod]
        [TestCategory("Project Dashboard")]
        public void PRC_ProjectDashboard_AfterControlSetReviewCompleted_ShouldDisplayControlSetSummary()
        {
            TestHelper.UpdateWorkflowState(workflowStateControSetReviewNotStarted);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.ClickStartReviewButton();
            this.analysisSetReviewActions.WaitForLoadPage();
            this.analysisSetReviewActions.DisableAutoAdvance();

            this.analysisSetReviewActions.ReviewAllDocumentsToDisplayFinishPopup();
            this.analysisSetReviewActions.VerifyFinishPopup();

            this.analysisSetReviewActions.ClickOkOnFinishPopup();
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            ProjectDashBoardActions.VerifyControlSetSummary(driver);
        }

     

       /// <summary>
        ///  This web test scenario tests review all qc documents and click finish in qcset complete popup
        /// should redirect to reviewer dashboard
        /// </summary>
        [TestMethod]
        public void PRC_ProjectDashboard_ChangeQCToTraining_ShouldReturnToTrainingState()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);

            ProjectDashBoardActions.GoBackToTraining(driver);
            CommonActions.WaitForLoadPage(driver);
            CommonActions.WaitForLoadPcDashboardPage(driver);
            
            ProjectDashBoardActions.VerifyTrainingState(driver);
        }

        /// <summary>
        /// This web test scenario Add addtional documents to project by selecting all documents
        /// </summary>
        [TestMethod]
        [TestCategory("Project Dashboard")]
        public void PRC_ProjectDashboard_AddDocumentsToProjectByAll_ShouldDisplayUpdatedTotalProjectDocumentsCount()
        {
            TestHelper.UpdateWorkflowState(workflowStateProjectCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            ProjectDashBoardActions.SelectAddDocuments(driver);

            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.ClickCreateButton(driver); //Click Create

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.VerifyTotalProjectDocumentsCount(driver);
          
        }

        /// <summary>
        /// This web test scenario Add addtional documents to project by tag
        /// </summary>
        [TestMethod]
        [TestCategory("Project Dashboard")]
        public void PRC_ProjectDashboard_AddDocumentsToProjectByTag_ShouldDisplayUpdatedTotalProjectDocumentsCount()
        {
            TestHelper.UpdateWorkflowState(workflowStateProjectCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.SelectAddDocuments(driver);

            CommonActions.WaitForLoadPage(driver);
            
            CreateProjectActions.SelectDocumentsByTag(driver);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.VerifyTotalProjectDocumentsCount(driver);

        }

        /// <summary>
        /// This web test scenario Add addtional documents to project by saved search
        /// </summary>
        [TestMethod]
        [TestCategory("Project Dashboard")]
        public void PRC_ProjectDashboard_AddDocumentsToProjectBySavedSearch_ShouldDisplayUpdatedTotalProjectDocumentsCount()
        {
            TestHelper.UpdateWorkflowState(workflowStateProjectCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.SelectAddDocuments(driver);

            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.SelectDocumentsBySavedSearch(driver);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.VerifyTotalProjectDocumentsCount(driver);

        }


        /// <summary>
        /// This web test scenario Add addtional documents to project by new search
        /// </summary>
        [TestMethod]
        [TestCategory("Project Dashboard")]
        public void PRC_ProjectDashboard_AddDocumentsToProjectByNewSearch_ShouldDisplayUpdatedTotalProjectDocumentsCount()
        {
            TestHelper.UpdateWorkflowState(workflowStateProjectCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.SelectAddDocuments(driver);

            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.SelectDocumentsByNewSearch(driver);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.VerifyTotalProjectDocumentsCount(driver);

        }

        /// <summary>
        /// This web test scenario tests Cancel button
        /// verifying redirect to admin dashboard
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_ProjectDashboard_AddDocumentsToProjectClickOnCancelButton_ShouldRedirectToAdminDashboard()
        {
            TestHelper.UpdateWorkflowState(workflowStateProjectCreated);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.SelectAddDocuments(driver);

            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.clickCancelBtn(driver);

            CommonActions.WaitForLoadPage(driver);

            ProjectDashBoardActions.VerifyTotalProjectDocumentsCountAfterCancel(driver);

        }

        /// <summary>
        ///  This web test scenario tests review all qc documents and click finish in qcset complete popup
        /// should redirect to reviewer dashboard
        /// </summary>
        [TestMethod]
        public void PRC_ProjectDashboard_ViewAllDocuments_ShouldShowAllDocsInDocList()
        {
            TestHelper.UpdateWorkflowState(WorkflowStateQcSetReviewCompleted);

            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);

            ProjectDashBoardActions.ViewAllDocuments(driver);
            CommonActions.WaitForLoadPage(driver);

            analysisSetReviewActions.IsAtDocListPage();

            analysisSetReviewActions.VerifyAllDocuments();
        }

    }
}


