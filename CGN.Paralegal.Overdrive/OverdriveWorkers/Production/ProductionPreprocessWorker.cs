#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="ProductionPreprocessWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Prabhu/Nagaraju</author>
//      <description>
//          This file has constants for Prodcution Preprocessor Worker
//      </description>
//      <changelog>
//          <date value="03/02/2012">Bug Fix 86335</date>
//          <date value="16/02/2012">Bug Fix 96588</date>
//          <date value="20/02/2012">97039- BVT issue fix</date>
//          <date value="02/03/2012">Bug fix 95615</date>
//          <date value="03/14/2012">Bug fix 97522</date>
//          <date value="03/15/2012">code warnings fix</date>
//          <date value="05/03/2012">Task #100232 </date>
//          <date value="02/26/2013">Bug Fix # 130801 </date>
//          <date value="01/23/2014">Bug Fix # 162524 - Added null check for the argument </date>
//          <date value="02/11/2014">Bug Fix # 163050, 162963 - Production unicode issue fix </date>
//          <date value="06/17/2014">NLog Exception fixing to avoid exception due to maximum querystring</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business.ProductionManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.Common;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Infrastructure;
using System.Web;
namespace LexisNexis.Evolution.Worker
{
    using Infrastructure.ExceptionManagement;
    using Business.CentralizedConfigurationManagement;

    public class ProductionPreprocessWorker : WorkerBase
    {
        private readonly List<string> _mOriginalCollectionIds = new List<string>(); //to get the dataset names for the Eror log
        private const string ConfigKeyNearNativeViewer = "NearNativeViewer";
        private List<DocumentBinaryEntity> _documentBinary;
        private List<Int32> _lstBatesAndDpnFieldTypes = new List<int> { 3004, 3005, 3006, 3007, 3008 };
        
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            var productionDocuments = (List<ProductionDocumentDetail>)envelope.Body;
            ProcessTheDocument(productionDocuments);
        }

