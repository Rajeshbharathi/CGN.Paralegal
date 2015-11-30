# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="RedactItHeartbeatWatcher.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This is a file that contains wait logic for the Redact-IT conversion
//      </description>
//      <changelog>
//          <date value="09/24/2014">Bug fix 175866</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion

using System;
using System.IO;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Worker
{
    using System.Collections.Specialized;

    using Infrastructure;

    public class RedactItHeartbeatWatcher
    {
        public static DocumentStatus CheckDocumentState(string docHbFilename, int expectedRepsonseLines)
        {
            if (String.IsNullOrEmpty(docHbFilename))
            {
                Tracer.Error("RedactItHeartbeatWatcher received empty path to heartbeat file ");
                return new DocumentStatus(DocumentStateEnum.Failure, "Internal error", "CheckDocumentState was called with null or empty docHBFilename");
            }

            FileStream hbFileStream;
            try
            {
                hbFileStream = new FileStream(docHbFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                return new DocumentStatus(DocumentStateEnum.NotFound);
            }
            try
            {
                using (StreamReader hbStreamReader = new StreamReader(hbFileStream))
                {
                    NameValueCollection responses = new NameValueCollection();
                    int responseCounter = 0;
                    string line;
                    while ((line = hbStreamReader.ReadLine()) != null)
                    {
                        LineInfo lineInfo = ParseLine(line);
                        if (null == lineInfo)
                        {
                            Tracer.Debug(string.Format("Could not parse heart beat file {0} line: {1}", docHbFilename, line));
                            continue; //If the line is invalid skip it and continue the parsing.
                        }

                        if (lineInfo.Info.Contains("notification.response"))
                        {
                            responseCounter++;
                        }

                        int equalSignPos = lineInfo.Info.IndexOf('=');
                        if (-1 != equalSignPos)
                        {
                            string name = lineInfo.Info.Substring(0, equalSignPos);
                            string value = (equalSignPos + 1 < lineInfo.Info.Length)
                                               ? lineInfo.Info.Substring(equalSignPos + 1)
                                               : String.Empty;
                            responses.Set(name, value);
                        }
                    }

                    if (responses["error"] != null || responses["warning"] != null)
                    {
                        string reason = Utils.GetRedactItErrorReason(responses);
                        string message = Utils.GetRedactItErrorMessage(responses);
                        return new DocumentStatus(DocumentStateEnum.Failure, reason, message);
                    }

                    //responseCounter could be greater than expected if for some reason the heartbeat file was not deleted for the last run
                    //and new run would append "repsonse" to the heartbeat. 
                    if (expectedRepsonseLines <= responseCounter)
                        return new DocumentStatus(DocumentStateEnum.Success);

                }
            }
            catch(Exception ex)
            {
                ex.AddUsrMsg("Problem in parsing heart beat file {0}", docHbFilename);
                ex.Trace().Swallow();
            }
            // Full file was scanned, but there were no error and no two response notifications
            return new DocumentStatus(DocumentStateEnum.NotReady);
        }

        public enum DocumentStateEnum
        {
            NotFound,
            NotReady,
            Success,
            Failure
        }

        public class DocumentStatus
        {
            public DocumentStatus(DocumentStateEnum documentState)
            {
                DocumentState = documentState;
            }

            public DocumentStatus(DocumentStateEnum documentState, string errorReason, string errorMessage)
                : this(documentState)
            {
                ErrorReason = errorReason;
                ErrorMessage = errorMessage;
            }

            public DocumentStateEnum DocumentState { get; set; }
            public string ErrorReason { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class LineInfo
        {
            public DateTime TimeStamp;
            public string Info;
        }

        private static LineInfo ParseLine(string line)
        {
            int index1, index2;
            index1 = line.IndexOf('[');
            if (-1 == index1) return null;
            index1++;
            index2 = line.IndexOf(']', index1);
            if (-1 == index2) return null;
            string strTimeStamp = line.Substring(index1, index2 - index1);
            DateTime timeStamp;
            if (!DateTime.TryParse(strTimeStamp, out timeStamp)) return null;
            index2++;

            index1 = line.IndexOf('[', index2);
            if (-1 == index1) return null;
            index1++;
            index2 = line.IndexOf(']', index1);
            if (-1 == index2) return null;
            index2++;
            string info = line.Substring(index2).Trim();

            var lineInfo = new LineInfo() { TimeStamp = timeStamp, Info = info };
            return lineInfo;
        }
    }
}
