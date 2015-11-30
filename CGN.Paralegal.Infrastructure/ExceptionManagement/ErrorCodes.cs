//---------------------------------------------------------------------------------------------------
// <copyright file="ErrorCodes.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil V</author>
//      <description>
//          This file contains the ErrorCodes class.
//      </description>
//      <changelog>
//          <date value="07/03/2010"></date>
//          <date value="25-Mar-2011">Error codes added for CaseMap</date>
//          <date value="05/12/2015">Added error codes for special characters check in Thesaurus, Stopword, Spelling variations</date>
//          <date value="12-05-2011">Added error codes for policy check in update field</date>
//          <date value="16-05-2011">Added error codes for dataset name special character validation</date>
//          <date value="26-07-2011">Added error codes for folder service error</date>
//          <date value="07/28/2015">Added error codes for document description while creating fields</date>
//          <date value="02-08-2011">Bugs Fixed #88553</date>
//          <date value="06-09-2011">Bugs Fixed #85239</date>
//          <date value="09/07/2015">Fix for bugs 88775, 88777, 88778</date>
//          <date value="14/09/2015">Bug fix #89070</date>
//          <date value="10/31/2015">added policy check for family documents</date>
//	        <date value="12-19-2011">Bug Fix #81330</date>
//	        <date value="1-19-2012">Task 95032 </date>
//          <date value="01/16/2012">87015 bug fixed</date>
//          <date value="20-Mar-2012">Fix for bug 97973</date>
//          <date value="03/29/2012">Bug fix for 98332</date>
//          <date value="04/02/2012">Bug fix for 98615</date>
//          <date value="11/04/2012">94842 bug fixed</date>
//          <date value="12/04/2012">98580 bug fixed</date>
//	        <date value="04/12/2012">Bug Fix for 98771 </date>
//          <date value="04/19/2012">Bug Fix 98566</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/26/2012">babugx : Task# 102781 - Velocity failover matter creation</date>
//          <date value="04/09/2012">Applied new policy changes - 108300</date>
//          <date value="09/25/2012">Organization Level Security  # 109025 and 109027</date>
//          <date value="08/10/2012">Folder Permission Changes - 109809</date>
//          <date value="04/10/2012">Task # 109811:ADM-SECURITY-002 - UI and Services - Advanced Permissions Tool</date>
//          <date value="07/11/2012"> Task # Fix # 112378 REV-REVIEWER SIDE NAVIGATION -001: Add Recently visit Reviewset by Users in Review Global Menu</date>
//          <date value="04-12-2012">Bug Fix For 114634 and 114635</date>
//          <date value="01-9-2013">Bug Fix For 113664</date>
//          <date value="01/31/2013">Fix for bug 127223</date>
//          <date value="02/24/2013">Bug fix #131420</date>
//          <date value="04/06/2013">ADM -ADMIN-002 -  Near Native Conversion Priority</date>
//          <date value="04/18/2013">CHEV 2.2.1 - ADM-LICENSE-002 - Implementation</date>
//          <date value="27-Oct-2012">Bug  # 126345 - Throwing the error message "Webpage couldn't be displayed" while clicking on Save button in create Organization/Dataset/Servers/UserGroup</date>
//          <date value="05/08/2013">CHEV2.2.1 - Defect# 137751, 137748, 137724, 137701, 137744, 127575 Licensing fix : babugx</date>
//          <date value="05-12-2013">Task # 134432-ADM 03 -Re Convresion</date>
//          <date value="07-17-2013">Bug # 147760 - Fix to tag documents in the manage conversion
//          <date value="07/17/2013">Bug Fix # 147755, 147758</date>
//          <date value="08/06/2013">Binary Externalization Implementation</date>
//          <date value="08/13/2012">Bug Fix # 149498: [MTS CERT 250K and 1300 K]Documents Mismatch Between Velocity Count and Dataset Dashboard Count in 250K data import.</date>
//          <date value="09/16/2013">Task # 150531 -ADM -REVIEWSET - 002 - Review sets & Binders - Back End - Required Binder Services for the reviewer dashboard                page -  BinderService.GetReviewSetsForABinder()
//          <date value="09/23/2013">Task # 150531 -ADM -REVIEWSET - 002 - All Back End Services For Reviewer Dashboard
//          <date value="09/26/2013">Task # 150468 -ADM -REVIEWSET - 002 -  Check  Out ,Release And Complete Fixes
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="10/09/2013">Dev Bug # 153705 -ReviewerDashboard click complete link should generate BulkTag audit log
//          <date value="10/17/2013">Bug # 155220 &155337  - Fix to get the xdl for single page production with more than 16 pages and capture the xdl file missing and unknown conversion errors 
//          <date value="11/06/2013">Bug fix 156594, 156963</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
namespace CGN.Paralegal.Infrastructure.ExceptionManagement
{
    /// <summary>
    /// Consists of error codes to display user friendly error messages.
    /// </summary>
    public static class ErrorCodes
    {
        public const string UnauthorizedErrorId = "501";
        public const string UnauthorizedError = "401";
        public const string UnknownError = "911";


        #region General Errors - Range: 600
        public const string GeneralException = "600";
        public const string UserSessionExpired = "601";
        public const string MessageValidation = "1003";
        public const string HttpContentSerializeException = "1004";
        public const string SqlConnectionErrorId = "602";
        public const string AccessDeniedForFeature = "1001";
        #endregion


        #region Deduplication Error Codes - (9050-9100)

        public const string ScheduledDEDuplicateErrorId = "9051";
        public const string AccessDeniedForDeduplication = "9052";
        #endregion

        #region Comments Error Codes - (700 - 750)
        public const string DocumentLevelCommentsNotAllowed = "701";

        public const string TextLevelCommentsNotAllowed = "703";

        public const string DeleteCommentsNotAllowed = "707";
        public const string ErrorWhileDeletingComments = "708";
        public const string AddComment = "723";
        public const string UpdateComment = "724";
        public const string GetComment = "725";
        public const string EditCommentpermissionDenied = "726";
        public const string AddCommentPermissionDenied = "727";
        public const string GetCommentPermissionDenied = "728";
        #endregion

        #region Document Viewer - (801 - 1100)

        public const string GetDocumentDataErrorId = "801";
        public const string NativeFileBinaryNotFound = "807";
        public const string ErrorNativeViewer = "809";
        public const string FileAccessDenied = "810";
        public const string DocumentHistoryServiceError = "812";
        public const string AccessDeniedForThreadViewer = "813";
        public const string AccessDeniedToReadFile = "814";
        public const string ErrorInReadingTheFile = "815";
        public const string TextFileNotFound = "816";
        public const string TextFileLarge = "817";
        public const string FailureForAddDocuments = "818";
        #endregion

        #region Policy Management Error Codes - (1300 - 1399)
        public const string RoleSystemErrId = "1300";
        public const string RoleAlreadyExistsErrorId = "1302";
        public const string InvalidRoleIdErrorId = "1303";
        public const string InvalidFolderIdErrorId = "1304";
        public const string InvalidUserGroupIdErrorId = "1305";
        public const string GetPolicyGroupServiceErrId = "1306";
        public const string GetFolderUserGroupsServiceErrId = "1308";
        public const string UpdateFolderUserGroupRoleServiceErrId = "1309";
        public const string InsertUserGroupFolderServiceErrId = "1310";
        public const string CreateRoleErrId = "1311";
        public const string BindRolesErrId = "1313";
        public const string UpdateUserGroupFolderPoliciesServiceErrId = "1314";
        public const string GetRoleServiceErrId = "1315";
        public const string CreateRoleServiceErrId = "1316";
        public const string UpdateRoleServiceErrId = "1317";
        public const string DeleteRoleServiceErrId = "1318";
        public const string GetRolePolicyServiceErrId = "1319";
        public const string GetUserGroupFolderRolesServiceErrId = "1320";
        public const string GetUserGroupFolderPoliciesServiceErrId = "1321";
        public const string CreateFolderUserGroupErrId = "1322";
        public const string FetchUserGroupPoliciesErrId = "1327";
        public const string InvalidPolicyGroupIdErrorId = "1328";
        public const string PolicySystemErrorId = "1329";
        public const string DeleteUserGroupFromFolderErrorId = "1330";
        public const string RoleNameValidationErrorId = "1350";
        public const string RoleIdValidationErrorId = "1349";
        public const string SystemRoleValidationErrorId = "1351";
        public const string BulkUpdateRolesServiceErrorId = "1352";


        #endregion

        #region Folder Management Error Constants - (1101 - 1199)
        public const string FolderAlreadyExistsErrorId = "1130";
        public const string FolderCreateServiceErrorId = "1101";
        public const string FolderUpdateServiceErrorId = "1102";
        public const string FolderDeleteServiceErrorId = "1103";
        public const string FolderFetchAuthorizedFoldersServiceErrorId = "1105";
        public const string FolderFetchFolderPropertiesServiceErrorId = "1107";
        public const string FolderFetchParentFolderServiceErrorId = "1108";
        public const string FolderFetchChildFoldersServiceErrorId = "1109";
        public const string CreateSessionDataError = "1112";
        public const string GetSessionDataError = "1113";
        public const string DeleteSessionDataError = "1114";
        public const string UserNotExistsForSessionData = "1115";
        public const string InValidOrganizationNameError = "1123";
        public const string InValidFolderNameError = "1124";
        #endregion

