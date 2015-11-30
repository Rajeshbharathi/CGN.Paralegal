/*
 * WebApiClientService provides common methods to call WebApi for global error handling purposes. 
 * This service will be decorated for global error handling, to avoid modifying internal $http functionalities.
 * 
 * Methods available:
 * WebApiClientService.get(url,config) - returns HttpPromise [same as $http]
 * WebApiClientService.post(url,config,data) - returns HttpPromise [same as $http]
 * WebApiClientService.delete(url,config) - returns HttpPromise [same as $http]
 * WebApiClientService.put(url,config,data) - returns HttpPromise [same as $http]
 * WebApiClientService.head(url,config) - returns HttpPromise [same as $http]
 * WebApiClientService.patch(url,config,data) - returns HttpPromise [same as $http]
 * WebApiClientService.jsonp(url,config) - returns HttpPromise [same as $http]
 */

(function () {
    "use strict";
    angular.module("app").factory("WebApiClientService", ["$http", function ($http) {

        var service = {};

        // Create short cut methods - get, post, put, delete, head, patch, jsonp
        function createShortMethods(names) {
            angular.forEach(names, function (name) {
                service[name] = function (url, config) {
                    config = config || {};
                    return $http(angular.extend(config, {
                        method: name,
                        url: url,
                        allowRequest: true
                    }));
                };
            });
        }

        function createShortMethodsWithData(names) {
            angular.forEach(names, function (name) {
                service[name] = function (url, data, config) {
                    config = config || {};
                    return $http(angular.extend(config, {
                        method: name,
                        url: url,
                        data: data,
                        allowRequest: true
                    }));
                };
            });
        }

        createShortMethods(["get", "delete", "head", "jsonp"]);
        createShortMethodsWithData(["post", "put", "patch"]);

        return service;

    }]);
}());