        /// <summary>
        /// Processes the data.
        /// </summary>
        /// <param name="productionDocuments">The production documents.</param>
        public void ProcessTheDocument(List<ProductionDocumentDetail> productionDocuments)
        {
            var jobDetails = new List<ProductionDocumentDetail>();
            if (productionDocuments != null)
            {
                _documentBinary = null; //Reset the document binary values
                productionDocuments = FillPlaceHolderFieldValues(productionDocuments); //Fill placeholder field values
                foreach (ProductionDocumentDetail productionDocumentDetail in productionDocuments)
                {
                    using (new EVTransactionScope(TransactionScopeOption.Suppress))
                    {
                        try
                        {
                            //The share path where the docs will be created in subfolders

                            //This path will only be used to store the document details like matterid, collectionid,doc ref id etc.Actual file will be in archivePath
                            var queryString = new StringBuilder();
                            string directoryPathGuid = Guid.NewGuid().ToString();
                            string fileNameWithoutExtension =
                                GetProductionFileName(productionDocumentDetail.StartingBatesNumber,
                                    productionDocumentDetail.DocumentProductionNumber,
                                    productionDocumentDetail.DCNNumber, productionDocumentDetail.IsDocumentExcluded);
                            string fileNameWithExtension;

                            //This would be as per below (it is based on this that DB will be checked if docs are created or not)
                            //PDF : 1 or noOfPages if 1 document per page
                            //Tiff: 1 or noOfPages if 1 document per page
                            //jpg or png: noOfPages 
                            switch (productionDocumentDetail.Profile.ImageType)
                            {
                                case ImageType.Tiff:
                                    fileNameWithExtension = fileNameWithoutExtension + Constants.Tiffextension;
                                    productionDocumentDetail.ExpectedDocumentCount =
                                        (productionDocumentDetail.Profile.IsOneImagePerPage
                                            ? productionDocumentDetail.NumberOfPages
                                            : 1);
                                    break;
                                default: //default is pdf
                                    fileNameWithExtension = fileNameWithoutExtension + Constants.Pdfextension;
                                    productionDocumentDetail.ExpectedDocumentCount =
                                        (productionDocumentDetail.Profile.IsOneImagePerPage
                                            ? productionDocumentDetail.NumberOfPages
                                            : 1);
                                    break;
                            }


                            if (String.IsNullOrEmpty(productionDocumentDetail.OriginalCollectionId))
                            {
                                _mOriginalCollectionIds.Add(productionDocumentDetail.OriginalCollectionId);
                            }
                            
                            string archiveBasePath = productionDocumentDetail.ArchivePath;
                            string baseSharePath = productionDocumentDetail.ExtractionLocation;

                            //Get values from the configuration file
                            string callbackUrl = CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.ProductionCallBackURL);

                            #region Form the source & destination path

                            string sourcePath = Path.Combine(archiveBasePath, directoryPathGuid, Constants.SourceKeyword);

                            var documentDetailBuilder = new StringBuilder();
                            documentDetailBuilder.Append(productionDocumentDetail.MatterId);
                            documentDetailBuilder.Append(Constants.Tilde);
                            documentDetailBuilder.Append(productionDocumentDetail.ProductionCollectionId);
                            documentDetailBuilder.Append(Constants.Tilde);
                            documentDetailBuilder.Append(productionDocumentDetail.DocumentId);
                            string documentDetails = documentDetailBuilder.ToString();

                            Directory.CreateDirectory(sourcePath);

                            sourcePath = sourcePath + Constants.Slash;
                            productionDocumentDetail.SourceDestinationPath = Path.Combine(archiveBasePath, directoryPathGuid); //Delete the extracted source file

                            #endregion

                            if (_documentBinary == null || !_documentBinary.Any())
                            {
                                List<string> documentIds = productionDocuments.Select(d => d.DocumentId).ToList();
                                GetMainIgcFile(sourcePath, productionDocumentDetail.MatterId,
                                    productionDocumentDetail.OriginalCollectionId, documentIds);
                            }
                            //Create source files and add query string for Source file, Markup File, Header footer config security file
                            string source = CreateSourceFiles(sourcePath, productionDocumentDetail,
                                productionDocumentDetail.DCNNumber);
                            queryString.Append(source);
                            //Target folder
                            if (IsDestinationPathValid(baseSharePath, fileNameWithExtension))
                            {
                                queryString.Append(Constants.QueryStringTargetPrefix);
                                queryString.Append(HttpUtility.UrlEncode(baseSharePath));
                                //destinationPath only stores the document details
                                //Notification URL
                                queryString.Append(Constants.QueryStringNotificationUrlPrefix);
                                queryString.Append(callbackUrl);

                                // Ningjun IGC 7.2 change default notification method to HTTP PUT. We need to set it to GET because our WCF service expect GET method.
                                queryString.Append(Constants.QueryStringNotificationVerb);

                                //Destination file name
                                queryString.Append(Constants.QueryStringDestinationFileName);
                                queryString.Append(fileNameWithExtension);

                                //Hearbeat file path-  Use to get the status of the document
                                //File name & Path are like pdf/tiff file, but the extension is .txt

                                productionDocumentDetail.HeartBeatFile = Path.Combine(baseSharePath, fileNameWithoutExtension + "_heartbeat" + Constants.TextFileExtension);
                                queryString.Append(Constants.QueryStringHeartBeatFileName);
                                queryString.Append(HttpUtility.UrlEncode(productionDocumentDetail.HeartBeatFile));

                                //This path is used by the production service to determine the matter collection document ids (all document details)
                                queryString.Append(Constants.QueryStringDocumentDetails);
                                queryString.Append(documentDetails);                                
                                productionDocumentDetail.QueryString = queryString.ToString();
                                productionDocumentDetail.SourceFile = source;
                                jobDetails.Add(productionDocumentDetail);
                                //If file name is DCN , Need to Delete same document produced by previous Procution Job(s).
                                //Extraction Location keep the latest produced document in a production set.
                                if (fileNameWithoutExtension.Equals(productionDocumentDetail.DCNNumber) &&
                                    productionDocumentDetail.IsVolumeContainExistingDocuments)
                                {
                                    DeleteExistingFilesInVolume(baseSharePath, fileNameWithoutExtension);
                                }
                                LogMessage(productionDocumentDetail, true,string.Format("Document with DCN:{0} is {1}", productionDocumentDetail.DCNNumber, Constants.ProductionPreSucess));
                            }
                            else
                            {
                                LogMessage(productionDocumentDetail, false, string.Format("Document with DCN:{0} and production file path: {1}\\{2}{3} {4}",productionDocumentDetail.DCNNumber, baseSharePath, fileNameWithExtension, Constants.FullStop, Constants.ErrorInValidFilePath));
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage(productionDocumentDetail, false,string.Format("Document with DCN:{0} is {1}-{2}", productionDocumentDetail.DCNNumber, Constants.ProductionPreFailure, ex.Message));
                            ex.AddUsrMsg(Constants.ProductionPreException, productionDocumentDetail.DCNNumber, ex);
                            ex.Trace().Swallow();
                            ReportToDirector(ex);
                        }
                    }
                }
                Send(jobDetails);
            }
        }

