(function () {
    'use strict';
    var ReviewerDataService = function (ReviewerConfiguration, $injector, $q) {
        var document, getComments, getDocumentSet, getExcerpts, modelFactory, setComments, setExcerpts;
        modelFactory = $injector.get(ReviewerConfiguration.service);
        document = {
            comments: [],
            excerpts: []
        };
        getDocumentSet = function (id) {
            var deferred;
            deferred = $q.defer();
            modelFactory.request(id)
                .success(function (response) {
                    return deferred.resolve(response);
                })
                .error(function (error) {
                    return deferred.reject(error);
                });
            return deferred.promise;
        };
        setComments = function (arr) {
            document.comments = arr;
        };
        setExcerpts = function (arr) {
            document.excerpts = arr;
        };
        getComments = function () {
            return document.comments;
        };
        getExcerpts = function () {
            return document.excerpts;
        };
        return {
            getDocumentSet: getDocumentSet,
            getComments: getComments,
            getExcerpts: getExcerpts,
            setComments: setComments,
            setExcerpts: setExcerpts
        };
    },
        app = angular.module('app');
    app.service('ReviewerDataService', ReviewerDataService);
    ReviewerDataService.$inject = ['ReviewerConfiguration', '$injector', '$q'];
}());
