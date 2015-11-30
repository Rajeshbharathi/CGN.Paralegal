(function () {
    "use strict";
    function CreatePredictionSetController($modalInstance, PredictionSetService) {
        var vm = this;
        function init() {
            //TODO : matterid, datasetid, projectid need to come from context
            vm.matterId = "2";
            vm.datasetId = "3";
            vm.projectId = "4";
            vm.localized = CreatePredictionSetResources;
        }
        vm.ok = function () {
            PredictionSetService.createPredictionSetJob(vm.matterId,
                vm.datasetId, vm.projectId).then(
                function () {
                    $modalInstance.close();
                },
                function () {
                    //TODO Exception Handling
                }
            );
        };
        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
        init();
    }
    angular.module('app').controller('CreatePredictionSetController',
         CreatePredictionSetController);
    CreatePredictionSetController.$inject = ['$modalInstance',
        'PredictionSetService'];
}());