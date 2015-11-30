(function () {
    "use strict";
    var config = function ($routeProvider) {
        $routeProvider.
            when('/', {
                controller: 'ReviewController',
                templateUrl: '/app/review/reviewer/ReviewView.html'
            });
    };
    angular.module('app').config(config);
    config.$inject = ['$routeProvider'];

}());