using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{
    public class FileDialogController : ApiController
    {
        /// <summary>
        /// Get shared path 
        /// </summary>
        /// <param name="organizationId">Organization</param>
        [Route("api/filedialog/share/{organizationId}")]
        public HttpResponseMessage GetShares(string organizationId)
        {
            try
            {
                //TODO: Share paths list fetch from WCF
                var filedialogs = SharedPath();
                return Request.CreateResponse<IEnumerable<FileDialog>>(HttpStatusCode.OK, filedialogs);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Get folder file path
        /// </summary>
        /// <param name="pathType">Path type</param>
        /// <param name="fileInfo">FileInfo</param>
        /// <returns></returns>
        [Route("api/filedialog/path/{pathType}")]
        public HttpResponseMessage PostGetFolderFilePath(string pathType,FileDialog fileInfo)
        {
            try
            {
                var filedialogs = GetFoldersFiles(pathType, fileInfo);

                return Request.CreateResponse<IEnumerable<FileDialog>>(HttpStatusCode.OK, filedialogs);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Get folder and files
        /// </summary>
        /// <param name="pathType">Path type</param>
        /// <param name="fileInfo">FileInfo</param>
        private static List<FileDialog> GetFoldersFiles(string pathType, FileDialog fileInfo)
        {
            var filedialogs = new List<FileDialog>();
            string path = fileInfo.Path;
            var sharePath = SharedPath();
            if ((sharePath.Exists(p => p.Path == path) && fileInfo.Type == "root"))
            {
                filedialogs = sharePath;
            }
            else
            {
                //TODO: Folders list fetch from WCF
                var folders = GetFolders(path);
                var index = path.LastIndexOf(@"\", StringComparison.Ordinal);
                var folderPath = sharePath.Exists(p => p.Path == path) ? path : path.Substring(0, index);
                var type = sharePath.Exists(p => p.Path == path) ? "root" : "folder";
                filedialogs.Add(new FileDialog {Name = "..(UP)", Path = folderPath, Type = type});
                filedialogs.AddRange(
                    folders.Select(
                        folder => new FileDialog {Name = folder.Replace((path + @"\"), ""), Path = folder, Type = "folder"}));

                if (pathType == "file")
                {
                    var files = GetFilePaths(path);
                    filedialogs.AddRange(
                        files.Select(file => new FileDialog {Name = file.Replace((path + @"\"), ""), Path = file, Type = "file"}));
                }
            }
            return filedialogs;
        }

        /// <summary>
        /// Get folder file path with validate
        /// </summary>
        /// <param name="pathType">Path type</param>
        /// <param name="fileInfo">FileInfo</param>
        [Route("api/filedialog/path/validate/{pathType}")]
        public HttpResponseMessage PostGetFolderFilePathWithValidate(string pathType, FileDialog fileInfo)
        {
            try
            {
                var filedialogs = new List<FileDialog>();
                if (IsValidateFilePath(fileInfo.Path, pathType))
                {
                    if (Path.HasExtension(fileInfo.Path))
                    {
                        fileInfo.Path = fileInfo.Path.Substring(0, fileInfo.Path.LastIndexOf(@"\", System.StringComparison.Ordinal));
                    }
                    filedialogs = GetFoldersFiles(pathType, fileInfo);
                }
                return Request.CreateResponse<IEnumerable<FileDialog>>(HttpStatusCode.OK, filedialogs);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        #region Service
        //TODO:Below methods will be replaced by service
        private static List<FileDialog> SharedPath()
        {
            //TODO: Share paths list fetch from WCF
            return new List<FileDialog>
                                  {
                                      new FileDialog{Name="import",Path=@"C:\SenShare\ImportTest",Size = 22},
                                      new FileDialog{Name="export",Path=@"C:\SenShare\ExportTest",Size = 23}
                                  };
        }

        private static IEnumerable<string> GetFolders(string path)
        {
            //TODO: Folders list come from WCF
            return Directory.GetDirectories(path);
        }

        private static IEnumerable<string> GetFilePaths(string path)
        {
            //TODO: File list come from WCF
            return Directory.GetFiles(path);
        }

        private static bool IsValidateFilePath(string path, string type)
        {
            //TODO: Validate path handled in WCF
            var sharePaths = SharedPath();
            if (sharePaths.Exists(f => path.Contains(f.Path)))
            {
                switch (type)
                {
                    case "folder":
                        return Directory.Exists(path);
                    case "file":
                        return (Directory.Exists(path) || File.Exists(path));
                }
            }
            return false;
        }
        #endregion


    }
}