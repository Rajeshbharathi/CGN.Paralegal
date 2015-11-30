(function () {
    "use strict";
    var app, NavigatorDirective;
    NavigatorDirective = function () {
        return {
            restrict: "EA",
            scope: {
                totalItems: "=lnNavigatorTotal",
                label: "@lnNavigatorLabel",
                update: "=lnNavigatorUpdate",
                reset: "=?lnNavigatorReset",
                isAdvanced: "=?lnNavigatorAdvanced"
            },
            templateUrl: "/App/common/directives/navigatorDirectiveView.html",
            link: function (scope, element) {
                var width, goTo, validate;
                if (scope.isAdvanced) {
                    width = 280;
                } else {
                    width = 155;
                }
                scope.$watch("totalItems", function (val, old) {
                    if (val !== old) {
                        var num, parentWidth, newWidth;
                        num = window.isNaN(parseInt(val, 10)) ? 2 : val.toString().length;
                        if (num < 1) {
                            num = 1;
                        }
                        parentWidth = element.parent().width();
                        newWidth = width + 6 * num;
                        newWidth = newWidth < parentWidth ? newWidth : parentWidth;
                        element.css("width", newWidth + "px");
                    }
                });

                scope.currentRecordNumber = 1;
                validate = function (num) {
                    if (num < 1 || num > scope.totalItems) {
                        var error = new Error();
                        error.name = "Validation";
                        error.message = "Please provide a value between 0 and " + scope.totalItems;
                        throw error;
                    }
                };
                goTo = function (num) {
                    validate(num);
                    if (!(num > scope.totalItems || num < 0)) {
                        scope.currentRecordNumber = num;
                        scope.update(num);
                    }
                };
                if (scope.reset) {
                    scope.$watch("reset.value", function (newVal) {
                        if (newVal) {
                            scope.currentRecordNumber = 1;
                            scope.reset.value = false;
                        }
                    });
                }
                scope.next = function () {
                    if (scope.currentRecordNumber < scope.totalItems) {
                        goTo(scope.currentRecordNumber + 1);
                    }
                };
                scope.last = function () {
                    if (scope.currentRecordNumber < scope.totalItems) {
                        goTo(scope.totalItems);
                    }
                };
                scope.prev = function () {
                    if (scope.currentRecordNumber <= scope.totalItems && scope.currentRecordNumber > 1) {
                        goTo(scope.currentRecordNumber - 1);
                    }
                };
                scope.first = function () {
                    if (scope.currentRecordNumber <= scope.totalItems && scope.currentRecordNumber > 1) {
                        goTo(1);
                    }
                };
                scope.disableNext = function () {
                    return scope.currentRecordNumber === scope.totalItems;
                };
                scope.disablePrev = function () {
                    return scope.currentRecordNumber === 1;
                };
                scope.submitForm = function ($event) {
                    var whichKey;
                    whichKey = $event.keyCode;
                    if (whichKey === 13) {
                        if (!(scope.currentRecordNumber > scope.totalItems || isNaN(scope.currentRecordNumber))) {
                            scope.update(scope.currentRecordNumber);
                        }
                    }
                };
            }
        };
    };
    app = angular.module("app");
    app.directive("lnNavigator", NavigatorDirective);
}());