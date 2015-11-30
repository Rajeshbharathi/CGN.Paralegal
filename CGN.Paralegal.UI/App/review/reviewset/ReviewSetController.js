(function () {
    "use strict";

    var ReviewSetController, app;

    ReviewSetController = function(AnalysisSetService,DocumentService, AppStateService, Utils,Constants) {
        var vm,
            isDocView = Utils.getRouteParam("to_view").toLowerCase().indexOf("doclist") === -1,
            isAdminModule = Utils.getRouteParam("from_module").toLowerCase().indexOf("admin")!== -1,
            setType = Utils.getAnalysisSetType(Utils.getRouteParam("setType")),
            setId = Utils.getRouteParam("setId"),
            setRoundNumber = Utils.getRouteParam("setRound");

        vm = this;
        vm.localized = Constants.getCommonResources();
        
        vm.clearAll = false;

        var appState = AppStateService.appState();

        vm.reviewerConfiguration = {
            service: "DocumentService",
            relevance: {
                NONE: "Not_Coded",
                NOTRELEVANT: "Not_Relevant",
                RELEVANT: "Relevant",
                SKIPPED: "Skipped"
            },
            widgets: {
                enabled: true,
                list: ["assistedReview"]
            },
            docView: false,
            listView: false,
            selectedDocument: {
                DocSequenceId: null,
                DocReferenceId: null
            },
            currentSetBinderId: setId,
            currentSetRound: setRoundNumber,
            currentSetArrayIndex:0,
            documentQueryContext: {
                "OrgId": appState.OrgId,
                "MatterId": appState.MatterId,
                "DatasetId": appState.DatasetId,
                "ProjectId": appState.ProjectId,
                "KeyWord": "",
                "ExportFilters": [],
                "Filters": [],
                "Sort": [],
                "AnalysisSet": vm.currentAnalysisSet,
                "PageIndex": 1,
                "PageSize": 500,
                "TotalDocuments": 0
            },
            docListIdMapper: {}
        };

        vm.columnNameMapper= {};
        vm.columns = [
            { field: "Row", title: "Row", hidden: false, filterable: false,sortable: false },
            { field: "", title: "", hidden: false },
            { field: "", title: "", hidden: false },
            { field: "", title: "", hidden: false },
            { field: "", title: "", hidden: false },
            { field: "", title: "", hidden: false },
            { field: "", title: "", hidden: false },
            { field: "", title: "", hidden: false }
        ];

        vm.searchQuery = "";

        vm.availableAnalysisSets = [];

        vm.currentAnalysisSet = {};
		vm.isAnalysisSetChanged=false;
        vm.changeAnalysisSet = function () {
			vm.isAnalysisSetChanged=true;
            vm.clear();
        };
        vm.bindDropDownList = function (e) {
            e.sender.select(vm.reviewerConfiguration.currentSetArrayIndex);
        };

        vm.selectSet = function (e) {
            var index = e.item.index();
            vm.currentAnalysisSet = vm.availableAnalysisSets[index];
            vm.reviewerConfiguration.currentSetArrayIndex = index;
            vm.reviewerConfiguration.currentSetRound = vm.availableAnalysisSets[index].CurrentRound;
            vm.reviewerConfiguration.currentSetBinderId = vm.availableAnalysisSets[index].BinderId;
            vm.reviewerConfiguration.documentQueryContext.AnalysisSet = vm.availableAnalysisSets[index];
        };
        vm.docList = {
            Documents: [],
            Total: 0
        };

        vm.defaultFilter = {
            logic: "and",
            filters:[]
        };
        vm.defaultSort =[];

        function getDoclist() {
            DocumentService.getDocumentlist(vm.reviewerConfiguration.documentQueryContext, {}).then(function(data) {
                vm.docList = data;

                angular.forEach(data.Documents, function(obj) {
                    vm.reviewerConfiguration.docListIdMapper[obj.Id] = obj.ReferenceId;
                });
                
                if(!angular.isUndefined(data.Documents) && data.Documents.length>0){
                    angular.forEach(data.Documents[0].Fields, function(obj,idx) {
                        var fieldName = obj.DisplayName;
                        vm.columnNameMapper[fieldName.split(" ").join("")] = obj.Name;

                        vm.columns[idx+1].field = fieldName.split(" ").join(""); 
                        vm.columns[idx+1].title = obj.DisplayName;
                    });
                }
                
                vm.reviewerConfiguration.documentQueryContext.TotalDocuments = data.Total;
                vm.reviewerConfiguration.docView = isDocView;
                vm.reviewerConfiguration.listView = !isDocView;
                
                vm.defaultFilter.filters = Utils.getDefaultGridFiltersArray(Utils.getRouteParam("filterValue"));

                if (vm.reviewerConfiguration.documentQueryContext.KeyWord.length === 0) {
                    vm.reviewerConfiguration.documentQueryContext.KeyWord = vm.searchQuery =
                        Utils.getDefaultGridSearchKeyWord(Utils.getRouteParam("filterValue"),
                        vm.columnNameMapper["ReviewerCategory"],
                        vm.columnNameMapper["PredictedCategory"]);
                }

                vm.gridData = {
                    dataSource: {
                        transport: {
                            read: buildDataSource,
                            parameterMap: function(options) {
                                return JSON.stringify(options);
                            }
                        },
                        schema: {
                            total: function() {
                                return vm.docList.Total;
                            },
                            parse: function(data) {
                                var rows = [];
                                var docList = data.Documents || null;
                                if (docList !== null && !angular.isUndefined(docList) && angular.isArray(docList)) {
                                    for (var i = 0; i < docList.length; i+=1) {
                                        var row = {
                                            "DocReferenceId": docList[i].ReferenceId,
                                            "DocSequenceId": docList[i].Id,
                                            "Row": docList[i].Id
                                        };
                                        var fields = docList[i].Fields;
                                        var l = fields.length;

                                        for (var j = 0; j < l; j+=1) {
                                            var displayName = fields[j].DisplayName;
                                            row[displayName.split(" ").join("")] = fields[j].Value;
                                            row.Name = fields[j].Name;
                                            row.DisplayName = fields[j].DisplayName;
                                        }

                                        rows.push(row);
                                    }
                                    if (vm.reviewerConfiguration.selectedDocument.DocSequenceId == null && docList.length>0) {
                                        vm.reviewerConfiguration.selectedDocument.DocSequenceId = docList[0].Id;
                                        vm.reviewerConfiguration.selectedDocument.DocReferenceId = docList[0].ReferenceId;
                                    }
                                }
                                return rows;
                            }
                        },
                        serverFiltering: true,
                        serverPaging: true,
                        serverSorting: true,
                        filter: vm.defaultFilter,
                        sort:vm.defaultSort
                    },
                    height: 550,
                    scrollable: true,
                    resizable: true,
                    selectable: "row",
                    sortable: {
                        mode: "single",
                        allowUnsort: false
                    },
                    pageable: {
                        buttonCount: 5,
                        refresh: true,
                        pageSizes: [100, 200, 500, 1000],
                        pageSize: 500
                    },
                    filterable : {
                        operators : {
                            string: {
                                contains: "Contains"
                            }
                        },
                        extra: false
                    },
                    columnMenu: true,
                    columns: vm.columns,
                    reorderable: true,
                    dataBound: function () {
                        var rowId = vm.reviewerConfiguration.selectedDocument.DocSequenceId;
                        if (rowId > this.dataSource.pageSize()) {
                            rowId = (rowId % this.dataSource.pageSize()) - 1;
                        } else if (rowId > 0) {
                            rowId--;
                        }

                        this.tbody.find("tr").eq(rowId).addClass("k-state-selected");
                        var row = $("tbody>tr:eq(" + rowId + ")");
                        if (!angular.isUndefined(row.offset())) {
                            this.content.scrollTop(0);
                            this.content.scrollTop(row.offset().top - this.content.offset().top);
                        }
                        this.select("tbody>tr:eq(" + rowId + ")");
                        if (vm.reviewerConfiguration.documentQueryContext.UpdatedPageIndex) {
                            var filtersObj = this.dataSource.options.filter;
                            var sortArr = this.dataSource.options.sort;
                            this.dataSource.query({
                                "page": vm.reviewerConfiguration.documentQueryContext.UpdatedPageIndex,
                                "pageSize": vm.reviewerConfiguration.documentQueryContext.PageSize,
                                "filter":filtersObj,
                                "sort": sortArr
                            });
                            delete vm.reviewerConfiguration.documentQueryContext.UpdatedPageIndex;
                        }
                        if (vm.defaultSort.length > 0) {
                            var grid = this;
                            grid.dataSource.sort(vm.defaultSort);
                            vm.defaultSort = [];
                        }
                        
                    },
                    change: function () {
                        var grid = this;
                      
                        var selectedRow = grid.select();                     
                        var dataItem = this.dataItem(selectedRow);
                        vm.reviewerConfiguration.selectedDocument.DocSequenceId = dataItem.DocSequenceId;
                        vm.reviewerConfiguration.selectedDocument.DocReferenceId = dataItem.DocReferenceId;

                    }
                };
            });

        }

        function updateIfFound(obj) {
            /*jshint validthis: true */
            var self = this;
            if (obj.Name === self.Name) {
                var value = self.Value ? self.Value : self.Order;
                obj.Value = value;
                return true;
            }
        }

        function buildDataSource(options) {

            options = options || null;
            var filtersArray = [],
                sortArray = [];

            if (vm.clearAll) {
                options.data.filter = null;
                options.data.sort = null;
                filtersArray =[];
                sortArray=[];
            }

            if (options !== null) {
                
                if (options.data.filter !== null && !angular.isUndefined(options.data.filter)) {

                    //map grid filters to query context
                    angular.forEach(options.data.filter.filters, function(obj) {
                        var columnName = vm.columnNameMapper[obj.field];
                        var filterObj = { "Name": columnName, "Value": obj.value, "Field":obj.field };
                        var found = filtersArray.some(updateIfFound, filterObj);
                        if (!found) {
                            filtersArray.push(filterObj);
                        }
                    });
                }


                if (options.data.sort !== null && !angular.isUndefined(options.data.sort)) {
                    
                    //map grid sort to query context

                    angular.forEach(options.data.sort, function(obj) {
                        var columnName = vm.columnNameMapper[obj.field];
                        var order = (obj.dir === "asc" ? "Ascending": "Descending");
                        var sortObj = { "Name": columnName, "Order": order ,"Field":obj.field};
                        var found = sortArray.some(updateIfFound, sortObj);
                        if (!found) {
                            sortArray.push(sortObj);
                        }
                    });
                }
                vm.reviewerConfiguration.documentQueryContext.PageIndex= options.data.page;
                vm.reviewerConfiguration.documentQueryContext.PageSize= options.data.pageSize;
            }
            vm.reviewerConfiguration.documentQueryContext.Filters = filtersArray;
            vm.reviewerConfiguration.documentQueryContext.Sort = sortArray;

            DocumentService.getDocumentlist(vm.reviewerConfiguration.documentQueryContext, {}).then(function (data) {
                vm.docList = data;
                vm.clearAll = !!vm.clearAll ? !vm.clearAll : vm.clearAll;
                
                vm.reviewerConfiguration.docListIdMapper = {};
                angular.forEach(data.Documents, function (obj) {
                    vm.reviewerConfiguration.docListIdMapper[obj.Id] = obj.ReferenceId;
                });
                
                vm.reviewerConfiguration.documentQueryContext.TotalDocuments = data.Total;
                
                
                options.success(data);
                
                
            }, function (err) {
                options.error(err);
            });
            
            
        }


        vm.search = function () {
		 if (vm.isAnalysisSetChanged === true)
            $("#grid").data("kendoGrid").dataSource.filter([]);
            vm.reviewerConfiguration.documentQueryContext.KeyWord = vm.searchQuery;
            $("#grid").data("kendoGrid").dataSource.read();
        };

        vm.searchOnKeyPress = function (e) {
            if (e.keyCode !== 13) {
                return false;
            }
            vm.search();
        };

        vm.clear = function () {
            vm.reviewerConfiguration.documentQueryContext.PageIndex = 1;
            vm.reviewerConfiguration.documentQueryContext.PageSize = 500;
            vm.reviewerConfiguration.documentQueryContext.Filters = [];
            vm.reviewerConfiguration.documentQueryContext.Sort = [];
            vm.reviewerConfiguration.documentQueryContext.KeyWord = "";
            vm.reviewerConfiguration.selectedDocument.DocSequenceId = 1;
            vm.searchQuery = "";
            vm.clearAll = true;
            $("#grid").data("kendoGrid").dataSource.filter([]);
            $("a.k-pager-refresh").trigger("click");
        };

        vm.export = function () {
            var postData = vm.reviewerConfiguration.documentQueryContext;
            postData.PageIndex = 1;
            postData.PageSize = vm.docList.Total;
            postData.ExportFilters = [];

            var grid = $("#grid").data("kendoGrid");
            angular.forEach(grid.columns, function (obj, idx) {
                if (!obj.hidden) {
                    postData.ExportFilters.push(obj.title);
                }
            });
            if (vm.docList.Total > 500) {
                DocumentService.scheduleExportJob(postData).then(function () {
                    vm.displayJobScheduledMessage = true;
                });
            } else {
                DocumentService.exportDocumentList(postData).then(function (response) {
                    Utils.exportCSV(response);
                });
            }
        };
        vm.hideExportMessage = function() {
            vm.displayJobScheduledMessage = false;
        };

        vm.goToReview = function() {
            
            //map query context to grid filters for next time
            var temp =[];
            if (vm.reviewerConfiguration.documentQueryContext.Filters.length > 0) {
                var storedFilters = vm.reviewerConfiguration.documentQueryContext.Filters;
                angular.forEach(storedFilters, function (obj) {
                    var sf = {"field":obj.Field,"operator":"contains","value":obj.Value};
                    temp.push(sf);
                    sf = null;
                });
                vm.defaultFilter.filters = temp;
                storedFilters = null;
            }

            if (vm.reviewerConfiguration.documentQueryContext.Sort.length > 0) {
                var storedSort = vm.reviewerConfiguration.documentQueryContext.Sort;
                temp = [];
                angular.forEach(storedSort, function (obj) {
                    var order = (obj.Order === "Ascending" ? "asc" : "desc");
                    var sortObj = { field: obj.Field, dir: order };
                    temp.push(sortObj);
                    sortObj = null;
                });
                vm.defaultSort = temp;
                storedSort = null;
            }
            temp = null;
            // map column states to context
            var grid = $("#grid").data("kendoGrid");
            angular.forEach(grid.columns, function(obj,idx) {
                vm.columns[idx].hidden = obj.hidden;
            });
            
         
          
            
            vm.reviewerConfiguration.docView = true;
            vm.reviewerConfiguration.listView = false;

        };
        vm.goToDashboard = function() {
            var EVUrl, PCUrl;
            if (!isAdminModule) {
                EVUrl = "/App/ReviewerApp.aspx?mod=review&view=dashboard";
                PCUrl = "/app/review/approot#/dashboard";
            } else {
                EVUrl = "/App/AdminApp.aspx?mod=analytics&view=dashboard";
                PCUrl = "/app/analytics/approot#/dashboard";
            }
            Utils.loadPage(EVUrl, PCUrl);
        };

        function getAvailableSets() {
            AnalysisSetService.getAvailableAnalysisSets().then(function (data) {
                var roundNumber = 1;
                angular.forEach(data, function (obj, idx) {
                    if (obj.Type.toUpperCase() === "TRAININGSET") {
                        obj.CurrentRound = roundNumber;
                        roundNumber += 1;
                    }
                    if (obj.Type.toUpperCase() === setType.toUpperCase()) {
                        if (vm.reviewerConfiguration.currentSetBinderId === "0") {
                            vm.currentAnalysisSet = obj;
                            vm.reviewerConfiguration.currentSetArrayIndex = idx;
                            vm.reviewerConfiguration.currentSetBinderId = obj.BinderId;
                            vm.reviewerConfiguration.documentQueryContext.AnalysisSet = obj;
                        } else {
                            if (obj.BinderId === setId) {
                                vm.currentAnalysisSet = obj;
                                vm.reviewerConfiguration.currentSetArrayIndex = idx;
                                vm.reviewerConfiguration.currentSetBinderId = obj.BinderId;
                                vm.reviewerConfiguration.documentQueryContext.AnalysisSet = obj;
                            }
                        }

                    }
                });
                vm.availableAnalysisSets = data;

                getDoclist();
            });
        }

        vm.docListTitle = function () {
            switch (vm.reviewerConfiguration.documentQueryContext.AnalysisSet.Type.toUpperCase()) {
                case ("CONTROLSET"):
                    return vm.localized.ControlSetDocuments;
                case ("TRAININGSET"):
                    return vm.localized.TrainingSetDocuments + " ( " + vm.localized.Set + " " +
                        vm.reviewerConfiguration.currentSetRound + " )";
                case ("QCSET"):
                    return vm.localized.QCSetDocuments+ " ( " +
                        vm.reviewerConfiguration.documentQueryContext.AnalysisSet.Name + " )";
                case ("PREDICTSET"):
                    return vm.localized.PredictionSetDocuments;
                case ("ALLDOCUMENTS"):
                    return vm.localized.AllDocuments;

            }
        };
        vm.showAutoCodeModal = false;
        vm.TruthSetFieldName = "Categories";
        vm.RelevantFieldValue = "";
        vm.hideAutoCodeModal = function () {
            vm.showAutoCodeModal = false;
        };
        vm.AutoCode = function () {
            vm.showAutoCodeModal = false;
            kendo.ui.progress($("#grid"), true);
            var postData = vm.reviewerConfiguration.documentQueryContext;
            DocumentService.autoCode(postData, vm.TruthSetFieldName, vm.RelevantFieldValue, {}).then(function() {
                kendo.ui.progress($("#grid"), false);
                $("#grid").data("kendoGrid").dataSource.read();
            });
        };

        vm.codeTillLastDocument = function (codingValue) {
            if(vm.d===null){
                return false;
            }
            vm.autoCodeValue = codingValue;
            codeDocument(1);
        };

        var codeDocument = function(idx){
            var current = idx;
            var idMap=vm.reviewerConfiguration.docListIdMapper;
            var postData = vm.reviewerConfiguration.documentQueryContext;
            var dataToSend = {
                "CodingValue" : vm.autoCodeValue === "Not_Relevant" ? "Not_Relevant" :
                                (vm.autoCodeValue === "Relevant" ? "Relevant":
                                (current % 3 === 0 ? "Not_Relevant": "Relevant"))
            };
            kendo.ui.progress($("#grid"), true);
            DocumentService.saveCoding(idMap[current], dataToSend).then(function () {
                current=current+1;
                if((current<vm.docList.Total)){
                    var refId=idMap[current];
                    if(refId!==null && !angular.isUndefined(refId)){
                        codeDocument(current);
                    }else{
                        
                        postData.PageIndex = Math.ceil(current / postData.PageSize);
                        DocumentService.getDocumentlist(postData, {}).then(function(data) {
                            angular.forEach(data.Documents, function(obj) {
                                idMap[obj.Id] = obj.ReferenceId;
                            });
                            codeDocument(current);
                        });
                    }
                }else{
                    kendo.ui.progress($("#grid"), false);
                    $("#grid").data("kendoGrid").dataSource.read();
                }
            });
        };

        getAvailableSets();
        
    };

    app = angular.module("app");

    app.controller("ReviewSetController", ReviewSetController);
    ReviewSetController.$inject = ["AnalysisSetService","DocumentService",
        "AppStateService", "Utils", "Constants"];

}());