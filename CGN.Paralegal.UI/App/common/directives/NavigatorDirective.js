(function () {
    "use strict";
    var app, NavigatorDirective;
    NavigatorDirective = ["Constants", function (Constants) {
        return {
            restrict: "EA",
            scope: {
                document: "=lnNavigatorDocument",
                label: "@lnNavigatorLabel",
                update: "=lnNavigatorUpdate",
                reset: "=?lnNavigatorReset",
                isAdvanced: "=?lnNavigatorAdvanced"
            },
            templateUrl: "/App/common/directives/navigatorDirectiveView.html",
            link: function (scope) {
                var goTo, lastRecordedNumber;
                scope.isSubmitted = false;
                scope.localized = Constants.getReviewResources();
                scope.currentRecordNumber = 1;
                scope.totalItems = scope.document.TotalDocumentCount;

                lastRecordedNumber = 0;

                scope.$watch("document", function (newValue, oldValue) {
                    if (newValue !== oldValue) {
                        scope.currentRecordNumber = newValue.DocumentIndexId;
                        scope.totalItems = newValue.TotalDocumentCount;
                        scope.isSubmitted = false;
                        scope.isRangeError = false;
                    }
                });

                scope.isRequired = function () {
                    if (scope.isSubmitted) {
                        
                        return isNaN(parseInt(lastRecordedNumber,10));
                    }
                    return false;
                };

                scope.validate = function (num) {
                    num = parseInt(num, 10);
                    return !isNaN(num) && num > 0 && num <(scope.totalItems+1);
                };
                goTo = function (num) {
                    if (scope.validate(num)) {
                        scope.isSubmitted = false;
                        scope.isRangeError = false;
                        scope.update(num,null);
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
                    if (!scope.disableNext()) {
                        goTo(scope.currentRecordNumber +1);
                }

                };
                scope.last = function () {
                    if (!scope.disableNext()) {
                        goTo(scope.totalItems);
                }

                };
                scope.prev = function () {
                    if (!scope.disablePrev()) {
                        goTo(scope.currentRecordNumber -1);
                }

                };
                scope.first = function () {
                    if (!scope.disablePrev()) {
                        goTo(1);
                }

                };
                scope.disableNext = function () {
                    var isViewInValid = scope.validate(scope.currentRecordNumber);
                    var isModelInValid = scope.currentRecordNumber === scope.totalItems;
                    return isModelInValid || !isViewInValid;

                };
                scope.disablePrev = function () {
                    var isViewInValid = scope.validate(scope.currentRecordNumber);
                    var isModelInValid = scope.currentRecordNumber === 1;
                    return isModelInValid || !isViewInValid;
                };
                scope.submitForm = function ($event) {
                    var whichKey;
                    whichKey = $event.keyCode;
                    if (whichKey === 13) {
                        scope.isSubmitted = true;
                        lastRecordedNumber = scope.navigatorForm.currentRecordNumber.$viewValue;
                        scope.isRangeError = !scope.isRequired() && !scope.validate(lastRecordedNumber);
                        goTo(scope.navigatorForm.currentRecordNumber.$viewValue);
                }
                };
            }
        };
    }];
    app = angular.module("app");
    app.directive("lnNavigator", NavigatorDirective);
}());