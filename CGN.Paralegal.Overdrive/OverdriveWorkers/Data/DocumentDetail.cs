# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="DocumentDetail.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Senthil P</author>
//      <description>
//          This is a file that contains Imports job helper methods
//      </description>
//      <changelog>
//          <date value="06/2/2012">Fix for Bugs 101490,94121,101319</date>
//          <date value="02/11/2015">CNEV 4.0 - Search sub-system changes : babugx</date>
//          <date value="02/17/2015">CNEV 4.0 - Search sub-system changes for overlay : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

using System;
using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// This class represents document detail information.
    /// </summary>
    /// <remarks></remarks>
    [Serializable]
    public class DocumentDetail
    {
        /// <summary>
        /// Gets the document.
        /// </summary>
        /// <remarks></remarks>
        public RVWDocumentBEO document { get; set; }

        public List<DcbTags> DcbTags { get; set; }

        public List<DocumentCommentBEO> DcbComments { get; set; }

        /// <summary>
        /// Gets the docType.
        /// </summary>
        /// <remarks></remarks>
        public DocumentsetType docType { get; set; }

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        /// <remarks></remarks>
        public string CorrelationId { get; set; }


        /// <summary>
        /// Gets the IsNewDocument.
        /// </summary>
        /// <remarks></remarks>
        public bool IsNewDocument { get; set; }


        /// <summary>
        /// Gets the OverlayMatchingField.
        /// </summary>
        /// <remarks></remarks>
        public List<RVWDocumentFieldBEO> OverlayMatchingField { get; set; }

        /// <summary>
        /// Gets the Parent DocId.
        /// </summary>
        /// <remarks></remarks>
        public string ParentDocId { get; set; }

        /// <summary>
        /// Gets or sets the index of the conversation.
        /// </summary>
        /// <value>
        /// The index of the conversation.
        /// </value>
        public string ConversationIndex { get; set; }

        /// <summary>
        /// Gets the Overlay Re ImportField.
        /// </summary>
        /// <remarks></remarks>
        public List<RVWDocumentFieldBEO> OverlayReImportField { get; set; }

        /// <summary>
        /// Gets OverlayIsNotSameContentFile
        /// </summary>
        /// <remarks></remarks>
        public bool OverlayIsNewContentFile { get; set; }

        /// <summary>
        /// dataset tags
        /// </summary>
        public List<RVWTagBEO> SystemTags { get; set; }

        /// <summary>
        /// Gets or Sets if document has assigned to any review set
        /// </summary>
        public bool HasAssignedToReviewSet { get; set; }

        /// <summary>
        /// Gets or Sets  Reviewset Id for document has assigned
        /// </summary>
        public List<string> Reviewsets { get; set; }
    }
}