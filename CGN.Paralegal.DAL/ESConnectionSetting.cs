using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.DAL
{
    internal static class ESConnectionSetting
    {
        static  ESConnectionSetting()
        {
            Node = "http://10.242.206.180:9200";
            IndexName = "paralegal";
        }
        
        [System.ComponentModel.DefaultValue("Paralegals")]
        public static string IndexName { get; set; }

        [System.ComponentModel.DefaultValue("http://localhost:9200")]
        public static string Node {get;set;} 

    }

}