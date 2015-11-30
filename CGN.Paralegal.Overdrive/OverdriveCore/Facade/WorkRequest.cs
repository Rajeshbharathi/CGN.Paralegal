using System;
using System.Collections.Generic;

namespace LexisNexis.Evolution.Overdrive
{
    using LexisNexis.Evolution.BusinessEntities;

    /// <summary>
    /// This is the implementation for a work request.
    /// </summary>
    [Serializable]
    public class WorkRequest
    {
        #region Constructors
        public WorkRequest()
        { }

        public WorkRequest(
            string sectionName,
            DataPipeName sourceDataPipeName,
            HiringPipeName hiringPipeName,
            DataPipeName logDataPipeName,
            ReportPipeName reportPipeName,
            RoleType roleType,
            PipelineType pipelineType,
            string pipelineId,
            string bootParameters)
        {
            SectionName = sectionName;
            InputDataPipeName = sourceDataPipeName;
            HiringPipeName = hiringPipeName;
            LogDataPipeName = logDataPipeName;
            ReportPipeName = reportPipeName;
            PipelineType = pipelineType;
            PipelineId = pipelineId;
            RoleType = roleType;
            BootParameters = bootParameters;
        }
        #endregion
        #region Properties

        public string SectionName { get; set; }

        /// <summary>
        /// Gets or sets the input data pipe name.
        /// </summary>
        /// <value>The input data pipe name.</value>
        public DataPipeName InputDataPipeName { get; set; }
        /// <summary>
        /// Gets or sets the hiring pipe name.
        /// </summary>
        /// <value>The report pipe name.</value>
        public HiringPipeName HiringPipeName { get; set; }
        public List<OutputSection> OutputSections { get; set; }
        /// <summary>
        /// Gets or sets the log data pipe name.
        /// </summary>
        /// <value>The log data pipe name.</value>
        public DataPipeName LogDataPipeName { get; set; }
        /// <summary>
        /// Gets or sets the report pipe name.
        /// </summary>
        /// <value>The report pipe name.</value>
        public ReportPipeName ReportPipeName { get; set; }
        /// <summary>
        /// Gets or sets the role type.
        /// </summary>
        /// <value>The role type.</value>
        public RoleType RoleType { get; set; }

        public WorkerIsolationLevel WorkerIsolationLevel { get; set; }

        /// <summary>
        /// Gets or sets the pipe line type.
        /// </summary>
        /// <value>The pipeline type.</value>
        public PipelineType PipelineType { get; set; }
        /// <summary>
        /// Gets or sets the pipeline id.
        /// </summary>
        /// <value>The pipeline id.</value>
        public string PipelineId { get; set; }
        /// <summary>
        /// Gets or sets the boot parameters.
        /// </summary>
        public string BootParameters { get; set; }

        public BaseJobBEO JobParameters { get; set; }

        #endregion

        [Serializable]
        public class OutputSection
        {
            public OutputSection(string name, DataPipeName dataPipeName)
            {
                Name = name;
                DataPipeName = dataPipeName;
            }

            public string Name { get; private set; }
            /// <summary>
            /// Gets or sets the output data pipe name.
            /// </summary>
            /// <value>The output data pipe name.</value>
            public DataPipeName DataPipeName { get; private set; }
        }
    }

    public enum WorkerIsolationLevel
    {
        Default = 0,
        SeparateThread,
        SeparateAppDomain,
        SeparateProcess
    }
}
