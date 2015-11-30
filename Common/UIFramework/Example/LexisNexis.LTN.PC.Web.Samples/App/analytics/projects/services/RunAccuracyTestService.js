(function () {
    "use strict";
    function RunAccuracyTestService(WebApiClientService) {
        function createRunAccuracyTest(projectId, controlsetId) {
            return WebApiClientService.post('/api/runaccuracytest/' + projectId + '/' + controlsetId);
        }
        var service = {
            createRunAccuracyTest: createRunAccuracyTest
        };
        return service;
    }
    angular.module('app')
        .factory('RunAccuracyTestService', RunAccuracyTestService);
    RunAccuracyTestService.$inject = ['WebApiClientService', '$q'];
}());
