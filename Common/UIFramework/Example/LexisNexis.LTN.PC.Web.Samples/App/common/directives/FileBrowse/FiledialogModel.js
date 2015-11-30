(function () {
    "use strict";
    angular.module('app')
        .factory('FileDialogModel', function () {
            var FileDialogModel = function () {
                this.name = '';
                this.path = '';
                this.type = '';
                this.size = 0;
            };
            return FileDialogModel;

        });

}());
