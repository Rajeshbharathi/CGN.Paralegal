/*
 * ErrorHandlerService - Creates and stores formatted error message from errors[HTTP/Javascript] to display on UI and 
 * allows removal of errors.
 * 
 */

(function () {
    "use strict";

    angular.module("app").factory("ErrorHandlerService", ["Utils", "Constants", function (Utils, Constants) {
       
        var handler = {
            errors: [],
            buildErrorMessage: function (err) {

                if (err && !angular.isUndefined(err.status)) {

                    if (err.status.toString() === "401") {
                        var sid="0";
                        if (err.data && err.data.Reason && err.data.Reason.Code) {
                            switch (err.data.Reason.Code) {
                                case "1236":
                                    sid="1";
                                    break;
                                case "1237" :
                                    sid="3";
                                    break;
                                default:
                                    sid= "0";
                                    break;
                            }
                        }

                            // redirect parent window
                        var EVUrl = "/login.aspx?sid="+sid;
                        Utils.loadPage(EVUrl,"?response=error");
                    }
                    
                    if (err.data && err.data.Detail) {
                        // handle custom http error response from WCF
                        err = err.data.Detail;
                        
                    } else if (err.data && err.data.ExceptionType) {
                        // handle custom http error response from webapi
                        err = err.data.ExceptionType + ": " + err.data.Message + "[ " + err.data.ExceptionMessage + " ]";
                    } else if (err.data && err.data.MessageDetail) {
                        // handle standard http error response
                        err = err.statusText + ":" + err.data.Message + "[" + err.data.MessageDetail + "]";
                    } else if (err.data && err.data.Message) {
                        // handle standard http error response
                        err = err.data.Message;
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
                    err = Constants.getCommonResources().UnknownError;
                }
                return err;
            },
            get: function () {
                return handler.errors;
            },
            addError: function (err) {
                var errorMessage = handler.buildErrorMessage(err);
                if (handler.errors.indexOf(errorMessage) < 0) {
                    handler.errors.push(errorMessage);
                }
            },
            clearError: function (idx) {
                handler.errors.splice(idx, 1);
            }
        };

        return handler;
    }]);
}());


