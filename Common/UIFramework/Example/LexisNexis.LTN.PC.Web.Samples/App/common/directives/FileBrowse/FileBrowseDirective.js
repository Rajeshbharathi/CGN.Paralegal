(function () {
    "use strict";

    angular.module("app")
        .directive('lnFilebrowse', function () {
            return {
                restrict: 'EA',
                scope: {
                    browsetype: '@lnFilebrowseType',
                    lnFilePath: '='
                },
                controller: 'FileBrowseController',
                controllerAs: 'FileBrowseController',
                replace: true,
                templateUrl: '/app/common/directives/FileBrowse/FileBrowseView.html'
            };
        });


}());
