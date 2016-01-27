using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.BusinessEntities.Search
{
    public class Comment
    {
        public string CommentId { set; get; }
        public string CommentText { set; get; }
        public string CreatedBy { set; get; }
        public string CreatedDate { set; get; }
        public string LastUpdatedBy { set; get; }
        public string LastUpdatedDate { set; get; }
    }
}
