(function () {
    "use strict";

    angular.module("app").factory("AppStateService", ["WebApiClientService","$q", function (WebApiClientService,$q) {
        var appStateDeferred = $q.defer(),
        appStateResolved = false,
        appState = {},
        resolveAppState = function () {
            WebApiClientService.get("/api/appstate").then(function (response) {
                appState = response.data;
                appStateDeferred.resolve(appState);
                appStateResolved = true;
            }, function (err) {
                throw err;
            });
        },
        getAppState = function () {
            if (!appStateResolved) {
                resolveAppState();
            }
            return appStateDeferred.promise;
        };
        
        

        return {
            getAppState: getAppState,
            appState: function () { return appState;}
        };
    }]);
}());