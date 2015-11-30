(function () {
    "use strict";
    function MattersDataService(WebApiClientService) {
        function getMatters() {
            return WebApiClientService.get('/api/matters').then(function (result) {
                return result.data;
            });
        }
        var service = {
            getMatters: getMatters
        };
        return service;

    }

    angular.module('app').factory('MattersDataService', MattersDataService);
    MattersDataService.$inject = ['WebApiClientService'];
}());