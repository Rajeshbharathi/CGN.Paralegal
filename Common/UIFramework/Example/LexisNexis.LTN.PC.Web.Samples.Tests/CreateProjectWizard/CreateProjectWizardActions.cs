using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support;
using Protractor;

namespace LexisNexis.LTN.PC.Web.Samples.Tests
{
    public class CreateProjectWizardActions
    {
        private string validUrl = ConfigurationManager.AppSettings["ValidAppUrl"];
        private string invalidUrl = ConfigurationManager.AppSettings["InvalidAppUrl"];
        
        public void GoToProjectListPage(IWebDriver driverInstance)
        {
            driverInstance.Navigate().GoToUrl(validUrl);
        }

        public void GoToInvalidUrl(IWebDriver driverInstance)
        {
            driverInstance.Navigate().GoToUrl(invalidUrl);
        }

        public void ClickNewProjectButton(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.Id("addNewProject")).Click();
        }

        public void PopulateEmptyProjectName(IWebDriver driverInstance)
        {
            IWebElement input = driverInstance.FindElement(NgBy.Input("ProjectWizardController.project.name"));
            input.Clear();
            //input.SendKeys("1");
            driverInstance.FindElement(By.Id("txtDescription")).Click();
        }

        public void PopulateProjectNameWithOneChar(IWebDriver driverInstance)
        {
            IWebElement input = driverInstance.FindElement(NgBy.Input("ProjectWizardController.project.name"));
            input.Clear();
            input.SendKeys("1");
            driverInstance.FindElement(By.Id("txtDescription")).Click();
        }
        public void PopulateProjectNameWithSpecialChar(IWebDriver driverInstance)
        {
            IWebElement input = driverInstance.FindElement(NgBy.Input("ProjectWizardController.project.name"));
            input.Clear();
            input.SendKeys("##TEST##");
            driverInstance.FindElement(By.Id("txtDescription")).Click();
        }

        public void PopulateValidProjectSettings(IWebDriver driverInstance)
        {
            IWebElement input = driverInstance.FindElement(NgBy.Input("ProjectWizardController.project.name"));
            IWebElement textarea = driverInstance.FindElement(By.Id("txtDescription"));
            ICollection<IWebElement> radioGroup = driverInstance.FindElements(NgBy.Input("ProjectWizardController.project.docSource"));
            IWebElement checkbox = driverInstance.FindElement(By.Id("checkboxes-0"));

            input.Clear();
            input.SendKeys("TestProject");
            textarea.Clear();
            textarea.SendKeys("Sample_Description_Text1#");
            radioGroup.ElementAt(0).Click();

        }


        public void ProjectSettingsClickNext(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#settingsForm>.btn")).Click();
        }


        public void PopulateValidControlSet(IWebDriver driverInstance)
        {
            IWebElement confidence = driverInstance.FindElement(By.Id("selConfidence"));
            IWebElement marginOfError = driverInstance.FindElement(By.Id("selMarginOfError"));
            IWebElement stratifyCheckbox = driverInstance.FindElement(By.Id("checkboxes-1"));
            IWebElement custodian = driverInstance.FindElement(By.Id("selCustodianFields"));
            IWebElement calculatedSize = driverInstance.FindElement(NgBy.Binding("ProjectWizardController.project.sampleSize"));
            IWebElement calculateBtn = driverInstance.FindElement(By.Id("calculateLimit"));

            confidence.FindElement(By.CssSelector("option[value='85']")).Click();
            marginOfError.FindElement(By.CssSelector("option[value='3']")).Click();
            stratifyCheckbox.Click();
            calculateBtn.Click();
        }

        public void ControlSetClickNext(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#controlsForm>.next")).Click();
        }

        public void ControlSetClickPrevious(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#controlsForm>.prev")).Click();
        }

        public void PopulateValidExampleSet(IWebDriver driverInstance)
        {

            IWebElement limitTextBox = driverInstance.FindElement(By.Id("numOfExamples"));
            limitTextBox.Clear();
            limitTextBox.SendKeys("3000");
        }

        public void ExamplesClickNext(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#examplesForm>.next")).Click();
        }


        public void ExamplesClickPrevious(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#examplesForm>.prev")).Click();
        }

        public void SummaryClickFinish(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#summaryForm>.submit")).Click();
        }

        public void SummaryClickPrevious(IWebDriver driverInstance)
        {
            driverInstance.FindElement(By.CssSelector("#summaryForm>.prev")).Click();
        }
    }
}
