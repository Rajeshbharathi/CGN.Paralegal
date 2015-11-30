(function () {
    "use strict";

    function ProjectsDataService(WebApiClientService, $q, ProjectModel) {
        var console = window.console,
            service;
        function getProjectsOData(odataOptions) {
            return WebApiClientService.get('/odata/ProjectsOData', {
                params: odataOptions
            });
        }

        function getProject(projectId) {
            var promise = WebApiClientService.get('/api/projects/' + projectId, { params: {} }).then(function (response) {
                var project = new ProjectModel();
                project.id = response.data.ID;
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
        }

        function getProjectSummary(projectId) {
            return WebApiClientService.get('/api/projects/summary/' + projectId, { params: {} });
        }

        function deleteProject(projectId) {
            var promise = WebApiClientService.delete('/api/projects/' + projectId, { params: {} }).then(function (data, status) {
                console.log(status + ":" + data);
            });
            return promise;
        }

        function addProject(project) {
            var promise = WebApiClientService.post('/api/projects/' + project.id, project).then(function (data, status) {
                console.log(status + ":" + data);
            });
            return promise;
        }

        function updateProject(project) {
            var promise = WebApiClientService.put('/api/projects/' + project.id, project).then(function (data, status) {
                console.log(status + ":" + data);
            });
            return promise;
        }
        service = {
            getProjectsOData: getProjectsOData,
            getProject: getProject,
            getProjectSummary: getProjectSummary,
            deleteProject: deleteProject,
            addProject: addProject,
            updateProject: updateProject
        };
        return service;

    }
    angular.module('app').factory('ProjectsDataService', ProjectsDataService);
    ProjectsDataService.$inject = ['WebApiClientService', '$q', 'ProjectModel'];
}());