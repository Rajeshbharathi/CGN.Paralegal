///#source 1 1 /App/analytics/app.js
 (function () {
     "use strict";
     angular
        .module('app', [
        'ngAnimate',
        'ngRoute',
        'ngMessages',
        'ngSanitize',
        'kendo.directives',
        'ui.bootstrap',
        'angular-loading-bar'
    ]);
 }());

///#source 1 1 /App/analytics/app.config.js

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

///#source 1 1 /App/analytics/app.routes.js
(function () {
    "use strict";

    function config($routeProvider) {
        $routeProvider.
            when('/', { controller: 'ProjectListController', controllerAs: 'ProjectListController', templateUrl: '/App/analytics/projects/projectList/ProjectListView.html' }).
            when('/list', { controller: 'ProjectListController', controllerAs: 'ProjectListController', templateUrl: '/App/analytics/projects/projectList/ProjectListView.html' }).
            when('/dashboard', { controller: 'ProjectDashboardController', controllerAs: 'ProjectDashboardController', templateUrl: '/App/analytics/projects/dashboard/ProjectDashboardView.html' }).
            when('/wizard/:id', { controller: 'ProjectWizardController', controllerAs: 'ProjectWizardController', templateUrl: '/App/analytics/projects/projectWizard/ProjectWizardView.html' }).
            when('/wizard', { controller: 'ProjectWizardController', controllerAs: 'ProjectWizardController', templateUrl: '/App/analytics/projects/projectWizard/ProjectWizardView.html' }).
            otherwise({redirectTo:"/"});
            
    }

    angular.module('app').config(config);
    config.$inject = ['$routeProvider'];

}());

