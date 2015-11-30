using System.Collections.Generic;
using System;
using LexisNexis.Evolution.BusinessEntities;
namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class LoadFileRecordCollection
    {
        /// <summary>
        /// Gets Records.
        /// </summary>
        /// <remarks></remarks>
        public List<LoadFileRecord> Records { get; set; }

        /// <summary>
        /// Gets the dataset.
        /// </summary>
        /// <remarks></remarks>
        public DatasetBEO dataset { get; set; }

        /// <summary>
        /// Unique identifier associated with a load file. 
        /// </summary>
        public string UniqueThreadString { get; set; } 
    }
}
