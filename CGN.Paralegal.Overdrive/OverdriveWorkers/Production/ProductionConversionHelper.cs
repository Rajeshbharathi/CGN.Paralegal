using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.Worker.Data;

namespace LexisNexis.Evolution.Worker
{
    public class ProductionConversionHelper
    {
        private const string ConDocumentTypeName = "Original Document";
        private const string ConPagingNameFormat = "_page";
        /// <summary>
        /// Rename Produced documents based on Bates Field Value
        /// </summary>
        /// <param name="productionDocumentDetail"></param>
        public List<string> RenameProducedImages(ProductionDocumentDetail productionDocumentDetail)
        {
            var producedImages = new List<string>();
            if (String.IsNullOrEmpty(productionDocumentDetail.ExtractionLocation)) return null;
            var files =
                new DirectoryInfo(productionDocumentDetail.ExtractionLocation).
                    GetFiles(productionDocumentDetail.StartingBatesNumber + ConPagingNameFormat + "*");
            foreach (var file in files)
            {
                if (file.DirectoryName == null) continue;
                if (String.IsNullOrEmpty(file.Name)) continue;
                var fileExtenstion = Path.GetExtension(file.Name);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                if (fileNameWithoutExtension == null) continue;
                var strPageNumber = fileNameWithoutExtension.Replace(
                    productionDocumentDetail.
                        StartingBatesNumber, "").Replace(
                            ConPagingNameFormat, "");
                long pageNumebr;

                if (!long.TryParse(strPageNumber, out pageNumebr)) continue;

                long batesBeginNumber;

                var strBatesBeginNumber = productionDocumentDetail.
                    StartingBatesNumber.Replace(
                        productionDocumentDetail.Profile.ProductionPrefix, "");
                if (!long.TryParse(strBatesBeginNumber,
                                   out batesBeginNumber))
                {
                    continue;
                }
                var batesRunningNumber = batesBeginNumber + pageNumebr;
                var fileName = productionDocumentDetail.Profile.ProductionPrefix +
                               batesRunningNumber.ToString("D" +
                                                           productionDocumentDetail.Profile.ProductionStartingNumber.
                                                               Length) +
                               fileExtenstion;
                var imageFilePath = Path.Combine(file.DirectoryName,
                                                 fileName);
                if (File.Exists(imageFilePath)) // If same document was reprocessed again using ‘Reprocess Job’, then need to delete old one & keep the latest
                {
                    File.Delete(imageFilePath);
                }
                File.Move(file.FullName, imageFilePath);
                producedImages.Add(imageFilePath);
            }
            return producedImages;
        }


        public bool UpdateProducedImageFilePath(string documentId, string collectionId, long matterId, List<string> producedImages,string userId)
        {
            var fileCount = 0;
            var documentTextEntities = new List<DocumentTextEntity>();
            producedImages.Sort();
            foreach (var imageFile in producedImages)
            {
                fileCount = fileCount + 1;
                documentTextEntities.Add(new DocumentTextEntity
                {
                    CollectionId = new Guid(collectionId),
                    CreatedBy = userId,
                    DocumentReferenceId = documentId,
                    SequenceId = fileCount,
                    DocumentTextType = new DocumentTextTypeEntity
                    {
                        TextTypeId = 2,
                        TextTypeName = ConDocumentTypeName
                    },
                    DocumentText = imageFile
                });
            }
            var documentMasterRecords = new List<DocumentMasterEntity>
                                            {
                                                new DocumentMasterEntity
                                                    {
                                                        DocumentReferenceId = documentId,
                                                        CollectionId = new Guid(collectionId)
                                                    }
                                            };
            var documentVaultManager = new DocumentVaultManager();
            return documentVaultManager.BulkUpdateImagesInDocumentText(matterId, documentMasterRecords, documentTextEntities,2);
        }
    }
}
