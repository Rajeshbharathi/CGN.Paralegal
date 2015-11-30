(function () {
    "use strict";
    function CreateControlSetController($modalInstance, ControlSetService,
        ControlsetModel) {
        var vm = this;
        function init() {
            vm.matterId = "2"; //TODO: matterid, datasetid, projectid need to come from context
            vm.datasetId = "3";
            vm.projectId = "4";
            vm.localized = CreateControlSetResources;
            vm.confidences = [{
                name: vm.localized.confidences_95 + vm.localized.percentage,
                value: vm.localized.confidences_95
            }, {
                name: vm.localized.confidences_90 + vm.localized.percentage,
                value: vm.localized.confidences_90
            }, {
                name: vm.localized.confidences_85 + vm.localized.percentage,
                value: vm.localized.confidences_85
            }];

            vm.errorMargins = [{
                name: vm.localized.errorMargins_p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_p5
            }, {
                name: vm.localized.errorMargins_1 + vm.localized.percentage,
                value: vm.localized.errorMargins_1
            }, {
                name: vm.localized.errorMargins_1p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_1p5
            }, {
                name: vm.localized.errorMargins_2 + vm.localized.percentage,
                value: vm.localized.errorMargins_2
            }, {
                name: vm.localized.errorMargins_2p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_2p5
            }, {
                name: vm.localized.errorMargins_5 + vm.localized.percentage,
                value: vm.localized.errorMargins_5
            }];
            vm.pageModel = new ControlsetModel();
        }
        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
        vm.calculateSampleSize = function (currentForm) {
            if (currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            vm.pageModel.confidenceLevel = vm.confidenceSelected.value;
            vm.pageModel.marginOfError = vm.errorMarginSelected.value;
            ControlSetService.getSampleSize(vm.matterId, vm.datasetId, vm.pageModel).then(
                function (response) {
                    vm.sampleSize = response.data;
                });
        };
        function addControlset(currentForm) {
            if (currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            vm.pageModel.confidenceLevel = vm.confidenceSelected.value;
            vm.pageModel.marginOfError = vm.errorMarginSelected.value;
            ControlSetService.addControlset(vm.matterId, vm.datasetId,
                vm.projectId, vm.pageModel).then(
                function () {
                    $modalInstance.close();
                },
                function () {
                    //TODO : Exception handling
                }
            );
        }
        vm.ok = function (currentForm) {
            addControlset(currentForm);
        };
        init();
    }
    angular.module('app').controller('CreateControlSetController',
         CreateControlSetController);
    CreateControlSetController.$inject = ['$modalInstance',
        'ControlSetService', 'ControlsetModel'];
}());