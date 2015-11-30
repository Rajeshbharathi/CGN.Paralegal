/* 
 * Workflow Wizard Directive
 **/

(function () {
    "use strict";

    angular.module("app")
        .directive("lnWorkflowWizard", ["WorkflowStateService", "Constants", "Utils",
            function (WorkflowStateService, Constants, Utils) {

            var STATES = Constants.getStateLabels(),
                ACTIONS = Constants.getActionLabels(),
                enabledCreateButtonStates = [STATES.PROJECTSETUP, STATES.TRAININGSET, STATES.PREDICTSET],
                enabledReviewButtonStates = [STATES.CONTROLSET, STATES.TRAININGSET,STATES.QCSET],
                enabledChangeButtonStates = [STATES.TRAININGSET,STATES.QCSET];
                

            return {
                restrict: "EA",
                scope: {
                    workflowStates: "=lnWorkflowWizardState",
                    documentReviewDetails: "=?lnWorkflowWizardReviewDetails",
                    changeCallback:"&?lnWorkflowWizardOnChange",
                },
                templateUrl: "/App/common/directives/WorkflowWizardDirectiveView.html",
                link: function (scope, element, attrs, ctrl) {
                    if (attrs.hasOwnProperty("lnWorkflowWizardReview")) {
                        scope.module = "REVIEW";
                        scope.reviewMode = true;
                    } else {
                        scope.module = "ADMIN";
                        scope.reviewMode = false;
                    }
                    scope.localized = Constants.getCommonResources();
                    scope.isCurrent = WorkflowStateService.isCurrent;
                    scope.isPending = WorkflowStateService.isPending;
                    scope.isCompleted = WorkflowStateService.isCompleted;
                    scope.isStateCurrent = WorkflowStateService.isStateCurrent;
                    scope.isStateInProgress = WorkflowStateService.isStateInProgress;
                    scope.isStatePending = WorkflowStateService.isStatePending;
                    scope.isStateCompleted = WorkflowStateService.isStateCompleted;
                    scope.isActionCompleted = WorkflowStateService.isActionCompleted;
                    scope.isActionPending = WorkflowStateService.isActionPending;
                    scope.isActionInProgress = WorkflowStateService.isActionInProgress;
                    scope.isCurrentActionCompleted = WorkflowStateService.isCurrentActionCompleted;
                    scope.isCurrentActionPending = WorkflowStateService.isCurrentActionPending;
                    scope.isCurrentActionInProgress = WorkflowStateService.isCurrentActionInProgress;
                    scope.getCurrentActionLabel = WorkflowStateService.getCurrentActionLabel;
                    scope.getNextActionLabel = WorkflowStateService.getNextActionLabel;
                    
                    scope.enableCreateButton = function (state) {
                        if (enabledCreateButtonStates.indexOf(state.Name) !== -1) {
                            return scope.isStateCompleted(state, scope.module);
                        }
                        
                        return false;
                    };
                    
                    scope.nextStateLabels = [];
                    scope.dropdownSelectLabel = {
                        "Name": "",
                        "adminActionLabel": scope.localized.Select
                    };
                    
                    scope.enableChangeButton = function (state) {
                        var bool = false;
                        if (enabledChangeButtonStates.indexOf(state.Name) !== -1) {
                            if(state.Name.toUpperCase()===STATES.TRAININGSET.toUpperCase()){
                                bool = bool || scope.isActionInProgress(scope.workflowStates, state.Name, ACTIONS.REVIEW);
                                
                            }
                            if(state.Name.toUpperCase()===STATES.QCSET.toUpperCase()){
                                bool = bool || scope.isActionCompleted(scope.workflowStates, state.Name, ACTIONS.REVIEW);
                            }
                        }
                        return bool;
                    };
                    scope.selectedState = null;

                    scope.setSelectedState = function (e) {
                        var index = e.item.index();
                        var dataItem=scope.nextStateLabels[index - 1];
                        if (index === 0 || dataItem.disabled) {
                            scope.selectedState = null;
                        } else {
                            scope.selectedState = dataItem;
                        }
                        WorkflowStateService.storeSelectedWorkflowState(scope.selectedState);
                    };

                    scope.showWorkflowChangeDropdown = function () {
                        return WorkflowStateService.workflowChangeAllowed();
                    };

                    scope.refreshDropdown = function () {
                        return Utils.modals[STATES.CHANGE.toLowerCase()];
                    };
                    
                    scope.showStartReviewButton = function (state) {
                        var check1 = false;
                        var check2 = false;
                        if (enabledReviewButtonStates.indexOf(state.Name) !== -1) {
                            if (state.Name === STATES.CONTROLSET) {
                                check1 = scope.isActionCompleted(scope.workflowStates, state.Name, ACTIONS.CREATE);
                                check2 = !scope.isActionInProgress(scope.workflowStates, state.Name, ACTIONS.REVIEW);

                            }
                            if (state.Name === STATES.QCSET) {
                                check1 = scope.isActionCompleted(scope.workflowStates, state.Name, ACTIONS.CREATE);
                                check2 = !scope.isActionInProgress(scope.workflowStates, state.Name, ACTIONS.REVIEW);
                                check2 = check2 && !scope.isActionCompleted(scope.workflowStates, state.Name, ACTIONS.REVIEW);

                            }
                            if (state.Name === STATES.TRAININGSET) {
                                check1 = scope.isActionCompleted(scope.workflowStates, state.Name, ACTIONS.CREATE);
                                check2 = scope.isActionPending(scope.workflowStates, state.Name, ACTIONS.REVIEW);

                            }
                            if (check1 && check2) {
                                return true;
                            }
                        }

                        return false;
                    };
                    scope.showContinueReviewButton = function (state) {
                        if (enabledReviewButtonStates.indexOf(state.Name) !== -1) {
                            return scope.isActionInProgress(scope.workflowStates, state.Name, ACTIONS.REVIEW);
                        }

                        return false;
                    };
                    /* Angular $scope.$watch syntax restriction*/
                    /* jshint unused: false */
                    scope.$watch(function () {
                        var areWorkflowStatesAvailable = (scope.workflowStates !== null && !angular.isUndefined(scope.workflowStates));
                        var isReviewDetailsAvailable = true;
                        if (scope.reviewMode) {
                            isReviewDetailsAvailable = (scope.documentReviewDetails !== null && !angular.isUndefined(scope.documentReviewDetails));
                        }

                        return areWorkflowStatesAvailable && isReviewDetailsAvailable;
                    }, function (newValue, oldValue) {
                        if (newValue) {
                            var currentStateName = WorkflowStateService.currentWorkflowState().Name;
                            if (scope.reviewMode) {
                                if (scope.isActionInProgress(scope.workflowStates, currentStateName, ACTIONS.REVIEW)) {
                                    if (currentStateName === STATES.CONTROLSET || currentStateName === STATES.QCSET) {
                                        scope.totalDocs = parseInt(scope.documentReviewDetails.TotalDocuments);
                                        scope.docsCodedTotal = parseInt(scope.documentReviewDetails.NumberOfRelevantDocuments, 10) +
                                            parseInt(scope.documentReviewDetails.NumberOfNotRelevantDocuments, 10);
                                        scope.docsCompletedPercent = Math.floor(((scope.docsCodedTotal / scope.totalDocs) * 100), 2);
                                    }
                                    if (currentStateName === STATES.TRAININGSET) {
                                        scope.currentRoundNumber = scope.documentReviewDetails.CurrentRound;
                                    }
                                }

                            } else {
                                var temp = [];
                                angular.forEach(scope.workflowStates, function (state) {
                                    if (currentStateName === STATES.TRAININGSET && state.Name === STATES.PREDICTSET) {
                                        temp.push(state);
                                    }
                                    if (currentStateName === STATES.QCSET && state.Name === STATES.TRAININGSET) {
                                        temp.push(state);
                                    }
                                });
                                scope.nextStateLabels = temp;
                            }
                        }
                    });



                }
            };
        }]);
}());