        #region Dataset Management Error Constants - (4000 - 4099)

        public const string TemplateAlreadyExists = "4005";
        public const string GetTemplatesError = "4006";
        public const string CreateDataSetError = "4008";
        public const string UpdateDataSetServiceErrorId = "4009";
        public const string CreateTemplateServiceErrorId = "4010";
        public const string UpdateTemplateServiceErrorId = "4011";
        public const string FetchDataSetDetailForFolderServiceErrorId = "4012";
        public const string GetDataSetDetailForDataSetServiceErrorId = "4013";
        public const string GetTemplatesForTemplateIdServiceErrorId = "4014";
        public const string GetDataTypeAndDataFormatListServiceErrorId = "4015";
        public const string DataSetCreateErrorId = "4016";
        public const string FieldCreateErrorId = "4017";
        public const string FieldUpdateErrorId = "4018";
        public const string FieldDeleteErrorId = "4019";
        public const string TemplateUpdateErrorId = "4020";
        public const string CreateDataSetAccessDeniedErrorId = "4021";
        public const string ModifyDataSetAccessDeniedErrorId = "4022";
        public const string ViewDataSetAccessDeniedErrorId = "4023";
        public const string DataSetUpdateErrorId = "4024";
        public const string GetDataSetDetailErrorId = "4025";
        public const string GetDataSetTemplateListErrorId = "4027";
        public const string GetTemplateDetailErrorId = "4028";
        public const string GetDataSetListErrorId = "4029";
        public const string GetDataTypeListErrorId = "4030";
        public const string GetFieldListErrorId = "4031";
        public const string DataSetBatesNumberNotUniqueErrorId = "4032";
        public const string GetDataSetDetailForCollectionIdError = "4033";
        public const string DataSetDataSetDeleted = "4034";
        //Bug 70814
        public const string DataSetFieldAlreadyExists = "4035";
        //**
        public const string DataSetDeleteJobEx = "4036";
        public const string FieldGetAllDefintionsError = "4037";
        public const string FieldGetDefaultFieldPoliciesError = "4038";
        public const string FieldGetFieldPoliciesForPrincipal = "4039";
        public const string FieldGetEffectiveFieldPermissionsForPrincipal = "4040";
        public const string FieldGetEffectiveFieldPermissions = "4041";
        public const string FieldModifyFieldPolicies = "4042";
        public const string FieldRemoveFieldPolicies = "4043";
        public const string SingleTitleFieldAllowed = "4044";
        public const string SingleContentFieldAllowed = "4045";
        public const string SingleDocuDescFieldAllowed = "4046";
        public const string SingleReasonCodeFieldAllowed = "4047";
        public const string TemplateNameError = "4050";
        public const string DataSetNotExists = "4052";
        public const string DataSetDeleted = "4053";
        public const string GetAllActiveDependentJobsError = "4052";
        public const string SearchKeywordsUsedError = "4055";
        public const string SearchKeywordUsedInFieldName = "4057";
        public const string PermissionDeniedToDelete = "4058";
        public const string TextFieldTypeLengthError = "4059";
        public const string UpdateSystemFieldError = "4060";
        public const string UpdateFieldPolicyError = "4061";
        public const string ReasonCodeSameDataset = "4062";
        public const string DeleteFieldPolicyError = "4063";
        public const string EVFieldTypeDefaultLengthErrorMessage = "4064";
        public const string EVFieldSingleEntryDuplicateErrorMessage = "4065";
        public const string EVFieldNameEmptyErrorMessage = "4066";
        public const string EvDcnNameShouldBeProvided = "4067";
        public const string EVDcnName_SystemFieldNameValidation = "4068";
        public const string DCNStartingNumberCannotBeEmpty = "4069";
        public const string CompressedFileExtractionLocationCannotBeEmpty = "4070";
        public const string CompressedFileExtractionLocationLimit = "4081";
        public const string BatesNumberStartWithMustBeNumeric = "4071";
        public const string UserCannotCreateDCN = "4073";
        public const string DCNPrefixCannotBeEmpty = "4074";
        public const string SingleEntryUniqueValueError = "4075";
        public const string DocumentDescriptioneSameDataset = "4076";
        public const string GetClusterStatus = "4077";
        public const string UpdateClusterStatus = "4078";
        public const string InvalidDCNValueObtainedForDataset = "4079";
        public const string DataSetDoesNotExists = "4082";
        public const string InvalidCompressedFileExtractionLocation = "4083";
       

        public const string ShareNameAlreadyExists = "4084";
        public const string ErrorwhilemappingTheShare = "4085";
        public const string ErrorwhileDeletingTheShare = "4086";
        public const string ErrorwhilUpdatingTheShare = "4087";
        public const string ErrorwhilGettingTheShare = "4088";
        public const string SystemFieldNameValidation = "4089";
        public const string ErrorInReordering = "4090";
        public const string AlreadyUseInSystemFieldError = "4091";
        #endregion


        #region Server Management Error Constants - (4100 - 4199)

        public const string CreateServerServiceErrorId = "4100";
        public const string UpdateServerServiceErrorId = "4101";
        public const string DeleteServerServiceErrorId = "4102";
        public const string FetchServerServiceErrorId = "4103";
        public const string FetchServerByTypeServiceErrorId = "4104";
        public const string InsertServerFailed = "4105";
        public const string UpdateServerFailed = "4106";
        public const string AccessDeniedForServerManagement = "4107";
        public const string ModifyServerAccessDeniedErrorId = "4108";
        public const string RemoveServerAccessDeniedErrorId = "4109";
        public const string DeleteServerFailed = "4111";
        public const string ViewServerAccessDeniedErrorId = "4114";
        public const string TestServerAccessDeniedErrorId = "4115";
        public const string ServerDuplicateServerErrorId = "4116";
        public const string ServerConnectionFailed = "4119";
        public const string ServerIdNotExists = "4125";
        public const string ServerNotExists = "4126";
        public const string EntityValidationValueType = "4128";
        public const string SearchSvcStart = "4140";
        public const string SearchSvcStop = "4141";
        public const string StartStopServiceErrorId = "4142";
        public const string SearchServerDownErrorId = "4143";
        public const string SearchIndexFailureErrorId = "4144";
        #endregion

        #region Matter Management Error and Exception Constants - (3000 - 3100)

        public const string MatterCreateServiceErrorId = "3001";
        public const string MatterUpdateServiceErrorId = "3002";
        public const string MatterFetchMatterDetailsServiceErrorId = "3003";
        public const string CreateMatterAccessDeniedErrorId = "3005";
        public const string UpdateMatterAccessDeniedErrorId = "3006";
        public const string ServerAccessDeniedErrorId = "3008";
        public const string MatterFetchMatterListServiceErrorId = "3009";
        public const string InvalidParentIdError = "3010";
        public const string MatterCreateSearchSubSystemErrorId = "3011";

        #endregion

        #region Job Management - (1600 - 1699)

