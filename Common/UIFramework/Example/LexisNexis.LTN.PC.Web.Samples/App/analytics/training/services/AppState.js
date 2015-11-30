(function () {
    "use strict";

    function AppState($rootScope, WebApiClientService, $q) {

        var isLoaded = false,
            globalState = {
                currentMatter: null,
                username: null
            },
            service;

        function ensureLoaded() {
            var promise, deferred;
            if (isLoaded) {
                deferred = $q.defer();
                deferred.resolve(true);
                return deferred.promise;
            }
            promise = WebApiClientService.get('/api/appState').then(function (result) {
                globalState.currentMatter = result.data.currentMatter;
                globalState.username = result.data.username;
                isLoaded = true;
                return true;
            });
            return promise;
        }

        function setState(key, value) {
            if (!globalState.hasOwnProperty(key)) {
                throw "Key " + key + " does not exist in app state";
            }
            globalState[key] = value;
        }

        function getState(key) {
            if (!globalState.hasOwnProperty(key)) {
                throw "Key " + key + " does not exist in app state";
            }
            return globalState[key];
        }
        service = {
            ensureLoaded: ensureLoaded,
            setState: setState,
            getState: getState
        };
        return service;

    }

    angular.module('app').factory('AppState', AppState);
    AppState.$inject = ['$rootScope', 'WebApiClientService', '$q'];

}());