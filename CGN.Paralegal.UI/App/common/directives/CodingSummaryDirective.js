(function () {
    "use strict";
    var app, CodingSummaryDirective;
    CodingSummaryDirective = function () {
        return {
            restrict: 'EA',
            scope: {
                configInfo: '=?config',
                navigate: '&?',
                showToggle: '=?',
                toggleHandler: '&?'
                
            },
            link: function(scope, element, attr) {
                if (scope.Items !== null && scope.configInfo.Items !== null) {

                    scope.$watch(function () {
                        return scope.configInfo;
                    }, function (newValue, oldValue) {
                        if (!!newValue && newValue !== oldValue) {
                            scope.runCalculations();
                        }
                    }, true);

                    scope.runCalculations = function () {
                        var total = 0;
                        angular.forEach(scope.configInfo.Items, function (item) {
                            total += item.Data;
                        });

                        angular.forEach(scope.configInfo.Items, function (item) {
                            item.Percent = (item.Data / total) * 100;
                            item.Width = item.Percent + "%";
                        });
                    };

                    scope.runCalculations();
                    angular.forEach(scope.configInfo.Items, function (item) {
                        if (item.Type ==="Relevant" ) {
                            item.ClassName = "relevant";
                            item.Icon = "fa fa-check-circle";
                            item.Text = "Relevant";
                        } else if (item.Type === "Not_Relevant") {
                            item.ClassName = "not-relevant";
                            item.Icon = "fa fa-times-circle";
                            item.Text = "Not Relevant";
                        } else if (item.Type === "Skipped") {
                            item.ClassName = "skipped";
                            item.Icon = "fa fa-arrow-circle-right";
                            item.Text = "Skipped";
                        }
                        else if (item.Type === "Not_Coded") {
                            item.ClassName = "not-coded";
                            item.Icon = "fa fa-minus-circle";
                            item.Text = "Not Coded";
                        }

                    });

                    scope.viewToggled = false;
                    scope.toggleView = function () {
                        scope.viewToggled = !scope.viewToggled;

                        //call the handler and tell it to toggle view (true or false)
                        scope.toggleHandler({ toggleView: scope.viewToggled });
                    };

                }
            },
            templateUrl: "/App/common/directives/CodingSummaryDirectiveView.html",
        };
    };

    app = angular.module("app");

    app.directive("lnCodingSummary", CodingSummaryDirective);

}());