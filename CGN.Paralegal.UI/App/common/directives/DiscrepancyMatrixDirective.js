(function () {
    "use strict";
    var app, DiscrepancyMatrixDirective;

    DiscrepancyMatrixDirective = ["DashboardService", "Utils", "Constants", function (DashboardService, Utils, Constants) {
        return {
            restrict: "A",
            scope: {
                discrepancies: "=lnDiscrepancyMatrixData",
                set: "=lnDiscrepancyMatrixSet"
            },
            templateUrl: "/App/common/directives/DiscrepancyMatrixDirectiveView.html",
            link: function (scope) {

                scope.runCalculations = function () {
                    scope.calculatedPrecision = (parseInt(scope.discrepancies[0][0], 10) / parseInt((scope.discrepancies[0][0]+scope.discrepancies[1][0]), 10)) * 100;
                    scope.calculatedRecall = (parseInt((scope.discrepancies[0][0]), 10) / parseInt((scope.discrepancies[0][0]+scope.discrepancies[0][1]), 10)) * 100;
                };

                scope.localized = Constants.getProjectResources();
                scope.runCalculations();

                scope.$watch(function () {
                    return scope.discrepancies;
                    }, function (newValue, oldValue) {
                    if (!!newValue && newValue !== oldValue) {
                        scope.runCalculations();
                    }
                }, true);



                scope.viewTruePositivesList = function () {
                    var binderId = (scope.set.BinderId === null || angular.isUndefined(scope.set.BinderId))? 0 : scope.set.BinderId;
                    var setType = scope.set.Type.replace(/['"]+/g, "");
                    var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/" + setType +
                        "/1/"+binderId+"/from_admin/to_doclist/TruePositives";
                    var PCUrl = "/app/review/approot#/analysissets/" + setType + 
                        "/1/"+binderId+"/from_admin/to_doclist/TruePositives";
                    Utils.loadPage(EVUrl, PCUrl);
                };
                scope.viewFalseNegativesList = function () {
                    var binderId = (scope.set.BinderId === null || angular.isUndefined(scope.set.BinderId))? 0 : scope.set.BinderId;
                    var setType = scope.set.Type.replace(/['"]+/g, "");
                    var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/" + setType + 
                        "/1/"+binderId+"/from_admin/to_doclist/FalseNegatives";
                    var PCUrl = "/app/review/approot#/analysissets/" + setType + 
                        "/1/"+binderId+"/from_admin/to_doclist/FalseNegatives";
                    Utils.loadPage(EVUrl, PCUrl);
                };
                scope.viewFalsePositivesList = function () {
                    var binderId = (scope.set.BinderId === null || angular.isUndefined(scope.set.BinderId))? 0 : scope.set.BinderId;
                    var setType = scope.set.Type.replace(/['"]+/g, "");
                    var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/" + setType +
                        "/1/"+binderId+"/from_admin/to_doclist/FalsePositives";
                    var PCUrl = "/app/review/approot#/analysissets/" + setType + 
                        "/1/"+binderId+"/from_admin/to_doclist/FalsePositives";
                    Utils.loadPage(EVUrl, PCUrl);
                };
                scope.viewTrueNegativesList = function () {
                    var binderId = (scope.set.BinderId === null || angular.isUndefined(scope.set.BinderId))? 0 : scope.set.BinderId;
                    var setType = scope.set.Type.replace(/['"]+/g, "");
                    var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/" + setType +
                        "/1/"+binderId+"/from_admin/to_doclist/TrueNegatives";
                    var PCUrl = "/app/review/approot#/analysissets/" + setType + 
                        "/1/"+binderId+"/from_admin/to_doclist/TrueNegatives";
                    Utils.loadPage(EVUrl, PCUrl);
                };
            }
        };
    }];

    app = angular.module("app");

    app.directive("lnDiscrepancyMatrix", DiscrepancyMatrixDirective);

}());