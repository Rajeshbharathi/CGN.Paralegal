using CGN.Paralegal.UI.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CGN.Paralegal.UI.Tests.PredictAll
{
    /// <summary>
    /// Web test for 'Predict All'
    /// </summary>
    [TestClass]
    public class PredictAllTests : TestBase
    {
        private const string WorkflowStatePredictAllNotCreated =
          @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        [TestInitialize]
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            TestHelper.UpdateWorkflowState(WorkflowStatePredictAllNotCreated);
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }

        //// To Do below web test cases need to be fixed once PredictAll tab implemented 
        ///// <summary>
        ///// This web test scenario verify predict all summary panel not to display details 
        ///// before the predictall is created
        ///// </summary>
        ///// 
        //[TestMethod]
        //[TestCategory("Predictive Coding")]
        //public void PRC_PredictAll_BeforePredictAllCreated_ShouldNotDisplayDetailsInPredictAllSummaryPanel()
        //{
        //    CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
        //    CommonActions.WaitForLoadPage(driver);
        //    PredictAllActions.IsPredictAllSummaryPanelExists(driver);
        //    PredictAllActions.VerifyDetailsNotDisplayedInPredictAllSummaryPanel(driver);
        //}

        ///// <summary>
        ///// This web test scenario verify predict all summary panel details are displayed 
        ///// after the predictall is created
        ///// </summary>
        ///// 
        //[TestMethod]
        //[TestCategory("Predictive Coding")]
        //public void PRC_PredictAll_AfterPredictAllCreated_ShouldDisplayDetailsInPredictAllSummaryPanel()
        //{
        //    CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
        //    CommonActions.WaitForLoadPage(driver);
        //    PredictAllActions.ClickOnCreateButton(driver);
        //    PredictAllActions.ClickOnConfirmButtonInPredictAllDocumentsPopup(driver);
        //    PredictAllActions.IsPredictAllSummaryPanelExists(driver);
        //    PredictAllActions.VerifyDetailsDisplayedInPredictAllSummaryPanel(driver);
        //}



    }
}

