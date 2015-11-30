(function () {
    "use strict";

    function AlertService($window) {
        function showAlert(msg, err) {
            $window.alert(msg);
        }
        var service = {
            showAlert: showAlert
        };
        return service;

    }
    angular.module('app')
        .factory('AlertService', AlertService);
    AlertService.$inject = ['$window'];
}());
