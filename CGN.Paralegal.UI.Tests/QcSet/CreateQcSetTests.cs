using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CGN.Paralegal.UI.Tests.Common;
using CGN.Paralegal.UI.Tests.ProjectDashboard;

namespace CGN.Paralegal.UI.Tests
{
    [TestClass]
    public class CreateQcSetTests : TestBase
    {


        private const string workflowStateQCSetNotCreated =
           @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""Completed"",""ReviewStatus"":""Completed"",""IsCurrent"":true,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";


        [TestInitialize] 
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            TestHelper.UpdateWorkflowState(workflowStateQCSetNotCreated);          
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }




        /// <summary>
        /// This web test scenario tests create qcset by selecting the statistical sample in sampling options
        /// and verify project dashboard show as correct next sate of 'Review QC Set'
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateQcSet_SelectStatisticalSampleAndClickCreateInCreateQcSetPopUp_ShouldCreateQcSet()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateQcSetActions.ClickCreateButtonToOpenCreateQcSetPopup(driver);

            CreateQcSetActions.CheckQcSetPopup(driver);

            CreateQcSetActions.EnterQcSetName(driver);

            CreateQcSetActions.SelectStatisticalSampleInSamplingOptions(driver);

            CreateQcSetActions.ClickCreateButtonInCreateQcSetPopup(driver);

            CreateQcSetActions.VerifyQcSetIsCreated(driver);

        }


        /// <summary>
        /// This web test scenario tests create qcset by selecting the fixed size in sampling options and verify  
        /// project dashboard show as correct next sate of 'Review QC Set'
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateQcSet_SelectFixedSizeAndClickCreateInCreateQcSetPopUp_ShouldCreateQcSet()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateQcSetActions.ClickCreateButtonToOpenCreateQcSetPopup(driver);

            CreateQcSetActions.CheckQcSetPopup(driver);

            CreateQcSetActions.EnterQcSetName(driver);

            CreateQcSetActions.SelectFixedSizeInSamplingOptions(driver);

            CreateQcSetActions.ClickCreateButtonInCreateQcSetPopup(driver);

            CreateQcSetActions.VerifyQcSetIsCreated(driver); 

        }

        /// <summary>
        /// This web test scenario tests cancel create qcset  and verify  
        /// project dashboard show as correct sate of 'Create QC Set'
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateQcSet_ClickCancelInCreateQcSetPopUp_ShouldNotCreateQcSet()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateQcSetActions.ClickCreateButtonToOpenCreateQcSetPopup(driver);

            CreateQcSetActions.CheckQcSetPopup(driver);

            CreateQcSetActions.ClickCancelButtonInCreateQcSetPopup(driver);

            CreateQcSetActions.VerifyCreateQcSetIsEnabled(driver);
        }


        /// <summary>
        /// This web test scenario tests default values of confidence level/
        /// margin of error/sample size values when qcset popup is opened
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateQcSet_VerifyDefaultValuesInCreateQcSetPopup_ShouldPass()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateQcSetActions.ClickCreateButtonToOpenCreateQcSetPopup(driver);

            CreateQcSetActions.CheckQcSetPopup(driver);

            CreateQcSetActions.CheckConfidenceLevelDefaultSelection(driver);

            CreateQcSetActions.CheckMarginOfErrorDefaultSelection(driver);

            CreateQcSetActions.CheckSampleSizeDefaultValue(driver);

        }
        /// <summary>
        /// This web test scenario tests error message for Prediction categorize is not
        /// coded any docuemnts  when qcset popup is opened
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateQcSet_VerifyPredictionCategorizeErrorMessageDisplayedInCreateQcSetPopup_ShouldPass()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);
            //add documents to project here 
            ProjectDashBoardActions.SelectAddDocuments(driver);
            CommonActions.WaitForLoadPage(driver);
            CreateProjectActions.ClickCreateButton(driver); //Click Create
            CommonActions.WaitForLoadPage(driver);
            CreateQcSetActions.ClickCreateButtonToOpenCreateQcSetPopup(driver);
            CreateQcSetActions.CheckQcSetPopup(driver);
            CreateQcSetActions.CheckQcPredictionErrorMessage(driver);

        }

        /// <summary>
        /// This web test scenario tests error message for Prediction categorize is not
        /// coded any docuemnts  when qcset popup is opened
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateQcSet_VerifyPredictionCategorizeErrorMessageNotDisplayedInCreateQcSetPopup_ShouldPass()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCAdminDashboardUrl);
            CommonActions.WaitForLoadPcDashboardPage(driver);
            CreateQcSetActions.ClickCreateButtonToOpenCreateQcSetPopup(driver);
            CreateQcSetActions.CheckQcSetPopup(driver);
            CreateQcSetActions.CheckQcPredictionErrorMessageDisabled(driver);

        }



       



    }
}
