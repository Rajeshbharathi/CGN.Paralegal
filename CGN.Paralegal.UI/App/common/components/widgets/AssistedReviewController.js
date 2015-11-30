(function () {
    "use strict";
    var AssistedReviewController = function ($scope, DocumentService, AnalysisSetService, Constants) {
        var currentDocument, vm,isPreCoded,
            codeDocument = function (cb) {
                var args = arguments;
                if(vm.selectedRelevance === null || angular.isUndefined(vm.selectedRelevance) || vm.selectedRelevance ===""){
                    return;
                }

                if (vm.selectedRelevance !== vm.relevance.NONE) {
                    var dataToSend = {
                        "CodingValue": vm.selectedRelevance
                    };

                    DocumentService.saveCoding(currentDocument.DocumentReferenceId, dataToSend).then(function () {
                        if(cb && angular.isFunction(cb)){
                            if (cb.apply(this, Array.prototype.slice.call(args, 1))) {
                                vm.selectedRelevance = vm.relevance.NONE;
                            }
                        }
                    });
                }
            };

        vm = this;
        vm.localized = Constants.getReviewerResources();

        vm.configuration = DocumentService.getConfiguration();

        vm.relevance = vm.configuration.relevance;
        
        if (currentDocument && currentDocument.Coding && currentDocument.Coding.IsCoded) {
            vm.selectedRelevance = currentDocument.Coding.Value;
        } else {
            vm.selectedRelevance = vm.relevance.NONE;
        }

        $scope.$watch(function() {
            return DocumentService.fetchCurrentDocument();
        }, function(newValue, oldValue) {
            if ((newValue !== oldValue || (currentDocument === null || angular.isUndefined(currentDocument))) && newValue !== null) {
                currentDocument = newValue;
                if (newValue && newValue.Coding && newValue.Coding.IsCoded) {
                    vm.selectedRelevance = newValue.Coding.Value;
                } else {
                    vm.selectedRelevance = vm.relevance.NONE;
                }
                isPreCoded = newValue.Coding.IsCoded;
            }
        });
        
        vm.isRelevant = function () {
            return vm.selectedRelevance === vm.relevance.RELEVANT;
        };
        vm.isNotRelevant = function () {
            return vm.selectedRelevance === vm.relevance.NOTRELEVANT;
        };
        vm.isSkipped = function () {
            return vm.selectedRelevance === vm.relevance.SKIPPED;
        };

        vm.isNextDisabled = function () {
            var bool = true;
            if (!isPreCoded) {
                bool = !(vm.isRelevant() || vm.isNotRelevant() || vm.isSkipped());
            }
            return bool;
        };

        vm.isReviewComplete = function () {
            var setType = vm.configuration.documentQueryContext.AnalysisSet.Type.toUpperCase();
            return AnalysisSetService.isReviewComplete(setType);
        };

        vm.showAutoAdvance = function () {
            var setType = vm.configuration.documentQueryContext.AnalysisSet.Type.toUpperCase();
            var allDocs = (Constants.getStateLabels().ALLDOCUMENTS.toUpperCase() === setType);
            return !$scope.isFilteredSet && !allDocs;
        };

        vm.showNext = function () {
            return vm.showAutoAdvance() && !$scope.autoadvance.checked;
        };

        vm.nextButtonText = function(){
            return (vm.isReviewComplete() ? vm.localized.Finish : vm.localized.Next);
        };
        
        vm.saveCoding = function () {
            if (!$scope.autoadvance.checked) {
                codeDocument();
            }else{
                var callback = $scope.getNextDocument;
                codeDocument(callback,currentDocument.DocumentIndexId,"uncoded");
            }
        };
    },
        app = angular.module("app");
    app.controller("AssistedReviewController", AssistedReviewController);
    AssistedReviewController.$inject = ["$scope", "DocumentService", "AnalysisSetService", "Constants"];
}());
