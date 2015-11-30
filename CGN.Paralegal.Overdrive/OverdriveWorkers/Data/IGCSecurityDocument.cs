namespace LexisNexis.Evolution.Worker.Data
{

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class IGCSecurityDocument
    {

        private object[] itemsField;

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("DateExpired", typeof(IGCSecurityDocumentDateExpired))]
        [System.Xml.Serialization.XmlElementAttribute("IsoBanners", typeof(IGCSecurityDocumentIsoBanners))]
        [System.Xml.Serialization.XmlElementAttribute("Password", typeof(IGCSecurityDocumentPassword))]
        [System.Xml.Serialization.XmlElementAttribute("RightFlags", typeof(IGCSecurityDocumentRightFlags))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentDateExpired
    {

        private string enableField;

        private string relativedaysField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string relativedays
        {
            get
            {
                return this.relativedaysField;
            }
            set
            {
                this.relativedaysField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBanners
    {

        private IGCSecurityDocumentIsoBannersIsoBannerColor[] isoBannerColorField;

        private IGCSecurityDocumentIsoBannersIsoBannerFont[] isoBannerFontField;

        private IGCSecurityDocumentIsoBannersIsoBannerFontHeight[] isoBannerFontHeightField;

        private IGCSecurityDocumentIsoBannersIsoBannerFontStyle[] isoBannerFontStyleField;

        private IGCSecurityDocumentIsoBannersWaterMark[] waterMarkField;

        private IGCSecurityDocumentIsoBannersScreenWaterMark[] screenWaterMarkField;

        private IGCSecurityDocumentIsoBannersScreenBanner[] screenBannerField;

        private IGCSecurityDocumentIsoBannersTopLeft[] topLeftField;

        private IGCSecurityDocumentIsoBannersTopCenter[] topCenterField;

        private IGCSecurityDocumentIsoBannersTopRight[] topRightField;

        private IGCSecurityDocumentIsoBannersLeftTop[] leftTopField;

        private IGCSecurityDocumentIsoBannersLeftCenter[] leftCenterField;

        private IGCSecurityDocumentIsoBannersLeftBottom[] leftBottomField;

        private IGCSecurityDocumentIsoBannersRightTop[] rightTopField;

        private IGCSecurityDocumentIsoBannersRightCenter[] rightCenterField;

        private IGCSecurityDocumentIsoBannersRightBottom[] rightBottomField;

        private IGCSecurityDocumentIsoBannersBottomLeft[] bottomLeftField;

        private IGCSecurityDocumentIsoBannersBottomCenter[] bottomCenterField;

        private IGCSecurityDocumentIsoBannersBottomRight[] bottomRightField;

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("IsoBannerColor")]
        public IGCSecurityDocumentIsoBannersIsoBannerColor[] IsoBannerColor
        {
            get
            {
                return this.isoBannerColorField;
            }
            set
            {
                this.isoBannerColorField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("IsoBannerFont")]
        public IGCSecurityDocumentIsoBannersIsoBannerFont[] IsoBannerFont
        {
            get
            {
                return this.isoBannerFontField;
            }
            set
            {
                this.isoBannerFontField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("IsoBannerFontHeight")]
        public IGCSecurityDocumentIsoBannersIsoBannerFontHeight[] IsoBannerFontHeight
        {
            get
            {
                return this.isoBannerFontHeightField;
            }
            set
            {
                this.isoBannerFontHeightField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("IsoBannerFontStyle")]
        public IGCSecurityDocumentIsoBannersIsoBannerFontStyle[] IsoBannerFontStyle
        {
            get
            {
                return this.isoBannerFontStyleField;
            }
            set
            {
                this.isoBannerFontStyleField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("WaterMark")]
        public IGCSecurityDocumentIsoBannersWaterMark[] WaterMark
        {
            get
            {
                return this.waterMarkField;
            }
            set
            {
                this.waterMarkField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("ScreenWaterMark")]
        public IGCSecurityDocumentIsoBannersScreenWaterMark[] ScreenWaterMark
        {
            get
            {
                return this.screenWaterMarkField;
            }
            set
            {
                this.screenWaterMarkField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("ScreenBanner")]
        public IGCSecurityDocumentIsoBannersScreenBanner[] ScreenBanner
        {
            get
            {
                return this.screenBannerField;
            }
            set
            {
                this.screenBannerField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("TopLeft")]
        public IGCSecurityDocumentIsoBannersTopLeft[] TopLeft
        {
            get
            {
                return this.topLeftField;
            }
            set
            {
                this.topLeftField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("TopCenter")]
        public IGCSecurityDocumentIsoBannersTopCenter[] TopCenter
        {
            get
            {
                return this.topCenterField;
            }
            set
            {
                this.topCenterField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("TopRight")]
        public IGCSecurityDocumentIsoBannersTopRight[] TopRight
        {
            get
            {
                return this.topRightField;
            }
            set
            {
                this.topRightField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("LeftTop")]
        public IGCSecurityDocumentIsoBannersLeftTop[] LeftTop
        {
            get
            {
                return this.leftTopField;
            }
            set
            {
                this.leftTopField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("LeftCenter")]
        public IGCSecurityDocumentIsoBannersLeftCenter[] LeftCenter
        {
            get
            {
                return this.leftCenterField;
            }
            set
            {
                this.leftCenterField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("LeftBottom")]
        public IGCSecurityDocumentIsoBannersLeftBottom[] LeftBottom
        {
            get
            {
                return this.leftBottomField;
            }
            set
            {
                this.leftBottomField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("RightTop")]
        public IGCSecurityDocumentIsoBannersRightTop[] RightTop
        {
            get
            {
                return this.rightTopField;
            }
            set
            {
                this.rightTopField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("RightCenter")]
        public IGCSecurityDocumentIsoBannersRightCenter[] RightCenter
        {
            get
            {
                return this.rightCenterField;
            }
            set
            {
                this.rightCenterField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("RightBottom")]
        public IGCSecurityDocumentIsoBannersRightBottom[] RightBottom
        {
            get
            {
                return this.rightBottomField;
            }
            set
            {
                this.rightBottomField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("BottomLeft")]
        public IGCSecurityDocumentIsoBannersBottomLeft[] BottomLeft
        {
            get
            {
                return this.bottomLeftField;
            }
            set
            {
                this.bottomLeftField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("BottomCenter")]
        public IGCSecurityDocumentIsoBannersBottomCenter[] BottomCenter
        {
            get
            {
                return this.bottomCenterField;
            }
            set
            {
                this.bottomCenterField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("BottomRight")]
        public IGCSecurityDocumentIsoBannersBottomRight[] BottomRight
        {
            get
            {
                return this.bottomRightField;
            }
            set
            {
                this.bottomRightField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersIsoBannerColor
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersIsoBannerFont
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersIsoBannerFontHeight
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersIsoBannerFontStyle
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersWaterMark
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersScreenWaterMark
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersScreenBanner
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersTopLeft
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersTopCenter
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersTopRight
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersLeftTop
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersLeftCenter
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersLeftBottom
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersRightTop
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersRightCenter
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersRightBottom
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersBottomLeft
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersBottomCenter
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentIsoBannersBottomRight
    {

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentPassword
    {

        private string enableField;

        private string stringField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string @string
        {
            get
            {
                return this.stringField;
            }
            set
            {
                this.stringField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlags
    {

        private IGCSecurityDocumentRightFlagsPrinting[] printingField;

        private IGCSecurityDocumentRightFlagsMeasurement[] measurementField;

        private IGCSecurityDocumentRightFlagsCopyToClipboard[] copyToClipboardField;

        private IGCSecurityDocumentRightFlagsChangeLayers[] changeLayersField;

        private IGCSecurityDocumentRightFlagsRepublishing[] republishingField;

        private IGCSecurityDocumentRightFlagsReviewMarkups[] reviewMarkupsField;

        private IGCSecurityDocumentRightFlagsAuthorAndReviewMarkups[] authorAndReviewMarkupsField;

        private IGCSecurityDocumentRightFlagsBurnInMarkups[] burnInMarkupsField;

        private IGCSecurityDocumentRightFlagsSaveAs[] saveAsField;

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("Printing")]
        public IGCSecurityDocumentRightFlagsPrinting[] Printing
        {
            get
            {
                return this.printingField;
            }
            set
            {
                this.printingField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("Measurement")]
        public IGCSecurityDocumentRightFlagsMeasurement[] Measurement
        {
            get
            {
                return this.measurementField;
            }
            set
            {
                this.measurementField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("CopyToClipboard")]
        public IGCSecurityDocumentRightFlagsCopyToClipboard[] CopyToClipboard
        {
            get
            {
                return this.copyToClipboardField;
            }
            set
            {
                this.copyToClipboardField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("ChangeLayers")]
        public IGCSecurityDocumentRightFlagsChangeLayers[] ChangeLayers
        {
            get
            {
                return this.changeLayersField;
            }
            set
            {
                this.changeLayersField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("Republishing")]
        public IGCSecurityDocumentRightFlagsRepublishing[] Republishing
        {
            get
            {
                return this.republishingField;
            }
            set
            {
                this.republishingField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("ReviewMarkups")]
        public IGCSecurityDocumentRightFlagsReviewMarkups[] ReviewMarkups
        {
            get
            {
                return this.reviewMarkupsField;
            }
            set
            {
                this.reviewMarkupsField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("AuthorAndReviewMarkups")]
        public IGCSecurityDocumentRightFlagsAuthorAndReviewMarkups[] AuthorAndReviewMarkups
        {
            get
            {
                return this.authorAndReviewMarkupsField;
            }
            set
            {
                this.authorAndReviewMarkupsField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("BurnInMarkups")]
        public IGCSecurityDocumentRightFlagsBurnInMarkups[] BurnInMarkups
        {
            get
            {
                return this.burnInMarkupsField;
            }
            set
            {
                this.burnInMarkupsField = value;
            }
        }

        /// <remarks/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), System.Xml.Serialization.XmlElementAttribute("SaveAs")]
        public IGCSecurityDocumentRightFlagsSaveAs[] SaveAs
        {
            get
            {
                return this.saveAsField;
            }
            set
            {
                this.saveAsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsPrinting
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsMeasurement
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsCopyToClipboard
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsChangeLayers
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsRepublishing
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsReviewMarkups
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsAuthorAndReviewMarkups
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsBurnInMarkups
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.1432")]
    [System.SerializableAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IGCSecurityDocumentRightFlagsSaveAs
    {

        private string enableField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute]
        public string enable
        {
            get
            {
                return this.enableField;
            }
            set
            {
                this.enableField = value;
            }
        }
    }
}