///#source 1 1 /App/review/app.js
"use strict";

 (function () {
    angular
        .module('app', [
        'ngAnimate',
        'ngRoute',
        'ngSanitize',
        'kendo.directives',
        'ui.bootstrap',
        'angular-loading-bar'
    ]);
})();

///#source 1 1 /App/review/review.resx.js
//IMPORTANT: This is a sample js file for localization strings
// This file should be created dynamically from resx file(s).
// DO NOT USE a static file this in production!

var ReviewResources = {
    Relevance: "Relevance",
    AssistedReview: "Assisted Review",
    Fields: "Fields",
    Control: "Control #",
    Excerpts: "Excerpts",
    Excerpt: "Excerpt",
    Comments: "Comments",
    DCN: "DCN",
    FileName: "File Name",
    ParentId: "ParentId",
    AllDocs: "All Docs",
    DocId: "DocId",
    Attachmt: "Attachmt",
    Batesmsg: "Batesmsg",
    EDFolder: "EDFolder",
    Author: "Author",
    Categories: "Categories",
    ThreadId: "ThreadId",
    Subject: "Subject",
    NonRelevant: "Non-Relevant",
    Relevant: "Relevant",
    more: "more",
    Add: "Add",
    Close: "Close",
    shorttext: 80,
    Document: "Document"
};



///#source 1 1 /App/review/app.config.js
(function () {
    "use strict";
    function config($provide, WebApiClientServiceDecoratorProvider) {

        // Decorate angular $exceptionHandler for any exceptions to handle using ErrorHandlerService 
        $provide.decorator('$exceptionHandler', ['$delegate', '$injector', function ($delegate, $injector) {
            return function (error, cause) {
                $delegate(error, cause);
                var ErrorHandlerService = $injector.get("ErrorHandlerService");
                if (!(error.status && error.status === "Custom ")) {
                    ErrorHandlerService.addError(error);
                }
            };
        }]);

        WebApiClientServiceDecoratorProvider.decorate($provide, ['WebApiClientService']);

    }
    angular.module('app').config(config);
    config.$inject = ['$provide', 'WebApiClientServiceDecoratorProvider'];
}());

///#source 1 1 /App/common/services/WebApiClientService.js
/*
 * WebApiClientService provides common methods to call WebApi for global error handling purposes. 
 * Provides all available shortcuts methods of $http for users.
 * This service will be decorated for global error handling, to avoid modifying internal $http functionalities.
 * 
 * Methods available:
 * WebApiClientService.get(url,config) - returns HttpPromise [same as $http]
 * WebApiClientService.post(url,config,data) - returns HttpPromise [same as $http]
 * WebApiClientService.delete(url,config) - returns HttpPromise [same as $http]
 * WebApiClientService.put(url,config,data) - returns HttpPromise [same as $http]
 * WebApiClientService.head(url,config) - returns HttpPromise [same as $http]
 * WebApiClientService.patch(url,config,data) - returns HttpPromise [same as $http]
 * WebApiClientService.jsonp(url,config) - returns HttpPromise [same as $http]
 */

(function () {
    "use strict";
    angular.module('app').factory('WebApiClientService', ['$http', '$q', function ($http, $q) {

        var service = {};

        // Create short cut methods - get, post, put, delete, head, patch, jsonp
        function createShortMethods(names) {
            angular.forEach(arguments, function (name) {
                service[name] = function (url, config) {
                    config = config || {};
                    return $http(angular.extend(config, {
                        method: name,
                        url: url,
                        allowRequest: true
                    }));
                };
            });
        }

        function createShortMethodsWithData(name) {
            angular.forEach(arguments, function (name) {
                service[name] = function (url, data, config) {
                    config = config || {};
                    return $http(angular.extend(config, {
                        method: name,
                        url: url,
                        data: data,
                        allowRequest: true
                    }));
                };
            });
        }

        createShortMethods('get', 'delete', 'head', 'jsonp');
        createShortMethodsWithData('post', 'put', 'patch');

        return service;

    }]);
}());
///#source 1 1 /App/common/decorators/WebApiClientServiceDecorator.js
/* WebApiClientDecorator decorates all the methods available in WebApiClient service 
 * to handle both synchronous and asynchronous errors. 
 * The error caught is stored by ErrorHandlerService for displaying in UI.
 * 
 */

