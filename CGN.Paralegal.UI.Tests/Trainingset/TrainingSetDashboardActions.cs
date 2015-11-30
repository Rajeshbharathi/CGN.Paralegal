namespace CGN.Paralegal.UI.Tests.Trainingset
{
    using FluentAssertions;
    using OpenQA.Selenium;
    using Protractor;


    public class TrainingSetDashboardActions
    {

        /// <summary>
        /// Click on training tab
        /// </summary>
        /// <param name="driver"></param> 
        internal static void ClickOnTrainingTab(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("trainingSetTab"));
            CommonActions.ClickElement(driver, By.Id("trainingSetTab"));
           
        }
        /// <summary>
        /// Click finish training review button
        /// </summary>
        internal static void ClickContinueTrainingReviewButton(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("modalDialog"));
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//h3[contains(text(),'Training Set Review Complete')]"));
            CommonActions.ClickElement(driver, By.Id("continueReviewBtn"));
        }

        /// <summary>
        /// Click finish training review button
        /// </summary>
        internal static void ClickFinishTrainingReviewButton(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("modalDialog"));
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//h3[contains(text(),'Training Set Review Complete')]"));
            CommonActions.ClickElement(driver, By.Id("finishReview"));
        }
        /// <summary>
        /// Click on Cancel Button in Finish Popup
        /// </summary>
        /// <param name="driver"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static void ClickCancelButtonInFinishPopUp(IWebDriver driver)
        {
            CommonActions.ClickElementInPopup(driver, By.Id("modalDialog"), By.Id("cancelButton"));
        }

        /// <summary>
        /// Click on Training Tab
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickTrainingTab(IWebDriver driver)
        {
            CommonActions.ClickElement(driver,By.Id("trainingSetTab"));
            
        }
        /// <summary>
        /// Verify that Run Predictions buttton is enabled
        /// </summary>
        /// <param name="driver"></param>
        internal static void VerifyRunPredictionsEnabled(IWebDriver driver)
        {
            CommonActions.ElementShouldBeEnabled(driver, By.Id("runPredictionsBtn"));
        }
        /// <summary>
        /// Click on Run Predictions button
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickRunPredictionsButton(IWebDriver driver)
        {
            CommonActions.ClickElement(driver,By.Id("runPredictionsBtn"));
            

        }
        /// <summary>
        /// Click on cancel button in Predict All Documents Popup
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickOnCancelButtonInPredictAllDocumentsPopup(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//button[contains(text(),'Cancel')]"));
        }
        /// <summary>
        /// Click on Confirm button in Predict All Documents Popup
        /// </summary>
        /// <param name="driver"></param>
       internal static void ClickOnConfirmButtonInPredictAllDocumentsPopup(IWebDriver driver)
        {
           CommonActions.ClickElement(driver, By.XPath("//button[contains(text(),'Confirm')]"));
        }
        /// <summary>
        /// Verifies that create verification set button is enabled
        /// </summary>
        /// <param name="driver"></param>
       internal static void VerifyPredictAllIsCompleted(IWebDriver driver)
       {
           CommonActions.ElementShouldBeEnabled(driver, By.Id("btnCreateQCSet"));
       }
        /// <summary>
        /// Selects Later Option in Run Predictions Popup
        /// </summary>
        /// <param name="driver"></param>
       internal static void SelectLaterOptionInPopup(IWebDriver driver)
       {
           var now = driver.FindElement(By.CssSelector(".modal input[type='radio'][value='true']"));
           now.Selected.Should().BeFalse();
           CommonActions.ClickElement(driver, By.CssSelector(".modal input[type='radio'][value='true']"));
          
       }
        /// <summary>
        /// Click on create verification set
        /// </summary>
        /// <param name="driver"></param>
       internal static void ClickOnCreateVerificationSet(IWebDriver driver)
       {
           CommonActions.ClickElement(driver, By.Id("btnCreateQCSet"));
       }
        /// <summary>
        /// Checks that qc set popup is displayed
        /// </summary>
        /// <param name="driver"></param>
       internal static void CheckVerificationSetPopup(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//div/h3[contains(text(),'Create Verification Set')]"));
        }
        /// <summary>
        /// Click on cancel button in create verification set popup
        /// </summary>
        /// <param name="driver"></param>
       internal static void ClickCancelButtonInCreateVerificationSetPopup(IWebDriver driver)
       {
           CommonActions.ClickElement(driver, By.XPath("//div[@class='modal-footer']//button[contains(text(),'Cancel')]"));
       }
        /// <summary>
        /// verifies that create verification set button is enabled
        /// </summary>
        /// <param name="driver"></param>
       internal static void VerifyCreateVerificationSetIsEnabled(IWebDriver driver)
       {
           CommonActions.ElementShouldBeEnabled(driver, By.Id("btnCreateQCSet"));
       }
        /// <summary>
        /// Enter Verification set name
        /// </summary>
        /// <param name="driver"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "OpenQA.Selenium.IWebElement.SendKeys(System.String)")]
       internal static void EnterVerificationSetName(IWebDriver driver)
       {
           CommonActions.EnterTextInTextBox(driver, By.Id("qcSetName"), Resources.Qcset.QcsetName);
       }
        /// <summary>
        /// Select Statistical sample in sampling option
        /// </summary>
        /// <param name="driver"></param>
       internal static void SelectStatisticalSampleOption(IWebDriver driver)
       {
           CommonActions.ClickElement(driver, By.Id("StatisticalSample"));
       }
        /// <summary>
        /// Select fixed size in sampling option
        /// </summary>
        /// <param name="driver"></param>
       [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "OpenQA.Selenium.IWebElement.SendKeys(System.String)")]
       internal static void SelectFixedSizeOption(IWebDriver driver)
       {
           CommonActions.ClickElement(driver, By.Id("FixedSize"));
           CommonActions.EnterTextInTextBox(driver, By.Id("qcSetSize"), Resources.Qcset.SampleSize);
       }
        /// <summary>
        /// verifies that verification set review is in progress
        /// </summary>
        /// <param name="driver"></param>
       internal static void VerifyVerificationsetReviewInProgressState(IWebDriver driver)
       {
           CommonActions.CheckElementEnabledAndText(driver, By.Id("verificationReviewStatus"), 
               CGN.Paralegal.UI.App.resources.Project.QCSetReviewInProgress);
       }


        /// <summary>
        /// click on create option create verification set popup
        /// </summary>
        /// <param name="driver"></param>
       internal static void ClickCreateButtonInCreateVerificationSetPopup(IWebDriver driver)
       {
           CommonActions.ClickElement(driver, By.XPath("//div[@class='modal-footer']//button[contains(text(),'Create')]"));
       }
        /// <summary>
        /// Verifies that Verification set tab is enabled
        /// </summary>
        /// <param name="driver"></param>
      internal static void VerifyVerificationSetIsCreated(IWebDriver driver)
       {
           CommonActions.ElementShouldBeEnabled(driver, By.Id("verificationSetTab"));
       }
        /// <summary>
        /// Verifies that rolling average chart is displayed
        /// </summary>
        /// <param name="driver"></param>
      internal static void VerifyRollingAverageChart(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.Id("rollingAverageChart"));
       }
        /// <summary>
        /// verifies that rolling average grid is displayed
        /// </summary>
        /// <param name="driver"></param>
        internal static void VerifyRollingAverageTable(IWebDriver driver)
       {
           CommonActions.ElementShouldNotBeNull(driver, By.Id("rollingAverageGrid"));
       }
        /// <summary>
        /// Click on predict ahead table view
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickTrainingTableView(IWebDriver driver)
       {
           CommonActions.ClickElement(driver, By.Id("predictAheadTableView"));
       }
        /// <summary>
        /// Verifies that predict discrepancies ahead is displayed
        /// </summary>
        /// <param name="driver"></param>
        internal static void VerifyPredictDiscrepanciesAhead(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("predictDescripanciesTrainingSet"));
        }
        /// <summary>
        /// Click on start review button in verification set tab
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickOnStartReview(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("btnVerificationSetReview"));
        }

        /// <summary>
        /// click on project dashboard button
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickProjectDashboard(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver,By.ClassName("modal-dialog"));
            CommonActions.ClickElement(driver,By.CssSelector(".pull-left.btn.btn-default.ng-binding.ng-scope"));
        }

        /// <summary>
        /// click on continue review button
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickOnContinueReview(IWebDriver driver)
        {
            CommonActions.ClickElement(driver,By.Id("btnTrainingReview"));
           
        }

        /// <summary>
        /// click on relevant box
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickRelevantCodingSummary(IWebDriver driver)
       {
           CommonActions.CheckParentAndClickElement(driver, By.Id("trainingSummary"), By.Id("Relevant"));
       }

       /// <summary>
       /// click on not relevant box
       /// </summary>
       /// <param name="driver"></param>
        internal static void ClickNotRelevantCodingSummary(IWebDriver driver)
       {
           CommonActions.CheckParentAndClickElement(driver, By.Id("trainingSummary"), By.Id("Not_Relevant"));
       }

        /// <summary>
        /// click on Skipped box
        /// </summary>
        /// <param name="driver"></param>
        internal static void ClickSkippedCodingSummary(IWebDriver driver)
        {
            CommonActions.CheckParentAndClickElement(driver, By.Id("trainingSummary"), By.Id("Skipped"));
        }

        /// <summary>
       /// click on not coded box
       /// </summary>
       /// <param name="driver"></param>
        internal static void ClickNotCodedCodingSummary(IWebDriver driver)
       {
           CommonActions.CheckParentAndClickElement(driver, By.Id("trainingSummary"), By.Id("Not_Coded"));
       }
        /// <summary>
        /// Click on table view
        /// </summary>
        /// <param name="driver"></param> 
        internal static void ClickOnTableView(IWebDriver driver)
        {
            CommonActions.CheckParentAndClickElement(driver, By.Id("trainingSummary"), By.Id("listView"));
        }

        /// <summary>
        /// Click on table view button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickTrainingTrendTableView(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("trainingtrendTableView"));
        }

        /// <summary>
        /// Click on chart view button
        /// </summary>
        /// <param name="driver"></param>
        public static void ClickTrainingTrendGraphView(IWebDriver driver)
        {
            CommonActions.ClickElement(driver,By.Id("trainingtrendGridView"));
        }

        /// <summary>
        /// Verify Training Trend Grid
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyTrainingTrendsGridView(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("trainingTrendsGrid"));
        }

        /// <summary>
        /// Verify Training Trend chart
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyTrainingTrendsChart(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.Id("trainingTrendChart"));
        }

        /// <summary>
        /// Verify Training Trend In Progress status
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyTrainingTrendsInProgressStatus(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver,NgBy.Binding("TrainingSetController.localized.TrainingSetReviewInProgress"));
          
        }
        /// <summary>
        /// Verify Training Trend In Progress status
        /// </summary>
        /// <param name="driver"></param>
        public static void VerifyPredictDiscrepanciesInProgressStatus(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver,NgBy.Binding("TrainingSetController.localized.TrainingSetReviewInProgress"));
        }


        /// <summary>
        /// Click on round link
        /// </summary>
        /// <param name="driver"></param> 
        internal static void ClickOnRoundLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//td//a"));
        }

        /// <summary>
        /// Click on project dashboard link
        /// </summary>
        /// <param name="driver"></param> 
        internal static void ClickOnProjectDashboardLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("projectDashboard"));
        }

        /// <summary>
        /// Click on relevant link
        /// </summary>
        /// <param name="driver"></param> 
        internal static void ClickOnRelevantLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//td//a[contains(@ng-click,'Relevant')]"));
        }

        /// <summary>
        /// Click on not relevant link
        /// </summary>
        /// <param name="driver"></param> 
        internal static void ClickOnNotRelevantLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.XPath("//td//a[contains(@ng-click,'Not_Relevant')]"));
        }

        /// <summary>
        /// Verify not relevant document exists 
        /// </summary>
        /// <param name="driver"></param> 
        internal static void VerifyNotRelevantDocuments(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//td//span[@ng-bind='dataItem.ReviewerCategory']"));
           
            CommonActions.CheckElementEnabledAndText(driver, By.XPath("//td//span[@ng-bind='dataItem.ReviewerCategory']"), Resources.Project.NotRelevant);
           
        }
        /// <summary>
        /// verify prediction discrepancies widget
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void VerifyPredictionDiscrepanciesWidget(IWebDriver driver)
        {
            CommonActions.ElementShouldNotBeNull(driver, By.XPath("//div[@ng-if='TrainingSetController.showDiscrepancies()']"));
            
        }

        /// <summary>
        /// click on true positive link in prediction discrepancies
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void ClickOnTruePositivesLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("truePositives"));
        }


        /// <summary>
        ///  click on false negative link in prediction discrepancies
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void ClickOnFalseNegativesLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("falseNegatives"));
        }

        /// <summary>
        ///  click on false positive link in prediction discrepancies
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void ClickOnFalsePositivesLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("falsePositives"));
        }

        /// <summary>
        ///  click on true negative link in prediction discrepancies
        /// </summary>
        /// <param name="driver">IWebDriver</param>
        internal static void ClickOnTrueNegativesLink(IWebDriver driver)
        {
            CommonActions.ClickElement(driver, By.Id("trueNegatives"));
        }

    }

}