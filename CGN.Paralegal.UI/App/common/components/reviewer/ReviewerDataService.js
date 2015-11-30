(function () {
    'use strict';
    var ReviewerDataService = function ($injector) {
        var modelFactory = {};
        modelFactory.init = function (config) {
            var service = $injector.get(config.service);
            modelFactory.getConfiguration = function () {
                return config;
            };
            angular.forEach(service, function (value, key) {
                modelFactory[key] = value;
            });
        };
        return modelFactory;

    },
        app = angular.module('app');
    app.service('ReviewerDataService', ReviewerDataService);
    ReviewerDataService.$inject = ['$injector'];
}());