///#source 1 1 /App/analytics/app.init.js
(function () {
    "use strict";

    function run() {

    }
    angular.module('app').run(run);
    run.$inject = [];

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
///#source 1 1 /App/analytics/projects/Projects.resx.js
//IMPORTANT: This is a sample js file for localization strings
// This file should be created dynamically from resx file(s).
// DO NOT USE a static file this in production!
var WizardResources = {
    ProjectSettings: 'Project Settings',
    Name: 'Name',
    Description: 'Description',
    DocSourceOptions: 'Document Source Options',
    Next: 'Next',
    NameRequired: 'Name is required',
    NameInvalid: 'Name is invalid',
    ValidationErrors: 'Please correct the errors below:',
    AllDocs: 'All Docs',
    Tag: 'Tag',
    SavedSearch: 'Saved Search',
    IdentifyRepeatedContent: 'Identify Repeated Content'
};

var ProjectResources = {
    ProjectName: 'Project',
    MatterName: 'Matter',
    Source: 'Source',
    CreatedBy: 'Created By',
    Documents: 'Documents',
    CreatedOn: 'Created On',
    Actions: 'Actions',
    FilterByMatter: 'Filter By Matter',
    FilterByDataset: 'Filter By Dataset',
    NewProject: 'New Project',
    Refresh: 'Refresh',
    Clear: 'Clear',
    Pagetitle: 'Assisted Review',
    System: 'System',
    Analytics: 'Analytics',
    Edit: 'Edit',
    Delete: 'Delete',
    ErrorOnFetch: 'Error on retrieving projects ',
    ErrorOnDelete: 'Error on deleting project '
};

var CreateControlSetResources = {
    WarningMessage: 'Warning: This job will delete your existing control set and progress statistics. This action cannot be undone.',
    Confidence: 'Confidence',
    MarginError: 'Margin of Error',
    ConfidenceValidationMessage: 'Select Confidence',
    MarginErrorValidationMessage: 'Select Margin of Error',
    SampleSize: 'Sample Size',
    Cancel: 'Cancel',
    OK: 'OK',
    Calculate: 'Calculate',
    SelectDefaultOption: '-- Select One --',
    ModalTitle: 'New Control Set',
    confidences_95: '95',
    confidences_90: '90',
    confidences_85: '85',
    errorMargins_5: '5.0',
    errorMargins_2p5: '2.5',
    errorMargins_2: '2.0',
    errorMargins_1p5: '1.5',
    errorMargins_1: '1.0',
    errorMargins_p5: '0.5',
    percentage: '%',
    ErrorOnSampleSize: 'Error on retrieving Sample Size:',
    ErrorOnAddControlSet: 'Error on add ControlSet:'
};


var CreateQCSetResources = {
    Confidence: 'Confidence',
    MarginError: 'Margin of Error',
    ConfidenceValidationMessage: 'Select Confidence',
    MarginErrorValidationMessage: 'Select Margin of Error',
    RelevantTypeValidationMessage: 'Select Relevance type',
    SamplingTypeValidationMessage: 'Select Sampling type',
    SampleSize: 'Sample Size',
    Statistical: 'Statistical',
    Percentage: 'Percentage',
    NotRelevant: 'Not Relevant',
    Relevant: 'Relevant',
    Sampling: 'Sampling',
    Both: 'Both',
    Cancel: 'Cancel',
    OK: 'OK',
    Calculate: 'Calculate',
    ModalTitle: 'New QC Set',
    confidences_95: '95',
    confidences_90: '90',
    confidences_85: '85',
    errorMargins_5: '5.0',
    errorMargins_2p5: '2.5',
    errorMargins_2: '2.0',
    errorMargins_1p5: '1.5',
    errorMargins_1: '1.0',
    errorMargins_p5: '0.5',
    percentage: '%',
    ErrorOnSampleSize: 'Error on retrieving Sample Size:',
    ErrorOnAddControlSet: 'Error on add ControlSet:'
};

var CreatePredictionSetResources = {
    ModalTitle: 'New Prediction Set',
    Cancel: 'Cancel',
    OK: 'OK',
    Message: 'Prediction Set job has been scheduled'
};

var RunAccuracyTestResources = {
    ModalTitle: 'Run Accuracy Test',
    Cancel: 'Cancel',
    OK: 'OK',
    Message: 'Accuracy Test job has been scheduled'
};

///#source 1 1 /App/analytics/projects/projectWizard/ProjectWizard.resx.js
//IMPORTANT: This is a sample js file for localization strings
// This file should be created dynamically from resx file(s).
// DO NOT USE a static file this in production!
var ProjectWizardResources = {
    ProjectSettings: 'Project Settings',
    Name: 'Name',
    Description: 'Description',
    DocSourceOptions: 'Document Source Options',
    Prev: 'Prev',
    Next: 'Next',
    Finish: 'Finish',
    NameRequired: 'Name is required',
    NameInvalid: 'Name is invalid (should have atleast one alphabet[a-z][A-Z] or number[0-9] between 2-200 characters)',
    DescriptionInvalid: 'Description is invalid',
    SampleCustomValidatorMessage: 'Description cannot be 1',
    ValidationErrors: 'Please correct the errors below:',
    NoWhiteSpaceValidation:"No whitespace",
    NumericValidation :"Atleast one numeric character",
    LowercaseValidation:"Atleast one lowercase character",
    UppercaseValidation: "Atleast one uppercase character",
    SpecialValidation: "Atleast one special character",
    AllDocs: 'All Docs',
    Tag: 'Tag',
    SavedSearch: 'Saved Search',
    IdentifyRepeatedContent: 'Identify Repeated Content',
    Confidence: 'Confidence',
    MarginOfError: 'Margin of Error',
    StratifyByCustodian: 'Stratify by Custodian',
    CustodianField: 'Custodian Field',
    SampleSize: 'Sample Size',
    Calculate: 'Calculate',
    ExampleSetOptions: 'Example Set Options',
    Recommended: 'Recommended',
    PreCoded: 'Pre-Coded',
    LimitExamples: 'Limit # of Examples',
    NumExamples: '# of Examples',
    SampleCustomValidator: 'Margin of Error must be greater than 2%',
    Summary:"Summary",
    ProjectName:"Project Name",
    Source:"Source",
    TotalDocuments:"Total Documents",
    DocumentTextField : "Document Text Field",
    DocumentText : "Document Text",
    CrossReferenceField:"Cross Reference Field",
    DCN : "DCN",
    CustodianField : "Custodian Field",
    ControlSet : "Control Set",
    PercentConfidence : "% Confidence",
    PercentMarginOfError : "% Margin of Error",
    TrainingSet : "Training Set"
};
///#source 1 1 /App/analytics/training/documentSets/DocumentSetsList.resx.js
//IMPORTANT: This is a sample js file for localization strings
// This file should be created dynamically from resx file(s).
// DO NOT USE a static file this in production!
var DocumentSetsListResources = {
    DocumentSets: 'Document Sets',
    Clear: 'Clear',
    Refresh: 'Refresh',
    Type: 'Type',
    Status: 'Status'
};
///#source 1 1 /App/analytics/projects/dashboard/ProjectDashboard.resx.js
//IMPORTANT: This is a sample js file for localization strings
// This file should be created dynamically from resx file(s).
// DO NOT USE a static file this in production!
var ProjectDashboardResources = {
    Control: 'Control',
    Train: 'Train',
    NewTrainingSet: 'New Training Set',
    TuneExamples: 'Tune Examples',
    AccuracyTest: 'Accuracy Test',
    Predict: 'Predict',
    QC: 'QC',
    Reports: 'Reports',
    Accuracy: 'Accuracy',
    Overturn: 'Overturn',
    Summary: 'Summary',
    Settings: 'Settings',
    Option1: 'Option 1',
    Option2: 'Option 2',
    Documents: 'Documents',
    Status: 'Status',
    Details: 'Details',
    SourceDataset: 'Source Dataset',
    TotalDocuments: 'Total Documents',
    CrossReferenceField: 'Cross Reference Field',
    ControlSet: 'Control Set',
    DatabaseServer: 'Database Server',
    AnalyticsServer: 'Analytics Server'
};
///#source 1 1 /App/analytics/training/trainingProgress/TrainingProgress.resx.js
//IMPORTANT: This is a sample js file for localization strings
// This file should be created dynamically from resx file(s).
// DO NOT USE a static file this in production!
var TrainingProgressResources = {
    TrainingProgress: 'TrainingProgress',
    ViewReport: 'View Report',
    Refresh: 'Refresh'
};
///#source 1 1 /App/analytics/projects/models/ProjectModel.js
(function () {
    "use strict";
    angular.module('app').factory('ProjectModel', function () {

        var ProjectModel = function () {
            this.id = 0;
            this.name = 'New Project';
            this.description = '';
            this.docSource = 'All';
            this.tag = '';
            this.identifyRepeatedContent = false;
            this.confidence = 0;
            this.marginOfError = 0;
            this.stratifyByCustodian = false;
            this.custodianField = null;
            this.sampleSize = null;
            this.limitExamples = false;
            this.numExamples = 0;
        };

        ProjectModel.prototype = {
            doSomething: function () {
                return;
            }
        };

        return ProjectModel;

    });


}());
///#source 1 1 /App/analytics/training/services/AppState.js
(function () {
    "use strict";

    function AppState($rootScope, WebApiClientService, $q) {

        var isLoaded = false,
            globalState = {
                currentMatter: null,
                username: null
            },
            service;

        function ensureLoaded() {
            var promise, deferred;
            if (isLoaded) {
                deferred = $q.defer();
                deferred.resolve(true);
                return deferred.promise;
            }
            promise = WebApiClientService.get('/api/appState').then(function (result) {
                globalState.currentMatter = result.data.currentMatter;
                globalState.username = result.data.username;
                isLoaded = true;
                return true;
            });
            return promise;
        }

        function setState(key, value) {
            if (!globalState.hasOwnProperty(key)) {
                throw "Key " + key + " does not exist in app state";
            }
            globalState[key] = value;
        }

        function getState(key) {
            if (!globalState.hasOwnProperty(key)) {
                throw "Key " + key + " does not exist in app state";
            }
            return globalState[key];
        }
        service = {
            ensureLoaded: ensureLoaded,
            setState: setState,
            getState: getState
        };
        return service;

    }

    angular.module('app').factory('AppState', AppState);
    AppState.$inject = ['$rootScope', 'WebApiClientService', '$q'];

}());
///#source 1 1 /App/common/services/AlertService.js
(function () {
    "use strict";

    function AlertService($window) {
        function showAlert(msg, err) {
            $window.alert(msg);
        }
        var service = {
            showAlert: showAlert
        };
        return service;

    }
    angular.module('app')
        .factory('AlertService', AlertService);
    AlertService.$inject = ['$window'];
}());

///#source 1 1 /App/common/services/MattersDataService.js
(function () {
    "use strict";
    function MattersDataService(WebApiClientService) {
        function getMatters() {
            return WebApiClientService.get('/api/matters').then(function (result) {
                return result.data;
            });
        }
        var service = {
            getMatters: getMatters
        };
        return service;

    }

    angular.module('app').factory('MattersDataService', MattersDataService);
    MattersDataService.$inject = ['WebApiClientService'];
}());
///#source 1 1 /App/analytics/training/services/DocumentSetsDataService.js
(function () {
    "use strict";
    function DocumentSetsDataService(WebApiClientService, $q) {
        function getDocumentSets(odataOptions) {
            return WebApiClientService.get('/odata/DocumentSets', {
                params: odataOptions
            });
        }
        var service = {
            getDocumentSets: getDocumentSets
        };
        return service;
    }
    angular.module('app').factory('DocumentSetsDataService', DocumentSetsDataService);
    DocumentSetsDataService.$inject = ['WebApiClientService', '$q'];

}());
///#source 1 1 /App/common/services/DatasetsDataService.js
(function () {
    "use strict";

    function DataSetsDataService(WebApiClientService) {
        function getDataSets() {
            return WebApiClientService.get('/api/datasets')
                .then(function (result) {
                    return result.data;
                });
        }

        function getDataSetsForMatterId(matterId) {
            return WebApiClientService.get('/api/datasets/matterid/' + matterId)
                .then(function (result) {
                    return result.data;
                });
        }
        var service = {
            getDataSets: getDataSets,
            getDataSetsForMatterId: getDataSetsForMatterId
        };
        return service;
    }
    angular.module('app')
        .factory('DataSetsDataService', DataSetsDataService);
    DataSetsDataService.$inject = ['WebApiClientService'];
}());

///#source 1 1 /App/analytics/projects/services/ProjectsDataService.js
(function () {
    "use strict";
    angular.module('app').factory('ProjectsDataService', ProjectsDataService);
    ProjectsDataService.$inject = ['$http', '$q', 'ProjectModel', 'WebApiClientService'];
    function ProjectsDataService($http, $q, ProjectModel, WebApiClientService) {

        var service = {
            getProjectsOData: getProjectsOData,
            getProject: getProject,
            getProjectSummary: getProjectSummary,
            deleteProject: deleteProject,
            addProject: addProject,
            updateProject: updateProject,
            checkUniqueProjectName: checkUniqueProjectName,
            calculateSampleSize : calculateSampleSize
        };
        return service;

        function getProjectsOData(odataOptions) {
            return WebApiClientService.get('/odata/ProjectsOData', {
                params: odataOptions
            });
        };

        function getProject(projectId) {
            var promise = WebApiClientService.get('/api/projects/' + projectId, { params: {} }).then(function (response) {
                var project = new ProjectModel();
                project.id = response.data.Id;
                project.name = response.data.Name;
                project.description = response.data.Description;
                project.docSource = response.data.DocSource;
                project.tag = response.data.Tag;
                project.identifyRepeatedContent = response.data.IdentifyRepeatedContent;
                project.confidence = response.data.Confidence;
                project.marginOfError = response.data.MarginOfError;
                project.stratifyByCustodian = response.data.StratifyByCustodian;
                project.custodianField = response.data.CustodianField;
                project.sampleSize = response.data.SampleSize;
                project.limitExamples = response.data.LimitExamples;
                project.numExamples = response.data.NumOfExamples;
                return project;
            });
            return promise;
        };

        function getProjectSummary(projectId) {
            return WebApiClientService.get('/api/projects/summary/' + projectId, { params: {} }).then(function (response) {
                return response.data;
            });
            return promise;
        };

        function deleteProject(projectId) {
            var promise = WebApiClientService.delete('/api/projects/' + projectId, { params: {} }).then(function (response) {
                //todo: handle errors
            });
            return promise;
        }

        function addProject(project) {
            var promise = WebApiClientService.post('/api/projects/' + project.id, project).then(function (response) {
                //todo: handle errors
            });
            return promise;
        }

        function updateProject(project) {
            var promise = WebApiClientService.put('/api/projects/' + project.id, project).then(function (response) {
                //todo: handle errors
            });
            return promise;
        }

        /*Sample Error Handling examples*/
        function checkUniqueProjectName(projectName) {
            var deferred = $q.defer();
            WebApiClientService.get('/api/projects/1', { params: {} }).then(function (result) {
                if (result.data.Name !== projectName) {
                    deferred.resolve(true);
                } else {
                    var err = new Error();
                    err.status = "Custom";
                    err.message = "Validation Failed"
                    deferred.reject(err);
                }

            });
            return deferred.promise;
        }

        function calculateSampleSize () {
            var min = 1, max = 100;
            var random = Math.floor(Math.random() * (max - min + 1)) + min;
            if(random%2==0){
                return WebApiClientService.get('/api/projects/samplesize', { params: {} }).then(function (response) {
                    return response.data;
                });
            } else if (random % 3) {
                return WebApiClientService.get('/api/test', { params: {} });
            } else {
                return WebApiClientService.get('/api/projects/samplesizeerror', { params: {} });
            }
            
        }
        
    }
 })();
///#source 1 1 /App/analytics/training/services/TrainingDataService.js
(function () {
    "use strict";

    function TrainingDataService(WebApiClientService, $q) {
        function getTrainingResults() {
            return WebApiClientService.get('/api/trainingprogress')
                .then(function (result) {
                    return result.data;
                });
        }
        var service = {
            getTrainingResults: getTrainingResults
        };
        return service;
    }
    angular.module('app')
        .factory('TrainingDataService', TrainingDataService);
    TrainingDataService.$inject = ['WebApiClientService', '$q'];

}());

///#source 1 1 /App/common/directives/BreadcrumbsDirective.js

(function () {
    "use strict";
    angular.module("app")
    .directive('lnBreadcrumbs', function () {
        return {
            scope: {
                breadcrumbs: "="
            },
            restrict: 'EA',
            replace: true,
            templateUrl: '/app/common/directives/breadcrumbsDirective.html',
            link: function (scope, element, attrs, ctrl) {
                //nothing to do here 
            }
        };
    });
 })();
///#source 1 1 /App/common/directives/ConfirmClickDirective.js

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

///#source 1 1 /App/common/directives/ValidationDirective.js
(function () {
    "use strict";
    //Adapted from https://github.com/technovert/angular-validation
    angular.module('app')
        .directive('updateOnBlur', function () {
            return {
                restrict: 'A',
                require: 'ngModel',
                priority: '100',
                /*ignore jslint start*/
                link: function (scope, elm, attr, ngModelCtrl) {
                    /*ignore jslint end*/
                    if (attr.type === 'radio' || attr.type === 'checkbox') {
                        return;
                    }
                    elm.unbind('input')
                        .unbind('keydown')
                        .unbind('change');
                    elm.bind('blur', function () {
                        scope.$apply(function () {
                            ngModelCtrl.$setViewValue(elm.val());
                        });
                    });
                }
            };
        })
        //from http://angular-ui.github.io/ui-utils/#/validate
        .directive('customValidate', function () {
            return {
                restrict: 'A',
                require: 'ngModel',
                link: function (scope, elm, attrs, ctrl) {
                    var validateFn, validators = {},
                        validateExpr = scope.$eval(attrs.customValidate);
                    if (!validateExpr) {
                        return;
                    }
                    if (angular.isString(validateExpr)) {
                        validateExpr = {
                            validator: validateExpr
                        };
                    }
                    angular.forEach(validateExpr, function (exprssn, key) {
                        validateFn = function (valueToValidate) {
                            var expression = scope.$eval(exprssn, {
                                '$value': valueToValidate
                            });
                            if (angular.isObject(expression) && angular.isFunction(expression.then)) {
                                // expression is a promise
                                expression.then(function () {
                                    ctrl.$setValidity(key, true);
                                }, function () {
                                    ctrl.$setValidity(key, false);
                                });
                                return valueToValidate;
                            }
                            if (expression) {
                                // expression is true
                                ctrl.$setValidity(key, true);
                                return valueToValidate;
                            }
                            // expression is false
                            ctrl.$setValidity(key, false);
                            return valueToValidate;
                        };
                        validators[key] = validateFn;
                        ctrl.$formatters.push(validateFn);
                        ctrl.$parsers.push(validateFn);
                    });

                    function apply_watch(watch) {
                        //string - update all validators on expression change
                        if (angular.isString(watch)) {
                            scope.$watch(watch, function () {
                                angular.forEach(validators, function (validatorFn) {
                                    validatorFn(ctrl.$modelValue);
                                });
                            });
                            return;
                        }
                        //array - update all validators on change of any expression
                        if (angular.isArray(watch)) {
                            angular.forEach(watch, function (expression) {
                                scope.$watch(expression, function () {
                                    angular.forEach(validators, function (validatorFn) {
                                        validatorFn(ctrl.$modelValue);
                                    });
                                });
                            });
                            return;
                        }
                        //object - update appropriate validator
                        if (angular.isObject(watch)) {
                            angular.forEach(watch, function (expression, validatorKey) {
                                //value is string - look after one expression
                                if (angular.isString(expression)) {
                                    scope.$watch(expression, function () {
                                        validators[validatorKey](ctrl.$modelValue);
                                    });
                                }
                                //value is array - look after all expressions in array
                                if (angular.isArray(expression)) {
                                    angular.forEach(expression, function (intExpression) {
                                        scope.$watch(intExpression, function () {
                                            validators[validatorKey](ctrl.$modelValue);
                                        });
                                    });
                                }
                            });
                        }
                    }
                    // Support for ui-validate-watch
                    if (attrs.customValidateWatch) {
                        apply_watch(scope.$eval(attrs.customValidateWatch));
                    }
                }
            };
        });
}());
///#source 1 1 /App/common/directives/CustomValidationDirective.js
(function () {
    "use strict";
    angular.module("app")
        .directive('lnSampleCustomValidator', ['projectsDataService', 'alertService', function (projectsDataService,
            alertService) {
            return {
                require: 'ngModel',
                restrict: 'A',
                controller: function () {
                    var ctrl = this;
                    function sampleValidation(value) {
                        if (!value || value.length <= 0) {
                            return value;
                        }
                        //note: this example doesn't make much sense, but the point is to
                        // demonstrate how a service call can be made in a validator
                        projectsDataService.getProject(1)
                            .then(function (project) {
                                if (value === project.id) {
                                    ctrl.$setValidity('lnSampleCustomValidator', false);
                                } else {
                                    ctrl.$setValidity('lnSampleCustomValidator', true);
                                }
                            }, function (error) {
                                alertService.showAlert('Error validating project: ' + error.status +
                                    ' ' + error.statusText);
                            })
                            .catch(function (msg) {
                                alertService.showAlert('Error validating project: ' + msg);
                            });
                        return value;
                    }

                    ctrl.$parsers.unshift(sampleValidation);
                    ctrl.$formatters.unshift(sampleValidation);

                }
            };
        }]);
}());
///#source 1 1 /App/analytics/training/trainingProgress/TrainingProgressDirective.js
(function () {
    "use strict";

    function TrainingProgressDirective() {
        return {
            restrict: 'EA',
            scope: {},
            controller: 'TrainingProgressController',
            templateUrl: '/app/analytics/training/trainingProgress/TrainingProgressDirective.html'
        };
    }
    angular.module('app')
        .directive('lnTrainingProgress', TrainingProgressDirective);
}());

///#source 1 1 /App/analytics/training/documentSets/DocumentSetsListDirective.js
(function () {
    "use strict";
    function DocumentSetsListDirective() {
        return {
            restrict: 'EA',
            scope: {},
            controller: 'DocumentSetsListController',
            templateUrl: '/app/analytics/training/documentSets/DocumentSetsListDirective.html'
        };
    }
    angular.module('app')
        .directive('lnDocumentSetsList', DocumentSetsListDirective);


}());
///#source 1 1 /App/analytics/training/documentSets/DocumentSetsListController.js
(function () {
    "use strict";

    function DocumentSetsListController($scope, MattersDataService, DataSetsDataService, DocumentSetsDataService,
        AlertService) {
        var vm = $scope, //this;   //hack, controllerAs is not working correctly here
            documentSetsDataSource = {},
            defaultSort = {};
        function init() {
            vm.localized = DocumentSetsListResources;
            vm.clear = clear;
            vm.refresh = refresh;
            vm.typeChanged = typeChanged;
            vm.statusChanged = statusChanged;
            bindTypes();
            bindStatuses();
            configureDocumentSetsGrid();
            }
        init();
        function configureDocumentSetsGrid() {
            defaultSort = [{field: "Name",dir: "asc"}];
            documentSetsDataSource = new kendo.data.DataSource({
                type: "odata",
                transport: {
                    read: function (options) {
                        var odataParams = kendo.data.transports.odata.parameterMap(options.data,"read");
                        DocumentSetsDataService.getDocumentSets(odataParams)
                            .success(function (result) {
                                options.success(result);
                            })
                            .error(function () {
                                //TODO Exception Handling
                            });
                    }
                },
                pageSize: 10,
                sort: defaultSort,
                serverPaging: true,
                serverSorting: true,
                serverFiltering: true,
                schema: {
                    data: function (data) {
                        return data.value;
                    },
                    total: function (data) {
                        return data["odata.count"];
                    }
                },
                error: function (error) {
                    AlertService.showAlert('Error retrieving document sets: ' + error.status +
                        ' ' + error.statusText);
                }
            });
            vm.gridDocSetsConfig = {
                dataSource: documentSetsDataSource,
                sortable: true,
                pageable: true,
                filterable: true,
                columns: [{
                    field: "Name",
                    title: "Name"
                }, {
                    field: "Type",
                    title: "Type"
                }, {
                    field: "Documents",
                    title: "Docs",
                    filterable: {
                        ui: "numerictextbox"
                    }
                }, {
                    field: "Reviewed",
                    title: "Reviewed",
                    filterable: {
                        ui: "numerictextbox"
                    }
                }, {
                    field: "NotReviewed",
                    title: "Not Reviewed",
                    filterable: {
                        ui: "numerictextbox"
                    }
                }, {
                    field: "Status",
                    title: "Status"
                }]
            };
        }

        function bindTypes() {
            MattersDataService.getMatters()
                .then(function (data) {
                    vm.types = data;
                }, function (error) {
                    AlertService.showAlert('Error retrieving matters: ' + error.status + ' ' + error.statusText);
                });
        }

        function bindStatuses() {
            DataSetsDataService.getDataSets()
                .then(function (data) {
                    vm.statuses = data;
                }, function (error) {
                    AlertService.showAlert('Error retrieving datasets: ' + error.status + ' ' + error.statusText);
                });
        }

        function refreshGrid() {
            documentSetsDataSource.read();
        }

        function refresh() {
            refreshGrid();
        }

        function typeChanged() {
            refreshGrid();
        }

        function statusChanged() {
            refreshGrid();
        }

        function clear() {
            vm.selectedType = null;
            vm.selectedStatus = null;
            documentSetsDataSource.page(1);
            documentSetsDataSource.sort(defaultSort);
            documentSetsDataSource.filter([]);
            refresh();
        }

        
    }
    angular.module('app')
        .controller('DocumentSetsListController', DocumentSetsListController);
    DocumentSetsListController.$inject = ['$scope', 'MattersDataService', 'DataSetsDataService',
        'DocumentSetsDataService', 'AlertService'];
}());

///#source 1 1 /App/analytics/projects/projectWizard/ProjectWizardController.js
(function () {
    "use strict";

    function ProjectWizardController($scope, $routeParams, $location, ProjectsDataService, ProjectModel,
        AlertService) {
        var vm = this,
            projectId = $routeParams.id;

        function isNew() {
            return (projectId === 'undefined');
        }

        function bindProject() {
            vm.isLoading = false;
            if (isNew()) {
                vm.project = new ProjectModel();
                vm.pagetitle = 'New Project';
            } else {
                vm.isLoading = true;
                ProjectsDataService.getProject(1)
                    .then(function (project) {
                        vm.project = project;
                        vm.isLoading = false;
                    });
                vm.pagetitle = 'Edit Project';
            }
        }

        function setTab(tabIndex) {
            vm.activetab.active = false;
            vm.activetab = vm.tabs[tabIndex];
            vm.activetab.active = true;
            vm.submitted = false;
        }

        function next(currentForm) {
            if (currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            currentForm.$setPristine();
            var index = vm.tabs.indexOf(vm.activetab);
            if (index + 1 < vm.tabs.length) {
                vm.activetab.active = false;
                vm.activetab = vm.tabs[index + 1];
                vm.activetab.active = true;
            }
        }

        function back(currentForm) {
            if (currentForm.$dirty && currentForm.$invalid) {
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            currentForm.$setPristine();
            var index = vm.tabs.indexOf(vm.activetab);
            if (index > 0) {
                vm.activetab.active = false;
                vm.activetab = vm.tabs[index - 1];
                vm.activetab.active = true;
            }
        }

        function sampleCustomValidator(marginOfError) {
            return marginOfError > 2;
        }

        function save(currentForm) {
            if (currentForm.$dirty && currentForm.$invalid) {
                return;
            }
            currentForm.$setPristine();
            if (isNew()) {
                ProjectsDataService.addProject(vm.project)
                    .then(function (data, status) {
                        $location.path('/list');
                    }, function (error) {
                        AlertService.showAlert('Error adding project: ' + error.status + ' ' + error.statusText);
                    });
            } else {
                ProjectsDataService.updateProject(vm.project)
                    .then(function (data, status) {
                        $location.path('/list');
                    }, function (error) {
                        AlertService.showAlert('Error updating project: ' + error.status + ' ' + error.statusText);
                    });
            }
        }

        function init() {
            vm.setTab = setTab;
            vm.next = next;
            vm.back = back;
            vm.save = save;
            vm.calculate = calculate;
            vm.sampleCustomValidator = sampleCustomValidator;
            vm.localized = ProjectWizardResources;
            vm.totalDocs = "67,890";
            vm.breadcrumbs = ['System', 'Matter 4'];
            vm.tabs = [{
                title: 'Settings',
                active: true,
                disabled: false
            }, {
                title: 'Controls',
                active: false,
                disabled: false
            }, {
                title: 'Example',
                active: false,
                disabled: false
            }, {
                title: 'Summary',
                active: false,
                disabled: false
            }];
            vm.activetab = vm.tabs[0];
            vm.tagoptions = [{
                text: 'Extracted Text Tag',
                value: 'Extracted'
            }, {
                text: 'Other',
                value: 'Other'
            }];
            vm.savedsearchoptions = [{
                text: 'Sample Search',
                value: 1
            }, {
                text: 'Another Search',
                value: 2
            }];
            vm.confidenceoptions = [{
                text: '75%',
                value: 75
            }, {
                text: '85%',
                value: 85
            }, {
                text: '95%',
                value: 95
            }];
            vm.marginoferrorsoptions = [{
                text: '2.0%',
                value: 2.0
            }, {
                text: '2.5%',
                value: 2.5
            }, {
                text: '3.0%',
                value: 3.0
            }, {
                text: '3.5%',
                value: 3.5
            }];
            vm.custodianfields = [{
                text: 'Other',
                value: 'Other'
            }, {
                text: 'Custodian',
                value: 'Custodian'
            }];
            bindProject();
        }

        vm.ProjectNameValidations = {
            async: {
                "ProjectNameAvailability": function (value) { return ProjectsDataService.checkUniqueProjectName(value); }
            }
        }

        vm.ProjectDescriptionValidations = {
            patterns: {
                "whitespace": /^\S+$/,
                "numeric": /\d+/,
                "lowercase": /[a-z]+/,
                "uppercase": /[A-Z]+/,
                "special": /\W+/
            }
        }


        var calculate = function () {
            ProjectsDataService.calculateSampleSize().then(function (data) {
                vm.project.sampleSize = data.size;
            });
        }
        init();
    }

    angular.module('app')
        .controller('ProjectWizardController', ProjectWizardController);
    ProjectWizardController.$inject = ['$scope', '$routeParams', '$location', 'ProjectsDataService', 'ProjectModel',
        'AlertService'];
}());

///#source 1 1 /App/analytics/projects/projectList/ProjectListController.js
(function () {
    "use strict";

    function ProjectListController($location, ProjectsDataService) {
        var projectsDataSource = {},
            refreshGrid = function () {
                projectsDataSource.read();
            },
            defaultSort = [{
                field: "ProjectName",
                dir: "asc"
            }],
            vm = this,
            clear = function () {
                projectsDataSource.page(1);
                projectsDataSource.sort(defaultSort);
                projectsDataSource.filter([]);
                vm.refresh();
            },
            editProject = function (projectId) {
                $location.path('/wizard/' + projectId);
            },
            goToDashboard = function () {
                $location.path('/dashboard');
            },
            deleteProject = function (projectId) {
                ProjectsDataService.deleteProject(projectId)
                    .then(
                        function () {
                            refreshGrid();
                        },
                        function () {
                            //TODO Exception Handling
                        }
                    );
            },
            refresh = function () {
                refreshGrid();
            },
            configureProjectsGrid = function () {
                projectsDataSource = new kendo.data.DataSource({
                    type: "odata",
                    transport: {
                        read: function (options) {
                            var odataParams = kendo
                                .data.transports
                                .odata.parameterMap(options.data, "read");
                            ProjectsDataService.getProjectsOData(odataParams)
                                .success(function (result) {
                                    options.success(result);
                                })
                                .error(function () {
                                    //TODO Exception Handling
                                });
                        }
                    },
                    pageSize: 10,
                    sort: defaultSort,
                    serverPaging: true,
                    serverSorting: true,
                    serverFiltering: true,
                    schema: {
                        data: function (data) {
                            return data.value;
                        },
                        total: function (data) {
                            return data["odata.count"];
                        }
                    },
                    error: function () {
                        //TODO Exception Handling
                    }
                });
                vm.gridProjectsConfig = {
                    dataSource: projectsDataSource,
                    sortable: true,
                    pageable: {
                        pageSizes: [10, 25, 50, 100]
                    },
                    filterable: true,
                    columns: [{
                        field: "ProjectName",
                        title: vm.localized.ProjectName,
                        template: "<a id='check-all' " +
                            "ng-click='ProjectListController.goToDashboard()'>#: ProjectName#</a>"
                    }, {
                        field: "MatterName",
                        title: vm.localized.MatterName
                    }, {
                        field: "Source",
                        title: vm.localized.Source
                    }, {
                        field: "CreatedBy",
                        title: vm.localized.CreatedBy
                    }, {
                        field: "CreatedOn",
                        title: vm.localized.CreatedOn,
                        template: "#= kendo.toString(kendo.parseDate(CreatedOn), 'G') #",
                        filterable: {
                            ui: "datetimepicker"
                        }
                    }, {
                        field: "Documents",
                        title: vm.localized.Documents,
                        filterable: {
                            ui: "numerictextbox"
                        }
                    }, {
                        field: "ProjectId",
                        filterable: false,
                        sortable: false,
                        title: vm.localized.Actions,
                        template: "<a class='btn btn-default btn-sm' id='check-all'" +
                            " ng-click='ProjectListController.editProject(#: ProjectId #)'>" + vm.localized.Edit +
                            "</a> <a class='btn btn-default btn-sm' id='check-all' " +
                            " ln-confirm-click='Are you sure you want to delete this record?' " +
                            " ng-click='ProjectListController.deleteProject(#: ProjectId #)'>" +
                            vm.localized.Delete + "</a>"
                    }]
                };
            },
            init = function () {
                vm.localized = ProjectResources;
                vm.pagetitle = vm.localized.Pagetitle;
                vm.breadcrumbs = [vm.localized.System, vm.localized.Analytics];
                vm.editProject = editProject;
                vm.deleteProject = deleteProject;
                vm.goToDashboard = goToDashboard;
                vm.clear = clear;
                vm.refresh = refresh;
                configureProjectsGrid();
            };
        init();
    }
    angular.module('app')
        .controller('ProjectListController', ProjectListController);
    ProjectListController.$inject = ['$location',
        'ProjectsDataService'];

}());

///#source 1 1 /App/analytics/projects/dashboard/ProjectDashboardController.js
(function () {
"use strict";

    function ProjectDashboardController($scope, $modal, ProjectsDataService, AlertService) {
        var vm = this;

        function bindDetails() {
            ProjectsDataService.getProjectSummary(1)
                .then(function (project) {
                    vm.sourcedataset = project.SourceDataSet;
                    vm.totaldocs = project.TotalDocs;
                    vm.crossreffield = project.CrossRefField;
                    vm.controlset = project.ControlSet;
                    vm.dbserver = project.DatabaseServer;
                    vm.analyticsserver = project.AnalyticsServer;
                }, function (error) {
                    AlertService.showAlert('Error retrieving project summary: ' + error.status + ' ' +
                        error.statusText);
                });
                }

        function showAlert(alert, alertType) {
            vm.alerts.push({
                msg: alert,
                type: alertType
            });
        }

        function closeAlert(index) {
            vm.alerts.splice(index, 1);
        }

        $scope.showNewControlSetModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/CreateControlSetView.html',
                controller: 'CreateControlSetController',
                controllerAs: 'CreateControlSetController',
                size: 'smx'
            });
            modalInstance.result.then(function () {
                showAlert('New Control Set created successfully', 'success');
            }, function () {
                return;
            });
        };

        $scope.showNewQCSetModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/CreateQCSetView.html',
                controller: 'CreateQCSetController',
                controllerAs: 'CreateQCSetController',
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                showAlert('New QC Set created successfully', 'success');
            }, function () {
                //modal was canceled
            });
        };

        $scope.showNewPredictionSetModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/CreatePredictionSetView.html',
                controller: 'CreatePredictionSetController',
                controllerAs: 'CreatePredictionSetController',
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                showAlert('New Prediction Set job is scheduled', 'success');
            }, function () {
                //modal was canceled
            });
        };


        $scope.showRunAccuracyTestModal = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/analytics/projects/modals/RunAccuracyTestView.html',
                controller: 'RunAccuracyTestController',
                controllerAs: 'RunAccuracyTestController',
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                showAlert('Run Accuracy Test is scheduled', 'success');
            }, function () {
                //modal was canceled
            });
        };
        function init() {
            vm.localized = ProjectDashboardResources;
            vm.breadcrumbs = ['System', 'Matter 4', 'AR - DS4'];
            vm.pagetitle = 'Sample Dashboard';
            vm.alerts = [];
            vm.enablesettings = false;
            vm.closeAlert = closeAlert;
            vm.showNewControlSetModal = $scope.showNewControlSetModal;
            vm.doSomething = function () {
                AlertService.showAlert('Do something useful');
            };
            bindDetails();
        }
        init();
    }
    angular.module('app')
        .controller('ProjectDashboardController', ProjectDashboardController);
    ProjectDashboardController.$inject = ['$scope', '$modal', 'ProjectsDataService', 'AlertService'];

}());

