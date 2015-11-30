#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="OutlookMessageExtractor.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Keerti/Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  OutlookMessageExtractor
//      </description>
//      <changelog>
//           <date value="01/17/2012">Bugs Fixed #95197-Made a fix for email redact issue by using overdrive non-linear pipeline </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces
using System;
using System.IO;
using System.Linq;
using Independentsoft.Pst;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.TraceServices;
#endregion

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Encapsulates Outlook message extraction functionality
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class OutlookMessageExtractor : IDisposable
    {
        public class ErrorCodes
        {
            public const string NoMessageForGivenEntryId = "NoMessageForGivenEntryId";
            public const string EmptyMsgFileGenerationFaliure = "UnableToGenerateEmptyMsgFile";
        }

        // Working pstFile isntance, we try to retain this accross extraction requests
        private PstFile WorkingPst { get; set; }
        private string WorkingPstFilePath { get; set; }

        /// <summary> Extract message designated by entry id from specified pst to designated msg file</summary>
        /// <param name="pstFilePath">The pst file from which to extract the message</param>
        /// <param name="entryId">The entryId of the message to extract</param>
        /// <param name="outputMsgFile">Output location of the extracted msg file</param>
        public void ExtractMessage(string pstFilePath, string entryId, string outputMsgFile)
        {
            pstFilePath.ShouldNotBeEmpty();
            SetWorkingPst(pstFilePath);
            if (!string.IsNullOrEmpty(entryId))
            {
                Item i = WorkingPst.GetItem(EntryIdToByteArray(entryId));
                if (i == null)
                {
                    throw new EVException().AddDbgMsg("Error occured attempting to create native email message for Outlook: Entryid ({0}) In ({1}) was not found.", entryId, pstFilePath);
                }

                outputMsgFile.ShouldNotBeEmpty();
                i.Save(outputMsgFile, true);
                if (!File.Exists(outputMsgFile))//create empty file in case msg file generation fails
                {
                    CreateEmptyMsgFile(outputMsgFile);
                }
            }
            else
            {
                CreateEmptyMsgFile(outputMsgFile);
            }
        }

        /// <summary>
        /// Creates the empty MSG file.
        /// </summary>
        /// <param name="msgFilePath">The MSG file path.</param>
        private static void CreateEmptyMsgFile(string msgFilePath)
        {
            try
            {
                File.WriteAllText(msgFilePath, String.Empty);
            }
            catch (Exception exception)
            {
                exception.AddResMsg(ErrorCodes.EmptyMsgFileGenerationFaliure);
                throw;
            }
        }
        /// <summary>
        /// Sets the local WorkingPst instance to pstFilepath. 
        /// If another pst is opened then the previous pst is closed
        /// </summary>
        private void SetWorkingPst(string pstFilePath)
        {
            if (string.Compare(pstFilePath, WorkingPstFilePath ?? string.Empty, true) != 0)
            {
                CloseWorkingPst();
                WorkingPst = new PstFile(pstFilePath);
                WorkingPstFilePath = pstFilePath;
            }
        }

        /// <summary> Closes the workingPstInstance </summary>
        private void CloseWorkingPst()
        {
            if (WorkingPst != null)
            {
                WorkingPst.Close();
                WorkingPst = null;
            }
            WorkingPstFilePath = string.Empty;
        }

        /// <summary> Converts entryId (hex string) to byte array </summary>
        private byte[] EntryIdToByteArray(string entryId)
        {
            return Enumerable.Range(0, entryId.Length).
                      Where(x => 0 == x % 2).
                      Select(x => Convert.ToByte(entryId.Substring(x, 2), 16)).
                      ToArray();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false; // to detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CloseWorkingPst();
                }

                disposed = true;
            }
        }
    }
}
