(function () {
    "use strict";
    function FileBrowseController($scope, $modal) {
        var vm = this;
        vm.open = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/common/directives/FileBrowse/FolderFilePathDialogView.html',
                controller: 'FolderFilePathDialogController',
                controllerAs: 'FolderFilePathDialogController',
                resolve: {
                    pathType: function () {
                        return $scope.browsetype;
                    },
                    pathText: function () {
                        return $scope.lnFilePath;
                    }
                },
                size: 'smx'
            });
            modalInstance.result.then(function (returnValue) {
                $scope.lnFilePath = returnValue;
            }, function () {
                $scope.lnFilePath = '';
            });
        };
        function init() {
            vm.localized = FileBrowseResources;
        }
        init();
    }
    angular.module('app')
        .controller('FileBrowseController', FileBrowseController);
    FileBrowseController.$inject = ['$scope', '$modal'];

}());
