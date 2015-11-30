using System;
using System.Text;
namespace LexisNexis.Evolution.Worker.Data
{
    /// <summary>
    /// LoadFile Record parser - worker Processor Log Information
    /// </summary>
    [Serializable]
    public class LoadFileParserLogInfo : BaseWorkerProcessLogInfo
    {
        public static implicit operator string(LoadFileParserLogInfo log)
        {
            var _stringRepresentation = new StringBuilder();
            _stringRepresentation.Append("Status :" + log.Information);
            return _stringRepresentation.ToString();
        }
    }
}