        public const string JobUpdateServiceErrorId = "1601";
        public const string JobDeleteServiceErrorId = "1602";
        public const string JobFetchAllJobsServiceErrorId = "1603";
        public const string JobFetchJobPropertiesServiceErrorId = "1604";
        public const string JobCreateServiceErrorId = "1606";
        public const string JobCreateJobFailed = "1610";
        public const string JobUpdateJobFailed = "1611";
        public const string JobDeleteJobFailed = "1612";
        public const string JobJobNameEmpty = "1614";
        public const string JobJobTypeEmpty = "1615";
        public const string JobNotificationTypeEmpty = "1616";
        public const string JobFolderIdEmpty = "1617";
        public const string JobJobScheduleTypeEmpty = "1618";
        public const string JobUserListEmpty = "1620";
        public const string JobRequestedRecordedCountZero = "1621";
        public const string JobPastScheduleStartDate = "1622";
        public const string JobNameAlreadyExists = "1623";
        public const string JobCompleted = "1624";
        public const string JobInvalidType = "1625";
        public const string JobFolderIdShouldBeDataset = "1626";
        public const string JobFolderIdShouldBeDataSetOrMatter = "1627";
        public const string AccessDeniedForJobManagement = "1628";
        public const string InvalidParametersJobs = "1629";
        public const string JobAccessDeniedtoViewJobSummery = "1630";
        public const string JobAccessDeniedtoSetJobPriority = "1631";
        public const string JobAccessDeniedtoViewLogfile = "1632";
        public const string JobNoJobsAvailable = "1633";
        public const string OperationDoesNotMatch = "1634";
        public const string ProblemInDoAtomicWork = "1640";
        public const string ProblemInGenerateTasks = "1641";
        public const string ProblemInJobInitialization = "1642";
        public const string ProblemInJobExecution = "1643";
        public const string AccessDeniedForUpdatingJobProperties = "1635";
        public const string AccessDeniedForDeletingJob = "1636";
        public const string AccessDeniedForExport = "1637";
        public const string ProblemInShutDown = "1645";
        public const string InvalidJobId = "1646";
        public const string JobAlreadyCancelled = "1647";
        public const string JobRunningState = "1648";
        public const string JobAlreadyPaused = "1649";
        public const string JobIsInScheduleState = "1650";
        public const string AccessDeniedToCreateClusterJob = "1651";
        public const string AccessDeniedToDeleteClusterJob = "1652";
        public const string AccessDeniedToViewClusterJob = "1653";
        public const string AccessDeniedToCancelClusterJob = "1654";
        public const string JobImportClusterRunningErrorID = "1655";
        public const string JobAlreadyCompleted = "1656";
        public const string EnterValidJobNameFolderIDJobTypeID = "1657";
        public const string HasInProgressJobErrorCode = "1658";
        public const string NearDuplicationJobInProgressJobErrorCode = "1660";
        public const string AnalyticsDeleteProjectErrorCode = "1600";
        public const string AnalyticsCreateProjectJobErrorCode = "1661";
        public const string AnalyticsUpdateProjectErrorCode = "1662";
        public const string AnalyticsCreatControlsetErrorCode = "1689";
        public const string GetAvailableDocsCountInProjectErrorCode = "1682";
        public const string CreateQcSetErrorCode = "1683";
        public const string AnalyticsCalculateSampleSizeErrorCode = "1690";
        public const string AnalyticsScheduleJobFoExportAnalysissetErrorCode = "1691";
        public const string AnalyticsCreatTrainingsetErrorCode = "1698";
        public const string AnalyticsScheduleJobForCategorizeControlsetErrorCode = "1679";
        public const string AnalyticsScheduleJobForCategorizeAllErrorCode = "1712";
        public const string AnalyticsValidateCreateProjectInfoErrorCode = "1709";

        public const string AnalyticsCreateJobForAddAdditionalDocuments = "1717";

        #endregion

        #region Imports - (2500 - 2599)
        public const string ImportsDataNotFoundErrorId = "2501";
        public const string ImpXmlFormatErrorId = "2502";
        public const string ScheduledImportErrorId = "2506";
        public const string ImportElectronicDataFileNotFoundErrorId = "2508";
        public const string ImportElectronicDataFileParseErrorId = "2509";
        public const string ImportGetProfilesErrorId = "2510";
        public const string ImportSaveProfileErrorId = "2512";
        public const string ImportSaveExistingProfileErrorId = "2513";
        public const string ImportGetJobDetailsErrorId = "2514";
        public const string ImportGetJobListErrorId = "2515";
        public const string ImportGetDetailsErrorId = "2518";
        public const string ImportErrorTestLogRun = "2519";
        public const string ImportsGetNoOfDocumentsErrorId = "2524";
        public const string ImportGetProfileConfigurationErrorId = "2528";
        public const string ReadDocumentFromPath = "2529";
        public const string ImportsFieldsFileNotGenerated = "2530";
        public const string ImportsUnableToParseDcbFile = "2531";
        public const string ImportsUnableToGetSourceFields = "2532";
        public const string ImportsNoJobLogFound = "2533";
        public const string ImportsLoadFilePreview = "2534";
        public const string ImportsLoadFileDelimiterSet = "2535";
        public const string ImportsLoadImportValidateHelperFile = "2537";
        public const string ImportsLoadTestPath = "2538";
        public const string ImportsLoadPathSubstitutionField = "2539";
        public const string ImportsNonrelatedLoadFile = "2540";
        public const string ImportsLoadFilePathNonExistance = "2541";
        public const string ImportsLoadFileNoRecords = "2542";
        public const string ImportsLoadFileLogFileNotExists = "2543";
        public const string ImportsLoadFileInvalidLogFile = "2544";
        public const string ImportsLoadFileNoProfile = "2546";
        public const string ImportsLoadFileFolderNonExistance = "2547";
        public const string ImportsLoadFileImageSetExists = "2548";
        public const string ImportsLoadFileImageSetNotExists = "2549";
        public const string ImportsLoadFileDataSetNotExists = "2550";
        public const string ImportsLoadFileProfileExists = "2551";
        public const string ImportsLoadFileErrGetSource = "2552";
        public const string ImportsOverlayDefault = "2553";
        public const string ImportScheduleMappedFieldsNotBelongsToDataset = "2554";
        public const string ImportScheduleImportNowPropertiesAssign = "2555";
        public const string ImportInvalidJobTypePropertiesAssign = "2556";
        public const string ImportMandatoryFieldsMissing = "2557";
        public const string ImportFolderPolicy = "2558";
        public const string ImportType = "2559";
        public const string ImportNoProfileAvailable = "2560";
        public const string ImportNoJobNameAvailable = "2561";
        public const string ImportsDcbFailureOnGetSourceFields = "2562";
        public const string DeleteDocumentFailedinConversionQueue = "2563";
        public const string ImportDiskFullErrorMessage = "2564";
        public const string ImportLoadImportReadHelperFile = "2565";
        #endregion

        #region Admin  Search (1700 - 1799)

        public const string GetSearchCategoriesErrorId = "1701";
        public const string GetSearchResultsErrorId = "1702";
        public const string GetSearchResultSetErrorId = "1703";
        public const string PopulateCategoriesErrorId = "1704";
        public const string PopulateSearchResultsErrorId = "1705";
        public const string InvalidParameter = "1706";
        public const string UnaccomplishedSearchResultErrorId = "1707";
        public const string NotAbleToConnectToSearchEngine = "1708";

        #endregion

        #region Group By (7300 - 7310)
        public const string AccessDeniedGettingGroupByResults = "7300";
        public const string ErrorRetrievingGroupByResults = "7301";
        #endregion

        #region Reviewer  Search (1800 - 1899)
        public const string GetTagsErrorId = "1801";
        public const string GetSearchFieldsErrorId = "1802";
        public const string GetSearchFileTypesErrorId = "1803";
        public const string GetDocumentPreviewDataErrorId = "1804";

        public const string SearchDocumentsAccessDeniedErrorId = "1812";
        public const string ConceptSearchAccessDeniedErrorId = "1819";

        public const string SearchHistoryNotSavedErrorId = "1840";
        public const string SavedSearchNotSavedErrorId = "1842";
        public const string SavedSearchNotDeletedErrorId = "1843";
        public const string SavedSearchNotModifiedErrorId = "1844";
        public const string SavedSearchNotRetrievedErrorId = "1845";
        public const string SearchAlertNotModifiedErrorId = "1848";

        public const string SearchHistoryNotDeletedErrorId = "1851";
        public const string SavedSearchAlreadyExistsErrorId = "1852";
        public const string SearchAlertAlreadyExistsSavedErrorId = "1853";
        public const string SearchHistoryCreateServiceErrorId = "1854";
        public const string SearchHistoryDeleteServiceErrorId = "1856";
        public const string SavedSearchCreateServiceErrorId = "1857";
        public const string SavedSearchUpdateServiceErrorId = "1858";
        public const string SavedSearchDeleteServiceErrorId = "1859";
        public const string SearchAlertCreateServiceErrorId = "1860";
        public const string SearchAlertUpdateServiceErrorId = "1861";
        public const string SearchAlertDeleteServiceErrorId = "1862";
        public const string SearchHistoryGetServiceErrorId = "1863";
        public const string SavedSearchGetServiceErrorId = "1864";
        public const string SearchAlertGetServiceErrorId = "1865";
        public const string SearchAlertGetPreviousServiceErrorId = "1866";
        public const string ErrorWhileDeletingTheDocuments = "1867";
        public const string ViewSearchHistoryDeniedError = "1869";
        public const string CreateSavedSearchDeniedError = "1870";
        public const string ModifySavedSearchDeniedError = "1871";
        public const string ViewSavedSearchDeniedError = "1872";
        public const string ExecuteSavedSearchDeniedError = "1873";
        public const string CreateSearchAlertDeniedError = "1874";
        public const string FieldDeniedError = "1875";
        public const string EmptyValue = "1876";
        public const string UpdateRedactIt = "1877";
        public const string FetchReferencedId = "1878";
        public const string ValueCantEmpty = "1879";

        public const string InvalidAlertSource = "1880";
        public const string DatasetIdentifierNotSpecified = "1882";
        public const string PageSizeInvalid = "1883";
        public const string MatterIdentifierNotSpecified = "1884";
        public const string FamilyIdentifierError = "1887";
        public const string SearchObjectEmptyError = "1888";
        public const string QueryListEmptyError = "1889";
        public const string ReConvertErrorCode = "1890";
        public const string RedactItError = "1891";
        public const string RedactItConversionFailure = "1893";
        public const string RedactItXdlFileMissing = "1892";
        public const string UnableToInsertXdl = "1894";
        public const string GetSelectedDocCountError = "1895";
        #endregion