(function () {
    'use strict';

    angular.module('app').provider('WebApiClientServiceDecorator', function () {

        // Wrap a single function [func] in another function that handles both synchronous and asynchonous errors.
        function decorate($injector, obj, func) {
            return angular.extend(function () {
                var handler = $injector.get('WebApiClientServiceDecorator');
                return handler.call(func, obj, arguments);
            }, func);
        }

        // Decorate all functions of the service [$delegate] with error handling. This function should be used as decorator
        // function in a call to $provide.decorator().
        var decorator = ['$delegate', '$injector', function ($delegate, $injector) {
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
                        if (promise && angular.isFunction(promise.then) && angular.isFunction(promise['catch'])) {
                            // promise is a genuine promise, so we call [handler.async].
                            handler.async(func, promise);
                        }

                        return result;
                    },


                    async: function (func, promise) {
                        promise['catch'](function (err) {
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




///#source 1 1 /App/common/directives/ErrorHandlerDirective.js
/**
 * ErrorHandlerDirective displays the error messages stored in ErrorHandlerService in the UI.
 * It also allows to dismiss the error message displayed.
 */

(function () {
    "use strict";
    var ErrorHandlerDirective = function () {
        return {
            controller: function ExceptionController(ErrorHandlerService) {
                var vm = this;
                vm.ErrorHandlerService = ErrorHandlerService;

            },
            template: '<div alert ng-repeat="error in ErrorHandlerService.errors track by $index" type="danger" close="ErrorHandlerService.errors.splice($index,1)">{{error}}</div>',
            link: function (scope, elem, attrs, ctrl) {
                scope.ErrorHandlerService = ctrl.ErrorHandlerService;
            }
        }
    };
    angular
    .module('app')
    .directive('lnErrorHandler', ErrorHandlerDirective);
    
}());

///#source 1 1 /App/common/services/ErrorHandlerService.js
/*
 * ErrorHandlerService - Creates and stores formatted error message from errors[HTTP/Javascript]  to display on UI and 
 * allows removal of errors.
 */

(function () {
    'use strict';

    angular.module('app').factory('ErrorHandlerService', [function () {

          var handler = {
              errors: [],
              buildErrorMessage: function (err, func) {

                  if (err && !angular.isUndefined(err.status)) {
                      if (err.data && err.data.ExceptionType) {
                          // handle custom http error response from webapi
                          err = err.data.ExceptionType + ": " + err.data.Message + "[ " + err.data.ExceptionMessage + " ]";
                      } else if (err.data && err.data.MessageDetail) {
                          // handle standard http error response
                          err = err.statusText + ":" + err.data.Message + "[" + err.data.MessageDetail + "]";
                      }
                  } else if (err && err.Message) {
                      // handle unformatted webapi error response
                      err = err.Message;
                  } else if (err && err.message) {
                      // handle Javascript errors
                      err = err.message;
                  }

                  // if error is unknown or has no message
                  if (!angular.isString(err)) {
                      err = ProjectResources.UnknownError;
                  }

                  return err;
              },
              get: function (err, func) {
                  return handler.errors;
              },
              addError: function (err, func) {
                  handler.errors.push(handler.buildErrorMessage(err));
              },
              clearError: function (idx) {
                  handler.errors.splice(idx, 1)
              }
          };

          return handler;
      }]);
}());



///#source 1 1 /App/common/directives/InputValidationDirective.js
(function () {
    "use strict";
    /* Angular 1.3 $validator use case
     * Angular Form validations and NgMessages - Reference docs: http://www.yearofmoo.com/2014/09/taming-forms-in-angularjs-1-3.html 
     * 
     * lnInputValidationSync - takes an object with custom validation methods which return a boolean value
     * eg: syncValidations ={
     *          "GreaterThanZero": function(value){ return value>0;}
     * }
     * 
     * lnInputValidationAsync - takes an object with custom validation methods which return a promise[eg. $http calls]   
     * eg: asyncValidations ={
     *          "UserNameAvailability": function(value){ return $http.get("api/users/availability/?name='value'");},
     *          "ProjectNameAvailability": function(value){ return $http.get("api/projects/availability/?name='value'");},
     * }
     * 
     * lnInputValidationPatterns - takes an object with custom validation patterns 
     * eg: patterns = {
     *          "noWhitespace": /^\S+$/,
     *          "allowNumeric":/\d+/,
     *          "AllowLowercase":/[a-z]+/,
     *          "uppercase":/[A-Z]+/, 
     *          "special":/\W+/
     *       }
     * 
     * lnInputValidationBatch - takes an array of custom sync validation methods only
     * eg: batchValidations =[greaterThanZero,checkAllPatterns]
     * 
     * 
     **/
    angular.module("app")
        .directive('lnInputValidation', function () {
            
                
            return {
                scope:{
                    validations: "=?lnInputValidationSync",
                    asyncValidations: "=?lnInputValidationAsync",
                    patterns: "=?lnInputValidationPatterns",
                    batch: "=?lnInputValidationBatch"
                },
                require: 'ngModel',
                link: function (scope, element, attrs, ngModel) {
                    
                    if (attrs.lnInputValidationBatch) {
                        ngModel.$validators.lnBatchValidations = function (value) {
                            var status = true;
                            angular.forEach(scope.batch, function (pattern) {
                                status = status && pattern.test(value);
                            });
                            return status;
                        };
                    }
                    if (attrs.lnInputValidationPatterns) {
                        angular.forEach(scope.patterns, function (pattern, name) {
                            ngModel.$validators[name] = function (modelValue, viewValue) {
                                var value = modelValue || viewValue;
                                return pattern.test(value);
                            }
                        });
                    }

                    if (attrs.lnInputValidationSync) {
                        angular.forEach(scope.validations, function (fn, name) {
                            ngModel.$validators[name] = function (modelValue, viewValue) {
                                var value = modelValue || viewValue;
                                return fn(value);
                            }
                        });
                    }

                    if (attrs.lnInputValidationAsync) {
                        angular.forEach(scope.asyncValidations, function (fn, name) {
                            ngModel.$asyncValidators[name] = function (modelValue, viewValue) {
                                var value = modelValue || viewValue;
                                return fn(value);
                            }
                        });
                    }
                }
            }
        });
}());
///#source 1 1 /App/review/app.routes.js
(function () {
    "use strict";
    var config = function ($routeProvider) {
        $routeProvider.
            when('/', {
                controller: 'ReviewController',
                templateUrl: '/app/review/reviewer/ReviewView.html'
            });
    };
    angular.module('app').config(config);
    config.$inject = ['$routeProvider'];

}());
///#source 1 1 /App/review/app.init.js
(function () {
    "use strict";

    function run() {
    }
    angular.module('app').run(run);
    run.$inject = [];

}());

///#source 1 1 /App/review/reviewer/ReviewController.js
(function () {
    "use strict";
    var ReviewController, app;

    ReviewController = function () {
        var vm;
        vm = this;
        vm.reviewerConfig = {};
    };

    app = angular.module('app');

    app.controller('ReviewController', ReviewController);
    ReviewController.$inject = [];

}());

///#source 1 1 /App/common/directives/HighlighterDirective.js
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
///#source 1 1 /App/common/directives/NavigatorDirective.js
(function () {
    "use strict";
    var app, NavigatorDirective;
    NavigatorDirective = function () {
        return {
            restrict: "EA",
            scope: {
                totalItems: "=lnNavigatorTotal",
                label: "@lnNavigatorLabel",
                update: "=lnNavigatorUpdate",
                reset: "=?lnNavigatorReset",
                isAdvanced: "=?lnNavigatorAdvanced"
            },
            templateUrl: "/App/common/directives/navigatorDirectiveView.html",
            link: function (scope, element) {
                var width, goTo, validate;
                if (scope.isAdvanced) {
                    width = 280;
                } else {
                    width = 155;
                }
                scope.$watch("totalItems", function (val, old) {
                    if (val !== old) {
                        var num, parentWidth, newWidth;
                        num = window.isNaN(parseInt(val, 10)) ? 2 : val.toString().length;
                        if (num < 1) {
                            num = 1;
                        }
                        parentWidth = element.parent().width();
                        newWidth = width + 6 * num;
                        newWidth = newWidth < parentWidth ? newWidth : parentWidth;
                        element.css("width", newWidth + "px");
                    }
                });

                scope.currentRecordNumber = 1;
                validate = function (num) {
                    if (num < 1 || num > scope.totalItems) {
                        var error = new Error();
                        error.name = "Validation";
                        error.message = "Please provide a value between 0 and " + scope.totalItems;
                        throw error;
                    }
                };
                goTo = function (num) {
                    validate(num);
                    if (!(num > scope.totalItems || num < 0)) {
                        scope.currentRecordNumber = num;
                        scope.update(num);
                    }
                };
                if (scope.reset) {
                    scope.$watch("reset.value", function (newVal) {
                        if (newVal) {
                            scope.currentRecordNumber = 1;
                            scope.reset.value = false;
                        }
                    });
                }
                scope.next = function () {
                    if (scope.currentRecordNumber < scope.totalItems) {
                        goTo(scope.currentRecordNumber + 1);
                    }
                };
                scope.last = function () {
                    if (scope.currentRecordNumber < scope.totalItems) {
                        goTo(scope.totalItems);
                    }
                };
                scope.prev = function () {
                    if (scope.currentRecordNumber <= scope.totalItems && scope.currentRecordNumber > 1) {
                        goTo(scope.currentRecordNumber - 1);
                    }
                };
                scope.first = function () {
                    if (scope.currentRecordNumber <= scope.totalItems && scope.currentRecordNumber > 1) {
                        goTo(1);
                    }
                };
                scope.disableNext = function () {
                    return scope.currentRecordNumber === scope.totalItems;
                };
                scope.disablePrev = function () {
                    return scope.currentRecordNumber === 1;
                };
                scope.submitForm = function ($event) {
                    var whichKey;
                    whichKey = $event.keyCode;
                    if (whichKey === 13) {
                        if (!(scope.currentRecordNumber > scope.totalItems || isNaN(scope.currentRecordNumber))) {
                            scope.update(scope.currentRecordNumber);
                        }
                    }
                };
            }
        };
    };
    app = angular.module("app");
    app.directive("lnNavigator", NavigatorDirective);
}());
///#source 1 1 /App/common/component/reviewer/DocumentSetModelFactory.js
(function () {
    "use strict";
    var DocumentSetModelFactory;
    DocumentSetModelFactory = function (WebApiClientService) {
        var documentSetModelFactory, serviceName;
        serviceName = "/api/reviewset";
        documentSetModelFactory = {
            request: function (id) {
                var url;
                url = id ? serviceName + "/:" + id : serviceName;
                return WebApiClientService.get(url);
            },
            addComments: function (docId, comment) {
                return WebApiClientService.put(serviceName + "/:" + docId, comment);
            },
            deleteComment: function (docId, commentId) {
                return WebApiClientService['delete'](serviceName + "/:" + docId(+"/" + commentId));
            },
            addExcerpts: function (docId, excerpt) {
                return WebApiClientService.put(serviceName + "/:" + docId, excerpt);
            },
            deleteExcerpts: function (docId, excerptId) {
                return WebApiClientService['delete'](serviceName + "/:" + docId, excerptId);
            }
        };
        return documentSetModelFactory;
    };
    angular.module("app")
        .factory('DocumentSetModelFactory', DocumentSetModelFactory);
    DocumentSetModelFactory.$inject = ['WebApiClientService'];
}());
///#source 1 1 /App/common/component/reviewer/ReviewerConfiguration.js
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

///#source 1 1 /App/common/component/reviewer/ReviewerWidgetsDefinition.js
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
///#source 1 1 /App/common/component/reviewer/ReviewerDataService.js
(function () {
    'use strict';
    var ReviewerDataService = function (ReviewerConfiguration, $injector, $q) {
        var document, getComments, getDocumentSet, getExcerpts, modelFactory, setComments, setExcerpts;
        modelFactory = $injector.get(ReviewerConfiguration.service);
        document = {
            comments: [],
            excerpts: []
        };
        getDocumentSet = function (id) {
            var deferred;
            deferred = $q.defer();
            modelFactory.request(id)
                .success(function (response) {
                    return deferred.resolve(response);
                })
                .error(function (error) {
                    return deferred.reject(error);
                });
            return deferred.promise;
        };
        setComments = function (arr) {
            document.comments = arr;
        };
        setExcerpts = function (arr) {
            document.excerpts = arr;
        };
        getComments = function () {
            return document.comments;
        };
        getExcerpts = function () {
            return document.excerpts;
        };
        return {
            getDocumentSet: getDocumentSet,
            getComments: getComments,
            getExcerpts: getExcerpts,
            setComments: setComments,
            setExcerpts: setExcerpts
        };
    },
        app = angular.module('app');
    app.service('ReviewerDataService', ReviewerDataService);
    ReviewerDataService.$inject = ['ReviewerConfiguration', '$injector', '$q'];
}());

///#source 1 1 /App/common/component/reviewer/ReviewerDirectiveController.js
(function () {
    'use strict';
    var ReviewerDirectiveController = function (ReviewerDataService, ReviewerConfiguration,
            WidgetsDefinition, ReviewResources) {
            var getCurrentDocument, item, layoutModes, vm, tabsOnLeftConfig,
                tabsOnRightConfig, resizeSplitter, i, len, ref;
            vm = this;
            vm.localized = ReviewResources;
            vm.currentDocument = {};
            vm.currentDocumentIndex = 1;
            vm.highlightIndex = 1;
            vm.resetNavigator = {
                value: false
            };
            tabsOnLeftConfig = [{
                collapsible: true,
                resizable: true,
                size: "30%",
                min: "20%",
                scrollable: false
            }, {
                collapsible: false,
                resizable: true,
                size: "70%",
                min: "30%",
                scrollable: false
            }];
            tabsOnRightConfig = [{
                collapsible: false,
                resizable: true,
                size: "70%",
                min: "30%",
                scrollable: false
            }, {
                collapsible: true,
                resizable: true,
                size: "30%",
                min: "20%",
                scrollable: false
            }];

            resizeSplitter = function (obj) {
                var delay = window.setTimeout;
                delay(function () {
                    var widget = obj.element,
                        newHeight = widget.find('.document-viewer')
                            .innerHeight(),
                        offset = 150,
                        viewportHeight = $('html', window.document)
                            .eq(0)
                            .height(),
                        calculatedHeight = viewportHeight - offset;
                    if (newHeight > calculatedHeight) {
                        calculatedHeight = newHeight + offset;
                    }
                    widget.height(calculatedHeight);
                    widget.find(".k-pane")
                        .height(calculatedHeight);
                    widget.find(".k-splitbar")
                        .height(calculatedHeight);
                }, 100);
            };
            vm.splitterTabLeftOptions = {
                orientation: "horizontal",
                panes: tabsOnLeftConfig,
                resize: function (e) {
                    if (e.sender) {
                        resizeSplitter(e.sender);
                    }
                },
                rebind: function (e) {
                    if (e.sender) {
                        e.sender.resize();
                    }
                }
            };
            vm.splitterTabRightOptions = {
                orientation: "horizontal",
                panes: tabsOnRightConfig,
                resize: function (e) {
                    if (e.sender) {
                        resizeSplitter(e.sender);
                    }
                },
                rebind: function (e) {
                    if (e.sender) {
                        e.sender.resize();
                    }
                }
            };
            vm.zoomOptions = [{
                key: "font-50",
                value: "50"
            }, {
                key: "font-75",
                value: "75"
            }, {
                key: "font-100",
                value: "100"
            }, {
                key: "font-125",
                value: "125"
            }, {
                key: "font-150",
                value: "150"
            }, {
                key: "font-175",
                value: "175"
            }, {
                key: "font-200",
                value: "200"
            }];
            vm.zoomed = "font-100";
            vm.resetZoom = function () {
                vm.zoomed = 'font-100';
            };
            vm.isZoomedIn = function () {
                return parseInt(vm.zoomed.substr(5), 10) > 100;
            };
            vm.isZoomedDefault = function () {
                return vm.zoomed === 'font-100';
            };
            layoutModes = ReviewerConfiguration.layout;
            vm.selectedLayoutMode = layoutModes.MODE_RIGHT;
            vm.swapLayoutMode = function (str) {
                vm.selectedLayoutMode = layoutModes[str];
            };
            vm.isLayoutModeLeft = function () {
                return vm.selectedLayoutMode === layoutModes.MODE_LEFT;
            };
            vm.isLayoutModeRight = function () {
                return vm.selectedLayoutMode === layoutModes.MODE_RIGHT;
            };
            vm.isWidgetsEnabled = ReviewerConfiguration.widgets.enabled;
            if (vm.isWidgetsEnabled) {
                vm.widgets = [];
                ref = ReviewerConfiguration.widgets.list;
                for (i = 0, len = ref.length; i < len; i = i + 1) {
                    item = ref[i];
                    vm.widgets.push(WidgetsDefinition[item]);
                }
            }
            getCurrentDocument = function () {
                ReviewerDataService.getDocumentSet()
                    .then(function (response) {
                        var indx;
                        indx = vm.currentDocumentIndex - 1;
                        vm.totalDocuments = response[indx].totaldocs;
                        vm.currentDocument = response[indx];
                        vm.totalHits = response[indx].totalhits;
                        ReviewerDataService.setComments(response[indx].comments);
                        return ReviewerDataService.setExcerpts(response[indx].excerpts);
                    });
            };
            getCurrentDocument();
            vm.updateDocument = function (idx) {
                vm.currentDocumentIndex = idx;
                getCurrentDocument(idx);
                vm.resetHitNavigator.value = true;
                vm.highlightIndex = 1;
            };
            vm.updateHit = function (idx) {
                vm.highlightIndex = idx;
            };
            vm.resetHitNavigator = {
                value: true
            };
        },
        app = angular.module('app');
    app.controller('ReviewerDirectiveController', ReviewerDirectiveController);
    ReviewerDirectiveController.$inject = ['ReviewerDataService', 'ReviewerConfiguration',
        'WidgetsDefinition', 'ReviewResources'];
}());

///#source 1 1 /App/common/component/reviewer/ReviewerDirective.js
(function () {
    'use strict';
    var ReviewerDirective = function () {
        return {
            restrict: 'EA',
            priority: 1000,
            terminal: true,
            controller: 'ReviewerDirectiveController',
            controllerAs: "ReviewerDirectiveController",
            templateUrl: "/App/common/component/reviewer/ReviewerDirectiveView.html"
        };
    },
        app = angular.module('app');
    app.directive('lnReviewer', ReviewerDirective);
}());


///#source 1 1 /App/common/component/widgets/AssistedReviewController.js
(function () {
    "use strict";
    var AssistedReviewController = function ($scope, ReviewerDataService,
        ReviewerConfiguration, ReviewResources) {
        var getSelectedText, relevance, vm;
        vm = this;
        vm.localized = ReviewResources;
        vm.excerptStatus = {
            open: false
        };
        vm.commentStatus = {
            open: false
        };
        relevance = ReviewerConfiguration.relevance;
        vm.selectedRelevance = relevance.NONE;
        vm.isRelevant = function () {
            return vm.selectedRelevance === relevance.RELEVANT;
        };
        vm.isNonRelevant = function () {
            return vm.selectedRelevance === relevance.NONRELEVANT;
        };
        vm.isNone = function () {
            return vm.selectedRelevance === relevance.NONE;
        };
        vm.isExampleDocument = false;
        vm.makeDocumentExample = function () {
            vm.isExampleDocument = true;
            return false;
        };
        vm.makeDocumentNotExample = function () {
            vm.isExampleDocument = false;
            return false;
        };
        vm.excerpts = ReviewerDataService.getExcerpts();
        $scope.$watch(function () {
            return ReviewerDataService.getExcerpts();
        }, function (val, old) {
            if ((val !== null) && val !== old) {
                vm.excerpts = val;
            }
        });
        getSelectedText = function () {
            var text;
            text = "";
            if (window.getSelection !== "undefined") {
                text = window.getSelection()
                    .toString();
            } else if ((document.selection !== null) && document.selection.type === "Text") {
                text = document.selection.createRange()
                    .text;
            }
            return text;
        };
        vm.addExcerpts = function () {
            var excerpt;
            excerpt = {};
            excerpt.text = getSelectedText();
            excerpt.type = vm.selectedRelevance;
            if (excerpt.type === "" || excerpt.type === null) {
                // TODO : Placeholder for handling exceptions
                var error = new Error();
                error.name = "Validation";
                error.message = "Please select Excerpt type";
                throw error;
            }
            if (excerpt.text === "") {
                // TODO : Placeholder for handling exceptions
                var error = new Error();
                error.name = "Validation";
                error.message = "Please select Excerpt text";
                throw error;
            }

            vm.excerpts.unshift(excerpt);
            ReviewerDataService.setExcerpts(vm.excerpts);
            vm.excerptStatus.open = true;
            return false;
        };
        vm.removeExcerpts = function (index) {
            vm.excerpts.splice(index, 1);
            ReviewerDataService.setExcerpts(vm.excerpts);
        };
        vm.comments = ReviewerDataService.getComments();
        $scope.$watch(function () {
            return ReviewerDataService.getComments();
        }, function (val, old) {
            if ((val !== null) && val !== old) {
                vm.comments = val;
            }
        });
        vm.commentItem = {
            text: ''
        };
        vm.addComments = function (commentItem) {
            var comment;
            comment = {
                userId: "johndox",
                text: commentItem.text
            };
            if (comment.text === null) {
                // TODO : Placeholder for handling exceptions 
                var error = new Error();
                error.name = "Validation";
                error.message = "Please Provide Comment";
                throw error;
            }
            vm.comments.unshift(comment);
            ReviewerDataService.setComments(vm.comments);
            vm.commentItem.text = '';
        };
        vm.removeComments = function (index) {
            vm.comments.splice(index, 1);
            ReviewerDataService.setComments(vm.comments);
        };
    },
        app = angular.module('app');
    app.controller('AssistedReviewController', AssistedReviewController);
    AssistedReviewController.$inject = ['$scope', 'ReviewerDataService',
        'ReviewerConfiguration', 'ReviewResources'];
}());

///#source 1 1 /App/common/directives/ngTruncate.js
(function () {
    'use strict';
    angular.module('app')
        .directive("ngTextTruncate", function (ValidationServices, CharBasedTruncation,
            WordBasedTruncation) {
            return {
                restrict: "A",
                scope: {
                    text: "=ngTextTruncate",
                    charsThreshould: "@ngTtCharsThreshold",
                    wordsThreshould: "@ngTtWordsThreshold",
                    customMoreLabel: "@ngTtMoreLabel",
                    customLessLabel: "@ngTtLessLabel"
                },
                controller: function ($scope, $element, $attrs) {
                    $scope.toggleShow = function () {
                        $scope.open = !$scope.open;
                    };
                    $scope.useToggling = $attrs.ngTtNoToggling === undefined;
                },
                link: function ($scope, $element) {
                    $scope.open = false;
                    ValidationServices.failIfWrongThreshouldConfig($scope.charsThreshould, $scope.wordsThreshould);
                    var CHARS_THRESHOLD = parseInt($scope.charsThreshould, 10),
                        WORDS_THRESHOLD = parseInt($scope.wordsThreshould, 10);
                    $scope.$watch("text", function () {
                        $element.empty();
                        if (CHARS_THRESHOLD) {
                            if ($scope.text && CharBasedTruncation.truncationApplies($scope,
                                    CHARS_THRESHOLD)) {
                                CharBasedTruncation.applyTruncation(CHARS_THRESHOLD, $scope,
                                    $element);
                            } else {
                                $element.append($scope.text);
                            }
                        } else {
                            if ($scope.text && WordBasedTruncation.truncationApplies($scope,
                                    WORDS_THRESHOLD)) {
                                WordBasedTruncation.applyTruncation(WORDS_THRESHOLD, $scope,
                                    $element);
                            } else {
                                $element.append($scope.text);
                            }
                        }
                    });
                }
            };
        })
        .factory("ValidationServices", function () {
            return {
                failIfWrongThreshouldConfig: function (firstThreshould, secondThreshould) {
                    if ((!firstThreshould && !secondThreshould) || (firstThreshould && secondThreshould)) {
                        throw "You must specify one, and only one, type of threshould (chars or words)";
                    }
                }
            };
        })
        .factory("CharBasedTruncation", function ($compile) {
            return {
                truncationApplies: function ($scope, threshould) {
                    return $scope.text.length > threshould;
                },
                applyTruncation: function (threshould, $scope, $element) {
                    if ($scope.useToggling) {
                        var el = angular.element("<span>" + $scope.text.substr(0, threshould) +
                            "<span ng-show='!open'>...</span>" +
                            "<span class='btn-link ngTruncateToggleText' " + "ng-click='toggleShow()'" +
                            "ng-show='!open'>" + " " +
                            ($scope.customMoreLabel || "More") +
                            "</span>" + "<span ng-show='open'>" +
                            $scope.text.substring(threshould) +
                            "<span class='btn-link ngTruncateToggleText'" +
                            "ng-click='toggleShow()'>" + " " +
                            ($scope.customLessLabel || "Less") +
                            "</span>" + "</span>" + "</span>");
                        $compile(el)($scope);
                        $element.append(el);
                    } else {
                        $element.append($scope.text.substr(0, threshould) + "...");
                    }
                }
            };
        })
        .factory("WordBasedTruncation", function ($compile) {
            return {
                truncationApplies: function ($scope, threshould) {
                    return $scope.text.split(" ")
                        .length > threshould;
                },
                applyTruncation: function (threshould, $scope, $element) {
                    var splitText = $scope.text.split(" "), el;
                    if ($scope.useToggling) {
                        el = angular.element("<span>" + splitText.slice(0, threshould)
                            .join(" ") + " " + "<span ng-show='!open'>...</span>" +
                            "<span class='btn-link ngTruncateToggleText' " + "ng-click='toggleShow()'" +
                            "ng-show='!open'>" + " " +
                            ($scope.customMoreLabel || "More") +
                            "</span>" + "<span ng-show='open'>" +
                            splitText.slice(threshould, splitText.length).join(" ") +
                            "<span class='btn-link ngTruncateToggleText'" +
                            "ng-click='toggleShow()'>" + " " +
                            ($scope.customLessLabel || "Less") +
                            "</span>" + "</span>" + "</span>");
                        $compile(el)($scope);
                        $element.append(el);
                    } else {
                        $element.append(splitText.slice(0, threshould)
                            .join(" ") + "...");
                    }
                }
            };
        });
}());

