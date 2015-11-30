using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Business.Document;

namespace LexisNexis.Evolution.Worker
{
    using System;
    using System.Threading.Tasks;
    using Business.Relationships;
    using BusinessEntities;
    using Infrastructure;
    using Infrastructure.ExceptionManagement;
    using Overdrive;

    internal class FamiliesLinker : WorkerBase
    {
        private FamiliesProcessor familiesProcessor;

        protected override void BeginWork()
        {
            base.BeginWork();

            var jobParameters = Utils.SmartXmlDeserializer(BootParameters);

            var isEDocsJob = false;
            var profileBEO = jobParameters as ProfileBEO;
            if (profileBEO != null)
            {
                ThreadsLinkingRequested = profileBEO.IsMapEmailThread;
                FamiliesLinkingRequested = profileBEO.IsImportFamilyRelations;

                if (profileBEO.ImportTypeName.Contains("E-docs"))
                {
                    isEDocsJob = true;
                    ThreadsLinkingRequested = true;
                    FamiliesLinkingRequested = true;
                }

                CollectionId = profileBEO.DatasetDetails.CollectionId;
                MatterId = profileBEO.DatasetDetails.Matter.FolderID;
                IsOverlay = !profileBEO.IsAppend;
            }

            var lawImportBEO = jobParameters as LawImportBEO;
            if (lawImportBEO != null)
            {
                ThreadsLinkingRequested = lawImportBEO.CreateThreads;
                FamiliesLinkingRequested = lawImportBEO.CreateFamilyGroups;

                CollectionId = lawImportBEO.CollectionId;
                MatterId = lawImportBEO.MatterId;
                IsOverlay = lawImportBEO.ImportOptions != ImportOptionsBEO.AppendNew;
            }

            if (profileBEO == null && lawImportBEO == null)
            {
                throw new EVException().AddDbgMsg("Unknown type of bootparameters: {0}", BootParameters);
            }

            vaultHelper = new VaultHelper(MatterId, CollectionId);

            Tracer.Info(
                "MatterId = {0}, CollectionId = {1}, VaultConnectionString = {2}, IsOverlay = {3}, ThreadsLinkingRequested = {4}, FamiliesLinkingRequested = {5}",
                MatterId, CollectionId, vaultHelper.VaultConnectionString, IsOverlay, ThreadsLinkingRequested,
                FamiliesLinkingRequested);

            Dictionary<DocumentId, ExistingFamilyRecord> existingFamilies = null;
            if (IsOverlay)
            {
                existingFamilies = vaultHelper.LoadExistingFamilies();
            }

            familiesProcessor = new FamiliesProcessor(IsOverlay, existingFamilies, isEDocsJob);
        }

        private long MatterId { get; set; }
        private string CollectionId { get; set; }
        private bool IsOverlay { get; set; }
        private bool ThreadsLinkingRequested { get; set; }
        private bool FamiliesLinkingRequested { get; set; }

        private VaultHelper vaultHelper;

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            if (null == envelope)
            {
                throw new ArgumentNullException("envelope");
            }

            try
            {
                if (!FamiliesLinkingRequested && !IsOverlay)
                {
                    Tracer.Error("Families linking was not requested, but families linker still got a message!\r\n"
                                 + "    Label = {0}, Body is {1}", envelope.Label,
                        (envelope.Body == null) ? "null" : envelope.Body.GetType().FullName);
                    return;
                }

             
                if (envelope.Label == "EndOfDataMarker")
                {
                    FinalizeRelationships();
                    return;
                }

                ProcessFamilyInfoRecordsFromOtherWorkers(envelope);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        private void ProcessFamilyInfoRecordsFromOtherWorkers(PipeMessageEnvelope envelope)
        {
            var familiesInfo = envelope.Body as FamiliesInfo;
            if (familiesInfo != null)
            {
                familiesProcessor.ProcessFamiliesInfo(familiesInfo);
            }
        }

        private void FinalizeRelationships()
        {
            Tracer.Debug("Families calculation started.");

            if (ThreadsLinkingRequested || IsOverlay)
            {
                var existingThreads = vaultHelper.LoadExistingThreads();

                Tracer.Debug("FamiliesLinker got {0} existing thread records.", existingThreads.Count);

                familiesProcessor.AppendThreadsRecords(existingThreads);
            }

            var finalParentRecords = familiesProcessor.FinalizeRelationships();

            Tracer.Debug("Families calculation completed. Starting production: Insert {0}, Update {1}, LeaveAlone {2}",
                finalParentRecords.Count(p => p.Disposition == DispositionEnum.Insert),
                finalParentRecords.Count(p => p.Disposition == DispositionEnum.Update),
                finalParentRecords.Count(p => p.Disposition == DispositionEnum.LeaveAlone));

            ProduceFamiliesRecords(finalParentRecords);
            Tracer.Debug("Families production completed.");
        }

        private void ProduceFamiliesRecords(IEnumerable<ParentRecord> parentRecords)
        {
            
            Parallel.Invoke(
           () => IndexHelper.UpsertFamiliesInSearchEngine(parentRecords, MatterId, CollectionId),
           () => vaultHelper.Insert(parentRecords.Where(p => p.Disposition == DispositionEnum.Insert)),
           () => vaultHelper.Update(parentRecords.Where(p => p.Disposition == DispositionEnum.Update))
           );
            IncreaseProcessedDocumentsCount(parentRecords.Count());
        }


        protected override void EndWork()
        {
            var type= PipelineType.ToString();
            if (type == "ImportLoadFile")
            {
                CleanUpImportLoadFileResources();
            }
        }

        /// <summary>
        /// Clean import load file resources
        /// </summary>
        private void CleanUpImportLoadFileResources()
        {
            try
            {
                DocumentBO.DeleteLoadFileImagePath(MatterId, WorkAssignment.JobId);
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
                Tracer.Error("Job Id {0} - Error occurred on cleanup load file resources", WorkAssignment.JobId);
            }
        }
    }
}