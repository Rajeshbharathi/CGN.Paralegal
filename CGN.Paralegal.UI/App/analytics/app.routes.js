(function () {
    "use strict";

    var AppStateResolver = {
        "load": function(AppStateService) {
            return AppStateService.getAppState();
        }
    };

    function config($routeProvider) {
        //$routeProvider - extended to add global resolve for appState
        angular.extend({}, $routeProvider, {
            "when": function(path, route){
                route.resolve = (route.resolve) ? route.resolve : {};
                angular.extend(route.resolve, AppStateResolver);
                $routeProvider.when(path, route);
                return this;
            }
            }).when("/", {
                controller: "DashboardController",
                controllerAs: "DashboardController",
                templateUrl: "/app/analytics/projects/dashboard/AdminDashboardView.html"
            }).when("/olddashboard", {
                controller: "ProjectDashboardController",
                controllerAs: "ProjectDashboardController",
                templateUrl: "/app/analytics/projects/dashboard/ProjectDashboardView.html"
            }).when("/wizard", {
                controller: "ProjectWizardController",
                controllerAs: "ProjectWizardController",
                templateUrl: "/app/analytics/projects/projectWizard/ProjectWizardView.html"
            }).when("/wizard/:mode", {
                controller: "ProjectWizardController",
                controllerAs: "ProjectWizardController",
                templateUrl: "/app/analytics/projects/projectWizard/ProjectWizardView.html"
            }).when("/dashboard", {
                controller: "DashboardController",
                controllerAs: "DashboardController",
                templateUrl: "/app/analytics/projects/dashboard/AdminDashboardView.html"
            });
    }

    angular.module("app").config(config);
    config.$inject = ["$routeProvider"];

}());
