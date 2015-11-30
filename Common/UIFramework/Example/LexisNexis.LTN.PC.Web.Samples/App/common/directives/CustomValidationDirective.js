(function () {
    "use strict";
    angular.module("app")
        .directive('lnSampleCustomValidator', ['projectsDataService', 'alertService', function (projectsDataService,
            alertService) {
            return {
                require: 'ngModel',
                restrict: 'A',
                controller: function () {
                    var ctrl = this;
                    function sampleValidation(value) {
                        if (!value || value.length <= 0) {
                            return value;
                        }
                        //note: this example doesn't make much sense, but the point is to
                        // demonstrate how a service call can be made in a validator
                        projectsDataService.getProject(1)
                            .then(function (project) {
                                if (value === project.id) {
                                    ctrl.$setValidity('lnSampleCustomValidator', false);
                                } else {
                                    ctrl.$setValidity('lnSampleCustomValidator', true);
                                }
                            }, function (error) {
                                alertService.showAlert('Error validating project: ' + error.status +
                                    ' ' + error.statusText);
                            })
                            .catch(function (msg) {
                                alertService.showAlert('Error validating project: ' + msg);
                            });
                        return value;
                    }

                    ctrl.$parsers.unshift(sampleValidation);
                    ctrl.$formatters.unshift(sampleValidation);

                }
            };
        }]);
}());