using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    using System.Globalization;

    public class DirectorCoreServices : IDirectorCoreServices
    {
        private const string UnknownMachine = "Unknown Machine";
        /// <summary>
        /// Get the jobs.
        /// </summary>
        /// <param name="machineName">Calling machine name.</param>
        /// <returns></returns>
        public List<JobInfo> GetJobs(string machineName)
        {
            try
            {
                return Director.Instance.ActiveJobList
                    .Where(j => j.PreviousPipelineState < PipelineState.Completed)
                    .Select(j => j.JobInfo).ToList();
            }
            catch
            {
                Tracer.Error("Unable to get jobs.");
                // Caller will handle this gracefully.
                throw;
            }
        }

        /// <summary>
        /// Get open jobs.
        /// </summary>
        /// <param name="machineName">Calling machine name.</param>
        /// <returns></returns>
        public OpenJobs GetOpenJobs(string machineName)
        {
            var caller = machineName;
            var openJobs = new OpenJobs { Jobs = new List<OpenJob>() };
            try
            {
                if (string.IsNullOrEmpty(machineName))
                    caller = UnknownMachine;
                foreach (var openJob in GetJobs(caller).Select(jobInfo => new OpenJob { PipelineId = jobInfo.PipelineId, Command = jobInfo.Command }))
                {
                    openJobs.Jobs.Add(openJob);
                }
            }
            catch
            {
                Tracer.Error("Unable to send open jobs to worker manager running in machine: {0}", caller);
            }
            return openJobs;
        }

        /// <summary>
        /// Get job info.
        /// </summary>
        /// <param name="pipelineId">Pipeline Id.</param>
        /// <param name="machineName">Calling machine name.</param>
        /// <returns></returns>
        public JobInfo GetJobInfo(string pipelineId, string machineName)
        {
            var caller = machineName;
            try
            {
                if (string.IsNullOrEmpty(machineName))
                    caller = UnknownMachine;
                var job = Director.Instance.ActiveJobList.FirstOrDefault(j => j.PipelineId.ToString(CultureInfo.InvariantCulture) == pipelineId);
                if (null != job && null != job.JobInfo) return job.JobInfo;
            }
            catch
            {
                Tracer.Error("Unable to send job info to worker manager running in machine: {0}", caller);
            }
            return null;
        }

        /// <summary>
        /// Get worker statistics
        /// </summary>
        /// <param name="pipelineId">Pipeline Id.</param>
        /// <param name="machineName">Calling machine name.</param>
        /// <returns></returns>
        public List<WorkerStatistics> GetWorkerStatistics(string pipelineId, string machineName)
        {
            var workerStatisticsList = new List<WorkerStatistics>();
            var caller = machineName;
            try
            {
                if (string.IsNullOrEmpty(machineName))
                    caller = UnknownMachine;
                var job = Director.Instance.ActiveJobList.FirstOrDefault(p => p.PipelineId.ToString(CultureInfo.InvariantCulture) == pipelineId);
                if (null != job && null != job.EVPipeline)
                {
                    var pipelineSections = job.EVPipeline.PipelineSections;
                    workerStatisticsList.AddRange(from pipelineBlock in pipelineSections from workerStatus in pipelineBlock.WorkerStatuses select workerStatus.Value.WorkerStatistics);
                }
            }
            catch
            {
                Tracer.Error("Unable to send worker statistics to request from machine: {0}", caller);
            }
            return workerStatisticsList;
        }
    }
}