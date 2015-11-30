(function () {
    "use strict";
    angular.module('app').factory('ControlsetModel', function () {

        var ControlsetModel = function () {
            this.confidenceLevel = 0;
            this.marginOfError = 0;
        };

        ControlsetModel.prototype = {
            doSomething: function () {
            }
        };

        return ControlsetModel;

    });


}());