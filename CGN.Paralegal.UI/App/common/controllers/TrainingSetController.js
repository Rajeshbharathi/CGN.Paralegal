(function () {
    "use strict";

    var TrainingSetController = function (DashboardService, WorkflowStateService, AnalysisSetService, Utils, Constants, AppStateService, $interval, $scope) {
        var vm = this,
            MODULES = Constants.getModuleLabels(),
            STATES = Constants.getStateLabels(),
            ACTIONS = Constants.getActionLabels(),
            STATUS = Constants.getStatusLabels(),            
            loadTrainingSetOverview = function (config) {
                config = config || {};
                AnalysisSetService.getAnalysisSetSummary(STATES.TRAININGSET, config).then(function (result) {
                    vm.trainingSetDetails = result;
                    setReviewStatusLabels();
                    vm.setTrainingConfigDetails();
                    var trainingSets = result.CompletedRoundDetails;
                    if (result.CompletedRoundDetails != null && result.CompletedRoundDetails.length != result.CurrentRound)
                    trainingSets.unshift(result.CurrentRoundProgress);
                    vm.trainingSetSummaryGridData = Utils.buildDashboardTrainingSummaryGrid(trainingSets);

                    var pieTrainingSetData = [];
                    pieTrainingSetData = [
                        Utils.getPieSeriesCategoryObj("Relevant", result.CompletedRoundsSummary.NumberOfRelevantDocuments),
                        Utils.getPieSeriesCategoryObj("NotRelevant", result.CompletedRoundsSummary.NumberOfNotRelevantDocuments),
                        Utils.getPieSeriesCategoryObj("Skipped", result.CompletedRoundsSummary.NumberOfSkippedDocuments)
                    ];

                    vm.trainingSetData = Utils.buildPieChartDataSource(pieTrainingSetData);

                    if (vm.hasCompletedTrainingRounds() && vm.isAdminUser===true) {
                        getTrainingSetDiscrepancies(config);
                        getControlSetPredictionScores(config);
                        getControlSetDiscrepancies(config);
                        loadControlSetDetails(config);

                    }
                });
            },
            setReviewStatusLabels = function () {
                var reviewComplete = vm.isTrainingSetReviewCompleted();
                var reviewInProgress = vm.isTrainingSetReviewInProgress();
                var created = vm.isCreated();
                var appStateUserGroups = Utils.getappStateUserGroups;
                if (reviewComplete) {
                    vm.reviewStatus = String.format(vm.localized.TrainingComplete, vm.trainingSetDetails.RoundsCompleted);
                    vm.reviewerStatus = vm.localized.TrainingCompleteReviewer;
                    if (vm.isPredictionJobComplete()) {
                        vm.adminAction = String.format(vm.localized.PredictionCompleteMessage, appStateUserGroups, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.RoundsCompleted);
                    } else if (vm.isPredictionJobInProgress()) {
                        vm.adminAction = String.format(vm.localized.PredictionInprogressMessage, appStateUserGroups, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.RoundsCompleted);
                    } else if (vm.isTrainingStateCurrent()) {
                        vm.adminAction = String.format(vm.localized.TrainingCompleteRunPredictionsMessage, appStateUserGroups, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.RoundsCompleted);
                    } else {
                        vm.adminAction = String.format(vm.localized.TrainingCompleteMessage, appStateUserGroups, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.RoundsCompleted);
                    }

                    vm.reviewerAction = String.format(vm.localized.TrainingCompleteReviewerActionMessage, vm.trainingSetDetails.RoundsCompleted);
                    vm.trainingSetConfig.Label = vm.localized.Status;
                    vm.trainingSetConfig.Header = vm.localized.TrainingSetReviewComplete;
                    vm.trainingSetConfig.Headline = String.format(vm.localized.AllDocumentsReviewed, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments);
                }
                else if (reviewInProgress) {
                    var totaldocumentsReviewed = vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments - vm.trainingSetDetails.CurrentRoundProgress.NumberOfNotCodedDocuments;
                    vm.reviewStatus = String.format(vm.localized.TrainingSetReviewInProgressWithName, vm.trainingSetDetails.CurrentRound);
                    vm.reviewerStatus = vm.reviewStatus;
                    vm.adminAction = String.format(vm.localized.TrainingSetReviewInProgressActionMessage,appStateUserGroups,totaldocumentsReviewed, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.CurrentRound);
                    vm.reviewerAction = String.format(vm.localized.TrainingSetReviewerInProgressActionMessage, totaldocumentsReviewed, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.CurrentRound);
                    vm.trainingSetConfig.Label = vm.localized.Status;
                    vm.trainingSetConfig.Header = vm.localized.TrainingSetReviewInProgress;
                    vm.trainingSetConfig.Headline = String.format(vm.localized.DocumentsNeedReview, vm.trainingSetDetails.CurrentRoundProgress.NumberOfNotCodedDocuments);
                }
                else if (created) {
                    vm.reviewStatus = String.format(vm.localized.TrainingSetReviewNotStartedWithName, vm.trainingSetDetails.CurrentRound);
                    vm.reviewerStatus = vm.reviewStatus;
                    vm.adminAction = String.format(vm.localized.TrainingSetReviewNotStartedActionMessage,appStateUserGroups, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.CurrentRound);
                    vm.reviewerAction = String.format(vm.localized.TrainingSetReviewerNotStartedActionMessage, vm.trainingSetDetails.CurrentRoundProgress.TotalDocuments, vm.trainingSetDetails.CurrentRound);
                    vm.trainingSetConfig.Label = vm.localized.Status;
                    vm.trainingSetConfig.Header = vm.localized.TrainingSetReviewNotStarted;
                    vm.trainingSetConfig.Headline = String.format(vm.localized.DocumentsNeedReview, vm.trainingSetDetails.CurrentRoundProgress.NumberOfNotCodedDocuments);
                }
            },
             loadControlSetDetails = function (config) {
                config = config || {};
                AnalysisSetService.getAnalysisSetSummary(STATES.CONTROLSET, config).then(function (result) {
                    vm.controlSetDetails = result;
                    var pieData =[];
                    if (vm.isControlSetReviewInProgress()) {
                        pieData =[
                            Utils.getPieSeriesCategoryObj("Relevant", result.NumberOfRelevantDocuments),
                            Utils.getPieSeriesCategoryObj("NotRelevant", result.NumberOfNotRelevantDocuments),
                            Utils.getPieSeriesCategoryObj("NotCoded", result.NumberOfNotCodedDocuments)
                    ];
                }
                    if (vm.isControlSetReviewCompleted()) {
                        pieData =[
                            Utils.getPieSeriesCategoryObj("Relevant", result.NumberOfRelevantDocuments),
                            Utils.getPieSeriesCategoryObj("NotRelevant", result.NumberOfNotRelevantDocuments)
                    ];
                }
                    vm.chartData = Utils.buildPieChartDataSource(pieData);
             });
        },
              isTrainingTrendsAvailable = function () {
                  return (vm.predictionScores !== null && !angular.isUndefined(vm.predictionScores));
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
                   getTrainingSetDiscrepancies = function (config) {
                       config = config || {};
                       AnalysisSetService.getTrainingSetDiscrepancies(config).then(function (data) {
                           if (data !== null && !angular.isUndefined(data)) {
                               if (data.length > 0) {
                                   vm.trainingSetDiscrepancies = data;
                                   angular.forEach(data, function (obj, idx) {
                                       obj.DifferenceRate = isNaN(obj.DifferenceRate) ? 0 : obj.DifferenceRate;
                                       obj.Round = parseInt(idx + 2, 10);
                                   });
                                   var miniChartData = Utils.buildTrainingDiscrepancyData(data);
                                   miniChartData.chartArea.height = 275;
                                   vm.trainingDiscrepanciesData = miniChartData;
                                   vm.trainingDiscrepanciesGridData = Utils.buildTrainingDiscrepancyGrid(data);
                               }
                           }

                       });
                   },
                    isDiscrepanciesAvailable = function() {
                return (vm.controlSetdiscrepancies !== null && !angular.isUndefined(vm.controlSetdiscrepancies));
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
                             vm.trainingTrendsData = Utils.buildMiniPredictionScoreChartData(data);
                             vm.trainingTrendsGridData = Utils.buildTrainingTrendsGrid(data);
                             vm.trainingTrendsDataXL = Utils.buildLargePredictionScoreChartData(data);

                         }
                     }
                 });

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

                  initSamplingOptions = function () {
                      vm.confidences = DashboardService.getConfidenceLevels();
                      vm.errorMargins = DashboardService.getMarginOfErrors();
                      vm.confidenceSelected = vm.controlSetDetails.ConfidenceLevel.toString();
                      vm.errorMarginSelected = vm.controlSetDetails.MarginOfError.toString();
                      vm.qcSetNameInvalid = false;
                      vm.qcSetSizeInvalid = false;
                      vm.qcSetPredictionInvalid = false;

                      vm.qcSetOptions = {
                          "Name": "",
                          "Type": "RelevantNotRelevant",
                          "SamplingOptions": "Statistical",
                          "Size": ""
                      };
                      vm.calculateSampleSize();
                  },
                   createQCSetMethod = function () {
                       var postData = {
                           "Name": vm.qcSetOptions.Name,
                           "ConfidenceLevel": (vm.isStatisticalOptionsSelected()) ? vm.confidenceSelected : 0,
                           "MarginOfError": (vm.isStatisticalOptionsSelected()) ? vm.errorMarginSelected : 0,
                           "TotalDocuments": (vm.isStatisticalOptionsSelected()) ? vm.sampleSize : vm.qcSetOptions.Size,
                           "SamplingMethod": vm.qcSetOptions.SamplingOptions,
                           "SubType": vm.qcSetOptions.Type
                       };

                       return AnalysisSetService.createAnalysisSet(STATES.QCSET, postData).then(function () {
                           return getWorkflowStates();
                       });
                   },
            validateQCSetForm = function () {
                var setName = vm.qcSetOptions.Name,
                    setSize = vm.qcSetOptions.Size;

                if (!setName.match(/^(?=^[0-9a-zA-Z])([a-zA-Z0-9-_,. ]{2,20})$/)) {
                    vm.qcSetNameInvalid = true;
                } else {
                    vm.qcSetNameInvalid = false;
                }
                if (vm.isFixedSizeSelected()) {
                    if (!(setSize > 0 && !isNaN(setSize))) {
                        vm.qcSetSizeErrorMessage = vm.localized.QCSetSizeInvalidNumber;
                        vm.qcSetSizeInvalid = true;
                    } else {
                        vm.qcSetSizeInvalid = false;
                    }
                }

                
                return !(vm.qcSetNameInvalid || vm.qcSetSizeInvalid);
            },
            getWorkflowStates = function(config) {
                config = config || {};
                WorkflowStateService.getWorkflowStates(MODULES.ADMIN, config).then(function(result) {
                    vm.workflowStates = addWorkflowActions(result);
                    setReviewStatusLabels();
                  
                });
            },
        addWorkflowActions = function (data) {
                var modified = [];
                angular.forEach(data, function (obj) {
                    if (obj.Name === STATES.TRAININGSET) {
                        obj.action = function () {
                            vm.modals.predictset = true;
                        };
                        obj.select = function () {
                            Utils.modals[STATES.CHANGE.toLowerCase()] = true;
                        };
                    }
                    obj.change = function () {
                        var bool = !WorkflowStateService.workflowChangeAllowed();
                        WorkflowStateService.allowWorkflowChange(bool);
                    };
                    modified.push(obj);
                });
                return modified;
        },
         silentRefresh = function () {
             WorkflowStateService.getWorkflowStates(MODULES.ADMIN, { ignoreLoadingBar: true }).then(function (result) {
                 vm.workflowStates = addWorkflowActions(result);
             });
         },
            init = function () {
                var config = { ignoreLoadingBar: true };              
                loadTrainingSetOverview(config);
                vm.resizeCharts();
            };

        vm.localized = Constants.getProjectResources();
        $scope.$on('LoadTrainingSet', function (events, args) {
            vm.workflowStates = args;
            init();
        });

        vm.modals = Utils.modals;
        vm.isScheduled = false;
        vm.scheduledTime = null;
        vm.enablePrediction = true;
        vm.setScheduledTime = function () {
            var now = new Date();
            var halfHours = Math.ceil(now.getMinutes() / 30);
            if (halfHours === 2) {
                now.setHours(now.getHours() + 1);
            }
            now.setMinutes((halfHours * 30) % 60);
            now.setSeconds(0);
            now.setMilliseconds(0);
            vm.scheduledTime = now;
        };
        vm.isPredictionCodingValid = function() {
            return DashboardService.validatePredictionCoding();
        };
        vm.isControlSetReviewCompleted = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.CONTROLSET, ACTIONS.REVIEW);
            };  

        vm.isControlSetReviewInProgress = function () {
            return WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.CONTROLSET, ACTIONS.REVIEW);
    };

        vm.isInvalidDateTime = function () {
            if (!vm.isScheduled) {
                return false;
            }
            var now = new Date();
            return (vm.scheduledTime < now);
        };
        vm.reviewStatus = vm.localized.ReviewNotStarted;
        vm.disableVerificationSet = false;
        vm.adminAction = "";
        vm.reviewerAction = "";
        vm.trainingTrendView = "0";
        vm.showTrainingDiscrepancies = function () {
            return vm.hasCompletedTrainingRounds() && vm.trainingDiscrepanciesData !== null;
        };
        vm.isTrainingSetReviewCompleted = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.TRAININGSET, ACTIONS.REVIEW);
        };
        vm.isTrainingStateCurrent = function () {
            return WorkflowStateService.isStateCurrent(STATES.TRAININGSET);
        };
        vm.isPredictAllCompleted = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.PREDICTSET, ACTIONS.REVIEW);
        };

        vm.isQCSetReviewCompleted = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.REVIEW);
        };
        vm.resizeCharts = function () {
            kendo.resize($(".k-chart"));
            vm.predictAheadView = "0";
        };
        vm.showRollingAverageGrid = function () {
            return vm.trainingDiscrepanciesGridData !== null && vm.predictAheadView === "1";
        };
        vm.showRollingAverageChart = function () {
            return vm.trainingDiscrepanciesData !== null && vm.predictAheadView === "0";
        };
        vm.isTrainingSetReviewNotStarted = function () {
            return WorkflowStateService.isActionPending(vm.workflowStates, STATES.TRAININGSET, ACTIONS.REVIEW);
        };

        vm.enableRunPrediction = function () {
            return vm.isTrainingSetReviewCompleted() && (WorkflowStateService.isStateCurrent(STATES.TRAININGSET) || WorkflowStateService.isStateCurrent(STATES.PROJECTSETUP));
        };
        vm.enableCreateVerificationSet = function () {
            return vm.isPredictAllCompleted() && WorkflowStateService.isStateCurrent(STATES.PREDICTSET);
        };
        vm.showDiscrepancies = function () {
            return (isDiscrepanciesAvailable() && vm.hasCompletedTrainingRounds());
        };
        vm.isTrainingSetReviewInProgress = function () {
            return WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.TRAININGSET, ACTIONS.REVIEW);
        };

        vm.isQCSetCreated = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.CREATE);
        };

        vm.isStatisticalOptionsSelected = function () {

            if (vm.qcSetOptions === null || angular.isUndefined(vm.qcSetOptions)) {
                return false;
            }
            return vm.qcSetOptions.SamplingOptions === "Statistical";
        };
        vm.isFixedSizeSelected = function () {

            if (vm.qcSetOptions === null || angular.isUndefined(vm.qcSetOptions)) {
                return false;
            }
            return vm.qcSetOptions.SamplingOptions === "FixedSize";
        };

        vm.reviewButtonText = function () {
            var text = vm.isTrainingSetReviewInProgress() || vm.isTrainingSetReviewCompleted() ?
                vm.localized.ContinueReview : vm.localized.StartReview;
            return text;
        };

        vm.enableReviewButton = function () {
            return vm.isTrainingSetReviewNotStarted() || vm.isTrainingSetReviewInProgress();
        };

        vm.calculateSampleSize = function () {
            vm.isLoading = true;
            var selected = {};
            selected.ConfidenceLevel = vm.confidenceSelected;
            selected.MarginOfError = vm.errorMarginSelected;
            DashboardService.getSampleSize(selected).then(function (num) {
                vm.sampleSize = num;
                vm.isLoading = false;
                vm.sampleSizeError = false;
            }, function () {
                vm.sampleSizeError = true;
                vm.isLoading = false;
            });

        };

        vm.setDefaultConfidence = function (e) {
            var index = vm.confidences.indexOfObject("value", vm.controlSetDetails.ConfidenceLevel.toString());
            e.sender.select(index == -1 ? 1 : index);
            e.sender.setOptions({
                change: function () {
                    vm.calculateSampleSize();
                }
            });
        };

        vm.setDefaultMargin = function (e) {
            var index = vm.errorMargins.indexOfObject("value", vm.controlSetDetails.MarginOfError.toString());
            e.sender.select(index == -1 ? 1 : index);
            e.sender.setOptions({
                change: function () {
                    vm.calculateSampleSize();
                }
            });
        };

        vm.setSelectedConfidence = function (e) {
            var index = e.item.index();
            vm.confidenceSelected = vm.confidences[index].value;
        };


        vm.setSelectedMargin = function (e) {
            var index = e.item.index();
            vm.errorMarginSelected = vm.errorMargins[index].value;
        };


        vm.viewTrainingReviewDocView = function (binderId, roundNumber) {
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/from_review/to_docview/All";
            var PCUrl = "/app/review/approot#/analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/from_review/to_docview/All";
            Utils.loadPage(EVUrl, PCUrl);
        };

        vm.viewTrainingSetDocumentList = function (binderId, roundNumber, coding) {
            var from = vm.isAdminUser ? "from_admin" : "from_review";
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/" + from + "/to_doclist/" + coding;
            var PCUrl = "/app/review/approot#/analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/" + from + "/to_doclist/" + coding;
            Utils.loadPage(EVUrl, PCUrl);
        };

        vm.showTrainingTrendsChart = function () {
            return vm.trainingTrendsData !== null && vm.trainingTrendView === "0";
        };
        vm.showTrainingTrendsGrid = function () {
            return vm.trainingTrendsDataGridData !== null && vm.trainingTrendView === "1";
        };

        vm.showTrainingScores = function () {
            return isTrainingTrendsAvailable() && vm.hasCompletedTrainingRounds();
        };
        
        vm.showCreateQCSetModal = function () {
            initSamplingOptions();
            vm.ShowQcSetPrerequisiteErrorMessage = false;
            DashboardService.validatePredictionCoding().then(function(isValid) {
                if (isValid === true) {
                    vm.ShowQcSetPrerequisiteErrorMessage = false;
                    vm.EnableQCSetModalConfirm = false;
                } else {
                      vm.EnableQCSetModalConfirm = true;
                      vm.ShowQcSetPrerequisiteErrorMessage = true;
                }
            });
           vm.modals.qcset = true;

        };
        vm.runPredicton = function () {

            vm.modals.predictset = true;        

        };

        vm.createQCSet = function () {
            if (!validateQCSetForm()) {
                return false;
            } else {
                if (vm.isFixedSizeSelected()) {
                    return DashboardService.getAvailableDocumentCount().then(function (count) {
                        if (parseInt(vm.qcSetOptions.Size, 10) >= count) {
                            vm.qcSetSizeInvalid = true;
                            vm.qcSetSizeErrorMessage = vm.localized.QCSetSizeOutOfRange + "" + (count - 1);
                        } else {
                            vm.EnableQCSetModalConfirm = false;
                            return createQCSetMethod();
                        }
                    });
                } else {
                    vm.EnableQCSetModalConfirm = false;
                    return createQCSetMethod();
                }
            }
        };
        vm.startPredictAll = function () {
            vm.enablePrediction = false;
            var postData = {
                "isSchedule": vm.isScheduled,
                "StartTime": (vm.scheduledTime === null) ? (new Date()).toISOString() : vm.scheduledTime.toISOString()
            };
            
            return DashboardService.schedulePredictAll(postData).then(function () {                
                var config = { ignoreLoadingBar: true };
                getWorkflowStates(config);
                return vm.workflowStates;
            });

        };
        vm.showTrainingTrends = function () {
            return (isTrainingTrendsAvailable() && vm.hasCompletedTrainingRounds());
        };
        vm.startReview = function () {
            var putData = [{
                "Name": STATES.TRAININGSET,
                "CreateStatus": STATUS.COMPLETED,
                "ReviewStatus": STATUS.INPROGRESS,
                "Order": 3
            }];
            WorkflowStateService.updateWorkflowState(putData, 0).then(function () {
                var binderId = vm.trainingSetDetails.CurrentRoundProgress.BinderId;
                var roundNumber = vm.trainingSetDetails.CurrentRound;
                vm.viewTrainingReviewDocView(binderId, roundNumber);
            });
        };
        vm.confirmChangeWorkflow = function () {
            var selectedState = WorkflowStateService.getSelectedWorkflowState();
            var putData = [{
                "Name": "PredictSet",
                "CreateStatus": STATUS.NOTSTARTED,
                "ReviewStatus": STATUS.NOTSTARTED,
                "Order": 4
            }];
            return WorkflowStateService.updateWorkflowState(putData, 0).then(function () {
                vm.modals.change = false;
                WorkflowStateService.updateWorkflowChangedStatus(true);               
                var config = { ignoreLoadingBar: true };
                getWorkflowStates(config);                
                return WorkflowStateService.allowWorkflowChange(false);
            });

        };
        vm.changeWorkflow = function() {
            vm.modals.change = true;
        };
        vm.hasCompletedTrainingRounds = function () {
            if (vm.trainingSetDetails === null || angular.isUndefined(vm.trainingSetDetails)) {
                return false;
            }
            return parseInt(vm.trainingSetDetails.RoundsCompleted, 10) > 0;
        };
        vm.setTrainingConfigDetails = function () {
            angular.forEach(vm.trainingSetConfig.Items, function (item) {
                item.Data;
                if (item.Type === "Relevant") {
                    item.Data = vm.trainingSetDetails.CurrentRoundProgress.NumberOfRelevantDocuments;
                } else if (item.Type === "Not_Relevant") {
                    item.Data = vm.trainingSetDetails.CurrentRoundProgress.NumberOfNotRelevantDocuments;
                } else if (item.Type === "Skipped") {
                    item.Data = vm.trainingSetDetails.CurrentRoundProgress.NumberOfSkippedDocuments;
                }
                else if (item.Type === "Not_Coded") {
                    item.Data = vm.trainingSetDetails.CurrentRoundProgress.NumberOfNotCodedDocuments;
                }
            });

            angular.forEach(vm.trainingSetTotalConfig.Items, function (item) {
                item.Data;
                if (item.Type === "Relevant") {
                    item.Data = vm.trainingSetDetails.CompletedRoundsSummary.NumberOfRelevantDocuments;
                } else if (item.Type === "Not_Relevant") {
                    item.Data = vm.trainingSetDetails.CompletedRoundsSummary.NumberOfNotRelevantDocuments;
                } else if (item.Type === "Skipped") {
                    item.Data = vm.trainingSetDetails.CompletedRoundsSummary.NumberOfSkippedDocuments;
                }
                else if (item.Type === "Not_Coded") {
                    item.Data = vm.trainingSetDetails.CompletedRoundsSummary.NumberOfNotCodedDocuments;
                }
            });
        };

        vm.isCreated = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.TRAININGSET, ACTIONS.CREATE);
        };

        vm.isPredictionJobComplete = function () {
            var currentState = WorkflowStateService.getCurrentWorkflowState(vm.workflowStates);
            return currentState.Name == STATES.PREDICTSET && WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.PREDICTSET, ACTIONS.REVIEW);
        };

        vm.isPredictionJobInProgress = function () {
            var currentState = WorkflowStateService.getCurrentWorkflowState(vm.workflowStates);
            return currentState.Name == STATES.PREDICTSET && WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.PREDICTSET, ACTIONS.CREATE);
        };

        vm.showTrainingSummaryGrid = function () {
            return vm.trainingSetSummaryGridData !== null && vm.trainingSummaryView === "1";
        };
        vm.trainingSetConfig =
        {
            Items: [
                {
                    Type: "Relevant"
                },
                {
                    Type: "Not_Relevant"
                },
                {
                    Type: "Skipped"
                },
                {
                    Type: "Not_Coded"
                }
            ]
        };


        vm.ShowQcSetPrerequisiteErrorMessage = false;
        vm.EnableQCSetModalConfirm = true;
        vm.trainingSetTotalConfig =
        {
                Items: [
                {
                    Type: "Relevant"
                },
                {
                    Type: "Not_Relevant"
                },
                {
                    Type: "Skipped"
                }
            ]
        };

        vm.toggleListIcon = function() {
            vm.trainingSummaryView = (vm.trainingSummaryView === "0") ? "1" : "0";
        };
        vm.viewTrainingSetListforAdmin = function (coding) {
            var binderId = vm.trainingSetDetails.CurrentRoundProgress.BinderId;
            var roundNumber = vm.trainingSetDetails.CurrentRound;
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/from_admin/to_doclist/" + coding;
            var PCUrl = "/app/review/approot#/analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/from_admin/to_doclist/" + coding;
            Utils.loadPage(EVUrl, PCUrl);
        };

        vm.viewTrainingSetListforReview = function (coding) {
            var binderId = vm.trainingSetDetails.CurrentRoundProgress.BinderId;
            var roundNumber = vm.trainingSetDetails.CurrentRound;
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/from_Review/to_doclist/" + coding;
            var PCUrl = "/app/review/approot#/analysissets/trainingset/" +
                roundNumber + "/" + binderId + "/from_Review/to_doclist/" + coding;
            Utils.loadPage(EVUrl, PCUrl);
        };


    };

    angular.module("app").controller("TrainingSetController", TrainingSetController);
    TrainingSetController.$inject = ["DashboardService", "WorkflowStateService",
"AnalysisSetService", "Utils", "Constants", "AppStateService", "$interval", "$scope"];
}());