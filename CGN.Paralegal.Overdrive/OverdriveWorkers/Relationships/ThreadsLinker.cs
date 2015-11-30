using System.Collections.Generic;

namespace LexisNexis.Evolution.Worker
{
    using System;
    using System.Threading.Tasks;
    using Business.Relationships;
    using BusinessEntities;
    using Infrastructure;
    using Infrastructure.ExceptionManagement;
    using Overdrive;

    internal class ThreadsLinker : WorkerBase
    {
        private ThreadsProcessor _threadsProcessor;

        protected override void BeginWork()
        {
            base.BeginWork();

            var jobParameters = Utils.SmartXmlDeserializer(BootParameters);

            var profileBEO = jobParameters as ProfileBEO;
            if (profileBEO != null)
            {
                ThreadsLinkingRequested = profileBEO.IsMapEmailThread;
                FamiliesLinkingRequested = profileBEO.IsImportFamilyRelations;

                if (profileBEO.ImportTypeName.Contains("E-docs"))
                {
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

            Dictionary<DocumentId, ExistingThreadRecord> existingThreads = null;
            if (IsOverlay)
            {
                existingThreads = vaultHelper.LoadExistingThreads();
            }

            _threadsProcessor = new ThreadsProcessor(IsOverlay, existingThreads);

            resultsAccumulator = new ThreadsCalculationResults();
        }

        private long MatterId { get; set; }
        private string CollectionId { get; set; }
        private bool IsOverlay { get; set; }
        private bool ThreadsLinkingRequested { get; set; }
        private bool FamiliesLinkingRequested { get; set; }

        private VaultHelper vaultHelper;

        private ThreadsCalculationResults resultsAccumulator;

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            if (null == envelope)
            {
                throw new ArgumentNullException("envelope");
            }

            try
            {
                if (!ThreadsLinkingRequested && !IsOverlay)
                {
                    Tracer.Error("Threads linking was not requested, but threads linker still got a message!\r\n"
                                 + "    Label = {0}, Body is {1}", envelope.Label,
                        (envelope.Body == null) ? "null" : envelope.Body.GetType().FullName);
                    return;
                }

                if (envelope.Label == "EndOfDataMarker")
                {
                    FinalizeRelationships();

                    if (FamiliesLinkingRequested || IsOverlay)
                    {
                        // When both linkings are requested it is responsibility of threads linker
                        // to tell to family linker when it can finalize the data
                        SendEndOfDataMarkerToFamiliesLinker();
                    }
                    return;
                }

                var threadsInfo = envelope.Body as ThreadsInfo;
                if (threadsInfo != null)
                {
                    //Tracer.Warning("Starting intermediate threads calculations");
                    var threadsCalculationResults = _threadsProcessor.ProcessThreadsInfo(threadsInfo);
                    resultsAccumulator.Append(threadsCalculationResults);

                    // Plan B: we don't do intermediate production now.
                    //Tracer.Warning("Starting intermediate threads production");
                    //ProduceThreadsRecords(threadsCalculationResults);
                    //Tracer.Warning("Finished intermediate threads production");
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        private void SendEndOfDataMarkerToFamiliesLinker()
        {
            var familiesAndThreadsPipe = GetOutputDataPipe("FamiliesLinker");
            var message = new PipeMessageEnvelope {Label = "EndOfDataMarker"};
            if (familiesAndThreadsPipe != null)
            {
                familiesAndThreadsPipe.Send(message);
            }
        }

        private void FinalizeRelationships()
        {
            // In here we supposed to handle all non-typical cases, such as 
            // 1. Immediate parent is not present among the documents;
            // 2. Root of the conversation  is not present among the documents;
            Tracer.Debug("Threads calculation started.");

            var threadsCalculationResults = _threadsProcessor.FinalizeRelationships();
            resultsAccumulator.Append(threadsCalculationResults);

            // Debug 
            //Tracer.Warning(resultsAccumulator.ToString());

            DontInsertChildlessTopmostNodes(resultsAccumulator.ListToInsert);

            // Debug 
            //Tracer.Warning(resultsAccumulator.ToString());

            Tracer.Debug("Threads calculation completed. Starting production: Insert {0}, Update {1}, Delete {2}",
                resultsAccumulator.InsertCount, resultsAccumulator.UpdateCount, resultsAccumulator.DeleteCount);
            ProduceThreadsRecords();
            Tracer.Debug("Threads production completed.");
        }

        private static void DontInsertChildlessTopmostNodes(List<Node> listToInsert)
        {
            if (listToInsert != null)
            {
                listToInsert.RemoveAll(node => node.ParentId == null && node.Children == null);
            }
        }

        private void ProduceThreadsRecords()
        {
            // Debug
            //Tracer.Debug(resultsAccumulator.ToString());

            Parallel.Invoke(
            () => vaultHelper.Delete(resultsAccumulator.ListToDelete, "DOC_DocumentThreads"),
            () => IndexHelper.DeleteFromSearchEngine(resultsAccumulator.ListToDelete, MatterId, CollectionId),
            () => vaultHelper.Insert(resultsAccumulator.ListToInsert),
            () => vaultHelper.Update(resultsAccumulator.ListToUpdate),
            () => IndexHelper.UpsertThreadsInSearchEngine(resultsAccumulator.ListToInsert.SafeConcat(resultsAccumulator.ListToUpdate), MatterId, CollectionId)
            );
            IncreaseProcessedDocumentsCount(
                resultsAccumulator.InsertCount +
                resultsAccumulator.UpdateCount +
                resultsAccumulator.DeleteCount
                );
        }
    }
}