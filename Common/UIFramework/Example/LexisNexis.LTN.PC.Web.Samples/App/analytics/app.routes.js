(function () {
    "use strict";

    function config($routeProvider) {
        $routeProvider.
            when('/', { controller: 'ProjectListController', controllerAs: 'ProjectListController', templateUrl: '/App/analytics/projects/projectList/ProjectListView.html' }).
            when('/list', { controller: 'ProjectListController', controllerAs: 'ProjectListController', templateUrl: '/App/analytics/projects/projectList/ProjectListView.html' }).
            when('/dashboard', { controller: 'ProjectDashboardController', controllerAs: 'ProjectDashboardController', templateUrl: '/App/analytics/projects/dashboard/ProjectDashboardView.html' }).
            when('/wizard/:id', { controller: 'ProjectWizardController', controllerAs: 'ProjectWizardController', templateUrl: '/App/analytics/projects/projectWizard/ProjectWizardView.html' }).
            when('/wizard', { controller: 'ProjectWizardController', controllerAs: 'ProjectWizardController', templateUrl: '/App/analytics/projects/projectWizard/ProjectWizardView.html' }).
            otherwise({redirectTo:"/"});
            
    }

    angular.module('app').config(config);
    config.$inject = ['$routeProvider'];

}());
