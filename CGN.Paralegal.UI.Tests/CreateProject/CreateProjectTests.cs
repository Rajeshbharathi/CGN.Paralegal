using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CGN.Paralegal.UI.Tests
{
    using CGN.Paralegal.UI.Tests.Common;


    /// <summary>
    /// Web test for 'Create Project'
    /// </summary>
    [TestClass]
    public class CreateProjectTests : TestBase
    {

        private const string workflowStateProjectNotCreated =
            @"[{""$id"":""1"",""Name"":""ProjectSetup"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":1},
{""$id"":""2"",""Name"":""ControlSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":2},
{""$id"":""3"",""Name"":""TrainingSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":3},
{""$id"":""4"",""Name"":""PredictSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":4},
{""$id"":""5"",""Name"":""QcSet"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":5},
{""$id"":""6"",""Name"":""Done"",""CreateStatus"":""NotStarted"",""ReviewStatus"":""NotStarted"",""IsCurrent"":false,""Order"":6}]";

        [TestInitialize]
        public void InitializeTestMethod()
        {
            TestBaseInitialize();
            TestHelper.UpdateWorkflowState(workflowStateProjectNotCreated);
        }

        [TestCleanup]
        public void CleanupTestMethod()
        {
            TestBaseCleanup();
        }

        /// <summary>
        /// This web test scenario tests create project by 
        /// clicking on the create button verifying project creation by Project dashboard navigation.
        /// </summary>
        /// 
        #region Happy Day Scenarios

        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_ProvideProjectInfoAndClickNext_ShouldGoToSelectDocumentsPage()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.TypeProjectInfo(driver);
            CreateProjectActions.ClickPrimaryButton(driver); //Click Next
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.ShouldDisdplaySelectDocumentsPage(driver);

        }

        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_DoNotProvideProjectInfoAndClickNext_ShouldDisdplayValidationError()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.ClickPrimaryButton(driver);
            CreateProjectActions.ShouldDisdplayValidationErrors(driver);
        }

        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_SelectAllDocuments_ShouldCreateProjectAndGotoDashboard()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.TypeProjectInfo(driver);
            CreateProjectActions.ClickPrimaryButton(driver); //Click Next
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.ClickCreateButton(driver); //Click Create

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateProjectActions.IsProjectStatusExists(driver).Should().BeTrue();
        }

        /// <summary>
        /// This web test scenario tests Confirm on create project by Tag 
        /// create project option and click OK on Confirm window then  
        /// verifying project creation by Project dashboard navigation.
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_SelectDocumentsByTag_ShouldCreateProjectAndGotoDashboard()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);        

            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.TypeProjectInfo(driver);
            CreateProjectActions.ClickPrimaryButton(driver); //Click Next
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.SelectDocumentsByTag(driver);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateProjectActions.IsProjectStatusExists(driver).Should().BeTrue();
        }

        /// <summary>
        /// This web test scenario tests Confirm on create project by Saved Search
        /// create project option and click OK on Confirm window then  
        /// verifying project creation by Project dashboard navigation.
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_SelectDocumentsBySavedSearch_ShouldCreateProjectAndGotoDashboard()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.TypeProjectInfo(driver);
            CreateProjectActions.ClickPrimaryButton(driver); //Click Next
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.SelectDocumentsBySavedSearch(driver);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateProjectActions.IsProjectStatusExists(driver).Should().BeTrue();
        }
        /// <summary>
        /// This web test scenario tests Confirm on create project by New Search 
        /// create project option and click OK on Confirm window then  
        /// verifying project creation by Project dashboard navigation.
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_SelectDocumentsByNewSearch_ShouldCreateProjectAndGotoDashboard()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.TypeProjectInfo(driver);
            CreateProjectActions.ClickPrimaryButton(driver); //Click Next
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.SelectDocumentsByNewSearch(driver);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateProjectActions.IsProjectStatusExists(driver).Should().BeTrue();
        }

    
        #endregion

        /// <summary>
        /// This web test scenario tests Cancel button
        /// verifying redirect to admin dashboard
        /// </summary>
        [TestMethod]
        [TestCategory("Predictive Coding")]
        public void PRC_CreateProject_ClickOnCancelButton_ShouldRedirectToAdminDashboard()
        {
            CommonActions.NavigateToUrl(driver, Settings.PCCreateProjectUrl);
            CommonActions.WaitForLoadPage(driver);

            CreateProjectActions.clickCancelBtn(driver);

            CommonActions.WaitForLoadPcDashboardPage(driver);

            CreateProjectActions.IsErrorBlockExists(driver).Should().BeTrue();

        }
    }
}