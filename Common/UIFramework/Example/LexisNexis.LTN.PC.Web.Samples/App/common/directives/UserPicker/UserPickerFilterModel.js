(function () {
    "use strict";
    angular.module('app')
        .factory('UserPickerFilterModel', function () {
            var UserPickerFilterModel = function () {
                this.filterText = '';
            };
            return UserPickerFilterModel;
        });


}());
