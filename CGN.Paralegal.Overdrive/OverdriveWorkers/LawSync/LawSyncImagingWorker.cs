using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.Business.Document;
using System.Xml;
using System.Web;
using LexisNexis.Evolution.Infrastructure.ServerManagement;
using System.Net;
using LexisNexis.Evolution.Infrastructure.EVContainer;


namespace LexisNexis.Evolution.Worker
{
    public class LawSyncImagingWorker : WorkerBase
    {
        private LawSyncBEO _jobParameter;
        private string _redactableSetCollectionId;
        private List<DocumentBinaryEntity> _documentsBinaryList;
        private string _datasetExtractionPath;
        private string _redactitPushUrl;
        private string _redactitTimeout;
        private List<JobWorkerLog<LawSyncLogInfo>> _logInfoList;
        private List<DocumentConversionLogBeo> _documentProcessStateList;
        private string _datasetCollectionId;
        private long _lawSyncJobId;
        private IDocumentVaultManager _vaultManager;
        private string _redactitCallbackUrl;

        protected override void BeginWork()
        {
            base.BeginWork();
            var hostId = ServerConnectivity.GetHostIPAddress();
            _redactitPushUrl = CmgServiceConfigBO.GetServiceConfigurationsforConfig
                (hostId, External.DataAccess.Constants.SystemConfigurationService, Constants.QueueServerUrl);
            _redactitTimeout = ApplicationConfigurationManager.GetValue(Constants.RedactItTimeout, Constants.NearNativeViewerSection);
            var baseServiceUri = new Uri(CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.WcfHostUrl));
            var lawServiceUri = new Uri(baseServiceUri, Constants.LawSyncConversionCallBackMethod);
            _redactitCallbackUrl = lawServiceUri.OriginalString;
            _vaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                if (message.Body == null)
                {
                    return;
                }

                var lawDocumentsList = (LawSyncDocumentCollection) message.Body;

                if (_jobParameter == null)
                {
                    if (lawDocumentsList.IsLawSyncReprocessJob)
                    {
                        _jobParameter = lawDocumentsList.OrginalJobParameter;
                    }
                    else
                    {
                        _jobParameter = (LawSyncBEO) XmlUtility.DeserializeObject(BootParameters, typeof (LawSyncBEO));
                    }
                }

                _redactableSetCollectionId = lawDocumentsList.RedactableSetCollectionId;
                _datasetExtractionPath = lawDocumentsList.DatasetExtractionPath;
                _datasetCollectionId = lawDocumentsList.DatasetCollectionId;
                _lawSyncJobId = lawDocumentsList.LawSynJobId;
                _logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
                _documentProcessStateList = new List<DocumentConversionLogBeo>();
                var lawImagingDocuments = lawDocumentsList.Documents.Where(d => d.IsImaging).ToList();

                if (_jobParameter.IsProduceImage && lawImagingDocuments.Any())
                {
                    var documentIds = lawImagingDocuments.Select(d => d.DocumentReferenceId).ToList();
                    _documentsBinaryList = GetNearNativeFileForDocuments(_jobParameter.MatterId,
                        _redactableSetCollectionId,
                        documentIds);
                    foreach (var document in lawImagingDocuments)
                    {
                        SendDocumentForImaging(document);
                    }
                }
                if (_documentProcessStateList.Any())
                {
                    UpdateDcoumentProcessState(_documentProcessStateList);
                }
                Send(lawDocumentsList);
                if (_logInfoList != null && _logInfoList.Any())
                {
                    SendLogPipe(_logInfoList);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
                LogMessage(Constants.LawSyncFailureinImagingMessage + ex.ToUserString());
            }
        }

