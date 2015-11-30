#region

using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;

#endregion

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    ///     ProductionPipeline
    /// </summary>
    public class ProductionPipeline : EVPipeline
    {
        /// <summary>
        ///     Gets or sets the name of the job.
        /// </summary>
        /// <value>
        ///     The name of the job.
        /// </value>
        public string JobName { get; set; }

        /// <summary>
        ///     Gets or sets the name of the job type.
        /// </summary>
        /// <value>
        ///     The name of the job type.
        /// </value>
        public string JobTypeName { get; set; }

        /// <summary>
        ///     Gets or sets the matter identifier.
        /// </summary>
        /// <value>
        ///     The matter identifier.
        /// </value>
        public long MatterId { get; set; }

        /// <summary>
        ///     Sets the pipeline type specific parameters.
        /// </summary>
        /// <param name="activeJob">The active job.</param>
        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);

            using (var stream = new StringReader(activeJob.BootParameters.ToString()))
            {
                var xmlStream = new XmlSerializer(typeof (ProductionDetailsBEO));
                var productionBeo = xmlStream.Deserialize(stream) as ProductionDetailsBEO;
                if (null != productionBeo)
                {
                    JobName = productionBeo.Profile.ProductionJobName;
                    JobTypeName = "Production Job";
                    MatterId = productionBeo.MatterId;
                }
            }
   
        }

        /// <summary>
        ///     Completeds this instance.
        /// </summary>
        /// <returns></returns>
        protected override bool Completed()
        {
            base.Completed();
          
            return true;
        }
    }
}
