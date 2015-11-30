(function () {
    "use strict";

    var VerificationSetController = function (DashboardService, WorkflowStateService, AnalysisSetService, Utils, Constants, AppStateService, $interval, $scope) {
        var vm = this,
            MODULES = Constants.getModuleLabels(),
            STATES = Constants.getStateLabels(),
            ACTIONS = Constants.getActionLabels(),
            STATUS = Constants.getStatusLabels(),

            setReviewStatusLabels = function () {
               var appStateUserGroups = Utils.getappStateUserGroups;
               
                if (vm.isReviewComplete()) {
                    vm.reviewStatus = vm.localized.QCSetReviewComplete;
                    vm.adminAction = String.format(vm.localized.AdminVerificationStatusReviewCompleted,appStateUserGroups,vm.selectedQCSet.TotalDocuments);
                    vm.reviewerAction = vm.localized.ReviewerVerificationStatusReviewCompleted;
                    vm.verificationSummaryConfig.Label = vm.localized.Status;
                    vm.verificationSummaryConfig.Header = vm.localized.QCSetReviewComplete;
                    vm.verificationSummaryConfig.Headline = String.format(vm.localized.AllDocumentsReviewed, vm.selectedQCSet.TotalDocuments);
                }
                else if (vm.isReviewInProgress()) {
                    vm.reviewStatus = vm.localized.QCSetReviewInProgress;
                    var reviewedDocs = vm.selectedQCSet.NumberOfNotRelevantDocuments + vm.selectedQCSet.NumberOfRelevantDocuments;
                    vm.adminAction = String.format(vm.localized.AdminVerificationStatusReviewInProgress,appStateUserGroups,reviewedDocs, vm.selectedQCSet.TotalDocuments);
                    vm.reviewerAction = String.format(vm.localized.ReviewerVerificationStatusReviewInProgress, reviewedDocs, vm.selectedQCSet.TotalDocuments);
                    vm.verificationSummaryConfig.Label = vm.localized.Status;
                    vm.verificationSummaryConfig.Header = vm.localized.QCSetReviewInProgress;
                    vm.verificationSummaryConfig.Headline = String.format(vm.localized.DocumentsNeedReview, vm.selectedQCSet.NumberOfNotCodedDocuments);
                }
                else if (vm.isCreated()) {
                    vm.reviewStatus = vm.localized.QCSetReviewNotStarted;
                    vm.adminAction = String.format(vm.localized.AdminVerificationStatusReviewNotStarted, appStateUserGroups, vm.selectedQCSet.TotalDocuments);
                    vm.reviewerAction = String.format(vm.localized.ReviewerVerificationStatusReviewNotStarted, vm.selectedQCSet.TotalDocuments);
                    vm.verificationSummaryConfig.Label = vm.localized.Status;
                    vm.verificationSummaryConfig.Header = vm.localized.QCSetReviewNotStarted;
                    vm.verificationSummaryConfig.Headline = String.format(vm.localized.DocumentsNeedReview, vm.selectedQCSet.NumberOfNotCodedDocuments);
                }
            },

            addIndicator = function (key, obj, len, arr) {
                if (len > 1) {
                    obj.increased = parseFloat(arr[len - 1][key]) > parseFloat(arr[len - 2][key]);
                    obj.decreased = parseFloat(arr[len - 1][key]) < parseFloat(arr[len - 2][key]);
                    obj.nochange = parseFloat(arr[len - 1][key]) === parseFloat(arr[len - 2][key]);
                } else {
                    obj.increased = false;
                    obj.decreased = false;
                    obj.nochange = true;
                }
                return obj;
            },

            populateQcSetCurrentPredictionScores = function (data) {
                var total = data.length;
                vm.currentQCRecall = addIndicator("Recall", { "value": data[total - 1].Recall * 100 }, total, data);
                vm.currentQCPrecision = addIndicator("Precision", { "value": data[total - 1].Precision * 100 }, total, data);
                vm.currentQCF1 = addIndicator("F1", { "value": data[total - 1].F1 * 100 }, total, data);
                populateF1Difference();
            },

            getQcSetPredictionScores = function (config) {
                var binderId = vm.selectedQCSet.BinderId;
                config = config || {};
                AnalysisSetService.getPredictionScores(STATES.QCSET, binderId, config).then(function (data) {
                    if (data !== null && !angular.isUndefined(data)) {
                        if (data.length > 0) {
                            vm.qcSetPredictionScores = data;
                            populateQcSetCurrentPredictionScores(data);
                        }
                    }
                });
            },

            populateCurrentPredictionScores = function (data) {
                var arr = [];
                angular.forEach(data, function (obj, idx) {
                    var newObj = angular.copy(obj);
                    newObj.Recall = addIndicator("Recall", { "value": data[idx].Recall * 100 }, idx + 1, data);
                    newObj.Precision = addIndicator("Precision", { "value": data[idx].Precision * 100 }, idx + 1, data);
                    newObj.F1 = addIndicator("F1", { "value": data[idx].F1 * 100 }, idx + 1, data);
                    newObj.Id = idx + 1;
                    arr.push(newObj);
                });
                return arr;
            },

            getControlSetPredictionScores = function (config) {
                config = config || {};
                AnalysisSetService.getPredictionScores(STATES.CONTROLSET, 0, config).then(function (data) {
                    if (data !== null && !angular.isUndefined(data)) {
                        if (data.length > 0) {
                            vm.predictionScores = data;
                            data = populateCurrentPredictionScores(data);
                            var total = data.length;
                            vm.currentRecall = data[total - 1].Recall;
                            vm.currentPrecision = data[total - 1].Precision;
                            vm.currentF1 = data[total - 1].F1;
                            populateF1Difference();
                        }
                    }
                });
            },

            getQcSetDiscrepancies = function (config) {
                var binderId = vm.selectedQCSet.BinderId;
                config = config || {};
                AnalysisSetService.getDiscrepancies(STATES.QCSET, binderId, config).then(function (data) {
                    if (data !== null && !angular.isUndefined(data)) {
                        if (data.length > 0) {
                            vm.qcSetDiscrepancies = data;
                            vm.qcFalseNegatives = data[0][1];
                            vm.qcFalseNegativesPercent = (data[0][1] / (data[0][0] + data[0][1])) * 100;
                            vm.resultsWithinTolerance = (vm.qcFalseNegativesPercent >= 0 && vm.qcFalseNegativesPercent < 5);
                            vm.resultsOutsideTolerance = (vm.qcFalseNegativesPercent >= 10);
                            vm.resultsInconclusive = (vm.qcFalseNegativesPercent >= 5 && vm.qcFalseNegativesPercent < 10);

                            vm.calculatedPrecision = (parseInt(data[0][0], 10) / parseInt((data[0][0] + data[1][0]), 10)) * 100;
                            vm.calculatedRecall = (parseInt((data[0][0]), 10) / parseInt((data[0][0] + data[0][1]), 10)) * 100;
                            vm.DiscrepancyStatisticsMsg = String.format(vm.localized.DiscrepancyStatisticsMsg, vm.calculatedPrecision.toFixed(1), vm.calculatedRecall.toFixed(1));
                            populateF1Difference();
                        }
                    }

                });
            },

            getControlSetDiscrepancies = function (config) {
                config = config || {};
                AnalysisSetService.getDiscrepancies(STATES.CONTROLSET, 0, config).then(function (data) {
                    if (data !== null && !angular.isUndefined(data)) {
                        if (data.length > 0) {
                            vm.controlSetdiscrepancies = data;
                            vm.falseNegatives = data[0][1];
                            vm.falseNegativesPercent = (data[0][1] / (data[0][0] + data[0][1])) * 100;
                        }
                    }

                });
            },
            populateF1Difference = function () {
                if (angular.isDefined(vm.currentQCF1) && angular.isDefined(vm.currentF1) && angular.isDefined(vm.qcFalseNegativesPercent)){
                    vm.F1Difference = vm.currentQCF1.value - vm.currentF1.value;
                    if (Math.abs(vm.F1Difference) < 4.0) {
                        vm.F1DifferenceMsg = String.format(vm.localized.VerificationF1Stable, vm.currentQCF1.value.toFixed(1), vm.qcFalseNegativesPercent.toFixed(1));
                    } else if (vm.F1Difference > 0.0) {
                        vm.F1DifferenceMsg = String.format(vm.localized.VerificationF1Increase, vm.F1Difference.toFixed(1), vm.currentQCF1.value.toFixed(1), vm.qcFalseNegativesPercent.toFixed(1));
                    } else {
                        vm.F1DifferenceMsg = String.format(vm.localized.VerificationF1Decrease, vm.F1Difference.toFixed(1), vm.currentQCF1.value.toFixed(1), vm.qcFalseNegativesPercent.toFixed(1));
                    }
                }
            },
        
            getQcSetInfo = function (config) {
                config = config || {};
                AnalysisSetService.getQCSetInfo(config).then(function (data) {
                    if (data !== null && !angular.isUndefined(data) && data.length > 0) {
                        vm.selectedQCSet = data[data.length-1];
                        if (vm.isReviewComplete(true) && vm.isAdminUser === true) {
                            getQcSetDiscrepancies();
                            getQcSetPredictionScores();
                        }
                        setReviewStatusLabels();
                        vm.setVerificationSummaryDetails();
                    }

                });
            },

            getWorkflowStates = function(config) {
                config = config || {};
                WorkflowStateService.getWorkflowStates(MODULES.ADMIN, config).then(function(result) {
                    vm.workflowStates = result;
                });
            },

            updateWorkflowToTraining = function(binderId) {
                var putData = [
                    {
                        "Name": STATES.TRAININGSET,
                        "CreateStatus": STATUS.NOTSTARTED,
                        "ReviewStatus": STATUS.NOTSTARTED,
                        "Order": 3
                    }
                ];
                WorkflowStateService.updateWorkflowState(putData, binderId).then(function () {
                    WorkflowStateService.updateWorkflowChangedStatus(true);
                    WorkflowStateService.allowWorkflowChange(false);
                    getWorkflowStates();
                });
            },


            init = function () {
                var config = { ignoreLoadingBar: true };
                getQcSetInfo(config);
                if (vm.isReviewComplete(true) && vm.isAdminUser === true) {
                    getControlSetPredictionScores(config);
                    getControlSetDiscrepancies(config);
                }
            };

        $scope.$on('LoadVerificationSet', function (events, args) {
            vm.workflowStates = args;
            init();
        });

        vm.localized = Constants.getProjectResources();

        vm.selectedQCSet = {};
        vm.qcSetPredictionScores = null;
        vm.qcSetDiscrepancies = null;

        vm.reviewStatus = vm.localized.ReviewNotStarted;
        vm.adminAction = "";
        vm.reviewerAction = "";

        vm.isCreated = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.CREATE);
        };

        vm.isReviewNotStarted = function (selectedSet) {
            if (selectedSet) {
                return vm.selectedQCSet.IsCurrent === true &&
                WorkflowStateService.isActionPending(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);
            } else {
                return WorkflowStateService.isActionPending(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);

            }
        };


        vm.isReviewInProgress = function (selectedSet) {
            if (selectedSet) {
                return vm.selectedQCSet.IsCurrent === true &&
                WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);
            } else {
                return WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);

            }
        };

        vm.isReviewComplete = function (selectedSet) {
            if (selectedSet) {
                return vm.selectedQCSet.IsCurrent === false ||
                WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);
            } else {
                return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);

            }
        };

        vm.viewQCDocumentListForAdmin = function (coding) {
            var binderId = vm.selectedQCSet.BinderId;
            var evUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/QcSet/1/" + binderId + "/from_admin/to_doclist/" + coding;
            var pcUrl = "/app/review/approot#/analysissets/QcSet/1/" + binderId + "/from_admin/to_doclist/" + coding;
            Utils.loadPage(evUrl, pcUrl);
        };
        vm.viewQCDocumentListForReview = function (coding) {
            var binderId = vm.selectedQCSet.BinderId;
            var evUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/QcSet/1/" + binderId + "/from_review/to_doclist/" + coding;
            var pcUrl = "/app/review/approot#/analysissets/QcSet/1/" + binderId + "/from_review/to_doclist/" + coding;
            Utils.loadPage(evUrl, pcUrl);
        };

        vm.verificationSummaryConfig =
        {
            Items: [
                {
                    Type: "Relevant",
                },
                {
                    Type: "Not_Relevant",
                },
                {
                    Type: "Not_Coded",
                }
            ]
        };

        vm.setVerificationSummaryDetails = function () {
            angular.forEach(vm.verificationSummaryConfig.Items, function (item) {
                if (item.Type === "Relevant") {
                    item.Data = vm.selectedQCSet.NumberOfRelevantDocuments;
                } else if (item.Type === "Not_Relevant") {
                    item.Data = vm.selectedQCSet.NumberOfNotRelevantDocuments;
                } else if (item.Type === "Not_Coded") {
                    item.Data = vm.selectedQCSet.NumberOfNotCodedDocuments;
                }
            });
        };

        vm.isChangeToTrainingAllowed = function () {
            return WorkflowStateService.isStateCurrent(STATES.QCSET) &&
                   WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);

        };

        vm.changeToTraining = function(config) {
            config = config || {};
            AnalysisSetService.getAnalysisSetSummary(STATES.TRAININGSET, config).then(function(result) {
                var trainingSetDetails = result;

                var roundNumber = parseInt(trainingSetDetails.CurrentRound, 10);
                AnalysisSetService.createTrainingSet(roundNumber).then(function(binderId) {
                    binderId = binderId.replace(/['"]+/g, "");
                    updateWorkflowToTraining(binderId);
                });
            });
        };

        vm.enableReviewButton = function () {
            return vm.isReviewNotStarted(true) || vm.isReviewInProgress(true);
        };

        vm.reviewButtonText = function () {
            var text = vm.isReviewInProgress() || vm.isReviewComplete() ?
                vm.localized.ContinueReview :
                vm.localized.StartReview;
            return text;
        };

        vm.startReview = function () {
            var putData = [{
                "Name": STATES.QCSET,
                "CreateStatus": STATUS.COMPLETED,
                "ReviewStatus": STATUS.INPROGRESS,
                "Order": 5
            }];
            WorkflowStateService.updateWorkflowState(putData, 0).then(function () {
                var setType = STATES.QCSET,
                 binderId = vm.selectedQCSet.BinderId,
                 roundNumber = vm.selectedQCSet.$id,
                 evUrl,
                 pcUrl;

                evUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/" + setType +
                    "/" + roundNumber + "/" + binderId + "/from_review/to_docview/All";
                pcUrl = "/app/review/approot#/analysissets/" + setType +
                    "/" + roundNumber + "/" + binderId + "/from_review/to_docview/All";
                Utils.loadPage(evUrl, pcUrl);
            });
        };
    };

    angular.module("app").controller("VerificationSetController", VerificationSetController);
    VerificationSetController.$inject = ["DashboardService", "WorkflowStateService",
"AnalysisSetService", "Utils", "Constants", "AppStateService", "$interval", "$scope"];
}());