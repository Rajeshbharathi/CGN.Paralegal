(function () {
    "use strict";
    var WidgetsDefinition = {
        assistedReview: {
            title: "Assisted Review",
            templateUrl: "/App/common/components/widgets/assistedReviewView.html"
        }
    },
        app = angular.module("app");
    app.constant("WidgetsDefinition", WidgetsDefinition);
}());