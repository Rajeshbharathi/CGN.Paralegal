using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class DcbSlice
    {
        public int FirstDocument { get; set; }
        public int NumberOfDocuments { get; set; }

        public DcbCredentials DcbCredentials { get; set; }
        public string ImageSetId { get; set; }
    }

    [Serializable]
    public class DcbCredentials
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
