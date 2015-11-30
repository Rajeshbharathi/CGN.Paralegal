using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// 'Export Load File' helper, contain common method which called across all workers, internally these methods will call the BO method to get file paths and metadata.
    ///  Purpose : Call BO and convert Business Entities into Worker Data entity
    /// </summary>
    public class ExportLoadFileHelper
    {
        private readonly ExportLoadJobDetailBEO _exportLoadJobDetailBeo;
        public ExportLoadFileHelper(string bootParameter)
        {
            _exportLoadJobDetailBeo = Utils.SmartXmlDeserializer(bootParameter) as ExportLoadJobDetailBEO;
        }

         /// <summary>
         /// Set Image source files
         /// </summary>
        public void SetImageSourceFiles(ExportDocumentDetail documentDetail, ExportOption exportOption)
        {
            if (!exportOption.IsImage && !exportOption.IsProduction) return;

            var imageCollectionId = exportOption.IsImage
                ? exportOption.ImageSetCollectionId
                : exportOption.ProductionSetCollectionId;

            var documentFiles = DocumentBO.GetImagesForExportLoadFile(documentDetail.DocumentId,
                exportOption.IsImage,
                exportOption.IsProduction,
                imageCollectionId, Convert.ToInt32(_exportLoadJobDetailBeo.MatterId));

            if ((documentDetail.ImageFiles!=null && documentDetail.ImageFiles.Any()) && documentFiles.Any())
            {
                var sourceFiles = documentFiles.OrderBy(p => p.Path).ToList();
                var pathIndex = 0;
                foreach (var image in documentDetail.ImageFiles)
                {
                    if (pathIndex > sourceFiles.Count) break;
                    image.SourceFilePath =  sourceFiles[pathIndex].Path;
                    image.DestinationFolder = string.Format("{0}{1}", documentDetail.ExportBasePath, image.DestinationFolder);
                    pathIndex++;
                }
            }
            else
            {
                var lstImgFiles = new List<ExportFileInformation>();
                if (documentFiles != null)
                {
                    lstImgFiles.AddRange(ConvertToExportFileInformation(documentFiles));
                }
                documentDetail.ImageFiles = lstImgFiles;
            }
        }

        /// <summary>
        /// Remove image files
        /// </summary>
        public void RemoveImageFile(IEnumerable<ExportDocumentDetail> documents,string exportBasePath)
        {
            foreach (var exportDocumentDetail in documents)
            {
                if (exportDocumentDetail.ImageFiles != null)
                {
                    exportDocumentDetail.ImageFiles.SafeForEach(f => f.SourceFilePath = string.Empty);
                    exportDocumentDetail.ImageFiles.SafeForEach(f => f.DestinationFolder = f.DestinationFolder.Replace(exportBasePath, String.Empty));
                }
                exportDocumentDetail.ExportBasePath = exportBasePath;
            }
        }

        private IEnumerable<ExportFileInformation> ConvertToExportFileInformation(IEnumerable<RVWExternalFileBEO> externalFileList)
        {
            return externalFileList.Select(externalFile => new ExportFileInformation { SourceFilePath = externalFile.Path }).ToList().OrderBy(f=>f.SourceFilePath);
        }
    }
}
