#region

using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Overdrive;

#endregion

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// Class contains worker base class for search engine specific operations
    /// All workers which does  update in search engine  inherits from this class
    /// This is to avoid duplication of code in init and commit bulk indexing in every worker which does bulk indexing in search engine  
    /// </summary>

    public class SearchEngineWorkerBase : WorkerBase
    {
        /// <summary>
        /// The _pipeline shared property key
        /// </summary>
        private readonly string _pipelineSharedPropertyKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchEngineWorkerBase"/> class.
        /// </summary>
        public SearchEngineWorkerBase()
        {
            _pipelineSharedPropertyKey = string.Format("CommitIndexStatus{0}", PipelineId);
        }


        /// <summary>
        /// Sets the commiy index status to initialized.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        protected void SetCommiyIndexStatusToInitialized(long matterId)
        {
            var pipelineProperty = GetPipelineSharedProperty(_pipelineSharedPropertyKey);
            lock (pipelineProperty)
            {
                if (pipelineProperty.Value == null)
                {
                    IndexWrapper.InitBulkIndex(matterId);
                    pipelineProperty.Value = "Initialized";
                }
            }
        }

        /// <summary>
        /// Sets the commit index status to completed.
        /// </summary>
        /// <param name="matterId">The matter identifier.</param>
        protected void SetCommitIndexStatusToCompleted(long matterId)
        {
            var pipelineProperty = GetPipelineSharedProperty(_pipelineSharedPropertyKey);
            lock (pipelineProperty)
            {
                if (pipelineProperty.Value != null && pipelineProperty.Value.ToString().Equals("Initialized"))
                {
                    IndexWrapper.CommitBulkIndex(matterId);
                    pipelineProperty.Value = "Completed";
                }
            }
        }
    }
}
