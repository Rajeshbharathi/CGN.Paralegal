namespace CGN.Paralegal.UI.Tests.ProjectDashboard
{
    using FluentAssertions;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;
    using Protractor;
    using System;
    
    public class ProjectDashBoardActions
    {
        /// <summary>
        /// Select predict all in dropdown
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void GoBackToTraining(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("btnBackToTraining"));
        }
        
        /// <summary>
        /// Verify next step has changed to predict all in trainingset tab
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyTrainingState(IWebDriver driver)
        {
            CommonActions.CheckElementEnabledAndText(driver, By.Id("trainingReviewStatus"), Resources.Project.ReviewNotStarted);
            
        }

        /// <summary>
        /// Select add documents to project in dropdown
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void SelectAddDocuments(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("addDocumentstoProject"));
        }

        /// <summary>
        /// Verify Total Project Documents count
        /// </summary>
        /// <param name="driver">IWebDriver</param>
       public static void VerifyTotalProjectDocumentsCount(IWebDriver driver)
        {
            CommonActions.CheckElementEnabledAndText(driver, By.Id("totalDocumentCount"), Resources.Project.TotalDocumentCountAfterAddDocuments);
           
        }

         

        /// <summary>
        /// Verify Total Project Documents count after cancel   
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyTotalProjectDocumentsCountAfterCancel(IWebDriver driver)
        {
            CommonActions.CheckElementEnabledAndText(driver, By.Id("totalDocumentCount"),Resources.Project.TotalDocumentCount);
        }
      
        /// <summary>
        /// Verify Control Set Summary
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyControlSetSummary(IWebDriver driver)
        {
            CommonActions.CheckElementEnabledAndText(driver,By.Id("RelevantCount"),Resources.Project.RelevantCount);
            CommonActions.CheckElementEnabledAndText(driver,By.Id("Not_RelevantCount"),Resources.Project.NotRelevantCount);
            CommonActions.CheckElementEnabledAndText(driver,By.Id("Not_CodedCount"),Resources.Project.NotCodedCount);
        }

        public static void ViewAllDocuments(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("viewAllDocuments"));
        }
    }
}
