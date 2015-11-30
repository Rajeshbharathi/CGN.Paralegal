using System.Globalization;

namespace CGN.Paralegal.UI.Tests.ControlSet
{
    using System;
    using System.Resources;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;
    using CGN.Paralegal.UI.Tests.Resources;
    

    class ControlSetDashboardActions
    {
        protected readonly WebDriverWait _wait;
        protected IWebDriver _driver;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="driver"></param>
        public ControlSetDashboardActions(IWebDriver driver)
        {
            this._driver = driver;
            this._wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
        }

        /// <summary>
        /// Verify that page is on control set not created state
        /// </summary>
        internal void VerifyControlSetNotCreatedState()
        {
            CommonActions.CheckElementEnabledAndText(this._driver, By.Id("controlsetStatus"), CGN.Paralegal.UI.App.resources.Project.NoControlSet);
        }

        /// <summary>
        /// Verify that page is at ready to start state
        /// </summary>
        internal void VerifyControlsetCreatedActionStatus()
        {
            CommonActions.CheckElementEnabledAndText(this._driver, By.Id("controlsetStatus"), CGN.Paralegal.UI.App.resources.Project.ControlSetReviewNotStarted);
        }

        /// <summary>
        /// Verify that page is at review in progress state
        /// </summary>
        internal void VerifyInProgressState()
        {
            CommonActions.CheckElementEnabledAndText(this._driver, By.Id("controlsetStatus"), CGN.Paralegal.UI.App.resources.Project.ControlSetReviewInProgress);

        }

        /// <summary>
        /// Verify that page is at review completed state
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "CGN.Paralegal.UI.Tests.CommonActions.CheckElementEnabledAndText(OpenQA.Selenium.IWebDriver,OpenQA.Selenium.By,System.String)")]
        internal void VerifyCompletedState()
        {
            CommonActions.CheckElementEnabledAndText(this._driver, By.Id("controlsetStatus"),CGN.Paralegal.UI.App.resources.Project.ControlSetReviewCompleted);

        }

        internal void VerifyCreateTrainingSet()
        {
            CommonActions.ElementShouldBeEnabled(this._driver, By.Id("btnCreateTrainingSet"));

        }

        /// <summary>
        /// Verify that current page is project dashboard page
        /// </summary>
        internal void IsAtProjectDashboardPage()
        {
            WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.Id("totalDocumentCount"));
        }


        /// <summary>
        /// Wait for load angular page
        /// </summary>
        internal void WaitForAngularShellPageLoad()
        {
            this._wait.Until(d =>
            {
                try
                {
                    var element = d.FindElement(By.Id("loading-bar-spinner"));
                    return !element.Displayed;
                }
                catch (Exception)
                {

                    return true;
                }
            });
        }


        /// <summary>
        /// Wait for load project
        /// </summary>
        internal void WaitForLoadPage()
        {
            this._wait.Until(d =>
            {
                try
                {
                    var element = d.FindElement(By.CssSelector(".k-loading-image"));
                    return !element.Displayed;
                }
                catch (Exception)
                {

                    return true;
                }
            });
        }

        /// <summary>
        /// Click on controlset tab 
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal void ClickOnControlSetTab(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(this._driver, By.XPath("//ul//li//a[contains(text(),'Control Set')][@ng-click='select()']"));
            CommonActions.ClickElement(driver, By.XPath("//ul//li//a[contains(text(),'Control Set')][@ng-click='select()']"));

        }

        /// <summary>
        /// verify prediction discrepancies widget
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal void VerifyPredictionDiscrepanciesWidget(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(this._driver, By.XPath("//div[@ng-if='ProjectDashboardController.showDiscrepancies()']"));
        }





    }

}