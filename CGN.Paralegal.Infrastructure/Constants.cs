//---------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil V</author>
//      <description>
//          This file contains the all Constants used for infrastructure
//      </description>
//      <changelog>
//          <date value="04/29/2015"></date>
//          <date value="07/12/2015">Enum AUEventName added for 86839 bug fixed</date>
//          <date value="08/10/2015">Bugs Fixed #85113</date>
//          <date value="02/16/2012">Changed the casing for the login attempt token</date>
//          <date value="4/5/2012">Fix for Bug# 98845</date>
//          <date value="11/04/2012">94842 bug fixed</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="12/10/2012">audit log fix for redaction</date>
//          <date value="12/10/2012">Fix for 128305</date>
//          <date value="04/18/2013">CHEV 2.2.1 - ADM-LICENSE-002 - Implementation</date>
//          <date value="06-11-2013">Bug Fix  # 142099 and 142102 - Auditing the law import events</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace CGN.Paralegal.Infrastructure
{
    using System.Diagnostics.CodeAnalysis;

    public class AUHelper
    {
        #region Constants and Fields

        internal const int MagicJobsSeparator = 100;

        internal const int MagicShift = 10000;

        #endregion

        #region Enums

        public enum AUGenericEventType
        {
            Started = 1,

            Completed = 2,

            Failed = 3,

            Paused = 4,

            Resumed = 5,

            Cancelled = 7
        }

        #endregion

        #region Public Methods and Operators

        public static int GetEventNameFromJobTypeId(int jobTypeId, AUGenericEventType auGenericEventType)
        {
            return GetEventTypeFromJobTypeId(jobTypeId) + (int)auGenericEventType;
        }

        public static int GetEventTypeFromJobTypeId(int jobTypeId)
        {
            checked
            {
                return MagicShift + jobTypeId * MagicJobsSeparator;
            }
        }

        #endregion
    }

    internal class Constants
    {
        #region Constants and Fields

        public const string AuditApplicationName = "@in_sApplicationName";

        public const string AuditAuditLogTable = "@in_tblAuditLog";

        public const string AuditCurrentFolderId = "@in_iCurrentFolderId";

        public const string AuditEventNameParameter = "@in_iEventNameId";

        public const string AuditEventTypeParameter = "@in_iEventTypeId";

        public const string AuditInformation = "@in_sInformation";

        public const string AuditTimeStamp = "@in_dTimeStamp";

        public const string AuditUserIdParameter = "@in_uUserGUID";

        public const string DataRange = "DataRange";

        public const string EvAuth = "EVAuth";

        public const string EvolutionAuditLog = "EV_AUD_InsertInto_AuditLog";

        public const string EvolutionAuditLogExpress = "EV_AUD_InsertAuditLogExpress";

        public const string IV_BYTES_FORMAT = "00000000";

        public const string IntegratedSecurity = "Integrated Security";

        public const string IntegratedSecurityOption = "IsNTAuthentication";

        public const string SSPI = "SSPI";

        public const string ServiceName = "ServiceName";

        public const string TimeStamp = "TimeStamp";

        public const string UTC = "(UTC)";

        public static string ContextTitle = "InTransaction";

        public static string DB_CON_STR = "ConcordanceEVConnection";

        public static string DB_CON_STR_LOG = "ConcordanceLogConnection";

        //Stored Proc to get default server information.

        public static string DataBaseDataSource = "Data Source";

        public static string DataBaseDataSourceColumn = "HostID";

        public static string DataBaseInitialCatalog = "Initial Catalog";

        public static string DataBaseInitialCatalogColumn = "DBName";

        public static string DataBasePassword = "Password";

        public static string DataBasePasswordColumn = "Password";

        public static string DataBaseUserId = "User ID";

        public static string DataBaseUserIdColumn = "UserName";

        public static string ErrorDataBaseConnectionStringUnAvailable = "8001";

        public static string ExceptionErrorMessageDefault = "Problem occured in server, Please try again later."; //

        public static string ExceptionResource = "Exception";

        public static string ExceptionResourceAssembly = "CGN.Paralegal.Infrastructure";

        public static string GetMatterDBDetails = "EV_CMG_GetMatterDBDetails";

        public static string GetSqlServerDetails = "EV_CMG_GetSQLServerDetails";

        public static string GetCmgSqlServerDetails = "EV_CMG_GetSQLServerDetails";

        public static string GetCmgSearchServerDetails = "EV_CMG_GetVelocityServerDetailsForMatterID";

        public static string GetCmgInstanceConfig = "EV_CMG_GetInstanceConfig";

        public static string GlobalCacheName = "GlobalCacheManager";

        public static string InputParamGetMatterDBDetails = "@in_iMatterid";

        public static string InputParamGetServerIdDetails = "@in_uServerid";

        public static string MSG_EXCEPTION_DESC = "In Error description";

        public static string UserCacheName = "UserSessionCacheManager";

        internal const string AddElement = "add";

        internal const string AllLowerCase = "alllowercase";

        internal const string AllUpperCase = "alluppercase";

        internal const string ApplicationElement = "application";

        internal const string ApplicationName = "ApplicationName";

        internal const string ApplicationsElement = "applications";

        internal const string CommaSeparator = ",";

        internal const int ContentFieldType = 2000;

        internal const string CustomSection = "customSection";

        internal const int DCNFieldType = 3000;

        internal const string DefaultApplication = "Concordance";

        internal const string DefaultTimeZone = "UTC";

        internal const int DescriptionFieldType = 3002;

        internal const string DocumentSelectionCacheKey = "DocumentSelectionCacheKey";

        internal const string ENCRYPTED = "encrypted";

        internal const string EvolutionCookieName = "ASP.Net_SessionId";

        /// <summary>
        /// below constants are used in DateTimeUtility class
        /// </summary>
        internal const string FIELD_TYPE_DATEFORMAT_DISPLAY = "YYYYMMDD";

        internal const string FIELD_TYPE_DATEFORMAT_HHMMSS_DISPLAY = "YYYYMMDDHHMMSS";

        internal const string FIELD_TYPE_DDMMYYY = "DDMMYYYY";

        internal const string FIELD_TYPE_DDMMYYY_APP_FORMAT = "dd/MM/yyyy";

        internal const string FIELD_TYPE_DDMMYYY_HHMMSS = "DDMMYYYYHHMMSS";

        internal const string FIELD_TYPE_DDMMYYY_HHMMSS_APP_FORMAT = "dd/MM/yyyy HH:mm:ss tt";

        internal const string FIELD_TYPE_MMDDYYY = "MMDDYYYY";

        internal const string FIELD_TYPE_MMDDYYY_APP_FORMAT = "MM/dd/yyyy";

        internal const string FIELD_TYPE_MMDDYYY_HHMMSS = "MMDDYYYYHHMMSS";

        internal const string FIELD_TYPE_MMDDYYY_HHMMSS_APP_FORMAT = "MM/dd/yyyy HH:mm:ss tt";

        internal const string FIELD_TYPE_YYYYMMDD_APP_FORMAT = "yyyy/MM/dd";

        internal const string FIELD_TYPE_YYYYMMDD_HHMMSS_APP_FORMAT = "yyyy/MM/dd HH:mm:ss tt";

        internal const string FileConfigurationSource = "File Configuration Source";

        internal const string HTTP_ERROR_CODE_UNAUTHORIZED = "501";

        internal const string LoginUrlFromInnerPath = @"..\login.aspx?sid=";

        internal const string NameAttribute = "name";

        internal const string NoContentFound = "No content available";

        internal const string NoTitleFound = "No title available";

        internal const int ReasonFieldType = 3001;

        internal const string SearchResultsCacheKey = "SearchResultsCacheKey";

        internal const string SettingsElement = "settings";

        internal const string SnippetField = "snippet";

        internal const string SubApplicationElement = "subapplication";

        internal const string SubApplicationsElement = "subapplications";

        internal const int TitleFieldType = 3003;

        internal const string Token = "Token ";

        internal const string UnAuthorized = "5";

        internal const string User = "user";

        internal const string UserGUID = "UserGUID";

        internal const string UserLogout = "UserLogout";

        internal const string UserService = "UserService";

        internal const string UserSessionInformation = "UserSessionInfo";

        internal const string ValueAttribute = "value";

        #endregion

        #region Constructors and Destructors

        private Constants()
        {
        }

        #endregion
    }

    #region ENUMS

    #region "Event Name"

    /// <summary>
    /// Set id for event names(eg: 'Search query executed'=1) in eneumrator, event name id should be 'EV_AUD_EventName' table 'EventName_Id' column value
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum AUEventName
    {
        Default = 0,

        MatterArchived = 1,

        MatterCreated = 2,

        MatterPropertyModified = 3,

        MatterUNArchived = 4,

        CommentAdded = 5,

        CommentDeleted = 6,

        CommentEdited = 7,

        ServerAdded = 8,

        ServerModified = 9,

        ServerRemoved = 10,

        DocumentExportCompleted = 11,

        DocumentImportCompleted = 12,

        ExportStarted = 13,

        ImportStarted = 14,

        DocumentDownloadCompleted = 15,

        DocumentDownloadRequested = 16,

        DocumentAccessed = 17,

        DocumentReviewed = 18,

        DocumentEmailed = 19,

        FolderCreated = 20,

        FolderDeleted = 21,

        FolderPropertyModified = 22,

        JobPaused = 23,

        JobDeleted = 24,

        JobCompleted = 25,

        JobFailure = 26,

        JobScheduled = 27,

        JobStarted = 28,

        JobStopped = 29,

        JobResumed = 30,

        JobPriorityChanged = 31,

        LoginFailure = 32,

        LoginSuccess = 33,

        SessionCreated = 34,

        SessionTerminated = 35,

        RedactionModified = 36,

        EmailNotificationSent = 39,

        NotificationSent = 40,

        PrintCompleted = 41,

        PrintRequested = 42,

        MatterProduced = 43,

        ReportAccessed = 44,

        ReportDefinitionCreated = 45,

        ReportDefinitionDeleted = 46,

        ReportDefinitionModified = 47,

        ReportDeleted = 48,

        ReportGenerated = 49,

        KBDictionaryModified = 50,

        SavedSearchQueryDeleted = 51,

        SearchQueryCreated = 52,

        SearchQueryExecuted = 53,

        SearchQuerySaved = 54,

        SearchServerAdded = 55,

        SearchServerRemoved = 56,

        SearchServerModified = 57,

        SearchSettingModified = 58,

        PolicyAllowed = 59,

        PolicyDenied = 60,

        DocumentTagModified = 61,

        DocumentTagged = 62,

        TextTagCreated = 63,

        TextTagModified = 64,

        UserAdded = 65,

        UserDeleted = 66,

        UserGroupAdded = 67,

        UserGroupDeleted = 68,

        UserGroupModified = 69,

        UserPropertyModified = 70,

        DataSetArchived = 71,

        DataSetCreated = 72,

        DataSetModified = 73,

        DataSetUNArchived = 74,

        ReviewSetArchived = 75,

        ReviewSetCreated = 76,

        ReviewSetModified = 77,

        ReviewSetUNArchived = 78,

        TemplateCreated = 79,

        TemplateModified = 80,

        BatesNumberExhausted = 81,

        ReviewSetMerged = 82,

        ReviewSetSplit = 84,

        DataSetRulesModified = 85,

        ReviewSetDeleted = 87,

        ImportStoppedTechnicalError = 88,

        SubscriptionAdded = 89,

        SubscriptionEdited = 90,

        DataViewerDocumentOpened = 91,

        DataViewerDocumentEdited = 92,

        FieldsShown = 93,

        FieldsHidden = 94,

        ConcatenatedSetCreate = 95,

        ConcatenatedSetPropertyModified = 96,

        ConcatenatedSetDelete = 97,

        DataSetDelete = 98,

        ThesaurusAdded = 99,

        ThesaurusEdited = 100,

        ThesaurusDeleted = 101,

        ThesaurusImported = 102,

        ThesaurusExported = 103,

        SpellingVariationAdded = 104,

        SpellingVariationEdited = 105,

        SpellingVariationDeleted = 106,

        SpellingVariationImported = 107,

        SpellingVariationExported = 108,

        StopWordAdded = 109,

        StopWordEdited = 110,

        StopWordDeleted = 111,

        StopWordExported = 112,

        ReasonForRedactionCreated = 113,

        ReasonForRedactionEdited = 114,

        ReasonForRedactionDeleted = 115,

        ReasonForRedactionReordered = 116,

        LoginUNAuthorized = 117,

        DEDuplicationStarted = 118,

        DEDuplicationCompleted = 119,

        DEDuplicationStoppedForTechnicalError = 120,

        ProductionCreated = 121,

        ProductionStarted = 122,

        ProductionPaused = 123,

        ProductionResumed = 124,

        ProductionStopped = 125,

        ProductionCompleted = 126,

        ProductionDocumentCreated = 127,

        ProductionEdited = 128,

        ProductionDeleted = 129,

        PrivilegeLogViewed = 130,

        PrivilegeLogExported = 131,

        DocumentTagCreated = 132,

        BulkTagTaggedUntagged = 133,

        DocumentTagDeleted = 134,

        TextTagDeleted = 135,

        TagFolderCreated = 136,

        TagFolderModified = 137,

        TagFolderDeleted = 138,

        TagGroupCreated = 139,

        TagGroupModified = 140,

        TagGroupDeleted = 141,

        TagSearched = 142,

        AuditLogExported = 143,

        Unlocked = 144,

        PasswordReset = 145,

        CreateAlert = 146,

        DeleteAlert = 147,

        ModifyAlert = 148,

        ModifySavedSearch = 149,

        BulkCommentAdded = 150,

        BulkCommentEdited = 151,

        BulkCommentDeleted = 152,

        SearchComment = 153,

        FilteringByCluster = 154,

        FilteringByBins = 155,

        BinningOnBins = 156,

        AuditLogDelete = 157,

        FieldEdit = 158,

        ReorderFields = 159,

        DeDuplicationCreated = 160,

        DEDUPLICATION_PAUSED = 161,

        DEDUPLICATION_RESUMED = 162,

        DEDUPLICATION_STOPPED = 163,

        DEDUPLICATION_DELETED = 165,

        ImportEdited = 167,

        BulkTagAdded = 168,

        BulkTagDeleted = 169,

        BulkTagStarted = 170,

        BulkTagCancelled = 171,

        BulkTagCompleted = 172,

        BulkTagQueued = 173,

        SendFactToCaseMap = 174,

        LinkBackToCaseMap = 175,

        ReviewerPrintJobRequest = 176,

        ReviewerPrintJobSuccess = 177,

        DocumentPrintSuccess = 178,

        DocumentPrintRequest = 179,

        EmailJobRequested = 180,

        EmailJobSuccess = 181,

        EmailJobFailure = 182,

        DocumentEmailEvent = 183,

        DocumentsAdded = 184,

        DocumentsRemoved = 185,

        PrivilegeLogJobCreated = 186,

        PrivilegeLogJobCompleted = 187,

        PrivilegeLogJobFailed = 188,

        PrivilegePaneAddReasonCode = 189,

        PrivilegePaneEditReasonCode = 190,

        PrivilegePaneAddDocumentDescription = 191,

        PrivilegePaneEditDocumentDescription = 192,

        DOCUMENT_IMPORT_PAUSED = 193,

        ConnectionTest = 194,

        ChangedFieldLevelSecurity = 195,

        ChangedFieldLevelSecurityPerUser = 196,

        SystemFieldSecurity = 197,

        SendDocumentLinkToCaseMap = 198,

        SendOneOrMoreDocumentToCaseMap = 199,

        LinkBackFromCaseMap = 200,

        ConvertLinks = 201,

        DocumentInculdedInConvertLinks = 202,

        ChangedTagLevelSecurity = 203,

        StartABackgroundTask = 205,

        CreateSavedSearchResult = 206,

        DeleteSavedSearchResults = 207,

        ExportSavedSearchResultsCSV = 208,

        SavedToResultList = 209,

        DocumentHistoryViewed = 216,

        FailedtoimportfromsecureDCB = 240,

        ExportLoadFilecompleted = 210,

        ExtractionFailsForPasswordProtectedData = 211,

        BulkPrintJobCreated = 212,

        BulkPrintJobCompleted = 213,

        BulkPrintJobFailed = 214,

        DocumentInBulkPrintJob = 215,

        ImageSetCreated = 217,

        ExportJobcreated = 218,

        ExportDocument = 219,

        ExportJobCompleted = 220,

        ExportJobFailed = 221,

        TagDeleteStarted = 222,

        TagDeleteCancelled = 223,

        TagDeleteCompleted = 224,

        RedactionSetting = 225,

        ImportJobCreated = 228,

        ImportJobCompleted = 229,

        ImportLoadFileJobFailed = 230,

        AddDocument = 231,

        OverlayJobCreated = 232,

        OverlayJobCompleted = 233,

        OverlayJobFailed = 234,

        UpdateDocument = 235,

        PreferenceModified = 237,

        NearNativeDocumentOpened = 238,

        MatterDeleted = 239,

        TagRemoved = 241,

        TagDeleteQueued = 242,

        UserGroupPolicyAllowed = 243,

        UserGroupPolicyDenied = 244,
        //need to find a better way to implement this
        ClusterJobStarted = 245,

        ClusterJobScheduled = 246,

        ClusterJobCompleted = 247,

        ClusterJobCancelled = 248,

        ClusterJobDeleted = 249,

        ClusterJobFailure = 250,

        ClusterJobPaused = 251,

        ClusterJobCreated = 252,

        LicenseAdded = 253,

        LicenseDeleted = 254,

        LicenseAssigned = 255,

        LicenseUnAssigned = 256,

        LicenseExpired = 257,

        PrinterAdded = 258,

        PrinterEdited = 259,

        Printerremoved = 260,

        RedactionCreated = 13306,

        RedactionDeleted = 13307,

        ChangeMarkNotesCreated = 13308,

        ChangeMarkNotesModified = 13309,

        ChangeMarkNotesDeleted = 13310,

        HighlightCreated = 13311,

        HighlightModified = 13312,

        HighlightDeleted = 13313,

        TextCreated = 13314,

        TextModified = 13315,

        TextDeteted = 13316,

        RectangleCreated = 13317,

        RectangleModified = 13318,

        RectangleDeleted = 13319,

        LineCreated = 13320,

        LineModified = 13321,

        LineDeleted = 13322,

        ArrowCreated = 13323,

        ArrowModified = 13324,

        ArrowDeleted = 13325,

        StampCreated = 13326,

        StampModified = 13327,

        StampDeleted = 13328,

        FindAndRedactionCreated = 13329,

        FindAndRedactionModified = 13330,

        FindAndRedactionDeleted = 13331,
        DocumentUntagged = 13332,
        SavedSearchShared = 13333,
        SavedSearchUnShared = 13334,
        LawImportJobCreated = 13506,
        NearDuplicationJobCreated=13706,
        BinderCreated = 13801,
        BinderDeleted = 13802,
        BinderEdited = 13803,
        ReviewsetCheckOut = 13901,
        ReviewsetComplete = 13902,
        ReviewsetRelease = 13903,
        LawSyncJobCreated = 14006
    }

    #endregion

    #region "Event Type"

    /// <summary>
    /// Set id for event Type(eg: 'Tagging'=2) in enumerator, event type id should be 'EV_AUD_EventType' table  'EventTypeId' column value
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum AUEventType
    {
        Default = 0,

        CaseMatterPropertyChanges = 1,

        Comments = 2,

        ConfigurationChanges = 3,

        DocumentImportExport = 4,

        DocumentDownload = 5,

        DocumentNavigation = 6,

        Email = 7,

        FolderPropertyChanges = 8,

        Jobs = 9,

        LicenseManagement = 10,

        LoginLogout = 11,

        Markups = 12,

        Notifications = 13,

        Print = 14,

        Productions = 15,

        Reports = 16,

        AdminSearch = 17,

        SecurityChanges = 18,

        Tagging = 19,

        UserGroupChanges = 20,

        DataSetPropertyChanges = 21,

        ReviewSetPropertyChanges = 22,

        DocumentDataViewer = 23,

        Editing = 24,

        ConcatenatedSet = 25,

        ReviewerSearch = 26,

        SearchAdministration = 27,

        DEDuplication = 28,

        AuditLog = 29,

        AdminAuthenticationReset = 30,

        SearchAlerts = 31,

        Clustering = 32,

        GroupBy = 33,

        SendToCaseMap = 34,

        Document = 35,

        PrivilegeLogJob = 36,

        PrivilegePaneReasonCode = 37,

        PrivilegePaneDocumentDescription = 38,

        FieldLevelSecurity = 39,

        TagLevelSecurity = 41,

        SavedSearchResults = 42,

        ExportLoadFile = 43,

        BulkPrint = 44,

        SearchAlertManagement = 45,

        DocumentHistory = 46,

        ImageSet = 47,

        ExportJob = 48,

        ExportDocument = 49,

        ImportJob = 50,

        ImportDocument = 51,

        ImportLoadFileJob = 52,

        AddDocument = 53,

        OverlayJob = 54,

        UpdateDocument = 55,

        Preferences = 56,

        NearNativeViewer = 57,

        Cluster = 58,

        Licensing = 59,

        PrinterManagement = 60,

        Reviewset = 11700,

        LawImportJob = 13500,

        NearDuplication =13700,

        Binder = 13702,

        LawSyncJob = 14000
        
    }

    #endregion

    #endregion
}