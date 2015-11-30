(function () {
    "use strict";
    var ReviewController, app;

    ReviewController = function () {
        var vm;
        vm = this;
        vm.reviewerConfig = {};
    };

    app = angular.module('app');

    app.controller('ReviewController', ReviewController);
    ReviewController.$inject = [];

}());
