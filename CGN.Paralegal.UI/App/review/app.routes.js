(function () {
    "use strict";

    var AppStateResolver = {
        "load": function (AppStateService) {
            return AppStateService.getAppState();
        }
    };

    var config = function ($routeProvider) {
        //$routeProvider.
        angular.extend({}, $routeProvider, {
                "when": function (path, route) {
                    route.resolve = (route.resolve) ? route.resolve : {};
                    angular.extend(route.resolve, AppStateResolver);
                    $routeProvider.when(path, route);
                    return this;
                }
            }).when("/", {
                controller: "DashboardController",
                controllerAs: "DashboardController",
                templateUrl: "/App/review/dashboard/ReviewerDashboardView.html"
            }).
            when("/olddashboard", {
                controller: "ReviewDashboardController",
                controllerAs: "ReviewDashboardController",
                templateUrl: "/App/review/dashboard/ReviewDashboardView.html"
            }).when("/analysissets/:setType/:setRound/:setId/:from_module/:to_view/:filterValue", {
                controller: "ReviewSetController",
                controllerAs: "ReviewSetController",
                templateUrl: "/App/review/reviewset/ReviewSetView.html"
            }).when("/dashboard", {
                controller: "DashboardController",
                controllerAs: "DashboardController",
                templateUrl: "/App/review/dashboard/ReviewerDashboardView.html"
            });
    };
    angular.module("app").config(config);
    config.$inject = ["$routeProvider"];

}());