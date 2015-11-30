(function () {
    "use strict";

    function ProjectWizardController($scope, $routeParams, $location, ProjectsDataService, ProjectModel,
        AlertService) {
        var vm = this,
            projectId = $routeParams.id;

        function isNew() {
            return (projectId === 'undefined');
        }

        function bindProject() {
            vm.isLoading = false;
            if (isNew()) {
                vm.project = new ProjectModel();
                vm.pagetitle = 'New Project';
            } else {
                vm.isLoading = true;
                ProjectsDataService.getProject(1)
                    .then(function (project) {
                        vm.project = project;
                        vm.isLoading = false;
                    });
                vm.pagetitle = 'Edit Project';
            }
        }

        function setTab(tabIndex) {
            vm.activetab.active = false;
            vm.activetab = vm.tabs[tabIndex];
            vm.activetab.active = true;
            vm.submitted = false;
        }

        function next(currentForm) {
            if (currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            currentForm.$setPristine();
            var index = vm.tabs.indexOf(vm.activetab);
            if (index + 1 < vm.tabs.length) {
                vm.activetab.active = false;
                vm.activetab = vm.tabs[index + 1];
                vm.activetab.active = true;
            }
        }

        function back(currentForm) {
            if (currentForm.$dirty && currentForm.$invalid) {
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            currentForm.$setPristine();
            var index = vm.tabs.indexOf(vm.activetab);
            if (index > 0) {
                vm.activetab.active = false;
                vm.activetab = vm.tabs[index - 1];
                vm.activetab.active = true;
            }
        }

        function sampleCustomValidator(marginOfError) {
            return marginOfError > 2;
        }

        function save(currentForm) {
            if (currentForm.$dirty && currentForm.$invalid) {
                return;
            }
            currentForm.$setPristine();
            if (isNew()) {
                ProjectsDataService.addProject(vm.project)
                    .then(function (data, status) {
                        $location.path('/list');
                    }, function (error) {
                        AlertService.showAlert('Error adding project: ' + error.status + ' ' + error.statusText);
                    });
            } else {
                ProjectsDataService.updateProject(vm.project)
                    .then(function (data, status) {
                        $location.path('/list');
                    }, function (error) {
                        AlertService.showAlert('Error updating project: ' + error.status + ' ' + error.statusText);
                    });
            }
        }

        function init() {
            vm.setTab = setTab;
            vm.next = next;
            vm.back = back;
            vm.save = save;
            vm.calculate = calculate;
            vm.sampleCustomValidator = sampleCustomValidator;
            vm.localized = ProjectWizardResources;
            vm.totalDocs = "67,890";
            vm.breadcrumbs = ['System', 'Matter 4'];
            vm.tabs = [{
                title: 'Settings',
                active: true,
                disabled: false
            }, {
                title: 'Controls',
                active: false,
                disabled: false
            }, {
                title: 'Example',
                active: false,
                disabled: false
            }, {
                title: 'Summary',
                active: false,
                disabled: false
            }];
            vm.activetab = vm.tabs[0];
            vm.tagoptions = [{
                text: 'Extracted Text Tag',
                value: 'Extracted'
            }, {
                text: 'Other',
                value: 'Other'
            }];
            vm.savedsearchoptions = [{
                text: 'Sample Search',
                value: 1
            }, {
                text: 'Another Search',
                value: 2
            }];
            vm.confidenceoptions = [{
                text: '75%',
                value: 75
            }, {
                text: '85%',
                value: 85
            }, {
                text: '95%',
                value: 95
            }];
            vm.marginoferrorsoptions = [{
                text: '2.0%',
                value: 2.0
            }, {
                text: '2.5%',
                value: 2.5
            }, {
                text: '3.0%',
                value: 3.0
            }, {
                text: '3.5%',
                value: 3.5
            }];
            vm.custodianfields = [{
                text: 'Other',
                value: 'Other'
            }, {
                text: 'Custodian',
                value: 'Custodian'
            }];
            bindProject();
        }

        vm.ProjectNameValidations = {
            async: {
                "ProjectNameAvailability": function (value) { return ProjectsDataService.checkUniqueProjectName(value); }
            }
        }

        vm.ProjectDescriptionValidations = {
            patterns: {
                "whitespace": /^\S+$/,
                "numeric": /\d+/,
                "lowercase": /[a-z]+/,
                "uppercase": /[A-Z]+/,
                "special": /\W+/
            }
        }


        var calculate = function () {
            ProjectsDataService.calculateSampleSize().then(function (data) {
                vm.project.sampleSize = data.size;
            });
        }
        init();
    }

    angular.module('app')
        .controller('ProjectWizardController', ProjectWizardController);
    ProjectWizardController.$inject = ['$scope', '$routeParams', '$location', 'ProjectsDataService', 'ProjectModel',
        'AlertService'];
}());
