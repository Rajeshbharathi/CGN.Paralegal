(function () {
    "use strict";
    var ReviewerConfiguration = {
        service: 'DocumentSetModelFactory',
        layout: {
            MODE_LEFT: "MODE_LEFT",
            MODE_RIGHT: "MODE_RIGHT"
        },
        relevance: {
            NONE: "NONE",
            NONRELEVANT: "NONRELEVANT",
            RELEVANT: "RELEVANT"
        },
        widgets: {
            enabled: true,
            list: ['assistedReview', 'fields']
        }
    },
        app = angular.module("app");

    app.constant('ReviewerConfiguration', ReviewerConfiguration);
    app.constant('ReviewResources', ReviewResources);
}());
