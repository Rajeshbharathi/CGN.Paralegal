using System;
using LexisNexis.Evolution.Overdrive.MSMQ;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public abstract class PipeName
    {
        public string MachineName { get; set; }

        public bool Exists
        {
            get
            {
                return LexisNexis.Evolution.Overdrive.MSMQ.MSMQPipeNameExtensions.GetExists(this);
            }
        }

        public uint Count
        {
            get
            {
                return LexisNexis.Evolution.Overdrive.MSMQ.MSMQPipeNameExtensions.Count(this);
            }
        }

        public PipeName()
        {
        }

        protected PipeName(string machineName)
        {
            MachineName = machineName;
        }

        protected const char Delimiter = '.';
        internal abstract string LongName
        {
            get;
        }
    }

    // Here Pipeline publishes open slots for workers
    [Serializable]
    public class HiringPipeName : PipeName
    {
        public string PipelineId { get; private set; } // unique within pipelineType
        public string SectionName { get; private set; } // Examples: "Vault", "Indexing", "Conversion", etc.
        public HiringPipeName()
        {
        }

        public HiringPipeName(string machineName, string pipelineId, string sectionName)
            : base(machineName)
        {
            PipelineId = pipelineId;
            SectionName = sectionName;
        }

        internal override string LongName
        {
            get
            {
                return SectionName + Delimiter + PipelineId;
            }
        }
    }

    [Serializable]
    public class ReportPipeName : PipeName
    {
        public PipelineType PipelineType { get; private set; } // Examples: "LoadFileImport", "EDLoaderImport", "DCBImport", etc.
        public string PipelineId { get; private set; } // unique within pipelineType
        public ReportPipeName()
        {
        }

        public ReportPipeName(string machineName, PipelineType pipelineType, string pipelineId)
            : base(machineName)
        {
            PipelineType = pipelineType;
            PipelineId = pipelineId;
        }

        internal override string LongName
        {
            get
            {
                return PipelineType.ToString() + Delimiter + PipelineId;
            }
        }
    }

    [Serializable]
    public class DataPipeName : PipeName
    {
        public PipelineType PipelineType { get; private set; }
        // Examples: "LoadFileImport", "EDLoaderImport", "DCBImport", etc.
        public string PipelineId { get; private set; } // unique within pipelineType
        public string SectionName { get; private set; } // Examples: "Vault", "Indexing", "Conversion", etc.

        public DataPipeName()
        {
        }

        public DataPipeName(string machineName, PipelineType pipelineType, string pipelineId, string sectionName)
            : base(machineName)
        {
            PipelineType = pipelineType;
            PipelineId = pipelineId;
            SectionName = sectionName;
        }

        internal override string LongName
        {
            get
            {
                return PipelineType.ToString() + Delimiter + PipelineId + Delimiter + SectionName;
            }
        }
    }
}