        /// <summary>
        /// Fills the place holder field values.
        /// </summary>
        /// <param name="productionDocuments">The production documents.</param>
        /// <returns>List of ProductionDocumentDetail</returns>
        private List<ProductionDocumentDetail> FillPlaceHolderFieldValues(List<ProductionDocumentDetail> productionDocuments)
        {
            try
            {
                var bootParameters = GetBootParameters();
                if (!productionDocuments.Any() || !bootParameters.Profile.IsInsertPlaceHolderPage) return productionDocuments;
                var fieldIdentifiers = GetPlaceHolderFieldIds(); //Get selected placeholder field Ids
                if (!fieldIdentifiers.Any()) return productionDocuments;
                var documentIdentifiers = GetDocumentIds(productionDocuments); //Get all document Ids for a give collection
                var documentFields = VaultDocumentSource.GetDocumentFields(bootParameters.MatterId,
                                                                                Guid.Parse(bootParameters.OriginalCollectionId),
                                                                                documentIdentifiers, fieldIdentifiers).ToList();
                var placeHolderFields = GetBootParameters().Profile.SelectedPlaceHolderFields;
                foreach (var doc in productionDocuments)
                {
                    doc.PlaceHolderFieldValues = new List<FieldIdentifierBEO>();
                    foreach (var field in placeHolderFields)//Loop through each selected placeholder field and set the value
                    {
                        var placeHolderField = new FieldIdentifierBEO {FieldId = field.FieldId, Name = field.Name, 
                            DataType = field.DataType, DataFormat = field.DataFormat};
                        doc.PlaceHolderFieldValues.Add(placeHolderField);
                        var documentField = documentFields.FirstOrDefault(
                            d => d.DocumentId == doc.DocumentId &&
                                 d.Id.ToString(CultureInfo.InvariantCulture) == field.FieldId);
                        if (documentField == null || string.IsNullOrWhiteSpace(documentField.Value)) continue;
                        placeHolderField.Value = documentField.Value.Length > 150
                            ? documentField.Value.Substring(0, 150)
                            : documentField.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(productionDocuments[0], false,
                    string.Format("{0} - {1}", Constants.ProductionPlaceholderFailure, ex.Message));
            }

            return productionDocuments;
        }

        /// <summary>
        /// Gets the document ids.
        /// </summary>
        /// <param name="documentCollection">The document collection.</param>
        /// <returns></returns>
        private static IEnumerable<DocumentIdentifierBEO> GetDocumentIds(IEnumerable<ProductionDocumentDetail> documentCollection)
        {
            return documentCollection.Select(doc => new DocumentIdentifierBEO {DocumentId = doc.DocumentId}).ToList();
        }

        /// <summary>
        /// Gets the place holder field ids.
        /// </summary>
        /// <returns></returns>
        private IList<FieldIdentifierBEO> GetPlaceHolderFieldIds()
        {
            var placeHolderFields = GetBootParameters().Profile.SelectedPlaceHolderFields;
            if (placeHolderFields.Any())
                placeHolderFields = placeHolderFields.FindAll(f => !string.IsNullOrWhiteSpace(f.FieldId) && f.FieldId != "0");
            return placeHolderFields.Select(field => new FieldIdentifierBEO {FieldId = field.FieldId, Name = field.Name}).ToList();
        }

        /// <summary>
        /// Gets the boot parameters.
        /// </summary>
        /// <returns></returns>
        private ProductionDetailsBEO GetBootParameters()
        {
            if (string.IsNullOrEmpty(BootParameters)) return null;
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(BootParameters);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(ProductionDetailsBEO));

            //Deserialization of bootparameter to get ProductionDetailsBEO
            return (ProductionDetailsBEO)xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// Validates if the destination path is valid or not
        /// </summary>
        /// <param name="baseSharePath">string</param>
        /// <param name="fileNameWithExtension">string</param>
        /// <returns>bool</returns>
        private static bool IsDestinationPathValid(string baseSharePath, string fileNameWithExtension)
        {
            if (!string.IsNullOrEmpty(baseSharePath) && !string.IsNullOrEmpty(fileNameWithExtension))
            {
                string destinationPath = Path.Combine(baseSharePath, fileNameWithExtension);
                if (!string.IsNullOrEmpty(destinationPath))
                {
                    return destinationPath.Length < Constants.FileMaxLimit;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Delete existing files in directory. 
        /// If file name is DCN , Need to Delete same document produced by previous Procution Job(s).
        /// </summary>
        private void DeleteExistingFilesInVolume(string volumePath, string fileName)
        {
            var fileList = Directory.GetFiles(volumePath, (fileName + "*"));
            foreach (var file in fileList)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    ex.AddDbgMsg("ProductionPreprocessWorker: DeleteExistingFilesInVolume - Failed to delete existing image(s) for job run id: {0}", PipelineId).Trace().Swallow();
                }
            }
        }

        /// <summary>
        /// Gives the configuration value for a key and sub application
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subapplication"></param>
        /// <returns></returns>
        private static string GetConfigurationValue(string key, string subapplication)
        {
            string result = ApplicationConfigurationManager.GetValue(key, subapplication);
            result = StringUtility.IsNullOrWhiteSpace(result) ? string.Empty : result;
            return result;
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        /// <param name="jobDetails">The job details.</param>
        private void Send(List<ProductionDocumentDetail> jobDetails)
        {
            var message = new PipeMessageEnvelope
            {
                Body = jobDetails
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(jobDetails.Count);
        }

        /// <summary>
        /// Method to create the source files - .xdl,.xrl,header footer config
        /// </summary>
        /// <param name="sourcePath">Path of the source folder.</param>
        /// <param name="jobParameters">Job Parameters.</param>
        /// <param name="dcnNumber">The DCN number.</param>
        /// <returns>
        /// Query string with source, markup and headerfooter config file
        /// </returns>
        private string CreateSourceFiles(string sourcePath, ProductionDocumentDetail jobParameters, string dcnNumber)
        {
            var queryString = new StringBuilder(Constants.QueryStringSourcePrefix);

            //.xdl file path is appended or html file path appended in case of place holder file
            if (jobParameters.IsDocumentExcluded)
            {
                queryString.Append(GetPlaceHolderFile(sourcePath, jobParameters, dcnNumber));
            }
            else if (_documentBinary != null && _documentBinary.Count > 0)
            {
                List<DocumentBinaryEntity> docBinaries =
                   _documentBinary.Where(f => f.DocumentReferenceId == jobParameters.OriginalDocumentReferenceId).ToList();
                foreach (DocumentBinaryEntity docBinary in docBinaries)
                {
                    //Write the file .xdl, .zdl, .idx to the source path
                    ByteArrayToFile(sourcePath + docBinary.BinaryReferenceId, docBinary.DocumentBinary());
                }
                queryString.Append(sourcePath + "document.xdl"); //Document.xdl is common for all binary document
            }
            else
            {
                Tracer.Error("Unable to get place holder or main igc file for job run id: {0}", PipelineId);
            }

            //Create the markup .xrl files on disk(source path)
            try
            {
                if (!jobParameters.IsDocumentExcluded) //if document is excluded then do not include the markup
                {
                    string markupFile = GetMarkUpFile(sourcePath, jobParameters);
                    if (!String.IsNullOrEmpty(markupFile))
                    {
                        queryString.Append(Constants.QueryStringMarkupFileNamePrefix);
                        queryString.Append(markupFile);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Unable to get markup files for job run id: {0}", PipelineId);
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }

            //Create the header footer config file on disk(source path)
            try
            {
                string headerFooterConfigFile = GetSecurityFile(sourcePath, jobParameters);
                if (!String.IsNullOrEmpty(headerFooterConfigFile))
                {
                    queryString.Append(Constants.QueryStringSecurityXmlFileNamePrefix);
                    queryString.Append(headerFooterConfigFile);
                }
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Unable to header footer files for job run id: {0}", PipelineId);
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }

            //fit within banners
            queryString.Append(Constants.QueryStringFitWithinBannersSetToTrue);
            return queryString.ToString();
        }

        /// <summary>
        /// Get the header footer config file for the document
        /// </summary>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="jobParameters">Job Parameters.</param>
        /// <returns>
        /// Path of the created header footer config xml file
        /// </returns>
        public string GetSecurityFile(string sourcePath, ProductionDocumentDetail jobParameters)
        {
            try
            {
                ProductionProfile profile = jobParameters.Profile;
                if (profile == null)
                {
                    return string.Empty;
                }
                string securityFile = CreateSecurityFile(sourcePath, jobParameters);
                return securityFile;
            }
            catch (Exception ex)
            {
                Tracer.Info("ProductionPreprocessWorker:GetSecurityFile() Sourcepath: {0}, Exception details: {1}", sourcePath, ex.ToDebugString());
                throw;
            }
        }

        /// <summary>
        /// Create the header footer config file for the document
        /// </summary>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="jobParameters">The job parameters.</param>
        /// <returns>
        /// Path of the created header footer config xml file
        /// </returns>
        private string CreateSecurityFile(string sourcePath, ProductionDocumentDetail jobParameters)
        {
            try
            {
                string topLeftString = string.Empty;
                string topCenterString = string.Empty;
                string topRightString = string.Empty;
                string bottomLeftString = string.Empty;
                string bottomCenterString = string.Empty;
                string bottomRightString = string.Empty;
                string fontColor = string.Empty;
                string fontString = string.Empty;
                string fontHeight = string.Empty;
                string fontStyle = string.Empty;
                string newSecurityFile = sourcePath + Guid.NewGuid();

                IGCSecurityDocument securityDocument = IGCSecurityDocumentFactory.GetIGCSecurityDocument();
                ProductionProfile profile = jobParameters.Profile;
                ProductionSetHeaderFooterFont font = profile.HeaderFooterFontSelection;
                ProductionSetHeaderFooter topLeft = profile.LeftHeader;
                ProductionSetHeaderFooter topCenter = profile.MiddleHeader;
                ProductionSetHeaderFooter topRight = profile.RightHeader;
                ProductionSetHeaderFooter bottomLeft = profile.LeftFooter;
                ProductionSetHeaderFooter bottomCenter = profile.MiddleFooter;
                ProductionSetHeaderFooter bottomRight = profile.RightFooter;

                if (font != null)
                {
                    fontColor = font.HeaderFooterColor ?? string.Empty;
                    fontString = font.HeaderFooterFont ?? string.Empty;
                    fontHeight = font.HeaderFooterFontSize;
                    fontStyle = ((int)font.HeaderFooterStyle).ToString(CultureInfo.InvariantCulture);
                }
                if (topLeft != null)
                    topLeftString = GetHeaderFooterString(topLeft, jobParameters);

                if (topCenter != null)
                    topCenterString = GetHeaderFooterString(topCenter, jobParameters);

                if (topRight != null)
                    topRightString = GetHeaderFooterString(topRight, jobParameters);

                if (bottomLeft != null)
                    bottomLeftString = GetHeaderFooterString(bottomLeft, jobParameters);

                if (bottomCenter != null)
                    bottomCenterString = GetHeaderFooterString(bottomCenter, jobParameters);

                if (bottomRight != null)
                    bottomRightString = GetHeaderFooterString(bottomRight, jobParameters);

                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).IsoBannerColor[0].@string = fontColor;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).IsoBannerFont[0].@string = fontString;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).IsoBannerFontHeight[0].@string = fontHeight;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).IsoBannerFontStyle[0].@string = fontStyle;

                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).TopLeft[0].@string = topLeftString;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).TopCenter[0].@string = topCenterString;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).TopRight[0].@string = topRightString;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).BottomLeft[0].@string = bottomLeftString;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).BottomCenter[0].@string = bottomCenterString;
                ((IGCSecurityDocumentIsoBanners)(securityDocument.Items[3])).BottomRight[0].@string = bottomRightString;

