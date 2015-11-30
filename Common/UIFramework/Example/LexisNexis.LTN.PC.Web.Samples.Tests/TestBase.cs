using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support;
using Protractor;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.QualityTools.UnitTestFramework;

namespace LexisNexis.LTN.PC.Web.Samples.Tests
{
    [TestClass]
    public class TestBase
    {
        static protected IWebDriver driver;
        static protected IWebDriver browser;
        

        // Use method to initialise drivers
        // driver - Selenium webdriver
        // browser- Selenium webdriver wrapped with protractor - for angular files
        public static void TestBaseInitialize()
        {
            driver = new ChromeDriver(ConfigurationManager.AppSettings["ChromeDriverPath"]);
            driver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromSeconds(30));
            browser = new NgWebDriver(driver);
        }

        // Use method to cleanup drivers
        public static void TestBaseCleanup()
        {
            browser.Quit();
            driver.Quit();
           
        }

    }
}
