(function () {
    "use strict";
    function DocumentSetsListDirective() {
        return {
            restrict: 'EA',
            scope: {},
            controller: 'DocumentSetsListController',
            templateUrl: '/app/analytics/training/documentSets/DocumentSetsListDirective.html'
        };
    }
    angular.module('app')
        .directive('lnDocumentSetsList', DocumentSetsListDirective);


}());