        private void SendDocumentForImaging(LawSyncDocumentDetail document)
        {
            int conversionStatus = (int)LawSyncProcessState.InProgress;
            try
            {

                var queryString = new StringBuilder();
                var sourceExtractionPath = Path.Combine(_datasetExtractionPath, Guid.NewGuid().ToString(),
                    Constants.SourceKeyword);
                Directory.CreateDirectory(sourceExtractionPath);

                document.DocumentExtractionPath = sourceExtractionPath; //Need to be deleted.

                //1)Set source file(xdl,zdl) & Markup xml
                var source = CreateSourceFiles(sourceExtractionPath, document.DocumentReferenceId);
                if (string.IsNullOrEmpty(source))
                {
                    document.IsImagesXdlAvailable = false;
                    _documentProcessStateList.Add(GetDocumentProcessStateInformation(document, (int)LawSyncProcessState.Failed));
                    ConstructLog(document.LawDocumentId, document.CorrelationId, document.DocumentControlNumber, Constants.LawSyncMissingImageMessage);
                    return;
                }
                document.IsImagesXdlAvailable = true;
                queryString.Append(source);

                //2)Set Image Folder
                queryString.Append(Constants.QueryStringTargetPrefix);
                queryString.Append(HttpUtility.UrlEncode(document.ImagesFolderPath));//Target folder for redact it to generate pdf

                
                //3)Set Produced Image File name
                var fileName = document.DocumentControlNumber + Constants.Tifextension;
                queryString.Append(Constants.QueryStringDestinationFileName);
                queryString.Append(fileName);

                //4)Set Hearbeat file path-  Use to get the status of the document. The extension is .txt
                var heartBeatFile = Path.Combine(document.ImagesFolderPath,
                    string.Format("{0}{1}{2}",document.DocumentControlNumber,Constants.HeartbeatFileName,Constants.TextFileExtension));
                queryString.Append(Constants.QueryStringHeartBeatFileName);
                queryString.Append(HttpUtility.UrlEncode(heartBeatFile));
                document.RedactItHeartBeatFilePath = heartBeatFile;

                //5)Set Image Format(.Tiff)
                queryString.Append(Constants.QueryStringOutputFormatPrefix);
                queryString.Append(Constants.TifKeyword);

                //6)Set Image Color settings
                if (_jobParameter.TiffImageColor == TiffImageColors.One) //tiff monochrome 
                {
                    queryString.Append(Constants.QueryStringTiffMonochromePrefix);
                    queryString.Append(Constants.TrueString);
                    queryString.Append(Constants.QueryStringTifDPI);
                }
                else //tiff colour
                {
                    var tiffBpp = ((int) _jobParameter.TiffImageColor).ToString(CultureInfo.InvariantCulture);
                    queryString.Append(Constants.QueryStringTiffBppPrefix);
                    queryString.Append(tiffBpp);
                    queryString.Append(Constants.QueryStringTifDPI);
                }

                //7)Set Redact-It Timeout
                queryString.Append(Constants.StepTimeout);
                queryString.Append(_redactitTimeout);

                //8)Set One images per page
                queryString.Append(Constants.QueryStringOneFilePerPagePrefix);
                queryString.Append(Constants.TrueString);

                
               //9)Set Redact It Job priority
                queryString.Append(Constants.QueryStringPriority);
               queryString.Append(((int)_jobParameter.ConversionPriority));

                //10 Redact-It callback Url
               queryString.Append(Constants.QueryStringNotificationUrlPrefix);
               queryString.Append(_redactitCallbackUrl);

               queryString.Append(Constants.PublishBlankPagesQueryString);
               queryString.Append(CmgServiceConfigBO.GetServiceConfigurationsforConfig(Constants.PublishBlankPages));

                PushDocumentToQueueServer(queryString, document.LawDocumentId, heartBeatFile);
            }
            catch (Exception ex)
            {
                ConstructLog(document.LawDocumentId, document.CorrelationId,document.DocumentControlNumber ,Constants.LawSyncImageSendFailureMessage);
                ex.AddDbgMsg("Law Document Id:{0}", document.LawDocumentId);
                ex.Trace().Swallow();
                ReportToDirector(ex);
                conversionStatus = (int)LawSyncProcessState.Failed;
            }

            _documentProcessStateList.Add(GetDocumentProcessStateInformation(document, conversionStatus));
        }

