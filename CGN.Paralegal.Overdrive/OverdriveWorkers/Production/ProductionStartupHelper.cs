#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ProductionStartupHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Henry</author>
//      <description>
//          This file contains all the  methods related to  ProductionStartupHelper
//      </description>
//      <changelog>
//          <date value="05-15-2013">Initial: Reconversion Processing</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

using System;
using System.Globalization;
using System.IO;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.MatterManagement;
using LexisNexis.Evolution.Business.ServerManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Worker.Data;

namespace LexisNexis.Evolution.Worker
{
    public class ProductionStartupHelper
    {

        public static ProductionDocumentDetail ConstructProductionModelDocument(ProductionDetailsBEO m_BootParameters)
        {
            var m_ProductionDocumentDetail = new ProductionDocumentDetail();
            m_ProductionDocumentDetail.MatterId = m_BootParameters.MatterId.ToString(CultureInfo.InvariantCulture);
            m_ProductionDocumentDetail.CreatedBy = m_BootParameters.CreatedBy;
            m_ProductionDocumentDetail.DocumentSelectionContext = m_BootParameters.DocumentSelectionContext;
            m_ProductionDocumentDetail.DocumentExclusionContext = m_BootParameters.DocumentExclusionContext;
            m_ProductionDocumentDetail.ProductionCollectionId = m_BootParameters.CollectionId;
            m_ProductionDocumentDetail.Profile = ConvertToProfileDataObjects(m_BootParameters);
            m_ProductionDocumentDetail.Profile.DatasetId = Convert.ToInt64(null); //TODO: check why we have this
            GetMatterDatasetDetails(m_ProductionDocumentDetail, m_BootParameters);
            m_ProductionDocumentDetail.GetText = m_BootParameters.Profile.GetScrubbedText;
            var productionExtractionPath = Path.Combine(m_BootParameters.Profile.ProductionSetLocation,
                                                       m_BootParameters.Profile.ProductionSetName);
            
            if (!Directory.Exists(productionExtractionPath))
            {
                Directory.CreateDirectory(productionExtractionPath);
            }

            m_ProductionDocumentDetail.ExtractionLocation = productionExtractionPath;
            m_ProductionDocumentDetail.ArchivePath = m_BootParameters.Profile.ProductionSetLocation;
            m_ProductionDocumentDetail.NearNativeConversionPriority = m_BootParameters.NearNativeConversionPriority;

            return m_ProductionDocumentDetail;

        }

