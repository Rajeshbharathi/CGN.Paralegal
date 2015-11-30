using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker
{
    [Serializable]
    public class ExportOption
    {
        public bool IsNative { get; set; }
        public string IncludeNativeTagName { get; set; }
        public bool IsImage { get; set; }
        public bool IsText { get; set; }
        //public bool RecreateFamilyGroup { get; set; }
        public string ImageSetCollectionId { get; set; }
        public bool IsProduction { get; set; }
        public string ProductionSetCollectionId { get; set; }
        public bool IsTag { get; set; }
        public List<string> TagList { get; set; }
        public bool IsComments { get; set; }
        public bool IsField { get; set; }
        public string ExportDestinationFolderPath { get; set; }
        //Load File Specific
        public string LoadFilePath { get; set; }
        public string LoadFileImageHelperFilePath { get; set; }
        public string LoadFileTextHelperFilePath { get; set; }
        public string TextOption1 { get; set; }
        public string TextOption2 { get; set; }
        /// <summary>
        /// Gets or sets the name of the field for native file.
        /// </summary>
        /// <value>
        /// The name of the field for native file.
        /// </value>
        public string FieldForNativeFileName
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the name of the field for text file.
        /// </summary>
        /// <value>
        /// The name of the field for text file.
        /// </value>
        public string FieldForTextFileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text field to export.
        /// </summary>
        /// <value>
        /// The text field to export.
        /// </value>
        public string TextFieldToExport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [is text field to export selected].
        /// </summary>
        /// <value>
        /// <c>true</c> if [is text field to export selected]; otherwise, <c>false</c>.
        /// </value>
        public bool IsTextFieldToExportSelected
        {
            get;
            set;
        }

    }
}
