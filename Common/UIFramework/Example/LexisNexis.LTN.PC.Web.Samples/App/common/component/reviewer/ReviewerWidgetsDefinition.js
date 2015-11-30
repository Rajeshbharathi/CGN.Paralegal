(function () {
    "use strict";
    var WidgetsDefinition = {
        assistedReview: {
            title: "Assisted Review",
            templateUrl: "/App/common/component/widgets/assistedReviewView.html"
        },
        fields: {
            title: "Fields",
            templateUrl: "/App/common/component/widgets/fieldsView.html"
        }
    },
        app = angular.module("app");
    app.constant('WidgetsDefinition', WidgetsDefinition);
}());