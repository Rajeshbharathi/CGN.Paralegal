using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Worker
{
    using Infrastructure.ExceptionManagement;

    public class ExportVolumeWorker : WorkerBase
    {
        private ExportDocumentCollection _exportDocumentCollection;
        private ExportVolumeBEO _volume;
        private string _exportBaseFolderPath = string.Empty;
        private long _currentVolumeNativeFileCount;
        private long _currentVolumeNativeRunningNumber;
        private long _currentVolumeNativeFileSize;
        private long _currentVolumeImageFileCount;
        private long _currentVolumeImageRunningNumber;
        private long _currentVolumeImageFileSize;
        private long _currentVolumeTextFileCount;
        private long _currentVolumeTextRunningNumber;
        private long _currentVolumeTextFileSize;
        private long _maxFileInFolder;
        private string _volumeSubFolderFormat = string.Empty;
        private string _nativeFolderName = string.Empty;
        private string _imageFolderName = string.Empty;
        private string _textFolderName = string.Empty;
        private long _maxFileSize;
        private const string MegaBytes = "MB";
        private const string GigaBytes = "GB";
        private const string TeraBytes = "TB";
        private string _bootParameter;
        private int _maxParallelThread;
        #region Overdrive

        protected override void BeginWork()
        {
            base.BeginWork();
            _bootParameter = BootParameters;
            _maxParallelThread = Convert.ToInt32(ApplicationConfigurationManager.GetValue("NumberOfMaxParallelism", "Export"));
        }

        protected override void ProcessMessage(PipeMessageEnvelope message)
        {
            try
            {
                _exportDocumentCollection = (ExportDocumentCollection)message.Body;

                #region Assertion
                //Pre Condition
                PipelineId.ShouldNotBeEmpty();
                BootParameters.ShouldNotBe(null);
                BootParameters.ShouldBeTypeOf<string>();
                _exportDocumentCollection.ShouldNotBe(null);
                _exportDocumentCollection.Documents.ShouldNotBe(null);
                _exportDocumentCollection.Documents.LongCount().ShouldBeGreaterThan(0);
                #endregion

                if (_exportDocumentCollection == null || _exportDocumentCollection.Documents == null)
                {
                    Tracer.Error("ExportOption Volume Worker: Document detail is not set in pipe message for job run id: {0}", PipelineId);
                    return;
                }

                if (_volume == null)
                {
                    InitializeForProcessing(BootParameters);
                }

                CalculateVolume(_exportDocumentCollection.Documents);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                ReportToDirector(ex);
            }
        }

        private void InitializeForProcessing(string exportBootParameter)
        {
            _exportBaseFolderPath = _exportDocumentCollection.ExportOption.ExportDestinationFolderPath;
            var bootParameters = GetExportBEO<ExportLoadJobDetailBEO>(exportBootParameter);
            if (bootParameters.ExportVolume != null)
            {
                _volume = bootParameters.ExportVolume;
            }
            else
            {
                throw new EVException().AddUsrMsg("ExportOption Volume Worker: Volume information is not set in boot parameter for job run id: {0}",
                    PipelineId);
            }

            if (_volume != null && _volume.VolumeFolders)
            {
                #region Base Folders Name
                if (_volume.CreateseparateFolderImageNative)
                {
                    if (_nativeFolderName == string.Empty)
                    {
                        _nativeFolderName = (!string.IsNullOrEmpty(_volume.NativeFileName)
                                                    ? _volume.NativeFileName
                                                    : "Native");
                    }
                    if (_imageFolderName == string.Empty)
                    {
                        _imageFolderName = (!string.IsNullOrEmpty(_volume.ImageFileName)
                                                ? _volume.ImageFileName
                                                : "Image");
                    }
                    if (_textFolderName == string.Empty)
                    {
                        _textFolderName = (!string.IsNullOrEmpty(_volume.TextFileName)
                                                ? _volume.TextFileName
                                                : "Text");
                    }
                }
                #endregion

                if (_volume.IsIncreaseVolumeFolder)
                {
                    _currentVolumeNativeRunningNumber = _currentVolumeImageRunningNumber = _currentVolumeTextRunningNumber = Convert.ToInt64(_volume.IncreaseFolderNmae);

                    _volumeSubFolderFormat = !string.IsNullOrEmpty(_volume.IncreaseFolderNmae) ? _volumeSubFolderFormat.PadRight(_volume.IncreaseFolderNmae.Length, '0') : "0";

                    if (_maxFileInFolder <= 0)
                    {
                        if (!string.IsNullOrEmpty(_volume.MaxNumberFilesInFolder))
                        {
                            _maxFileInFolder = Convert.ToInt64(_volume.MaxNumberFilesInFolder);
                        }
                    }
                    if (!String.IsNullOrEmpty(_volume.MemorySize))
                    {
                        _maxFileSize = SetVolumeMaxSize();
                    }
                }
            }
        }

        private void CalculateVolume(IEnumerable<ExportDocumentDetail> documents)
        {
            var exportDocumentDetails = new List<ExportDocumentDetail>();
            var loadFileHelper = new ExportLoadFileHelper(_bootParameter);
           
            Parallel.ForEach(documents.ToList(),
                new ParallelOptions { MaxDegreeOfParallelism = _maxParallelThread },
                (docDetail) => loadFileHelper.SetImageSourceFiles(docDetail, _exportDocumentCollection.ExportOption));

            foreach (var documentDetail in documents)
            {
                exportDocumentDetails.Add(SetDestinationFolderByFileType(documentDetail));
               
                if (exportDocumentDetails.Count <= 100) continue;
                loadFileHelper.RemoveImageFile(exportDocumentDetails, _exportBaseFolderPath);
                Send(exportDocumentDetails);
                exportDocumentDetails.Clear();
            }

            if (!exportDocumentDetails.Any()) return;
            loadFileHelper.RemoveImageFile(exportDocumentDetails, _exportBaseFolderPath);
            Send(exportDocumentDetails);
            exportDocumentDetails.Clear();
        }

        private void Send(List<ExportDocumentDetail> docDetails)
        {
            try
            {
                _exportDocumentCollection.Documents.Clear();
                _exportDocumentCollection.Documents = docDetails;
                var message = new PipeMessageEnvelope
                {
                                      Body = _exportDocumentCollection
                                  };
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(_exportDocumentCollection.Documents.Count());
            }
            catch (Exception exception)
            {
                Tracer.Error("Export Volume Worker: Unable to send message for job run id: {0} Exception details: {1}",
                             PipelineId, exception);
            }
        }

        #endregion

        #region Destination Folder
        /// <summary>
        /// Set volume folder for source file.
        /// </summary>      
        private ExportDocumentDetail SetDestinationFolderByFileType(ExportDocumentDetail documentDetail)
        {
            GetVolumeName(documentDetail);

            CreateFolders(documentDetail);
            return documentDetail;
        }

        /// <summary>
        /// Get the volume name
        /// </summary>
        /// <param name="documentDetail">The document details</param>
        /// <returns></returns>
        private void GetVolumeName(ExportDocumentDetail documentDetail)
        {
            if (_exportDocumentCollection.ExportOption.IsNative &&
                documentDetail.NativeFiles != null &&
                documentDetail.NativeFiles.Count > 0)
            {
                GetNativeFileVolumeDetails(documentDetail);
            }

            if ((_exportDocumentCollection.ExportOption.IsImage ||
                    _exportDocumentCollection.ExportOption.IsProduction) &&
                documentDetail.ImageFiles != null &&
                documentDetail.ImageFiles.Count > 0)
            {
                GetImageFileVolumeDetails(documentDetail);
            }

            if (_exportDocumentCollection.ExportOption.IsText &&
                documentDetail.TextFiles != null &&
                documentDetail.TextFiles.Count > 0)
            {
                GetTextFileVolumeDetails(documentDetail);
            }
        }
        /// <summary>
        /// Gets the text file volume details.
        /// </summary>
        /// <param name="documentDetail">The document detail.</param>
        private void GetTextFileVolumeDetails(ExportDocumentDetail documentDetail)
        {
            long textFileCount = 0;
            textFileCount += documentDetail.TextFiles.Count;
            var volumeFolderName = (!string.IsNullOrEmpty(_volume.VolumeFoldersName)) ? _volume.VolumeFoldersName : string.Empty;
            var volumeName = volumeFolderName;
            if (_volume.VolumeFolders && _volume.IsIncreaseVolumeFolder)
            {
                var folderRunningIndex = _volume.IsMemorySize
                    ? GetVolumeFolderTextRunningIndexWithFileSize(
                        textFileCount, documentDetail.TextFiles).ToString(_volumeSubFolderFormat)
                    : GetVolumeTextFolderRunningIndexWithFileCount(textFileCount).ToString(
                        _volumeSubFolderFormat);
                volumeName = volumeFolderName + folderRunningIndex;
            }
            SetDestinationFolder(documentDetail.TextFiles, volumeName, _textFolderName);
        }

        /// <summary>
        /// Gets the image file volume details.
        /// </summary>
        /// <param name="documentDetail">The document detail.</param>
        private void GetImageFileVolumeDetails(ExportDocumentDetail documentDetail)
        {
            long imageFileCount = 0;
            imageFileCount += documentDetail.ImageFiles.Count;
            var volumeFolderName = (!string.IsNullOrEmpty(_volume.VolumeFoldersName)) ? _volume.VolumeFoldersName : string.Empty;
            var volumeName = volumeFolderName;
            if (_volume.VolumeFolders && _volume.IsIncreaseVolumeFolder)
            {
                var folderRunningIndex = _volume.IsMemorySize
                    ? GetVolumeFolderImageRunningIndexWithFileSize(
                        imageFileCount, documentDetail.ImageFiles).ToString(_volumeSubFolderFormat)
                    : GetVolumeImageFolderRunningIndexWithFileCount(imageFileCount).ToString(
                        _volumeSubFolderFormat);
                volumeName = volumeFolderName + folderRunningIndex;
            }
            SetDestinationFolder(documentDetail.ImageFiles, volumeName, _imageFolderName);
        }

        /// <summary>
        /// Gets the native file volume details.
        /// </summary>
        /// <param name="documentDetail">The document detail.</param>
        private void GetNativeFileVolumeDetails(ExportDocumentDetail documentDetail)
        {
            long nativeFileCount = 0;
            nativeFileCount += documentDetail.NativeFiles.Count;
            
            var volumeFolderName = (!string.IsNullOrEmpty(_volume.VolumeFoldersName)) ? _volume.VolumeFoldersName : string.Empty;
            var volumeName = volumeFolderName;
            if (_volume.VolumeFolders && _volume.IsIncreaseVolumeFolder)
            {
                var folderRunningIndex = _volume.IsMemorySize
                    ? GetVolumeNativeFolderRunningIndexWithFileSize(
                        nativeFileCount, documentDetail.NativeFiles).ToString(_volumeSubFolderFormat)
                    : GetVolumeNativeFolderRunningIndexWithFileCount(nativeFileCount).ToString(
                        _volumeSubFolderFormat);
                volumeName = volumeFolderName + folderRunningIndex;
            }
            SetDestinationFolder(documentDetail.NativeFiles, volumeName, _nativeFolderName);
        }

        /// <summary>
        /// Get Export File size
        /// </summary>
        /// <param name="exportFiles">Export Files</param>
        private long GetExportFileSize(IEnumerable<ExportFileInformation> exportFiles)
        {
            return (from file in exportFiles
                    where !string.IsNullOrEmpty(file.SourceFilePath) && File.Exists(file.SourceFilePath)
                    select new FileInfo(file.SourceFilePath) into fileInfo
                    select fileInfo.Length).Sum();
        }

        /// <summary>
        /// Sets the destination folder.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="volumeName">Name of the volume.</param>
        /// <param name="folderName">Name of the folder.</param>
        private void SetDestinationFolder(IEnumerable<ExportFileInformation> documents, string volumeName, string folderName)
        {
            foreach (var file in documents)
            {
                file.DestinationFolder = Path.Combine(_exportBaseFolderPath, folderName, volumeName);
            }
        }

        /// <summary>
        /// Get volume folder for given file based on file count
        /// </summary>    
        private long GetVolumeNativeFolderRunningIndexWithFileCount(long fileCount)
        {
            if ((_currentVolumeNativeFileCount + fileCount) < (_maxFileInFolder + 1))
            {
                _currentVolumeNativeFileCount += fileCount;
            }
            else
            {
                _currentVolumeNativeRunningNumber = _currentVolumeNativeRunningNumber + 1;
                _currentVolumeNativeFileCount = fileCount; //Set Starting Number
            }

            return _currentVolumeNativeRunningNumber;
        }

        /// <summary>
        /// Get volume folder for given file based on size
        /// </summary>        
        private long GetVolumeNativeFolderRunningIndexWithFileSize(long fileCount, List<ExportFileInformation> nativeFiles)
        {
            var fileSize = GetExportFileSize(nativeFiles);
            _currentVolumeNativeFileSize = _currentVolumeNativeFileSize + fileSize;
            if ((_currentVolumeNativeFileCount + fileCount) > (_maxFileInFolder))
            {
                _currentVolumeNativeRunningNumber = _currentVolumeNativeRunningNumber + 1;
                _currentVolumeNativeFileCount = fileCount;
                _currentVolumeNativeFileSize = fileSize;
            }
            else if ((_currentVolumeNativeFileSize) > (_maxFileSize + 1))
            {
                _currentVolumeNativeRunningNumber = _currentVolumeNativeRunningNumber + 1;
                _currentVolumeNativeFileCount = fileCount;
                _currentVolumeNativeFileSize = fileSize;
            }
            else
            {
                _currentVolumeNativeFileCount += fileCount;
            }
            return _currentVolumeNativeRunningNumber;
        }

        /// <summary>
        /// Get volume folder for given file based on file count
        /// </summary>    
        private long GetVolumeImageFolderRunningIndexWithFileCount(long fileCount)
        {
            if ((_currentVolumeImageFileCount + fileCount) < (_maxFileInFolder + 1))
            {
                _currentVolumeImageFileCount += fileCount;
            }
            else
            {
                _currentVolumeImageRunningNumber = _currentVolumeImageRunningNumber + 1;
                _currentVolumeImageFileCount = fileCount; //Set Starting Number
            }

            return _currentVolumeImageRunningNumber;
        }

        /// <summary>
        /// Get volume folder for given file based on size
        /// </summary>        
        private long GetVolumeFolderImageRunningIndexWithFileSize(long fileCount, List<ExportFileInformation> imageFiles)
        {
            var fileSize = GetExportFileSize(imageFiles);
            _currentVolumeImageFileSize = _currentVolumeImageFileSize + fileSize;
            if ((_currentVolumeImageFileCount + fileCount) > (_maxFileInFolder))
            {
                _currentVolumeImageRunningNumber = _currentVolumeImageRunningNumber + 1;
                _currentVolumeImageFileCount = fileCount;
                _currentVolumeImageFileSize = fileSize;
            }
            else if ((_currentVolumeImageFileSize) > (_maxFileSize + 1))
            {
                _currentVolumeImageRunningNumber = _currentVolumeImageRunningNumber + 1;
                _currentVolumeImageFileCount = fileCount;
                _currentVolumeImageFileSize = fileSize;
            }
            else
            {
                _currentVolumeImageFileCount += fileCount;
            }
            return _currentVolumeImageRunningNumber;
        }

        /// <summary>
        /// Get volume folder for given file based on file count
        /// </summary>    
        private long GetVolumeTextFolderRunningIndexWithFileCount(long fileCount)
        {
            if ((_currentVolumeTextFileCount + fileCount) < (_maxFileInFolder + 1))
            {
                _currentVolumeTextFileCount += fileCount;
            }
            else
            {
                _currentVolumeTextRunningNumber = _currentVolumeTextRunningNumber + 1;
                _currentVolumeTextFileCount = fileCount; //Set Starting Number
            }

            return _currentVolumeTextRunningNumber;
        }

        /// <summary>
        /// Get volume folder for given file based on size
        /// </summary>        
        private long GetVolumeFolderTextRunningIndexWithFileSize(long fileCount,List<ExportFileInformation> textFiles)
        {
            var fileSize = GetExportFileSize(textFiles);
            _currentVolumeTextFileSize = _currentVolumeTextFileSize + fileSize;
            if ((_currentVolumeTextFileCount + fileCount) > (_maxFileInFolder))
            {
                _currentVolumeTextRunningNumber = _currentVolumeTextRunningNumber + 1;
                _currentVolumeTextFileCount = fileCount;
                _currentVolumeTextFileSize = fileSize;
            }
            else if ((_currentVolumeTextFileSize) > (_maxFileSize + 1))
            {
                _currentVolumeTextRunningNumber = _currentVolumeTextRunningNumber + 1;
                _currentVolumeTextFileCount = fileCount;
                _currentVolumeTextFileSize = fileSize;
            }
            else
            {
                _currentVolumeTextFileCount += fileCount;
            }
            return _currentVolumeTextRunningNumber;
        }

        /// <summary>
        /// Volume Max Size
        /// </summary>       
        private long SetVolumeMaxSize()
        {
            long maxSize = 0;
            var userVolumeSize = (!string.IsNullOrEmpty(_volume.MemorySize) ? Convert.ToInt64(_volume.MemorySize) : 0);
            switch (_volume.MemoryScale.Trim().ToUpper())
            {
                case MegaBytes:
                    maxSize = userVolumeSize * 1024 * 1024;
                    break;
                case GigaBytes:
                    maxSize = userVolumeSize * 1024 * 1024 * 1024;
                    break;
                case TeraBytes:
                    maxSize = userVolumeSize * 1024 * 1024 * 1024 * 1024;
                    break;
            }
            return maxSize;
        }

        /// <summary>
        /// Create folder in export path
        /// </summary>       
        private void CreateFolders(ExportDocumentDetail documentDetail)
        {
            if (documentDetail.NativeFiles != null)
            {
                documentDetail.NativeFiles.ForEach(file => Directory.CreateDirectory(file.DestinationFolder));
            }

            if (documentDetail.TextFiles != null)
            {
                documentDetail.TextFiles.ForEach(file => Directory.CreateDirectory(file.DestinationFolder));
            }

            if (documentDetail.ImageFiles != null)
            {
                documentDetail.ImageFiles.ForEach(file => Directory.CreateDirectory(file.DestinationFolder));
            }
        }
        #endregion

        #region Common
        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private T GetExportBEO<T>(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(T));

            //Deserialization of bootparameter to get ImportBEO
            return (T)xmlStream.Deserialize(stream);
        }
        #endregion
    }
}
