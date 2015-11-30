(function () {
    "use strict";

    function UserPickerDialogController($modalInstance, UserPickerService, UserPickerFilterModel,
        previousSelectionUserList, treeViewType) {
        var vm = this,
            selectedUser = [],
            typeGroup = "group",
            typeUser = "user",
            typeUserAndUserGroup = "usersandgroup",
            treeTextField = "UserName",
            treeSubNode = "Users",
            previousSelectedUser = previousSelectionUserList;
        function getData(organizationId) {

            if (treeViewType === typeGroup) {
                vm.title = vm.localized.TitleUserGroups;
                return UserPickerService.getUserGroups(organizationId);
            }

            if (treeViewType === typeUser) {
                vm.title = vm.localized.TitleUsers;
                return UserPickerService.getUser(organizationId);
            }

            if (treeViewType === typeUserAndUserGroup) {
                vm.title = vm.localized.TitleUserGroupsUsers;
                return UserPickerService.getUserGroupsWithUsers(organizationId);
            }
        }
        function getDatasourceResult(organizationId) {

            var dataResult = new kendo.data.HierarchicalDataSource({
                transport: {
                    read: function (options) {
                        getData(organizationId)
                            .success(function (response) {
                                var responseData = UserPickerService.constructAndSetStateOnData(response, previousSelectionUserList);
                                options.success(responseData);
                            })
                            .error(function () {
                                //TODO : Exception handling
                            });
                    }
                },
                schema: {
                    model: {
                        children: treeSubNode,
                        expanded: true
                    }
                }
            });
            return dataResult;
        }
        function configureTree(organizationId) {
            //Configure Data
            vm.treeOptions = {
                loadOnDemand: false,
                dataTextField: [treeTextField],
                checkboxes: {
                    template: "<input type='checkbox' ng-checked='dataItem.checked' " +
                        "ng-model='dataItem.checked' ng-click='UserPickerDialogController.onCheck(dataItem)'/>",
                    checkChildren: true
                }
            };
            //Set Data source
            vm.treeData = getDatasourceResult(organizationId);
        }
        vm.onCheck = function (dataItem) {
            if (!angular.isUndefined(dataItem.Users)) { //root selection
                selectedUser = UserPickerService.addOrRemoveAllUser(dataItem, selectedUser);
            } else { //Signle selection 
                selectedUser = UserPickerService.addOrRemoveUser(dataItem, selectedUser);
            }
        };
        vm.ok = function () {
            $modalInstance.close(selectedUser);
        };
        vm.cancel = function () {

            $modalInstance.dismiss('cancel');
        };

        function getDatasourceFilterResult(organizationId, filterInfo) {

            var dataResult = new kendo.data.HierarchicalDataSource({
                transport: {
                    read: function (options) {
                        UserPickerService.postFilterUserGroupsAndUsers(organizationId,
                                filterInfo)
                            .success(function (response) {
                                var responseData = UserPickerService.constructAndSetStateOnData(response, previousSelectionUserList);
                                options.success(responseData);
                            })
                            .error(function () {
                                //TODO : Exception handling
                            });
                    }
                },
                schema: {
                    model: {
                        children: treeSubNode,
                        expanded: true
                    }
                }
            });
            return dataResult;
        }
        vm.reset = function () {

            if (treeViewType === typeUserAndUserGroup) {
                var organizationId = 2; // Todo: Need to come from context
                //Set Data source
                vm.treeData = getDatasourceResult(organizationId);

            } else { //Default Filters
                vm.treeData.filter({});
            }
            vm.filterText = '';
        };
        vm.filternode = function () {
            if (treeViewType === typeUserAndUserGroup) { //Custom Filter
                vm.filterInfo = new UserPickerFilterModel();
                vm.filterInfo.filterText = vm.filterText;
                vm.treeData = getDatasourceFilterResult(2, vm.filterInfo);

            } else { //Default Filters
                vm.treeData.filter({
                    field: treeTextField,
                    operator: "contains",
                    value: vm.filterText
                });
            }
        };
        function init() {
            var organizationId = 2; // TODO: Need to come from context
            vm.localized = UserPickerResources;
            if (!angular.isUndefined(previousSelectedUser)) {
                selectedUser = previousSelectedUser;
            }
            configureTree(organizationId);
        }
        init();
    }
    angular.module('app')
        .controller('UserPickerDialogController', UserPickerDialogController);
    UserPickerDialogController.$inject = ['$modalInstance', 'UserPickerService',
        'UserPickerFilterModel', 'previousSelectionUserList', 'treeViewType'];
}());