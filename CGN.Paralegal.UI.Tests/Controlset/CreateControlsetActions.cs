using System;
using System.Configuration;
using System.Globalization;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CGN.Paralegal.UI.Tests
{
    public class CreateControlsetActions
    {               
        /// <summary>
       /// Switch to Main window
       /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void SwitchToMainWindow(IWebDriver driver)
        {
            driver.SwitchTo().DefaultContent();
        }

        /// <summary>
        /// Click Create control set button to open Create controlset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickStartButtonToOpenCreateControlsetPopup(IWebDriver driver)
        {
            CommonActions.ClickElement(driver,By.Id("btnCreate1"));
            
        }

        /// <summary>
        /// Check controlset popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void CheckControlSetPopup(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("modalDialog"));
           
        }

        /// <summary>
        /// Check confidence level default selected values
        /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void CheckConfidenceLevelDefaultSelection(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.XPath("//input[@id='selConfidence'][@value='95']"));
           
       }

       /// <summary>
       /// Check margin of error default selected values
       /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void CheckMarginOfErrorDefaultSelection(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.XPath("//input[@id='selMarginOfError'][@value='2.5']"));
           
       }


       /// <summary>
       /// Check sample size default values
       /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void CheckSampleSizeDefaultValue(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.XPath("//b[contains(text(),'1,537')]"));
          
       }

       /// <summary>
       /// Change confidence level values
       /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void ChangeConfidenceLevelSelection(IWebDriver driver)
       {
            CommonActions.ClickElement(driver, By.XPath("//input[@id='selConfidence'][@value='99']"));
       }

       /// <summary>
       /// Change margin of error values
       /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void ChangeMarginOfErrorSelection(IWebDriver driver)
       {  
           CommonActions.ClickElement(driver, By.XPath("//input[@id='selMarginOfError'][@value='5']"));
       }

        /// <summary>
        ///  check Sample size re calculated value
        /// </summary>
       /// <param name="driver">IWebDriver</param>
        public static void CheckSampleSizeReCalculatedValue(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//b[contains(text(),'1,537')]"));
        }

        /// <summary>
        /// Click OK button to create controlset
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickOkButtonToCreateControlset(IWebDriver driver)
       {  
           CommonActions.ClickElement(driver, By.Id("confirmBtn"));

       }

        /// <summary>
        /// Click Cancel for create controlset
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickCancelButtonInCreateControlsetPopup(IWebDriver driver)
        {
            
            CommonActions.ClickElement(driver, By.XPath("//button[contains(text(),'Cancel')]"));
            
        }

       /// <summary>
       /// Check Next status after create controlset
       /// </summary>
        /// <param name="driver">IWebDriver</param>
       public static void CheckNextStatusIsReviewControlset(IWebDriver driver)
       {
           CommonActions.CheckElementEnabledAndText(driver, By.Id("controlsetStatus"), CGN.Paralegal.UI.App.resources.Project.ControlSetReviewNotStarted);
          
       }


       /// <summary>
       /// Check create "controlset buttton" is enabled 
       /// </summary>
       /// <param name="driver">IWebDriver</param>
       [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "CGN.Paralegal.UI.Tests.CommonActions.CheckElementEnabledAndText(OpenQA.Selenium.IWebDriver,OpenQA.Selenium.By,System.String)")]
        public static void CheckStatusForCreateControlset(IWebDriver driver)
       {
           CommonActions.CheckElementEnabledAndText(driver, By.Id("controlsetStatus"), CGN.Paralegal.UI.App.resources.Project.NoControlSet);
          
       }
    }
}