        #region NotificationManagement - (4200 - 4299)
        public const string CreateNotificationErrorId = "4201";
        public const string UpdateNotificationErrorId = "4202";
        public const string GetAllNotificationForUserErrorId = "4205";
        public const string NotificationSubscriptionMandatoryErrorId = "4209";
        public const string CreateNotificationMessageErrorId = "4210";
        public const string GetAllNotificationMessageErrorId = "4211";
        public const string UpdateUserNotificationErrorId = "4215";
        public const string DeleteNotificationMessageForUserErrorId = "4216";
        public const string DeleteSendMessageErrorId = "4217";
        public const string EvolutionUserGetAllGroupError = "2006";
        public const string GetDomainName = "4218";
        public const string SendCustomMessageErrorId = "4220";
        public const string SendMessageThruEmailError = "4221";
        public const string NavigateUrlSend = "4222";
        public const string EvolutionNotificationDataAlreadyExistsErrorId = "4225";
        public const string EvolutionNotificationDataDeleteNotExistErrorId = "4226";
        public const string EvolutionNotificationDataDeleteErrorId = "4227";
        public const string EvolutionNotificationDataCreateErrorId = "4229";
        public const string AccessDeniedForNotification = "4230";
        public const string ViewNotificationAccessDeniedErrorId = "4231";
        public const string DeleteNotificationAccessDeniedErrorId = "4232";
        public const string CreateSubscriptionForUserErrorId = "4233";
        public const string NotificationDataDeleteSubscriptionErrorId = "4234";
        public const string UpdateSubscriptionForUserErrorId = "4235";
        public const string GetSubscriptionForUserErrorId = "4236";
        public const string NavigateUrlViewNotification = "4237";
        public const string NavigateUrlManageSubscription = "4238";
        public const string NavigateUrlManageNotification = "4239";
        public const string NotificationInfo = "4240";
        public const string NavigateUrlAddSubscription = "4242";
        public const string SmtpServerUNAvailableError = "4244";
        public const string SubscriptionNotExists = "4245";
        public const string InvalidFolderId = "4246";
        public const string RecipientsMandatory = "4247";
        public const string MessageMandatory = "4248";
        public const string InvalidUser = "4249";
        #endregion

        #region Centralized Configuration Management - (4300 - 4399)
        public const string FetchCmgServicesServiceErrorId = "4301";
        public const string FetchCmgInstanceConfigurationsErrorId = "4302";
        public const string UpdateCmgInstanceConfigurationsErrorId = "4303";
        public const string AddCmgInstanceServiceErrorId = "4304";
        public const string DeleteCmgInstanceServiceErrorId = "4305";
        public const string FetchCmgServersErrorId = "4306";
        public const string FetchCmgGeneralConfigServiceErrorId = "4307";
        public const string UpdateCmgGeneralConfigServiceErrorId = "4308";
        public const string AddCmgGeneralConfigServiceErrorId = "4309";
        public const string StopOrStartCmgServicesServiceErrorId = "4310";
        public const string FetchCmgServicesStatusErrorId = "4311";
        public const string AddServiceInstanceErrorId = "4312";
        public const string DeleteCmgServiceInstanceErrorId = "4313";
        public const string FetchCmgServiceConfigurationsErrorId = "4314";
        public const string ErrorInvalidServiceId = "4315";
        #endregion

        
        #region EVLogs - (4400 - 4499)
        public const string FetchNLogAnHourDataServiceErrorId = "4400";
        public const string FetchNLogDataServiceErrorId = "4401";
        public const string FetchAllNLogDataServiceErrorId = "4402";
        public const string FetchAllRelatedNLogDataServiceErrorId = "4403";

        #endregion

        #region Audit Management- (5000- 5050)
        public const string GetEventTypesErrorId = "5004";
        public const string NoRowsErrorId = "5005";
        public const string AccessDeniedToViewAuditLog = "5006";
        #endregion

        #region API Error Constants
        public const string InvalidEventTypeId = "5011";
        public const string InvalidStartDate = "5012";
        public const string InvalidEndDate = "5013";
        public const string DateExceeds = "5014";

        public const string InvalidPageNo = "5015";
        public const string InvalidRecordSize = "5016";
        public const string EventTypeIdRequired = "5017";
        public const string StartDateRequired = "5018";
        public const string EndDateRequired = "5019";
        public const string AddADdomain = "4131";
        public const string EditADdomain = "4132";
        public const string AddADServer = "4133";
        public const string EditADServer = "4134";
        #endregion

        #region Tags Error Constants - (5501 - 5700)
        public const string LoadTagErrorId = "5501";
        public const string CreateTagErrorId = "5502";
        public const string UpdateTagErrorId = "5503";
        public const string GetTagErrorId = "5504";
        public const string DeleteTagErrorId = "5505";
        public const string CreateTagTemplateErrorId = "5506";
        public const string GetTagTemplateErrorId = "5507";
        public const string InvalidTagTemplateIdErrorId = "5508";
        public const string UpdateTagTemplateErrorId = "5509";
        public const string TaggingDocumentErrorId = "5510";
        public const string GetTagFamilyErrorId = "5511";
        public const string GetTagBehaviorErrorId = "5512";
        public const string ParentTagScope = "5515";
        public const string FolderParentTag = "5516";
        public const string GroupParentTag = "5517";
        public const string TagFixed = "5519";
        public const string TagLocked = "5520";
        public const string TagUnique = "5521";
        public const string TagCannotBeDeleted = "5522";
        public const string DuplicateShortcutKey = "5523";
        public const string FolderShortcutKey = "5524";
        public const string GetTagsInDocumentErrorId = "5527";
        public const string TagExists = "5528";
        public const string AccessDeniedForTagManagement = "5531";
        public const string DenyDeleteTag = "5532";
        public const string DenyViewTag = "5533";
        public const string DenyBulkTagging = "5535";
        public const string DenyTagFamilyAndGroup = "5537";
        public const string TagTemplateAlreadyExists = "5538";
        public const string TagNameEmpty = "5539";
        public const string FolderGroupTagNameExists = "5540";
        public const string InvalidShortcutKeys = "5541";
        public const string TypeNotHaveShortcutKey = "5542";
        public const string InvalidScopeShortcutKey = "5543";
        public const string GetTagPolicyErrorId = "5544";
        public const string UpdatetagPoliciesErrorId = "5546";
        public const string DeleteTagPoliciesErrorId = "5547";
        public const string AccessDeniedForTagging = "5549";
        public const string DenyTagCreateFromTemplate = "5550";
        public const string DenyViewTagHistory = "5551";
        public const string DenyViewTagStatistics = "5552";
        public const string InvalidTagType = "5553";
        public const string InvalidTagId = "5554";
        public const string InvalidCollectionId = "5555";
        public const string InvalidMatterId = "5556";
        public const string TagNotAvailable = "5557";
        public const string BulkTagJobExistsForTag = "5558";
        #endregion

        #region Near Native Error Constants
        public const string SaveMarkup = "6001";
        public const string CallPushUri = "6004";
        public const string SaveNearNativeBinary = "6005";
        #endregion

        #region Vault/Search Management- (5060- 5500)
        public const string VaultConnectionCreateError = "5066";
        public const string PartialDocumentImport = "5067";
        public const string BulkDocumentInsertError = "5069";
        public const string BulkInsertNoFieldsAvailable = "5070";
        public const string BulkInsertNoDocumentsAvailable = "5071";
        public const string VaultWorker_DocumentCollectionEmpty = "5080";
        public const string VaultWorker_DatasetInformationIsEmpty = "5081";
        public const string VaultWorker_DocumentHashmapAddFailure = "5083";
        public const string VaultWorker_DocumentHashmapUpdateFailure = "5084";
        public const string VaultWorker_DocumentHashmapDeleteforUpdateFailure = "5085";
        public const string VaultWorkerDocumentFieldsInsertFailure = "5087";
        public const string VaultWorkerDocumentTextInsertFailure = "5086";
        #endregion

        #region ReviewSet - (1900- 1999)

        public const string AddDataSetRulesError = "1901";
        public const string AddDocumentsToReviewSetError = "1902";
        public const string CreateReviewSetError = "1904";
        public const string DeleteReviewSetError = "1905";
        public const string DeleteDocumentsFromReviewSetError = "1906";
        public const string SplitReviewSetError = "1908";
        public const string UpdateReviewSetError = "1909";
        public const string RemoveDocumentsFromReviewSetError = "1910";
        public const string GetReviewSetDetailsError = "1911";
        public const string GetDataSetRulesError = "1912";
        public const string GetAllReviewSetForDataSetError = "1913";
        public const string GetAllReviewSetWithDataSetError = "1914";
        public const string GetAllDocumentsForDataSetError = "1915";
        public const string NameAlreadyExistsError = "1916";
        public const string GetCheckedOutReviewSetsError = "1926";
        public const string InvalidDatasetError = "1927";
        public const string InvalidUserIdError = "1928";
        public const string RecentReviewsetError = "1929";
        public const string UpdateReviewSetVisit="1930";

