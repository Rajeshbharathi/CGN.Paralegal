using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.ServiceContracts.Search
{
    public class PLDetail
    {
        public string ParalegalId { set; get; }
        public string Name { set; get; }
        public List<Location> Locations { set; get; }
        public List<AreaOfPractise> AOPs { set; get; }
        public List<Comment> Comments { set; get; }
        public Contact PLContact { set; get; }
    }
}
