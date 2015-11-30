
//-----------------------------------------------------------------------------------------
// <copyright file="EVSearchSyntax.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Nagaraju</author>
//      <description>
//       Contains search syntax of EV 
//      </description>
//      <changelog>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/07/2012">Task # 101476(CR BVT Fixes)</date>
//          <date value="06/28/2012">Bug Fix # 82137</date>
//          <date value="02/13/2015">CNEV 4.0 - Search Engine Replacement - ESLite Integration : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------


namespace CGN.Paralegal.Infrastructure.Common
{
    public sealed class EVSearchSyntax
    {
     
        public const string Tag = EVSystemFields.Tag + ":";
        public const string NotReviewed = "NOT\"" + EVSystemFields.Tag + "\":\"Reviewed\"";
        public const string Reviewed = "\"" + EVSystemFields.Tag + "\":\"Reviewed\"";
        
        //TODO: Search Engine Replacement - Search Sub System - Yet to test
        // Lucene syntax to filter for field absence
        public const string NotContentReviewSetId = "_missing_:" + EVSystemFields.ReviewSetId;
        public const string NotContentBinderId = "_missing_:" + EVSystemFields.BinderId;

        // Lucene syntax to filter for field exists 
        public const string ContentReviewSetId = "_exists_:" + EVSystemFields.ReviewSetId;


        public const string NOT = "NOT";
        public const string OpenParanthesis = "(";
        public const string ClosedParenthesis = ")";
        public const string DocumentKey = EVSystemFields.DocumentKey + ":";
        public const string TagValueFormat = "evtag";
        public const string DatasetId = EVSystemFields.DatasetId + ":";
        public const string ProductionSet = EVSystemFields.ProductionSets + ":";
        public const string And = " AND ";
        public const string FamilyId = EVSystemFields.FamilyId + ":";

        //TODO: Search Engine Replacement - Search Sub System - Yet to test
        // Lucene syntax to filter for field absence 
        public const string NotContent = "_missing_:";

        public const string DocumentKeyHash = "DOCUMENT_KEY_HASH" + ":";
        public const string NotReviewedTag = "\"" + EVSystemFields.Tag + "\":\"REVIEW STATUS>>>NOT REVIEWED\"";
        public const string ReviewedTag = "\"" + EVSystemFields.Tag + "\":\"REVIEW STATUS>>>REVIEWED\"";

    }
}
