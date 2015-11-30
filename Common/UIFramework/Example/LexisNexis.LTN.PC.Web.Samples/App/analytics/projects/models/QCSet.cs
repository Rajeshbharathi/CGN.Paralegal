namespace LexisNexis.LTN.PC.Web.Samples.Models
{
    public class QCSet
    {
        public bool IsStatistical { get; set; }

        public byte ConfidenceLevel { get; set; }
        public float MarginOfError { get; set; }

        public byte Percentage { get; set; }

        public string RelevantType { get; set; }

    }
}