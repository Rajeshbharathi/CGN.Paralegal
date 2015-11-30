
#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ProductionImagingWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Prabhu/Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  ProductionImagingWorker
//      </description>
//      <changelog>
//          <date value="04/06/2013">ADM -ADMIN-002 -  Near Native Conversion Priority</date>
//          <date value="05-12-2013">Task # 134432-ADM 03 -Re Convresion</date>
//          <date value="05-21-2013">Bug # 142937,143536 and 143037 -ReConvers Buddy Defects</date>
//          <date value="06-27-2013">Bug # 146526 -Disposing WebResponse object</date>
//          <date value="07-02-2013">Bug # 146561 -Production reprocessing and conversion engine failiure case</date>
//          <date value="07-03-2013">Bug # 146561 and 145022 - Fix to show  all the documents are listing out in production manage conversion screen</date>
//         <date value="07-03-2013">Bug # 147839  Fix to reprocess the failed documents in production when redactable set is imageset</date>
//          <date value="10/29/2013">Bug  # 155811 & 156500 - Fix to dead lock exception occured in update process set entity and sort production set documents in proper dcn order
//          <date value="10/29/2013">Bug  # 155811  - Fix to  sort production set documents in proper dcn order
//          <date value="10/23/2013">Bug # 156607 - Fix to avoid infinite conversion validation for documents without heartbeat files by having absolute timeout
//          <date value="03/24/2015">Bug Fix 184140 - Publish blank pages</date>
//          <date value="10/01/2015">Making sure that reprocessing passes right conversion priority to IGC</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Infrastructure.ServerManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.TraceServices;
using OverdriveWorkers.Production;

namespace LexisNexis.Evolution.Worker
{
    using Infrastructure.ExceptionManagement;

    public class ProductionImagingWorker : WorkerBase
    {
        private const string NearNativeViewer = "NearNativeViewer";

        private const string PriorityQueryStringName = "&priority=";
        private static IDocumentVaultManager _mDocumentVaultMngr = null;
        private static long _matterId;
        /// <summary>
        /// The _conversion priority
        /// </summary>
        private int _conversionPriority;
        /// <summary>
        /// The is reprocess job
        /// </summary>
        private bool _isReprocessJob;

