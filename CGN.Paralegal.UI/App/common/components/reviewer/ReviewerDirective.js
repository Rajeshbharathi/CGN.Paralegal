(function () {
    "use strict";
    var lnReviewer = function (DocumentService,WidgetsDefinition,Constants) {
        return {
            restrict: "EA",
            scope: {"config":"=lnReviewer"},
            priority: 1000,
            terminal: true,
            controller: "ReviewerDirectiveController",
            controllerAs: "ReviewerDirectiveController",
            templateUrl: "/App/common/components/reviewer/ReviewerDirectiveView.html",
            link: function (scope, element, attrs, ctrl) {
                var i, len, ref, 
                    init = function () {
                        if (ctrl.isWidgetsEnabled) {
                            ctrl.widgets = [];
                            ref = ctrl.Configuration.widgets.list;
                            for (i = 0, len = ref.length; i < len; i = i + 1) {
                                ctrl.widgets.push(WidgetsDefinition[ref[i]]);
                            }
                        }
                        var docQuery=ctrl.Configuration.documentQueryContext;
                        if(docQuery){
                            var hasFilters = docQuery.Filters.length>0;
                            var hasSort= docQuery.Sort.length>0;
                            var hasSearch = docQuery.KeyWord.length > 0;
                            var setType = docQuery.AnalysisSet.Type.toUpperCase();
                            var allDocs = (Constants.getStateLabels().ALLDOCUMENTS.toUpperCase() === setType);


                            scope.isFilteredSet = hasFilters || hasSort || hasSearch;
                            if(scope.isFilteredSet || allDocs){
                                scope.autoadvance = {checked : false};
                            }else{
                                if(!scope.autoadvance){
                                    scope.autoadvance = {checked :  true};
                                }
                            }
                        }

                        if (ctrl.Configuration.service) {
                            if (ctrl.Configuration.selectedDocument.DocSequenceId !== null) {
                                scope.getNextDocument(ctrl.Configuration.selectedDocument.DocSequenceId, null);
                            } else {
                                scope.getNextDocument(1, "uncoded");
                            }
                        }

                    },
                    /* Angular $scope.$watch syntax restriction*/
                    /* jshint unused: false */
                    watchOnce = scope.$watch("config", function (newValue, oldValue) {
                            if (newValue !== null && !angular.isUndefined(newValue) && Object.keys(newValue).length>0) {
                                ctrl.Configuration = newValue;
                                ctrl.isWidgetsEnabled = newValue.widgets.enabled;
                                DocumentService.storeConfiguration(newValue);
                                init();
                                watchOnce();
                            }
                        },true);
            }
        };
    },
        app = angular.module("app");
    app.directive("lnReviewer", lnReviewer);
    lnReviewer.$inject = ["DocumentService", "WidgetsDefinition", "Constants"];
}());

