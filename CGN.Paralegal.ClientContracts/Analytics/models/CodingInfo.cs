# region File Header
# endregion

namespace CGN.Paralegal.ClientContracts.Analytics
{
    /// <summary>
    /// Coding Info
    /// </summary>
    public class CodingInfo
    {
        public bool IsCoded { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Coding Value (Relevant/Not Relevant)
    /// </summary>
    public enum CodingValue
    {
        Relevant=1,
        NotRelevant=2
    }
}