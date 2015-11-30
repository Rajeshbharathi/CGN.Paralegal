using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace CGN.Paralegal.UI.Tests
{
    /// <summary>
    /// Create Project -E2E user actions for test 'Create Project'
    /// </summary>
    public class DocListActions
    {

        #region Variables

        private const string ExportVal = "ReviewSetController.export()";

        protected readonly WebDriverWait _wait;
        protected IWebDriver _driver;
       
        #endregion Variables


        public DocListActions(IWebDriver driver)
        {
            
        }

        /// <summary>
        /// Check Doc Review Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool IsShouldOpenDocReviewAndDocList(IWebDriver driver)
        {
            try
            {
                CommonActions.ElementShouldNotBeNull(driver, By.XPath("//a[text()='View Set']"));
               
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static void DisableAutoAdvance(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("autoAdvance"));
            CommonActions.ClickElement(driver, By.Id("autoAdvance"));
            
        }


        /// <summary>
        /// Check Doc Review Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool ShouldDoNavigationBtwDocsInDocReview(IWebDriver driver)
        {
            try
            {
                var element = driver.FindElement(By.XPath("//a[text()='View Set']"));
                return element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Check Doc List Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool ShouldDisplaySelectedColumnsInDocList(IWebDriver driver)
        {
            try
            {
                WaitForKendoRefresh(driver);
                
                var element = driver.FindElement(By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
                return element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Check Doc List Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool ShouldDisplaySearchedResultsInDocList(IWebDriver driver)
        {
            try
            {
                WaitForKendoRefresh(driver);
                
                var element = driver.FindElement(By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
                return element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    
        /// <summary>
        /// Check Doc List Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool ShouldDisplaySortedFilteredResultsInDocList(IWebDriver driver)
        {
            try
            {
                WaitForKendoRefresh(driver);
                
                var element = driver.FindElement(By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
                return element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Check Doc List Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool ShouldExportCSV(IWebDriver driver)
        {
            try
            {

                var element = driver.FindElement(By.Id("btnExport"));
                return element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Check Doc List Exists
        /// </summary>
        /// <param name="driver"></param>        
        public static bool ShouldShowJobScheduleMessage(IWebDriver driver)
        {
            try
            {

                var element = driver.FindElement(By.Id("btnExport"));
                return element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
       
        /// <summary>
        /// Click Start Review
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        public static void ClickStartReviewToOpenDocReview(IWebDriver driver)
        {   
            CommonActions.ClickElement(driver, By.Id("btnCreate"));
            
        }
        
        /// <summary>
        /// Click next button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickNextDocButton(IWebDriver driver)
        {   
            CommonActions.ClickElement(driver, By.XPath("//span[@title='Next']"));
            
        }

        /// <summary>
        /// Click last button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickLastDocButton(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,By.XPath("//span[@title='Last']"));
        }

        /// <summary>
        /// Click previous button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickPreviousDocButton(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,By.XPath("//span[@title='Previous']"));
        }
        /// <summary>
        /// Click first doc button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickFirstDocButton(IWebDriver driver)
        {
           CommonActions.CheckVisibleAndClickElement(driver,By.XPath("//span[@title='First']"));
        }
        /// <summary>
        /// Click view set in doc review
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickViewSetInDocReview(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("viewSet"));
        }

        /// <summary>
        /// Click View Document in doc list
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickViewDocumentInDocList(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//a[contains(text(),'View Documents')]"));
        }
        /// <summary>
        /// Click Export button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickExportCsv(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver, By.Id("btnExport"));
        }
        /// <summary>
        /// Click project dashboard review
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickProjectDashboardReview(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,By.XPath("//a[contains(text(),'Project Dashboard')]"));
        }
        /// <summary>
        /// Click relevant doc list
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickRelevantDocList(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,By.Id("Relevant"));
        }
        /// <summary>
        /// Click not relevant
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickNotRelevantDocList(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,By.Id("Not_Relevant"));
        }
        /// <summary>
        /// Click not coded
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickNotCodedDocList(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,By.Id("Not_Coded"));
        }

        /// <summary>
        /// Click columns
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickColumnsDisabledAndEnabled(IWebDriver driver)
        {

            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
           CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li[@class='k-item k-columns-item k-state-default']//span[@class='k-sprite k-i-columns']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='0']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='1']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='2']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='3']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='4']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='5']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='0']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='1']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='2']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='3']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='4']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//li//span//input[@data-index='5']"));
            
        }
        /// <summary>
        /// Click filter search
        /// </summary>
        /// <param name="driver"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "OpenQA.Selenium.IWebElement.SendKeys(System.String)")]
        public static void ClickFilterSearch(IWebDriver driver)
        {
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//span[@class='k-sprite k-filter']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//span[@class='k-sprite k-filter']"));
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//span[@class='k-sprite k-filter']"));
            WaitForElementLoad(driver, By.XPath("//div//input[@class='k-textbox']"));
            CommonActions.EnterTextInTextBox(driver, By.XPath("//div//input[@class='k-textbox']"), Resources.Project.Coded);
            CommonActions.CheckVisibleAndClickElement(driver,
                By.XPath("//div//button[contains(text(),'Filter')]"));
        }
        /// <summary>
        /// Click filter clear
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickFilterClear(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
            WaitForKendoRefresh(driver);
            WaitForElementLoad(driver, By.XPath("//span[@class='k-sprite k-filter']"));
            CommonActions.ClickElement(driver, By.XPath("//span[@class='k-sprite k-filter']"));
            WaitForKendoRefresh(driver);
            WaitForElementLoad(driver, By.XPath("//div//button[contains(text(),'Clear')]"));
            CommonActions.ClickElement(driver,By.XPath("//div//button[contains(text(),'Clear')]"));
            WaitForKendoRefresh(driver);
        }
        /// <summary>
        /// Click sort ascending
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickSortAscending(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
            WaitForKendoRefresh(driver);
            WaitForElementLoad(driver, By.CssSelector(".k-column-menu .k-sort-asc>.k-link"));
            CommonActions.CheckVisibleAndClickElement(driver, By.CssSelector(".k-column-menu .k-sort-asc>.k-link"));
        }
        /// <summary>
        /// Click sort descending
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickSortDescending(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//th[@data-field='ReviewerCategory']//a//span[@class ='k-icon k-i-arrowhead-s']"));
            WaitForKendoRefresh(driver);
            WaitForElementLoad(driver, By.CssSelector(".k-column-menu .k-sort-asc>.k-link"));
            CommonActions.CheckVisibleAndClickElement(driver,By.CssSelector(".k-column-menu .k-sort-desc>.k-link"));
        }
        /// <summary>
        /// Click Search button in Doc list
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickSearchButton(IWebDriver driver)
        {
            CommonActions.EnterTextInTextBox(driver, By.XPath("//input[@type='text']"), Resources.Project.Relevant);
            CommonActions.ClickElement(driver, By.XPath("//input[@value='Search']"));
        }
        

        
         /// <summary>
        /// Wait for load angular page
        /// </summary>
        public static void WaitForElementLoad(IWebDriver driver, By by)
        {            
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            wait.Until(d =>
            {
                try
                {
                    var element = d.FindElement(by);
                    return element.Displayed;
                }
                catch (Exception)
                {

                    return true;
                }
            });
        }

        /// <summary>
        /// Wait for element to hide
        /// </summary>
        public static void WaitForElementHide(IWebDriver driver, By by)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            wait.Until(d =>
            {
                try
                {
                    var element = d.FindElement(by);
                    return !element.Displayed;
                }
                catch (Exception)
                {

                    return true;
                }
            });
        }
        /// <summary>
        /// Wait for kendo widgets to refresh
        /// </summary>
        public static void WaitForKendoRefresh(IWebDriver driver)
        {
            WaitForElementLoad(driver, By.CssSelector(".k-loading-image"));
            WaitForElementHide(driver, By.CssSelector(".k-loading-image"));
        }


    }
}