        #endregion

        # region Preference (3200 - 3249)

        public const string GetPreferenceForFolderErrorId = "3200";
        public const string GetPreferenceForUserErrorId = "3201";
        public const string GetPreferenceForUserGroupErrorId = "3202";
        public const string AssignPreferenceToFolderErrorId = "3203";
        public const string AssignPreferenceToUserErrorId = "3204";
        public const string AssignPreferenceToUserGroupErrorId = "3205";
        public const string GetAllPreferencesErrorId = "3206";
        public const string AccessDeniedForApplyPreference = "3208";
        #endregion

        #region Document (101-200)
        public const string DocumentGetMasterData = "101";
        public const string DocumentInsert = "102";
        public const string ErrorGlobalReplaceSchedule = "104";
        public const string ErrorFetchDocumentInfoForTitle = "111";
        public const string DeleteDocument = "107";
        public const string AddDocumentBinary = "108";
        public const string DocumentControlNumberExhausted = "109";
        public const string ErrorBulkTaggingSchedule = "113";
        public const string ErrorGetDocumentLockStatus = "114";
        public const string NullDocumentObjectWhileUpdatingContent = "116";
        public const string NullDocumentContentWhileUpdatingContent = "117";
        public const string OriginalDocumentUnavailable = "118";
        public const string SystemFields = "119";
        public const string DocumentDeleteAccessDenied = "120";
        public const string ErrorBulkDeleteSchedule = "121";
        public const string DocumentTextCount = "125";
        public const string CreateDocumentMasterError = "126";
        public const string GenericErrorAddingDocumentMetadata = "127";
        public const string VaultDeleteRelationshipFailure = "128";
        public const string GenericAddDocumentBulkError = "130";
        public const string TextFilesNotInDocumentEntity = "133";
        public const string GenericErrorAddDocumentTextToVault = "134";
        public const string GenericAddDocumentMasterRecordError = "135";
        public const string GenericAddDocumentFieldsError = "136";
        public const string RedactItPushFailure = "138";
        public const string GenericRedactItCallPushFailure = "139";
        public const string NoFilesWithDocumentForConversion = "140";
        public const string GenericErrorPushDocumentForConversion = "141";
        public const string ErrorCreatingDestinationUrlForConversion = "142";
        public const string NearNativeConversionAdapaterInitializeFailure = "143";
        public const string NoDocumentsInConversionWorker = "144";
        public const string DocumentUpdatedEarlier = "145";
        public const string DocumentReConvertSubmissionFailure = "146";
        public const string AccessDeniedForReConversionSubimmsion = "147";
        public const string ReConversionNoNativeFileFound = "148";
        #endregion

        #region "User(1200-1299)"
        public const string CreateUserErrorId = "1201";
        public const string DuplicateUserId = "1202";
        public const string UpdateUserErrorId = "1204";
        public const string UserNotExist = "1205";
        public const string DeleteUserErrorId = "1207";
        public const string UserNotExistWhileDeleting = "1208";
        public const string GetAllUsersErrorId = "1210";
        public const string GetUserByLoginIdErrorId = "1211";
        public const string GetAllShiftsErrorId = "1214";
        public const string NavigateUrlEdit = "1215";
        public const string ExportUsersErrorId = "1216";
        public const string ImportUsersErrorId = "1217";

        public const string AccountLocked = "1220";
        public const string AccessDeniedForUserManagement = "1221";
        public const string AccessDeniedForUpdatingUser = "1222";
        public const string AccessDeniedForDeletingUser = "1223";
        public const string AccessDeniedForViewingUsers = "1224";
        public const string UNAuthorizedUser = "1225";
        public const string AlreadyLoggedIn = "1226";
        public const string SamePasswordErrorId = "1227";
        public const string InvalidPassword = "1228";
        public const string InvalidUserIdOrPassword = "1230";
        public const string ChangePassword = "1231";
        public const string ChangePasswordErrorId = "1232";
        public const string LogoutErrorId = "1233";
        public const string PasswordComplexityErrorId = "1234";
        public const string UpdateUserNameErrorId = "1235";
        public const string ForcedLogOut = "1236";
        public const string SessionExpired = "1237";
        public const string AccessDeniedCreateSuperAdmin = "1238";
        public const string AccessDeniedUpdateSuperAdmin = "1239";
        public const string AccessDeniedDeleteSuperAdmin = "1240";
        public const string PasswordLengthErrorId = "1241";
        public const string PasswordEmptyErrorId = "1242";
        public const string UserNameFormatError = "4129";
        public const string AccessDeniedUpdateAdmin = "1243";
        public const string AccessDeniedDeleteAdmin = "1244";
        public const string AccessDeniedDeleteSelf = "1245";
        public const string PasswordSameAsUserName = "1246";
        public const string UserDeleted = "1247";
        public const string ErrorOnChangePassword = "1248";
        public const string EnterValidPassword = "1249";
        public const string InvalidOperation = "1250";
        public const string MandatoryUserGroupErrorCode = "1251";
        public const string CannotChangeOtherSuperAdminPassword = "1252";
        public const string InactiveAccount="1253";

        #endregion

        #region "UserGroups(2000-2050)"
        public const string CreateUserGroupErrorId = "2002";
        public const string DeleteUserGroupErrorId = "2003";
        public const string GetAllMembershipErrorId = "2004";
        public const string GetUserGroupByIdErrorId = "2005";
        public const string GetAllUserGroupsErrorId = "2006";
        public const string GetAllUserGroupForUser = "2008";
        public const string UpdateUserGroupErrorId = "2009";
        public const string AccessDeniedForCreatingUserGroup = "2010";
        public const string AccessDeniedForUpdatingUserGroup = "2012";
        public const string AccessDeniedForDeletingUserGroup = "2013";
        public const string AccessDeniedForManagingUsersGroup = "2014";
        public const string GetAllUserGroupForFolder = "2015";
        public const string UserGroupNotExists = "2016";
        public const string UserGroupDeleted = "2017";
        public const string ValidateUserGroupName = "2018";
        public const string UsergroupNameError = "2019";
        public const string AccessDeniedDeleteAdministratorGroup = "2020";
        public const string UserGroupNameFormatError = "2021";

        #endregion

        #region "AdminAuthenticationReset(2051-2099)"
        public const string GetAllSuperAdminErrorId = "2051";
        public const string GetAllSecretQuestionsErrorId = "2052";
        public const string ValidateAnswersErrorId = "2053";
        public const string ResetPasswordErrorId = "2054";
        #endregion

        #region "ADUser(1500-1599)"
        public const string DeleteFromADServers = "1501";
        public const string DeleteFromADDomain = "1502";
        public const string UpdateAdServerError = "1509";
        public const string GetAllADUsersErrorId = "1511";
        public const string GetADUserDetailsErrorId = "1515";
        public const string ADDomainExists = "1516";
        public const string ADServerExists = "1517";
        public const string AccessDeniedForAddADDomain = "1519";
        public const string AccessDeniedForEditADDomain = "1521";
        public const string AccessDeniedForViewADDomain = "1522";
        public const string AccessDeniedForRemoveADDomain = "1524";
        public const string EvolutionLdapRootExists = "1527";
        public const string DirectoryUserNotFound = "1530";
        public const string DirectoryGroupNotFound = "1531";
        public const string NullValueCantEnter = "1531";
        #endregion

        #region Knowledge Base Management(2100-2199)

