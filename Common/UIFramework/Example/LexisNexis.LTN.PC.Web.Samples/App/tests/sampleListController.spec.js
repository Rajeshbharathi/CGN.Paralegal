/// <reference path="references.js"/>
/// <reference path="~/app/views/sampleList/sampleList.controller.js"/>

describe('Sample List Controller', function () {
    var scope, httpBackend, controller;

    beforeEach( module('app') );

    //inject the contentMocks service
    beforeEach(inject(function ($rootScope, $controller, $httpBackend, _$location_, $injector, MattersDataService,
        DataSetsDataService, ProjectsDataService, AlertService) {
        httpBackend = $httpBackend;
        scope = $rootScope.$new();

        //$httpBackend.whenGET("/api/matters").passThrough();
        $httpBackend.whenGET(/api\/.*/).passThrough();

        controller = $controller('SampleListCtrl', {
            $scope: scope,
            $location: _$location_,
            MattersDataService: MattersDataService,
            DataSetsDataService: DataSetsDataService,
            ProjectsDataService: ProjectsDataService,
            AlertService: AlertService
        });

        //scope.$digest resolves the promise against the httpbackend
        scope.$digest();
        //httpbackend.flush() resolves all request against the httpbackend
        //to fake a async response, (which is what happens on a real setup)
        httpBackend.flush();
    }));

    it('should have scope defined', function () {
        expect(scope).toBeDefined();
    });

    it('should have a page title', function () {
        expect(scope.pagetitle.length).toBeGreaterThan(5);
    });

    it('should have a breadcrumbs', function () {
        expect(scope.breadcrumbs.length).toBe(2);
    });

    it('should have matters', function () {
        expect(scope.matters.length).toBeGreaterThan(0);
    });

    it('should have datasets', function () {
        expect(scope.datasets.length).toBeGreaterThan(0);
    });

    it('should allow resetting the view', function () {
        scope.clear();
        expect(scope.selectedmatter).toBeNull();
        expect(scope.selecteddataset).toBeNull();
        expect(scope.gridProjectsConfig.dataSource.page).toBe(1);
    });

    it('should allow deleting a project', function () {
        controller.deleteProject(1);
        //todo
    });

    it('should editing a project', function () {
        controller.deleteProject(1);
        //todo
    });

});