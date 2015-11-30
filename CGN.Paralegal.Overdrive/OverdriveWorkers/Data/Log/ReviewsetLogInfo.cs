using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ReviewsetLogInfo : BaseWorkerProcessLogInfo
    {
        public string ReviewsetID { get; set; }
        public string ReviewsetName { get; set; }

        public static implicit operator string(ReviewsetLogInfo log)
        {
            var info = new StringBuilder();
            info.Append("Added Review set : " + log.ReviewsetName + " \n, ");
            return info.ToString();
        }
    }
}
