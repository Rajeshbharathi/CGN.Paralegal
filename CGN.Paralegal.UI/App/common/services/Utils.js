(function () {
    "use strict";

    angular.module("app").factory("Utils", ["$location","$window", "$routeParams", "AppStateService",
        function ($location,$window, $routeParams, AppStateService) {

        kendo.dataviz.LegendItem.fn.createMarker = function () {
            var item = this,
                options = item.options,
                markerColor = options.markerColor,
                markers = options.markers,
                markerType = "rect",
                markerWidth = 7,
                markerHeight = 7,
                markerMargin = { top: 0, left: 0, right: 0, bottom: 0 };

            if (options.series.legend) {
                if (options.series.legend.marker) {
                    if (options.series.legend.marker.type) {
                        markerType = options.series.legend.marker.type;
                    }
                    if (options.series.legend.marker.width) {
                        markerWidth = options.series.legend.marker.width;
                    }
                    if (options.series.legend.marker.height) {
                        markerHeight = options.series.legend.marker.height;
                    }
                    if (options.series.legend.marker.margin) {
                        markerMargin = options.series.legend.marker.margin;
                    }
                }
            }


            var markerOptions = kendo.deepExtend({}, markers, {
                background: markerColor,
                border: {
                    color: markerColor
                },
                type: markerType,
                width: markerWidth,
                height: markerHeight,
                margin: markerMargin
            });

            item.container.append(new kendo.dataviz.ShapeElement(markerOptions));
        }

        var hasEVShell = function () {
            return ($window.top !== $window);
        },
        baseRoute = "/api/orgs/{orgid}/matters/{matterid}/datasets/{datasetid}/projects/{projectid}/",
        analysisSetsRoute = baseRoute + "analysissets/",
        projectRoute = baseRoute + "project/",
        /*TODO: refactor URLs into generic*/
        routeMapper = {
            "AVAILABLEANALYSISSETSROUTE": analysisSetsRoute,
            "AVAILABLEDOCUMENTCOUNTROUTE": baseRoute+"availabledoccount",
            "CATEGORIZECONTROLSETROUTE": baseRoute+"controlset/categorize",
            "CATEGORIZETRAININGSETROUTE": baseRoute+"analysisSetType/TrainingSet/binderid/{binderId}/categorize",
            "CHANGEDWORKFLOWSTATEROUTE": baseRoute+"analytics/changed-workflow-state",
            "CONTROLSETDOCLISTROUTE": analysisSetsRoute+"controlset/documents",
            "CONTROLSETDOCUMENTPAGEROUTE": "/api/orgs/matters/datasets/projects/analysissets/controlset/document",
            "CONTROLSETDOCUMENTROUTE": analysisSetsRoute+"controlset/documents",
            "CONTROLSETSUMMARYROUTE": baseRoute+"controlset/summary",
            "CREATECONTROLSETROUTE": baseRoute+"controlset",
            "CREATEPROJECTROUTE": projectRoute,
            "CREATEQCSETROUTE": baseRoute+"qcset",
            "CREATETRAININGSETROUTE": baseRoute+"trainingset",
            "DASHBOARDDETAILSROUTE": projectRoute,
            "DELETEPROJECTROUTE":baseRoute,
            "DISCREPANCIESROUTE": baseRoute+"analysisSetTypes/{setType}/analysisSets/{setId}/categorization/discrepancies",
            "DOCUMENTCOUNTROUTE": baseRoute + "documentcounts",
            "EXPORTCONTROLSETROUTE": analysisSetsRoute+"controlset/documents/export",
            "EXPORTQCSETROUTE": analysisSetsRoute+"qcset/documents/export",
            "EXPORTTRAININGSETROUTE": analysisSetsRoute+"trainingset/documents/export",
            "EXPORTALLDOCUMENTSROUTE": analysisSetsRoute + "alldocuments/documents/export",
            "GETQCSETSINFOROUTE": baseRoute+"qcSets",
            "GETSAVEDSEARCHROUTE": projectRoute+"savedsearches",
            "GETTAGSROUTE": projectRoute+"tags",
            "NEXTUNCODEDDOCUMENTROUTE": baseRoute+"uncodedDocument",
            "PREDICTALLSUMMARYROUTE":baseRoute+"predictAllSummary",
            "PREDICTIONSCORESROUTE": baseRoute+"analysisSetTypes/{setType}/analysisSets/{setId}/categorization/scores",
            "QCSETDOCLISTROUTE": analysisSetsRoute+"qcset/documents",
            "QCSETDOCUMENTPAGEROUTE": "/api/orgs/matters/datasets/projects/analysissets/qcset/document",
            "QCSETDOCUMENTROUTE": analysisSetsRoute+"qcset/documents",
            "SAMPLESIZEROUTE": baseRoute+"controlset/samplesize",
            "SAVECODINGROUTE": baseRoute+"documents",
            "SCHEDULECONTROLSETEXPORTJOBROUTE": analysisSetsRoute+"controlset/documents/export/job",
            "SCHEDULEPREDICTALLROUTE": baseRoute+"categorize",
            "SCHEDULEQCSETEXPORTJOBROUTE": analysisSetsRoute+"qcset/documents/export/job",
            "SCHEDULETRAININGSETEXPORTJOBROUTE": analysisSetsRoute+"trainingset/documents/export/job",
            "SCHEDULEALLDOCUMENTSEXPORTJOBROUTE": analysisSetsRoute + "alldocuments/documents/export/job",
            "TRAININGSETDOCLISTROUTE": analysisSetsRoute+"trainingset/documents",
            "TRAININGSETDOCUMENTPAGEROUTE": "/api/orgs/matters/datasets/projects/analysissets/trainingset/document",
            "TRAININGSETDOCUMENTROUTE": analysisSetsRoute+"trainingset/documents",
            "TRAININGSETROUTE": baseRoute+"trainingset",
            "TRAININGSETSUMMARYROUTE": baseRoute+"training/summary",
            "TRAININGSETDISCREPANCIESROUTE":baseRoute+"discrepancies/trainingset",
            "UPDATEDWORKFLOWROUTE": baseRoute + "analysisset/{binderId}/analytics/workflow-state",
            "VALIDATEPROJECTINFOROUTE": projectRoute + "validate",
            "VALIDATEPREDICTIONCODINGROUTE": baseRoute +"qcset/"+ "validate",
            "WORKFLOWROUTE": baseRoute+"analytics/workflow-state",
            "ALLDOCUMENTSDOCLISTROUTE": analysisSetsRoute + "alldocuments/documents",
            "ALLDOCUMENTSDOCUMENTROUTE": analysisSetsRoute + "alldocuments/documents",
            "ALLDOCUMENTSDOCUMENTPAGEROUTE": "/api/orgs/matters/datasets/projects/analysissets/alldocuments/document",
        },
        analysisSets = {
            "CONTROLSET": "ControlSet",
            "TRAININGSET": "TrainingSet",
            "QCSET": "QcSet",
            "PREDICTSET": "PredictSet",
            "ALLDOCUMENTS": "AllDocuments"
        },
        pieColors= {
            "Relevant":"#B1D877",
            "NotRelevant":"#F16A70",
            "Skipped":"#8CDCDA",
            "NotCoded":"#4D4D4D"
        },
        getDifferenceRates = function(arr){
            var rates = [];
            angular.forEach(arr,function(obj){
                rates.push(Number((parseFloat(obj.DifferenceRate)*100).toFixed(2)));
            });
            return rates;
        },
        getRollingAverage = function(arr){
            var rates = getDifferenceRates(arr),
            rollingAvg,temp=[],
            truncate=[],avg=[];
            angular.forEach(rates,function(obj,idx){
                var index=idx+1;
                if(idx <= 3){
                    temp = angular.copy(rates);
                    truncate=temp.splice(0,index);
                    rollingAvg=truncate.reduce(function(previous, next) { return previous + next; }, 0)/index;
                    
                }
                if(idx>3){
                    temp = angular.copy(rates);
                    truncate=temp.splice(idx-3,4);
                    rollingAvg=truncate.reduce(function(previous, next) { return previous + next; }, 0)/4;   
                }
                truncate=[];
                temp=[];
                avg.push(rollingAvg.toFixed(2));
            });
            return avg;
        },
        getCategories=function(arr){
            var categories=[];
            angular.forEach(arr,function(obj){
                categories.push(obj.Round);
            });
            var xMax= Math.max.apply(Math,categories);
            categories.push(++xMax,++xMax);
            return categories;
        },
        buildStackedLineChartDataFromArray= function (data) {
                var recallScores = [0],
                    precisionScores=[0],
                    f1Scores =[0],
                    categoryData=[0];
                var temp = angular.copy(data);
                angular.forEach(temp, function(obj,idx) {
                    recallScores.push(Number((parseFloat(obj.Recall.value)).toFixed(2)));
                    precisionScores.push(Number((parseFloat(obj.Precision.value)).toFixed(2)));
                    f1Scores.push(Number((parseFloat(obj.F1.value)).toFixed(2)));
                    categoryData.push((parseInt(idx,10)+1));
                });
                var xMax= Math.max.apply(Math,categoryData);
                categoryData.push(++xMax,++xMax);
                var maxValue = Math.ceil(Math.max(Math.max.apply(null,recallScores),Math.max.apply(null,precisionScores),Math.max.apply(null,f1Scores))),
                    yMax = ((maxValue+5)>90?100:(maxValue+5));
                temp=null;
                return {
                        legend: {
                            visible: true,
                            position: "bottom",
                            labels: {
                                font: "14px Helvetica, Arial",
                                color: "#333",
                                margin: {
                                    right: 15,
                                }
                            },
                        },
                        seriesDefaults: {
                            type: "line",
                            missingValues: "gap",
                            stack: false,
                            markers:{
                                visible:true
                            },
                            style:"smooth"
                        },
                        chartArea: {
                            background: "transparent"
                        },
                        series: [{
                                name: "Recall",
                                data: recallScores,
                                color: "#348f37",
                                legend: {
                                    marker: {
                                        type: "circle",
                                        width: 9,
                                        height: 9,
                                        margin: { top: 0 }
                                    }
                                }
                        }, {
                                name: "Precision",
                                data: precisionScores,
                                color: "#fbc02d",
                                legend: {
                                    marker: {
                                        type: "circle",
                                        width: 9,
                                        height: 9,
                                        margin: { top: 0 }
                                    }
                                }
                        }, {
                                name: "F1",
                                data: f1Scores,
                                color: "#5126ab",
                                legend: {
                                    marker: {
                                        type: "circle",
                                        width: 9,
                                        height: 9,
                                        margin: { top: 0 }
                                    }
                                }
                        }],
                        valueAxis: {
                            max: yMax,
                            title: {
                                text: "%"
                            },
                            line: {
                                visible: true
                            },
                            majorGridLines: {
                                visible: false
                            },
                            minorGridLines: {
                                visible: false
                            }
                        },
                        categoryAxis: {
                            categories: categoryData,
                            justified:true,
                            majorGridLines: {
                                visible: false
                            },
                            minorGridLines: {
                                visible: false
                            },
                            majorTicks: {
                                visible: false
                            },
                            minorTicks: {
                                visible: false
                            },
                            line: {
                                visible: true
                            },
                            labels:{
                                step: (data.length <= 10) ? 1 : ((data.length <= 50) ? 5 : 10),
                                rotation: 0,
                            }
                        },
                        tooltip: {
                            visible: true,
                            template: "#= series.name #: #= value #",
                            color:"#fff"
                        }
                    };
            },
            goToDocList=function(binderId,roundNumber){
                var EVUrl = "/App/ReviewerApp.aspx?mod=review&view=analysissets/trainingset/" +
                    roundNumber + "/" + binderId + "/from_admin/to_doclist/All";
                var PCUrl = "/app/review/approot#/analysissets/trainingset/" +
                    roundNumber + "/" + binderId + "/from_admin/to_doclist/All";
                loadPage(EVUrl, PCUrl);
            },
            loadPage=function (EVUrl,PCUrl) {
                var browser;
                if (hasEVShell()) {
                    browser = $window.top;
                    if (EVUrl.length > 0) {
                        browser.location = EVUrl;
                    }
                } else {
                    if (PCUrl.length > 0) {
                        $window.location = PCUrl;
                    }
                }
            },
            loadView = function (Url) {
                    if (Url.length > 0) {
                        $location.path(Url);
                    }
            },
        handler = {
            loadPage: loadPage,
            loadView: loadView,
            modals : {
                "controlset": false,
                "trainingset": false,
                "qcset": false,
                "predictset": false,
                "change": false,
                "delete":false
            },
            getRouteParam: function (str) {
                return $routeParams[str];
            },
            getQueryParamString: function(param){
                var query = $window.location.href.split("?")[1];
                if (query !== null && !angular.isUndefined(query) && query.length > 0) {
                    var vars = query.split("%2F");
                    for (var i = 0; i < vars.length; i+=1) {
                        var pair = vars[i].split("=");
                        if (pair[0] === param) {
                            return pair[1];
                        }
                    }
                }
                return false;
            },
            getLocationString: function() {
                return $window.location.href.split("#/")[1];
            },
            getDefaultGridFiltersArray: function (str) {
                var temp = [];
                switch (str.toUpperCase()) {
                    case "ALL":
                        break;
                    case "DISCREPANCIES":
                        break;
                    case "TRUEPOSITIVES":
                        temp = [{
                            "field": "ReviewerCategory",
                            "value": "Relevant",
                            "operator": "contains"
                        },
                        {
                            "field": "PredictedCategory",
                            "value": "Relevant",
                            "operator": "contains"
                        }];
                        break;
                    case "FALSEPOSITIVES":
                        temp = [{
                            "field": "ReviewerCategory",
                            "value": "Not_Relevant",
                            "operator": "contains"
                        },
                        {
                            "field": "PredictedCategory",
                            "value": "Relevant",
                            "operator": "contains"
                        }];
                        break;
                    case "FALSENEGATIVES":
                        temp = [{
                            "field": "ReviewerCategory",
                            "value": "Relevant",
                            "operator": "contains"
                        },
                        {
                            "field": "PredictedCategory",
                            "value": "Not_Relevant",
                            "operator": "contains"
                        }];
                        break;
                    case "TRUENEGATIVES":
                        temp = [{
                            "field": "ReviewerCategory",
                            "value": "Not_Relevant",
                            "operator": "contains"
                        },
                        {
                            "field": "PredictedCategory",
                            "value": "Not_Relevant",
                            "operator": "contains"
                        }];
                        break;
                    default:
                        temp = [{
                            "field": "ReviewerCategory",
                            "value": str,
                            "operator": "contains"
                        }];
                        break;
                }

                return temp;
            },

            getDefaultGridSearchKeyWord: function (filter, reviewerFieldName, predictedFieldName) {
                var query;
                switch (filter.toUpperCase()) {
                    case "DISCREPANCIES":
                        query = "(((\"ReviewerCategory\":\"Relevant\") AND (\"PredictedCategory\":\"Not_Relevant\")) OR ((\"ReviewerCategory\":\"Not_Relevant\") AND (\"PredictedCategory\":\"Relevant\")))";
                        query = query.split("ReviewerCategory").join(reviewerFieldName).
                            split("PredictedCategory").join(predictedFieldName);
                        return query;
                    default:
                        return "";
                }
            },

            buildControlSetRichnessChartData: function (controlSetData) {
                var chartData = {
                    legend: {
                        visible: false,
                    },
                    seriesDefaults: {
                        type: "donut",
                        holeSize: 70,
                        size: 12,
                        donut: {
                            overlay: {
                                gradient: "none"
                            }
                        },
                    },
                    chartArea: {
                        width: 170,
                        height: 230,
                    },
                    series: [
                        {
                            type: "donut",
                            data: [
                                {
                                    category: "Relevant",
                                    value: controlSetData.PercentageOfTotalPopulation,
                                    color: "#27ae60",
                                    margin: 0,
                                    padding: 0,
                                },
                                {
                                    category: "Not Relevant",
                                    value: 100 - controlSetData.PercentageOfTotalPopulation,
                                    color: "#cc0033",
                                    margin: 0,
                                    padding: 0,
                                }
                            ]
                        }
                    ],
                    tooltip: {
                        visible: true,
                        template: "#= category # - #= kendo.format('{0:P}', percentage) #"
                    }
                };

                return chartData;
            },

            buildPieChartDataSource : function (seriesData){
                var chartData = {
                        legend: {
                            visible: false
                        },
                        chartArea: {
                            width: 120,
                            height: 120,
                            background: "transparent"
                        },
                        seriesDefaults: {
                            labels: {
                                distance: -30,
                                position: "inside",
                                visible: true,
                                template: "#= kendo.format('{0:P0}', percentage)#"
                            },
                            style:"flat",
                            pie: {
                                 "overlay": {
                                     "gradient": "none"
                                 }
                             }
                        },
                        series: [{
                            type : "pie",
                            startAngle : 150,
                            padding: 10,
                            data: seriesData
                        }],
                        tooltip: {
                            visible: false
                        }
                    };
                return chartData;
            },
            buildMiniPredictionScoreChartData:function(data){
                var obj = buildStackedLineChartDataFromArray(data);
                obj.chartArea.height=275;
                return obj;
            },
            buildLargePredictionScoreChartData:function(data){
                var obj = buildStackedLineChartDataFromArray(data);
                obj.tooltip.visible = true;
                obj.chartArea.height=450;
                var len=data.length;
                return obj;
            },
            buildTrainingDiscrepancyData:function(arr){
                var maxValue = Math.max(Math.max.apply(null, getDifferenceRates(arr)),Math.max.apply(null, getRollingAverage(arr))),
                yMax=((maxValue+5)>90) ? 100 : (maxValue+5);
                return {
                    title:{
                        text:"Discrepancies / Round",
                        font: "12px sans-serif",
                    },
                    legend: {
                        visible: true,
                        position: "bottom",
                        labels: {
                            font: "14px Helvetica, Arial",
                            color: "#333",
                            margin: { right: 15 }
                        },
                    },
                    autoBind:false,
                    seriesDefaults: {
                        tooltip: {
                            visible: true,
                            template: "#= series.name #: #= value #",
                            color:"#fff"
                        },
                        missingValues: "gap",
                        stack: false
                    },
                    chartArea: {
                        height:450,
                        background: "transparent"
                    },
                    series: [{
                        name:"Rolling Average",
                        type: "line",
                        data: getRollingAverage(arr),
                        color:"#348f37",
                        axis: "rollingAvg",
                        markers: {
                            visible: true
                        },
                        legend: {
                            marker: {
                                type: "circle",
                                width: 9,
                                height: 9,
                                margin: { top: 0 }
                            }
                        }
                    },{
                        name:"% / Round",
                        type: "area",
                        data: getDifferenceRates(arr),
                        line: {
                            style: "smooth"
                        },
                        markers: {
                            visible: false
                        },
                        color: "#5126ab",
                        axis: "differencePercent",
                        legend: {
                            marker: {
                                type: "circle",
                                width: 9,
                                height: 9,
                                margin: { top: 0 }
                            }
                        }

                    }],
                    valueAxes: [{
                        name: "rollingAvg",
                        min:0,
                        max: yMax,
                        title: {
                            text: "%"
                        },
                        labels: {
                            format: "{0}"
                        },
                        line: {
                            visible: true
                        },
                        majorGridLines: {
                            visible: false
                        },
                        minorGridLines: {
                            visible: false
                        }
                    },{
                        name: "differencePercent",
                        min:0,
                        max: yMax,
                        visible:false
                    }],
                    categoryAxis: {
                        min:2,
                        categories: getCategories(arr),
                        axisCrossingValues: [0, 0],
                        majorGridLines: {
                            visible: false
                        },
                        majorTicks: {
                            visible: false
                        },
                        justified: true,
                        labels: {
                            step: (arr.length <= 10) ? 1: ((arr.length <= 50) ? 5: 10),
                            rotation: 0,
                        }
                    }
                };
            },
            buildMiniTrainingDiscrepancyData: function (data) {
                var obj = buildTrainingDiscrepancyGrid(data);
                obj.chartArea.height = 275;
                return obj;
            },
            buildTrainingDiscrepancyGrid :function(arr){

                var data = [],
                avg= getRollingAverage(arr);
                angular.forEach(arr,function(obj,idx){
                    obj.RollingAverage = avg[idx];
                    data.push(obj);
                });
                
                var obj = {
                    dataSource: data.reverse(),
                            resizable: false,
                            sortable: false,
                            pageable: false,
                            filterable: false,
                            columnMenu: false,
                            columns: [{
                                    field: "SetName",
                                    title: "Round",
                                    template: function (dataItem) {
                                        return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                            dataItem.BinderId + "\",\"" + parseInt(dataItem.SetName.split(" ")[2], 10) + "\",\"All\")'>" + dataItem.SetName + "</a>";

                                    }
                                }, {
                                    field: "TotalDifference",
                                    title: "#",
                                    width: 70,
                                    template: function (dataItem) {
                                        if (dataItem.TotalDifference > 0) {
                                            return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                            dataItem.BinderId + "\",\"" + parseInt(dataItem.SetName.split(" ")[2], 10) + "\",\"Discrepancies\")'>" + dataItem.TotalDifference + "</a>";
                                        } else {
                                            return dataItem.TotalDifference;
                                        }

                                    }
                                }, {
                                    field: "DifferenceRate",
                                    title: "% / Round",
                                    template: function (dataItem) {
                                        return Number((parseFloat(dataItem.DifferenceRate)*100).toFixed(2));
                                    }
                                }, {
                                    field: "RollingAverage",
                                    title: "Rolling Avg"
                                }]
                        };
                return obj;
            },
            buildDashboardTrainingSummaryGrid :function(arr){
                var data = angular.copy(arr);
                var obj = {
                    dataSource: data,
                    resizable: false,
                    sortable: false,
                    pageable: false,
                    filterable: false,
                    columnMenu: false,
                    columns: [
                        {
                            field: "Name",
                            title: "Training Rounds",
                            template: function(dataItem) {
                                return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                    dataItem.BinderId + "\",\"" + parseInt(dataItem.Name.split(" ")[2], 10) + "\",\"All\")'>" + dataItem.Name + "</a>";
                            
                            }
                           
                        }, {
                            field: "NumberOfRelevantDocuments",
                            title: "Relevant",
                            template: function(dataItem) {
                                if (dataItem.NumberOfRelevantDocuments > 0) {
                                    return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                        dataItem.BinderId + "\",\"" + parseInt(dataItem.Name.split(" ")[2], 10) + "\",\"Relevant\")'>" + dataItem.NumberOfRelevantDocuments + "</a>";
                                } else {
                                    return dataItem.NumberOfRelevantDocuments;
                                }
                            }
                        }, {
                            field: "NumberOfNotRelevantDocuments",
                            title: "Not Relevant",
                            template: function (dataItem) {
                                if (dataItem.NumberOfNotRelevantDocuments > 0) {
                                    return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                        dataItem.BinderId + "\",\"" + parseInt(dataItem.Name.split(" ")[2], 10) + "\",\"Not_Relevant\")'>" + dataItem.NumberOfNotRelevantDocuments + "</a>";
                                } else {
                                    return dataItem.NumberOfNotRelevantDocuments;
                                }
                            }
                        }, {
                            field: "NumberOfSkippedDocuments",
                            title: "Skipped",
                            template: function (dataItem) {
                                if (dataItem.NumberOfSkippedDocuments > 0) {
                                    return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                        dataItem.BinderId + "\",\"" + parseInt(dataItem.Name.split(" ")[2], 10) + "\",\"Skipped\")'>" + dataItem.NumberOfSkippedDocuments + "</a>";
                                } else {
                                    return dataItem.NumberOfSkippedDocuments;
                                }
                            }
                        }, {
                            field: "NumberOfNotCodedDocuments",
                            title: "Not Coded",
                            template: function (dataItem) {
                                if (dataItem.NumberOfNotCodedDocuments > 0) {
                                    return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                        dataItem.BinderId + "\",\"" + parseInt(dataItem.Name.split(" ")[2], 10) + "\",\"Not_Coded\")'>" + dataItem.NumberOfNotCodedDocuments + "</a>";
                                } else {
                                    return dataItem.NumberOfNotCodedDocuments;
                                }
                            }
                        }]
                };
                return obj;
            },

            buildTrainingTrendsGrid :function(arr){
                var dataSource = angular.copy(arr);
                var obj = {
                        dataSource: dataSource.reverse(),
                        resizable: false,
                        sortable: false,
                        pageable: false,
                        selectable:"cell",
                        filterable: false,
                        columnMenu: false,
                        columns: [{
                                field: "SetName",
                                title: "Round",
                                template:function(dataItem){
                                    if(dataItem.SetName){
                                        return "<a class='text-primary' href='javascript:void(0)' ng-click='TrainingSetController.viewTrainingSetDocumentList(\"" +
                                            dataItem.BinderId + "\",\"" + dataItem.Id + "\",\"All\")'>" + dataItem.SetName + "</a>";
                                    }
                                }
                            }, {
                                field: "Recall",
                                title: "Recall",
                                template:function(dataItem){
                                    if(dataItem.Recall.increased){
                                        return "<span class='formatted'>"+kendo.toString(dataItem.Recall.value, "n1")+
                                            "% </span><span class='glyphicon glyphicon-arrow-up text-success'></span>";
                                    }
                                    if(dataItem.Recall.decreased){
                                        return "<span class='formatted'>"+kendo.toString(dataItem.Recall.value, "n1")+
                                            "% </span><span class='glyphicon glyphicon-arrow-down text-danger'></span>";
                                    }
                                    return "<span class='formatted'>"+kendo.toString(dataItem.Recall.value, "n1")+
                                        "% </span><span class='glyphicon glyphicon-resize-horizontal text-primary'></span>";
                                    
                                }
                            }, {
                                field: "Precision",
                                title: "Precision",
                                template:function(dataItem){
                                    if(dataItem.Precision.increased){
                                        return "<span class='formatted'>"+kendo.toString(dataItem.Precision.value, "n1")+
                                            "% </span><span class='glyphicon glyphicon-arrow-up text-success'></span>";
                                    }
                                    if(dataItem.Precision.decreased){
                                        return "<span class='formatted'>"+kendo.toString(dataItem.Precision.value, "n1")+
                                            "% </span><span class='glyphicon glyphicon-arrow-down text-danger'></span>";
                                    }
                                    return "<span class='formatted'>"+kendo.toString(dataItem.Precision.value, "n1")+
                                        "% </span><span class='glyphicon glyphicon-resize-horizontal text-primary'></span>";
                                    
                                }
                            }, {
                                field: "F1",
                                title: "F1",
                                template:function(dataItem){
                                    if(dataItem.F1.increased){
                                        return "<span class='formatted'>"+kendo.toString(dataItem.F1.value, "n1")+
                                            "% </span><span class='glyphicon glyphicon-arrow-up text-success'></span>";
                                    }
                                    if(dataItem.F1.decreased){
                                        return "<span class='formatted'>"+kendo.toString(dataItem.F1.value, "n1")+
                                            "% </span><span class='glyphicon glyphicon-arrow-down text-danger'></span>";
                                    }
                                    return "<span class='formatted'>"+kendo.toString(dataItem.F1.value, "n1")+
                                        "% </span><span class='glyphicon glyphicon-resize-horizontal text-primary'></span>";
                                    
                                }
                            }]
                    };
                return obj;
            },
            getWebApiRouteString : function (methodName) {
                var data = AppStateService.appState();
                var str = routeMapper[methodName.toUpperCase()].replace("{orgid}", data.OrgId)
                    .replace("{matterid}", data.MatterId)
                    .replace("{datasetid}", data.DatasetId)
                    .replace("{projectid}", data.ProjectId);

                return str;

            },
           getappStateUserGroups: function() {
                var appState = AppStateService.appState();
                return (appState === null || appState.UserGrops === null) ? "The Reviewer": (appState.UserGrops.toString()) ===""?"The Reviewer": appState.UserGrops.toString().replace(",",", ");

            },
            getAnalysisSetType : function(str){
                return analysisSets[str.toUpperCase()];
            },
            getPieSeriesCategoryObj:function(key,val){
                return {
                        category: key,
                        value: parseInt(val, 10),
                        color: pieColors[key]
                    };
            },
            exportCSV: function (data) { 
                var D = document,
                    a = D.createElement("a"),
                    strMimeType = "data:text/csv;charset=utf-8,",
                    csv = data,
                    csvData = null,
                    filename = "Documents.csv";


                if (navigator.msSaveBlob) { // IE10+

                    return navigator.msSaveBlob(new Blob([csv], { type: strMimeType }), filename);
                }


                if ("download" in a) { //html5 A[download]
                    csvData = encodeURIComponent(csv);
                    a.href = strMimeType + csvData;
                    a.setAttribute("download", filename);
                    a.innerHTML = "downloading...";
                    D.body.appendChild(a);
                    setTimeout(function () {
                        a.click();
                        D.body.removeChild(a);
                    }, 66);
                    return true;
                }
            }
        };

        return handler;
    }]);
}());

String.format = function() {
    var s = arguments[0];
    for (var i = 0; i < arguments.length - 1; i++) {
        var reg = new RegExp("\\{" + i + "\\}", "gm");
        s = s.replace(reg, arguments[i + 1]);
    }

    return s;
};

Array.prototype.indexOfObject = function(prop, value) {
    for (var i = 0; i < this.length; i++) {
        if (this[i][prop] === value) {
            return i;
        }
    }

    return -1;
};
