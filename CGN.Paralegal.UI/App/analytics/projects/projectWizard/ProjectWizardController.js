(function () {
    "use strict";

    var ProjectWizardController = function ($scope, DashboardService,Utils,Constants) {
        var vm = this, init, getDocumentCount, validateName, validatePrefix, isAddAdditionalDocuments = false;

        vm.localized = Constants.getProjectResources();
        vm.documentCount = 0;
        vm.tabs = {
            ProjectSetup: false,
            SelectDocuments: false,
            ProjectFields: false,
            SamplingOptions: false,
            JobOptions: false,
            Summary: false
        };
        
        vm.invalid ={
            Name:false,
            FieldPrefix:false,
            Search:false,
            Count:false
        };
        
        vm.disabled = {
            ProjectSetup: true,
            SelectDocuments: true,
            ProjectFields: true,
            SamplingOptions: true,
            JobOptions: true,
            Summary: true
        };

        vm.hide = {
            ProjectSetup: false,
            SelectDocuments: false,
            ProjectFields: false,
            SamplingOptions: false,
            JobOptions: false,
            Summary: false
        };

        vm.completed = {
            ProjectSetup: false,
            SelectDocuments: false,
            ProjectFields: false,
            SamplingOptions: false,
            JobOptions: false,
            Summary: false
        };

        vm.showTab = function (tab) {
            angular.forEach(vm.tabs, function (value, key) {
                if (key === tab) {
                    vm.tabs[key] = true;
                    vm.disabled[key] = false;
                } else {
                    vm.tabs[key] = false;
                    vm.disabled[key] = !(vm.completed[key]);
                }
            });
        };

        vm.project={
            Name:"",
            Description:"",
            FieldPrefix:"PC_",
            DocumentSource: null,
            IsAddAdditionalDocuments: false
        };

        vm.selectedOption = null;

        vm.documentOptions = {
            allDocuments : { option: "all", value: "all", label:vm.localized.AllDatasetDocuments, disable:false},
            tag: { option: "tag", value: "", label: "", disable: false },
            savedSearch: { option: "savedSearch", value: "", label: "", disable: false },
            query: { option: "query", value: "", label: "", disable: false }
        };

        vm.tags = [];

        vm.savedSearches = [];

        vm.tagOptions = {};

        vm.searchOptions = {};

        vm.isSelectedOption = function (opt) {
            return opt === vm.selectedOption.option;
        };

        vm.getLabel = function () {
            return vm.selectedOption.label;
        };

        vm.getAllDocumentCount = function () {
            vm.selectedOption = vm.documentOptions.allDocuments;

            getDocumentCount(vm.selectedOption);
        };
        vm.resetDocumentCount = function () {
            vm.documentCount = parseInt(0, 10);
            vm.noDocumentsFound = (vm.documentCount === 0);
            vm.invalid.Count= false;
        };
        vm.search = function () {
            if (vm.selectedOption.value === "" || vm.selectedOption.value === null) {
                vm.invalid.Search = true;
                vm.resetDocumentCount();
                vm.selectedOption.label = "";
            } else {
                vm.invalid.Search = false;
                vm.selectedOption.label = "'" + vm.selectedOption.value + "'";
                getDocumentCount(vm.selectedOption);
            }
        };
        vm.enter = function ($event) {
            var whichKey;
            whichKey = $event.keyCode;
            if (whichKey === 13) {
                vm.search();
            }
        };

        validateName = function(str){
            vm.invalid.Name = false;
            if(str.length === 0){
                vm.invalid.Name = true;
                vm.ProjectNameErrorMessage =vm.localized.ErrorNameRequired;
                return;
            }
            if(str.length>40){
                vm.invalid.Name = true;
                vm.ProjectNameErrorMessage =vm.localized.ErrorNameMaxChars;
                return;
            }
            if(str.match(/^(?=^[0-9a-zA-Z])([a-zA-Z0-9-_,. ]{1,40})$/)===null){
                vm.invalid.Name = true;
                vm.ProjectNameErrorMessage =vm.localized.ErrorNameInvalid;
                return;
            }
            return;
        };

        validatePrefix = function(str){
            vm.invalid.FieldPrefix = false;
            if(str.length === 0){
                vm.invalid.FieldPrefix=true;
                vm.FieldPrefixErrorMessage =vm.localized.ErrorPrefixRequired;
                return;
            }
            if(str.length>6){
                vm.invalid.FieldPrefix=true;
                vm.FieldPrefixErrorMessage =vm.localized.ErrorPrefixMaxChars;
                return;
            }
            if(str.match(/^(?=^[0-9a-zA-Z])([a-zA-Z0-9-_,. ]{1,6})$/)===null){
                vm.invalid.FieldPrefix=true;
                vm.FieldPrefixErrorMessage =vm.localized.ErrorPrefixInvalid;
                return;
            }
            return;
        };

        vm.validateSetup = function(){
            var name=vm.project.Name,
                prefix=vm.project.FieldPrefix;
            
            validateName(name);
            validatePrefix(prefix);

            if(!(vm.invalid.Name || vm.invalid.FieldPrefix)){

                var postData ={
                    "Name":vm.project.Name,
                    "FieldPrefix":vm.project.FieldPrefix
                };
                DashboardService.validateProjectInfo(postData).then(function(response){
                    if(!(response.IsValidProjectName && response.IsValidFieldPrefix )){
                        vm.invalid.Name = !response.IsValidProjectName;
                        vm.invalid.FieldPrefix = !response.IsValidFieldPrefix;
                        if(vm.invalid.Name){
                            vm.ProjectNameErrorMessage =vm.localized.ErrorNameUnique;
                        }
                        if(vm.invalid.FieldPrefix){
                            vm.FieldPrefixErrorMessage =vm.localized.ErrorPrefixUnique;
                        }
                    }else{
                        vm.completed.ProjectSetup = true;
                        vm.showTab("SelectDocuments");
                    }
                });
            }

        };

        vm.cancel = function () {
            Utils.loadPage("/SystemManagement/SystemDashboard.aspx?PageView=Admin", "approot#/dashboard");
        };

        vm.addDocumentCancel = function() {
           var Url = "/dashboard";
            Utils.loadView(Url);
        };

        vm.createProject = function () {
            var docSource = {
                SelectedMode: 0,
                CollectionId:null,
                TagId: null,
                SavedSearchId: null,
                SearchText: null
            };
            switch (vm.selectedOption.option) {
                case "all":
                    docSource.SelectedMode = 0;
                    break;
                case "tag":
                    docSource.SelectedMode = 1;
                    docSource.TagId = vm.selectedOption.value;
                    break;
                case "savedSearch":
                    docSource.SelectedMode = 2;
                    docSource.SavedSearchId = vm.selectedOption.value;
                    break;
                case "query":
                    docSource.SelectedMode = 3;
                    docSource.SearchText = vm.selectedOption.value;
            }
            vm.project.DocumentSource = docSource;
            vm.project.IsAddAdditionalDocuments = isAddAdditionalDocuments;
            if (vm.hide.ProjectSetup === true) {
                DashboardService.createProject(vm.project).then(function (result) {
                    var Url = "/dashboard";
                    Utils.loadView(Url);
                });
            } else {
                DashboardService.createProject(vm.project).then(function (result) {
                    var EVUrl = "/App/AdminApp.aspx?mod=analytics&view=dashboard&folderId=" + result.Id;
                    Utils.loadPage(EVUrl, "#/dashboard");
                });
            }
        };

        getDocumentCount = function (params) {
            var postData = { type: "", tag: "", savedsearchId: 0, query: "" };
            postData.type = params.option.toLowerCase();

            switch (params.option) {
                case "tag":
                    postData.tag = params.value;
                    break;
                case "savedSearch":
                    postData.savedsearchId = params.value;
                    break;
                case "query":
                    postData.query = params.label;
            }


            DashboardService.getDocumentCount(postData).then(function (result) {
                vm.documentCount = parseInt(result, 10);
                vm.noDocumentsFound = (vm.documentCount === 0);
                vm.invalid.Count = true;
            });
        };

        init = function () {
            vm.tabs.ProjectSetup = true;
            vm.disabled.ProjectSetup = false;
            vm.invalid.Count= false;
            vm.documentCount = parseInt(0, 10);
            vm.noDocumentsFound = (vm.documentCount === 0);
            vm.getAllDocumentCount();
            if (Utils.getRouteParam("mode") === "add") {
                isAddAdditionalDocuments = true;
                vm.hide.ProjectSetup = true;
                vm.showTab("SelectDocuments");
            }
            DashboardService.getSavedSearches().then(function (result) {
                if (result.length && result.length > 0) {
                    vm.savedSearches = result;
                    vm.searchOptions = {
                        dataSource: result,
                        dataTextField: "SavedSearchName",
                        dataValueField: "SavedSearchId",
                        optionLabel: vm.localized.SelectSavedSearch,
                        select: function (e) {
                            if (e.item.index() > 0) {
                                var dataItem = this.dataItem(e.item.index());
                                vm.selectedOption.value = dataItem.SavedSearchId;
                                vm.selectedOption.label = dataItem.SavedSearchName;

                                getDocumentCount(vm.selectedOption);
                                
                            } else {
                                vm.resetDocumentCount();
                                vm.selectedOption.label = "";
                            }
                            $scope.$apply();
                        }
                    };
                } else {
                    vm.documentOptions.savedSearch.disable = true;
                }
                
            });

            DashboardService.getTags().then(function (result) {
                if (result.length && result.length > 0) {
                    vm.tags = result;
                    vm.tagOptions = {
                        dataSource: result,
                        dataTextField: "m_TagDisplayName",
                        dataValueField: "m_Id",
                        optionLabel: vm.localized.SelectTag,
                        select: function (e) {
                            if (e.item.index() > 0) {
                                var dataItem = this.dataItem(e.item.index());
                                vm.selectedOption.value = dataItem.m_Id;
                                vm.selectedOption.label = dataItem.m_TagDisplayName;
                                getDocumentCount(vm.selectedOption);
                            } else {
                                vm.resetDocumentCount();
                                vm.selectedOption.label = "";
                            }
                            $scope.$apply();
                        }
                    };
                } else {
                    vm.documentOptions.tag.disable = true;
                }
            });

        };

        init();

    };

    angular.module("app").controller("ProjectWizardController", ProjectWizardController);
    ProjectWizardController.$inject = ["$scope", "DashboardService", "Utils", "Constants"];

}());