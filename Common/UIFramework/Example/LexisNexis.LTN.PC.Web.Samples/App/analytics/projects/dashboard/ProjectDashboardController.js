(function () {
"use strict";

    function ProjectDashboardController($scope, $modal, ProjectsDataService, AlertService) {
        var vm = this;

        function bindDetails() {
            ProjectsDataService.getProjectSummary(1)
                .then(function (project) {
                    vm.sourcedataset = project.SourceDataSet;
                    vm.totaldocs = project.TotalDocs;
                    vm.crossreffield = project.CrossRefField;
                    vm.controlset = project.ControlSet;
                    vm.dbserver = project.DatabaseServer;
                    vm.analyticsserver = project.AnalyticsServer;
                }, function (error) {
                    AlertService.showAlert('Error retrieving project summary: ' + error.status + ' ' +
                        error.statusText);
                });
                }

        function showAlert(alert, alertType) {
            vm.alerts.push({
                msg: alert,
                type: alertType
            });
        }

        function closeAlert(index) {
            vm.alerts.splice(index, 1);
        }

        $scope.showNewControlSetModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/CreateControlSetView.html',
                controller: 'CreateControlSetController',
                controllerAs: 'CreateControlSetController',
                size: 'smx'
            });
            modalInstance.result.then(function () {
                showAlert('New Control Set created successfully', 'success');
            }, function () {
                return;
            });
        };

        $scope.showNewQCSetModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/CreateQCSetView.html',
                controller: 'CreateQCSetController',
                controllerAs: 'CreateQCSetController',
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                showAlert('New QC Set created successfully', 'success');
            }, function () {
                //modal was canceled
            });
        };

        $scope.showNewPredictionSetModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/CreatePredictionSetView.html',
                controller: 'CreatePredictionSetController',
                controllerAs: 'CreatePredictionSetController',
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                showAlert('New Prediction Set job is scheduled', 'success');
            }, function () {
                //modal was canceled
            });
        };


        $scope.showRunAccuracyTestModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/RunAccuracyTestView.html',
                controller: 'RunAccuracyTestController',
                controllerAs: 'RunAccuracyTestController',
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                showAlert('Run Accuracy Test is scheduled', 'success');
            }, function () {
                //modal was canceled
            });
        };
        function init() {
            vm.localized = ProjectDashboardResources;
            vm.breadcrumbs = ['System', 'Matter 4', 'AR - DS4'];
            vm.pagetitle = 'Sample Dashboard';
            vm.alerts = [];
            vm.enablesettings = false;
            vm.closeAlert = closeAlert;
            vm.showNewControlSetModal = $scope.showNewControlSetModal;
            vm.doSomething = function () {
                AlertService.showAlert('Do something useful');
            };
            bindDetails();
        }
        init();
    }
    angular.module('app')
        .controller('ProjectDashboardController', ProjectDashboardController);
    ProjectDashboardController.$inject = ['$scope', '$modal', 'ProjectsDataService', 'AlertService'];

}());
