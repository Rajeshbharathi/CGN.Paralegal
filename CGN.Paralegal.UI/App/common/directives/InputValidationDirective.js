/* Angular 1.3 $validator use case
 * Angular Form validations and NgMessages - Reference docs: http://www.yearofmoo.com/2014/09/taming-forms-in-angularjs-1-3.html 
 * 
 * lnInputValidationSync - takes an object with custom validation methods which return a boolean value
 * eg: syncValidations ={
 *          "GreaterThanZero": function(value){ return value>0;}
 * }
 * 
 * lnInputValidationAsync - takes an object with custom validation methods which return a promise[eg. $http calls]   
 * eg: asyncValidations ={
 *          "UserNameAvailability": function(value){ return $http.get("api/users/availability/?name="value"");},
 *          "ProjectNameAvailability": function(value){ return $http.get("api/projects/availability/?name="value"");},
 * }
 * 
 * lnInputValidationPatterns - takes an object with custom validation patterns 
 * eg: patterns = {
 *          "noWhitespace": /^\S+$/,
 *          "allowNumeric":/\d+/,
 *          "AllowLowercase":/[a-z]+/,
 *          "uppercase":/[A-Z]+/, 
 *          "special":/\W+/
 *       }
 * 
 * lnInputValidationBatch - takes an array of custom sync validation methods only
 * eg: batchValidations =[greaterThanZero,checkAllPatterns]
 * 
 * 
 **/

(function () {
    "use strict";

    angular.module("app")
        .directive("lnInputValidation", function () {
            
                
            return {
                scope: {
                    validations: "=?lnInputValidationSync",
                    asyncValidations: "=?lnInputValidationAsync",
                    patterns: "=?lnInputValidationPatterns",
                    batch: "=?lnInputValidationBatch"
                },
                require: "ngModel",
                link: function (scope, element, attrs, ngModel) {

                    if (attrs.lnInputValidationBatch) {
                        ngModel.$validators.lnBatchValidations = function (value) {
                            var status = true;
                            angular.forEach(scope.batch, function (pattern) {
                                status = status && pattern.test(value);
                            });
                            return status;
                        };
                    }
                    if (attrs.lnInputValidationPatterns) {
                        angular.forEach(scope.patterns, function (pattern, name) {
                            ngModel.$validators[name] = function (modelValue, viewValue) {
                                var value = modelValue || viewValue;
                                return pattern.test(value);
                            };
                        });
                    }

                    if (attrs.lnInputValidationSync) {
                        angular.forEach(scope.validations, function (fn, name) {
                            ngModel.$validators[name] = function (modelValue, viewValue) {
                                var value = modelValue || viewValue;
                                return fn(value);
                            };
                        });
                    }

                    if (attrs.lnInputValidationAsync) {
                        angular.forEach(scope.asyncValidations, function (fn, name) {
                            ngModel.$asyncValidators[name] = function (modelValue, viewValue) {
                                var value = modelValue || viewValue;
                                return fn(value);
                            };
                        });
                    }
                }
            };
        });
}());