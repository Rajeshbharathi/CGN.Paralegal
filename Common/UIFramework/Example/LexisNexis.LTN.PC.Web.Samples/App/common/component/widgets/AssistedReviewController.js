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
