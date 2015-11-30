using OpenQA.Selenium;
using Protractor;
using System;

namespace CGN.Paralegal.UI.Tests
{
    using FluentAssertions;
    using OpenQA.Selenium.Support.UI;

    /// <summary>
    /// Create Project -E2E user actions for test 'Create Project'
    /// </summary>
    public class CreateProjectActions
    {

        #region Variables

        private const string PrimaryButton = "btn-primary";
        private const string DefaultButton = "btn-default";
        private const string SelectedTag = "FR1";
        private const string SpanReviewed = "Reviewed";
        private const string TagRadio = "ProjectWizardController.documentOptions.tag";
        private const string SavedSearchRadio = "ProjectWizardController.documentOptions.savedSearch";
        private const string DoQuery = "ProjectWizardController.documentOptions.query";
        private const string TagDroplist = "k-select";
        private const string SavedSearchLiName = "motion";

        #endregion Variables

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "OpenQA.Selenium.IWebElement.SendKeys(System.String)")]
        public static void TypeProjectInfo(IWebDriver driver)
        {
            CommonActions.EnterTextInTextBox(driver, NgBy.Model("ProjectWizardController.project.Name"), Resources.Project.ProjectName);
            CommonActions.EnterTextInTextBox(driver, NgBy.Model("ProjectWizardController.project.Description"), Resources.Project.ProjectDescription);
            CommonActions.EnterTextInTextBox(driver, NgBy.Model("ProjectWizardController.project.FieldPrefix"), Resources.Project.ProjectPrefix);
        }

        public static void ShouldDisdplaySelectDocumentsPage(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, NgBy.Model("ProjectWizardController.selectedOption"));
        }

        public static void ShouldDisdplayValidationErrors(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver,NgBy.Binding("ProjectWizardController.ProjectNameErrorMessage"));
            CommonActions.ElementShouldNotBeNull(driver, NgBy.Binding("ProjectWizardController.FieldPrefixErrorMessage"));
        }


        /// <summary>
        /// Check Error block exists
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static bool IsErrorBlockExists(IWebDriver driver)
        {
            try
            {
                driver.FindElement(By.CssSelector(".alert.alert-danger"));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check 'Total document label' exists
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static bool IsProjectStatusExists(IWebDriver driver)
        {
            try
            {
                
                CommonActions.ElementShouldBeEnabled(driver, By.Id("totalDocumentCount"));
                
            }
            catch (Exception)
            {

                return false;
            }
            return true;
        }

        public static void ClickPrimaryButton(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("nextBtn"));
            
        }
         public static void ClickCreateButton(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("createBtn"));
        }
        /// <summary>
        /// Select Documents By Tag
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void SelectDocumentsByTag(IWebDriver driver)
        {
           
                CommonActions.CheckVisibleAndClickElement(driver, By.Id("radioBtn"));
                CommonActions.ClickElement(driver, By.XPath("//span[@class='" + TagDroplist + "']"));
                CommonActions.ClickElement(driver, By.XPath("//ul/li[contains(text(), '" + SelectedTag + "')]"));
                

                //create button
                var createButton = driver.FindElement(By.Id("createBtn"));
                createButton.Should().NotBeNull();
                if (createButton.Enabled)
                    createButton.Click();
                else
                    CommonActions.ClickElement(driver, By.XPath("//ul/li[contains(text(), '" + SpanReviewed + "')]"));
                   
        }
        /// <summary>
        /// Select Documents By Saved Search 
        /// </summary>
        /// <param name="driver">IWebDriver</param>
       public static void SelectDocumentsBySavedSearch(IWebDriver driver)
        {

            CommonActions.CheckVisibleAndClickElement(driver, By.Id("savedSearchBtn"));
               
                CommonActions.ClickElement(driver, By.XPath("//span[@class='" + TagDroplist + "']"));
               
                CommonActions.ClickElement(driver, By.XPath("//ul/li[contains(text(), '" + SavedSearchLiName + "')]"));

                CommonActions.CheckVisibleAndClickElement(driver, By.Id("createBtn"));
                
            
        }

        /// <summary>
        /// Select Documents By New Search
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void SelectDocumentsByNewSearch(IWebDriver driver)
        {           
           
            if (driver.FindElement(By.XPath("//input[@ng-value='" + DoQuery + "']")).Enabled == true)
            {
                CommonActions.ClickElement(driver,By.XPath("//input[@ng-value='" + DoQuery + "']"));
                CommonActions.EnterTextInTextBox(driver, By.XPath("//input[@ng-model='ProjectWizardController.documentOptions.query.value']"), Resources.Project.SearchQuery);
                CommonActions.CheckVisibleAndClickElement(driver, By.XPath("//button[@ng-click='ProjectWizardController.search()']"));
                CommonActions.CheckVisibleAndClickElement(driver, By.ClassName(PrimaryButton));
               
            }           
        }

        /// <summary>
        /// Click Cancel in Predictive Coding
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void clickCancelBtn(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.ClassName(DefaultButton));
           
        }

    }
}
