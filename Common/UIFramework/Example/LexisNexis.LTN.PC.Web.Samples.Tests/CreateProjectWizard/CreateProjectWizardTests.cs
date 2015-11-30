using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support;
using Protractor;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.QualityTools.UnitTestFramework;

namespace LexisNexis.LTN.PC.Web.Samples.Tests
{
    [TestClass]
    public class CreateProjectWizardTests : TestBase
    {
        private CreateProjectWizardActions projectWizard;

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void InitializeTestMethod() {
            TestBaseInitialize();
            projectWizard = new CreateProjectWizardActions();
            
        }

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void CleanupTestMethod() {
            TestBaseCleanup();
            projectWizard = null;
        }
        
        /* 
         * Sample Test Case 
         * Category descibes scenario - for grouping test cases
         * Naming : Scenario_State_Expectation
         * 
         */
        [TestMethod]
        [TestCategory("Create Project - Wizard")]
        public void PRC_CreateProject_ProjectList_ShouldDisplay()
        {
            Action loadProjectList = () => projectWizard.GoToProjectListPage(browser);
            loadProjectList.ShouldNotThrow<InvalidOperationException>("page should exist");

            IWebElement button = browser.FindElement(By.Id("addNewProject"));
            button.Should().NotBeNull("New Project button should be present");
            button.Displayed.Should().BeTrue("New Project button should be displayed");
            button.Enabled.Should().BeTrue("New Project button should be enabled");

            IWebElement grid = browser.FindElement(By.CssSelector(".k-grid"));
            grid.Should().NotBeNull("grid should be present");
            grid.Displayed.Should().BeTrue("grid should be displayed");

            grid.FindElements(By.CssSelector(".k-grid-content tr")).Count().Should().BeGreaterThan(0, "grid should display projects");

        }


        /* 
         * Sample Test Case for Boundary condition
         */
        [TestMethod]
        [TestCategory("Create Project - Wizard")]
        public void PRC_CreateProject_InvalidProjectSettingsOnNext_ShouldDisplayError()
        {
            projectWizard.GoToProjectListPage(browser);
            projectWizard.ClickNewProjectButton(browser);
            projectWizard.PopulateEmptyProjectName(browser);
            
            browser.FindElement(By.CssSelector("#projectName>.text-error")).Displayed.Should().BeTrue("Error message should be displayed");
            
            projectWizard.PopulateProjectNameWithOneChar(browser);
            
            browser.FindElement(By.CssSelector("#projectName>.text-error")).Displayed.Should().BeTrue("Error message should be displayed");

            projectWizard.PopulateProjectNameWithSpecialChar(browser);
            
            browser.FindElement(By.CssSelector("#projectName>.text-error")).Displayed.Should().BeTrue("Error message should be displayed");
        }

        /* 
         * Sample Test Case for Right condition
         */
        [TestMethod]
        [TestCategory("Create Project - Wizard")]
        public void PRC_CreateProject_ValidProjectSettingsOnNext_ShouldNavigateToControlSet()
        {
            projectWizard.GoToProjectListPage(browser);
            projectWizard.ClickNewProjectButton(browser);
            projectWizard.PopulateValidProjectSettings(browser);

            projectWizard.ProjectSettingsClickNext(browser);

            IWebElement tab = browser.FindElement(By.CssSelector(".tabs-left ul>li[heading='Settings']"));
            tab.GetAttribute("class").Split(' ').Should().NotContain("active","Project Settings Tab should be inactive");
            tab.Displayed.Should().BeTrue("Project Settings Tab should be visible");
            tab.Enabled.Should().BeTrue("Project Settings Tab should be clickable");

            IWebElement controlTab = browser.FindElement(By.CssSelector(".tabs-left ul>li[heading='Control']"));
            controlTab.GetAttribute("class").Split(' ').Should().Contain("active", "Control Tab should be active");
            controlTab.Displayed.Should().BeTrue("Control Tab should be displayed");
            controlTab.Enabled.Should().BeTrue("Control Tab should be clickable");
            IWebElement exampleTab = browser.FindElement(By.CssSelector(".tabs-left ul>li[heading='Examples']"));
            exampleTab.GetAttribute("class").Split(' ').Should().NotContain("active", "Examples Tab should be inactive");
            exampleTab.Displayed.Should().BeTrue("Examples Tab should be displayed");
            exampleTab.Enabled.Should().BeTrue("Examples Tab should be clickable");
            IWebElement summaryTab = browser.FindElement(By.CssSelector(".tabs-left ul>li[heading='Summary']"));
            summaryTab.GetAttribute("class").Split(' ').Should().NotContain("active", "Summary Tab should be inactive");
            summaryTab.Displayed.Should().BeTrue("Summary Tab should be displayed");
            summaryTab.Enabled.Should().BeTrue("Summary Tab should be clickable");

        }

        /*
         * Sample Test Case for Inverse Use case
         * Not Available at the moment
         */
        
        
        /*
         * Sample Test Case for Cross Use case
         * 
         */
        [TestMethod]
        [TestCategory("Create Project - Wizard")]
        public void PRC_CreateProject_NewProjectCreated_ShouldDisplayProjectInProjectList()
        {
            projectWizard.GoToProjectListPage(browser);
            projectWizard.ClickNewProjectButton(browser);
            projectWizard.PopulateValidProjectSettings(browser);
            projectWizard.ProjectSettingsClickNext(browser);
            projectWizard.PopulateValidControlSet(browser);
            projectWizard.ControlSetClickNext(browser);
            projectWizard.PopulateValidExampleSet(browser);
            projectWizard.ExamplesClickNext(browser);

            IWebElement projectName = browser.FindElement(NgBy.Binding("ProjectWizardController.project.name"));          
            projectName.Text.Should().Be("TestProject");

            projectWizard.SummaryClickFinish(browser);
            IWebElement grid = browser.FindElement(By.CssSelector(".k-grid"));
            grid.FindElement(By.XPath(".//span[text()='Case2']")).Should().NotBeNull();
        }

        /*
         * Sample Test Case for Exception condition
         */

        [TestMethod]
        [TestCategory("Create Project - Wizard")]
        public void PRC_CreateProject_InvalidUrl_ShouldThrowError()
        {

        }

    }
}