        /// <summary>
        /// Converts to profile data objects.
        /// </summary>
        /// <returns></returns>
        private static ProductionProfile ConvertToProfileDataObjects(ProductionDetailsBEO m_BootParameters)
        {
            var profile = new ProductionProfile
            {
                DatasetId = m_BootParameters.Profile.DatasetId,
                DpnPrefix = m_BootParameters.Profile.DpnPrefix,
                DpnStartingNumber = m_BootParameters.Profile.DpnStartingNumber,
                HeaderFooterFontSelection = new ProductionSetHeaderFooterFont
                {
                    HeaderFooterColor = m_BootParameters.Profile.HeaderFooterFontSelection != null ? m_BootParameters.Profile.HeaderFooterFontSelection.HeaderFooterColor : null,
                    HeaderFooterFont = m_BootParameters.Profile.HeaderFooterFontSelection != null ? m_BootParameters.Profile.HeaderFooterFontSelection.HeaderFooterFont : null,
                    HeaderFooterFontSize = m_BootParameters.Profile.HeaderFooterFontSelection != null ? m_BootParameters.Profile.HeaderFooterFontSelection.HeaderFooterFontSize : null,
                    HeaderFooterHeight = m_BootParameters.Profile.HeaderFooterFontSelection != null ? m_BootParameters.Profile.HeaderFooterFontSelection.HeaderFooterHeight : 0,
                    HeaderFooterStyle = m_BootParameters.Profile.HeaderFooterFontSelection != null ? (FontStyles)m_BootParameters.Profile.HeaderFooterFontSelection.HeaderFooterStyle : FontStyles.Normal
                },
                ImageType = (ImageType)m_BootParameters.Profile.ImageType,
                IsBurnMarkups = m_BootParameters.Profile.IsBurnMarkups,
                IsIncludeArrowsMarkup = m_BootParameters.Profile.IsIncludeArrowsMarkup,
                IsIncludeBoxesMarkup = m_BootParameters.Profile.IsIncludeBoxesMarkup,
                IsIncludeHighlightsMarkup = m_BootParameters.Profile.IsIncludeHighlightsMarkup,
                IsIncludeLinesMarkup = m_BootParameters.Profile.IsIncludeLinesMarkup,
                IsIncludeReasonsWithMarkup = m_BootParameters.Profile.IsIncludeReasonsWithMarkup,
                IsIncludeRedactionsMarkup = m_BootParameters.Profile.IsIncludeRedactionsMarkup,
                IsIncludeRubberStampMarkup = m_BootParameters.Profile.IsIncludeRubberStampMarkup,
                IsIncludeTextBoxMarkup = m_BootParameters.Profile.IsIncludeTextBoxMarkup,
                IsInsertPlaceHolderPage = m_BootParameters.Profile.IsInsertPlaceHolderPage,
                IsOneImagePerPage = m_BootParameters.Profile.IsOneImagePerPage,
                IsPrintDCNInPlaceHolderPage = m_BootParameters.Profile.IsPrintDCNInPlaceHolderPage,
                IsPrintDPNInPlaceHolderPage = m_BootParameters.Profile.IsPrintDPNInPlaceHolderPage,
                LeftFooter = new ProductionSetHeaderFooter
                {
                    DatasetFieldSelected = m_BootParameters.Profile.LeftFooter.DatasetFieldSelected,
                    IsIncrementNeededInText = m_BootParameters.Profile.LeftFooter.IsIncrementNeededInText,
                    Option = (HeaderFooterOptions)m_BootParameters.Profile.LeftFooter.Option,
                    TextPrefix = m_BootParameters.Profile.LeftFooter.TextPrefix,
                    TextStartingNumber = m_BootParameters.Profile.LeftFooter.TextStartingNumber
                },
                LeftHeader = new ProductionSetHeaderFooter
                {
                    DatasetFieldSelected = m_BootParameters.Profile.LeftHeader.DatasetFieldSelected,
                    IsIncrementNeededInText = m_BootParameters.Profile.LeftHeader.IsIncrementNeededInText,
                    Option = (HeaderFooterOptions)m_BootParameters.Profile.LeftHeader.Option,
                    TextPrefix = m_BootParameters.Profile.LeftHeader.TextPrefix,
                    TextStartingNumber = m_BootParameters.Profile.LeftHeader.TextStartingNumber
                },
                MiddleFooter = new ProductionSetHeaderFooter
                {
                    DatasetFieldSelected = m_BootParameters.Profile.MiddleFooter.DatasetFieldSelected,
                    IsIncrementNeededInText = m_BootParameters.Profile.MiddleFooter.IsIncrementNeededInText,
                    Option = (HeaderFooterOptions)m_BootParameters.Profile.MiddleFooter.Option,
                    TextPrefix = m_BootParameters.Profile.MiddleFooter.TextPrefix,
                    TextStartingNumber = m_BootParameters.Profile.MiddleFooter.TextStartingNumber

                },
                MiddleHeader = new ProductionSetHeaderFooter
                {
                    DatasetFieldSelected = m_BootParameters.Profile.MiddleHeader.DatasetFieldSelected,
                    IsIncrementNeededInText = m_BootParameters.Profile.MiddleHeader.IsIncrementNeededInText,
                    Option = (HeaderFooterOptions)m_BootParameters.Profile.MiddleHeader.Option,
                    TextPrefix = m_BootParameters.Profile.MiddleHeader.TextPrefix,
                    TextStartingNumber = m_BootParameters.Profile.MiddleHeader.TextStartingNumber

                },
                PrintDCNInPlaceHolderPrefferedText = m_BootParameters.Profile.PrintDCNInPlaceHolderPrefferedText,
                ProductionPrefix = m_BootParameters.Profile.ProductionPrefix,
                ProductionSetNumberingType = (ProductionNumbering)m_BootParameters.Profile.ProductionSetNumberingType,
                ProductionStartingNumber = m_BootParameters.Profile.ProductionStartingNumber,
                ProfileId = m_BootParameters.Profile.ProfileId,
                ProfileName = m_BootParameters.Profile.ProfileName,
                RightFooter = new ProductionSetHeaderFooter
                {
                    DatasetFieldSelected = m_BootParameters.Profile.RightFooter.DatasetFieldSelected,
                    IsIncrementNeededInText = m_BootParameters.Profile.RightFooter.IsIncrementNeededInText,
                    Option = (HeaderFooterOptions)m_BootParameters.Profile.RightFooter.Option,
                    TextPrefix = m_BootParameters.Profile.RightFooter.TextPrefix,
                    TextStartingNumber = m_BootParameters.Profile.RightFooter.TextStartingNumber
                },
                RightHeader = new ProductionSetHeaderFooter
                {
                    DatasetFieldSelected = m_BootParameters.Profile.RightHeader.DatasetFieldSelected,
                    IsIncrementNeededInText = m_BootParameters.Profile.RightHeader.IsIncrementNeededInText,
                    Option = (HeaderFooterOptions)m_BootParameters.Profile.RightHeader.Option,
                    TextPrefix = m_BootParameters.Profile.RightHeader.TextPrefix,
                    TextStartingNumber = m_BootParameters.Profile.RightHeader.TextStartingNumber

                },
                TiffImageColor = (TiffImageColor)m_BootParameters.Profile.TiffImageColor
            };
            return profile;
        }


