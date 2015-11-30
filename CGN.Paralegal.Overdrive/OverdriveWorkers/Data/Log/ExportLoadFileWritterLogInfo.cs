using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Export LoadFile Writter  - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class ExportLoadFileWritterLogInfo : BaseWorkerProcessLogInfo
    {
        public string DCN { get; set; }   
        public bool IsErrorInNativeFile { get; set; }
        public bool IsErrorInImageFile { get; set; }
        public bool IsErrorInTextFile { get; set; }
    }
}
