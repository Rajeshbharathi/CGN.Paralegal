using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LexisNexis.LTN.PC.Web.Samples.models
{
    public class Review
    {
        public string totaldocs { get; set; }
        public string currentdoc { get; set; }
        public string DCN { get; set; }
        public string content { get; set; }

        public Fields fields { get; set; }
        public List<string> excerpts { get; set; }
        public List<Comments> comments { get; set; }

        public string relevant { get; set; }
        public string example { get; set; }
        public string totalhits { get; set; }
        public string starthit { get; set; }
        public string endhit { get; set; }
        public string totalpagecount { get; set; }
        public string currentpage { get; set; }
    }

    public class Fields
    {
        public string parentId { get; set; }
        public string fileName { get; set; }
        public string docId { get; set; }
        public string attachment { get; set; }
        public string baseMsg { get; set; }
        public string edFolder { get; set; }
        public string author { get; set; }
        public string categories { get; set; }
        public string threadId { get; set; }
        public string subject { get; set; }
    }
    public class Comments
    {
        public string id { get; set; }
        public string text { get; set; }
        public string userId { get; set; }
        public string isDeletable { get; set; }
    }
}