        private static void GetMatterDatasetDetails(ProductionDocumentDetail m_ProductionDocumentDetail, ProductionDetailsBEO m_BootParameters)
        {
            DatasetBEO dataset =
                DataSetBO.GetDataSetDetailForCollectionId(m_BootParameters.OriginalCollectionId);
            m_ProductionDocumentDetail.OriginalCollectionId = dataset.RedactableDocumentSetId;
            //Assign redactable set id as default collection id
            m_ProductionDocumentDetail.DatasetCollectionId = m_BootParameters.OriginalCollectionId;
            //Native set collection id
            m_ProductionDocumentDetail.OriginalDatasetName = dataset.FolderName;
            m_ProductionDocumentDetail.OriginalDatasetId = (int)dataset.FolderID;

            m_ProductionDocumentDetail.lstProductionFields =
                DataSetBO.GetDataSetFields(Convert.ToInt64(m_ProductionDocumentDetail.OriginalDatasetId),
                                           m_ProductionDocumentDetail.ProductionCollectionId);
            m_ProductionDocumentDetail.dataSetBeo =
                DataSetBO.GetDataSetDetailForDataSetId(Convert.ToInt64(m_ProductionDocumentDetail.OriginalDatasetId));
            m_ProductionDocumentDetail.lstDsFieldsBeo =
                DataSetBO.GetDataSetFields(Convert.ToInt64(m_ProductionDocumentDetail.OriginalDatasetId),
                                           m_ProductionDocumentDetail.DatasetCollectionId);
            m_ProductionDocumentDetail.matterBeo =
                MatterBO.GetMatterDetails(m_ProductionDocumentDetail.dataSetBeo.ParentID.ToString(CultureInfo.InvariantCulture));
            m_ProductionDocumentDetail.SearchServerDetails =
                ServerBO.GetSearchServer(m_ProductionDocumentDetail.matterBeo.SearchServer.Id);
        }


    }
}
