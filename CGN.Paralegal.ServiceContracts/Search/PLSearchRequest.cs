using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.ServiceContracts.Search
{

    public class PLSearchRequest
    {
        public SearchType Type { get; set; }

        public string SearchQuery { get; set; }

        
    }
}