        public const string AddRedactionReasonErrorId = "2100";
        public const string UpdateRedactionReasonErrorId = "2101";
        public const string DeleteRedactionReasonErrorId = "2102";
        public const string GetRedactionReasonErrorId = "2103";
        public const string UpdateSearchSettingErrorId = "2105";
        public const string GetSearchSettingErrorId = "2106";
        public const string AddThesaurusErrorId = "2107";
        public const string UpdateThesaurusErrorId = "2108";
        public const string DeleteThesaurusErrorId = "2109";
        public const string GetThesaurusErrorId = "2110";
        public const string AddSpellingVariationErrorId = "2111";
        public const string UpdateSpellingVariationErrorId = "2112";
        public const string DeleteSpellingVariationErrorId = "2113";
        public const string GetSpellingVariationErrorId = "2114";
        public const string AddStopWordErrorId = "2115";
        public const string UpdateStopWordErrorId = "2116";
        public const string DeleteStopWordErrorId = "2117";
        public const string GetStopWordErrorId = "2118";
        public const string CustomizeThesaurusDeniedErrorId = "2120";
        public const string CustomizeSpellingVariationDeniedErrorId = "2121";
        public const string CustomizeStopWordDeniedErrorId = "2122";
        public const string UpdatingConceptSearchingDeniedErrorId = "2123";
        public const string UpdatingPreviewSizeDeniedErrorId = "2124";
        public const string CustomizeKnowledgebaseDeniedErrorId = "2125";
        public const string TurningOnOffRedactionReasonDeniedErrorId = "2126";
        public const string ReasonNameNotUniqueErrorId = "2130";
        public const string ThesaurusWordNotUniqueErrorId = "2131";
        public const string VariationWordNotUniqueErrorId = "2132";
        public const string StopWordNotUniqueErrorId = "2133";
        public const string SynonymsNotUniqueErrorId = "2134";
        public const string VariationsNotUniqueErrorId = "2135";
        public const string ReasonNameNotExistsErrorId = "2137";
        public const string ReasonIdNotExistsErrorId = "2138";
        public const string ReasonMovementRestrictionErrorId = "2139";
        public const string ThesaurusWordNotExistsErrorId = "2140";
        public const string SynonymsNotExistsErrorId = "2141";
        public const string ThesaurusWordIdNotExistsErrorId = "2142";
        public const string VariationWordIdNotExistsErrorId = "2143";
        public const string VariationWordNotExistsErrorId = "2144";
        public const string VariationsNotExistsErrorId = "2145";
        public const string StopWordNotExistsErrorId = "2146";
        public const string StopWordIdNotExistsErrorId = "2147";
        public const string CollectionIdFormatErrorId = "2148";
        public const string MatterIdNotExistsErrorId = "2150";
        public const string LanguageIdNotExistsErrorId = "2151";
        public const string ReviewsetIdFormatErrorId = "2153";
        public const string ArchivedReviewsetIdFormatErrorId = "2154";
        public const string InvalidReviewsetIdFormatErrorId = "2155";
        public const string MappedDocumentErrorId = "2156";
        public const string AddLanguageErrorId = "2158";
        public const string RemoveLanguageErrorId = "2159";
        public const string InvalidPreviewSizeErrorId = "2160";
        public const string LanguageIdNameChangeErrorId = "2161";
        public const string CollectionIdNotExistsErrorId = "2162";
        public const string InvalidLanguageIdFormatErrorId = "2163";
        public const string ReviewsetNameErrorId = "2164";
        public const string CollectionIdErrorId = "2165";
        public const string DocumentIdErrorId = "2166";
        public const string TargetFileNotAvailable = "2170";
        public const string CanPerformMarkupsDeniedErrorId = "2171";
        public const string ReviewSetNameAlreadtExists = "2172";
        public const string ThesaurusSearchKeywordsUsedError = "2173";
        public const string StopWordSearchKeywordsUsedError = "2174";
        public const string SpellingVariationSearchKeywordsUsedError = "2175";
        public const string AddStopwordToSearchError = "2176";
        public const string UpdateStopwordInSearchError = "2177";
        public const string AddThesaurusToSearchError = "2178";
        public const string UpdateThesaurusInSearchError = "2179";
        public const string AddSpellingVariationsToSearchError = "2180";
        public const string UpdateSpellingVariationsInSearchError = "2181";
        public const string DeleteThesaurusInSearchError = "2182";
        public const string DeleteStopwordInSearchError = "2183";
        public const string DeleteVariationsInSearchError = "2184";

        #endregion

        # region API DataSet Error Constants (8000-8100)

        public const string UpdateDuplicateFieldError = "8000";
        public const string UpdateDataSetParentError = "8001";
        public const string UpdateDataSetFolderTypeCodeError = "8002";
        public const string CreateDataSetNameRequiredError = "8003";
        public const string UpdateDataSetNameRequiredError = "8004";
        public const string UpdateDataSetParentChangeError = "8005";
        public const string CreateDataSetParentError = "8006";
        public const string UpdateDataSetTemplateError = "8007";
        public const string CreateDataSetTemplateError = "8008";
        public const string CreateTemplateXmlError = "8009";
        public const string CreateDataSetFolderTypeCodeError = "8010";
        public const string FieldAlreadyDeletedError = "8011";
        public const string DeleteDatasetDeleteError = "8012";
        public const string InvalidDatasetId = "8014";
        public const string InvalidClusterStatus = "8026";
        public const string BatesRenumberingNotAllowed = "8015";
        public const string InvalidFieldId = "8016";
        public const string SystemFieldCanNotBeDeleted = "8017";
        public const string DcnFieldCanNotBeDeleted = "8018";
        public const string DcnFieldCanNotBeUpdated = "8019";
        public const string InValidDataSetNameError = "8020";
        public const string InValidDCNPrefixError = "8021";
        public const string NullClusteringTask = "8022";
        public const string ClustersUnavailable = "8023";
        public const string ClustersInProgress = "8024";


        # endregion

        # region API Matter Error Constants (8101-8200)

        public const string MatterDetailsEmptyStringError = "8101";
        public const string GetMatterDetailsError = "8103";
        public const string CreateMatterCreatedByError = "8104";
        public const string ValidMatterIdError = "8105";
        public const string ValidDataSetIdError = "8106";
        public const string ValidSqlServerIdError = "8107";
        public const string ValidSearchServerIdError = "8108";
        public const string ValidSqlServerFormatError = "8109";
        public const string ValidSearchServerFormatError = "8110";
        public const string EmptyMatterNameError = "8111";
        public const string UpdateMatterParentFolderStatus = "8112";
        public const string MatterNotExists = "8113";
        public const string MatterDeleted = "8114";
        public const string MatterInputParameter = "8115";
        public const string UpdatedMatterInfo = "8116";
        public const string LoadBalancerNotRegistered = "9971";

        # endregion

        #region Production 7101 - 7199
        public const string XdlFileMissing = "7103"; //Xdl file missing in vault
        //privilage Log
        public const string GetPrivilegeLogErrorId = "7105";
        //production job summary
        public const string CancelJobErrorId = "7108";
        public const string GetProductionLogErrorId = "7119";
        public const string UpdateJobStatusErrorId = "7110";
        public const string GetJobDetailsErrorId = "7111";
        //production set 
        public const string GetProductionJobErrorId = "7112";
        //preference 
        public const string GetPreferenceErrorId = "7113";
        //DAO
        public const string CreateDocumentSetErrorId = "7114";
        public const string UpdateDocumentSetMasterErrorId = "7115";
        public const string GetCompletedJobErrorId = "7116";
        public const string GetProductionProfilesErrorId = "7117";
        public const string SaveProfileErrorId = "7118";
        //production job details
        //Service error codes
        public const string RedactioNotificationFailed = "7121";
        public const string EditAccessDeniedInProductionProfile = "7122";
        public const string CreateAccessDeniedInCreateProductionProfile = "7123";
        public const string FailedToSaveProfile = "7124";
        public const string FailedToGetProfiles = "7125";
        public const string CreateAccessDeniedInProductionSet = "7126";
        public const string FailedToCreateProductionSet = "7127";
        public const string AccessDeniedForProduction = "7130";
        public const string FailedToGetProductionSetDetails = "7131";
        public const string FailedToGetJobCompleteDetails = "7132";
        public const string FailedToGetFieldDetailsForDataset = "7139"; //"Get field details for dataset failed"
        public const string FailedToGetDatasetForFolder = "7140";//Get dataset for folder failed
        public const string FailedToGetCompleteJobDetails = "7141";//Getting completed job details failed
        public const string GetAllDocumentSetErrorId = "7144";//

        public const string FailedToUpdatePrevligeLog = "7143";

        #endregion

        # region Folder API Error Constants 7200 - 7300

        public const string FolderNameRequiredError = "7200";
        public const string FolderNameUpdateRequiredError = "7201";
        public const string FolderUnderDataSetError = "7202";
        public const string CreateFolderAccessDenied = "7203";
        public const string DataSetDetailsFormatterIdError = "7205";
        public const string MatterUnderSystemError = "7206";
        public const string SystemFolderUnderSystemError = "7207";
        public const string MatterDataSetDeleteError = "7208";
        public const string FolderWithChildFoldersDeleteError = "7209";
        public const string MatterUnderMatterError = "7211";
        public const string MatterUnderDataSetError = "7212";
        public const string MatterUnderMatterFolderError = "7213";
        public const string UpdateSystemFolderError = "7214";
        public const string GetInvalidFolderDetailError = "7215";
        public const string InvalidFolderType = "7217";
        public const string InValidFolderTypeCodeError = "7218";
        public const string InValidUpdateParentFolderError = "7219";
        public const string InValidUpdateFolderTypeError = "7220";
        public const string InValidUpdateFolderIdError = "7221";
        public const string InValidUpdateInputFolderIdError = "7222";
        public const string InValidCreateParentFolderIdError = "7223";
        public const string GetInvalidParentFolderDetailError = "7224";
        public const string UpdateFolderAccessDenied = "7226";
        public const string DeleteFolderAccessDenied = "7227";
        public const string DeleteMatterAccessDenied = "7228";
        public const string GetDeletedFolderDetailError = "7229";
        public const string GetInvalidMatterFolderDetailError = "7230";

        # endregion

