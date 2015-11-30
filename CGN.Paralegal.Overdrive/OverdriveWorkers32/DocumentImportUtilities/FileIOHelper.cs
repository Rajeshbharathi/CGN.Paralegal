#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="FileIOHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Keerti</author>
//      <description>
//          This file contains all the  methods related to a file io helper
//      </description>
//      <changelog>
//           <date value="08/11/2011">Bugs Fixed #92070</date>
//           <date value="01/10/2012">Bugs Fixed #92992</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
#region Namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
#endregion

namespace LexisNexis.Evolution.FileUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Encapsulates FileLocation related actions - mostly extension functions.
    /// </summary>
    public class FileIOHelper
    {

        /// <summary>
        /// Occurs when [batch of documents are available].
        /// </summary>

        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event Action<List<string>, bool> BatchOfDocumentsAvailable;

        /// <summary>
        /// list of file extensions that should not batch
        /// </summary>
        private readonly List<string> doNotBatchList = new List<string>() { ".pst" };

        /// <summary>
        /// Gets source sorted list of file names from the specified source location.
        /// </summary>
        /// <param name="locations">The locations.</param>
        /// <param name="excludedFileList">The excluded file list.</param>
        /// <param name="batchSize"></param>
        /// <returns>
        /// Sorted list of file names
        /// </returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public void GetSortedFileList(IEnumerable<FileLocation> locations, IEnumerable<string> excludedFileList, int batchSize)
        {
            List<string> fileList = new List<string>();
            bool isLastDocument;
            long totalDocuments = 0;
            long documentsSent = 0;
            #region Delegate that verifies if given file is part of exclude list
            Func<string, bool> handleFile = delegate(string location)
            {
                // Boot parameter always give file extensions wihtout "." (UI rule). But GetExtension returns "." as well. So always exclude first character
                var extension = Path.GetExtension(location);
                if (extension != null && (excludedFileList != null && excludedFileList.Any() && (excludedFileList.Contains(extension.Substring(1)))))
                {
                    // file is in excluded list - hence don't do anything.
                    return true;
                }
                if (doNotBatchList != null && doNotBatchList.Count > 0 &&
                    doNotBatchList.Contains(Path.GetExtension(location)))
                {
                    documentsSent++;
                    BatchOfDocumentsAvailable(new List<string>() { location }, totalDocuments == documentsSent);
                }
                else
                {
                    // excluded list may be empty - file is not in excluded list.
                    fileList.Add(location);

                    if (fileList.Count >= batchSize)
                    {
                        documentsSent += fileList.Count;
                        //this is to identify the last message in case no of files equal to batch size
                        isLastDocument = (totalDocuments == documentsSent);
                        // raise event if batch size is greater than or equal to configured batch size
                        BatchOfDocumentsAvailable(fileList, isLastDocument);

                        // clear and continue the rest of processing
                        fileList.Clear();
                    }
                }

                return true;
            };
            #endregion  Delegate that verifies if given file is part of exclude list

            if (locations != null)
            {
                //decode the location file path as we encode it in UI
                locations.ToList<FileLocation>().ForEach(fileLocation => fileLocation.Path = HttpUtility.HtmlDecode(fileLocation.Path));

                foreach (var location in locations)
                {
                    try
                    {
                        isLastDocument = false;
                        if (location.LocationType.Equals("Folder", StringComparison.CurrentCultureIgnoreCase))
                        {
                            #region When location is a folder
                            if (Directory.Exists(location.Path)) // If source is a directory, get all files in the directory.
                            {
                                SearchOption searchOption = location.IsIncludeSubfolder ?
                                                SearchOption.AllDirectories : // Get all the files from the directory - including files in sub directories.  
                                                SearchOption.TopDirectoryOnly; // Get all the files from the first level directory

                                string[] filesInLocation = Directory.GetFiles(location.Path, "*", searchOption);
                                
                                if (filesInLocation.Any())
                                {
                                    totalDocuments += filesInLocation.Count();
                                    foreach (string file in filesInLocation)
                                    {
                                        handleFile(file);
                                    }
                                }

                            }
                            else
                            {

                                throw new EVException().AddUsrMsg("Can not access Source Location: " + location.Path).AddResMsg(ErrorCodes.EDLoaderSourceLocationNotAccessible);
                            }
                            #endregion When location is a folder
                        }
                        else
                        {
                            #region When location is a file
                            // So check if the file exists
                            if (File.Exists(location.Path))
                            {
                                // if it exists, then handle file by checking if it's in excluded list and then raising the event if it's batch size is more than or equal to configured batch size.
                                handleFile(location.Path);
                            }
                            else
                            {
                                throw new EVException().AddUsrMsg("Can not access Source Location: " + location.Path).AddResMsg(ErrorCodes.EDLoaderSourceLocationNotAccessible);
                            }

                            #endregion When location is a file
                        }
                    }
                    #region Exception Handling
                    catch (Exception exception)
                    {
                        exception.AddErrorCode(ErrorCodes.EDLoaderGetFileListFailure);
                        throw;
                    }
                    #endregion Exception Handling

                }

                // if there are final set of documents left, send them through event.
                // DO NOT CHECK if filelist has any documents, the worker expecting messages in the pipeline, needs a message qualifying itself as the last message.
                // It's upto that worker to identify the final message.
                if (fileList.Count > 0)
                    BatchOfDocumentsAvailable(fileList, true);

            }
        }
    }
}

