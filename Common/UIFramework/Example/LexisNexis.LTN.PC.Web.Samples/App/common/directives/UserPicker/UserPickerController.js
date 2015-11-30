(function () {
    "use strict";
    function UserPickerController($scope, $modal, UserPickerService) {
        var vm = this,
            getSelectedUsers = function (selectedUsers) {
                var result = UserPickerService.constructUserList(selectedUsers);
                $scope.lnSelectedList = result.join(); //Outer scope context
            };
        vm.assign = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/common/directives/UserPicker/UserPickerDialogView.html',
                controller: 'UserPickerDialogController',
                controllerAs: 'UserPickerDialogController',
                resolve: {
                    previousSelectionUserList: function () {
                        return vm.userList;
                    },
                    treeViewType: function () {
                        return $scope.userPickerType;
                    }
                },
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                vm.userList = returnValue;
                getSelectedUsers(vm.userList);
            }, function () {
                vm.userList = {};
            });
        };

        vm.unassign = function () {
            if (vm.currentSelectedNodes.length > 0) {
                var result = UserPickerService.removeUsersFromUserList(vm.userList, vm.currentSelectedNodes);
                $scope.lnSelectedList = result.join(); //Outer scope context
            }
        };
        function init() {
            vm.localized = UserPickerResources;
        }
        init();
    }
    angular.module('app')
        .controller('UserPickerController', UserPickerController);
    UserPickerController.$inject = ['$scope', '$modal', 'UserPickerService'];
}());