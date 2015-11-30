

//-----------------------------------------------------------------------------------------
// <copyright file="EVSystemFields.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Nagaraju</author>
//      <description>
//       Contains EV System Fields
//      </description>
//      <changelog>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/11/2012">Dev Bug Fix # 102177 </date>
//          <date value="06/14/2012">BugFix#102242</date>
//          <date value="6/25/2012">Task # 102811</date>
//          <date value="05/10/2013">BugFix 130823 - Tag delete performance issue fix</date>
//      <date value="07/17/2013">CNEV 2.2.1 - CR005 Implementation : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------


namespace CGN.Paralegal.Infrastructure.Common
{
    public sealed class EVSystemFields
    {
        //Matter

        public const string MatterId = "_EVMatterId";


        //Datasets

        public const string DatasetId = "_EVDatasetId";

        //Binder
        public const string BinderId = "_EVBinderId";

        //imports

        public const string ImportType = "_EVImportType";
        public const string FamilyId = "_EVFamilyId";
        public const string ParentDocId = "_EVParentDocId";
        public const string RelationshipType = "_EVRelationshipType";
        public const string NativeFilePath = "_EVNativeFilePath";
        public const string DocumentId = "_EVDocumentID";
        public const string DocumentKey = "SearchKey";
        public const string ImageSets = "_EVImageSets";
        public const string FileType = "_EVDocumentFileType";
        public const string FileExtension = "_EVFileExtension";
        public const string DcbId = "_EVDCBId";
        public const string FileSize = "_EVFileSize";
        //public const string ImportedDate = "_EVImportedDate";
        public const string ImportDescription = "_EVImportDescription";
        public const string MD5HashFieldName = "_EVMD5Hash";
        public const string SHAHashFieldName = "_EVSHA1Hash";
        public const string ImportMessage = "_EVImportMessage";
        public const string RelationShipDocumentId = "_EVRelationShipDocumentId";
        public const string RelationShipParentId = "_EVRelationShipParentId";
        public const string LawDocumentId = "_EVLawDocId";

        //REViewSets 

        public const string ReviewSetId = "_evreviewsetid";


        //Tags

        public const string Tag = "_EVTag";

        //Comments

        public const string Comment = "_EVComment";

        //MarkUps

        public const string MarkUp = "_EVMarkup";

        //Redactions 

        public const string RedactionText = "_EVRedactionText";


        //Production Sets

        public const string ProductionSets = "_EVProductionSets";

        //DeDuplication 
        public const string Duplicate = "_EVDuplicate";
        public const string DuplicateId = "_EVDuplicateId";


        //All system fields

        public const string AllSystemFields = "_evredactiontext,_evmarkup,_evtag,_evcomment,_evfileextension,_evdocumentfiletype,_evreviewsetid,_evdatasetid,_evmatterid,_evproductionsets,_evimagesets,document_key,_evdcbid,_evdocumentid,_evimporttype,_evfamilyid,_evparentdocid,_evrelationshiptype,_evduplicateid,_evnativefilepath,snippet,category";

        //document modified date

        public const string DocumentModifiedDate = "_EVDocumentModifiedDate";

        //snippet

        public const string Snippet = "Snippet";

        //Near duplicate fields
        public const string ND_Sort = "ND_Sort";
        public const string ND_EquiSortAtt = "ND_EquiSortAtt";
        public const string ND_FamilyID = "ND_FamilyID";
        public const string ND_ClusterID = "ND_ClusterID";
        public const string ND_Similarity = "ND_Similarity";
        public const string ND_IsMaster = "ND_IsMaster";
        // Markup

        public const string RedactionSearchSave = "EVRedaction";
        public const string ArrowSearchSave = "EVArrows";
        public const string RectangleSearchSave = "EVRectangle";
        public const string LinesSearchSave = "EVLines";
        public const string StampSearchSave = "EVStamp";
        public const string NotesSearchSave = "EVNotes";
        public const string DcnField = "DCN";

        //Conversion Pages Count
        public const string PagesNatives = "_EVPagesNatives";
        public const string PagesImages = "_EVPagesImages";

        public const string ProjectId = "_EVATXProjectId";
    }
}
