(function () {
    "use strict";
    function PredictionSetService(WebApiClientService) {
        function createPredictionSetJob(matterId, datasetId, projectId) {
            return WebApiClientService.post('/api/predictionset/' +
                matterId + '/' + datasetId + '/' + projectId);
        }
        var service = {

            createPredictionSetJob: createPredictionSetJob
        };
        return service;
    }
    angular.module('app').factory('PredictionSetService', PredictionSetService);
    PredictionSetService.$inject = ['WebApiClientService'];
}());