                SaveSecurityFile(newSecurityFile, securityDocument);

                return newSecurityFile;
            }
            catch (Exception ex)
            {
                Tracer.Error("Production Preprocess Worker: CreateSecurityFile() : {0}, Exception details: {1}", sourcePath, ex.ToDebugString());
                throw;
            }
        }

        /// <summary>
        /// Save the header footer config file for the document
        /// </summary>
        /// <param name="newSecurityFile">The new security file.</param>
        /// <param name="securityDocument">The security document.</param>
        private void SaveSecurityFile(string newSecurityFile, IGCSecurityDocument securityDocument)
        {
            try
            {
                var fs = new FileStream(newSecurityFile, FileMode.Create);
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.Write(SerializeObject(securityDocument).Replace(Constants.EncodingFormatUTF16, Constants.EncodingFormatUTF8));
                }
            }
            catch (Exception ex)
            {
                Tracer.Info("Production Preprocess Worker: SaveSecurityFile() : {0}, Exception details: {1}", newSecurityFile, ex.ToDebugString());
                throw;
            }
        }

        /// <summary>
        /// Serializes the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        private string SerializeObject(object obj)
        {
            try
            {
                var sb = new StringBuilder();
                var xws = new XmlWriterSettings
                                            {
                                                NewLineHandling = NewLineHandling.None
                                            };

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var xs = new XmlSerializer(obj.GetType());
                using (XmlWriter xwriter = XmlWriter.Create(sb, xws))
                {
                    xs.Serialize(xwriter, obj, ns);
                    xwriter.Flush();
                    return sb.ToString();

                }
            }
            catch (Exception ex)
            {
                Tracer.Error("Unable to serialize object. Exception details: {0}", ex.ToDebugString());
                throw;
            }
        }

        /// <summary>
        /// Get header footer string
        /// </summary>
        /// <param name="headerFooter">production header footer</param>
        /// <param name="jobParameters">job parameters</param>
        /// <returns>
        /// header footer string
        /// </returns>
        private string GetHeaderFooterString(ProductionSetHeaderFooter headerFooter, ProductionDocumentDetail jobParameters)
        {
            try
            {
                headerFooter.ShouldNotBe(null);
                ProductionProfile profile = jobParameters.Profile;
                switch (headerFooter.Option)
                {
                    case HeaderFooterOptions.Text:
                        if (!headerFooter.IsIncrementNeededInText)
                        {
                            return (headerFooter.TextPrefix == null
                                        ? string.Empty
                                        : InsertLineBreaks(CleanInvalidXmlChars(headerFooter.TextPrefix)) +
                                   (headerFooter.TextStartingNumber ?? string.Empty));
                        }
                        return string.Format(Constants.BatesFormat, (headerFooter.TextPrefix == null ? string.Empty : InsertLineBreaks(CleanInvalidXmlChars(headerFooter.TextPrefix))),
                                             (headerFooter.TextStartingNumber == null ? string.Empty : GetStartingNumber(jobParameters.StartBatesRunningNumber, headerFooter.TextStartingNumber)));
                    case HeaderFooterOptions.DateAndTime:
                        return Constants.DateTimeFormat;
                    case HeaderFooterOptions.Date:
                        return Constants.DateFormat;
                    case HeaderFooterOptions.Time:
                        return Constants.TimeFormat;
                    case HeaderFooterOptions.DocumentProductionNumber:
                        return jobParameters.DocumentProductionNumber;
                    case HeaderFooterOptions.PageNumber:
                        return Constants.PageNumberFormat;
                    case HeaderFooterOptions.ProductionNumber:
                        return string.Format(Constants.BatesFormat,
                                           (profile.ProductionPrefix ?? string.Empty),
                                           (profile.ProductionStartingNumber == null
                                                ? string.Empty
                                                : GetStartingNumber(jobParameters.StartBatesRunningNumber,
                                                                    profile.ProductionStartingNumber)));
                    case HeaderFooterOptions.DatasetField:
                        RVWDocumentBEO document = DocumentBO.GetDocumentDataViewFromVault(jobParameters.MatterId, jobParameters.DatasetCollectionId, jobParameters.OriginalDocumentReferenceId, jobParameters.CreatedBy, true);
                        if (document.FieldList != null && document.FieldList.Count > 0)
                        {
                            RVWDocumentFieldBEO field = document.FieldList.FirstOrDefault(x => x.FieldId == headerFooter.DatasetFieldSelected);
                            if (field != null)
                            {
                                //If user configures bates and dpn fields while scheduling the production job 
                                //The field values for the bats and dpn wont exists since  we create bates and dpn fields on the creation of production set
                                //and we can update the fields values of those fields only when document binary create and it is done in production vault and velocty worker
                                //so using production document details which will have dpn and bates field values
                                if (_lstBatesAndDpnFieldTypes.Contains(field.FieldType.DataTypeId))
                                {
                                    if (string.IsNullOrEmpty(field.FieldValue) || jobParameters.Profile.ProfileId > 0)
                                    {
                                        GetFieldVauleForDpnAndBates(jobParameters, field);
                                    }
                                }
                            }
                            if (field != null && !string.IsNullOrEmpty(field.FieldValue))
                            {
                                return CleanInvalidXmlChars(field.FieldValue);
                            }
                        }
                        break;
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Tracer.Info("Production Preprocess Worker: GetHeaderFooterString() Exception details: {0}", ex);
                throw;
            }
            return string.Empty;
        }

        /// <summary>
        /// Get Field Vaule For DpnAndBates
        /// </summary>
        /// <param name="productionDocumentDetail">production document detail</param>
        /// <param name="fieldSelected">field Selected </param>
        /// <returns></returns>
        private void GetFieldVauleForDpnAndBates(ProductionDocumentDetail productionDocumentDetail, RVWDocumentFieldBEO fieldSelected)
        {
            switch (fieldSelected.FieldType.DataTypeId)
            {
                case (Int32)Constants.FieldTypeIds.DPN:
                    fieldSelected.FieldValue = productionDocumentDetail.DocumentProductionNumber;
                    break;
                case (Int32)Constants.FieldTypeIds.BatesBegin:
                    fieldSelected.FieldValue = productionDocumentDetail.StartingBatesNumber;
                    break;
                case (Int32)Constants.FieldTypeIds.BatesEnd:
                    fieldSelected.FieldValue = productionDocumentDetail.EndingBatesNumber;
                    break;
                case (Int32)Constants.FieldTypeIds.BatesRange:
                    var batesRange = new StringBuilder(productionDocumentDetail.StartingBatesNumber);
                    batesRange.Append(Constants.UnderScore);
                    batesRange.Append(productionDocumentDetail.EndingBatesNumber);
                    fieldSelected.FieldValue = batesRange.ToString();
                    break;
                default:
                    fieldSelected.FieldValue = fieldSelected.FieldValue;
                    break;
            }
        }
        /// <summary>
        /// Inserts line breaks after every 'n' characters to display header/footer text with overwriting on each other.
        /// This is the work around implemented and has to remain till IGC gets back with appropriate fix
        /// </summary>
        /// <param name="headerFooter"></param>
        /// <returns></returns>
        private string InsertLineBreaks(string headerFooter)
        {
            if (headerFooter.Equals(Constants.EmptyBatesFormat))
            {
                return headerFooter;
            }
            //get number of characters to display in header/footer text produced image
            int charsPerLine = int.Parse((GetConfigurationValue("HeaderFooterCharactersPerLine", ConfigKeyNearNativeViewer).Equals(string.Empty) ? "0" : GetConfigurationValue("HeaderFooterCharactersPerLine", ConfigKeyNearNativeViewer)));

            //get total number of characters allowed to display in header/footer text produced image
            int totalChars = int.Parse((GetConfigurationValue("HeaderFooterTotalCharacters", ConfigKeyNearNativeViewer).Equals(string.Empty) ? headerFooter.Length.ToString(CultureInfo.InvariantCulture) : GetConfigurationValue("HeaderFooterTotalCharacters", ConfigKeyNearNativeViewer)));

            //if total characters in the text is not more than configuration value, read the complete text in char array
            char[] chars = headerFooter.Length > totalChars ? headerFooter.Substring(0, totalChars).ToCharArray() : headerFooter.ToCharArray();

            int lastLinebreak = 0;
            bool wantLinebreak = false;
            var sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (wantLinebreak)
                {
                    sb.Append("<br/>");
                    lastLinebreak = i; wantLinebreak = false;
                }
                sb.Append(chars[i]);
                if (i - lastLinebreak + 1 == charsPerLine)
                {
                    wantLinebreak = true;
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Get the starting number.
        /// </summary>
        /// <param name="runningNumber">Running number.</param>
        /// <param name="intialStart">Initial start number</param>
        private string GetStartingNumber(long runningNumber, string intialStart)
        {
            long intialStartNumber;
            string format = string.Empty;
            for (int i = 1; i <= intialStart.Length; i++)
            {
                format = format + "0";
            }
            //Check if initial start is numeric - if not return
            if (!long.TryParse(intialStart, out intialStartNumber))
            {
                return string.Empty;
            }
            long newNumber = runningNumber + intialStartNumber;
            //Check if length of the new number is greater than initial start length
            return newNumber.ToString(format);
        }

        /// <summary>
        /// This removes characters that are invalid for xml encoding
        /// http://www.w3.org/TR/xml11/#charsets 
        /// http://www.w3.org/TR/REC-xml/#charsets
        /// http://www.unicode.org/Public/UNIDATA/UCD.html
        /// http://www.theplancollection.com/house-plan-related-articles/hexadecimal-value-invalid-character
        /// </summary>
        /// <param name="text">Text to be encoded.</param>
        /// <returns>Text with invalid xml characters removed.</returns>
        private string CleanInvalidXmlChars(string text)
        {
            //Removing the spl character from the text
            const string pattern = "[/,\\,(,),~,!,@,#,$,%,^,&,*,<,>,?]";
            return Regex.Replace(text, pattern, string.Empty);
        }

        /// <summary>
        /// Create the markup file for document
        /// </summary>
        /// <param name="sourcePath">Source Path</param>
        /// <param name="jobBusinessEntity">The job business entity.</param>
        /// <returns>
        /// The markup file path
        /// </returns>
        public string GetMarkUpFile(string sourcePath, ProductionDocumentDetail jobBusinessEntity)
        {
            try
            {
                //Check if markup is needed
                ProductionProfile profile = jobBusinessEntity.Profile;
                if (profile == null || !profile.IsBurnMarkups)
                {
                    return string.Empty;
                }
                string markupFile = string.Empty;
                var documentMetaDataBusinessEntity = new DocumentMetaDataBEO
                {
                    CollectionId = jobBusinessEntity.OriginalCollectionId,
                    DocumentReferenceId = jobBusinessEntity.OriginalDocumentReferenceId,
                    MatterId = jobBusinessEntity.MatterId
                };

                RVWMarkupBEO redActionBusinessEntity = DocumentBO.GetRedactionXml(jobBusinessEntity.MatterId.Trim(), documentMetaDataBusinessEntity.CollectionId, documentMetaDataBusinessEntity.DocumentReferenceId);

                //If markup file exists write to disk
                if (redActionBusinessEntity != null && !string.IsNullOrEmpty(redActionBusinessEntity.MarkupXml))
                {
                    //Add the version string
                    string markupXmlText = Constants.xmlVersionString.Replace(Constants.Slash, string.Empty) + redActionBusinessEntity.MarkupXml;
                    //Apply user selections like to include or exclude markups
                    XmlDocument markupXml = ApplyUserSelections(markupXmlText, profile);
                    markupFile = sourcePath + Guid.NewGuid();
                    markupXml.Save(markupFile);
                }
                return markupFile;
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("ProductionPreprocessWorker:GetMarkUpFile() Sourcepath:{0}", sourcePath);
                throw;
            }
        }

        /// <summary>
        /// Removes or adds the nodes for the markups based on user selection
        /// </summary>
        /// <param name="xmlString">xml string</param>
        /// <param name="profile">The profile.</param>
        /// <returns></returns>
        private XmlDocument ApplyUserSelections(string xmlString, ProductionProfile profile)
        {
            //using XmlDocument so that underlying XML data can be edited
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(xmlString);

                XPathNavigator nav = doc.CreateNavigator();

                //All notes need to be removed from markups
                //#defect#87222
                DeleteMarkupNodes(nav, Constants.XpathNotes);

                //Based on user selection markups removed
                if (!profile.IsIncludeArrowsMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathArrow);

                if (!profile.IsIncludeBoxesMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathBoxes);

                if (!profile.IsIncludeHighlightsMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathHighlights);

                if (!profile.IsIncludeLinesMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathLines);

                if (!profile.IsIncludeTextBoxMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathTextBox);

                if (!profile.IsIncludeRubberStampMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathRubberStamp);

                if (!profile.IsIncludeRedactionsMarkup)
                    DeleteMarkupNodes(nav, Constants.XpathRedactions);

                //Execute below code when include available reasons with markups in unchecked
                if (!profile.IsIncludeReasonsWithMarkup)
                {
                    SetAttribute(nav, Constants.XpathComment, Constants.Blank);
                }
            }
            catch (Exception exception)
            {
                Tracer.Info("Production Preprocess Worker:ApplyUserSelections() {0}, Exception details: {1}", xmlString, exception);
            }

            return doc;
        }

        /// <summary>
        /// Sets the attribute at the xpath with specified value
        /// </summary>
        /// <param name="nav">The nav.</param>
        /// <param name="xpath">xpath</param>
        /// <param name="newValue">new value</param>
        private void SetAttribute(XPathNavigator nav, string xpath, string newValue)
        {
            XmlNamespaceManager xmlNamespaceManger = new XmlNamespaceManager(nav.NameTable);
            XPathExpression xpathExpression;

            xmlNamespaceManger.AddNamespace(Constants.XmlNamespacePrefix, Constants.XmlNamespace);
            xpathExpression = nav.Compile(xpath);

            xpathExpression.SetContext(xmlNamespaceManger);

            XPathNodeIterator nodeIterator = nav.Select(xpathExpression);

            foreach (XPathNavigator curNav in nodeIterator)
            {
                curNav.SetValue(newValue);
            }
        }

        /// <summary>
        /// Deleted the specified nodes in XPath
        /// </summary>
        /// <param name="nav">The nav.</param>
        /// <param name="xpath">xpath</param>
        private void DeleteMarkupNodes(XPathNavigator nav, string xpath)
        {
            var xmlNamespaceManger = new XmlNamespaceManager(nav.NameTable);
            XPathExpression xpathExpression;

            //SET THE XMLNS WITH XPATH EXPRESSION
            xmlNamespaceManger.AddNamespace(Constants.XmlNamespacePrefix, Constants.XmlNamespace);
            xpathExpression = nav.Compile(xpath);
            xpathExpression.SetContext(xmlNamespaceManger);

            XPathNodeIterator nodeIterator = nav.Select(xpathExpression);

            //Delete the nodes
            while (nodeIterator.MoveNext())
            {
                nodeIterator.Current.DeleteSelf();
                nodeIterator = nav.Select(xpathExpression);
            }
        }

        /// <summary>
        /// Create the IGC files
        /// </summary>
        /// <param name="sourcePath">Source path</param>
        /// <param name="matterId">Matter identifier</param>
        /// <param name="collectionId">Collection identifier</param>
        /// <param name="documentIds"></param>
        /// <returns>The path of the .xdl file created</returns>  
        public void GetMainIgcFile(string sourcePath, string matterId, string collectionId, List<string> documentIds)
        {
            try
            {
                //Get all the brava file names for the document in a list (.xdl, .zdl..)
                _documentBinary = DocumentBO.GetBulkBinaryDocumentFromVault(matterId, collectionId, documentIds, Constants.BravaBinaryTypeId);
            }
            catch (Exception exception)
            {
                Tracer.Info("ProductionPreprocessWorker:GetPlaceHolderFile() SourcePath:{0}, Exception details:{1}", sourcePath, exception);
                throw;
            }
        }

        /// <summary>
        /// Function to save byte array to a file
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="byteArray">The byte array.</param>
        private void ByteArrayToFile(string fileName, byte[] byteArray)
        {
            // Open file for reading  
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                // Writes a block of bytes to this stream using data from a byte array.  
                fileStream.Write(byteArray, 0, byteArray.Length);
            }
        }

        /// <summary>
        /// Create the Place holder files in html format files
        /// </summary>
        /// <param name="sourcePath">Source path</param>
        /// <param name="productionDocument">The production document.</param>
        /// <param name="dcnNumber">The DCN number.</param>
        /// <returns>
        /// The path of the .xdl file created
        /// </returns>
        public string GetPlaceHolderFile(string sourcePath, ProductionDocumentDetail productionDocument, string dcnNumber)
        {
            try
            {
                var filePath = string.Format("{0}{1}.html", sourcePath, Guid.NewGuid());
                var bootParams = GetBootParameters(); 
                var placeholderHtml = ProductionBO.GetPlaceHolderHtml(bootParams.Profile.PlaceHolderText,
                    productionDocument.PlaceHolderFieldValues, productionDocument.DocumentProductionNumber,
                    productionDocument.AllBates, productionDocument.StartingBatesNumber,
                    productionDocument.EndingBatesNumber);
                File.WriteAllText(filePath, placeholderHtml, Encoding.UTF8);
                return filePath;
            }
            catch (Exception exception)
            {
                Tracer.Info("ProductionPreprocessWorker:GetPlaceHolderFile() SourcePath:{0}, DCN:{1}, Exception details:{2}",
                    sourcePath, dcnNumber, exception);
                throw;
            }
        }

        /// <summary>
        /// Get the production file name
        /// </summary>
        /// <param name="startingBatesNumber"></param>
        /// <param name="dpn"></param>
        /// <param name="dcn"></param>
        /// <param name="isExclude"></param>
        /// <returns></returns>
        private string GetProductionFileName(string startingBatesNumber, string dpn, string dcn, bool isExclude)
        {
            if (isExclude) //File name should be a DCN for the excluded document 
            {
                return !string.IsNullOrEmpty(startingBatesNumber) ? startingBatesNumber : dcn;
            }
            if (!string.IsNullOrEmpty(startingBatesNumber))
            {
                return startingBatesNumber;
            }
            return !string.IsNullOrEmpty(dpn) ? dpn : dcn;
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        private void LogMessage(ProductionDocumentDetail documentDetail, bool success, string message)
        {
            try
            {
                var log = new List<JobWorkerLog<ProductionParserLogInfo>>();
                var parserLog = new JobWorkerLog<ProductionParserLogInfo>
                                    {
                                        JobRunId =
                                            (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                                        CorrelationId = documentDetail.CorrelationId,
                                        WorkerRoleType = Constants.ProductionPreProcessRoleId,
                                        WorkerInstanceId = WorkerId,
                                        IsMessage = false,
                                        Success = success, //!string.IsNullOrEmpty(documentDetail.DocumentProductionNumber) && success,
                                        CreatedBy = documentDetail.CreatedBy,
                                        LogInfo =
                                            new ProductionParserLogInfo
                                                {
                                                    Information = message, //string.IsNullOrEmpty(documentDetail.DocumentProductionNumber) ? "Unable to produce this document" : message,
                                                    BatesNumber = documentDetail.AllBates,
                                                    DatasetName = documentDetail.OriginalDatasetName,
                                                    DCN = documentDetail.DCNNumber,
                                                    ProductionDocumentNumber = documentDetail.DocumentProductionNumber,
                                                    ProductionName = documentDetail.Profile.ProfileName
                                                }
                                    };
                // TaskId
                log.Add(parserLog);
                SendLog(log);
            }
            catch (Exception exception)
            {
                Tracer.Error("Production Preprocess Worker: Unable to log message. Exception details: {0}", exception);
                throw;
            }
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<ProductionParserLogInfo>> log)
        {
            LogPipe.Open();
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        protected override void EndWork()
        {
            base.EndWork();
            //dispose the list that contains bates and dpn fieldtypes
            _lstBatesAndDpnFieldTypes = null;
        }
    }
}
