(function () {
    "use strict";
    angular.module('app').factory('ProjectModel', function () {

        var ProjectModel = function () {
            this.id = 0;
            this.name = 'New Project';
            this.description = '';
            this.docSource = 'All';
            this.tag = '';
            this.identifyRepeatedContent = false;
            this.confidence = 0;
            this.marginOfError = 0;
            this.stratifyByCustodian = false;
            this.custodianField = null;
            this.sampleSize = null;
            this.limitExamples = false;
            this.numExamples = 0;
        };

        ProjectModel.prototype = {
            doSomething: function () {
                return;
            }
        };

        return ProjectModel;

    });


}());