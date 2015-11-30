using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.Worker.Data
{
     [Serializable]
     public class DocumentCollection
    {
        /// <summary>
        /// Gets the documents.
        /// </summary>
        /// <remarks></remarks>
         public List<DocumentDetail> documents { get; set; }

         /// <summary>
         /// Gets the dataset.
         /// </summary>
         /// <remarks></remarks>
         public DatasetBEO dataset { get; set; }


         /// <summary>
         /// Gets or Sets if tags are deleted in Overlay configuration (job configuration provided by user)
         /// </summary>
         public bool IsDeleteTagsForOverlay { get; set; }

         /// <summary>
         /// Originator Id used for Search Index / Update Completion Validation
         /// </summary>
         public string Originator { get; set; }

         /// <summary>
         /// Get or Set- Include native file or not. 
         /// </summary>
         /// <remarks></remarks>
         public bool IsIncludeNativeFile { get; set; }

    }
}
