(function () {
    "use strict";
    var DocumentSetModelFactory;
    DocumentSetModelFactory = function (WebApiClientService) {
        var documentSetModelFactory, serviceName;
        serviceName = "/api/reviewset";
        documentSetModelFactory = {
            request: function (id) {
                var url;
                url = id ? serviceName + "/:" + id : serviceName;
                return WebApiClientService.get(url);
            },
            addComments: function (docId, comment) {
                return WebApiClientService.put(serviceName + "/:" + docId, comment);
            },
            deleteComment: function (docId, commentId) {
                return WebApiClientService['delete'](serviceName + "/:" + docId(+"/" + commentId));
            },
            addExcerpts: function (docId, excerpt) {
                return WebApiClientService.put(serviceName + "/:" + docId, excerpt);
            },
            deleteExcerpts: function (docId, excerptId) {
                return WebApiClientService['delete'](serviceName + "/:" + docId, excerptId);
            }
        };
        return documentSetModelFactory;
    };
    angular.module("app")
        .factory('DocumentSetModelFactory', DocumentSetModelFactory);
    DocumentSetModelFactory.$inject = ['WebApiClientService'];
}());