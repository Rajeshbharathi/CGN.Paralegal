(function () {
    "use strict";
    angular.module("app")
        .directive('lnUserPicker', function () {
            return {
                restrict: 'EA',
                scope: {
                    userPickerType: '@lnUserPickerType',
                    lnSelectedList: '='
                },
                controller: 'UserPickerController',
                controllerAs: 'UserPickerController',
                replace: true,
                templateUrl: '/app/common/directives/UserPicker/UserPickerView.html'
            };
        });
}());
