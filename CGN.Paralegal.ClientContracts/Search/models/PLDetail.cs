using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.ClientContracts.Search
{
    public class PLDetail
    {
        public int ParaLegalID { get; set; }
        public string Name { get; set; }
        public List<Location> Locations { set; get; }
        public List<AreaOfPractise> AOPs { set; get; }
        public List<Comment> Comments { set; get; }
        public Contact PLContact { set; get; }
    }
}
