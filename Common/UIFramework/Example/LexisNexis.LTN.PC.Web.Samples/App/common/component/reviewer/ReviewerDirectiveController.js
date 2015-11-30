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
