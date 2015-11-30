using System.Globalization;
using System.Web.Http;

namespace CGN.Paralegal.UI
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Web.Optimization;

    public class AppController : BaseApiController
    {
        private const string AppHtmlTemplate =
            @"<!DOCTYPE html>
            <html>
            <head>
                <title>Analytics</title>
                {KendoCssBundle}
                {BootstrapCssBundle}

                <link href=""/Content/css/font-awesome4.3.0/css/font-awesome.min.css"" rel=""stylesheet"" />
                <link href=""/Content/css/site.css"" rel=""stylesheet"" />
                <meta name=""viewport"" content=""initial-scale=1"">
            </head>
            <body ng-app=""app"" ng-cloak>
                <div ln-error-handler></div>
                <div ng-view></div>

                {AngularJsBundle}
                {AppJsBundle}
            </body>
            </html>";

        /// <summary>
        /// Get App page
        /// </summary>
        /// <returns></returns>
        /// Web api can not be static - approval from lead
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [Route("app/{module}/approot")]
        public HttpResponseMessage Get(string module)
        {
            return module == "analytics"?
                GetResponse(BundleConfig.AnalyticsJsBundlePath) : GetResponse(BundleConfig.ReviewJsBundlePath);
        }

        ///// <summary>
        ///// Get Reviewer page
        ///// </summary>
        ///// <returns></returns>
        ///// Web api can not be static - approval from lead
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        //[Route("app/review/approot")]
        //public HttpResponseMessage GetReviewPage()
        //{
        //    return GetResponse(BundleConfig.ReviewJsBundlePath);
        //}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private HttpResponseMessage GetResponse(string appJsBundle)
        {
            var response =
                AppHtmlTemplate.Replace("{KendoCssBundle}", Styles.Render(BundleConfig.KendoCssBundlePath).ToString())
                    .Replace("{BootstrapCssBundle}", Styles.Render(BundleConfig.BootstrapCssBundlePath).ToString())
                    .Replace("{AngularJsBundle}", Scripts.Render(BundleConfig.AngularJsBundlePath).ToString())
                    .Replace("{AppJsBundle}", Scripts.Render(appJsBundle).ToString());

            return new HttpResponseMessage { Content = new StringContent(response, Encoding.UTF8, "text/html") };
        }

    }
}