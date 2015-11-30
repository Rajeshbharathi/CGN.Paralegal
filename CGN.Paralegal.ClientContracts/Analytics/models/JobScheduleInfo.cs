using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.ClientContracts.Analytics
{
    /// <summary>
    /// Job schedule Info
    /// </summary>
    public class JobScheduleInfo
    {
        /// <summary>
        /// Indicates Job is schedule for future date or not
        /// </summary>
        public bool IsSchedule { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
        public string StartTime { get; set; }
    }
}
