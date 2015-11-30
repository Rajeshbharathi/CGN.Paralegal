//---------------------------------------------------------------------------------------------------
// <copyright file="LogUtility.cs" company="Cognizant">
//      Copyright (c) Cognizant. All rights reserved.
// </copyright>
// <header>
//      <author>Suneeth Senthil/ Anandhi</author>
//      <description>
//          This file contains the File Info wrapper class created for holding file information.
//      </description>
//      <changelog>
//          <date value="3/30/2014">CNEV 3.0 - Requirement Bug #165088 - Document Delete NFR and functional fix : babugx</date>
//          <date value="5/2/2014">CNEV 3.0 - Bug# 168471,168515 - Deduplication and Billing report fix : babugx</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using CGN.Paralegal.Infrastructure.ExceptionManagement;
using System.Linq;
using System.Text;
using System.Threading;

namespace CGN.Paralegal.Infrastructure.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Interface to hold the basic file details
    /// </summary>
    public interface IFileInfo
    {
        #region Properties
        /// <summary>
        /// Read only Property that indicates the file path
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Read only Property that indicates if the physical file exists
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Read only Property that indicates if the physical file exists
        /// </summary>
        DateTime LastAccessTime { get; }

        /// <summary>
        /// Read only Property that indicates if the file extension
        /// </summary>
        string Extension { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Method to set the file path
        /// </summary>
        /// <param name="path"></param>
        void SetPath(string path);

        /// <summary>
        /// Method to delete the file
        /// </summary>
        void Delete();

        void BatchDelete(IEnumerable<string> pathList);

        #endregion
    }

    /// <summary>
    /// This is a wrapper class to the FileInfo
    /// </summary>
    public class FileInfoWrapper : IFileInfo
    {
        #region Private Variables

        /// <summary>
        /// FileInfo Member
        /// </summary>
        private FileInfo file;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public FileInfoWrapper()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Property to indicate file path
        /// </summary>
        public string Path
        {
            get
            {
                return file.FullName;
            }
        }

        /// <summary>
        /// Property to indicate if file exists
        /// </summary>
        public bool Exists
        {
            get
            {
                return file.Exists;
            }
        }

        /// <summary>
        /// Property that denotes the last access time of the file
        /// </summary>
        public DateTime LastAccessTime
        {
            get
            {
                return file.LastAccessTime;
            }
        }

        /// <summary>
        /// Property to indicate the file extension
        /// </summary>
        public string Extension
        {
            get
            {
                return file.Extension;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method to set the file path
        /// </summary>
        /// <param name="path">Path of the file</param>
        public void SetPath(string path)
        {
            file = new FileInfo(path);
        }

        /// <summary>
        /// Method to delete the file
        /// </summary>
        public void Delete()
        {
            file.Delete();
        }

        /// <summary>
        /// Invoke Cmd window to execute del command for set of documents
        /// </summary>
        /// <param name="pathList">IEnumerable<string></param>
        public void BatchDelete(IEnumerable<string> pathList)
        {
            if (pathList == null || !pathList.Any())
                return;
            var cmdArg = new StringBuilder();
            // -- Build del command. /C represents run command and then terminate
            cmdArg.Append(@"/C del");
            foreach (var pt in pathList)
            {
                // --Loop thru every path and build the cmd argument for del
                // --/Q meant for quiet / silent
                // --/F meant for read-only file delete
                cmdArg.Append(@" /F/Q ");
                cmdArg.Append(string.Format("\"{0}\"", pt));
            }
            Tracer.Info("Del Command : {0}", cmdArg.ToString());
            var startInfo = new ProcessStartInfo("cmd", cmdArg.ToString())
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }

        #endregion
    }


    public class ParallelFileSystemProcessor
    {
        private List<string> fileSystemParam = null;

        public void SetParam(List<string> param)
        {
            fileSystemParam = param;
        }

        public void ParallelDelete(Action<string> fileOp)
        {
            var threads = new Thread[fileSystemParam.Count];
            for (int paramIdx = 0; paramIdx < fileSystemParam.Count; paramIdx++)
            {
                threads[paramIdx] = new Thread(ThreadDoWork);
                var threadParameter = new ParallelThreadParameter() { FileOp = fileOp, Param = fileSystemParam[paramIdx] };
                threads[paramIdx].Start(threadParameter);
            }

            for (int paramIdx = 0; paramIdx < fileSystemParam.Count; paramIdx++)
            {
                threads[paramIdx].Join();
            }
        }

        private static void ThreadDoWork(object data)
        {
            var threadParameter = data as ParallelThreadParameter;
            Debug.Assert(threadParameter != null, "threadParameter != null");
            threadParameter.FileOp(threadParameter.Param);
        }

        public static void DeleteFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    Tracer.Info("File successfully deleted : {0}", fileName);
                }
                else
                {
                    Tracer.Info("File not found (or) unauthorized : {0}", fileName);
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg(String.Format("Error in deleting file : {0}", fileName)).Trace().Swallow();
            }
        }

        public static void DeleteDirectory(string dirName)
        {
            try
            {
                if (Directory.Exists(dirName))
                {

                    Directory.Delete(dirName, true);
                    Tracer.Info("Directory successfully deleted : {0}", dirName);
                }
                else
                {
                    Tracer.Info("Directory not found (or) unauthorized : {0}", dirName);
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg(String.Format("Error in deleting directory : {0}", dirName)).Trace().Swallow();
            }
        }
    }

    public class ParallelThreadParameter
    {
        public Action<string> FileOp;
        public string Param;
    }

    /// <summary>
    /// Interface to hold the basic Directory details
    /// </summary>
    public interface IDirectoryInfo
    {
        #region Properties

        /// <summary>
        /// Read only Property that indicates if the physical file exists
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Read only Property that indicates name of the directory
        /// </summary>
        string Name { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Method to set the file path
        /// </summary>
        /// <param name="path">File Path</param>
        void SetPath(string path);

        /// <summary>
        /// Method to delete the file
        /// </summary>
        void Delete();

        /// <summary>
        /// Method to delete the file recursively
        /// </summary>
        void Delete(bool recursive);

        /// <summary>
        /// Method to batch delete folders
        /// </summary>
        /// <param name="dirList"></param>
        void BatchDelete(IEnumerable<string> dirList);

        #endregion
    }

    /// <summary>
    /// This is a wrapper class to the DirectoryInfo
    /// </summary>
    public class DirectoryInfoWrapper : IDirectoryInfo
    {
        #region Private Variables

        /// <summary>
        /// DirectoryInfo Member
        /// </summary>
        private DirectoryInfo directory;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DirectoryInfoWrapper()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Property to indicate File Name
        /// </summary>
        public string Name
        {
            get
            {
                return directory.Name;
            }
        }

        /// <summary>
        /// Property to indicate if directory exists
        /// </summary>
        public bool Exists
        {
            get
            {
                return directory.Exists;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method to set the file directory
        /// </summary>
        /// <param name="path">Path of the directory</param>
        public void SetPath(string path)
        {
            directory = new DirectoryInfo(path);
        }

        /// <summary>
        /// Method to delete the file
        /// </summary>
        public void Delete()
        {
            directory.Delete();
        }

        /// <summary>
        /// Method to delete the directory recursively
        /// </summary>
        public void Delete(bool recursive)
        {
            directory.Delete(recursive);
        }

        /// <summary>
        /// Invoke cmd window to run rmdir command against set of directories
        /// </summary>
        /// <param name="dirList">IEnumerable<string></param>
        public void BatchDelete(IEnumerable<string> dirList)
        {
            if (dirList == null || !dirList.Any())
                return;

            var batches = dirList.Batch(10);
            foreach (var batch in batches)
            {
                var cmdArg = new StringBuilder();
                // -- Build rmdir command. /C represents run command and then terminate
                cmdArg.Append(@"/C rmdir");
                foreach (var pt in batch)
                {
                    // --Loop thru every path and build the cmd argument for del
                    // --/Q meant for quiet / silent
                    // --/F meant for read-only file delete
                    cmdArg.Append(@" /S/Q ");
                    cmdArg.Append(string.Format("\"{0}\"", pt));
                }
                Tracer.Info("rmdir command : {0}", cmdArg.ToString());
                var startInfo = new ProcessStartInfo("cmd", cmdArg.ToString())
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
            }
        }
        #endregion
    }
}
