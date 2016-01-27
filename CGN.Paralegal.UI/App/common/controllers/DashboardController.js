(function () {
    "use strict";

    var DashboardController = function (DashboardService, AppStateService, WorkflowStateService, AnalysisSetService, Utils, Constants, $interval, $rootScope, $scope) {
       var vm = this,
            
           /* MODULES = Constants.getModuleLabels(),
            STATES = Constants.getStateLabels(),
            ACTIONS = Constants.getActionLabels(),
            STATUS = Constants.getStatusLabels(),*/



            getDashboardDetails = function(config) {
                config = config || {};
                DashboardService.getDashboardDetails(config).then(function(data) {
                    data.CodingValues = data.CodingValues || ["Relevant", "Not Relevant"];
                    data.CreatedDate = data.CreatedDate ? new Date(data.CreatedDate) : "";
                    vm.dashboardDetails = data;
                    var currentState = WorkflowStateService.getCurrentWorkflowState(vm.workflowStates);
                    if (currentState != null && (currentState.Name === STATES.TRAININGSET ||
                        currentState.Name === STATES.PREDICTSET)) {
                        vm.activeTab = "trainingset";
                    } else if (currentState != null && currentState.Name === STATES.QCSET) {
                        vm.activeTab = "verificationset";
                    } else {
                        vm.activeTab = "controlset";
                    }
                    enableTabs();
                });
            },
            enableTabs = function() {
                if (vm.activeTab == 'controlset') {
                    loadControlSetView(vm.workflowStates);
                } else if (vm.activeTab == 'trainingset' && vm.enableTrainingSet()) {
                    loadTrainingSetView(vm.workflowStates);
                } else if (vm.activeTab == 'verificationset' && vm.enableVerificationSet()) {
                    loadVerificationSetView(vm.workflowStates);
                }
            },
            getWorkflowStates = function(config) {
                config = config || {};
                WorkflowStateService.getWorkflowStates(MODULES.ADMIN, config).then(function(result) {
                    vm.workflowStates = result;
                    getDashboardDetails(config);
                    showPredictControlset();
                });
            },

            getTopTenAOP = function () {
                    DashboardService.getTopTenAOP().then(function (data) {
                    return vm.topTenAOP = data;
                })
            },
            getTopTenCity = function () {
                DashboardService.getTopTenCity().then(function (data) {
                    return vm.topTenCity = data;
                })
            },
            getTopTenParaLegal = function () {
                DashboardService.getTopTenParaLegal().then(function (data) {
                    return vm.topTenParaLegal = data;
                })
            },
            showPredictControlset = function() {
                AnalysisSetService.validateCategorizeControlSetJob().then(function(response) {
                    vm.showPredictControlsetMenu = WorkflowStateService.isActionInProgress(vm.workflowStates, STATES.TRAININGSET, ACTIONS.REVIEW) && response;
                });
            },
            loadControlSetView = function(workflowStates) {
                $rootScope.$broadcast('LoadControlSet', workflowStates);
            },
            loadTrainingSetView = function(workflowStates) {
                $rootScope.$broadcast('LoadTrainingSet', workflowStates);
            },
            loadVerificationSetView = function(workflowStates) {
                $rootScope.$broadcast('LoadVerificationSet', workflowStates);
            },

            //SignalR auto-refresh
            hubConnection = null,
            workflowStateHubProxy = null,
            initSignalR = function () {
                hubConnection = $.hubConnection();

                hubConnection.logging = true;

                hubConnection.error(function (error) {
                    console.log('SignalR error: ' + error);
                });

                workflowStateHubProxy = hubConnection.createHubProxy('workflowStateHub');

                // Define client-side methods that the server will call
                workflowStateHubProxy.on('updateWorkflowState', function(projectId, workflowStates) {
                    if (vm.dashboardDetails.Id === projectId) {
                        if (!angular.equals(workflowStates, vm.workflowStates)) {
                            vm.workflowStates = workflowStates;
                            var config = {
                                ignoreLoadingBar: true
                            };
                            getDashboardDetails(config);
                        }

                    }
                });

                // Start the connection
                hubConnection.start().done(function () {
                    // Call registerNotification on the server to notify me when workflow state changes
                    workflowStateHubProxy.invoke('registerNotification', AppStateService.appState()).done(function() {
                        console.log('registerNotification successful');
                    });
                });

            },

            init = function () {
                //getWorkflowStates();
                getTopTenAOP();
                getTopTenCity();
                getTopTenParaLegal();
                //initSignalR();
            };
       vm.goToSearchResult = function (search_term) {
           var searchQuery = search_term
           if (!angular.isUndefined(searchQuery)) {
               var PCUrl = "/app/analytics/approot#/search/" + searchQuery;
               var EVUrl = "/app/adminapp.aspx?mod=analytics&view=dashboard";
               Utils.loadPage(EVUrl, PCUrl);
           }
           else {
               window.alert("Please enter the search query")
           }

       };

       vm.goToProfile = function (paralegalid) {
           var paraLegal = paralegalid
           if (!angular.isUndefined(paraLegal)) {
               var PCUrl = "/app/analytics/approot#/paralegal/" + paraLegal;
               var EVUrl = "/app/adminapp.aspx?mod=analytics&view=dashboard";
               Utils.loadPage(EVUrl, PCUrl);
           }
       };

        vm.activeTab = "controlset";
        vm.tabClick = function (name) {
            if (vm.activeTab === name) return false;
            if (name == "controlset" && vm.enableControlSet()) {
                vm.activeTab = "controlset";
            }
            else if (name == "trainingset" && vm.enableTrainingSet()) {
                vm.activeTab = "trainingset";
            }
            else if (name == "verificationset" && vm.enableVerificationSet()) {
                vm.activeTab = "verificationset";
            } else {
                return false;
            }
            enableTabs();
            return false;
        };

        
        vm.isActiveTab = function (name) {
            return vm.activeTab == name;
        };

       // vm.localized = Constants.getProjectResources();
        vm.isLoading = false;
        vm.popupDeleteProject = function () {
            vm.projectName = "";
            vm.projectNameError = false;
            vm.modals.delete = true;
        };

        vm.invalidProjectName = function () {
            var bool = (vm.projectName === null || angular.isUndefined(vm.projectName));
            bool = bool || (angular.isString(vm.projectName) && vm.projectName.length === 0);
            if (bool) {
                vm.projectNameError = false;
            }
            return bool;
        };

        vm.deleteProject = function () {
            if (vm.projectName === vm.dashboardDetails.Name) {
                vm.projectNameError = false;
                return DashboardService.deleteProject().then(function () {
                    var EVUrl = "/DatasetManagement/DatasetDashboard.aspx?folderId=" + vm.dashboardDetails.Id +
                        "&datasetId=" + vm.dashboardDetails.DatasetId + "&matterId" + vm.dashboardDetails.MatterId;
                    Utils.loadPage(EVUrl, "#/dashboard?ProjectDeleted");
                    return true;
                });
            } else {
                vm.projectNameError = true;
                return false;
            }
        };

        vm.categorizeControlSet = function (config) {
            config = config || {};
            AnalysisSetService.manualCategorizeControlSet(config).then(function () {
                vm.showPredictControlsetMenu = false;
            });
        };

        vm.modals = Utils.modals;
        vm.enableControlSet = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.PROJECTSETUP, ACTIONS.CREATE);
        };

        vm.enableTrainingSet = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.TRAININGSET, ACTIONS.CREATE);
        };

        vm.enableVerificationSet = function () {
            return WorkflowStateService.isActionCompleted(vm.workflowStates, STATES.QCSET, ACTIONS.CREATE);
        };

        vm.showAddDocumentMenu = true;
        vm.addDocument = function () {
          var Url = "/wizard/add";
            Utils.loadView(Url);
        };
        vm.viewAllDocs = function () {
            var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/alldocuments/1/0/from_admin/to_doclist/All";
            var PCUrl = "/app/review/approot#/analysissets/alldocuments/1/0/from_admin/to_doclist/All";
            Utils.loadPage(EVUrl, PCUrl);
        };

        init();
    };

    angular.module("app").controller("DashboardController", DashboardController);
    DashboardController.$inject = ["DashboardService","AnalysisSetService", "Utils", "Constants", "$interval", "$scope"];

}());