(function () {
    "use strict";
    function DocumentSetsDataService(WebApiClientService, $q) {
        function getDocumentSets(odataOptions) {
            return WebApiClientService.get('/odata/DocumentSets', {
                params: odataOptions
            });
        }
        var service = {
            getDocumentSets: getDocumentSets
        };
        return service;
    }
    angular.module('app').factory('DocumentSetsDataService', DocumentSetsDataService);
    DocumentSetsDataService.$inject = ['WebApiClientService', '$q'];

}());