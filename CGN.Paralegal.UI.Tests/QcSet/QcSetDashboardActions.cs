using System;
using System.Configuration;
using System.Globalization;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Protractor;

namespace CGN.Paralegal.UI.Tests
{
    using CGN.Paralegal.UI.Tests.Common;

    public class QcSetDashboardActions
    {

        /// <summary>
        /// Verify Qcset not created state in reviewer dashboard
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyQcSetNotCreatedState(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("verificationSetTab"));
            CommonActions.ElementShouldNotBeNull(driver, By.ClassName("disabled-tab"));
        }

        /// <summary>
        /// Verify qcset review ready to start in reviewer dashboard
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyQcsetReviewReadyToStartState(IWebDriver driver)
        {
            CommonActions.CheckElementEnabledAndText(driver, By.Id("btnVerificationSetReview"), Resources.Project.StartReview);

        }

        /// <summary>
        /// Verify qcset review inprogress state in reviewer dashboard
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void VerifyQcsetReviewInProgressState(IWebDriver driver)
        {
            CommonActions.CheckElementEnabledAndText(driver, By.Id("verificationReviewStatus"), 
                CGN.Paralegal.UI.App.resources.Project.QCSetReviewInProgress);
        }

        /// <summary>
        /// Click relevant doc list in qcset summary widget
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyRelevantDocListInQcSetSummary(IWebDriver driver)
        {
            
            CommonActions.CheckParentAndClickElement(driver, By.Id("QcSetSummary"), By.Id("Relevant"));
        }

        /// <summary>
        /// Click not relevant link in qcset summary widget
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyNotRelevantDocListInQcSetSummary(IWebDriver driver)
        {
           
            CommonActions.CheckParentAndClickElement(driver, By.Id("QcSetSummary"), By.Id("Not_Relevant"));
        }


        /// <summary>
        /// Click not coded list in qcset summary widget
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyNotCodedDocListInQcSetSummary(IWebDriver driver)
        {
           
            CommonActions.CheckParentAndClickElement(driver, By.Id("QcSetSummary"), By.Id("Not_Relevant"));
        }

        /// <summary>
        ///  click on project dashboard link 
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        public static void ClickOnProjectDashboardLink(IWebDriver driver)
        {   
            CommonActions.ClickElement(driver, By.XPath("//a[@ng-click='ReviewSetController.goToDashboard()']"));
        }
    }
}
