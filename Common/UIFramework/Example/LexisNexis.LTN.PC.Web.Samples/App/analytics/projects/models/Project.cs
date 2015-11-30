using System;

namespace LexisNexis.LTN.PC.Web.Samples.Models
{
    public class Project {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string MatterName { get; set; }
        public string Source { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Documents { get; set; }
    }
}