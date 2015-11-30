using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// Backup Restore Startup  - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class BackupRestoreLogInfo : BaseWorkerProcessLogInfo
    {
        public string DocumentKey { get; set; }
    }
}
