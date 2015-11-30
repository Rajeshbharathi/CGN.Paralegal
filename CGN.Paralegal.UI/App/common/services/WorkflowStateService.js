(function () {
    "use strict";

    var WorkflowStateService = function (WebApiClientService, $filter, Utils, Constants) {

        var STATES = Constants.getStateLabels(),
        ACTIONS = Constants.getActionLabels(),
        MODULES = Constants.getModuleLabels(),
        STATUS = Constants.getStatusLabels(),
        currentStateObject = null,
        currentAction = null,
        changedState = null,
        cacheData = null,
        localized = Constants.getCommonResources(),
        STATELABELS = {
            ADMIN: {
                "ProjectSetup": localized.Setup,
                "ControlSet": localized.Control,
                "TrainingSet": localized.Train,
                "QcSet": localized.QC,
                "PredictSet": localized.Predict,
                "Done": localized.Done
            },
            REVIEW: {
                "ProjectSetup": localized.ProjectSetup,
                "ControlSet": localized.ControlSetReview,
                "TrainingSet": localized.TrainSystem,
                "QcSet": localized.QCSetReview,
                "PredictSet": localized.PredictAll,
                "Done": localized.Done
            }
        },
        NEXTSTATELABELS = {
            "ProjectSetup": "",
            "ControlSet": "",
            "TrainingSet": localized.CreateTrainingSet,
            "PredictSet": localized.PredictAll,
            "QcSet": "",
            "Done": ""
        },
        isWorkflowChangeAllowed = false,
        allowWorkflowChange = function (bool) {
            isWorkflowChangeAllowed = bool;
        },
        workflowChangeAllowed = function () {
            return isWorkflowChangeAllowed;
        },
        workflowStateChangeStatus = false,
        hasWorkflowStateChanged = function () {
            return workflowStateChangeStatus;
        },
        updateWorkflowChangedStatus = function (bool) {
            workflowStateChangeStatus = bool;
        },
        getWorkflowStates = function (module) {
            var url = Utils.getWebApiRouteString("workflowRoute");
            return getChangedWorkflowState().then(function(){
                return WebApiClientService.get(url).then(function (response) {                
                    var data = [];
                    angular.forEach(response.data, function (obj) {
                        obj.label = STATELABELS[module][obj.Name];
                        obj.adminActionLabel = NEXTSTATELABELS[obj.Name];
                        data.push(obj);
                    });
                    currentStateObject = getStateCurrent(data, module);                
                    if (isActionInProgress(data, currentStateObject.Name, ACTIONS.CREATE)) {
                        currentAction = ACTIONS.CREATE;
                    } else {
                        if (isActionCompleted(data, currentStateObject.Name, ACTIONS.CREATE)) {
                            if (isActionPending(data, currentStateObject.Name, ACTIONS.REVIEW)) {
                                currentAction = ACTIONS.CREATE;
                            } else {
                                currentAction = ACTIONS.REVIEW;
                            }
                        }
                    }
                    cacheData = data;
                    return data;
                }, function (err) {
                    throw err;
                });
            });

        },
        getChangedWorkflowState = function () {
            var url = Utils.getWebApiRouteString("changedWorkflowStateRoute");
            return WebApiClientService.get(url).then(function (response) {
                changedState = response.data;
                return response.data;
            }, function (err) {
                throw err;
            });
        },
        getTrainingSetNextLabel=function (){
            if(currentStateObject !==null && changedState!==null){
                if(currentStateObject.Name === STATES.TRAININGSET){
                    if (currentStateObject[ACTIONS.REVIEW] !== STATUS.NOTSTARTED && changedState.Name === STATES.PREDICTSET) {
                        return localized.PredictAll;
                    }else{
                        return localized.ContinueReview;
                    }
                } 
            }
        },
        updateWorkflowState = function (stateList,binderId) {
            var url = Utils.getWebApiRouteString("updatedworkflowRoute").replace("{binderId}",binderId);
            return WebApiClientService.put(url,stateList).then(function (response) {
                return response.data;
            }, function (err) {
                throw err;
            });
        },
        getData = function () {
            return cacheData;
        },
        nextWorkflowState =null,
        storeSelectedWorkflowState = function (state) {
            nextWorkflowState = state;
        },
        getSelectedWorkflowState = function () {
            return nextWorkflowState;
        },

        getStateCurrent = function (arr) {
            var found = false;
            var requiredObj = null;
            angular.forEach(arr, function (obj) {
                if (found) {
                    return;
                }
                if(obj.IsCurrent){
                    requiredObj = obj;
                    found=true;
                }
                
            });
            if (requiredObj !== null) {
                return requiredObj;
            }
            throw new Error("Unable to determine workflow state.");
        },
        currentWorkflowState = function () {
            if (currentStateObject === null) {
                return getStateCurrent();
            }
            return currentStateObject;
        },
        isStateCurrent = function (state) {
            if (state === null || angular.isUndefined(state) || currentStateObject === null) {
                return false;
            }
            if(angular.isString(state)){
                return (state === currentStateObject.Name);
            }
            if(state.hasOwnProperty("Name")){
                return (state.Order === currentStateObject.Order && state.Name === currentStateObject.Name);
            }
            
        },
        isStateInProgress = function (state, module) {
            if (state === null || angular.isUndefined(state)) {
                return false;
            }
            if (module === MODULES.REVIEW) {
                return state[ACTIONS.REVIEW] === STATUS.INPROGRESS;
            }
            return state[ACTIONS.CREATE] === STATUS.INPROGRESS || state[ACTIONS.REVIEW] === STATUS.INPROGRESS;
        },
        isStatePending = function (state, module) {
            if (state === null || angular.isUndefined(state)) {
                return false;
            }

            if (module === MODULES.REVIEW) {
                return state[ACTIONS.REVIEW] === STATUS.NOTSTARTED;
            }
            return state[ACTIONS.CREATE] === STATUS.NOTSTARTED && state[ACTIONS.REVIEW] === STATUS.NOTSTARTED;
        },
        isStateCompleted = function (state, module) {
            if (state === null || angular.isUndefined(state)) {
                return false;
            }
            if (module === MODULES.REVIEW) {
                return state[ACTIONS.REVIEW] === STATUS.COMPLETED;
            }
            return state[ACTIONS.CREATE] === STATUS.COMPLETED && state[ACTIONS.REVIEW] === STATUS.COMPLETED;
        },
        isActionCompleted = function (arr, stateName, action) {
            if (arr === null || angular.isUndefined(arr) || !stateName || !action) {
                return false;
            }
            var selectedState = $filter("filter")(arr, { Name: stateName }, true)[0];

            return selectedState[action] === STATUS.COMPLETED;
        },
        isActionInProgress = function (arr, stateName, action) {
            if (arr === null || angular.isUndefined(arr) || !stateName || !action) {
                return false;
            }
            var selectedState = $filter("filter")(arr, { Name: stateName }, true)[0];

            return selectedState[action] === STATUS.INPROGRESS;
        },
        isActionPending = function (arr, stateName, action) {
            if (arr === null || angular.isUndefined(arr) || !stateName || !action) {
                return false;
            }
            var selectedState = $filter("filter")(arr, { Name: stateName }, true)[0];

            return selectedState[action] === STATUS.NOTSTARTED;
        },
        getCurrentActionLabel = function (state, module) {
            var label = "";

            switch (state.Name) {
                case STATES.PROJECTSETUP:
                    if (state[ACTIONS.CREATE] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.SetupProject : localized.ProjectSetup;
                    }
                    break;
                case STATES.CONTROLSET:
                    if (state[ACTIONS.CREATE] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.CreateControlSet : localized.ProjectSetup;
                    }
                    if (state[ACTIONS.REVIEW] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewControlSet : localized.ControlSetReview;
                    }
                    break;
                case STATES.TRAININGSET:
                    if (state[ACTIONS.REVIEW] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewControlSet : localized.ControlSetReview;
                    }
                    if (state[ACTIONS.REVIEW] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewTrainingSet : localized.Training;
                    }
                    break;
                case STATES.PREDICTSET:
                    if (state[ACTIONS.REVIEW] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewTrainingSet : localized.Training;
                    }
                    if (state[ACTIONS.REVIEW] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.PredictAll : localized.PredictAll;
                    }
                    break;
                case STATES.QCSET:
                    if (state[ACTIONS.CREATE] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.PredictAll : localized.PredictAll;
                    }
                    if (state[ACTIONS.CREATE] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.CreateQCSet : localized.PredictAll;
                    }
                    if (state[ACTIONS.REVIEW] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewQCSet : localized.QCSetReview;
                    }
                    
                    break;
                case STATES.Done:
                    label = localized.ReviewQCSet;
                    break;
            }
            return label;
        },
        getNextActionLabel = function (state, module) {
            var label = "";
            switch (state.Name) {
                case STATES.PROJECTSETUP:
                    if (state[ACTIONS.CREATE] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.CreateControlSet : localized.ControlSetReview;
                    }
                    break;
                case STATES.CONTROLSET:
                    if (state[ACTIONS.REVIEW] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewControlSet : localized.ControlSetReview;
                    }
                    if (state[ACTIONS.REVIEW] === STATUS.INPROGRESS) {
                        label = module === MODULES.ADMIN ? localized.ContinueReview : localized.ControlSetReview;
                    }
                    if (state[ACTIONS.REVIEW] === STATUS.COMPLETED) {
                        label = module === MODULES.ADMIN ? localized.ReviewTrainingSet : localized.Training;
                    }

                    break;
                case STATES.TRAININGSET:
                    if (state[ACTIONS.REVIEW] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewTrainingSet : localized.Training;
                    }
                    if (state[ACTIONS.REVIEW] === STATUS.INPROGRESS) {
                        label = module === MODULES.ADMIN ? getTrainingSetNextLabel() : localized.Round;
                    }
                    if (state[ACTIONS.REVIEW] === STATUS.COMPLETED || hasWorkflowStateChanged()) {
                        label = localized.PredictAll;
                    }

                    break;
                case STATES.PREDICTSET:
                    if (state[ACTIONS.REVIEW] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.PredictAll : localized.PredictAll;
                    }
                    if (state[ACTIONS.REVIEW] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.CreateQCSet : localized.QCSetReview;
                    }

                    break;
                case STATES.QCSET:                   
                    if (state[ACTIONS.CREATE] === STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.CreateQCSet : localized.QCSetReview;
                    }
                    if (state[ACTIONS.CREATE] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.ReviewQCSet : localized.QCSetReview;
                    }
                    if (state[ACTIONS.REVIEW] !== STATUS.NOTSTARTED) {
                        label = module === MODULES.ADMIN ? localized.FinalizeProject : localized.FinalizeProject;
                    }

                    break;
                case STATES.Done:
                    label = localized.FinalizeProject;
                    break;
            }

            return label;
        },
        getCurrentWorkflowState = function(arr) {
                var found = false;
                var currentState = null;
                angular.forEach(arr, function(obj) {
                    if (found) {
                        return;
                    }
                    if (obj.IsCurrent) {
                        currentState = obj;
                        found = true;
                    }
                });

                return currentState;
            },
        isCurrentActionCompleted = function (state) {
            if (state === null || angular.isUndefined(state)) {
                return false;
            }
            if (state.Name === currentStateObject.Name) {
                return state[currentAction] === STATUS.COMPLETED;
            }
            return false;
        },
        isCurrentActionPending = function (state) {
            if (state === null || angular.isUndefined(state)) {
                return false;
            }
            if (state.Name === currentStateObject.Name) {
                return state[currentAction] === STATUS.NOTSTARTED;
            }
            return false;
        },
        isCurrentActionInProgress = function (state) {
            if (state === null || angular.isUndefined(state)) {
                return false;
            }
            if (state.Name === currentStateObject.Name) {
                return state[currentAction] === STATUS.INPROGRESS;
            }
            return false;
        },
        isCurrent= function(state){
            return state.Order === currentWorkflowState().Order;
        },
        isPending= function(state){
            return state.Order > currentWorkflowState().Order;
        },
        isCompleted = function(state){
            if(state.Order < currentWorkflowState().Order){
                return true;
            }
            if(state.Order === currentWorkflowState().Order){
                return isStateCompleted(state,"ADMIN");
            }

        };

        return {
            getWorkflowStates: getWorkflowStates,
            isCurrent: isCurrent,
            isCompleted: isCompleted,
            isPending: isPending,
            isStateCurrent: isStateCurrent,
            isStateInProgress: isStateInProgress,
            isStatePending: isStatePending,
            isStateCompleted: isStateCompleted,
            isActionCompleted: isActionCompleted,
            isActionPending: isActionPending,
            isActionInProgress: isActionInProgress,
            isCurrentActionCompleted: isCurrentActionCompleted,
            isCurrentActionPending: isCurrentActionPending,
            isCurrentActionInProgress: isCurrentActionInProgress,
            getCurrentActionLabel: getCurrentActionLabel,
            getNextActionLabel: getNextActionLabel,
            currentWorkflowState: currentWorkflowState,
            getChangedWorkflowState: getChangedWorkflowState,
            updateWorkflowState: updateWorkflowState,
            allowWorkflowChange: allowWorkflowChange,
            workflowChangeAllowed: workflowChangeAllowed,
            hasWorkflowStateChanged: hasWorkflowStateChanged,
            updateWorkflowChangedStatus: updateWorkflowChangedStatus,
            storeSelectedWorkflowState: storeSelectedWorkflowState,
            getSelectedWorkflowState: getSelectedWorkflowState,
            getCurrentWorkflowState: getCurrentWorkflowState,
            getData: getData
        };

    };

    angular.module("app").service("WorkflowStateService", WorkflowStateService);
    WorkflowStateService.$inject = ["WebApiClientService", "$filter", "Utils", "Constants"];

}());