(function () {
    "use strict";
    //Adapted from https://github.com/technovert/angular-validation
    angular.module('app')
        .directive('updateOnBlur', function () {
            return {
                restrict: 'A',
                require: 'ngModel',
                priority: '100',
                /*ignore jslint start*/
                link: function (scope, elm, attr, ngModelCtrl) {
                    /*ignore jslint end*/
                    if (attr.type === 'radio' || attr.type === 'checkbox') {
                        return;
                    }
                    elm.unbind('input')
                        .unbind('keydown')
                        .unbind('change');
                    elm.bind('blur', function () {
                        scope.$apply(function () {
                            ngModelCtrl.$setViewValue(elm.val());
                        });
                    });
                }
            };
        })
        //from http://angular-ui.github.io/ui-utils/#/validate
        .directive('customValidate', function () {
            return {
                restrict: 'A',
                require: 'ngModel',
                link: function (scope, elm, attrs, ctrl) {
                    var validateFn, validators = {},
                        validateExpr = scope.$eval(attrs.customValidate);
                    if (!validateExpr) {
                        return;
                    }
                    if (angular.isString(validateExpr)) {
                        validateExpr = {
                            validator: validateExpr
                        };
                    }
                    angular.forEach(validateExpr, function (exprssn, key) {
                        validateFn = function (valueToValidate) {
                            var expression = scope.$eval(exprssn, {
                                '$value': valueToValidate
                            });
                            if (angular.isObject(expression) && angular.isFunction(expression.then)) {
                                // expression is a promise
                                expression.then(function () {
                                    ctrl.$setValidity(key, true);
                                }, function () {
                                    ctrl.$setValidity(key, false);
                                });
                                return valueToValidate;
                            }
                            if (expression) {
                                // expression is true
                                ctrl.$setValidity(key, true);
                                return valueToValidate;
                            }
                            // expression is false
                            ctrl.$setValidity(key, false);
                            return valueToValidate;
                        };
                        validators[key] = validateFn;
                        ctrl.$formatters.push(validateFn);
                        ctrl.$parsers.push(validateFn);
                    });

                    function apply_watch(watch) {
                        //string - update all validators on expression change
                        if (angular.isString(watch)) {
                            scope.$watch(watch, function () {
                                angular.forEach(validators, function (validatorFn) {
                                    validatorFn(ctrl.$modelValue);
                                });
                            });
                            return;
                        }
                        //array - update all validators on change of any expression
                        if (angular.isArray(watch)) {
                            angular.forEach(watch, function (expression) {
                                scope.$watch(expression, function () {
                                    angular.forEach(validators, function (validatorFn) {
                                        validatorFn(ctrl.$modelValue);
                                    });
                                });
                            });
                            return;
                        }
                        //object - update appropriate validator
                        if (angular.isObject(watch)) {
                            angular.forEach(watch, function (expression, validatorKey) {
                                //value is string - look after one expression
                                if (angular.isString(expression)) {
                                    scope.$watch(expression, function () {
                                        validators[validatorKey](ctrl.$modelValue);
                                    });
                                }
                                //value is array - look after all expressions in array
                                if (angular.isArray(expression)) {
                                    angular.forEach(expression, function (intExpression) {
                                        scope.$watch(intExpression, function () {
                                            validators[validatorKey](ctrl.$modelValue);
                                        });
                                    });
                                }
                            });
                        }
                    }
                    // Support for ui-validate-watch
                    if (attrs.customValidateWatch) {
                        apply_watch(scope.$eval(attrs.customValidateWatch));
                    }
                }
            };
        });
}());