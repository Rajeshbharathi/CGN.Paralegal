(function () {
    "use strict";

    angular.module("app").factory("AnalysisSetService", ["WebApiClientService", "Utils",
        function (WebApiClientService, Utils) {
        
            var reviewCompleted = {
                    "CONTROLSET": false,
                    "TRAININGSET": false,
                    "PREDICTSET": false,
                    "QCSET": false
                },
                createAnalysisSet = function (setType, postData) {
                    var url = Utils.getWebApiRouteString("create" + setType + "Route");
                    return WebApiClientService.post(url, postData).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });

                },
                createTrainingSet = function () {
                    var url = Utils.getWebApiRouteString("createTrainingSetRoute");
                    return WebApiClientService.post(url).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });

                },
                getAnalysisSetSummary = function (setType,config) {
                    config=config || {};
                    var url = Utils.getWebApiRouteString(setType + "SummaryRoute");
                    return WebApiClientService.get(url,config).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                getPredictAllSummary = function(config){
                    config=config || {};
                    var url = Utils.getWebApiRouteString("predictAllSummaryRoute");
                    return WebApiClientService.get(url,config).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                getAvailableAnalysisSets = function () {
                    var getAvailableAnalysisSetsRoute = Utils.getWebApiRouteString("availableAnalysisSetsRoute");
                    return WebApiClientService.get(getAvailableAnalysisSetsRoute).then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                manualCategorizeControlSet = function () {
                    var url = Utils.getWebApiRouteString("categorizeControlSetRoute");
                    return WebApiClientService.post(url + "/manual").then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                validateCategorizeControlSetJob = function () {
                    var url = Utils.getWebApiRouteString("categorizeControlSetRoute");
                    return WebApiClientService.get(url + "/validate").then(function (response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                },
                getPredictionScores = function (setType,setId,config) {
                    config=config || {};
                    var url = Utils.getWebApiRouteString("predictionScoresRoute").replace("{setType}", setType).replace("{setId}", setId);
                    return WebApiClientService.get(url,config).then(function (result) {
                        return result.data;
                    }, function (err) {
                        throw err;
                    });

                },
                getDiscrepancies = function (setType,setId,config) {
                    config=config || {};
                    var url = Utils.getWebApiRouteString("discrepanciesRoute").replace("{setType}", setType).replace("{setId}", setId);
                    return WebApiClientService.get(url,config).then(function (result) {
                        return result.data;
                    }, function (err) {
                        throw err;
                    });

                },
                getTrainingSetDiscrepancies = function (config) {
                    config=config || {};
                    var url = Utils.getWebApiRouteString("trainingSetDiscrepanciesRoute");
                    return WebApiClientService.get(url,config).then(function (result) {
                        return result.data;
                    }, function (err) {
                        throw err;
                    });

                },
                getQCSetInfo = function (config) {
                    config=config || {};
                    var url = Utils.getWebApiRouteString("getQcSetsInfoRoute");
                    return WebApiClientService.get(url,config).then(function (result) {
                        return result.data;
                    }, function (err) {
                        throw err;
                    });

                },
                getAddDocumentsToAnalysisSet = function () {
                    var url = Utils.getWebApiRouteString("trainingSetDoclistRoute");
                    return WebApiClientService.put(url + "/add").then(function(response) {
                        return response.data;
                    }, function (err) {
                        throw err;
                    });
                
                },
                isReviewComplete= function (setType) {
                    return reviewCompleted[setType.toUpperCase()];
                },
                setReviewComplete= function (setType) {
                    reviewCompleted[setType.toUpperCase()] = true;
                };

        return {
            createAnalysisSet: createAnalysisSet,
            getAnalysisSetSummary : getAnalysisSetSummary,
            getAvailableAnalysisSets: getAvailableAnalysisSets,
            isReviewComplete: isReviewComplete,
            setReviewComplete: setReviewComplete,
            getPredictAllSummary:getPredictAllSummary,
            manualCategorizeControlSet: manualCategorizeControlSet,
            validateCategorizeControlSetJob: validateCategorizeControlSetJob,
            createTrainingSet: createTrainingSet,
            getDiscrepancies: getDiscrepancies,
            getPredictionScores: getPredictionScores,
            getQCSetInfo: getQCSetInfo,
            getTrainingSetDiscrepancies: getTrainingSetDiscrepancies,
            getAddDocumentsToAnalysisSet: getAddDocumentsToAnalysisSet
        };
    }]);
}());