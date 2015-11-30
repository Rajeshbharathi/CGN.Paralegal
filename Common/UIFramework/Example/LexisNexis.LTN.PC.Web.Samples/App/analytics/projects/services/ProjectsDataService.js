(function () {
    "use strict";
    angular.module('app').factory('ProjectsDataService', ProjectsDataService);
    ProjectsDataService.$inject = ['$http', '$q', 'ProjectModel', 'WebApiClientService'];
    function ProjectsDataService($http, $q, ProjectModel, WebApiClientService) {

        var service = {
            getProjectsOData: getProjectsOData,
            getProject: getProject,
            getProjectSummary: getProjectSummary,
            deleteProject: deleteProject,
            addProject: addProject,
            updateProject: updateProject,
            checkUniqueProjectName: checkUniqueProjectName,
            calculateSampleSize : calculateSampleSize
        };
        return service;

        function getProjectsOData(odataOptions) {
            return WebApiClientService.get('/odata/ProjectsOData', {
                params: odataOptions
            });
        };

        function getProject(projectId) {
            var promise = WebApiClientService.get('/api/projects/' + projectId, { params: {} }).then(function (response) {
                var project = new ProjectModel();
                project.id = response.data.Id;
                project.name = response.data.Name;
                project.description = response.data.Description;
                project.docSource = response.data.DocSource;
                project.tag = response.data.Tag;
                project.identifyRepeatedContent = response.data.IdentifyRepeatedContent;
                project.confidence = response.data.Confidence;
                project.marginOfError = response.data.MarginOfError;
                project.stratifyByCustodian = response.data.StratifyByCustodian;
                project.custodianField = response.data.CustodianField;
                project.sampleSize = response.data.SampleSize;
                project.limitExamples = response.data.LimitExamples;
                project.numExamples = response.data.NumOfExamples;
                return project;
            });
            return promise;
        };

        function getProjectSummary(projectId) {
            return WebApiClientService.get('/api/projects/summary/' + projectId, { params: {} }).then(function (response) {
                return response.data;
            });
            return promise;
        };

        function deleteProject(projectId) {
            var promise = WebApiClientService.delete('/api/projects/' + projectId, { params: {} }).then(function (response) {
                //todo: handle errors
            });
            return promise;
        }

        function addProject(project) {
            var promise = WebApiClientService.post('/api/projects/' + project.id, project).then(function (response) {
                //todo: handle errors
            });
            return promise;
        }

        function updateProject(project) {
            var promise = WebApiClientService.put('/api/projects/' + project.id, project).then(function (response) {
                //todo: handle errors
            });
            return promise;
        }

        /*Sample Error Handling examples*/
        function checkUniqueProjectName(projectName) {
            var deferred = $q.defer();
            WebApiClientService.get('/api/projects/1', { params: {} }).then(function (result) {
                if (result.data.Name !== projectName) {
                    deferred.resolve(true);
                } else {
                    var err = new Error();
                    err.status = "Custom";
                    err.message = "Validation Failed"
                    deferred.reject(err);
                }

            });
            return deferred.promise;
        }

        function calculateSampleSize () {
            var min = 1, max = 100;
            var random = Math.floor(Math.random() * (max - min + 1)) + min;
            if(random%2==0){
                return WebApiClientService.get('/api/projects/samplesize', { params: {} }).then(function (response) {
                    return response.data;
                });
            } else if (random % 3) {
                return WebApiClientService.get('/api/test', { params: {} });
            } else {
                return WebApiClientService.get('/api/projects/samplesizeerror', { params: {} });
            }
            
        }
        
    }
 })();