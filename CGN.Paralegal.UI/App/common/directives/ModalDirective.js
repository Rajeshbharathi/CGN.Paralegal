(function () {
    "use strict";
    var app, ModalDirective;

    ModalDirective = ["$q", "Constants", function ($q, Constants) {
        return {
            restrict: "A",
            scope: {
                show:"=lnModal",
                title: "@lnModalTitle",
                disableConfirm: "=?lnModalValidateConfirm",
                confirmCallback: "&?lnModalOnConfirm",
                cancelCallback: "&?lnModalOnCancel",
                confirmLabel: "@?lnModalConfirmLabel",
                cancelLabel: "@?lnModalCancelLabel",
                hideFooter:"=?lnModalHideFooter",
                modalClass: "@?lnModalClass"
            },
            transclude:true,
            template: "<div class='modal fade in {{modalClass}}' id='modalDialog' ng-if='isModalShown()'> <div class='modal-backdrop fade in'></div> <div class='modal-dialog '>" +
                " <div class='modal-content'> <div class='modal-header'> <h3 class='modal-title'> {{ title }} " +
                " <span class='pull-right' ng-click='cancel()'> <i class='glyphicon glyphicon-remove-circle'></i>" +
                "  </span> </h3> </div> <div class='modal-body'> <div ng-transclude></div> </div> <div class='modal-footer'>" +
                "  <button class='btn btn-primary' id='confirmBtn' ng-disabled='disableConfirm' ng-click='confirm()'> {{ confirmLabel }} </button>" +
                "  <button class='btn btn-default' id='cancelButton' ng-click='cancel()'> {{ cancelLabel }} </button> </div>" +
                "  </div> </div> </div>",
            link: function (scope) {
                
                scope.localized = Constants.getCommonResources();
                scope.confirmLabel = scope.confirmLabel || scope.localized.Confirm;
                scope.cancelLabel = scope.cancelLabel || scope.localized.Cancel;
                scope.hideFooter = scope.hideFooter || false;

                scope.isModalShown = function () {
                    return scope.show;
                };

                scope.confirm = function () {
                    var resolveCallback;
                    if (!scope.disableConfirm) {
                        resolveCallback = scope.confirmCallback();
                    } else {
                        resolveCallback = false;
                    }
                    
                    $q.when(resolveCallback).then(function (response) {
                        if (response !== false) {
                            scope.show = false;
                        }
                    });                    
                };

                scope.cancel = function () {
                    var resolveCallback = scope.cancelCallback();
                    $q.when(resolveCallback).then(function (response) {
                        if (response !== false) {
                            scope.show = false;
                        }
                    });
                };

                var watch=scope.$watch(function(){
                    return scope.show;
                },function(newValue){
                    if(newValue){
                        $("body").addClass("modal-open");
                    }else{
                        $("body").removeClass("modal-open");
                    }
                });

                scope.$on("$destroy", function() {
                    watch();
                });
            }
        };
    }];

    app = angular.module("app");

    app.directive("lnModal", ModalDirective);

}());