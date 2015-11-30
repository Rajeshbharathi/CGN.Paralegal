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


