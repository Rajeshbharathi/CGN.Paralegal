#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetUnAssignWorker" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>babugx</author>
//      <description>
//          This file covers the functionality to unassign the document(s) from a reviewset.
//          It removes the association of the document from the reviewset in both vault database and Search Sub system
//      </description>
//      <changelog>
//          <date value="27-Aug-2013">Created</date>
//          <date value="09/06/2013">CNEV 2.2.2 - Split Reviewset NFR fix - babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

#region Namespace

using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.Business.ReviewSet;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.ServiceContracts.ReviewSet;
using LexisNexis.Evolution.ServiceImplementation.ReviewSet;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using LexisNexis.Evolution.Infrastructure.Common;

#endregion

namespace LexisNexis.Evolution.Worker
{
    public class ReviewsetUnAssignWorker : WorkerBase
    {
        #region Private Variables

        private int _totalDocumentCount;
        private IReviewSetService _reviewsetServiceInstance;
        private string _createdBy;
        private IDocumentVaultManager _documentVaultManager;

        #endregion

        /// <summary>
        /// Gets the RVW reviewer search service instance.
        /// </summary>
        private IReviewSetService ReviewerSetInstance
        {
            get { return _reviewsetServiceInstance ?? (_reviewsetServiceInstance = new ReviewSetService()); }
        }

        /// <summary>
        /// Gets the DocumentVaultManager instance
        /// </summary>
        public IDocumentVaultManager DocumentVaultManager
        {
            get { return _documentVaultManager ?? (_documentVaultManager = new DocumentVaultManager()); }
            set { _documentVaultManager = value; }
        }

        /// <summary>
        /// overrides base ProcessMessage method
        /// </summary>
        /// <param name="envelope"></param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            try
            {
                var reviewsetRecord = (DocumentRecordCollection)envelope.Body;
                reviewsetRecord.ShouldNotBe(null);
                reviewsetRecord.Documents.ShouldNotBe(null);
                reviewsetRecord.ReviewsetDetails.ShouldNotBe(null);
                reviewsetRecord.ReviewsetDetails.CreatedBy.ShouldNotBeEmpty();
                reviewsetRecord.ReviewsetDetails.DatasetId.ShouldNotBe(0);
                reviewsetRecord.ReviewsetDetails.MatterId.ShouldNotBe(0);
                reviewsetRecord.ReviewsetDetails.SplitReviewSetId.ShouldNotBeEmpty();
                reviewsetRecord.ReviewsetDetails.BinderId.ShouldNotBeEmpty();

                _createdBy = reviewsetRecord.ReviewsetDetails.CreatedBy;
                _totalDocumentCount = reviewsetRecord.TotalDocumentCount;

                var removeTmpList = ConvertDocumentRecordtoReviewsetDocument(reviewsetRecord.Documents,
                    reviewsetRecord.ReviewsetDetails.CreatedBy, reviewsetRecord.ReviewsetDetails.SplitReviewSetId);

                UnAssignReviewsetInVault(removeTmpList, reviewsetRecord.ReviewsetDetails.DatasetId);

                UnAssignReviewsetInSearchIndex(removeTmpList, reviewsetRecord.ReviewsetDetails);
                ////TODO: Search Engine Replacement - Search Sub System - Update ReviewSetId in search engine
                Send(reviewsetRecord);
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        #region Helper Methods

        /// <summary>
        /// Unassigns the reviewset id for the list of documents from Vault
        /// </summary>
        /// <param name="documents">List<ReviewsetDocumentBEO></param>
        /// <param name="datasetId">long</param>
        private void UnAssignReviewsetInVault(List<ReviewsetDocumentBEO> documents, long datasetId)
        {
            //adds to DB
            using (var transScope = new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                //Get dataset details
                var dsBeo = DataSetBO.GetDataSetDetailForDataSetId(datasetId);
                dsBeo.ShouldNotBe(null);

                //Remove the reviewset association for the documents
                ReviewSetBO.DeleteDocumentsFromReviewSetForOverdrive(dsBeo.Matter.FolderID.ToString(),
                    dsBeo.CollectionId,
                    documents);
            }
        }

        /// <summary>
        /// Unassigns the reviewset id for the list of documents from search server
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="reviewset">ReviewsetRecord</param>
        private void UnAssignReviewsetInSearchIndex(List<ReviewsetDocumentBEO> documents, ReviewsetRecord reviewset)
        {
            const int batchSize = 1000;
            var processedCount = 0;

            var fields = new Dictionary<string, string>
            {
                {EVSystemFields.ReviewSetId, reviewset.SplitReviewSetId},
                {EVSystemFields.BinderId, reviewset.BinderId}
            };
            var indexManagerProxy = new IndexManagerProxy(reviewset.MatterId, reviewset.CollectionId);
            while (processedCount != documents.Count)
            {
                List<ReviewsetDocumentBEO> tmpDocuments;
                if ((documents.Count - processedCount) < batchSize)
                {
                    tmpDocuments = documents.Skip(processedCount).Take(documents.Count - processedCount).ToList();
                    processedCount += documents.Count - processedCount;
                }
                else
                {
                    tmpDocuments = documents.Skip(processedCount).Take(batchSize).ToList();
                    processedCount += batchSize;
                }
                var docs = tmpDocuments.Select(doc => new DocumentBeo() 
                { Id = doc.DocumentId, Fields = fields }).ToList();

                indexManagerProxy.BulkUnAssignFields(docs);
            }
        }


        /// <summary>
        /// Converts document record to review set document BEO
        /// </summary>
        /// <param name="documentList"></param>
        /// <param name="createdBy"> </param>
        /// <param name="reviewSetId"> </param>
        /// <returns></returns>
        private List<ReviewsetDocumentBEO> ConvertDocumentRecordtoReviewsetDocument(
            List<DocumentIdentityRecord> documentList, string createdBy, string reviewSetId)
        {
            return documentList.Select(document => new ReviewsetDocumentBEO
            {
                DocumentId = document.DocumentId, CollectionViewId = reviewSetId, 
                TotalRecordCount = _totalDocumentCount, FamilyId = document.FamilyId, 
                DCN = document.DocumentControlNumber, CreatedBy = createdBy
            }).ToList();
        }

        private void Send(DocumentRecordCollection reviewsetRecord)
        {
            var message = new PipeMessageEnvelope
            {
                Body = reviewsetRecord
            };
            if (null != OutputDataPipe)
            {
                OutputDataPipe.Send(message);
                IncreaseProcessedDocumentsCount(reviewsetRecord.Documents.Count);
            }
        }

        #endregion
    }
}