        /// <summary>
        /// Begins the worker process.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();
            try
            {
                _mDocumentVaultMngr = EVUnityContainer.Resolve<IDocumentVaultManager>("DocumentVaultManager");
                _isReprocessJob = !string.IsNullOrEmpty(PipelineType.Moniker) &&
                                  PipelineType.Moniker.ToLower().Contains("conversionreprocess");
                _conversionPriority = GetConversionPriority();
                Tracer.Info("Conversion priority {0}",_conversionPriority);

            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

      
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            if (envelope.Body == null)
            {
                return;
            }

            var productionDocuments = (List<ProductionDocumentDetail>)envelope.Body;

            try
            {
                ProcessMessageForProduction(productionDocuments);
                Send(productionDocuments);
                IncreaseProcessedDocumentsCount(productionDocuments.Count);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        /// <summary>
        /// Processes the message for production.
        /// </summary>
        /// <param name="productionDocuments">The production documents.</param>
        private  void ProcessMessageForProduction(List<ProductionDocumentDetail> productionDocuments)
        {
            var documentConversionLogBeos = new List<DocumentConversionLogBeo>();
            if (productionDocuments == null || !productionDocuments.Any()) return;
            var productionDocument = productionDocuments.FirstOrDefault();
            if (productionDocument != null && !String.IsNullOrEmpty(productionDocument.MatterId))
                _matterId = Convert.ToInt64(productionDocument.MatterId);
            var errorDocuments = new List<ProductionDocumentDetail>();
            foreach (var productionDocumentDetail in productionDocuments)
            {
                byte conversionStatus = EVRedactItErrorCodes.Submitted;
                short reasonId = EVRedactItErrorCodes.Na;
               try
                {
                    ProductionProfile profileBusinessEntity = productionDocumentDetail.Profile;
                    TiffImageColor tiffImageColor;
                    string hostId = ServerConnectivity.GetHostIPAddress();
                    string redactitPushUrl = CmgServiceConfigBO.GetServiceConfigurationsforConfig
                        (hostId, External.DataAccess.Constants.SystemConfigurationService,"QueueServerUrl");
                                                                                                            
                    string redactitTimeout = GetConfigurationValue("RedactItTimeout", NearNativeViewer);
                    string fileTypeKeyword = "pdf";
                    string thumbNailFormat = string.Empty;
                    string tiffBpp = string.Empty;
                    string tiffMonochrome = string.Empty;
                    string oneFilePerPage = string.Empty;

                    //Append the push url
                    var uri = new StringBuilder(productionDocumentDetail.QueryString);

                    //Apply the 6.outputtype
                    if (profileBusinessEntity != null)
                    {
                        switch (profileBusinessEntity.ImageType)
                        {
                            case ImageType.Jpg:
                                fileTypeKeyword = Constants.PdfKeyword;
                                thumbNailFormat = Constants.JpgKeyword;
                                break;
                            case ImageType.Png:
                                fileTypeKeyword = Constants.PdfKeyword;
                                thumbNailFormat = Constants.PngKeyword;
                                break;
                            case ImageType.Tiff:
                                fileTypeKeyword = Constants.TiffKeyword;
                                tiffImageColor = profileBusinessEntity.TiffImageColor;
                                if (tiffImageColor == TiffImageColor.One) //monochrome
                                {
                                    tiffMonochrome = Constants.TrueString;
                                }
                                else
                                {
                                    tiffBpp = ((int) tiffImageColor).ToString();
                                }
                                if (profileBusinessEntity.IsOneImagePerPage)
                                {
                                    oneFilePerPage = Constants.TrueString;
                                }
                                break;
                            default:
                                fileTypeKeyword = Constants.PdfKeyword;
                                if (profileBusinessEntity.IsOneImagePerPage)
                                {
                                    oneFilePerPage = Constants.TrueString;
                                }
                                break;
                        }
                    }
                    uri.Append(Constants.QueryStringOutputFormatPrefix);
                    uri.Append(fileTypeKeyword);

                    uri.Append("&StepTimeout=");
                    uri.Append(redactitTimeout);

                    //Apply the 7.Redact It Job priority
                    uri.Append(PriorityQueryStringName);
                    //uri.Append(_nearNativeConversionPriority.ToString());
                    uri.Append(_conversionPriority);

                    //Apply the 8.thumbnails elements if reqd
                    if (!String.IsNullOrEmpty(thumbNailFormat))
                    {
                        uri.Append(Constants.QueryStringThumbFormatPrefix);
                        uri.Append(thumbNailFormat);
                        uri.Append(Constants.QueryStringThumbPagesPrefix);
                        uri.Append(Constants.ThumbPagesAll);
                        // a-All f-firstpageonly 1-Pagenumber
                        uri.Append(Constants.QueryStringThumbNamePrefix);
                        uri.Append((productionDocumentDetail.XdlThumbFileName == string.Empty
                                        ? Constants.ThumbDefaultPageName
                                        : productionDocumentDetail.XdlThumbFileName));
                        //this is mandatory if jpeg or png needed.
                        uri.Append(Constants.QueryStringThumbQualityPrefix);
                        uri.Append(Constants.ThumbQuality); //1-100
                        uri.Append(Constants.QueryStringThumbSizesPrefix);
                        uri.Append(Constants.ThumbDefaultSizes); //1000,1000
                    }

                    //Apply 9. tiff colour if applicable
                    if (!String.IsNullOrEmpty(tiffBpp))
                    {
                        uri.Append(Constants.QueryStringTiffBppPrefix);
                        uri.Append(tiffBpp);
                        uri.Append(Constants.QueryStringTifDPI);
                    }

                    //Apply 10. tiff monochrome if applicable
                    if (!String.IsNullOrEmpty(tiffMonochrome))
                    {
                        uri.Append(Constants.QueryStringTiffMonochromePrefix);
                        uri.Append(tiffMonochrome);
                        uri.Append(Constants.QueryStringTifDPI);
                    }

                    //Apply 11. Check if one file per page is needed
                    if (!String.IsNullOrEmpty(oneFilePerPage))
                    {
                        uri.Append(Constants.QueryStringOneFilePerPagePrefix);
                        uri.Append(oneFilePerPage);
                    }

                    uri.Append(Constants.PublishBlankPagesQueryString);
                    uri.Append(CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.PublishBlankPages));

                    uri.Append(Constants.QueryStringScrubbedText);
                    uri.Append(productionDocumentDetail.GetText.ToString());


                    //TODO: Log the data

                    var request = WebRequest.Create(redactitPushUrl);
                    byte[] byteArray = Encoding.UTF8.GetBytes(uri.ToString());
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = byteArray.Length;
                    using (var dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        using (var response = request.GetResponse())
                        {
                            string status = ((HttpWebResponse) response).StatusDescription.ToUpper().Trim();
                            
                            if (!status.Equals(Constants.OkKeyword))
                            {
                                    Tracer.Warning(
                                        " DCN = {0}, DocumentId = {1}, CollectionId = {2} and HeartBeatFile {3}\r\n",
                                        productionDocumentDetail.DCNNumber,
                                        productionDocumentDetail.DocumentId,
                                        productionDocumentDetail.DatasetCollectionId,
                                        productionDocumentDetail.HeartBeatFile);
                            }
                        }

                    }
                        productionDocumentDetail.ConversionEnqueueTime = DateTime.UtcNow;
                       
                }
                catch (Exception ex)
                {

                    ex.AddUsrMsg("Production Imaging Worker: Unable to produce the document DCN: {0}",
                                 productionDocumentDetail.DCNNumber);
                    ex.Trace().Swallow();
                    conversionStatus = EVRedactItErrorCodes.Failed;
                    reasonId = EVRedactItErrorCodes.FailedToSendFile;
                    productionDocument.ErrorMessage = string.Format("Document with DCN:{0} is {1}-{2}", productionDocumentDetail.DCNNumber, Constants.ProductionPreFailure, ex.Message);
                    errorDocuments.Add(productionDocument);
                   
                  
                }
                documentConversionLogBeos.Add(ConvertToDocumentConversionLogBeo(productionDocumentDetail,
                                                                                  conversionStatus,
                                                                                  reasonId));
            }
            ProductionLogHelper.SendProductionLogs(LogPipe, errorDocuments, PipelineId, WorkerId, Constants.ProductionImagingWokerRoleId);
            BulkUpdateProcessSetStatus(documentConversionLogBeos);
        }

      
        /// <summary>
        /// Gives the configuration value for a key and sub application
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subapplication"></param>
        /// <returns></returns>
        private static string GetConfigurationValue(string key, string subapplication)
        {
            string result = null;

            result = ApplicationConfigurationManager.GetValue(key, subapplication);
            result = StringUtility.IsNullOrWhiteSpace(result) ? string.Empty : result;

            return result;
        }

        /// <summary>
        /// Sends the specified job details.
        /// </summary>
        /// <param name="jobDetails">The job details.</param>
        private void Send(List<ProductionDocumentDetail> jobDetails)
        {
            var message = new PipeMessageEnvelope()
                              {
                                  Body = jobDetails
                              };
            OutputDataPipe.Send(message);
        }

        /// <summary>
        /// Converts to document conversion log beo.
        /// </summary>
        /// <param name="productionDocumentDetail">The production document detail.</param>
        /// <param name="status">The status.</param>
        /// <param name="reasonId">The reason id.</param>
        /// <returns></returns>
        private DocumentConversionLogBeo ConvertToDocumentConversionLogBeo(ProductionDocumentDetail productionDocumentDetail, byte status, short reasonId)
        {

            if (productionDocumentDetail == null || WorkAssignment == null) return null;
            var documentConversionLogBeo = new DocumentConversionLogBeo
                                               {
                                                   DocumentId = productionDocumentDetail.DocumentId,
                                                   CollectionId = productionDocumentDetail.OriginalCollectionId,
                                                   DCN =productionDocumentDetail.DCNNumber,
                                                   ProcessJobId = WorkAssignment.JobId,
                                                   JobRunId = WorkAssignment.JobId,
                                                   Status = status,
                                                   ReasonId = reasonId,
                                                   ModifiedDate = DateTime.UtcNow
                                               };
            return documentConversionLogBeo;
        }



        /// <summary>
        /// Bulks the update process set status.
        /// </summary>
        /// <param name="documentConversionLogBeos">The document conversion log beos.</param>
        private void BulkUpdateProcessSetStatus(IList<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {
                if (!documentConversionLogBeos.Any()) return;
                //same worker is used for both production and reprocessing pipeline
                //intital marking of process document for reprocessing is done in reprocessing startup worker
                //so here we mark the documents with failed if they are not sent to conversion engine
                
                documentConversionLogBeos = _isReprocessJob
                                                ? documentConversionLogBeos.Where(
                                                    doc => doc.Status == EVRedactItErrorCodes.Failed).ToList()
                                                : documentConversionLogBeos;
                _mDocumentVaultMngr.AddOrUpdateConversionLogs(_matterId, documentConversionLogBeos, _isReprocessJob);
            }
            catch (Exception exception)
            {
                //continue the production process with out updating the conversion /process status
                exception.Trace().Swallow();
            }
        }
        /// <summary>
        /// Gets the conversion priority.
        /// </summary>
        /// <returns></returns>
        private int GetConversionPriority()
        {
            if (_isReprocessJob)
            {
                var reprocessingBootParameters = (ConversionReprocessJobBeo)XmlUtility.DeserializeObject(BootParameters, typeof(ConversionReprocessJobBeo));
                reprocessingBootParameters.ShouldNotBe(null);
                return reprocessingBootParameters.NearNativeConversionPriority;

            }
            var productionBootParameters = (ProductionDetailsBEO)XmlUtility.DeserializeObject(BootParameters, typeof(ProductionDetailsBEO));
            productionBootParameters.ShouldNotBe(null);
            return productionBootParameters.NearNativeConversionPriority;
        }




    }
}