        private DocumentConversionLogBeo GetDocumentProcessStateInformation(LawSyncDocumentDetail lawDocument, int status)
        {
            var documentProcessState = new DocumentConversionLogBeo
                                       {
                JobRunId = _lawSyncJobId,
                ProcessJobId = WorkAssignment.JobId,

                DocumentId = lawDocument.DocumentReferenceId,
                CollectionId = _datasetCollectionId,
                ConversionStatus = status,
                Status = EVRedactItErrorCodes.Failed, //default state

                ModifiedDate = DateTime.UtcNow
            };

            //Category Reason
            if (documentProcessState.ConversionStatus == (int)LawSyncProcessState.Failed)
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.ImageConversionFailure;
            }
            else
            {
                documentProcessState.ReasonId = (int)Constants.LawSynProcessStateErrorCodes.Successful;
            }
            return documentProcessState;
        }

        private void UpdateDcoumentProcessState(List<DocumentConversionLogBeo> documentConversionLogBeos)
        {
            try
            {
                //Need to Modify
                _vaultManager.AddOrUpdateConversionLogs(Convert.ToInt64(_jobParameter.MatterId),
                                                            documentConversionLogBeos, true);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }
        
        /// <summary>
        /// Send document to queue server
        /// </summary>
        private void PushDocumentToQueueServer(StringBuilder queryString, int lawDocumentId, string heartBeatFile)
        {
            var request = WebRequest.Create(_redactitPushUrl);
            byte[] byteArray = Encoding.UTF8.GetBytes(queryString.ToString());
            request.Method = Constants.HttpPostMethod;
            request.ContentType = Constants.HttpUlrEncoded;
            request.ContentLength = byteArray.Length;
            using (var dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
                using (var response = request.GetResponse())
                {
                    var status = ((HttpWebResponse) response).StatusDescription.ToUpper().Trim();
                    if (!status.Equals(Constants.OkKeyword))
                    {
                        Tracer.Warning("Error occured in send document to Queue for DCN:{0}, HeartBeatFile:{1}", lawDocumentId, heartBeatFile);
                    }
                }
            }
        }

        public void ConstructLog(int lawDocumentId, int documentCorrelationId, string documentControlNumber, string message)
        {
            var sbErrorMessage=new StringBuilder();
            sbErrorMessage.Append(message);
            sbErrorMessage.Append(Constants.MessageDCN);
            sbErrorMessage.Append(documentControlNumber);
            sbErrorMessage.Append(Constants.MessageLawDocumentId);
            sbErrorMessage.Append(lawDocumentId);
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = Convert.ToInt64(PipelineId),
                CorrelationId = documentCorrelationId,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncImagingWorkerRoleType,
                ErrorCode = (int)LawSyncErrorCode.ImageConversionFailure,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new LawSyncLogInfo
                {
                    LawDocumentId = lawDocumentId,
                    DocumentControlNumber = documentControlNumber,
                    Information = sbErrorMessage.ToString(),
                    IsFailureInImageConversion = true
                }
            };
            _logInfoList.Add(lawSyncLog);
        }

        public void LogMessage(string message)
        {
            var logInfoList = new List<JobWorkerLog<LawSyncLogInfo>>();
            var lawSyncLog = new JobWorkerLog<LawSyncLogInfo>
            {
                JobRunId = (!string.IsNullOrEmpty(PipelineId)) ? Convert.ToInt64(PipelineId) : 0,
                CorrelationId = 0,
                WorkerInstanceId = WorkerId,
                WorkerRoleType = Constants.LawSyncImagingWorkerRoleType,
                Success = false,
                CreatedBy = _jobParameter.CreatedBy,
                IsMessage = false,
                LogInfo = new LawSyncLogInfo
                {
                    Information = message,
                    IsFailureInImageConversion = true
                }
            };
            logInfoList.Add(lawSyncLog);
            SendLogPipe(logInfoList);
        }


        public List<DocumentBinaryEntity> GetNearNativeFileForDocuments(long matterId, string collectionId, List<string> documentIds)
        {

            //Get all the brava file names for the document in a list (.xdl, .zdl..)
            return DocumentBO.GetBulkBinaryDocumentFromVault(matterId.ToString(CultureInfo.InvariantCulture), collectionId, documentIds,
                Constants.BravaBinaryTypeId);

        }

        private string CreateSourceFiles(string sourcePath, string documentId)
        {
            var queryString = new StringBuilder(Constants.QueryStringSourcePrefix);


            if (_documentsBinaryList != null &&  _documentsBinaryList.Any())
            {
                var docBinaries = _documentsBinaryList.Where(f => f.DocumentReferenceId == documentId).ToList();
                if (docBinaries.Any())
                {
                    foreach (var docBinary in docBinaries)
                    {
                        var fileName = Path.Combine(sourcePath, docBinary.BinaryReferenceId);
                        //Write the file .xdl, .zdl, .idx to the source path
                        CreateFileFromBinary(fileName, docBinary.DocumentBinary());
                    }
                    queryString.Append(Path.Combine(sourcePath, Constants.DocumentXdl)); //Document.xdl is common for all binary document
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }

            //Set Markup file
            if (!_jobParameter.IsBurnMarkups) return queryString.ToString();

            //Create the markup .xrl files on disk(source path)
            string markupFile = GetMarkUpFile(sourcePath, documentId);
            if (!String.IsNullOrEmpty(markupFile))
            {
                queryString.Append(Constants.QueryStringMarkupFileNamePrefix);
                queryString.Append(markupFile);
            }

            return queryString.ToString();
        }


        /// <summary>  
        /// Function to save byte array to a file  
        /// </summary>  
        private void CreateFileFromBinary(string fileName, byte[] byteArray)
        {
            // Open file for reading  
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                // Writes a block of bytes to this stream using data from a byte array.  
                fileStream.Write(byteArray, 0, byteArray.Length);
            }
        }


        /// <summary>
        /// Create the markup file for document
        /// </summary>
        public string GetMarkUpFile(string sourcePath, string documentId)
        {
            var markupFile = string.Empty;
            var markupObject = DocumentBO.GetRedactionXml(
                _jobParameter.MatterId.ToString(CultureInfo.InvariantCulture), _redactableSetCollectionId, documentId);

            //If markup file exists write to disk
            if (markupObject != null && !string.IsNullOrEmpty(markupObject.MarkupXml))
            {
                //Add the version string
                var markupXmlText = Constants.xmlVersionString.Replace(Constants.Slash, string.Empty) +
                                    markupObject.MarkupXml;
                //Apply user selections like to include or exclude markups
                var markupXml = ApplyUserMarkupSelections(markupXmlText);
                 markupFile = Path.Combine(sourcePath, Guid.NewGuid().ToString());
                markupXml.Save(markupFile);
            }
            return markupFile;
        }

        /// <summary>
        /// Removes or adds the nodes for the markups based on user selection
        /// </summary>
        /// <param name="markupText">xml string </param>
        private XmlDocument ApplyUserMarkupSelections(string markupText)
        {
            //using XmlDocument so that underlying XML data can be edited
            var markupDocument = new XmlDocument();
            markupDocument.LoadXml(markupText);
            var pathNavigator = markupDocument.CreateNavigator();

            //All notes need to be removed from markups
            RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathNotes);

            //Based on user selection markups removed
            if (!_jobParameter.IsIncludeArrowsMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathArrow);
            }

            if (!_jobParameter.IsIncludeBoxesMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathBoxes);
            }

            if (!_jobParameter.IsIncludeHighlightsMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathHighlights);
            }

            if (!_jobParameter.IsIncludeLinesMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathLines);
            }

            if (!_jobParameter.IsIncludeTextBoxesMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathTextBox);
            }

            if (!_jobParameter.IsIncludeRubberStampsMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathRubberStamp);
            }

            if (!_jobParameter.IsIncludeRedactionsMarkup)
            {
                RemoveMarkupNodesFromMarkupDocument(pathNavigator, Constants.XpathRedactions);
            }

            //Execute below code when include available reasons with markups in unchecked
            if (!_jobParameter.IsIncludeReasonsWithsMarkup)
            {
                SetAttributeInMarkupDocument(pathNavigator, Constants.XpathComment, Constants.Blank);
            }
            return markupDocument;
        }

        /// <summary>
        /// Sets the attribute at the xpath with specified value
        /// </summary>
        private void SetAttributeInMarkupDocument(XPathNavigator navigator, string xpath, string newValue)
        {
            if (navigator.NameTable == null) return;
            var xmlNamespaceManger = new XmlNamespaceManager(navigator.NameTable);
            xmlNamespaceManger.AddNamespace(Constants.XmlNamespacePrefix, Constants.XmlNamespace);
            var xpathExpression = navigator.Compile(xpath);
            xpathExpression.SetContext(xmlNamespaceManger);
            var nodeIterator = navigator.Select(xpathExpression);
            foreach (XPathNavigator curNav in nodeIterator)
            {
                curNav.SetValue(newValue);
            }
        }

        /// <summary>
        /// Deleted the specified nodes in XPath
        /// </summary>
        private void RemoveMarkupNodesFromMarkupDocument(XPathNavigator navigator, string xpath)
        {
            if (navigator.NameTable == null) return;
            var xmlNamespaceManger = new XmlNamespaceManager(navigator.NameTable);

            //SET THE XMLNS WITH XPATH EXPRESSION
            xmlNamespaceManger.AddNamespace(Constants.XmlNamespacePrefix, Constants.XmlNamespace);
            var xpathExpression = navigator.Compile(xpath);

            xpathExpression.SetContext(xmlNamespaceManger);

            var nodeIterator = navigator.Select(xpathExpression);
            //Delete the nodes
            while (nodeIterator.MoveNext())
            {
                nodeIterator.Current.DeleteSelf();
                nodeIterator = navigator.Select(xpathExpression);
            }
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(LawSyncDocumentCollection documentCollection)
        {
            var message = new PipeMessageEnvelope { Body = documentCollection };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(documentCollection.Documents.Count);
        }

        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLogPipe(List<JobWorkerLog<LawSyncLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope { Body = log };
            LogPipe.Send(message);
        }
    }
}

