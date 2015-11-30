(function () {
    "use strict";

    angular.module("app").factory("DocumentService", ["WebApiClientService", "Utils", function (WebApiClientService, Utils) {
        var document = null,
            configuration= null,
            factory = {
                getDocument: function (idx, postData, type) {
                    type = angular.isString(type) ? type : null;
                    if (type === null) {
                        var getDocServiceName = Utils.getWebApiRouteString(postData.AnalysisSet.Type + "DocumentRoute");
                        return WebApiClientService.get(getDocServiceName + "/" + idx).then(function (response) {
                            return response.data;
                        }, function (err) {
                            throw err;
                        });
                    } else {
                        var getUncodedDocument = Utils.getWebApiRouteString("nextUncodedDocumentRoute");
                        return WebApiClientService.post(getUncodedDocument + "/" + idx, postData).then(function (response) {
                            return response.data;
                        }, function (err) {
                            throw err;
                        });
                    }
                },
                getPage: function (getData, idx) {
                    idx = idx || 0;
                    var getPageServiceName = Utils.getWebApiRouteString(getData.AnalysisSet.Type + "DocumentPageRoute");
                    return WebApiClientService.get(getPageServiceName + "/" + idx).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });

                },
                saveCoding: function (docId, putData) {
                    var getCodingRoute = Utils.getWebApiRouteString("saveCodingRoute");
                    return WebApiClientService.put(getCodingRoute + "/" + docId, putData).then(function () {
                        return true;
                    }, function () {
                        return false;
                    }, function (err) {
                        throw err;
                    });
                },
                getDocumentlist: function (postData, config) {
                    config = config || {};
                    var getDocumentListRoute = Utils.getWebApiRouteString(postData.AnalysisSet.Type + "DocListRoute");

                    return WebApiClientService.post(getDocumentListRoute, postData, config).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                autoCode: function (postData, fieldName, fieldValue, config) {
                    config = config || {};
                    var autoCodeRoute = Utils.getWebApiRouteString(postData.AnalysisSet.Type + "DocListRoute");
                    autoCodeRoute = autoCodeRoute.replace("documents", "autocode/") + fieldName + "/" + fieldValue;
                    return WebApiClientService.put(autoCodeRoute, postData, config).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                exportDocumentList: function (postData, config) {
                    config = config || {};
                    var getExportListRoute = Utils.getWebApiRouteString("export" + postData.AnalysisSet.Type + "Route");
                    return WebApiClientService.post(getExportListRoute, postData, config).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                scheduleExportJob: function (postData, config) {
                    config = config || {};
                    var getExportJobRoute = Utils.getWebApiRouteString("schedule" + postData.AnalysisSet.Type + "ExportJobRoute");
                    return WebApiClientService.post(getExportJobRoute, postData, config).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                storeCurrentDocument : function (doc) {
                    document = doc;
                },

                fetchCurrentDocument : function () {
                    return document;
                },
                getConfiguration : function () {
                    return configuration;
                },
                storeConfiguration: function (data) {
                    configuration = data;
                }
            };

        return factory;
    }]);
}());