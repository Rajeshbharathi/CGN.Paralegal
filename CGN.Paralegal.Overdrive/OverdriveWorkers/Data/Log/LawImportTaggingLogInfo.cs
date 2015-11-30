using System;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class LawImportTaggingLogInfo : BaseWorkerProcessLogInfo
    {
        public override string ToString()
        {
            return string.Format("Document Id: {0} DCN:{1} CrossReferenceField:{2}", DocumentId, DCN, CrossReferenceField );
        }
        public string DocumentId { get; set; }
        public string DCN { get; set; }
    }
}
