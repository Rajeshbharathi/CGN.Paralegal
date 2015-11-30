(function () {
    "use strict";
    var app, HighlighterDirective;

    HighlighterDirective = function () {
        return {
            restrict: "A",
            scope: {
                data: "=?lnHighlighterData",
                highlightIndex: "=?lnHighlighterIndex"
            },
            template: '<div ng-bind-html="data.content" class="text-justify v-padding-bottom-30"></div>',
            link: function (scope, element) {
                scope.$watch("highlightIndex", function (newVal, oldVal) {
                    var hits;
                    if (newVal !== oldVal && newVal) {
                        hits = element.find(".hits");
                        hits.eq(oldVal - 1).removeClass("active");
                        hits.eq(newVal - 1).addClass("active");
                    }
                });
            }
        };
    };

    app = angular.module('app');

    app.directive('lnHighlighter', HighlighterDirective);

}());