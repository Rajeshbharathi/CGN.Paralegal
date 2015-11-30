using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{

    public class NearDuplicationPipeline : EVPipeline
    {

        public string JobName;
        public string JobTypeName;
        private NearDuplicationJobBEO _nearDuplicationBootParameter;
        private const string AuEventJobName = "Job Name";
        private const string AuEventJobtype = "Job Type";
      

        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);

            using (var stream = new StringReader(activeJob.BootParameters.ToString()))
            {
                var xmlStream = new XmlSerializer(typeof(NearDuplicationJobBEO));
                _nearDuplicationBootParameter = xmlStream.Deserialize(stream) as NearDuplicationJobBEO;
                if (_nearDuplicationBootParameter == null) return;
                JobName = _nearDuplicationBootParameter.JobName;
                JobTypeName = "Near Duplication";
            }
        }

       

    }
}
