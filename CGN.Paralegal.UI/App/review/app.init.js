(function () {
    "use strict";

    function run($rootScope, $timeout, $window) {
        var ev = window.top.document,
            iframe = angular.element(ev).find("iframe").get(0),
            adminTreeParent = angular.element(ev).find("#organisationTree").get(0),
            innerDoc = !!iframe ? (iframe.contentDocument || iframe.contentWindow.document) : null,
            evHeight = angular.element(ev).height() - 75;

        function resizeIframe() {
            var pcHeight = angular.element(innerDoc).height();

            if (pcHeight > 5000) {
                return;
            }

            iframe.style.height = (pcHeight > evHeight) ? pcHeight + "px" : evHeight + "px";
            if(adminTreeParent){
                var adminTree = $(adminTreeParent).find(".RadTreeView.RadTreeView_Default").get(0),
                    adminSplitter = $(ev).find(".admin-splitter").get(0);
                adminTreeParent.style.height = (pcHeight > evHeight) ? (pcHeight+200) + "px" : evHeight + "px";
                adminTree.style.height = (pcHeight > evHeight) ? (pcHeight+170) + "px" : evHeight + "px";
                adminSplitter.style.height = (pcHeight > evHeight) ? pcHeight + "px" : evHeight + "px";
            }
            kendo.resize($(".k-chart"));
        }

        function debounce(callback, interval) {
            var timeout = null;
            return function () {
                $timeout.cancel(timeout);
                timeout = $timeout(callback, interval);
            };
        }
        if (!!iframe) {
            $rootScope.$on("$viewContentLoaded", function () {
                $rootScope.$watch(function () {
                    return $(document).height();
                }, debounce(resizeIframe, 100));
            });
            $window.onresize = function () {
                resizeIframe();
            };
        }else{
            $window.onresize = function () {
                kendo.resize($(".k-chart"));
            };
        }
        
    }
    angular.module("app").run(run);
    run.$inject = ["$rootScope", "$timeout", "$window"];

}());

