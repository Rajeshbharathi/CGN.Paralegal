(function () {
    "use strict";

    function FileDialogService(WebApiClientService) {

        function getShares(organizationId) {
            return WebApiClientService.get('/api/filedialog/share/' + organizationId);
        }

        function getFolderFilePath(type, fileinfo) {
            return WebApiClientService.post('/api/filedialog/path/' + type, fileinfo);
        }

        function getFolderFilePathsWithValidate(type, fileinfo) {
            return WebApiClientService.post('/api/filedialog/path/validate/' + type, fileinfo);
        }
        var service = {
            getShares: getShares,
            getFolderFilePath: getFolderFilePath,
            getFolderFilePathsWithValidate: getFolderFilePathsWithValidate
        };
        return service;
    }
    angular.module('app')
        .factory('FileDialogService', FileDialogService);
    FileDialogService.$inject = ['WebApiClientService'];
}());
