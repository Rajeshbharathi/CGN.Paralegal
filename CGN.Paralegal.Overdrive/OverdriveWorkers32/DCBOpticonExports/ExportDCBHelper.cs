//-----------------------------------------------------------------------------------------
// <copyright file="ExportLoadFileHelper" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Thanikairajan</author>
//      <description>
//          Class for batch ExportLoadFileHelper
//      </description>
//      <changelog>
//          <date value="02/2/2011"></date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------


using System;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.BatchJobs.DcbOpticonExports
{

    

    /// <summary>
    /// This class represents the a DCB Opticon job task BEO.
    /// </summary>
    [Serializable]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute]
    [System.Xml.Serialization.XmlTypeAttribute("DCBOpticonImportJobTask")]
    [System.Xml.Serialization.XmlRootAttribute]
    public class ExportDCBFileJobTaskBEO : BaseJobTaskBusinessEntity
    {
        private string documentId;
        private uint docno;

        /// <summary>
        /// Gets or sets the document identifier
        /// </summary>
        /// <value>Document identifier.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string DocumentId
        {
            get { return documentId; }
            set { documentId = value; }
        }

        [System.Xml.Serialization.XmlElementAttribute]
        public uint Docnumber
        {
            get { return docno; }
            set { docno = value; }
        }
    }



    /// <summary>
    /// This class represents the Volume level information.
    /// </summary>
    [Serializable]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute]
    [System.Xml.Serialization.XmlTypeAttribute("VolumeHelper")]
    [System.Xml.Serialization.XmlRootAttribute]
    public class VolumeHelper
    {
        /// <summary>
        /// Gets or sets the Fincreament.
        /// </summary>
        public int Fincreament
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the From.
        /// </summary>
        public string From
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the FilePath.
        /// </summary>
        public string FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the FilePath.
        /// </summary>
        public string VolumeName
        {
            get;
            set;
        }
    }


    class JsonComment
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string SelectedText { get; set; }
        public string FieldId { get; set; }
        public string IndexInDocument { get; set; }
        public string SelectedHtml { get; set; }
    }

}

