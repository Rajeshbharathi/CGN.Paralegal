#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ProductionHeaderFooterFont.cs" company="Cognizant">
//      Copyright (c) Cognizant. All rights reserved.
// </copyright>
// <header>
//      <author>Deepthi Bitra</author>
//      <description>
//          Business Entity For Production Management
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces

#endregion

using System;
namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ProductionSetHeaderFooterFont
    {
        /// <summary>
        /// Gets or sets the HeaderFooterColour
        /// </summary>
        /// <value>HeaderFooterColour</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string HeaderFooterColor
        { set; get; }


        /// <summary>
        /// Gets or sets the HeaderFooterStyle
        /// </summary>
        /// <value>HeaderFooterStyle</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public FontStyles HeaderFooterStyle
        { set; get; }


        /// <summary>
        /// Gets or sets the HeaderFooterFont
        /// </summary>
        /// <value>HeaderFooterFont</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string HeaderFooterFont
        { set; get; }

        /// <summary>
        /// Gets or sets the HeaderFooterFontSize
        /// </summary>
        /// <value>HeaderFooterFontSize</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string HeaderFooterFontSize
        { set; get; }



        /// <summary>
        /// Gets or sets the HeaderFooterHeight
        /// </summary>
        /// <value>HeaderFooterHeight</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public int HeaderFooterHeight
        { set; get; }
    }
}
