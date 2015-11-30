(function () {
    "use strict";
    angular.module('app').factory('QCSetModel', function () {

        var QCSetModel = function () {
            this.isStatistical = false;
            this.confidenceLevel = 0;
            this.marginOfError = 0;
            this.percentage = 0;
            this.relevantType = "";
        };

        QCSetModel.prototype = {
            doSomething: function () {
            }
        };

        return QCSetModel;

    });
}());