using System;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CGN.Paralegal.UI.Tests
{
    public class CommonActions
    {
        public const string TrainingTabId = "trainingSetTab";
        public const string VerificationTabId = "verificationSetTab";
        public static int resilienceCount = Settings.ResilienceCount;
        /// <summary>
        /// Navigate to the specific URL
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="url">relative/partial URL</param>
        public static void NavigateToUrl(IWebDriver driver, string url)
        {
            driver.Navigate().GoToUrl(Settings.BaseUrl + url);
        }

        /// <summary>
        /// Refresh Main window
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void RefreshMainWindow(IWebDriver driver)
        {
            driver.Navigate().Refresh();
        }


        public static void ClickTab(IWebDriver driver, string tabId)
        {
            ClickElement(driver, By.Id(tabId));
        }
        /// <summary>
        /// Wait for load dashboard page
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void WaitForLoadPcDashboardPage(IWebDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            wait.Until(ExpectedConditions.ElementExists(By.Id("dashboard")));
            WaitForLoadPage(driver);
        }

        /// <summary>
        /// Wait for load dashboard page
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        public static void ElementShouldNotBeNull(IWebDriver driver, By query, Boolean resetResilience = true)
        {   
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                var ele = wait.Until(ExpectedConditions.ElementExists(query));
                ele.Should().NotBeNull();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    ElementShouldNotBeNull(driver, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
           
        }

        /// <summary>
        /// Click element
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        public static void ClickElement(IWebDriver driver, By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                wait.Until(d => d.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => d.FindElement(query).Enabled).Should().BeTrue();
                var element = wait.Until(ExpectedConditions.ElementExists(query));
                element.Should().NotBeNull();
                element.Click();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    ClickElement(driver, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }

        }

        /// <summary>
        /// Click element in popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="modal">modal</param>
        /// <param name="query">element name</param>
        /// <param name="resetResilience">reset resilience</param>
        public static void ClickElementInPopup(IWebDriver driver, By modal,By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                var dialog = wait.Until(ExpectedConditions.ElementExists(modal));
                dialog.Should().NotBeNull();

                wait.Until(d => dialog.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => dialog.FindElement(query).Enabled).Should().BeTrue();
                var ele = wait.Until(d=> dialog.FindElement(query));
                ele.Should().NotBeNull();
                ele.Click();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    ClickElementInPopup(driver, modal, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }

        }

        /// <summary>
        /// Check popup
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="modal">modal</param>
        /// <param name="query">element name</param>
        /// <param name="resetResilience">reset resilience</param>
        public static void CheckPopup(IWebDriver driver, By modal, By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                var dialog = wait.Until(ExpectedConditions.ElementExists(modal));
                dialog.Should().NotBeNull();
                var ele = wait.Until(d => dialog.FindElement(query));
                ele.Should().NotBeNull();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    CheckPopup(driver, modal, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }

        }

        /// <summary>
        /// Check Pop up header and click element
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="modal">modal</param>
        /// <param name="header">header</param>
        /// <param name="query">element name</param>
        /// <param name="resetResilience">reset resilience</param>
        public static void CheckHeaderAndClickElementInPopup(IWebDriver driver, By modal,By header, By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                var dialog = wait.Until(ExpectedConditions.ElementExists(modal));
                dialog.Should().NotBeNull();
                var headerElement = wait.Until(d=> dialog.FindElement(header));
                headerElement.Should().NotBeNull();

                wait.Until(d => dialog.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => dialog.FindElement(query).Enabled).Should().BeTrue();
                var ele = wait.Until(d => dialog.FindElement(query));
                ele.Should().NotBeNull();
                ele.Click();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    CheckHeaderAndClickElementInPopup(driver, modal, header,query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }

        }
        /// <summary>
        /// Check Parent and click element
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="modal">modal</param>
        /// <param name="header">header</param>
        /// <param name="query">element name</param>
        /// <param name="resetResilience">reset resilience</param>
        public static void CheckParentAndClickElement(IWebDriver driver, By query,By queryin, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                wait.Until(d => d.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => d.FindElement(query).Enabled).Should().BeTrue();
                var ele = wait.Until(ExpectedConditions.ElementExists(query));
                ele.Should().NotBeNull();

                wait.Until(d => ele.FindElement(queryin).Displayed).Should().BeTrue();
                wait.Until(d => ele.FindElement(queryin).Enabled).Should().BeTrue();
                var element = wait.Until(d => ele.FindElement(queryin));
                element.Should().NotBeNull();
                element.Click();
               
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    CheckParentAndClickElement(driver,query,queryin,false);  
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }
        
        /// <summary>
        ///  Verify the element visible and click element
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        public static void CheckVisibleAndClickElement(IWebDriver driver, By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                wait.Until(d => d.FindElement(query).Enabled).Should().BeTrue();
                wait.Until(d => d.FindElement(query).Displayed).Should().BeTrue();
                var ele = wait.Until(ExpectedConditions.ElementExists(query));
                ele.Should().NotBeNull();
                ele.Click();
            }
           catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    CheckVisibleAndClickElement(driver, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }

        /// <summary>
        ///  Verify the element visible and enabled
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        public static void ElementShouldBeEnabled(IWebDriver driver, By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                wait.Until(d => d.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => d.FindElement(query).Enabled).Should().BeTrue();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    ElementShouldBeEnabled(driver, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }

        /// <summary>
        ///  Verify the element visible and enabled
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        /// <param name="text">element text to match</param>
        public static void CheckElementEnabledAndText(IWebDriver driver, By query, string text, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if(resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                wait.Until(d => d.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => d.FindElement(query).Enabled).Should().BeTrue();
                var ele = wait.Until(ExpectedConditions.ElementExists(query));
                ele.Should().NotBeNull();
                ele.Text.Should().Contain(text);
            }
            catch(Exception ex)
            {
                resilienceCount--;
                if(resilienceCount > 0)
                {
                    CheckElementEnabledAndText(driver, query, text, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }

        /// <summary>
        ///  Verify the element visible and disabled
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        public static void ElementShouldBeDisabled(IWebDriver driver, By query, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                wait.Until(d => d.FindElement(query).Displayed).Should().BeTrue();
                wait.Until(d => d.FindElement(query).Enabled).Should().BeFalse();
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    ElementShouldBeDisabled(driver, query, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }

        /// <summary>
        ///  Enter text in textbox
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        /// <param name="query">element name</param>
        /// <param name="text">Text</param>
        public static void EnterTextInTextBox(IWebDriver driver, By query, string text, Boolean resetResilience = true)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            if (resetResilience)
            {
                resilienceCount = Settings.ResilienceCount;
            }
            try
            {
                var ele = wait.Until(ExpectedConditions.ElementExists(query));
                ele.Should().NotBeNull();
                ele.SendKeys(text);
            }
            catch (Exception ex)
            {
                resilienceCount--;
                if (resilienceCount > 0)
                {
                    EnterTextInTextBox(driver, query, text, false);
                }
                else
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }

       
        /// <summary>
        /// Wait for page load
        /// </summary>
        /// <param name="driver"></param>
        public static void WaitForLoadPage(IWebDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            wait.Until(d =>
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
        /// Load after sec
        /// </summary>
        public static void WaitForElement(IWebDriver driver)
        {
            driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(Settings.ImplicitWait));
        }

    }
}
