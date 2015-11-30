#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ProductionHeaderFooter.cs" company="Cognizant">
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
    public class ProductionSetHeaderFooter
    {
        /// <summary>
        /// Gets or sets the ProductionHeaderFooterOptions.
        /// </summary>
        /// <value>ProductionHeaderFooterOptions.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public HeaderFooterOptions Option
        { set; get; }

        /// <summary>
        /// Gets or sets the TextPrefix
        /// </summary>
        /// <value>TextPrefix</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string TextPrefix
        { set; get; }

        /// <summary>
        /// Gets or sets the TextStartingNumber
        /// </summary>
        /// <value>TextStartingNumber.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string TextStartingNumber
        { set; get; }


        /// <summary>
        /// Gets or sets the IsIncrementNeededInText
        /// </summary>
        /// <value>IsIncrementNeededInText</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncrementNeededInText
        { set; get; }


        /// <summary>
        /// Gets or sets the DatasetFieldSelected
        /// </summary>
        /// <value>DatasetFieldSelected</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public int DatasetFieldSelected
        { set; get; }
    }
}
