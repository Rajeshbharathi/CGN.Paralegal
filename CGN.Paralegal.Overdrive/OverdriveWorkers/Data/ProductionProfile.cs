#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ProductionSetProfilesBEO.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
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
    #region Enumerations
    [Serializable]
    public enum ImageType
    {
        Pdf = 0,
        Tiff = 1,
        Jpg = 2,
        Png = 3
    }

    [Serializable]
    public enum TiffImageColor
    {

        One = 0,
        Four = 4,
        Eight = 8,
        TwentyFour = 24
    }

    [Serializable]
    public enum FontStyles
    {
        Normal = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = 3
    }
    [Serializable]
    public enum ProductionNumbering
    {
        ProductionNumber = 1,
        BatesNumber = 2,
        No = 0
    }

    [Serializable]
    public enum HeaderFooterOptions
    {
        None = 0,
        Text = 1,
        DateAndTime = 2,
        Date = 3,
        Time = 4,
        DocumentProductionNumber = 5,
        PageNumber = 6,
        ProductionNumber = 7,
        DatasetField = 8
    }
    
    #endregion

    [Serializable]
    public class ProductionProfile
    {
        #region Private Fields

        private long profileId;
        private string profileName;

        private bool isBurnMarkups;
        private bool isIncludeArrowsMarkup;
        private bool isIncludeBoxesMarkup;
        private bool isIncludeHighlightsMarkup;
        private bool isIncludeLinesMarkup;
        private bool isIncludeRedactionsMarkup;
        private bool isIncludeReasonsWithsMarkup;
        private bool isIncludeTextBoxMarkup;
        private bool isIncludeRubberStampMarkup;

        private ProductionNumbering productionSetNumberingType;
        private string productionPrefix;
        private string productionStartingnumber;
        private string dpnPrefix;
        private string dpnStartingnumber;


        private ProductionSetHeaderFooter leftHeader;
        private ProductionSetHeaderFooter middleHeader;
        private ProductionSetHeaderFooter rightHeader;
        private ProductionSetHeaderFooter leftFooter;
        private ProductionSetHeaderFooter middleFooter;
        private ProductionSetHeaderFooter rightFooter;
        private ProductionSetHeaderFooterFont headerFooterFontSelection;

        private ImageType imageType;
        private TiffImageColor tiffImageColor;

        private bool isOneImagePerPage;
        private long datasetid;

        private bool isInsertPlaceHolderPage;
        private bool isPrintDPNInPlaceHolderPage;
        private bool isPrintDCNInPlaceHolderPage;
        private string printDCNInPlaceHolderPrefferedText;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the profile id.
        /// </summary>
        /// <value>The profile id.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public long ProfileId
        {
            get { return profileId; }
            set { profileId = value; }
        }

        /// <summary>
        /// Gets or sets the DatasetId
        /// </summary>
        /// <value>The DatasetId.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public long DatasetId
        {
            get { return datasetid; }
            set { datasetid = value; }
        }

        /// <summary>
        /// Gets or sets the profile name.
        /// </summary>
        /// <value>The profile name.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string ProfileName
        {
            get { return profileName; }
            set { profileName = value; }
        }

        /// <summary>
        /// Gets or sets the if markups need to be burned.
        /// </summary>
        /// <value>Is markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsBurnMarkups
        {
            get { return isBurnMarkups; }
            set { isBurnMarkups = value; }
        }

        /// <summary>
        /// Gets or sets the if arrow markup needed.
        /// </summary>
        /// <value>Is arrow markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeArrowsMarkup
        {
            get { return isIncludeArrowsMarkup; }
            set { isIncludeArrowsMarkup = value; }
        }

        /// <summary>
        /// Gets or sets the if boxes markup needed.
        /// </summary>
        /// <value>Is boxes markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeBoxesMarkup
        {
            get { return isIncludeBoxesMarkup; }
            set { isIncludeBoxesMarkup = value; }
        }

        /// <summary>
        /// Gets or sets the if highlights markup needed.
        /// </summary>
        /// <value>Is highlights markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeHighlightsMarkup
        {
            get { return isIncludeHighlightsMarkup; }
            set { isIncludeHighlightsMarkup = value; }
        }

        /// <summary>
        /// Gets or sets the if lines markup needed.
        /// </summary>
        /// <value>Is lines markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeLinesMarkup
        {
            get { return isIncludeLinesMarkup; }
            set { isIncludeLinesMarkup = value; }
        }

        /// <summary>
        /// Gets or sets the if redaction markup needed.
        /// </summary>
        /// <value>Is redaction markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeRedactionsMarkup
        {
            get { return isIncludeRedactionsMarkup; }
            set { isIncludeRedactionsMarkup = value; }
        }

        /// <summary>
        /// Gets or sets the if reasons for markup needed.
        /// </summary>
        /// <value>Is reasons for markup needed.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeReasonsWithMarkup
        {
            get { return isIncludeReasonsWithsMarkup; }
            set { isIncludeReasonsWithsMarkup = value; }
        }


        /// <summary>
        /// Gets or sets the left header
        /// </summary>
        /// <value>Left header.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooter LeftHeader
        {
            get { return leftHeader; }
            set { leftHeader = value; }
        }

        /// <summary>
        /// Gets or sets the middle header
        /// </summary>
        /// <value>Middle header.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooter MiddleHeader
        {
            get { return middleHeader; }
            set { middleHeader = value; }
        }

        /// <summary>
        /// Gets or sets the right header
        /// </summary>
        /// <value>Right header</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooter RightHeader
        {
            get { return rightHeader; }
            set { rightHeader = value; }
        }

        /// <summary>
        /// Gets or sets the left footer
        /// </summary>
        /// <value>Left footer.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooter LeftFooter
        {
            get { return leftFooter; }
            set { leftFooter = value; }
        }

        /// <summary>
        /// Gets or sets the middle footer
        /// </summary>
        /// <value>Middle footer.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooter MiddleFooter
        {
            get { return middleFooter; }
            set { middleFooter = value; }
        }

        /// <summary>
        /// Gets or sets the right footer
        /// </summary>
        /// <value>right footer.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooter RightFooter
        {
            get { return rightFooter; }
            set { rightFooter = value; }
        }

        /// <summary>
        /// Gets or sets the header footer font.
        /// </summary>
        /// <value>the header footer font.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionSetHeaderFooterFont HeaderFooterFontSelection
        {
            get { return headerFooterFontSelection; }
            set { headerFooterFontSelection = value; }
        }

        /// <summary>
        /// Gets or sets prod set numbering type.
        /// </summary>
        /// <value>Production set numbering type.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ProductionNumbering ProductionSetNumberingType
        {
            get { return productionSetNumberingType; }
            set { productionSetNumberingType = value; }
        }

        /// <summary>
        /// Gets or sets prod prefix.
        /// </summary>
        /// <value>Production prefix.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string ProductionPrefix
        {
            get { return productionPrefix; }
            set { productionPrefix = value; }
        }

        /// <summary>
        /// Gets or sets prod starting number.
        /// </summary>
        /// <value>Production starting number.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string ProductionStartingNumber
        {
            get { return productionStartingnumber; }
            set { productionStartingnumber = value; }
        }

        /// <summary>
        /// Gets or sets Document production starting number.
        /// </summary>
        /// <value>Document production starting number.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string DpnStartingNumber
        {
            get { return dpnStartingnumber; }
            set { dpnStartingnumber = value; }
        }

        /// <summary>
        /// Gets or sets Document Production Prefix.
        /// </summary>
        /// <value>Document Production Prefix.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public string DpnPrefix
        {
            get { return dpnPrefix; }
            set { dpnPrefix = value; }
        }

        /// <summary>
        /// Gets or sets tiff image color.
        /// </summary>
        /// <value>Tiff image color.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public TiffImageColor TiffImageColor
        {
            get { return tiffImageColor; }
            set { tiffImageColor = value; }
        }

        /// <summary>
        /// Gets or sets Image type.
        /// </summary>
        /// <value>Image type.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public ImageType ImageType
        {
            get { return imageType; }
            set { imageType = value; }
        }

        /// <summary>
        /// Is one image to be generated per page.
        /// </summary>
        /// <value>Boolean value if one image per page.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsOneImagePerPage
        {
            get { return isOneImagePerPage; }
            set { isOneImagePerPage = value; }
        }

        /// <summary>
        /// To include text box mark up.
        /// </summary>
        /// <value>To include text box mark up.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeTextBoxMarkup
        {
            get { return isIncludeTextBoxMarkup; }
            set { isIncludeTextBoxMarkup = value; }
        }

        /// <summary>
        /// To include rubberstamp markup
        /// </summary>
        /// <value>Boolean value if rubber stamp to be included.</value>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsIncludeRubberStampMarkup
        {
            get { return isIncludeRubberStampMarkup; }
            set { isIncludeRubberStampMarkup = value; }
        }

        /// <summary>
        /// Is place holder page needed
        /// </summary>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsInsertPlaceHolderPage
        {
            get { return isInsertPlaceHolderPage; }
            set { isInsertPlaceHolderPage = value; }
        }

        /// <summary>
        /// Is DPN included in place holder page
        /// </summary>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsPrintDPNInPlaceHolderPage
        {
            get { return isPrintDPNInPlaceHolderPage; }
            set { isPrintDPNInPlaceHolderPage = value; }
        }

        /// <summary>
        /// Is DCN included in place holder page
        /// </summary>
        [System.Xml.Serialization.XmlElementAttribute]
        public bool IsPrintDCNInPlaceHolderPage
        {
            get { return isPrintDCNInPlaceHolderPage; }
            set { isPrintDCNInPlaceHolderPage = value; }
        }

        /// <summary>
        /// Preffered text for DCN
        /// </summary>
        [System.Xml.Serialization.XmlElementAttribute]
        public string PrintDCNInPlaceHolderPrefferedText
        {
            get { return printDCNInPlaceHolderPrefferedText; }
            set { printDCNInPlaceHolderPrefferedText = value; }
        }


        #endregion
    }








}
