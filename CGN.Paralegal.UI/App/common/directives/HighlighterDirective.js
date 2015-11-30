(function () {
    "use strict";
    var app, HighlighterDirective;

    HighlighterDirective = function () {
        return {
            restrict: "A",
            scope: {
                data: "=?lnHighlighterData"
            },
            template: "<pre ng-bind-html='data.Content' class='text-justify normal-word-break container-fluid'></pre>",
            link: function () {
                
            }
        };
    };

    app = angular.module("app");

    app.directive("lnHighlighter", HighlighterDirective);

}());