///#source 1 1 /App/analytics/training/trainingProgress/TrainingProgressController.js
(function () {
    "use strict";
    function TrainingProgressController($scope, TrainingDataService, AlertService) {
        var progressDataSource = {},
            vm = $scope; //this;   //todo: hack, controllerAs is not working correctly here

        function configureChart() {
            progressDataSource = new kendo.data.DataSource({
                data: vm.chartData
            });
            vm.chartTrainingProgress = {
                dataSource: progressDataSource,
                seriesDefaults: {
                    type: "line",
                    errorBars: {
                        value: "percentage(20)"
                    }
                },
                series: [{
                    field: "Precision",
                    name: "Precision"
                }, {
                    field: "Recall",
                    name: "Recall"
                }, {
                    field: "Accuracy",
                    name: "Accuracy (F1)"
                }],
                valueAxis: {
                    labels: {
                        format: "{0}%"
                    },
                    line: {
                        visible: true
                    },
                    axisCrossingValue: 0
                },
                categoryAxis: {
                    field: "Name",
                    line: {
                        visible: true
                    }
                },
                legend: {
                    position: "bottom"
                },
                tooltip: {
                    visible: true,
                    format: "{0}%",
                    template: "#= series.name #: #= value #%"
                }
            };
        }

        function viewreport() {
            AlertService.showAlert('View report');
        }

        function bindChart() {
            TrainingDataService.getTrainingResults()
                .then(function (data) {
                    vm.chartData = data;
                    progressDataSource.data(data);
                }, function (error) {
                    AlertService.showAlert('Error loading training results: ' + error.status + ' ' +
                        error.statusText);
                });
        }
        function refresh() {
            bindChart();
        }

        function init() {
            vm.localized = TrainingProgressResources; //set this to the json generated from the resx file
            vm.chartData = {};
            vm.viewreport = viewreport;
            vm.refresh = refresh;
            configureChart();
            bindChart();
        }
        init();
    }
    angular.module('app')
        .controller('TrainingProgressController', TrainingProgressController);
    TrainingProgressController.$inject = ['$scope', 'TrainingDataService', 'AlertService'];


}());

