(function () {
    "use strict";

    function DataSetsDataService(WebApiClientService) {
        function getDataSets() {
            return WebApiClientService.get('/api/datasets')
                .then(function (result) {
                    return result.data;
                });
        }

        function getDataSetsForMatterId(matterId) {
            return WebApiClientService.get('/api/datasets/matterid/' + matterId)
                .then(function (result) {
                    return result.data;
                });
        }
        var service = {
            getDataSets: getDataSets,
            getDataSetsForMatterId: getDataSetsForMatterId
        };
        return service;
    }
    angular.module('app')
        .factory('DataSetsDataService', DataSetsDataService);
    DataSetsDataService.$inject = ['WebApiClientService'];
}());
