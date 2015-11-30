(function () {
    "use strict";

    function TrainingProgressDirective() {
        return {
            restrict: 'EA',
            scope: {},
            controller: 'TrainingProgressController',
            templateUrl: '/app/analytics/training/trainingProgress/TrainingProgressDirective.html'
        };
    }
    angular.module('app')
        .directive('lnTrainingProgress', TrainingProgressDirective);
}());
