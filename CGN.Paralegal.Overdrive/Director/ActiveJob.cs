using System;
using System.Diagnostics;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.Overdrive
{
    internal class ActiveJob
    {
        internal ActiveJob()
        {
            Status = Director.JobStatus.Loaded;
            CurrentPipelineState = PipelineState.Empty;
            PreviousPipelineState = PipelineState.Empty;
        }

        public PipelineState CurrentPipelineState { get; set; }
        public PipelineState PreviousPipelineState { get; set; }

        public JobInfo JobInfo { get; set; }

        public int JobId
        {
            get
            {
                Debug.Assert(null != JobInfo);
                return JobInfo.JobId;
            }
        }

        public int PipelineId
        {
            get
            {
                Debug.Assert(null != JobInfo);
                return Convert.ToInt32(JobInfo.PipelineId);
            }
        }

        public int JobTypeId
        {
            get
            {
                Debug.Assert(null != JobInfo);
                return JobInfo.JobTypeId;
            }
        }

        public string UserGuid
        {
            get
            {
                Debug.Assert(null != JobInfo);
                return JobInfo.ScheduleCreatedBy;
            }
        }

        public string JobName { get; private set; }
        public string JobTypeName { get; private set; }
        public long MatterId { get; set; }

        public object BootParameters
        {
            get
            {
                Debug.Assert(null != Beo);
                return Beo.BootParameters;
            }
        }

        public bool IsFirstWorkerComplete
        {
            get
            {
                return CurrentPipelineState == PipelineState.FirstWorkerCompleted;
            }
        }

        public bool HaveAllWorkersQuit
        {
            get
            {
                return CurrentPipelineState == PipelineState.AllWorkersQuit;
            }
        }

        public JobBusinessEntity BusinessEntity { get; set; }
        public BaseJobBEO Beo { get; set; }
        public EVPipeline EVPipeline { get; set; }
        public double ProgressPercentage { get; set; }
        public Director.JobStatus Status { get; set; }
        public long TotalNumberOfDocumentsToProcess { get; set; }
        public long NumberOfDocumentsProcessed { get; set; }
    }
}