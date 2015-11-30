(function () {
    "use strict";

    function TrainingDataService(WebApiClientService, $q) {
        function getTrainingResults() {
            return WebApiClientService.get('/api/trainingprogress')
                .then(function (result) {
                    return result.data;
                });
        }
        var service = {
            getTrainingResults: getTrainingResults
        };
        return service;
    }
    angular.module('app')
        .factory('TrainingDataService', TrainingDataService);
    TrainingDataService.$inject = ['WebApiClientService', '$q'];

}());
