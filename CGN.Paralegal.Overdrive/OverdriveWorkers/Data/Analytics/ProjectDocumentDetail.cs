using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverdriveWorkers.Data
{
    [Serializable]
    public class  ProjectDocumentDetail
    {
        /// <summary>
        /// Gets or sets the document reference Id.
        /// </summary>
        public string DocumentReferenceId { get; set; }


        /// <summary>
        /// Gets or sets the Text file path.
        /// </summary>
        public string TextFilePath { get; set; }


        /// <summary>
        /// Get or set document predictedCategory
        /// </summary>
        public string PredictedCategory { get; set; }

        /// <summary>
        /// Gets or sets the document score.
        /// </summary>
        /// <value>
        /// The document score.
        /// </value>
        public double DocumentScore { get; set; }

        /// <summary>
        /// Get or set document need to be updated or not
        /// </summary>
        public bool IsDocumentUpdate { get; set; }

        /// <summary>
        /// Get or set document Index status
        /// </summary>
        public bool DocumentIndexStatus { get; set; }

        /// <summary>
        /// Get or set document Text size
        /// </summary>
        public int DocumentTextSize { get; set; }


        /// <summary>
        /// Get or Set Document id
        /// </summary>
        public long DocId { get; set; }
    }

    [Serializable]
    public class ProjectDocumentCollection
    {
        public List<ProjectDocumentDetail> Documents { get; set; }

        public int ProjectFieldId { get; set; }
    }
}
