(function () {
    "use strict";

    var ControlSetController = function (DashboardService, WorkflowStateService, AnalysisSetService, Utils, Constants, AppStateService, $interval, $scope) {
        var vm = this,
            MODULES = Constants.getModuleLabels(),
            STATES = Constants.getStateLabels(),
            ACTIONS = Constants.getActionLabels(),
            STATUS = Constants.getStatusLabels(),
            loadControlSetDetails = function(config) {
                config = config || {};
                AnalysisSetService.getAnalysisSetSummary(STATES.CONTROLSET, config).then(function(result) {
                    vm.controlSetDetails = result;
                    setReviewStatusLabels();
                    vm.controlSetDetails.PercentageOfTotalPopulation = vm.isControlSetCreated() ? result.PercentageOfTotalPopulation : 0;
                    vm.controlSetDetails.EstimatedTotalDocuments = vm.isControlSetCreated() ? result.EstimatedTotalDocuments : 0;

                    vm.controlSetRichnessChartData = Utils.buildControlSetRichnessChartData(vm.controlSetDetails);
                    vm.ControlSetPrevalenceMsg = String.format(vm.localized.ControlSetPrevalenceMsg, vm.controlSetDetails.PercentageOfTotalPopulation.toFixed(1));
                    vm.EstimatedRichnessMsg = String.format(vm.localized.EstimatedRichnessMsg, vm.controlSetDetails.EstimatedTotalDocuments);

                    angular.forEach(vm.controlsetSummary.Items, function(item) {
                        if (item.Type === "Relevant") {
                            item.Data = result.NumberOfRelevantDocuments;
                        } else if (item.Type === "Not_Relevant") {
                            item.Data = result.NumberOfNotRelevantDocuments;
                        } else if (item.Type === "Not_Coded") {
                            item.Data = result.NumberOfNotCodedDocuments;
                        }
                    });
                });
            },
      setReviewStatusLabels = function() {
          var reviewComplete = vm.isControlSetReviewComplete();
            var reviewInProgress = vm.isControlSetReviewInProgress();
            var created = vm.isControlSetCreated();
            var totalDocumentsReviewed = vm.controlSetDetails.TotalDocuments -vm.controlSetDetails.NumberOfNotCodedDocuments;
            var appStateUserGroups = Utils.getappStateUserGroups;
            if (reviewComplete) {
                vm.reviewStatus = vm.localized.ControlSetReviewCompleted;
                vm.reviewerStatus = vm.reviewStatus;
                if (vm.isTrainingSetCreated()) {
                    vm.adminAction = String.format(vm.localized.ControlSetReviewCompletedActionMessage, appStateUserGroups, vm.controlSetDetails.TotalDocuments);
                    vm.reviewerAction = String.format(vm.localized.ControlSetReviewerCompletedActionMessage, vm.controlSetDetails.TotalDocuments);
                } else {
                    vm.adminAction = String.format(vm.localized.ControlSetReviewCompletedNoTrainingActionMessage, appStateUserGroups, vm.controlSetDetails.TotalDocuments);
                    vm.reviewerAction = String.format(vm.localized.ControlSetReviewerCompletedNoTrainingActionMessage, vm.controlSetDetails.TotalDocuments);
                }
                vm.controlsetSummary.Label = vm.localized.Status;
                vm.controlsetSummary.Header = vm.localized.ControlSetReviewCompleted;
                vm.controlsetSummary.Headline = String.format(vm.localized.AllDocumentsReviewed, vm.controlSetDetails.TotalDocuments);
            } else if (reviewInProgress) {
                vm.reviewStatus = vm.localized.ControlSetReviewInProgress;
                vm.reviewerStatus = vm.reviewStatus;
                vm.adminAction = String.format(vm.localized.ControlSetReviewInProgressActionMessage, appStateUserGroups,totalDocumentsReviewed, vm.controlSetDetails.TotalDocuments);
                vm.reviewerAction = String.format(vm.localized.ControlSetReviewerInProgressActionMessage, totalDocumentsReviewed, vm.controlSetDetails.TotalDocuments);
                vm.controlsetSummary.Label = vm.localized.Status;
                vm.controlsetSummary.Header = vm.localized.ControlSetReviewInProgress;
                vm.controlsetSummary.Headline = String.format(vm.localized.DocumentsNeedReview, vm.controlSetDetails.NumberOfNotCodedDocuments);
            } else if (created == true) {
                vm.reviewStatus = vm.localized.ControlSetReviewNotStarted;
                vm.reviewerStatus = vm.reviewStatus;
                vm.adminAction = String.format(vm.localized.ControlSetReviewNotStartedActionMessage, appStateUserGroups, vm.controlSetDetails.TotalDocuments);
                vm.reviewerAction = String.format(vm.localized.ControlSetReviewerNotStartedActionMessage, vm.controlSetDetails.TotalDocuments);
                vm.controlsetSummary.Label = vm.localized.Status;
                vm.controlsetSummary.Header = vm.localized.ControlSetReviewNotStarted;
                vm.controlsetSummary.Headline = String.format(vm.localized.DocumentsNeedReview, vm.controlSetDetails.NumberOfNotCodedDocuments);
            } else if (created == false) {
                vm.reviewStatus = vm.localized.NoControlSet;
                vm.reviewerStatus = vm.localized.ControlSetnotReadyReviewer;
                vm.adminAction = vm.localized.ControlSetNotStartedActionMessage+", "+vm.localized.ClickCreateControlSet;
                vm.reviewerAction = vm.localized.ControlSetNotStartedActionMessage;
                vm.controlsetSummary.Label = vm.localized.Status;
                vm.controlsetSummary.Header = vm.localized.NoControlSet;
            }
        },
            enableCreateControlSet = function() {
                vm.enableCreateControlSet = vm.isProjectCreated() && !vm.isControlSetCreated() && !vm.isControlSetCreateInprogress();
            },
            initSamplingOptions = function() {
                vm.confidences = DashboardService.getConfidenceLevels();
                vm.errorMargins = DashboardService.getMarginOfErrors();
                vm.confidenceSelected = "95";
                vm.errorMarginSelected = "2.5";
                vm.calculateSampleSize();
            },
            init = function() {
                var config = { ignoreLoadingBar: true };
                loadControlSetDetails(config);
                enableCreateControlSet();               
            };

        vm.localized = Constants.getProjectResources();
        vm.modals = Utils.modals;
        $scope.$on('LoadControlSet', function (events, args) {
            vm.workflowStates = args;
            init();
        });

        vm.reviewStatus = vm.localized.ReviewNotStarted;
        vm.adminAction = "";
        vm.reviewerAction = "";

        vm.controlsetSummary =
        {
        Items: [
                {
                    Type: "Relevant"
                },
                {
                    Type: "Not_Relevant"
                },
                {
                    Type: "Not_Coded"
                }
            ]
        };

        vm.showCreateControlSetModal = function() {
            initSamplingOptions();
            vm.modals.controlset = true;
        };

        vm.startReview = function () {
            var putData = [{
                "Name": STATES.CONTROLSET,
                "CreateStatus": STATUS.COMPLETED,
                "ReviewStatus": STATUS.INPROGRESS,
                "Order": 2
            }];
            WorkflowStateService.updateWorkflowState(putData, 0).then(function () {
                var binderId = "0",
                roundNumber = "1",
                EVUrl,
                PCUrl;

                EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/ControlSet" +
                    "/" + roundNumber + "/" + binderId + "/from_review/to_docview/All";
                PCUrl = "/app/review/approot#/analysissets/ControlSet" +
                    "/" + roundNumber + "/" + binderId + "/from_review/to_docview/All";
                Utils.loadPage(EVUrl, PCUrl);
            });
        };

        vm.createControlSet = function() {
            var selected = {};
            selected.ConfidenceLevel = vm.confidenceSelected;
            selected.MarginOfError = vm.errorMarginSelected;
            selected.SampleSize = vm.sampleSize;
            return AnalysisSetService.createAnalysisSet(STATES.CONTROLSET, selected).then(function() {
                vm.enableCreateControlSet = false;
            });
        };

        vm.showCreateTrainingSet = function () {
            return vm.isControlSetReviewComplete() &&
                !vm.isTrainingSetCreateInprogress() &&
                !vm.isTrainingSetCreated() &&
                !vm.isCreateTrainingSetClicked;
        };

        vm.createTrainingSet = function () {
            vm.isCreateTrainingSetClicked = true;
            AnalysisSetService.createTrainingSet();
        };

        vm.sampleSizeError = false;
        vm.calculateSampleSize = function() {
            vm.isLoading = true;
            var selected = {};
            selected.ConfidenceLevel = vm.confidenceSelected;
            selected.MarginOfError = vm.errorMarginSelected;
            DashboardService.getSampleSize(selected).then(function(num) {
                vm.sampleSize = num;
                vm.isLoading = false;
                vm.sampleSizeError = false;
            }, function() {
                vm.sampleSizeError = true;
                vm.isLoading = false;
            });
        };

        vm.isControlSetReviewNotStarted = function () {
            return WorkflowStateService.isActionPending(vm.workflowStates, STATES.CONTROLSET, ACTIONS.REVIEW);
        };

        vm.isControlSetReviewInProgress = function () {
            return WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.CONTROLSET, ACTIONS.REVIEW);
        };

        vm.isControlSetReviewComplete = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.CONTROLSET, ACTIONS.REVIEW);
        };

        vm.isControlSetCreated = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.CONTROLSET, ACTIONS.CREATE);
        };

        vm.isTrainingSetCreateInprogress = function () {
            return WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.TRAININGSET, ACTIONS.CREATE);
        };

        vm.isTrainingSetCreated = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.TRAININGSET, ACTIONS.CREATE);
        };

        vm.isControlSetCreateInprogress = function () {
            return WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.CONTROLSET, ACTIONS.CREATE);
        };
        
        vm.isProjectCreated = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.PROJECTSETUP, ACTIONS.CREATE);
        };
        
        vm.reviewButtonText = function () {
            var text = vm.isControlSetReviewInProgress() || vm.isControlSetReviewComplete() ?
                vm.localized.ContinueReview :
                vm.localized.StartReview;
            return text;
        };

        vm.enableReviewButton = function () {
            return (vm.isControlSetCreated() && vm.isControlSetReviewNotStarted()) || vm.isControlSetReviewInProgress();
        };

        vm.viewDocListForAdmin = function (coding) {
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/ControlSet/1/0/from_admin/to_doclist/" + coding;
            var PCUrl = "/app/review/approot#/analysissets/ControlSet/1/0/from_admin/to_doclist/" + coding;
            Utils.loadPage(EVUrl, PCUrl);
        };

        vm.viewDocListForReview = function (coding) {
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/controlset/1/0/from_review/to_doclist/" + coding ;
            var PCUrl = "/app/review/approot#/analysissets/controlset/1/0/from_review/to_doclist/" + coding;
            Utils.loadPage(EVUrl, PCUrl);
        };
    };

    angular.module("app").controller("ControlSetController", ControlSetController);
    ControlSetController.$inject = ["DashboardService", "WorkflowStateService",
"AnalysisSetService", "Utils", "Constants", "AppStateService", "$interval", "$scope"];
}());