        #region Reviewset API Error Constants 9201 - 9300
        public const string ManageReviewSetAccessDenied = "9201";      
        public const string ViewReviewSetAccessDenied = "9206";        
        public const string InValidEnableCheckInUseLockRule = "9209";
        public const string InValidAllowViewReadOnlyRule = "9210";
        public const string InValidReviewsetRule = "9211";
        public const string InValidReviewsetErrorId = "9214";
        public const string InValidDateErrorId = "9215";
        public const string InValidStartAndDueDateErrorId = "9216";
        public const string DeleteReviewsetErrorId = "9218";
        public const string UnAssignedUserSearch = "9222";
        public const string ReviewSetArchivedPreConditionForDelete = "9223";
        public const string VaultAlternateIdNotValidErrorId = "9224";

        #endregion

        #region Reports 9301 - 9350
        public const string GetReportListFailed = "9301";
        public const string GetDefaultReportServerInformationFailed = "9302";
        public const string LogReportFailed = "9303";

        #endregion

        #region Delivery Options 9400-9510

        public const string DeletePrintDocumentFailed = "9408";
        public const string GetPrintDocumentPathFailed = "9409";
        public const string GetPrintDocumentPropertiesFailed = "9410";
        public const string GetPrintDocumentListPropertiesFailed = "9411";
        public const string PrintQueueError = "9412";
        public const string SourceDirectoryNotExists = "9413";
        public const string CreatePrintDocumentSourceError = "9414";
        public const string MergePrintDocumentError = "9415";
        public const string CreateSeperatorSheetError = "9416";
        public const string UnableToQueueToRedactit = "9417";
        public const string WriteAccessForSharePathIsDenied = "9418";
        public const string EmailQueueError = "9419";
        public const string SelectDocumentToPrintError = "9420";
        public const string ErrorWhileSchedulingPrintJob = "9421";
        public const string ErrorInGettingPrintToFileProperties = "9422";
        public const string ErrorWhileCancellingPrintJob = "9423";
        public const string ErrorWhileGettingListOfPrintJob = "9424";
        public const string AccessDeniedForPrintingDocuments = "9425";

        public const string AccessDeniedForEmailingDocuments = "9426";
        public const string SelectDocumentsToEmail = "9427";
        public const string ErrorGettingListOfEmailJobProperties = "9428";
        public const string ErrorGettingListOfDocumentsForRequery = "9429";
        public const string ErrorCancellingEmailJob = "9430";
        public const string ErrorGettingEmailJobProperties = "9431";
        public const string ErrorWhileSchedulingEmailJob = "9432";
        public const string ErrorGettingListOfCompressedFileProperties = "9433";
        public const string ErrorDeletingCompressedFile = "9434";
        public const string ErrorGettingCompressedFileProperties = "9435";
        public const string NoNativeFilesFound = "9436";
        public const string RedactItPublishError = "9437";
        public const string ProfileNameDuplicateError = "9512";
        #endregion

        #region Import job specific error code
        public const string bootParameterSerializeException = "bootParameterSerializeException";
        public const string InitializeError = "InitializeError";
        public const string InitializeHelperMessage = "InitializeHelperMessage";
        public const string GenerateTasksError = "GenerateTasksError";
        public const string ResourceManagerObjectCreateFailure = "ResourceManagerObjectCreateFailure";
        public const string EDLoaderSourceLocationNotAccessible = "EDLoaderSourceLocationNotAccessible";
        public const string EDLoaderGetFileListFailure = "EDLoaderGetFileListFailure";
        public const string ImportDCBUserNameCannotBeEmpty = "DCBSecurityUserNameCannotBeEmpty";
        public const string NoFileLocationsForEDLoaderFileListParserWorker = "NoFileLocationsForEDLoaderFileListParserWorker";
        public const string EDLoaderExtractionWorker_ObtainDatasetDetailsFailure = "EDLoaderExtractionWorker_ObtainDatasetDetailsFailure";
        public const string EDLoaderExtractionWorker_FailedToObtainMatterDetails = "EDLoaderExtractionWorker_FailedToObtainMatterDetails";
        public const string EDLoaderExtractionWorker_NativeFileNotAvailable = "EDLoaderExtractionWorker_NotNativeFileAvailable";
        public const string EDLoaderExtractionWorker_DecryptionKeyUnavailable = "EDLoaderExtractionWorker_DecryptionKeyUnavailable";
        public const string EDLoaderExtractionWorker_ConfigurationUnavailable = "EDLoaderExtractionWorker_ConfigurationUnavailable";
        public const string EDLoaderFileParserWorker_OptionalConfigurationUnavailable = "EDLoaderFileParserWorker_OptionalConfigurationUnavailable";
        public const string EDLoaderFileParserWorker_MandatoryConfigurationUnavailable = "EDLoaderFileParserWorker_MandatoryConfigurationUnavailable";
        public const string EDLoaderFileParserWorker_DatasetOrMatterDetailsUnavailable = "EDLoaderFileParserWorker_DatasetOrMatterDetailsUnavailable";
        public const string EDLoaderExtractionWorker_NoFilesToProcess = "EDLoaderExtractionWorker_NoFilesToProcess";
        public const string EDocsCollectionEntityEmpty = "EDocsCollectionEntityEmpty";
        public const string EDocsEmailGenaratorWorker_MsgGenerationFailed = "EmailGenaratorWorkerMsgFileGenerationFailed";
        public const string EmptyMsgFileGenerationFaliure = "UnableToGenerateEmptyMsgFile";
        #endregion

        #region Privilege Log - Range: 9451 - 9509       

        public const string XmlStringNotWellFormed = "9451"; //Xml string is not well formed. Unable to recreate object.
        public const string LogFoldePathMissing = "9452";// Log folder path missing
        public const string LogFoldePathNotSpecified = "9453";// Log folder path not specified
        public const string LogFileNameMissing = "9454";// Log file name is missing
        public const string LogFilehasInvalidCharacters = "9455";// File name has some invalid characters
        public const string ErrorGettingDatasetDetails = "9456";// Error in getting dataset detail for a dataSet id (Service error)
        public const string ErrorGettingDatasetDetailsNullReturned = "9457";// GetDataSetDetailForDataSetId returned a null value.Specified dataset may not exist
        public const string ErrorGettingCollectionMatterDetails = "9458";// Error while getting collection and matter details
        public const string ErrorGettingAllTags = "9459";// Error while getting all tags (Service error)
        public const string ErrorGettingAllReasonForRedaction = "9460";// Error while getting all reason for redaction (Service error)
        public const string ErrorFetchingHeaderDetails = "9461";// Error while fetching header details
        public const string ErrorCreatingCsvFile = "9462";// Error creating csv file
        public const string ErrorGettingUserDetails = "9463";// Error while getting user details (Service error)
        public const string ErrorGettingAllSavedSearch = "9464";// Error while getting all saved search (Service error)
        public const string ErrorGettingDocumentFilterOptionNotSet = "9465";// Document filter option is not set
        public const string ErrorGettingDocuments = "9466";// Error while getting documents
        public const string ErrorGettingDocumentFieldData = "9467";// Error while getting document field data (Service error)
        public const string ErrorGettingDocumentTags = "9468";// Error while getting document tag data (Service error)
        public const string ErrorGettingDocumentFieldValues = "9469";// Error while getting document field values
        public const string ErrorGettingDocumentTagValues = "9470";// Error while getting document tag values
        public const string ErrorGettingDocumentRedactionXml = "9471";// Error while getting document redaction xml (Service error)
        public const string ErrorGettingDocumentRedactionReasonValues = "9472";// Error while getting document redaction reason values
        public const string ErrorInInitialize = "9473";// Error in initialize method
        public const string ErrorGenerateTask = "9474";// Error in generate task method
        public const string ErrorDoAtomicWork = "9475";// Error in do atomic work method
        public const string ErrorShutdown = "9476";// Error in shutdown method

        public const string ErrorGettingAllPrivilegeLogProfiles = "9501";//Error Getting All Privilege Log Profiles
        public const string ErrorGettingPrivilegeLogProfileDetails = "9502";//Error Getting Privilege Log Profile Details
        public const string ErrorCreatingPrivilegeLogProfile = "9503";//Error Creating PrivilegeLog Profile
        public const string ErrorUpdatingPrivilegeLogProfile = "9504";//Error Updating PrivilegeLog Profile
        public const string ErrorValidatingPath = "9505";//Error Validating Path
        public const string ErrorCreatingPrivilegeLogJob = "9506";//Error CreatingPrivilegeLog Job
        public const string ErrorGettingPrivilegeLogJob = "9507";//Error Getting PrivilegeLog Job
        public const string ErrorGettingPrivilegeLogDetails = "9508";//Error Getting PrivilegeLog Details
        public const string ErrorGettingAllPrivilegeLogJobs = "9509";//Error Getting All PrivilegeLog Jobs
        public const string AccessDeniedForRolesAndPolicy = "9510";//No policy for Getting All PrivilegeLog Jobs
        public const string AccessDeniedForPrivilegeLog = "9511";//No Policy  for creating PL Job

        // LogJobException
        #endregion

