using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Protractor;
using System;
using System.Configuration;
using System.Globalization;

namespace CGN.Paralegal.UI.Tests
{
    using System.Diagnostics;
    using System.IO;

    [TestClass]
    public class TestBase
    {
        static protected IWebDriver driver;
        static protected IWebDriver browser;
        // Use method to initialize drivers
        // driver - Selenium webdriver
        // browser- Selenium webdriver wrapped with protractor - for angular files
        public static void TestBaseInitialize()
        {
            browser = new ChromeDriver(ConfigurationManager.AppSettings["ChromeDriverPath"]);
            driver = new NgWebDriver(browser);
            var timeout = Convert.ToInt32(ConfigurationManager.AppSettings["DriverScriptTimeout"], CultureInfo.InvariantCulture);
            driver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromSeconds(timeout));
            driver.Manage().Window.Maximize();
        }

        // Use method to cleanup drivers
        public static void TestBaseCleanup()
        {
            if (browser != null)
            {
                browser.Quit();
                browser.Dispose();
            }
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
            

        }
    }
}
