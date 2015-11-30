using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CGN.Paralegal.UI.Tests.Common;

namespace CGN.Paralegal.UI.Tests
{
    [TestClass]
    public class CreateControlsetTests : TestBase
    {


        private const string workflowStateControlSetNotCreated =
           @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";


        [TestInitialize] 
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            TestHelper.UpdateWorkflowState(workflowStateControlSetNotCreated);          
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }



        /// <summary>
        /// This web test scenario tests default values of margin of error
        /// /confidence level/sample size values when Controlset Popup is open
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateControlSet_VerifyDefaultSampleSizeCalculationInCreateControlSetPopup_ShouldPass()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateControlsetActions.ClickStartButtonToOpenCreateControlsetPopup(driver);

            CreateControlsetActions.CheckControlSetPopup(driver);

            CreateControlsetActions.CheckConfidenceLevelDefaultSelection(driver);

            CreateControlsetActions.CheckMarginOfErrorDefaultSelection(driver);

            CreateControlsetActions.CheckSampleSizeDefaultValue(driver);

            CreateControlsetActions.SwitchToMainWindow(driver);
        }


        /// <summary>
        /// This web test scenario tests Sample size value for different combination
        /// of margin of error/confidence level in Controlset Popup
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateControlSet_VerifyNonDefaultSampleSizeCalculationInCreateControlSetPopup_ShouldPass()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateControlsetActions.ClickStartButtonToOpenCreateControlsetPopup(driver);
           
            CreateControlsetActions.CheckControlSetPopup(driver);

            CreateControlsetActions.ChangeConfidenceLevelSelection(driver);

            CreateControlsetActions.ChangeMarginOfErrorSelection(driver);

            CreateControlsetActions.CheckSampleSizeReCalculatedValue(driver);

            CreateControlsetActions.SwitchToMainWindow(driver);
        }

        /// <summary>
        /// This web test scenario tests create controlset and verify  
        /// project dashboard show as correct next sate of 'Review Control Set'
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateControlSet_ClickOkInCreateControlSetPopUp_ShouldCreateControlSet()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateControlsetActions.ClickStartButtonToOpenCreateControlsetPopup(driver);

            CommonActions.WaitForLoadPage(driver);

            CreateControlsetActions.CheckControlSetPopup(driver);

            CreateControlsetActions.ClickOkButtonToCreateControlset(driver);
            
            CommonActions.WaitForLoadPage(driver);

            CommonActions.RefreshMainWindow(driver);

            CreateControlsetActions.CheckNextStatusIsReviewControlset(driver);

            CreateControlsetActions.SwitchToMainWindow(driver);
        }

        /// <summary>
        /// This web test scenario tests cancel create controlset  and verify  
        /// project dashboard show as correct sate of 'Create Control Set'
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateControlSet_ClickCancelInCreateControlSetPopUp_ShouldNotCreateControlSet()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateControlsetActions.ClickStartButtonToOpenCreateControlsetPopup(driver);

            CreateControlsetActions.CheckControlSetPopup(driver);

            CreateControlsetActions.ClickCancelButtonInCreateControlsetPopup(driver);

            CreateControlsetActions.CheckStatusForCreateControlset(driver);

            CreateControlsetActions.SwitchToMainWindow(driver);
        }

        //TODO: Need to add a cross test to verify the controlset creation from the review dashboard as part of sprint 3


    }
}
