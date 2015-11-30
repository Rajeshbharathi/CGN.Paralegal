(function () {
    "use strict";
    function QCSetService(WebApiClientService) {
        function getSampleSize(matterId, datasetId, qcSet) {
            return WebApiClientService.post('/api/qcset/samplesize/'
                + matterId + '/' + datasetId, qcSet);
        }
        function addQCset(matterId, datasetId, projectId, qcSet) {
            return WebApiClientService.post('/api/qcset/'
                + matterId + '/' + datasetId + '/' + projectId, qcSet);
        }
        var service = {
            getSampleSize: getSampleSize,
            addQCset: addQCset
        };
        return service;
    }
    angular.module('app').factory('QCSetService', QCSetService);
    QCSetService.$inject = ['WebApiClientService'];
}());