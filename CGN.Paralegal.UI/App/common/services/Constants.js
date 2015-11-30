(function () {
    "use strict";

    angular.module("app").factory("Constants", [function () {

        var MODULES = Object.freeze({
                ADMIN: "ADMIN",
                REVIEW: "REVIEW"
            }),
            STATES = Object.freeze({
                PROJECTSETUP: "ProjectSetup",
                CONTROLSET: "ControlSet",
                TRAININGSET: "TrainingSet",
                QCSET: "QcSet",
                PREDICTSET: "PredictSet",
                DONE: "Done",
                CHANGE: "Change",
                ALLDOCUMENTS:"AllDocuments"
            }),
            STATUS = Object.freeze({
                NOTSTARTED: "NotStarted",
                INPROGRESS: "Inprogress",
                COMPLETED: "Completed"
            }),
            ACTIONS = Object.freeze({
                CREATE: "CreateStatus",
                REVIEW: "ReviewStatus"
            }),
            RELEVANCE= Object.freeze({
                NONE: "Not_Coded",
                NOTRELEVANT: "Not_Relevant",
                RELEVANT: "Relevant",
                SKIPPED: "Skipped"
            }),
            COMMONRESOURCES = Object.freeze(CommonResources),
            PROJECTRESOURCES =  Object.freeze(ProjectResources),
            REVIEWRESOURCES =  Object.freeze(ReviewResources),
            REVIEWERRESOURCES =  Object.freeze(ReviewerResources),
            getModuleLabels = function () {
                return MODULES;
            },
            getStateLabels = function () {
                return STATES;
            },
            getStatusLabels = function () {
                return STATUS;
            },
            getActionLabels = function () {
                return ACTIONS;
            },
            getCodingLabels = function () {
                return RELEVANCE;
            },
            getCommonResources = function () {
                return COMMONRESOURCES;
            },
            getProjectResources = function () {
                return PROJECTRESOURCES;
            },
            getReviewResources = function () {
                return REVIEWRESOURCES;
            },
            getReviewerResources = function () {
                return REVIEWERRESOURCES;
            };

        return {
            getModuleLabels: getModuleLabels,
            getStateLabels: getStateLabels,
            getStatusLabels: getStatusLabels,
            getActionLabels: getActionLabels,
            getCodingLabels: getCodingLabels,
            getCommonResources: getCommonResources,
            getProjectResources: getProjectResources,
            getReviewResources: getReviewResources,
            getReviewerResources: getReviewerResources
        };
        
    }]);
}());