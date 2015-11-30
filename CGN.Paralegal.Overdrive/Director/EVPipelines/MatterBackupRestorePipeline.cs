using System;

using LexisNexis.Evolution.BusinessEntities;
using System.Xml.Serialization;
using System.IO;

namespace LexisNexis.Evolution.Overdrive
{
    public class MatterBackupRestorePipeline : EVPipeline
    {
        public string RestoreCollectionName { get; set; }
        public long MatterId { get; set; }
     
        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);

            using (var stream = new StringReader(activeJob.BootParameters.ToString()))
            {
                var xmlStream = new XmlSerializer(typeof(BackupRestoreProfileBEO));
                BackupRestoreProfileBEO backupProfileBEO = xmlStream.Deserialize(stream) as BackupRestoreProfileBEO;
                if (null != backupProfileBEO)
                {
                    MatterId = Convert.ToInt64(backupProfileBEO.MatterId);
                    //RestoreCollectionName = reIndex.MatterDetails.RestoreCollectionName;                
                }
            }
        }

    }
}