        #region SaveSearch Results (7000-70100)
        public const string SaveSearchExportBinaryInsertError = "7000";
        public const string NoSaveSearchResultError = "7001";
        public const string InsertSavedSearchResultError = "7002";
        public const string NoSavedSearchResultforId = "7003";
        public const string UnableToDeleteSavedSearchInformation = "7004";
        public const string SearchResultIdentifierNullError = "7005";
        public const string SearchResultBinaryNotFoundError = "7006";
        public const string SavingSearchResultNotAllowed = "7007";
        public const string ExportSavedSearchResultNotAllowed = "7008";
        public const string DeleteSavedSearchResultNotAllowed = "7009";
        public const string ExportOthersSavedSearchResultNotAllowed = "7010";
        public const string DeleteOthersSavedSearchResultNotAllowed = "7011";
        public const string ViewOthersSavedSearchResultNotAllowed = "7012";
        public const string ViewSavedSearchResultNotAllowed = "7013";
        #endregion

        #region CaseMap (3200-3300)
        public const string GenerateDocumentFactXmlError = "3200";
        public const string GenerateDocumentLinksError = "3201";
        public const string GenerateDcbLinksXmlError = "3202";
        public const string GenerateFieldDefinitionXmlError = "3203";
        public const string AccessDeniedForCaseMap = "3207";

        #region DCBLinksToCaseMap
        public const string DoAtomicError = "3204";
        #endregion
        #endregion

        #region "License Management"

        public const string LICENSE_DOES_NOT_EXISTS = "9600";
        public const string LICENSE_EXPIRED = "9601";
        public const string LICENSE_INVALID = "9602";
        public const string LICENSE_UNABLE_TO_VERIFY = "9603";
        public const string LICENSE_UNABLE_TO_CREATE = "9604";
        public const string LICENSE_UNABLE_TO_DELETE = "9605";
        public const string LICENSE_UNABLE_TO_ASSIGN_USER = "9606";
        public const string LICENSE_UNABLE_TO_MARK_EXPIRED = "9607";
        public const string LICENSE_CREATE_SERVICE_ERROR = "9608";
        public const string LICENSE_DELETE_SERVICE_ERROR = "9609";
        public const string LICENSE_GET_ALL_LICENSE_SERVICE_ERROR = "9610";
        public const string LICENSE_ASSIGN_USER_SERVICE_ERROR = "9611";
        public const string LICENSE_GET_USERS_SERVICE_ERROR = "9612";
        public const string LICENSE_ALREADY_EXISTS_ERROR = "9613";
        public const string LICENSE_RENEW_ERROR = "9614";
        public const string LICENSE_VALIDITY_ERROR = "9615";
        public const string LICENSE_DOES_NOT_EXISTS_UNABLE_TO_AUTO_ASSIGN = "9616";
        public const string LICENSE_RENEW_ERROR_UNABLE_TO_AUTO_ASSIGN = "9617";
        public const string AccessDeniedForLicenseManagement = "9618";

        #endregion

        #region Export Load file - Range: 9700 - 9900
        public const string ExportErrorInInitialize = "9702";// Error in initialize method
        public const string ExportErrorGenerateTask = "9703";// Error in generate task method
        public const string ExportErrorDoAtomicWork = "9704";// Error in do atomic work method
        public const string ExportErrorShutdown = "9705";// Error in shutdown method
        public const string ExportErrorAccessFolder = "9706";// Error while creating or accessing the folder 
        public const string ExportErrorCreatingFile = "9707";// Error while creating or accessing the File
        public const string ErrorGettingDocumentBinary = "9708";// Error while getting document binary
        public const string ErrorWrintingTextFile = "9709"; //Error while creating or writing text file
        public const string ErrorWrintingBinaryFile = "9710"; //Error while creating or writing binary file
        public const string ExportErrorGettingDocumentFilterOptionNotSet = "9711";// Document filter option is not set
        public const string ExportErrorGettingDocuments = "9712";// Error while getting documents
        public const string ExportErrorGettingDocumentsMetaData = "9713";// Error while getting documents meta data
        public const string ErrorGettingDocumentMetaData = "9715";// Error while getting document meta data
        public const string ExportErrorGettingTag = "9716"; //Error while getting Tag
        public const string ExportErrorConvertDate = "9717"; //Error while convert string in to date in dataset field
        public const string ExportErrorZeroDocument = "9718"; //Ther are zero document to export
        public const string ExportErrorDeletingFile = "9719";// Error while deleting or accessing the File
        public const string ExportErrorGetEVDocumentFailed = "9720";// Error while deleting or accessing the File
        public const string ExportErrorInvalidPath = "9721";// Error invalid export source path    
        public const string ExportErrorSaveTemplate = "9722";// Error while save Template     
        #endregion

        #region Printer Management Error Codes(20001-20006)

        public const string InvalidPrinterLocation = "20001";
        public const string GetMappedPrinterFailed = "20002";
        public const string AddPrinterFailed = "20003";
        public const string RemoveMappedPrinterFailed = "20004";
        public const string EditMappedPrinterFailed = "20005";
        public const string GetAllMappedPrintersFailed = "20006";
        public const string PrinterFriendlyNameNotUnique = "20007";
        public const string PrinterNotExists = "20008";
        public const string MapPrinterPermissionDenied = "20009";

        #endregion

        #region Bulk Print Error Codes

        public const string BulkPrintJobInitialisationError = "20100";
        public const string BulkPrintJobTaskGenerationError = "20101";
        public const string BulkPrintJobDoAtomicWorkError = "20102";
        public const string BulkPrintQueueError = "20103";
        public const string CreateBulkPrintJobServiceError = "20104";
        public const string CancelBulkPrintJobServiceError = "20105";
        public const string GetBulkPrintJobPropertiesError = "20106";
        public const string GetListOfBulkPrintJobServiceError = "20107";
        public const string BulkPrintPermissionDenied = "20108";
        public const string BulkPrintInvalidFileError = "20109";

        #endregion

      

        #region Query Container - 9951 - 9999
        public const string InvalidQueryString = "9966";
        public const string InvalidQueryTerm = "9967";
        public const string StopWordInQuery = "9968";
        public const string BinFilterNotValid = "9970";
        public const string VaultNotReadyError = "9999";
        #endregion

        #region External API - 100000 - 200000
        public const string InvalidUid = "100000";
        #endregion
        #region Dashboards 10000-20000
        public const string AddminDashBoardAccesssDeniedError = "10001";
        public const string ReviewerDashBoardAccesssDeniedError = "10002";
        public const string ReviewerAdminDashBoardAccesssDeniedError = "10003";
        public const string ReviewerAdminReportsDashBoardAccesssDeniedError = "10004";
        public const string ReportsDashBoardAccesssDeniedError = "10005";
        #endregion

        #region Binary Externalization
        public const string AccessDeniedToBinaryExternalizationPath = "30001";
        #endregion
        public const string GetAllOrgFoldersFailureError = "1120";
        public const string OrganizationLevelAccessDeniedError = "1121";

        #region ReProcessing

        public const string ReProcessScheduleJobError = "200110";
        public const string ReprocessScheduleJobAccessDeniedError = "200111";
        public const string ReProcessGetConversionLogError = "200112";
        public const string ReProcessGetRedactItErrorReasons = "200113";
        public const string RedactItFailToConvert = "200114";
        public const string ConversionResultsExportJobError = "200115";
        public const string ConversionResultsExportGetJobError = "200116";
        public const string ConversionResultsExportScheduleJobError = "200117";
        #endregion

        #region Binder 21000 - 21100

        public const string AddBinderErrorId = "21000";
        public const string EditBinderErrorId = "21001";
        public const string DeleteBinderErrorId = "21002";
        public const string BinderAlreadyExistsErrorId = "21003";
        public const string GetBinderDetailErrorId = "21004";
        public const string GetAllReviewSetsForBinderErrorId = "21005";
        public const string InvalidBinderIdOrBinderDoesNotExist = "21006";
        public const string BinderDeleted = "21007";
        public const string GetAllAuthorizedBindersError = "21008";
        public const string HandleBinderReviewSetActionError = "21009";
        public const string InvalidReviewSetInvalidActionError = "21010";
        public const string AcessDeniedToCreateBinder = "21011";
        public const string AcessDeniedToEditBinder = "21012";
        public const string AcessDeniedToDeleteBinder = "21013";
        public const string SingleUserCheckOutErrror = "21014";
        public const string ReviewSetCompleteBulkTagAuditEventLogFailure = "21015";
        public const string AcessDeniedToManagePredictiveCodingProject = "21016";
        public const string AcessDeniedToReviewPredictiveCodingProject = "21017";

        #endregion

        #region LAW PreDiscovery
        public const string LawCaseAlreadyExist = "1980"; // The LAW case is already associated with the organization
        public const string LawCaseInvalidDataset = "1982"; // The LAW case is associated with an invalid datasetId
        #endregion

        #region general

        public const string UpdateJobTemplateFail = "9801"; // Fail to update job template

        #endregion
#region reports 
        //reports feature reserves from 22000 to 23000
#endregion

    }
}
