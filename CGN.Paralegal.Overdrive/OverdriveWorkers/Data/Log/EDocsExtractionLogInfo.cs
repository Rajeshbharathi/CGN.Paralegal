#region Header

//-----------------------------------------------------------------------------------------
// <copyright file=" EDocsExtractionLogInfo.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Nagaraju</author>
//      <description>
//          Helper Class containing methods for EDocs extraction log 
//      </description>
//      <changelog>
//          <date value="03-april-2011">created</date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------
#endregion


using System;
using System.Text;



namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// EDocsExtraction - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class EDocsExtractionLogInfo : BaseWorkerProcessLogInfo
    {
        private string m_DatasetExtractedPath = string.Empty;
        /// <summary>
        /// Dataset extraction path
        /// </summary>
        public string DatasetExtractedPath
        {
            get { return m_DatasetExtractedPath; }
            set { m_DatasetExtractedPath = value; }
        }
        public static implicit operator string(EDocsExtractionLogInfo log)
        {
            var info = new StringBuilder();
            info.Append(log.Information);
            return info.ToString();
        }
    }
}
