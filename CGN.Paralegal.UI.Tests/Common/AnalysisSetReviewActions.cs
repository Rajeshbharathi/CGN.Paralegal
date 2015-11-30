namespace CGN.Paralegal.UI.Tests.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using FluentAssertions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;

    class AnalysisSetReviewActions
    {

        protected readonly WebDriverWait _wait;
        protected IWebDriver _driver;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="driver"></param>
        public AnalysisSetReviewActions(IWebDriver driver)
        {
            this._driver = driver;
            this._wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
        }

        internal void DisableAutoAdvance()
        {
           CommonActions.CheckVisibleAndClickElement(this._driver, By.Id("autoAdvance"));
        }

        /// <summary>
        /// Click Start Review button
        /// </summary>
        internal void ClickStartReviewButton()
        {
            CommonActions.ClickElement(this._driver, By.Id("btnCreate"));
        }

        internal void ClickVerificationSetStartReview()
        {
            CommonActions.ClickElement(_driver, By.Id("btnVerificationSetReview"));
        }

        /// <summary>
        /// Training Set Tab Click Start Review  button
        /// </summary>
        internal void ClickTrainingSetStartReviewButton()
        {
            CommonActions.ClickElement(this._driver, By.Id("btnTrainingReview"));
        }

        /// <summary>
        /// Click TraningSet Tab button
        /// </summary>
        internal void ActivateTrainingTab()
        {
            CommonActions.ClickElement(this._driver, By.Id("trainingSetTab"));
        }
        /// <summary>
        /// Click VerificationSet Tab button
        /// </summary>
        internal void ActivateVerificationTab()
        {
            CommonActions.ClickElement(this._driver, By.Id("verificationSetTab"));
        }


      
        /// <summary>
        /// Click the Continue Review button
        /// </summary>
        internal void ClickContinueReviewButton()
        {   
           CommonActions.ClickElement(this._driver,By.Id("btnCreate"));
        }

        /// <summary>
        /// Click First document button
        /// </summary>
        internal void ClickFirstDocumentButton()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElement(this._driver, By.CssSelector("span.glyphicon.glyphicon-fast-backward.ng-scope"));
        }

        /// <summary>
        /// Go to specified document number
        /// </summary>
        /// <param name="p">specified document number</param>
        internal void GoToDocument(int p)
        {
            this.WaitForAngularShellPageLoad();
            
            var ele = this._driver.FindElement(By.ClassName("navigator-input"));
            ele.Clear();
            ele.SendKeys(p.ToString(CultureInfo.InvariantCulture));
            ele.SendKeys(Keys.Return);

            var currentDoc = this._driver.FindElement(By.Id("currentRecordNumber")).GetAttribute("value");
            Assert.AreEqual(p, Convert.ToInt32(currentDoc, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Click next document button
        /// </summary>
        internal void ClickNextDocumentButton()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver, By.XPath("//span[@class='glyphicon glyphicon-chevron-right']"));
        }


        /// <summary>
        /// Mark current document as relevant
        /// </summary>
        internal void MarkDocumentAsRelevant()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver, By.Id("relevant"));
        }

        /// <summary>
        /// Mark current document as not relevant
        /// </summary>
        internal void MarkDocumentAsNotRelevant()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver,By.Id("notrelevant"));
        }

        /// <summary>
        /// Mark current document as not relevant
        /// </summary>
        internal void MarkDocumentAsSkipped()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver, By.Id("skip"));
        }

        /// <summary>
        /// Verify that current document is marked as relevant
        /// </summary>
        internal void VerifyDocumentIsMarkedAsRelevant()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.Id("relevant"));
            var coding = this._driver.FindElement(By.Id("relevant"));
            var v = coding.GetAttribute("class");
            v.Should().Contain("active");
        }

        /// <summary>
        /// Verify that current document is marked as not relevant
        /// </summary>
        internal void VerifyDocumentIsMarkedAsNotRelevant()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.Id("notrelevant"));
            var coding = this._driver.FindElement(By.Id("notrelevant"));
            var v = coding.GetAttribute("class");
            v.Should().Contain("active");
        }

        /// <summary>
        /// Verify that current document is marked as not relevant
        /// </summary>
        internal void VerifyDocumentIsMarkedAsskipped()
        {
            this.WaitForAngularShellPageLoad();
            
            var ele = this._driver.FindElement(By.Id("skip"));
            var r1 = ele.GetAttribute("class");
            r1.Contains("btn-warning").Should().BeTrue();
            var nextBtn = this._driver.FindElement(By.XPath("//span[contains(text(),'Next')]"));
            nextBtn.Enabled.Should().BeTrue();
        }

        /// <summary>
        /// Verify that the document text is visable
        /// </summary>
        internal void VerifyDocumentTextVisible()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.TagName("pre"));
           
        }

        /// <summary>
        /// Verify that out of range message is displayed
        /// </summary>
        internal void VerifyOutOfRangeErrorMessageIsDisplayed()
        {
            CommonActions.ElementShouldNotBeNull(this._driver, By.XPath("//p[contains(text(), 'Please enter a number between')]"));
           
        }

        /// <summary>
        /// Get the total number of documents in review set
        /// </summary>
        /// <returns></returns>
        internal int GetMaxDocumentNumber()
        {
            WebDriverWait wait = new WebDriverWait(this._driver, TimeSpan.FromSeconds(Settings.WaitTimeOut));
            var totalDocs = wait.Until(d => d.FindElement(By.Id("totalDocuments")));
            return Convert.ToInt32(totalDocs.Text.Trim(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Verify that current document number is as specified
        /// </summary>
        /// <param name="p">specified number</param>
        internal void IsAtDocument(int p)
        {
            this.WaitForAngularShellPageLoad();
            
            var ele = this._driver.FindElement(By.Id("currentRecordNumber"));
            var currentDoc = ele.GetAttribute("value");
            Assert.AreEqual(p, Convert.ToInt32(currentDoc, CultureInfo.InvariantCulture));
        }
        /// <summary>
        /// Click next unencoded doument button
        /// </summary>
        internal void ClickNextUncodedDocumentButton()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver,By.XPath("//span[contains(text(), 'Next')]"));
        }
        /// <summary>
        /// Click last document button
        /// </summary>
        internal void ClickLastDocumentButton()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElement(this._driver, By.CssSelector("span.glyphicon.glyphicon-fast-forward.ng-scope"));
        
        }
        /// <summary>
        /// Click project dashboard link at top left corner of page
        /// </summary>
        internal void ClickProjectDashboardLink()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver, By.LinkText("Project Dashboard"));
           
        }

        /// <summary>
        /// Click view sets link at top left corner of page
        /// </summary>
        internal void ClickViewSetsLink()
        {
            CommonActions.CheckVisibleAndClickElement(this._driver, By.LinkText("View Set"));
        }

        /// <summary>
        /// Check if is jumping to DocListPage
        /// </summary>
        internal void IsAtDocListPage()
        {
            CommonActions.ElementShouldNotBeNull(this._driver, By.Id("grid"));
            
        }



        /// <summary>
        /// Verify the control set review finish popup
        /// </summary>
        internal void VerifyFinishPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.CssSelector(".modal-dialog"));
            CommonActions.ElementShouldNotBeNull(this._driver, By.XPath("//p[contains(text(),'You have completed coding')]"));
        }

        internal void ClickCloseOnFinishPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElementInPopup(this._driver, By.ClassName("modal-dialog"), By.Id("closeFinishReviewPopup"));
        }

        internal void ClickOkOnFinishPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElementInPopup(this._driver, By.ClassName("modal-dialog"), By.LinkText("OK"));
        }
        /// <summary>
        /// Click the cancel button on control set review finish popup
        /// </summary>
        internal void ClickCancelOnFinishPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElementInPopup(this._driver, By.ClassName("modal-dialog"), By.LinkText("Cancel"));
        }
        /// <summary>
        /// Click the project dashboard button on the control set review finish popup
        /// </summary>
        internal void ClickProjectDashboardOnFinishPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElementInPopup(this._driver, By.ClassName("modal-dialog"), By.LinkText("Project Dashboard"));
       }

        /// <summary>
        /// Verify that current page is review page
        /// </summary>
        internal void IsAtReviewPage()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.CssSelector("div.container-fluid.reviewer"));
        }


        /// <summary>
        /// Verify that current page is review page
        /// </summary>
        internal void IsAtReviewerDashboardPage()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ElementShouldNotBeNull(this._driver, By.Id("dashboard"));
        }

        /// <summary>
        /// Review all documents in the control set so that the review finish window popups
        /// </summary>
        internal void ReviewAllDocumentsAutoAdvanceToDisplayFinishPopup()
        {
            var count = this.GetMaxDocumentNumber();
            for (int i = 1; i <= count; i++)
            {

                if (i % 2 == 0) this.MarkDocumentAsRelevant();
                else this.MarkDocumentAsNotRelevant();
            }
        }
        
        /// <summary>
        /// Review all documents in the control set so that the review finish window popups
        /// </summary>
        internal void ReviewAllDocumentsToDisplayFinishPopup()
        {
            var count = this.GetMaxDocumentNumber();
            for (int i = 1; i <= count; i++)
            {

                this.GoToDocument(i);

                if (i % 2 == 0) this.MarkDocumentAsRelevant();
                else this.MarkDocumentAsNotRelevant();
            }

            this.ClickNextUncodedDocumentButton();
        }

        /// <summary>
        /// Review all documents as relevant
        /// </summary>
        internal void ReviewAllDocumentsAsRelevant()
        {
            var count = this.GetMaxDocumentNumber();
            for (int i = 1; i <= count; i++)
            {
                this.GoToDocument(i);
                this.MarkDocumentAsRelevant();               
            }

            this.ClickNextUncodedDocumentButton();
        }

        /// <summary>
        /// Review all documents as not relevant
        /// </summary>
        internal void ReviewAllDocumentsAsNotRelevant()
        {
            var count = this.GetMaxDocumentNumber();
            for (int i = 1; i <= count; i++)
            {
                this.GoToDocument(i);
                this.MarkDocumentAsNotRelevant();
            }
            this.ClickNextUncodedDocumentButton();
            
        }


        /// <summary>
        /// Review all documents as not relevant
        /// </summary>
        internal void ReviewAllDocumentsAsSkipped()
        {
            var count = this.GetMaxDocumentNumber();
            for (int i = 1; i <= count; i++)
            {

                this.GoToDocument(i);
                this.MarkDocumentAsSkipped();
            }
            this.ClickNextUncodedDocumentButton();
            
        }

        /// <summary>
        /// Click next document button
        /// </summary>
        internal void CheckConfirmationModalWindow()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.CheckPopup(this._driver, By.ClassName("modal-dialog"), By.XPath("//h3[contains(text(),'Training Set Review Complete')]"));
        }

        /// <summary>
        /// Check continue training confirmation modal
        /// </summary>
        internal void CheckContinueTrainingConfirmationModalWindow()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.CheckPopup(this._driver, By.ClassName("modal-dialog"), By.XPath("//h3[contains(text(),'Continue Training')]"));
        }

        /// <summary>
        /// Check QcSet confirmation modal
        /// </summary>
        internal void CheckQcSetCompleteConfirmationModalWindow()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.CheckPopup(this._driver, By.ClassName("modal-dialog"), By.XPath("//h3[contains(text(),'Verification Set Review Complete')]"));
        }
      
        /// <summary>
        /// Click finish in Qc Set Finish Popup
        /// </summary>
        internal void ClickPrimaryButtonOnQcSetPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElementInPopup(this._driver, By.Id("modalContent"), By.Id("finishQcSetReview"));
        }
        
        /// <summary>
        /// Click cancel in finish training review
        /// </summary>
        internal void ClickCancelButtonInFinishTrainingSetReview()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.ClickElementInPopup(this._driver, By.Id("modalDialog"), By.Id("cancelBtn"));
        }

        /// <summary>
        /// Click close in continue training popup
        /// </summary>
        internal void ClickCloseButtonInContinueTrainingPopup()
        {
            this.WaitForAngularShellPageLoad();
            CommonActions.CheckHeaderAndClickElementInPopup(this._driver, By.ClassName("modal-dialog"), By.XPath("//h3[contains(text(),'Continue Training')]"), By.LinkText("Close"));
        }

        /// <summary>
        /// Wait for load angular page
        /// </summary>
        public void WaitForAngularShellPageLoad()
        {
            this._wait.Until(d =>
            {
                try
                {
                    var element = d.FindElement(By.Id("loading-bar-spinner"));
                    return !element.Displayed;
                }
                catch (Exception)
                {

                    return true;
                }
            });
        }


        /// <summary>
        /// Wait for load project
        /// </summary>
        public void WaitForLoadPage()
        {
            this._wait.Until(d =>
            {
                try
                {
                    d.SwitchTo().DefaultContent();
                    var element = d.FindElement(By.CssSelector(".k-loading-image"));
                    return !element.Displayed;
                }
                catch (Exception)
                {

                    return true;
                }
            });
        }

        public void VerifyAllDocuments()
        {
            var totaldocs = this._driver.FindElement(By.Id("docListTotalItems"));
            totaldocs.Text.Should().Contain("30");

            var analysisSetsDropList = this._driver.FindElement(By.Id("analysisSetsDropList"));
            analysisSetsDropList.Text.Should().Contain("All Documents");
        }
    }
}
