(function () {
    "use strict";
    function CreateQCSetController($modalInstance, QCSetService, QCSetModel) {
        var vm = this,
            typeStatistical = "statistical",
            typePercentage = "percentage";
        function init() {
            //TODO : matterid, datasetid, projectid need to come from context
            vm.matterId = "2";
            vm.datasetId = "3";
            vm.projectId = "4";
            vm.localized = CreateQCSetResources;
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
            vm.pageModel = new QCSetModel();
        }
        init();
        function setPageModel() {
            if (vm.sampling === typeStatistical) {
                vm.pageModel.confidenceLevel = vm.confidenceSelected.value;
                vm.pageModel.marginOfError = vm.errorMarginSelected.value;
                vm.pageModel.isStatistical = true;

            } else if (vm.sampling === typePercentage) {
                vm.pageModel.isStatistical = false;
                vm.pageModel.percentage = vm.sliderPercentage;
            }
            vm.pageModel.relevantType = vm.relevance;
            return vm.pageModel;
        }
        function validateForm(currentForm) {
            if (angular.isUndefined(vm.relevance)) {
                vm.errorOnRelvanceType = true;
            } else {
                vm.errorOnRelvanceType = false;
            }

            if (angular.isUndefined(vm.sampling)) {
                vm.errorOnSamplingType = true;
            } else {
                vm.errorOnSamplingType = false;
            }
            
            if (vm.sampling === typeStatistical && currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
            } else {
                vm.submitted = false;
            }

            if (vm.errorOnRelvanceType || vm.errorOnSamplingType || vm.submitted) {
                return false;
            }
            return true;
        }
        vm.ok = function (currentForm) {
            var status = validateForm(currentForm);
            if (status) {
                QCSetService.addQCset(vm.matterId, vm.datasetId,
                    vm.projectId, setPageModel()).then(
                    function () {
                        $modalInstance.close();
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
            }
        };

        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
        vm.calculateSampleSize = function (currentForm) {
            var status = validateForm(currentForm);
            if (status) {
                QCSetService.getSampleSize(vm.matterId,
                    vm.datasetId, setPageModel()).then(
                    function (response) {
                        vm.sampleSize = response.data;
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
            }
        };
    }
    angular.module('app').controller('CreateQCSetController', CreateQCSetController);
    CreateQCSetController.$inject = ['$modalInstance', 'QCSetService', 'QCSetModel'];
}());