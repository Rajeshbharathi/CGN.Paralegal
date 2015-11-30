(function () {
    "use strict";
    function RunAccuracyTestController($modalInstance, RunAccuracyTestService) {
        var vm = this;
        function init() {
            //TODO  : projectid and controlsetid need to come from context
            vm.projectId = "4";
            vm.controlsetId = "5";
            vm.localized = RunAccuracyTestResources;
        }
        init();
        vm.ok = function () {

            RunAccuracyTestService.createRunAccuracyTest(vm.projectId, vm.controlsetId).then(
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
    }
    angular.module('app').controller('RunAccuracyTestController', RunAccuracyTestController);
    RunAccuracyTestController.$inject = ['$modalInstance', 'RunAccuracyTestService'];
}());