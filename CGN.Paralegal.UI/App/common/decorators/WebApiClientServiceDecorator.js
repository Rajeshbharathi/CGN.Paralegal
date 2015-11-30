/* 
 * WebApiClientDecorator decorates all the methods available in WebApiClient service 
 * to handle both synchronous and asynchronous errors. 
 * The error caught is stored by ErrorHandlerService for displaying in UI.
 * 
 */
(function () {
    "use strict";

    angular.module("app").provider("WebApiClientServiceDecorator", function () {

        // Wrap a single function [func] in another function that handles both synchronous and asynchonous errors.
        function decorate($injector, obj, func) {
            return angular.extend(function () {
                var handler = $injector.get("WebApiClientServiceDecorator");
                return handler.call(func, obj, arguments);
            }, func);
        }

        // Decorate all functions of the service [$delegate] with error handling. This function should be used as decorator
        // function in a call to $provide.decorator().
        var decorator = ["$delegate", "$injector", function ($delegate, $injector) {
            // Loop over all functions in $delegate and wrap these functions using the [decorate] functions above.
            for (var prop in $delegate) {
                if (angular.isFunction($delegate[prop])) {
                    $delegate[prop] = decorate($injector, $delegate, $delegate[prop]);
                }
            }
            return $delegate;
        }];

        // The actual service:
        return {
            // Decorate the mentioned [services] with automatic error handling. 
            decorate: function ($provide, services) {
                angular.forEach(services, function (service) {
                    $provide.decorator(service, decorator);
                });
            },

            $get: function (ErrorHandlerService) {

                var handler = {
                    call: function (func, self, args) {
                        var result;
                        try {
                            result = func.apply(self, args);
                        } catch (err) {
                            // Catch synchronous errors.
                            ErrorHandlerService.addError(err);
                        }

                        // Catch asynchronous errors.
                        var promise = result && result.$promise || result;
                        if (promise && angular.isFunction(promise.then) && angular.isFunction(promise["catch"])) {
                            // promise is a genuine promise, so we call [handler.async].
                            handler.async(func, promise);
                        }

                        return result;
                    },


                    async: function (func, promise) {
                        promise["catch"](function (err) {
                            if (err.status && err.status !== "Custom") {
                                ErrorHandlerService.addError(err);
                            }
                        });
                        return promise;
                    }
                };

                return handler;
            }
        };
    });
      
}());



