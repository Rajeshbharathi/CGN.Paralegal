using System;
using System.Configuration;
using System.Globalization;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CGN.Paralegal.UI.Tests
{
    public class CreateQcSetActions
    {
        /// <summary>
        /// Click create button to open Create qcset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickCreateButtonToOpenCreateQcSetPopup(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("trainingSetTab"));
            CommonActions.WaitForLoadPage(driver);
            CommonActions.ClickElement(driver, By.Id("btnCreateQCSet"));
            CommonActions.WaitForLoadPage(driver);
        }

        /// <summary>
        /// Check qcset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void CheckQcSetPopup(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//div/h3[contains(text(),'Create Verification Set')]"));
        }

        public static void CheckQcPredictionErrorMessage(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.CssSelector(".text-danger.ng-binding"));
        }

        public static void CheckQcPredictionErrorMessageDisabled(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.CssSelector(".text-danger.ng-binding.ng-hide"));
        }
        /// <summary>
        /// Check create button in qcset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void CheckCreateButtonDisabled(IWebDriver driver)
        {
            CommonActions.ElementShouldBeDisabled(driver, By.Id("confirmBtn"));
        }
        /// <summary>
        /// Fill qcset name
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "OpenQA.Selenium.IWebElement.SendKeys(System.String)")]
        public static void EnterQcSetName(IWebDriver driver)
        {
            CommonActions.EnterTextInTextBox(driver, By.XPath("//input[@id='qcSetName']"),Resources.Qcset.QcsetName);
        }
        
        /// <summary>
        /// Select statistical sample in sampling options
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void SelectStatisticalSampleInSamplingOptions(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//input[@id='StatisticalSample']"));
        }

        /// <summary>
        /// Select fixedsize in sampling options
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void SelectFixedSizeInSamplingOptions(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//input[@id='FixedSize']"));
            CommonActions.EnterTextInTextBox(driver, By.XPath("//input[@id='qcSetSize']"), Resources.Qcset.SampleSize);
        }

        /// <summary>
        /// Click create button to create qcset
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickCreateButtonInCreateQcSetPopup(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//div[@class='modal-footer']//button[contains(text(),'Create')]"));
            CommonActions.WaitForLoadPage(driver);
        }

        /// <summary>
        /// Click Cancel in create qcset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickCancelButtonInCreateQcSetPopup(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//div[@class='modal-footer']//button[contains(text(),'Cancel')]"));
        }


        /// <summary>
        /// Check Next status after create qcset
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyQcSetIsCreated(IWebDriver driver)
        {
            CommonActions.ElementShouldBeEnabled(driver, By.Id("verificationSetTab"));
        }

        /// <summary>
        /// Check Next status after clicking cancel in create qcset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyCreateQcSetIsEnabled(IWebDriver driver)
        {
            CommonActions.ElementShouldBeEnabled(driver, By.Id("btnCreateQCSet"));
        }

        /// <summary>
        /// Check confidence level default selected values
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void CheckConfidenceLevelDefaultSelection(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//span[@aria-owns='qcSetConfidenceSelect_listbox']//span//span[contains(text(),'95')]"));
        }

         /// <summary>
       /// Check margin of error default selected values
       /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void CheckMarginOfErrorDefaultSelection(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.XPath("//span[@aria-owns='qcSetErrorMarginSelect_listbox']//span//span[contains(text(),'2.5')]"));
       }


       /// <summary>
       /// Check sample size default values
       /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void CheckSampleSizeDefaultValue(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.XPath("//b[contains(text(),'1,537')]"));
       }
          
    }
}
