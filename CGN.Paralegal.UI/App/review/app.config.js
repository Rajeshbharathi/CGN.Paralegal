(function () {
    "use strict";
    function config($provide, WebApiClientServiceDecoratorProvider) {

        // Decorate angular $exceptionHandler for any exceptions to handle using ErrorHandlerService 
        $provide.decorator("$exceptionHandler", ["$delegate", "$injector", function ($delegate, $injector) {
            return function (error, cause) {
                $delegate(error, cause);
                var ErrorHandlerService = $injector.get("ErrorHandlerService");
                if (!(error.status && error.status === "Custom")) {
                    ErrorHandlerService.addError(error);
                }
            };
        }]);

        WebApiClientServiceDecoratorProvider.decorate($provide, ["WebApiClientService"]);

    }

    angular.module("app").config(config);
    config.$inject = ["$provide", "WebApiClientServiceDecoratorProvider"];
}());
