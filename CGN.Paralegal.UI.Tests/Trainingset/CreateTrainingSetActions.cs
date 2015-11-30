namespace CGN.Paralegal.UI.Tests.Trainingset
{
    using System;
    using System.Globalization;

    using FluentAssertions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;

    using Protractor;

    class CreateTrainingSetActions
    {

        //protected readonly WebDriverWait _wait;
        protected IWebDriver _driver;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="driver"></param>
        public CreateTrainingSetActions(IWebDriver driver)
        {
            this._driver = driver;
        }


        /// <summary>
        /// Click next document button
        /// </summary>
        internal void IsAtTrainingSetReviewPage()
        {

            CommonActions.ElementShouldNotBeNull(this._driver, By.XPath("//h3[contains(text(),'Training Set Review ( Set 1 )')]"));
           
        }

    }
}
