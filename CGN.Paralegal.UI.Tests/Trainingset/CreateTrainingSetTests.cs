using CGN.Paralegal.UI.Tests.Common;
using CGN.Paralegal.UI.Tests.ProjectDashboard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace CGN.Paralegal.UI.Tests.Trainingset
{
    using CGN.Paralegal.UI.Tests.Controlset;

    [TestClass]
    public class CreateTrainingSetTests : TestBase
    {
        private AnalysisSetReviewActions analysisSetReviewActions;

        private CreateTrainingSetActions createTrainingSetActions;

        const string workflowStateTrainingSetCreated =
    @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        const string workflowStateTrainingSetNotCreated =
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
            this.createTrainingSetActions = new CreateTrainingSetActions(driver);
            
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }

        /// <summary>
        /// Test clicking start training on finish control set review popup
        /// </summary>
        [TestMethod]
        public void PRC_CreateTrainingSet_ClickCreateTrainingSet_ShouldCreateTrainingSetAndGoToTrainingTab()
        {
            TestHelper.UpdateWorkflowState(workflowStateTrainingSetNotCreated);
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            analysisSetReviewActions.WaitForLoadPage();
            CommonActions.ClickElement(driver, By.Id("btnCreateTrainingSet"));

            analysisSetReviewActions.WaitForLoadPage();
            ProjectDashBoardActions.VerifyTrainingState(driver);

        }

        /// <summary>
        /// Test clicking start training review button in review dashboard
        /// </summary>
        [TestMethod]
        public void PRC_CreateTrainingSet_ClickStartReviewInReviewDasboard_ShouldCreateTrainingSetAndStartReviewRound1()
        {
            TestHelper.UpdateWorkflowState(workflowStateTrainingSetCreated);
            CommonActions.NavigateToUrl(driver, Settings.PCReviewDashboardUrl);

            analysisSetReviewActions.WaitForLoadPage();
            analysisSetReviewActions.ActivateTrainingTab();
            analysisSetReviewActions.ClickTrainingSetStartReviewButton();

            this.analysisSetReviewActions.WaitForLoadPage();
                  
            createTrainingSetActions.IsAtTrainingSetReviewPage();
        }

        
    }
}
