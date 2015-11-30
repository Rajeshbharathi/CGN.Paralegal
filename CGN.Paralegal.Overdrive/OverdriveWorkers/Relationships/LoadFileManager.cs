using System;

using LexisNexis.Evolution.DataAccess.JobManagement;

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    /// <summary>
    /// Load File (CSV/.TXT ...) manager
    /// </summary>
    public class LoadFileManager
    {
        #region Properties

        /// <summary>
        /// Load File location
        /// </summary>
        public Uri LoadFileUri { get; set; }


        /// <summary>
        ///Column delimiter
        /// </summary>      
        public char ColumnDelimiter { get; set; }


        /// <summary>
        /// Quote character
        /// </summary>     
        public char QuoteCharacter { get; set; }


        /// <summary>
        /// New line delimiter
        /// </summary>    
        public char NewlineDelimiter { get; set; }


        /// <summary>
        ///  Encoding Type
        /// </summary>    
        public string EncodingType { get; set; }


        /// <summary>
        ///  Date Format
        /// </summary>    
        public string DateFormat { get; set; }

        /// <summary>
        ///Load-File header option.
        /// </summary>      
        public bool IsFirstLineHeader { get; set; }

        private string _threadingString;


        /// <summary>
        /// Gets or sets the threading string. It's used to calculate Threading Constraint.
        /// It's not used directly in calculating document relationships. Threading Constraint is used.
        /// </summary>
        /// <value>
        /// The threading string.
        /// </value>
        public string ThreadingString
        {
            get { return _threadingString; }
            set { _threadingString = value; }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadFileManager"/> class.
        /// </summary>
        public LoadFileManager() { }

        /// <summary>
        ///  Initialize load-file delimiter information
        /// </summary>
        /// <param name="loadFileUri">Location </param>
        /// <param name="columnDelimiter">Column delimiter</param>
        /// <param name="quoteCharacter">Quote Character</param>
        /// <param name="newlineDelimiter">New line delimiter</param>
        /// <param name="encodingType">Encoding type</param>
        /// <param name="dateFormat">Date format</param>
        /// <param name="isFirstLineHeader">Has header</param>
        /// <param name="threadingString"> </param>
        public LoadFileManager(Uri loadFileUri, char columnDelimiter, char quoteCharacter, char newlineDelimiter, string encodingType, string dateFormat, bool isFirstLineHeader, string threadingString)
        {
            //Set value
            LoadFileUri = loadFileUri;
            ColumnDelimiter = columnDelimiter;
            QuoteCharacter = quoteCharacter;
            NewlineDelimiter = newlineDelimiter;
            EncodingType = encodingType;
            DateFormat = dateFormat;
            IsFirstLineHeader = isFirstLineHeader;
            ThreadingString = threadingString;
        }
    }
}
