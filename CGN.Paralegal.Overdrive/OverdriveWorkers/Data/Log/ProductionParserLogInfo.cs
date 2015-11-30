using System;
using System.Text;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// LoadFile Record parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class ProductionParserLogInfo : BaseWorkerProcessLogInfo
    {
        public string DCN { get; set; }
        public string BatesNumber { get; set; }
        public string ProductionDocumentNumber { get; set; }
        public string DatasetName { get; set; }
        public string ProductionName { get; set; }

        public static implicit operator string(ProductionParserLogInfo log)
        {
            var info = new StringBuilder();
            info.Append("DCN: " + log.DCN + " \n, ");
            info.Append("Bates Number: " + log.BatesNumber + " \n, ");
            info.Append("Production Document Number: " + log.ProductionDocumentNumber + " \n, ");
            info.Append("Dataset Name: " + log.DatasetName + " \n, ");
            info.Append("Production Name: " + log.ProductionName + " \n, ");
            return info.ToString();
        }
    }
}
