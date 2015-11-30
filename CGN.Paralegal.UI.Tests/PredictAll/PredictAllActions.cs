namespace CGN.Paralegal.UI.Tests.PredictAll
{
    using FluentAssertions;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;
    using Protractor;
    using System;
    public class PredictAllActions
    {
        /*
        /// <summary>
        /// Verify predict all summary panel exists
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void IsPredictAllSummaryPanelExists(IWebDriver driver)
        {
            WebDriverWait _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            IWebElement predictAllSummaryPanel = _wait.Until(d => d.FindElement(By.XPath("//div/h4[contains(text(),'Predict All Summary')]")));
            predictAllSummaryPanel.Should().NotBeNull();
        }

        /// <summary>
        /// Verify summary details are not displayed in predict all summary panel 
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void VerifyDetailsNotDisplayedInPredictAllSummaryPanel(IWebDriver driver)
        {
            WebDriverWait _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            IWebElement predictAll = _wait.Until(d => d.FindElement(By.XPath("//div[@ng-if='!ProjectDashboardController.isPredictAllCompleted()']")));
            predictAll.Should().NotBeNull();
        }

        /// <summary>
        /// Verify summary details are displayed in predict all summary panel 
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void VerifyDetailsDisplayedInPredictAllSummaryPanel(IWebDriver driver)
        {
            WebDriverWait _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            IWebElement predictAll = _wait.Until(d => d.FindElement(By.XPath("//div[@ng-if='ProjectDashboardController.isPredictAllCompleted()']")));
            predictAll.Should().NotBeNull();
        }

        /// <summary>
        /// Click on create button to create predict all  
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void ClickOnCreateButton(IWebDriver driver)
        {
            WebDriverWait _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            IWebElement createButton = _wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Create')]")));
            createButton.Should().NotBeNull();
            createButton.Click();
        }

        /// <summary>
        /// Click on confirm button in predict all documents popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void ClickOnConfirmButtonInPredictAllDocumentsPopup(IWebDriver driver)
        {
            WebDriverWait _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            IWebElement ConfirmButton = _wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Confirm')]")));
            ConfirmButton.Should().NotBeNull();
            ConfirmButton.Click();
        }
          */
    }
}



