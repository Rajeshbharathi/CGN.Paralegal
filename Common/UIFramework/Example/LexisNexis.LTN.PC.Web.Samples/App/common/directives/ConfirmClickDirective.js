
//based on https://github.com/kendo-labs/angular-kendo/issues/253
(function () {
    "use strict";
    angular.module("app")
        .directive('lnConfirmClick', function () {
            return {
                priority: -1,
                restrict: 'A',
                link: function (scope, element, attrs) {
                    element.bind('click', function (e) {
                        var message = attrs.lnConfirmClick;
                        if (message && !confirm(message)) {
                            e.stopImmediatePropagation();
                            e.preventDefault();
                        }
                    });
                }
            };
        });
}());
