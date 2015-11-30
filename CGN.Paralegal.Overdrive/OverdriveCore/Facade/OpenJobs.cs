using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class OpenJobs
    {
        public OpenJobs()
        {
        }

        public List<OpenJob> Jobs { get; set; }
    }
}
