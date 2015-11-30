using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Extension methods for EDRM Document entity
    /// </summary>
    public static class MiscEDRMActions    
    {
        /// <summary>
        /// Deletes all non native files (text, images etc).
        /// </summary>
        /// <param name="edrmDocument">The EDRM document in which file entities referring to non native files to be deleted.</param>
        /// <returns> Success status </returns>
        public static bool DeleteNonNativeFiles(this EDRMManager edrmManager)
        {
            try
            {                
                List<DocumentEntity> edrmDocument = edrmManager.Documents.ToList();
                string fileName = edrmManager.SourcePath + @"\" + edrmManager.EDRMFileName;
                FileInfo edrmFileOnDisk = new FileInfo(fileName);

                edrmDocument.ForEach(p => DeleteFilesInDocumentEntity(p, edrmFileOnDisk));
                
                edrmFileOnDisk = null;
                edrmDocument = null;
                edrmManager = null;

                FileInfo file = new FileInfo(fileName);

                if (!GCCleanAndDeleteFile(file))
                {
                    Thread.Sleep(300);
                    GCCleanAndDeleteFile(file);
                }
                
                return true;
            }
            catch
            {                
                return false;
            }
        
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.GC.Collect")]
        private static bool GCCleanAndDeleteFile(FileInfo fileObject)
        {
            try
            {
                // GC.Collect() to release file locks by existing objects. 
                // If this statement is not there, sometimes, file would be locked and delete will error out.
                GC.Collect();
                fileObject.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes all files except native type in document entity.
        /// </summary>
        /// <param name="document">The document entity</param>
        /// <param name="edrmFile">The edrm file.</param>
        private static void DeleteFilesInDocumentEntity(DocumentEntity document, FileInfo edrmFile)
        {
            // Loop through all file entities in EDRM Document
            foreach (ExternalFileEntity externalFile in document.Files.Where(file => !file.FileType.ToLower().Equals(Constants.EDRMAttributeNativeFileType.ToLower())).SelectMany(file => file.ExternalFile))
            {
                // Delete all non native files
                File.Delete(CreateFilePath(externalFile,edrmFile.DirectoryName));
            }
        }


        /// <summary>
        /// Creates file path for specified file in EDRM document.
        /// Handles 1) relative path from EDRM location, 2) file at EDRM location and 3) absolute path the file
        /// </summary>
        /// <param name="externalFile"> The external file entity. It represents EDRM file's external file entity </param>
        /// <param name="eDRMFileLocation"> The eDRM file location. </param>
        /// <returns>
        /// Complete file URI
        /// </returns>
        private static string CreateFilePath(ExternalFileEntity externalFile, string eDRMFileLocation)
        {
            // file path and file name from external file BEO
            string fileLocation = externalFile.FilePath, fileName = externalFile.FileName;            

            // file at EDRM location
            if (string.IsNullOrEmpty(fileLocation))
            {
                return eDRMFileLocation + @"\" + fileName;
            }
            else
            {
                // Check if file location is absolute path
                // Condition 1: if file location contains ":", it's drive location. for example C:\ - hence it's absolute path.
                // Condition 2: if file location's first character is "\\" it's shared drive - hence it's absolute path.
                if (fileLocation.Contains(":") || fileLocation.Substring(0, 1).Equals("\\"))
                {
                    // does last character of file location have \. if not add it.
                    if (!fileLocation.Substring(fileLocation.Length - 1, 1).Equals(@"\")) fileLocation = fileLocation + @"\";

                    return fileLocation + fileName;
                }
                else // relative path to the file from EDRM location
                {
                    // does last character of file location have \. if not add it.
                    if (!fileLocation.Substring(fileLocation.Length - 1, 1).Equals(@"\")) fileLocation = fileLocation + @"\";

                    return eDRMFileLocation + @"\" + fileLocation + fileName;
                }
            }
        }
    }
}
