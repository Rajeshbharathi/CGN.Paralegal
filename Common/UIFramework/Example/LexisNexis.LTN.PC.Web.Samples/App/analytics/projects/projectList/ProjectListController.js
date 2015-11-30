(function () {
    "use strict";

    function ProjectListController($location, ProjectsDataService) {
        var projectsDataSource = {},
            refreshGrid = function () {
                projectsDataSource.read();
            },
            defaultSort = [{
                field: "ProjectName",
                dir: "asc"
            }],
            vm = this,
            clear = function () {
                projectsDataSource.page(1);
                projectsDataSource.sort(defaultSort);
                projectsDataSource.filter([]);
                vm.refresh();
            },
            editProject = function (projectId) {
                $location.path('/wizard/' + projectId);
            },
            goToDashboard = function () {
                $location.path('/dashboard');
            },
            deleteProject = function (projectId) {
                ProjectsDataService.deleteProject(projectId)
                    .then(
                        function () {
                            refreshGrid();
                        },
                        function () {
                            //TODO Exception Handling
                        }
                    );
            },
            refresh = function () {
                refreshGrid();
            },
            configureProjectsGrid = function () {
                projectsDataSource = new kendo.data.DataSource({
                    type: "odata",
                    transport: {
                        read: function (options) {
                            var odataParams = kendo
                                .data.transports
                                .odata.parameterMap(options.data, "read");
                            ProjectsDataService.getProjectsOData(odataParams)
                                .success(function (result) {
                                    options.success(result);
                                })
                                .error(function () {
                                    //TODO Exception Handling
                                });
                        }
                    },
                    pageSize: 10,
                    sort: defaultSort,
                    serverPaging: true,
                    serverSorting: true,
                    serverFiltering: true,
                    schema: {
                        data: function (data) {
                            return data.value;
                        },
                        total: function (data) {
                            return data["odata.count"];
                        }
                    },
                    error: function () {
                        //TODO Exception Handling
                    }
                });
                vm.gridProjectsConfig = {
                    dataSource: projectsDataSource,
                    sortable: true,
                    pageable: {
                        pageSizes: [10, 25, 50, 100]
                    },
                    filterable: true,
                    columns: [{
                        field: "ProjectName",
                        title: vm.localized.ProjectName,
                        template: "<a id='check-all' " +
                            "ng-click='ProjectListController.goToDashboard()'>#: ProjectName#</a>"
                    }, {
                        field: "MatterName",
                        title: vm.localized.MatterName
                    }, {
                        field: "Source",
                        title: vm.localized.Source
                    }, {
                        field: "CreatedBy",
                        title: vm.localized.CreatedBy
                    }, {
                        field: "CreatedOn",
                        title: vm.localized.CreatedOn,
                        template: "#= kendo.toString(kendo.parseDate(CreatedOn), 'G') #",
                        filterable: {
                            ui: "datetimepicker"
                        }
                    }, {
                        field: "Documents",
                        title: vm.localized.Documents,
                        filterable: {
                            ui: "numerictextbox"
                        }
                    }, {
                        field: "ProjectId",
                        filterable: false,
                        sortable: false,
                        title: vm.localized.Actions,
                        template: "<a class='btn btn-default btn-sm' id='check-all'" +
                            " ng-click='ProjectListController.editProject(#: ProjectId #)'>" + vm.localized.Edit +
                            "</a> <a class='btn btn-default btn-sm' id='check-all' " +
                            " ln-confirm-click='Are you sure you want to delete this record?' " +
                            " ng-click='ProjectListController.deleteProject(#: ProjectId #)'>" +
                            vm.localized.Delete + "</a>"
                    }]
                };
            },
            init = function () {
                vm.localized = ProjectResources;
                vm.pagetitle = vm.localized.Pagetitle;
                vm.breadcrumbs = [vm.localized.System, vm.localized.Analytics];
                vm.editProject = editProject;
                vm.deleteProject = deleteProject;
                vm.goToDashboard = goToDashboard;
                vm.clear = clear;
                vm.refresh = refresh;
                configureProjectsGrid();
            };
        init();
    }
    angular.module('app')
        .controller('ProjectListController', ProjectListController);
    ProjectListController.$inject = ['$location',
        'ProjectsDataService'];

}());
