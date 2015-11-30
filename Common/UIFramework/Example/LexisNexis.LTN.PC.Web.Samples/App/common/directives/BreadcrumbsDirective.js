
(function () {
    "use strict";
    angular.module("app")
    .directive('lnBreadcrumbs', function () {
        return {
            scope: {
                breadcrumbs: "="
            },
            restrict: 'EA',
            replace: true,
            templateUrl: '/app/common/directives/breadcrumbsDirective.html',
            link: function (scope, element, attrs, ctrl) {
                //nothing to do here 
            }
        };
    });
 })();