///#source 1 1 /App/analytics/projects/modals/CreateControlSetController.js
(function () {
    "use strict";
    function CreateControlSetController($modalInstance, ControlSetService,
        ControlsetModel) {
        var vm = this;
        function init() {
            vm.matterId = "2"; //TODO: matterid, datasetid, projectid need to come from context
            vm.datasetId = "3";
            vm.projectId = "4";
            vm.localized = CreateControlSetResources;
            vm.confidences = [{
                name: vm.localized.confidences_95 + vm.localized.percentage,
                value: vm.localized.confidences_95
            }, {
                name: vm.localized.confidences_90 + vm.localized.percentage,
                value: vm.localized.confidences_90
            }, {
                name: vm.localized.confidences_85 + vm.localized.percentage,
                value: vm.localized.confidences_85
            }];

            vm.errorMargins = [{
                name: vm.localized.errorMargins_p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_p5
            }, {
                name: vm.localized.errorMargins_1 + vm.localized.percentage,
                value: vm.localized.errorMargins_1
            }, {
                name: vm.localized.errorMargins_1p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_1p5
            }, {
                name: vm.localized.errorMargins_2 + vm.localized.percentage,
                value: vm.localized.errorMargins_2
            }, {
                name: vm.localized.errorMargins_2p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_2p5
            }, {
                name: vm.localized.errorMargins_5 + vm.localized.percentage,
                value: vm.localized.errorMargins_5
            }];
            vm.pageModel = new ControlsetModel();
        }
        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
        vm.calculateSampleSize = function (currentForm) {
            if (currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            vm.pageModel.confidenceLevel = vm.confidenceSelected.value;
            vm.pageModel.marginOfError = vm.errorMarginSelected.value;
            ControlSetService.getSampleSize(vm.matterId, vm.datasetId, vm.pageModel).then(
                function (response) {
                    vm.sampleSize = response.data;
                });
        };
        function addControlset(currentForm) {
            if (currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
                return;
            }
            vm.submitted = false;
            vm.pageModel.confidenceLevel = vm.confidenceSelected.value;
            vm.pageModel.marginOfError = vm.errorMarginSelected.value;
            ControlSetService.addControlset(vm.matterId, vm.datasetId,
                vm.projectId, vm.pageModel).then(
                function () {
                    $modalInstance.close();
                },
                function () {
                    //TODO : Exception handling
                }
            );
        }
        vm.ok = function (currentForm) {
            addControlset(currentForm);
        };
        init();
    }
    angular.module('app').controller('CreateControlSetController',
         CreateControlSetController);
    CreateControlSetController.$inject = ['$modalInstance',
        'ControlSetService', 'ControlsetModel'];
}());
///#source 1 1 /App/analytics/projects/modals/CreatePredictionSetController.js
(function () {
    "use strict";
    function CreatePredictionSetController($modalInstance, PredictionSetService) {
        var vm = this;
        function init() {
            //TODO : matterid, datasetid, projectid need to come from context
            vm.matterId = "2";
            vm.datasetId = "3";
            vm.projectId = "4";
            vm.localized = CreatePredictionSetResources;
        }
        vm.ok = function () {
            PredictionSetService.createPredictionSetJob(vm.matterId,
                vm.datasetId, vm.projectId).then(
                function () {
                    $modalInstance.close();
                },
                function () {
                    //TODO Exception Handling
                }
            );
        };
        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
        init();
    }
    angular.module('app').controller('CreatePredictionSetController',
         CreatePredictionSetController);
    CreatePredictionSetController.$inject = ['$modalInstance',
        'PredictionSetService'];
}());
///#source 1 1 /App/analytics/projects/modals/CreateQCSetController.js
(function () {
    "use strict";
    function CreateQCSetController($modalInstance, QCSetService, QCSetModel) {
        var vm = this,
            typeStatistical = "statistical",
            typePercentage = "percentage";
        function init() {
            //TODO : matterid, datasetid, projectid need to come from context
            vm.matterId = "2";
            vm.datasetId = "3";
            vm.projectId = "4";
            vm.localized = CreateQCSetResources;
            vm.confidences = [{
                name: vm.localized.confidences_95 + vm.localized.percentage,
                value: vm.localized.confidences_95
            }, {
                name: vm.localized.confidences_90 + vm.localized.percentage,
                value: vm.localized.confidences_90
            }, {
                name: vm.localized.confidences_85 + vm.localized.percentage,
                value: vm.localized.confidences_85
            }];
            vm.errorMargins = [{
                name: vm.localized.errorMargins_p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_p5
            }, {
                name: vm.localized.errorMargins_1 + vm.localized.percentage,
                value: vm.localized.errorMargins_1
            }, {
                name: vm.localized.errorMargins_1p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_1p5
            }, {
                name: vm.localized.errorMargins_2 + vm.localized.percentage,
                value: vm.localized.errorMargins_2
            }, {
                name: vm.localized.errorMargins_2p5 + vm.localized.percentage,
                value: vm.localized.errorMargins_2p5
            }, {
                name: vm.localized.errorMargins_5 + vm.localized.percentage,
                value: vm.localized.errorMargins_5
            }];
            vm.pageModel = new QCSetModel();
        }
        init();
        function setPageModel() {
            if (vm.sampling === typeStatistical) {
                vm.pageModel.confidenceLevel = vm.confidenceSelected.value;
                vm.pageModel.marginOfError = vm.errorMarginSelected.value;
                vm.pageModel.isStatistical = true;

            } else if (vm.sampling === typePercentage) {
                vm.pageModel.isStatistical = false;
                vm.pageModel.percentage = vm.sliderPercentage;
            }
            vm.pageModel.relevantType = vm.relevance;
            return vm.pageModel;
        }
        function validateForm(currentForm) {
            if (angular.isUndefined(vm.relevance)) {
                vm.errorOnRelvanceType = true;
            } else {
                vm.errorOnRelvanceType = false;
            }

            if (angular.isUndefined(vm.sampling)) {
                vm.errorOnSamplingType = true;
            } else {
                vm.errorOnSamplingType = false;
            }
            
            if (vm.sampling === typeStatistical && currentForm.$invalid) {
                currentForm.$setDirty();
                vm.submitted = true;
            } else {
                vm.submitted = false;
            }

            if (vm.errorOnRelvanceType || vm.errorOnSamplingType || vm.submitted) {
                return false;
            }
            return true;
        }
        vm.ok = function (currentForm) {
            var status = validateForm(currentForm);
            if (status) {
                QCSetService.addQCset(vm.matterId, vm.datasetId,
                    vm.projectId, setPageModel()).then(
                    function () {
                        $modalInstance.close();
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
            }
        };

        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
        vm.calculateSampleSize = function (currentForm) {
            var status = validateForm(currentForm);
            if (status) {
                QCSetService.getSampleSize(vm.matterId,
                    vm.datasetId, setPageModel()).then(
                    function (response) {
                        vm.sampleSize = response.data;
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
            }
        };
    }
    angular.module('app').controller('CreateQCSetController', CreateQCSetController);
    CreateQCSetController.$inject = ['$modalInstance', 'QCSetService', 'QCSetModel'];
}());
///#source 1 1 /App/analytics/projects/modals/RunAccuracyTestController.js
(function () {
    "use strict";
    function RunAccuracyTestController($modalInstance, RunAccuracyTestService) {
        var vm = this;
        function init() {
            //TODO  : projectid and controlsetid need to come from context
            vm.projectId = "4";
            vm.controlsetId = "5";
            vm.localized = RunAccuracyTestResources;
        }
        init();
        vm.ok = function () {

            RunAccuracyTestService.createRunAccuracyTest(vm.projectId, vm.controlsetId).then(
                function () {
                    $modalInstance.close();
                },
                function () {
                    //TODO Exception Handling
                }
            );
        };
        vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
    }
    angular.module('app').controller('RunAccuracyTestController', RunAccuracyTestController);
    RunAccuracyTestController.$inject = ['$modalInstance', 'RunAccuracyTestService'];
}());
///#source 1 1 /App/analytics/projects/models/ControlsetModel.js
(function () {
    "use strict";
    angular.module('app').factory('ControlsetModel', function () {

        var ControlsetModel = function () {
            this.confidenceLevel = 0;
            this.marginOfError = 0;
        };

        ControlsetModel.prototype = {
            doSomething: function () {
            }
        };

        return ControlsetModel;

    });


}());
///#source 1 1 /App/analytics/projects/models/QCSetModel.js
(function () {
    "use strict";
    angular.module('app').factory('QCSetModel', function () {

        var QCSetModel = function () {
            this.isStatistical = false;
            this.confidenceLevel = 0;
            this.marginOfError = 0;
            this.percentage = 0;
            this.relevantType = "";
        };

        QCSetModel.prototype = {
            doSomething: function () {
            }
        };

        return QCSetModel;

    });
}());
///#source 1 1 /App/analytics/projects/services/ControlSetService.js
(function () {
    "use strict";
    function ControlSetService(WebApiClientService) {
        function getSampleSize(matterId, datasetId, controlset) {
            return WebApiClientService.post('/api/controlset/samplesize/'
                + matterId + '/' + datasetId, controlset);
        }
        function addControlset(matterId, datasetId, projectId, controlset) {
            return WebApiClientService.post('/api/controlset/'
                + matterId + '/' + datasetId + '/' + projectId, controlset);
        }
        var service = {
            getSampleSize: getSampleSize,
            addControlset: addControlset
        };
        return service;
    }
    angular.module('app').factory('ControlSetService', ControlSetService);
    ControlSetService.$inject = ['WebApiClientService'];
}());
///#source 1 1 /App/analytics/projects/services/PredictionSetService.js
(function () {
    "use strict";
    function PredictionSetService(WebApiClientService) {
        function createPredictionSetJob(matterId, datasetId, projectId) {
            return WebApiClientService.post('/api/predictionset/' +
                matterId + '/' + datasetId + '/' + projectId);
        }
        var service = {

            createPredictionSetJob: createPredictionSetJob
        };
        return service;
    }
    angular.module('app').factory('PredictionSetService', PredictionSetService);
    PredictionSetService.$inject = ['WebApiClientService'];
}());
///#source 1 1 /App/analytics/projects/services/QCSetService.js
(function () {
    "use strict";
    function QCSetService(WebApiClientService) {
        function getSampleSize(matterId, datasetId, qcSet) {
            return WebApiClientService.post('/api/qcset/samplesize/'
                + matterId + '/' + datasetId, qcSet);
        }
        function addQCset(matterId, datasetId, projectId, qcSet) {
            return WebApiClientService.post('/api/qcset/'
                + matterId + '/' + datasetId + '/' + projectId, qcSet);
        }
        var service = {
            getSampleSize: getSampleSize,
            addQCset: addQCset
        };
        return service;
    }
    angular.module('app').factory('QCSetService', QCSetService);
    QCSetService.$inject = ['WebApiClientService'];
}());
///#source 1 1 /App/analytics/projects/services/RunAccuracyTestService.js
(function () {
    "use strict";
    function RunAccuracyTestService(WebApiClientService) {
        function createRunAccuracyTest(projectId, controlsetId) {
            return WebApiClientService.post('/api/runaccuracytest/' + projectId + '/' + controlsetId);
        }
        var service = {
            createRunAccuracyTest: createRunAccuracyTest
        };
        return service;
    }
    angular.module('app')
        .factory('RunAccuracyTestService', RunAccuracyTestService);
    RunAccuracyTestService.$inject = ['WebApiClientService', '$q'];
}());

///#source 1 1 /App/common/directives/FileBrowse/FileBrowse.resx.js
var FileBrowseResources = {
    Browse: 'Browse',
    BrowseTypeFolderTitle: 'Select Folder',
    BrowseTypeFileTitle: 'Select File',
    BrowseTypeCreateFileTitle: 'Create File',
    ErrorInfoPath: 'Select file path',
    ErrorInfoFileName: 'Enter file name',
    Path: 'Path',
    Name: 'Name',
    AvailableSpace: 'Available Space',
    FileName: 'File Name',
    Ok: 'OK',
    Cancel: 'Cancel',
    MB: 'MB',
    ErrorOnFetchSharePath: 'Error retrieving on share path:',
    ErrorOnFetchFilePath: 'Error retrieving on file paths ',
    ErrorOnFetchFilePathWithValidate: 'Error retrieving on file paths with validate'
};

///#source 1 1 /App/common/directives/FileBrowse/FileBrowseController.js
(function () {
    "use strict";
    function FileBrowseController($scope, $modal) {
        var vm = this;
        vm.open = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/common/directives/FileBrowse/FolderFilePathDialogView.html',
                controller: 'FolderFilePathDialogController',
                controllerAs: 'FolderFilePathDialogController',
                resolve: {
                    pathType: function () {
                        return $scope.browsetype;
                    },
                    pathText: function () {
                        return $scope.lnFilePath;
                    }
                },
                size: 'smx'
            });
            modalInstance.result.then(function (returnValue) {
                $scope.lnFilePath = returnValue;
            }, function () {
                $scope.lnFilePath = '';
            });
        };
        function init() {
            vm.localized = FileBrowseResources;
        }
        init();
    }
    angular.module('app')
        .controller('FileBrowseController', FileBrowseController);
    FileBrowseController.$inject = ['$scope', '$modal'];

}());

///#source 1 1 /App/common/directives/FileBrowse/FileBrowseDirective.js
(function () {
    "use strict";

    angular.module("app")
        .directive('lnFilebrowse', function () {
            return {
                restrict: 'EA',
                scope: {
                    browsetype: '@lnFilebrowseType',
                    lnFilePath: '='
                },
                controller: 'FileBrowseController',
                controllerAs: 'FileBrowseController',
                replace: true,
                templateUrl: '/app/common/directives/FileBrowse/FileBrowseView.html'
            };
        });


}());

///#source 1 1 /App/common/directives/FileBrowse/FiledialogModel.js
(function () {
    "use strict";
    angular.module('app')
        .factory('FileDialogModel', function () {
            var FileDialogModel = function () {
                this.name = '';
                this.path = '';
                this.type = '';
                this.size = 0;
            };
            return FileDialogModel;

        });

}());

///#source 1 1 /App/common/directives/FileBrowse/FileDialogService.js
(function () {
    "use strict";

    function FileDialogService(WebApiClientService) {

        function getShares(organizationId) {
            return WebApiClientService.get('/api/filedialog/share/' + organizationId);
        }

        function getFolderFilePath(type, fileinfo) {
            return WebApiClientService.post('/api/filedialog/path/' + type, fileinfo);
        }

        function getFolderFilePathsWithValidate(type, fileinfo) {
            return WebApiClientService.post('/api/filedialog/path/validate/' + type, fileinfo);
        }
        var service = {
            getShares: getShares,
            getFolderFilePath: getFolderFilePath,
            getFolderFilePathsWithValidate: getFolderFilePathsWithValidate
        };
        return service;
    }
    angular.module('app')
        .factory('FileDialogService', FileDialogService);
    FileDialogService.$inject = ['WebApiClientService'];
}());

///#source 1 1 /App/common/directives/FileBrowse/FolderFilePathDialogController.js
(function () {
    "use strict";

    function FolderFilePathDialogController($modalInstance, FileDialogModel, FileDialogService, pathType, pathText) {

        var vm = this,
            browseFileType = 'file',
            browseFolderType = 'folder',
            browseCreateFileType = 'createfileinfolder',
            rootPathType = 'root';
        function bindShares(organizationId) {

            FileDialogService.getShares(organizationId)
                .then(
                    function (response) {
                        vm.fileInformations = response.data;
                        vm.colShow = true;
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
        }
        function getFolderFilePathsWithValidate(type, fileinfo) {
            FileDialogService.getFolderFilePathsWithValidate(type, fileinfo)
                .then(
                    function (response) {
                        vm.fileInformations = response.data;
                        vm.colShow = false;
                        if (vm.fileinfo.type === rootPathType) {
                            vm.colShow = true;
                        }
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
        }
        function bindFolderFilePath(filePath) {
            var type = vm.browseType,
                filename,
                folderPath;
            if (vm.browseType === browseCreateFileType) {
                filename = filePath.substr(filePath.lastIndexOf("\\") + 1);
                folderPath = filePath.substr(0, filePath.lastIndexOf("\\"));
                filePath = folderPath;
                vm.lnModalFileName = filename;
                type = browseFolderType;
            }
            vm.fileinfo.path = filePath;
            vm.fileinfo.type = vm.browseType;
            vm.spath = filePath;
            getFolderFilePathsWithValidate(type, vm.fileinfo);
        }
        function getFodlderFilePath(type, fileinfo) {
            FileDialogService.getFolderFilePath(type, fileinfo)
                .then(
                    function (response) {
                        vm.fileInformations = response.data;
                        vm.colShow = false;
                        if (vm.fileinfo.type === rootPathType) {
                            vm.colShow = true;
                        }
                    },
                    function () {
                        //TODO Exception Handling
                    }
                );
        }
        vm.select = function (val) {
            vm.fileinfo.path = val.Path;
            vm.fileinfo.name = val.Name;
            vm.fileinfo.type = val.Type;
            vm.spath = val.Path;
            if (vm.fileinfo.type === browseFileType) {
                return;
            }
            getFodlderFilePath(vm.browseType, vm.fileinfo);
        };
        vm.ok = function () {
            if (angular.isUndefined(vm.spath) || vm.spath === null || vm.spath === '') {
                vm.isErrorInfoPath = true;
                return;
            }
            if (vm.browseType === browseFileType && vm.fileinfo.type !== browseFileType) {
                vm.isErrorInfoPath = true;
                return;
            }

            if (vm.browseType === browseCreateFileType) {
                if ((angular.isUndefined(vm.lnModalFileName) || vm.lnModalFileName === null ||
                        vm.lnModalFileName === '' || vm.lnModalFileName.lastIndexOf(".") === -1)) {
                    vm.isErrorInfoFileName = true;
                    vm.isErrorInfoPath = false;
                    return;
                }
                vm.spath = vm.spath + '\\' + vm.lnModalFileName;
            }

            $modalInstance.close(vm.spath);
        };
        vm.cancel = function () {

            $modalInstance.dismiss('cancel');
        };
        function init() {
            // Todo: Need to come from context
            var organizationId = 2;
            vm.localized = FileBrowseResources;
            vm.browseType = pathType;
            vm.colShow = true;
            vm.isErrorInfoPath = false;
            vm.showFileName = false;
            vm.isErrorInfoFileName = false;

            if (vm.browseType === browseFolderType) {
                vm.title = vm.localized.BrowseTypeFolderTitle;
            } else if (vm.browseType === browseFileType) {
                vm.title = vm.localized.BrowseTypeFileTitle;
            } else if (vm.browseType === browseCreateFileType) {
                vm.title = vm.localized.BrowseTypeCreateFileTitle;
                vm.showFileName = true;
            }

            vm.fileinfo = new FileDialogModel();

            if (angular.isUndefined(pathText) || pathText === '') {

                bindShares(organizationId);

            } else {
                bindFolderFilePath(pathText);
            }
        }
        init();
    }
    angular.module('app')
        .controller('FolderFilePathDialogController', FolderFilePathDialogController);
    FolderFilePathDialogController.$inject = ['$modalInstance',
        'FileDialogModel', 'FileDialogService', 'pathType', 'pathText'];

}());

///#source 1 1 /App/common/directives/UserPicker/Userpicker.resx.js
var UserPickerResources = {
    Assign: 'Assign',
    UnAssign: 'Un Assign',
    TitleUserGroups: 'Select User Groups',
    TitleUsers: 'Select Users',
    TitleUserGroupsUsers: 'Select Users / User Groups',
    Filter: 'Filter',
    Reset: 'Reset',
    Cancel: 'Cancel',
    Ok: 'Ok',
    Error: 'Error on bind data:'
};

///#source 1 1 /App/common/directives/UserPicker/UserPickerController.js
(function () {
    "use strict";
    function UserPickerController($scope, $modal, UserPickerService) {
        var vm = this,
            getSelectedUsers = function (selectedUsers) {
                var result = UserPickerService.constructUserList(selectedUsers);
                $scope.lnSelectedList = result.join(); //Outer scope context
            };
        vm.assign = function () {
            var modalInstance = $modal.open({
                templateUrl: '/app/common/directives/UserPicker/UserPickerDialogView.html',
                controller: 'UserPickerDialogController',
                controllerAs: 'UserPickerDialogController',
                resolve: {
                    previousSelectionUserList: function () {
                        return vm.userList;
                    },
                    treeViewType: function () {
                        return $scope.userPickerType;
                    }
                },
                size: 'smx'
            });

            modalInstance.result.then(function (returnValue) {
                vm.userList = returnValue;
                getSelectedUsers(vm.userList);
            }, function () {
                vm.userList = {};
            });
        };

        vm.unassign = function () {
            if (vm.currentSelectedNodes.length > 0) {
                var result = UserPickerService.removeUsersFromUserList(vm.userList, vm.currentSelectedNodes);
                $scope.lnSelectedList = result.join(); //Outer scope context
            }
        };
        function init() {
            vm.localized = UserPickerResources;
        }
        init();
    }
    angular.module('app')
        .controller('UserPickerController', UserPickerController);
    UserPickerController.$inject = ['$scope', '$modal', 'UserPickerService'];
}());
///#source 1 1 /App/common/directives/UserPicker/UserPickerDialogController.js
(function () {
    "use strict";

    function UserPickerDialogController($modalInstance, UserPickerService, UserPickerFilterModel,
        previousSelectionUserList, treeViewType) {
        var vm = this,
            selectedUser = [],
            typeGroup = "group",
            typeUser = "user",
            typeUserAndUserGroup = "usersandgroup",
            treeTextField = "UserName",
            treeSubNode = "Users",
            previousSelectedUser = previousSelectionUserList;
        function getData(organizationId) {

            if (treeViewType === typeGroup) {
                vm.title = vm.localized.TitleUserGroups;
                return UserPickerService.getUserGroups(organizationId);
            }

            if (treeViewType === typeUser) {
                vm.title = vm.localized.TitleUsers;
                return UserPickerService.getUser(organizationId);
            }

            if (treeViewType === typeUserAndUserGroup) {
                vm.title = vm.localized.TitleUserGroupsUsers;
                return UserPickerService.getUserGroupsWithUsers(organizationId);
            }
        }
        function getDatasourceResult(organizationId) {

            var dataResult = new kendo.data.HierarchicalDataSource({
                transport: {
                    read: function (options) {
                        getData(organizationId)
                            .success(function (response) {
                                var responseData = UserPickerService.constructAndSetStateOnData(response, previousSelectionUserList);
                                options.success(responseData);
                            })
                            .error(function () {
                                //TODO : Exception handling
                            });
                    }
                },
                schema: {
                    model: {
                        children: treeSubNode,
                        expanded: true
                    }
                }
            });
            return dataResult;
        }
        function configureTree(organizationId) {
            //Configure Data
            vm.treeOptions = {
                loadOnDemand: false,
                dataTextField: [treeTextField],
                checkboxes: {
                    template: "<input type='checkbox' ng-checked='dataItem.checked' " +
                        "ng-model='dataItem.checked' ng-click='UserPickerDialogController.onCheck(dataItem)'/>",
                    checkChildren: true
                }
            };
            //Set Data source
            vm.treeData = getDatasourceResult(organizationId);
        }
        vm.onCheck = function (dataItem) {
            if (!angular.isUndefined(dataItem.Users)) { //root selection
                selectedUser = UserPickerService.addOrRemoveAllUser(dataItem, selectedUser);
            } else { //Signle selection 
                selectedUser = UserPickerService.addOrRemoveUser(dataItem, selectedUser);
            }
        };
        vm.ok = function () {
            $modalInstance.close(selectedUser);
        };
        vm.cancel = function () {

            $modalInstance.dismiss('cancel');
        };

        function getDatasourceFilterResult(organizationId, filterInfo) {

            var dataResult = new kendo.data.HierarchicalDataSource({
                transport: {
                    read: function (options) {
                        UserPickerService.postFilterUserGroupsAndUsers(organizationId,
                                filterInfo)
                            .success(function (response) {
                                var responseData = UserPickerService.constructAndSetStateOnData(response, previousSelectionUserList);
                                options.success(responseData);
                            })
                            .error(function () {
                                //TODO : Exception handling
                            });
                    }
                },
                schema: {
                    model: {
                        children: treeSubNode,
                        expanded: true
                    }
                }
            });
            return dataResult;
        }
        vm.reset = function () {

            if (treeViewType === typeUserAndUserGroup) {
                var organizationId = 2; // Todo: Need to come from context
                //Set Data source
                vm.treeData = getDatasourceResult(organizationId);

            } else { //Default Filters
                vm.treeData.filter({});
            }
            vm.filterText = '';
        };
        vm.filternode = function () {
            if (treeViewType === typeUserAndUserGroup) { //Custom Filter
                vm.filterInfo = new UserPickerFilterModel();
                vm.filterInfo.filterText = vm.filterText;
                vm.treeData = getDatasourceFilterResult(2, vm.filterInfo);

            } else { //Default Filters
                vm.treeData.filter({
                    field: treeTextField,
                    operator: "contains",
                    value: vm.filterText
                });
            }
        };
        function init() {
            var organizationId = 2; // TODO: Need to come from context
            vm.localized = UserPickerResources;
            if (!angular.isUndefined(previousSelectedUser)) {
                selectedUser = previousSelectedUser;
            }
            configureTree(organizationId);
        }
        init();
    }
    angular.module('app')
        .controller('UserPickerDialogController', UserPickerDialogController);
    UserPickerDialogController.$inject = ['$modalInstance', 'UserPickerService',
        'UserPickerFilterModel', 'previousSelectionUserList', 'treeViewType'];
}());
///#source 1 1 /App/common/directives/UserPicker/UserPickerDirective.js
(function () {
    "use strict";
    angular.module("app")
        .directive('lnUserPicker', function () {
            return {
                restrict: 'EA',
                scope: {
                    userPickerType: '@lnUserPickerType',
                    lnSelectedList: '='
                },
                controller: 'UserPickerController',
                controllerAs: 'UserPickerController',
                replace: true,
                templateUrl: '/app/common/directives/UserPicker/UserPickerView.html'
            };
        });
}());

///#source 1 1 /App/common/directives/UserPicker/UserPickerFilterModel.js
(function () {
    "use strict";
    angular.module('app')
        .factory('UserPickerFilterModel', function () {
            var UserPickerFilterModel = function () {
                this.filterText = '';
            };
            return UserPickerFilterModel;
        });


}());

///#source 1 1 /App/common/directives/UserPicker/UserPickerService.js
(function () {
    "use strict";

    function UserPickerService(WebApiClientService) {

        function getUserGroupsWithUsers(organizationId) {
            return WebApiClientService.get('/api/userpicker/usergroupsandusers/' + organizationId);
        }

        function getUserGroups(organizationId) {
            return WebApiClientService.get('/api/userpicker/usergroups/' + organizationId);
        }

        function getUser(organizationId) {
            return WebApiClientService.get('/api/userpicker/users/' + organizationId);
        }

        function postFilterUserGroupsAndUsers(organizationId, filter) {
            return WebApiClientService.post('/api/userpicker/usergroupswithusers/filter/' + organizationId, filter);
        }

        /* Start : UserPickerController - Helper method  */
        function constructUserList(users) {
            var result = [],
                len,
                index,
                total,
                subTotal;
            if (users.length > 0) {
                total = users.length;
                for (len = 0; len < total; len = len + 1) {
                    result.push(users[len].UserName);
                    if (!angular.isUndefined(users[len].Users) &&
                            users[len].Users !== null) {
                        subTotal = users[len].Users.length;
                        for (index = 0; index < subTotal; index = index + 1) {
                            result.push(users[len].Users[index].UserName);
                        }
                    }
                }
            }
            return result;
        }
        function removeUsersFromUserList(userList, selectedUsers) {

            var users = userList,
                ulen,
                len,
                total = users.length,
                removeCount = selectedUsers.length;
            for (ulen = 0; ulen < removeCount; ulen = ulen + 1) {
                for (len = 0; len < total; len = len + 1) {

                    if (users[len].UserId === selectedUsers[ulen].UserId) {
                        userList.splice(len, 1);
                        break;
                    }
                }
            }
            return userList;
        }
        /* End : UserPickerController - Helper method  */

        /* Start : UserPickerDialogController - Helper method  */
        function constructAndSetStateOnData(response, selectedUsers) {
            var responseData = response,
                len,
                index,
                dataCount,
                num,
                sIndex,
                ulen,
                parent,
                hasChild,
                count,
                dataLength,
                sUserLen,
                pUserLen;
            //Set Group Id for sub users.
            for (len = 0; len < responseData.length; len = len + 1) {
                parent = responseData[len];
                hasChild = (!angular.isUndefined(parent.Users));
                if (hasChild) {
                    count = parent.Users.length;
                    for (index = 0; index < count; index = index + 1) {
                        parent.Users[index].Groupid = parent.UserId;
                    }
                }
            }
            //For Retain previous selection
            if (!angular.isUndefined(selectedUsers)) {

                for (dataCount = 0; dataCount < responseData.length; dataCount = dataCount + 1) {
                    //Root
                    dataLength = selectedUsers.length;
                    for (num = 0; num < dataLength; num = num + 1) {
                        if (responseData[dataCount].UserId === selectedUsers[num].UserId) {
                            responseData[dataCount].checked = true;
                            break;
                        }
                    }

                    //Sub users
                    if (!angular.isUndefined(responseData[dataCount].Users) &&
                            responseData[dataCount].Users.length > 0) {
                        sUserLen = responseData[dataCount].Users.length;
                        for (sIndex = 0; sIndex <
                                sUserLen; sIndex = sIndex + 1) {
                            pUserLen = selectedUsers.length;
                            for (ulen = 0; ulen < pUserLen; ulen = ulen + 1) {
                                if (responseData[dataCount].Users[sIndex].UserId === selectedUsers[ulen].UserId) {
                                    responseData[dataCount].Users[sIndex].checked = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return responseData;
        }
        function addOrRemoveAllUser(dataItem, selectedUsersList) {
            var rootuser = [],
                index,
                ulen,
                len,
                uIndex,
                usersList,
                slen,
                subuser,
                userExists,
                total;
            if (dataItem.checked) {
                rootuser.UserId = dataItem.UserId;
                rootuser.UserName = dataItem.UserName;
                rootuser.isGroup = true;
                selectedUsersList.push(rootuser);
                usersList = dataItem.Users;
                total = usersList.length;
                for (index = 0; index < total; index = index + 1) {
                    userExists = false;
                    subuser = [];
                    subuser.UserId = usersList[index].UserId;
                    subuser.UserName = usersList[index].UserName;
                    subuser.Groupid = dataItem.UserId;
                    slen = selectedUsersList.length;
                    if (slen > 0) {
                        for (ulen = 0; ulen < slen; ulen = ulen + 1) {

                            if (selectedUsersList[ulen].UserId === subuser.UserId) {
                                userExists = true;
                                break;
                            }
                        }
                    }
                    if (!userExists) {
                        selectedUsersList.push(subuser);
                    }
                }
            } else { //Un Select
                slen = selectedUsersList.length;
                for (len = 0; len < slen; len = len + 1) {

                    if (selectedUsersList[len].UserId === dataItem.UserId) {
                        selectedUsersList.splice(len, 1);
                        break;
                    }
                }
                for (uIndex = 0; uIndex <= slen; 0) {
                    if (selectedUsersList[uIndex].Groupid === dataItem.UserId) {
                        selectedUsersList.splice(uIndex, 1);
                        if (uIndex > 0) {
                            uIndex = uIndex - 1;
                        }
                    } else {
                        uIndex = uIndex + 1;
                    }
                }
            }
            return selectedUsersList;
        }
        function addOrRemoveUser(dataItem, selectedUsersList) {
            var len,
                index,
                user = [],
                total;
            if (dataItem.checked) {
                user.UserId = dataItem.UserId;
                user.UserName = dataItem.UserName;
                user.Groupid = dataItem.Groupid;
                selectedUsersList.push(user);
            } else {
                total = selectedUsersList.length;
                //Unselect selection
                for (len = 0; len < total; len = len + 1) {
                    if (selectedUsersList[len].UserId === dataItem.UserId) {
                        selectedUsersList.splice(len, 1);
                        break;
                    }
                }
                //Unselect root node also
                for (index = 0; index < total; index = index + 1) {

                    if (selectedUsersList[index].UserId === dataItem.Groupid) {
                        selectedUsersList.splice(index, 1);
                        break;
                    }
                }
            }
            return selectedUsersList;
        }
        /* End : UserPickerDialogController - Helper method  */
        var service = {
            getUserGroupsWithUsers: getUserGroupsWithUsers,
            getUserGroups: getUserGroups,
            getUser: getUser,
            postFilterUserGroupsAndUsers: postFilterUserGroupsAndUsers,
            constructUserList: constructUserList,
            removeUsersFromUserList: removeUsersFromUserList,
            constructAndSetStateOnData: constructAndSetStateOnData,
            addOrRemoveAllUser: addOrRemoveAllUser,
            addOrRemoveUser: addOrRemoveUser
        };
        return service;
    }
    angular.module('app')
        .factory('UserPickerService', UserPickerService);
    UserPickerService.$inject = ['WebApiClientService'];
}());
