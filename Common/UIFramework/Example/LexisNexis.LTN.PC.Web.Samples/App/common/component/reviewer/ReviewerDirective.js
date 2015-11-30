(function () {
    'use strict';
    var ReviewerDirective = function () {
        return {
            restrict: 'EA',
            priority: 1000,
            terminal: true,
            controller: 'ReviewerDirectiveController',
            controllerAs: "ReviewerDirectiveController",
            templateUrl: "/App/common/component/reviewer/ReviewerDirectiveView.html"
        };
    },
        app = angular.module('app');
    app.directive('lnReviewer', ReviewerDirective);
}());

