(function () {
    "use strict";

    function FolderFilePathDialogController($modalInstance, FileDialogModel, FileDialogService, pathType, pathText) {

        var vm = this,
            browseFileType = 'file',
            browseFolderType = 'folder',
            browseCreateFileType = 'createfileinfolder',
            rootPathType = 'root';
        function bindShares(organizationId) {

            FileDialogService.getShares(organizationId)
                .then(
                    function (response) {
                        vm.fileInformations = response.data;
                        vm.colShow = true;
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
        }
        function getFolderFilePathsWithValidate(type, fileinfo) {
            FileDialogService.getFolderFilePathsWithValidate(type, fileinfo)
                .then(
                    function (response) {
                        vm.fileInformations = response.data;
                        vm.colShow = false;
                        if (vm.fileinfo.type === rootPathType) {
                            vm.colShow = true;
                        }
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
        }
        function bindFolderFilePath(filePath) {
            var type = vm.browseType,
                filename,
                folderPath;
            if (vm.browseType === browseCreateFileType) {
                filename = filePath.substr(filePath.lastIndexOf("\\") + 1);
                folderPath = filePath.substr(0, filePath.lastIndexOf("\\"));
                filePath = folderPath;
                vm.lnModalFileName = filename;
                type = browseFolderType;
            }
            vm.fileinfo.path = filePath;
            vm.fileinfo.type = vm.browseType;
            vm.spath = filePath;
            getFolderFilePathsWithValidate(type, vm.fileinfo);
        }
        function getFodlderFilePath(type, fileinfo) {
            FileDialogService.getFolderFilePath(type, fileinfo)
                .then(
                    function (response) {
                        vm.fileInformations = response.data;
                        vm.colShow = false;
                        if (vm.fileinfo.type === rootPathType) {
                            vm.colShow = true;
                        }
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
        }
        vm.select = function (val) {
            vm.fileinfo.path = val.Path;
            vm.fileinfo.name = val.Name;
            vm.fileinfo.type = val.Type;
            vm.spath = val.Path;
            if (vm.fileinfo.type === browseFileType) {
                return;
            }
            getFodlderFilePath(vm.browseType, vm.fileinfo);
        };
        vm.ok = function () {
            if (angular.isUndefined(vm.spath) || vm.spath === null || vm.spath === '') {
                vm.isErrorInfoPath = true;
                return;
            }
            if (vm.browseType === browseFileType && vm.fileinfo.type !== browseFileType) {
                vm.isErrorInfoPath = true;
                return;
            }

            if (vm.browseType === browseCreateFileType) {
                if ((angular.isUndefined(vm.lnModalFileName) || vm.lnModalFileName === null ||
                        vm.lnModalFileName === '' || vm.lnModalFileName.lastIndexOf(".") === -1)) {
                    vm.isErrorInfoFileName = true;
                    vm.isErrorInfoPath = false;
                    return;
                }
                vm.spath = vm.spath + '\\' + vm.lnModalFileName;
            }

            $modalInstance.close(vm.spath);
        };
        vm.cancel = function () {

            $modalInstance.dismiss('cancel');
        };
        function init() {
            // Todo: Need to come from context
            var organizationId = 2;
            vm.localized = FileBrowseResources;
            vm.browseType = pathType;
            vm.colShow = true;
            vm.isErrorInfoPath = false;
            vm.showFileName = false;
            vm.isErrorInfoFileName = false;

            if (vm.browseType === browseFolderType) {
                vm.title = vm.localized.BrowseTypeFolderTitle;
            } else if (vm.browseType === browseFileType) {
                vm.title = vm.localized.BrowseTypeFileTitle;
            } else if (vm.browseType === browseCreateFileType) {
                vm.title = vm.localized.BrowseTypeCreateFileTitle;
                vm.showFileName = true;
            }

            vm.fileinfo = new FileDialogModel();

            if (angular.isUndefined(pathText) || pathText === '') {

                bindShares(organizationId);

            } else {
                bindFolderFilePath(pathText);
            }
        }
        init();
    }
    angular.module('app')
        .controller('FolderFilePathDialogController', FolderFilePathDialogController);
    FolderFilePathDialogController.$inject = ['$modalInstance',
        'FileDialogModel', 'FileDialogService', 'pathType', 'pathText'];

}());
