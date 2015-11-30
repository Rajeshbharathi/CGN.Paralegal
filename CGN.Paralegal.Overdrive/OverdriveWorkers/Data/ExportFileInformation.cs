using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker
{
    [Serializable]
    public class ExportFileInformation
    {
        public string SourceFilePath { get; set; }
        public string DestinationFolder { get; set; }
        public bool IsScrubbedText { get; set; }
        public string ContentToExport { get; set; }
        public bool IsTextFieldExportEnabled { get; set; }
        
    }
}
