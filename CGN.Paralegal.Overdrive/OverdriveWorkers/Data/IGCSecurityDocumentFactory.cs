# region File Header
/*******************************************************************************************
 * File Name        :   IGCSecurityDocumentFactory.cs
 * File Description :   Factory object to create IGCSecurityDocument object
 * Author(s)        :   Cognizant
 * *****************************************************************************************
 * Change Log
 * *****************************************************************************************
 * Date                 Comments 
 * 
 * *****************************************************************************************/
#endregion

#region Namespaces

#endregion

namespace LexisNexis.Evolution.Worker.Data
{
        public static class IGCSecurityDocumentFactory
        {
            //Factory method to get the IGCSecurityDocument
            public static IGCSecurityDocument GetIGCSecurityDocument()
            {

                IGCSecurityDocument igcSecurityDocument = new IGCSecurityDocument {Items = new object[4]};

                igcSecurityDocument.Items[0] = new IGCSecurityDocumentDateExpired();
                igcSecurityDocument.Items[1] = new IGCSecurityDocumentRightFlags();
                igcSecurityDocument.Items[2] = new IGCSecurityDocumentPassword();
                igcSecurityDocument.Items[3] = new IGCSecurityDocumentIsoBanners();


                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerColor = new IGCSecurityDocumentIsoBannersIsoBannerColor[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerColor[0] = new IGCSecurityDocumentIsoBannersIsoBannerColor();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerFont = new IGCSecurityDocumentIsoBannersIsoBannerFont[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerFont[0] = new IGCSecurityDocumentIsoBannersIsoBannerFont();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerFontHeight = new IGCSecurityDocumentIsoBannersIsoBannerFontHeight[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerFontHeight[0] = new IGCSecurityDocumentIsoBannersIsoBannerFontHeight();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerFontStyle = new IGCSecurityDocumentIsoBannersIsoBannerFontStyle[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).IsoBannerFontStyle[0] = new IGCSecurityDocumentIsoBannersIsoBannerFontStyle();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).TopLeft = new IGCSecurityDocumentIsoBannersTopLeft[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).TopLeft[0] = new IGCSecurityDocumentIsoBannersTopLeft();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).TopCenter = new IGCSecurityDocumentIsoBannersTopCenter[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).TopCenter[0] = new IGCSecurityDocumentIsoBannersTopCenter();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).TopRight = new IGCSecurityDocumentIsoBannersTopRight[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).TopRight[0] = new IGCSecurityDocumentIsoBannersTopRight();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).BottomLeft = new IGCSecurityDocumentIsoBannersBottomLeft[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).BottomLeft[0] = new IGCSecurityDocumentIsoBannersBottomLeft();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).BottomCenter = new IGCSecurityDocumentIsoBannersBottomCenter[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).BottomCenter[0] = new IGCSecurityDocumentIsoBannersBottomCenter();

                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).BottomRight = new IGCSecurityDocumentIsoBannersBottomRight[1];
                ((IGCSecurityDocumentIsoBanners)igcSecurityDocument.Items[3]).BottomRight[0] = new IGCSecurityDocumentIsoBannersBottomRight();

                return igcSecurityDocument;
            }
        }
    }