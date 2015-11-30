(function () {
    "use strict";
    function ControlSetService(WebApiClientService) {
        function getSampleSize(matterId, datasetId, controlset) {
            return WebApiClientService.post('/api/controlset/samplesize/'
                + matterId + '/' + datasetId, controlset);
        }
        function addControlset(matterId, datasetId, projectId, controlset) {
            return WebApiClientService.post('/api/controlset/'
                + matterId + '/' + datasetId + '/' + projectId, controlset);
        }
        var service = {
            getSampleSize: getSampleSize,
            addControlset: addControlset
        };
        return service;
    }
    angular.module('app').factory('ControlSetService', ControlSetService);
    ControlSetService.$inject = ['WebApiClientService'];
}());