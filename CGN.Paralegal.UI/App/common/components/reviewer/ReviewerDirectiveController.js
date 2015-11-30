(function () {
    "use strict";
    var ReviewerDirectiveController, app;
    ReviewerDirectiveController = function ($scope,DocumentService, AnalysisSetService, Utils, Constants, WorkflowStateService) {
        var vm, tabsOnRightConfig,
            STATES = Constants.getStateLabels(),
            STATUS = Constants.getStatusLabels();

        vm = this;
        vm.isTrainingRoundOneContinue = false;
        vm.localized = Constants.getReviewerResources();
        vm.currentDocument = {};
        vm.resetNavigator = {
            value: false
        };
        vm.Configuration = {};
    
        tabsOnRightConfig = [{
            collapsible: false,
            resizable: true,
            size: "70%",
            min: "30%",
            scrollable: false
        }, {
            collapsible: false,
            resizable: true,
            size: "30%",
            min: "20%",
            scrollable: false
        }];
        
        vm.splitterTabRightOptions = {
            orientation: "horizontal",
            panes: tabsOnRightConfig
        };

        vm.zoomOptions = [{
            key: "font-50",
            value: "50"
        }, {
            key: "font-75",
            value: "75"
        }, {
            key: "font-100",
            value: "100"
        }, {
            key: "font-125",
            value: "125"
        }, {
            key: "font-150",
            value: "150"
        }, {
            key: "font-175",
            value: "175"
        }, {
            key: "font-200",
            value: "200"
        }, {
            key: "font-400",
            value: "400"
        }];
        vm.zoomed = "font-100";

        vm.zoomIn = function () {
            var current = parseInt(vm.zoomed.split("-")[1], 10),
                next;

            if (current === 400 || current === 200) {
                next = 400;
            } else {
                next = (current + 25);
            }

            vm.zoomed = "font-" + next;
        };

        vm.zoomOut = function () {
            var current = parseInt(vm.zoomed.split("-")[1], 10),
                prev;

            if (current === 50) {
                prev = 50;
            } else if (current === 400) {
                prev = 200;
            } else {
                prev = current - 25;
            }

            vm.zoomed = "font-" + prev;
        };

        vm.isZoomedInMax = function () {
            return vm.zoomed === "font-400";
        };
        vm.isZoomedOutMax = function () {
            return vm.zoomed === "font-50";
        };

        vm.isControlSet = function () {
            return vm.Configuration.documentQueryContext.AnalysisSet.Type === Utils.getAnalysisSetType(STATES.CONTROLSET.toUpperCase());
        };
        vm.isTrainingSet = function () {
            return vm.Configuration.documentQueryContext.AnalysisSet.Type === Utils.getAnalysisSetType(STATES.TRAININGSET.toUpperCase());
        };

        vm.isTrainingRoundOneContinued = function() {
            return vm.isTrainingSet() && !AnalysisSetService.isReviewComplete(STATES.TRAININGSET.toUpperCase()) && vm.isTrainingRoundOneContinue;
        };

        vm.isTrainingSetReviewCompleted = function () {
            return vm.isTrainingSet() && AnalysisSetService.isReviewComplete(STATES.TRAININGSET.toUpperCase());
        };

        vm.isTrainingSetReviewInProgress = function () {
            return vm.isTrainingSet() && !AnalysisSetService.isReviewComplete(STATES.TRAININGSET.toUpperCase());
        };

        vm.isQCSet = function () {
            return vm.Configuration.documentQueryContext.AnalysisSet.Type === Utils.getAnalysisSetType(STATES.QCSET.toUpperCase());
        };
        vm.isPredictionSet = function () {
            return vm.Configuration.documentQueryContext.AnalysisSet.Type === Utils.getAnalysisSetType(STATES.PREDICTSET.toUpperCase());
        };

        vm.analysisSetTitle = function () {
            if (!(vm.Configuration && vm.Configuration.documentQueryContext)) {
                return "";
            }
            switch (vm.Configuration.documentQueryContext.AnalysisSet.Type) {
                case Utils.getAnalysisSetType(STATES.CONTROLSET.toUpperCase()):
                    return vm.localized.ControlSetReview;

                case Utils.getAnalysisSetType(STATES.TRAININGSET.toUpperCase()):
                    return vm.localized.TrainingSetReview + " ( " + vm.localized.Set + " " +
                        vm.Configuration.currentSetRound + " )";

                case Utils.getAnalysisSetType(STATES.QCSET.toUpperCase()):
                    return vm.localized.QCSetReview + " ( " +
                        vm.Configuration.documentQueryContext.AnalysisSet.Name + " )";

                case Utils.getAnalysisSetType(STATES.PREDICTSET.toUpperCase()):
                    return vm.localized.PredictionSetReview;

                case Utils.getAnalysisSetType(STATES.ALLDOCUMENTS.toUpperCase()):
                    return vm.localized.AllDocumentsReview;
            }
        };

        vm.currentSetRound = 0;
        vm.nextSetRound = 0;

        function updateCurrentDocument(response) {
            var validDoc = false;
            var refId = response.DocumentReferenceId;
            if (refId === null || angular.isUndefined(refId) || (angular.isString(refId) && refId.length === 0)) {
                if (!$scope.isFilteredSet) {
                    vm.currentSetRound = parseInt(vm.Configuration.currentSetRound, 10);
                    vm.nextSetRound = parseInt(vm.Configuration.currentSetRound, 10) + 1;
                    var setType = vm.Configuration.documentQueryContext.AnalysisSet.Type.toUpperCase();
                    
                    if (setType === STATES.TRAININGSET.toUpperCase()) {
                        WorkflowStateService.getChangedWorkflowState().then(function(state) {
                            if (state !== null && !angular.isUndefined(state)) {
                                if (state.Name === STATES.PREDICTSET) {
                                    AnalysisSetService.setReviewComplete(setType);
                                    vm.finishReview(setType); // Finish training popup
                                } else if (vm.currentSetRound === 1) {
                                    AnalysisSetService.getAddDocumentsToAnalysisSet().then(function(data) {
                                        if (data !== null && !angular.isUndefined(data)) {
                                            if (data === 50) {
                                                vm.isTrainingRoundOneContinue = true;
                                            } else {
                                                vm.isTrainingRoundOneContinue = false;
                                            }
                                        }
                                        vm.finishReview(setType); // All in one designation popup
                                    });
                                } else { // Continue training popup
                                    vm.finishReview(setType);
                                }
                            }
                        });
                    } else {
                        AnalysisSetService.setReviewComplete(setType);
                        vm.finishReview(setType);
                    }
                }
                
            } else {
                validDoc = true;
                vm.totalDocuments = response.TotalDocumentCount;
                vm.currentDocument = response;
                vm.currentDocument.TotalDocumentCount = vm.Configuration.documentQueryContext.TotalDocuments;                
                vm.currentDocument.Content = response.Pages.Text;
                if (response.DocumentIndexId !== 0) {
                    vm.Configuration.selectedDocument.DocSequenceId = response.DocumentIndexId;
                    vm.Configuration.selectedDocument.DocReferenceId = response.DocumentReferenceId;
                } else {
                    response.DocumentIndexId = parseInt(vm.Configuration.selectedDocument.DocSequenceId, 10);
                }

                vm.totalPages = response.Pages.TotalPageCount;
                vm.currentPageIndex = response.Pages.Index;
                DocumentService.storeCurrentDocument(vm.currentDocument);
            }
            $(".reviewer").focus();

            return validDoc;
        }

        $scope.getNextDocument = function (idx, isUncoded) {
            if (isNaN(idx)) {
                idx = 1;
            }

            isUncoded = angular.isString(isUncoded) ? isUncoded : null;
            
            if (angular.isUndefined(vm.Configuration.docListIdMapper[idx])) {
                vm.Configuration.documentQueryContext.UpdatedPageIndex = Math.ceil(idx / vm.Configuration.documentQueryContext.PageSize);

                var postData = vm.Configuration.documentQueryContext;
                postData.PageIndex = Math.ceil(idx / vm.Configuration.documentQueryContext.PageSize);
                DocumentService.getDocumentlist(postData, {}).then(function (data) {
                    angular.forEach(data.Documents, function (obj) {
                        vm.Configuration.docListIdMapper[obj.Id] = obj.ReferenceId;
                    });
                    vm.Configuration.documentQueryContext.TotalDocuments = data.Total;
                    vm.Configuration.selectedDocument.DocSequenceId = idx;
                    vm.Configuration.selectedDocument.DocReferenceId = vm.Configuration.docListIdMapper[idx];
                    var index = ((isUncoded === null) ?
                        vm.Configuration.selectedDocument.DocReferenceId :
                        vm.Configuration.selectedDocument.DocSequenceId);
                    DocumentService.getDocument(index, vm.Configuration.documentQueryContext, isUncoded)
                        .then(function (response) {
                            $('.scroller').parent().scrollTop(0);
                            return updateCurrentDocument(response);
                    });
                });

            } else {
                vm.Configuration.selectedDocument.DocSequenceId = idx;
                vm.Configuration.selectedDocument.DocReferenceId = vm.Configuration.docListIdMapper[idx];
                var index = isUncoded === null ? vm.Configuration.selectedDocument.DocReferenceId : vm.Configuration.selectedDocument.DocSequenceId;
                DocumentService.getDocument(index, vm.Configuration.documentQueryContext, isUncoded)
                .then(function (response) {
                    $('.scroller').parent().scrollTop(0);
                    return updateCurrentDocument(response);
                });
            }
        };

        vm.loadMore = function () {
            var next = parseInt(vm.currentPageIndex, 10) + 1;
            var total = parseInt(vm.totalPages, 10);
            if (next < total) {
                DocumentService.getPage(vm.Configuration.documentQueryContext, next)
                    .then(function (response) {
                        vm.currentDocument.Content += response.Text;
                        vm.currentPageIndex = response.Index;
                 });
             }
        };

        vm.setReviewComplete = {
            "CONTROLSET": false,
            "TRAININGSET": false,
            "PREDICTSET": false,
            "QCSET": false
        };

        vm.showFinishModal = function () {
            var bool = false;
            angular.forEach(vm.setReviewComplete, function (value) {
                bool = bool || value;
            });
            return bool;
        };

        vm.finishReview = function (setType) {
            //new behavior: mark the control set finished before showing the finish popup
            if (vm.isControlSet()) {
                vm.finishCompleteSetReview();
            }

            vm.setReviewComplete[setType.toUpperCase()] = true;
        };
        vm.hideFinishModal = function () {
            var setType = vm.Configuration.documentQueryContext.AnalysisSet.Type.toUpperCase();
            vm.setReviewComplete[setType.toUpperCase()] = false;
            if (Object.keys(vm.currentDocument).length === 0 || angular.isUndefined(vm.currentDocument.DocumentReferenceId)) {
                var refId = vm.Configuration.docListIdMapper[1];
                $scope.getNextDocument(refId, null);
            }

        };

        vm.hideFinishModalAndContinueTrainingReview = function () {
            var setType = vm.Configuration.documentQueryContext.AnalysisSet.Type.toUpperCase();
            vm.setReviewComplete[setType.toUpperCase()] = false;
            var isUncoded = 'uncoded';
            var idx = vm.Configuration.documentQueryContext.TotalDocuments + 1;
            $scope.getNextDocument(idx, isUncoded);
        };

        
        vm.goToParentDashboard = function () {
            var isAdminView = Utils.getRouteParam("from_module").toLowerCase().indexOf("admin") !== -1;
            var EVUrl, PCUrl;
            if (isAdminView) {
                EVUrl = "/app/adminapp.aspx?mod=analytics&view=dashboard";
                PCUrl = "/app/analytics/approot#/dashboard";
            } else {
                EVUrl = "/app/reviewerapp.aspx?mod=review&view=dashboard";
                PCUrl = "/app/review/approot#/dashboard";
            }
                Utils.loadPage(EVUrl, PCUrl);
        };

        vm.goToListView = function () {
            vm.Configuration.docView = false;
            vm.Configuration.listView = true;

        };
        
        vm.createTrainingSet = function () {
            var setType = vm.Configuration.documentQueryContext.AnalysisSet.Type.toUpperCase(),
                trainingSetNumber;
            if (setType === STATES.CONTROLSET.toUpperCase()) {
                trainingSetNumber = 0;    
            } else {
                trainingSetNumber = vm.Configuration.currentSetRound;
            }
            AnalysisSetService.createTrainingSet().then(function (binderId) {
                binderId = binderId.replace(/['"]+/g, "");
                vm.Configuration.currentSetRound = parseInt(trainingSetNumber, 10) + 1;
                
                var roundNumber = parseInt(trainingSetNumber, 10) + 1;
                var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/trainingset/" +
                    roundNumber + "/" + binderId + "/from_review/to_docview/All";
                var PCUrl = "/app/review/approot#/analysissets/trainingset/" +
                    roundNumber + "/" + binderId + "/from_review/to_docview/All";
                Utils.loadPage(EVUrl, PCUrl);
            });
        };

        vm.continueTraining = function() {
            vm.createTrainingSet();
        };

        vm.finishTraining = function () {
            AnalysisSetService.createTrainingSet().then(function () {
                vm.goToParentDashboard();
            });
        };

        vm.finishCompleteSetReview=function(){
            var setName = vm.Configuration.documentQueryContext.AnalysisSet.Type,
                binderId = vm.Configuration.documentQueryContext.AnalysisSet.BinderId;

            var putData = [{
                "Name": setName,
                "CreateStatus": STATUS.COMPLETED,
                "ReviewStatus": STATUS.COMPLETED,
                "Order": 0
            }];
            return WorkflowStateService.updateWorkflowState(putData, binderId).then(function () {
                //New behavior: we're not creating the first training set in here anymore
                //if(setName.toUpperCase()===STATES.CONTROLSET.toUpperCase()){
                //    vm.createTrainingSet();
                //}
                if(setName.toUpperCase()===STATES.QCSET.toUpperCase()){
                    vm.goToParentDashboard();
                }
                if(setName.toUpperCase()===STATES.TRAININGSET.toUpperCase()){
                    vm.finishTraining();
                }
            });    
        };
    };
    app = angular.module("app");

    app.controller("ReviewerDirectiveController", ReviewerDirectiveController);
    ReviewerDirectiveController.$inject = ["$scope","DocumentService", "AnalysisSetService", "Utils", "Constants", "WorkflowStateService"];
}());
