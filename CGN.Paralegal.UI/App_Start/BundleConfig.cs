namespace CGN.Paralegal.UI
{
    using System.Web.Optimization;

    using Utils;

    public class BundleConfig
    {
        public const string AnalyticsJsBundlePath = "~/bundles/analytics/js";

        public const string ReviewJsBundlePath = "~/bundles/review/js";

        public const string AngularJsBundlePath = "~/bundles/angular/js";

        public const string BootstrapCssBundlePath = "~/bundles/bootstrap/css";

        public const string KendoCssBundlePath = "~/bundles/kendo/css";

        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(AnalyticsJsBundle());
            bundles.Add(ReviewJsBundle());
            bundles.Add(AngularJsBundle());

            bundles.Add(BootstrapCssBundle());
            bundles.Add(KendoCssBundle());

            BundleTable.EnableOptimizations = true;
        }

        private static Bundle AnalyticsJsBundle()
        {
            var bundle = new ScriptBundle(AnalyticsJsBundlePath).Include(
                "~/App/resources/Project.resx",
                "~/App/resources/Review.resx",
                "~/App/resources/Common.resx",
                "~/App/resources/Reviewer.resx",
                "~/App/analytics/app.js",
                "~/App/analytics/app.config.js",
                "~/App/analytics/app.routes.js",
                "~/App/analytics/app.init.js",
                "~/App/common/services/AppStateService.js",
                "~/App/common/services/Utils.js",
                "~/App/common/services/Constants.js",
                "~/App/common/services/WebApiClientService.js",
                "~/App/common/decorators/WebApiClientServiceDecorator.js",
                "~/App/common/directives/VisibleDirective.js",
                "~/App/common/directives/ErrorHandlerDirective.js",
                "~/App/common/services/ErrorHandlerService.js",
                "~/App/common/directives/InputValidationDirective.js",
                "~/App/common/directives/WorkflowWizardDirective.js",
                "~/App/common/directives/ModalDirective.js",
                "~/App/common/directives/DiscrepancyMatrixDirective.js",
                "~/App/common/directives/CodingSummaryDirective.js",
                "~/App/common/services/DashboardService.js",
                "~/App/common/services/AnalysisSetService.js",
                "~/App/common/services/DocumentService.js",
                "~/App/common/services/WorkflowStateService.js",
                "~/App/analytics/projects/projectWizard/ProjectWizardController.js",
                "~/App/common/controllers/DashboardController.js",
                "~/App/common/controllers/ControlSetController.js",
                "~/App/common/controllers/TrainingSetController.js",
                "~/App/common/controllers/VerificationSetController.js");

            bundle.Transforms.Clear();

            bundle.Transforms.Add(new ResxTransform());

            //OPTIONAL: uncomment this line after we fix Angular injection that prevents JavaScript minification. 
            //Minification is not really needed since we zip all resources
            //bundle.Transforms.Add(new JsMinify());

            return bundle;
        }

        private static Bundle ReviewJsBundle()
        {
            var bundle = new ScriptBundle(ReviewJsBundlePath).Include(

                "~/App/resources/Project.resx",
                "~/App/resources/Review.resx",
                "~/App/resources/Common.resx",
                "~/App/resources/Reviewer.resx",
                "~/App/review/app.js",
                "~/App/review/app.config.js",
                "~/App/review/app.routes.js",
                "~/App/review/app.init.js",
                "~/App/common/services/AppStateService.js",
                "~/App/common/services/Utils.js",
                "~/App/common/services/WebApiClientService.js",
                "~/App/common/services/Constants.js",
                "~/App/common/decorators/WebApiClientServiceDecorator.js",
                "~/App/common/directives/VisibleDirective.js",
                "~/App/common/directives/ErrorHandlerDirective.js",
                "~/App/common/services/ErrorHandlerService.js",
                "~/App/common/directives/InputValidationDirective.js",
                "~/App/common/directives/ngInfiniteScroll.js",
                "~/App/common/directives/WorkflowWizardDirective.js",
                "~/App/common/directives/NavigatorDirective.js",
                "~/App/common/directives/HighlighterDirective.js",                      
                "~/App/common/services/DashboardService.js",
                "~/App/common/services/AnalysisSetService.js",
                "~/App/common/services/DocumentService.js",
                "~/App/common/components/reviewer/ReviewerWidgetsDefinition.js",
                "~/App/common/components/reviewer/ReviewerDirectiveController.js",
                "~/App/common/components/reviewer/ReviewerDirective.js",
                "~/App/common/components/widgets/AssistedReviewController.js",                
                "~/App/common/directives/CodingSummaryDirective.js",
                "~/App/common/services/WorkflowStateService.js",
                "~/App/review/reviewset/ReviewSetController.js",
                "~/App/common/controllers/DashboardController.js",
                "~/App/common/controllers/ControlSetController.js",
                "~/App/common/controllers/TrainingSetController.js",
                "~/App/common/controllers/VerificationSetController.js");

            bundle.Transforms.Clear();

            bundle.Transforms.Add(new ResxTransform());

            //OPTIONAL: uncomment this line after we fix Angular injection that prevents JavaScript minification. 
            //Minification is not really needed since we zip all resources
            //bundle.Transforms.Add(new JsMinify());

            return bundle;
        }

        private static Bundle AngularJsBundle()
        {
            var bundle = new ScriptBundle(AngularJsBundlePath).Include(
                "~/Content/js/jquery1.10.2/jquery-1.10.2.min.js",
                "~/Content/js/angular-1.3.3/angular.js",
                "~/Content/js/angular-1.3.3/angular-animate.js",
                "~/Content/js/angular-1.3.3/angular-resource.js",
                "~/Content/js/angular-1.3.3/angular-route.js",
                "~/Content/js/angular-1.3.3/angular-messages.js",
                "~/Content/js/angular-1.3.3/angular-sanitize.js",
                "~/Content/js/angular-1.3.3/ui-bootstrap-tpls-0.13.3/ui-bootstrap-tpls-0.13.3.min.js",
                "~/Content/js/angular-1.3.3/angular-loading-bar-0.6.0/loading-bar.min.js",
                "~/Content/js/kendo2014.2.716/kendo.all.min.js",
                "~/Content/js/kendo2014.2.716/kendo.angular.min.js",
                "~/Content/js/bootstrap3.3.5/bootstrap.min.js",
                "~/Content/js/Microsoft.AspNet.SignalR.JS.2.2.0/jquery.signalR-2.2.0.min.js");

            return bundle;
        }

        private static Bundle BootstrapCssBundle()
        {
            var bundle = new StyleBundle(BootstrapCssBundlePath)
                .Include("~/Content/css/bootstrap3.3.5/bootstrap.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/css/bootstrap3.3.5/bootstrap-theme.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/css/angular1.2.21/angular-loading-bar-0.5.0/loading-bar.min.css", new CssRewriteUrlTransform());

            return bundle;
        }

        private static Bundle KendoCssBundle()
        {
            var bundle = new StyleBundle(KendoCssBundlePath)
                .Include("~/Content/css/kendo2014.2.716/kendo.common.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/css/kendo2014.2.716/kendo.default.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/css/kendo2014.2.716/kendo.dataviz.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/css/kendo2014.2.716/kendo.dataviz.default.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/css/kendo2014.2.716/kendo.bootstrap.min.css", new CssRewriteUrlTransform());

            return bundle;
        }

    }
}