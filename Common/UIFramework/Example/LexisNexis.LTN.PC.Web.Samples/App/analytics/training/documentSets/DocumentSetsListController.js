(function () {
    "use strict";

    function DocumentSetsListController($scope, MattersDataService, DataSetsDataService, DocumentSetsDataService,
        AlertService) {
        var vm = $scope, //this;   //hack, controllerAs is not working correctly here
            documentSetsDataSource = {},
            defaultSort = {};
        function init() {
            vm.localized = DocumentSetsListResources;
            vm.clear = clear;
            vm.refresh = refresh;
            vm.typeChanged = typeChanged;
            vm.statusChanged = statusChanged;
            bindTypes();
            bindStatuses();
            configureDocumentSetsGrid();
            }
        init();
        function configureDocumentSetsGrid() {
            defaultSort = [{field: "Name",dir: "asc"}];
            documentSetsDataSource = new kendo.data.DataSource({
                type: "odata",
                transport: {
                    read: function (options) {
                        var odataParams = kendo.data.transports.odata.parameterMap(options.data,"read");
                        DocumentSetsDataService.getDocumentSets(odataParams)
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
                error: function (error) {
                    AlertService.showAlert('Error retrieving document sets: ' + error.status +
                        ' ' + error.statusText);
                }
            });
            vm.gridDocSetsConfig = {
                dataSource: documentSetsDataSource,
                sortable: true,
                pageable: true,
                filterable: true,
                columns: [{
                    field: "Name",
                    title: "Name"
                }, {
                    field: "Type",
                    title: "Type"
                }, {
                    field: "Documents",
                    title: "Docs",
                    filterable: {
                        ui: "numerictextbox"
                    }
                }, {
                    field: "Reviewed",
                    title: "Reviewed",
                    filterable: {
                        ui: "numerictextbox"
                    }
                }, {
                    field: "NotReviewed",
                    title: "Not Reviewed",
                    filterable: {
                        ui: "numerictextbox"
                    }
                }, {
                    field: "Status",
                    title: "Status"
                }]
            };
        }

        function bindTypes() {
            MattersDataService.getMatters()
                .then(function (data) {
                    vm.types = data;
                }, function (error) {
                    AlertService.showAlert('Error retrieving matters: ' + error.status + ' ' + error.statusText);
                });
        }

        function bindStatuses() {
            DataSetsDataService.getDataSets()
                .then(function (data) {
                    vm.statuses = data;
                }, function (error) {
                    AlertService.showAlert('Error retrieving datasets: ' + error.status + ' ' + error.statusText);
                });
        }

        function refreshGrid() {
            documentSetsDataSource.read();
        }

        function refresh() {
            refreshGrid();
        }

        function typeChanged() {
            refreshGrid();
        }

        function statusChanged() {
            refreshGrid();
        }

        function clear() {
            vm.selectedType = null;
            vm.selectedStatus = null;
            documentSetsDataSource.page(1);
            documentSetsDataSource.sort(defaultSort);
            documentSetsDataSource.filter([]);
            refresh();
        }

        
    }
    angular.module('app')
        .controller('DocumentSetsListController', DocumentSetsListController);
    DocumentSetsListController.$inject = ['$scope', 'MattersDataService', 'DataSetsDataService',
        'DocumentSetsDataService', 'AlertService'];
}());
