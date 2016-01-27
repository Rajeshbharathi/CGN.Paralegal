(function () {
    "use strict";

    var DashboardService = function (WebApiClientService, Utils) {
        var confidences = [{
            name: "90%",
            value: "90"
        }, {
            name: "95%",
            value: "95"
        }, {
            name: "99%",
            value: "99"
        }],

        errorMargins = [{
            name: "1%",
            value: "1"
        }, {
            name: "2.5%",
            value: "2.5"
        }, {
            name: "5%",
            value: "5"
        }],

         
         getTopTenAOP = function () {
             var url =  Utils.getWebApiRouteString("gettoptenaoproute");
             return WebApiClientService.get(url).then(function (result) {
                 return result.data;
             }, function (err) {
                 throw err;
             });
         },
         getTopTenCity = function () {
             var url = Utils.getWebApiRouteString("gettoptencityroute");
             return WebApiClientService.get(url).then(function (result) {
                 return result.data;
             }, function (err) {
                 throw err;
             });
         },
         getTopTenParaLegal = function () {
             var url = Utils.getWebApiRouteString("gettoptenparalegalroute");
             return WebApiClientService.get(url).then(function (result) {
                 return result.data;
             }, function (err) {
                 throw err;
             });
         },
        getDashboardDetails = function (config) {
            config=config || {};
            var url = Utils.getWebApiRouteString("dashboardDetailsRoute");
            return WebApiClientService.get(url,config).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });
        },

        getSampleSize = function (controlset) {
            var url = Utils.getWebApiRouteString("sampleSizeRoute");
            return WebApiClientService.post(url, controlset, { headers: { "Content-Type": "application/json" } }).then(function (response) {
                return response.data;
            }, function (err) {
                throw err;
            });

        },
        getTags = function () {
            var url = Utils.getWebApiRouteString("getTagsRoute");
            return WebApiClientService.get(url).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });
        },
        getSavedSearches = function () {
            var url = Utils.getWebApiRouteString("getSavedSearchRoute");
            return WebApiClientService.get(url).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });
        },
        getDocumentCount = function (option) {
            var url = Utils.getWebApiRouteString("documentCountRoute");
            return WebApiClientService.post(url, option).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });

        },
        createProject = function (params) {
            var url = Utils.getWebApiRouteString("createProjectRoute");
            return WebApiClientService.post(url, params).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });

        },
        validateProjectInfo = function(postData){
            var url = Utils.getWebApiRouteString("validateProjectInfoRoute");
            return WebApiClientService.post(url, postData).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });

        },
        deleteProject = function(){
            var url = Utils.getWebApiRouteString("deleteProjectRoute");
            return WebApiClientService.delete(url, {}).then(function (result) {
                return result;
            }, function (err) {
                throw err;
            });
        },
        getAvailableDocumentCount = function () {
            var url = Utils.getWebApiRouteString("availableDocumentCountRoute");
            return WebApiClientService.get(url).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });

        },
        validatePredictionCoding= function() {
            var url = Utils.getWebApiRouteString("validatePredictionCodingRoute");
            return WebApiClientService.get(url).then(function (result) {
                return result.data;
            }, function (err) {
                throw err;
            });
        },
        schedulePredictAll = function (time) {
            var url = Utils.getWebApiRouteString("schedulePredictAllRoute");
            return WebApiClientService.post(url + "/", time).then(function (response) {
                return response.data;
            }, function (err) {
                throw err;
            });
        },
        getConfidenceLevels = function () {
            return confidences;
        },
        getMarginOfErrors = function () {
            return errorMargins;
        };

        return {
            getDashboardDetails: getDashboardDetails,
            getSampleSize: getSampleSize,
            getTags: getTags,
            getSavedSearches: getSavedSearches,
            getDocumentCount: getDocumentCount,
            createProject: createProject,
            getConfidenceLevels: getConfidenceLevels,
            getMarginOfErrors: getMarginOfErrors,
            schedulePredictAll: schedulePredictAll,
            getAvailableDocumentCount:getAvailableDocumentCount,
            validateProjectInfo: validateProjectInfo,
            validatePredictionCoding: validatePredictionCoding,
            deleteProject:deleteProject,
            getTopTenAOP: getTopTenAOP,
            getTopTenCity: getTopTenCity,
            getTopTenParaLegal: getTopTenParaLegal
        };
    };

    angular.module("app").service("DashboardService", DashboardService);
    DashboardService.$inject = ["WebApiClientService", "Utils"];

}());