using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LexisNexis.Evolution.BusinessEntities;
namespace LexisNexis.Evolution.Worker
{
    [Serializable]
    public class SearchContext
    {
        public RVWSearchBEO SearchObject { get; set; }
    }
}
