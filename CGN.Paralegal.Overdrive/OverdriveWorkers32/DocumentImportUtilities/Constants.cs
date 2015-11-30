# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="Constants.cs">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          This is a file that contains Constants used in DocumentImportUtilities helper class
//      </description>
//      <changelog>
//          <date value="19-August-2010"></date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion

#region Namespace

#endregion

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    public static class Constants
    {
        #region private Constructor

        #endregion

        /// <summary>
        /// Represents EDRM attribute value "native"
        /// </summary>
        internal const string EDRMAttributeNativeFileType = "native";

        /// <summary>
        /// Represents EDRM attribute value text
        /// </summary>
        internal const string EDRMAttributeTextFileType = "text";

        internal const double KBConversionConstant = 1024.0;

        /// <summary>
        /// Represents MS Outlook e-mail file Extension
        /// </summary>
        internal const string EVCorlibEMailMessageExtension = @".htm";
        internal const string CNEvEmailMessageExtension = @".msg";
        internal const string DiskFullErrorMessage = "There is not enough space on the disk";

        #region Audit Log related constants

        /// <summary>
        /// Tag name for extraction failure error
        /// </summary>
        internal const string ExtractionErrorTagName = "^ArcEx";

        /// <summary>
        /// Extraction error for password protected file
        /// </summary>
        public const string ExtractionFailString = "(encrypted) unable to extract file due to encryption";

        /// <summary>
        /// Tag name for file 
        /// </summary>
        internal const string FileNameTag = "#FileName";

        #endregion

        #region Load File
        
        public const string NativeFileType = "Native";

        #endregion
    }
}
