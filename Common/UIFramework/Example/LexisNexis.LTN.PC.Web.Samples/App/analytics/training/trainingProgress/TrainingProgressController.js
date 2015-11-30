(function () {
    "use strict";
    function TrainingProgressController($scope, TrainingDataService, AlertService) {
        var progressDataSource = {},
            vm = $scope; //this;   //todo: hack, controllerAs is not working correctly here

        function configureChart() {
            progressDataSource = new kendo.data.DataSource({
                data: vm.chartData
            });
            vm.chartTrainingProgress = {
                dataSource: progressDataSource,
                seriesDefaults: {
                    type: "line",
                    errorBars: {
                        value: "percentage(20)"
                    }
                },
                series: [{
                    field: "Precision",
                    name: "Precision"
                }, {
                    field: "Recall",
                    name: "Recall"
                }, {
                    field: "Accuracy",
                    name: "Accuracy (F1)"
                }],
                valueAxis: {
                    labels: {
                        format: "{0}%"
                    },
                    line: {
                        visible: true
                    },
                    axisCrossingValue: 0
                },
                categoryAxis: {
                    field: "Name",
                    line: {
                        visible: true
                    }
                },
                legend: {
                    position: "bottom"
                },
                tooltip: {
                    visible: true,
                    format: "{0}%",
                    template: "#= series.name #: #= value #%"
                }
            };
        }

        function viewreport() {
            AlertService.showAlert('View report');
        }

        function bindChart() {
            TrainingDataService.getTrainingResults()
                .then(function (data) {
                    vm.chartData = data;
                    progressDataSource.data(data);
                }, function (error) {
                    AlertService.showAlert('Error loading training results: ' + error.status + ' ' +
                        error.statusText);
                });
        }
        function refresh() {
            bindChart();
        }

        function init() {
            vm.localized = TrainingProgressResources; //set this to the json generated from the resx file
            vm.chartData = {};
            vm.viewreport = viewreport;
            vm.refresh = refresh;
            configureChart();
            bindChart();
        }
        init();
    }
    angular.module('app')
        .controller('TrainingProgressController', TrainingProgressController);
    TrainingProgressController.$inject = ['$scope', 'TrainingDataService', 'AlertService'];


}());
