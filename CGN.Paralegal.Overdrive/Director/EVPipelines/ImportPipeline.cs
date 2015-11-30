using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.Document;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;


namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    ///     ImportPipeline
    /// </summary>
    public class ImportPipeline : EVPipeline
    {
        /// <summary>
        ///     The _matter identifier
        /// </summary>
        private long _matterId;

        /// <summary>
        /// The families and threads calculations initiated
        /// </summary>
        protected bool FamiliesAndThreadsCalculationsInitiated;
        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        /// <value>
        /// The name of the job.
        /// </value>
        protected string JobName { get; set; }
        /// <summary>
        /// Gets or sets the name of the job type.
        /// </summary>
        /// <value>
        /// The name of the job type.
        /// </value>
        protected string JobTypeName { get; set; }
        /// <summary>
        /// Gets or sets the source path.
        /// </summary>
        /// <value>
        /// The source path.
        /// </value>
        protected string SourcePath { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [threads linking requested].
        /// </summary>
        /// <value>
        /// <c>true</c> if [threads linking requested]; otherwise, <c>false</c>.
        /// </value>
        protected bool ThreadsLinkingRequested { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [families linking requested].
        /// </summary>
        /// <value>
        /// <c>true</c> if [families linking requested]; otherwise, <c>false</c>.
        /// </value>
        protected bool FamiliesLinkingRequested { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is overlay.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is overlay; otherwise, <c>false</c>.
        /// </value>
        protected bool IsOverlay { get; set; }

        protected long  MatterId { get; set; }

        /// <summary>
        ///     Sets the pipeline type specific parameters.
        /// </summary>
        /// <param name="activeJob">The active job.</param>
        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);

            switch (JobTypeId)
            {
                case 14:
                    using (var stream = new StringReader(activeJob.BootParameters.ToString()))
                    {
                        var xmlStream = new XmlSerializer(typeof (ImportBEO));
                        var importBeo = xmlStream.Deserialize(stream) as ImportBEO;
                        if (null != importBeo)
                        {
                            JobName = importBeo.ImportJobName;
                            JobTypeName = importBeo.ImportTypeName;
                            SourcePath = importBeo.Locations.First();

                            ThreadsLinkingRequested = importBeo.IsMapEmailThread;
                            FamiliesLinkingRequested = importBeo.IsImportFamilyRelations;
                            IsOverlay = !importBeo.IsAppend;
                            MatterId = importBeo.MatterId;
                        }
                    }
                    break;
                case 2: // DCB
                case 8: // eDocs
                    var profileBeo = Utils.SmartXmlDeserializer(activeJob.BootParameters.ToString()) as ProfileBEO;
                    if (null != profileBeo)
                    {
                        JobName = profileBeo.ImportJobName;
                        JobTypeName = profileBeo.ImportTypeName;
                        if (profileBeo.Locations != null && profileBeo.Locations.Any()) // DCB
                        {
                            SourcePath = profileBeo.Locations.First();
                            ThreadsLinkingRequested = profileBeo.IsMapEmailThread;
                            FamiliesLinkingRequested = profileBeo.IsImportFamilyRelations;
                        }
                        else if (profileBeo.FileLocations != null && profileBeo.FileLocations.Any()) //EDOCS
                        {
                            SourcePath = profileBeo.FileLocations[0].Path;
                            ThreadsLinkingRequested = true;
                            FamiliesLinkingRequested = true;
                        }
                        if (profileBeo.DatasetDetails != null && profileBeo.DatasetDetails.Matter != null)
                            _matterId = profileBeo.DatasetDetails.Matter.FolderID;
                        IsOverlay = !profileBeo.IsAppend;
                    }
                    break;
            }
          
        }

        /// <summary>
        /// Periodics the pipeline servicing hook.
        /// </summary>
        protected override void PeriodicPipelineServicingHook()
        {
            base.PeriodicPipelineServicingHook();
            if (FamiliesAndThreadsCalculationsInitiated)
            {
                return;
            }

            if (PipelineStatus.PipelineState < PipelineState.FirstWorkerCompleted)
            {
                //Tracer.Warning("FIRST WORKER HAS NOT COMPLETED YET!");
                return;
            }

            //PipelineSection triggerSection = Overlay
            //    // In case of Overlay we initiate finalization after specific section is done.
            //    ? PipelineSections.Find(pipelineSection => pipelineSection.Name == "Indexing")
            //    // In case of Append we initiate finalization after second section (i.e. before Vault)
            //    : PipelineSections.Find(pipelineSection => pipelineSection.OrderNumber == 1);

            // Plan B: Always finalize relationships AFTER Indexing.
            //PipelineSection triggerSection = PipelineSections.Find(pipelineSection => pipelineSection.Name == "Indexing");

            foreach (var pipelineSection in PipelineSections)
            {
                if (pipelineSection.DataPipeName != null && pipelineSection.DataPipeName.Count > 0)
                {
                    return;
                }

                // Debug
                //if (pipelineSection.Name == "LoadFileRecordParser") 

                if (pipelineSection.Name == "Indexing") 
                {
                    break;
                }
            }

            InitiateThreadsAndFamiliesCalculation();
        }

        /// <summary>
        ///     Completeds this instance.
        /// </summary>
        /// <returns></returns>
        protected override bool Completed()
        {
            base.Completed();
          
            if (FamiliesAndThreadsCalculationsInitiated)
            {
                return true;
            }

            InitiateThreadsAndFamiliesCalculation();
            if (JobTypeId == 14)
            {
                CleanUpImportLoadFileResources();
            }
            return false;
        }


        /// <summary>
        /// Clean import load file resources
        /// </summary>
        private void CleanUpImportLoadFileResources()
        {
            try
            {
                DocumentBO.DeleteLoadFileImagePath(MatterId, JobId);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                Tracer.Error("Job Id {0} - Error occurred on cleanup load file resources", JobId);
            }
        }

        /// <summary>
        /// Initiates the threads and families calculation.
        /// </summary>
        private void InitiateThreadsAndFamiliesCalculation()
        {
            if (ThreadsLinkingRequested || IsOverlay)
            {
                // Sending special message to ThreadsLinker
                var threadsSection = FindPipelineSection("ThreadsLinker");
                var threadsSectionDataPipeName = threadsSection.DataPipeName;
                using (var dataPipe = new Pipe(threadsSectionDataPipeName))
                {
                    dataPipe.Open();
                    var envelope = new PipeMessageEnvelope {Label = "EndOfDataMarker"};
                    dataPipe.Send(envelope);
                }
                Tracer.Info("Threads finalization initiated.");
            }

            if (FamiliesLinkingRequested && !ThreadsLinkingRequested && !IsOverlay)
            {
                // Sending special message to FamiliesLinker
                var familiesSection = FindPipelineSection("FamiliesLinker");
                var familiesSectionDataPipeName = familiesSection.DataPipeName;
                using (var dataPipe = new Pipe(familiesSectionDataPipeName))
                {
                    dataPipe.Open();
                    var envelope = new PipeMessageEnvelope {Label = "EndOfDataMarker"};
                    dataPipe.Send(envelope);
                }
                Tracer.Info("Families calculation initiated.");
            }

            // If both threads and families linking are requested, then we expect threads linker to send EndOfDataMarker to families linker
            FamiliesAndThreadsCalculationsInitiated = true;
        }
    }
}
