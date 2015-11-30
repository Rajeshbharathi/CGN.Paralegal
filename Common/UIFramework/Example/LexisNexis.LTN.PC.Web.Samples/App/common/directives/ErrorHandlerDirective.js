/**
 * ErrorHandlerDirective displays the error messages stored in ErrorHandlerService in the UI.
 * It also allows to dismiss the error message displayed.
 */

(function () {
    "use strict";
    var ErrorHandlerDirective = function () {
        return {
            controller: function ExceptionController(ErrorHandlerService) {
                var vm = this;
                vm.ErrorHandlerService = ErrorHandlerService;

            },
            template: '<div alert ng-repeat="error in ErrorHandlerService.errors track by $index" type="danger" close="ErrorHandlerService.errors.splice($index,1)">{{error}}</div>',
            link: function (scope, elem, attrs, ctrl) {
                scope.ErrorHandlerService = ctrl.ErrorHandlerService;
            }
        }
    };
    angular
    .module('app')
    .directive('lnErrorHandler', ErrorHandlerDirective);
    
}());
