using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Facade.UIO.DataSet
{
    /// <summary>
    /// Uniquely Identifies An Entity 
    /// </summary>
   public  interface IEntityIdentifier
    {
        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        string UniqueIdentifier { get; }


    }
}
