#region

using System;
using System.Globalization;
using System.Linq;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.BusinessEntities.Common;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

#endregion

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    /// IncludeDocumentsPipeline
    /// </summary>
    public class IncludeDocumentsPipeline : EVPipeline
    {
     
        private string _jobBootParameter;

        /// <summary>
        ///     Sets the pipeline type specific parameters.
        /// </summary>
        /// <param name="activeJob">The active job.</param>
        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);
            _jobBootParameter = activeJob.BootParameters.ToString();
            
        }


        /// <summary>
        /// Completed this instance.
        /// </summary>
        /// <returns></returns>
        protected override bool Completed()
        {
            base.Completed();
            return AnalyticsJob.IncludeDocumentsPipelineComplete(JobId, _jobBootParameter);
        }
    }
}
