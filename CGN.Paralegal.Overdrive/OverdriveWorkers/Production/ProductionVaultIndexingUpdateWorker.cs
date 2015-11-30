# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="ProductionVaultIndexingUpdateWorker.cs" company="LexisNexis">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <author>Prabhu</author>
//      <description>
//          This is a file that contains ProductionVaultIndexingUpdateWorker class 
//      </description>
//      <changelog>
//          <date value="03/02/2012">Bug Fix 86335</date>
//          <date value="07/02/2012">Bug Fix 95652</date>
//          <date value="03/04/2012">Bug Fix 98615</date>
//          <date value="05/03/2012">Task #100232 </date>
//          <date value="6/5/2012">Fix for bug 100692 & 100624 - babugx</date>
//          <date value="04/17/2013">Task #135044 - Bates And Dpn in DDV and table view for conversion failed documenets </date>
//         <date value="02/20/2014">ADM-REPORTS-003  - Cleaning the existing Audit Log</date>
//         <date value="03/14/2014">ADM-REPORTS-003  - Included code changes for New Audit Log</date>
//          <date value="03/25/2014">Task 163335 - Dev Testing - Pass production set job name to audit log</date>
//          <date value="04/02/2014">Bug fix 167319 </date>
//          <date value="04/03/2014">CNEV 4.0 - Task# 186758 - Search Sub System and IndexBO Integration Changes : babugx</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using LexisNexis.Evolution.Business.IR;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.External.VaultManager;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using OverdriveWorkers.Production;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using LexisNexis.Evolution.Business.MatterManagement;


namespace LexisNexis.Evolution.Worker
{
    public class ProductionVaultIndexingUpdateWorker : SearchEngineWorkerBase, IDisposable
    {
        private IDocumentVaultManager _documentVaultMngr;
        private string _mMatterId;
        private string _mCollectionId;
        


        private Dictionary<string, List<KeyValuePair<string, string>>> _documentDetails;

        protected override void BeginWork()
        {
            base.BeginWork();
            try
            {
                _documentVaultMngr = new DocumentVaultManager();
                var _jobParameter =
               (ProductionDetailsBEO)XmlUtility.DeserializeObject(BootParameters, typeof(ProductionDetailsBEO));
                _jobParameter.ShouldNotBe(null);
                _mMatterId = _jobParameter.MatterId.ToString();
                SetCommiyIndexStatusToInitialized(Convert.ToInt64(_mMatterId));
                
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
        }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            List<ProductionDocumentDetail> productionDocuments = null;
            List<ProductionDocumentDetail> errorDocuments = null;

            try
            {
                productionDocuments = envelope.Body as List<ProductionDocumentDetail>;

                Debug.Assert(productionDocuments != null, "productionDocuments != null");
                if (productionDocuments == null || !productionDocuments.Any())
                {
                    Tracer.Warning("Empty batch received! Skipped.");
                    return;
                }

                productionDocuments.ShouldNotBeEmpty();

                using (new EVTransactionScope(TransactionScopeOption.Suppress))
                {
                    if (productionDocuments != null && productionDocuments.Any())
                    {
                        var productionDocument = productionDocuments.First();
                        productionDocument.MatterId.ShouldNotBeEmpty();
                        _mMatterId = productionDocument.MatterId;
                        productionDocument.dataSetBeo.ShouldNotBe(null);
                        productionDocument.dataSetBeo.CollectionId.ShouldNotBeEmpty();
                        _mCollectionId = productionDocument.dataSetBeo.CollectionId;

                        var docs = productionDocuments.Select(doc => new DocumentDataBEO { DocumentId = doc.DocumentId, CollectionId = _mCollectionId }).ToList();
                        var docFields = MatterBO.GetCollectionDocumentFields(Int64.Parse(_mMatterId), docs);
                        docFields.ShouldNotBe(null);
                        _documentDetails = ConstructDocumentFieldsForSearchSubSystem(docFields);

                        ProcessDocumentFields(productionDocuments);
                    }
                }
            }
            catch (Exception ex)
            {
                errorDocuments = productionDocuments;
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
            ProductionLogHelper.SendProductionLogs(LogPipe, errorDocuments, PipelineId, WorkerId,
                Constants.ProductionVaultIndexingUpdateWokerRoleId);
        }
        /// <summary>
        /// Ends the work.
        /// </summary>
        protected override void EndWork()
        {
            base.EndWork();
            SetCommitIndexStatusToCompleted(Convert.ToInt64(_mMatterId));
            
        }
    
        /// <summary>
        /// Helper method to consolidate document fields
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>

        //TODO: ReIndexDocumentFieldBEO entity will have to be renamed
        private Dictionary<string, List<KeyValuePair<string, string>>> ConstructDocumentFieldsForSearchSubSystem(List<ReIndexDocumentFieldBEO> fields)
        {
            var documents = new Dictionary<string, List<KeyValuePair<string, string>>>();
            var docGroups = fields.GroupBy(f => f.DocumentKey);

            foreach (var doc in docGroups)
            {
                var docFields = new List<KeyValuePair<string, string>>();
                foreach (var fld in doc)
                {
                    var matchField = docFields.FirstOrDefault(f => f.Key == fld.Name);
                    if (!string.IsNullOrEmpty(matchField.Key))
                    {
                        var currentVal = matchField.Value;
                        docFields.Remove(matchField);
                        docFields.Add(new KeyValuePair<string, string>(fld.Name,
                            string.IsNullOrEmpty(currentVal)
                                ? fld.FieldValue
                                : string.Format("{0},{1}", currentVal, fld.FieldValue)));
                    }
                    else
                    {
                        docFields.Add(new KeyValuePair<string, string>(fld.Name, fld.FieldValue));
                    }
                }
                documents.Add(doc.Key, docFields);
            }
            return documents;
        }

        /// <summary>
        /// Process the document for production fields
        /// </summary>
        /// <param name="lstProductionDocuments"></param>
        private void ProcessDocumentFields(List<ProductionDocumentDetail> lstProductionDocuments)
        {
            
            if (lstProductionDocuments.Any())
            {
                var lstBatesAndDpnFields = new List<DocumentFieldsBEO>();
                var indexDocFields = new Dictionary<string, List<KeyValuePair<string, string>>>();
                foreach (var productionDocument in lstProductionDocuments)
                {
                    if (_documentDetails.ContainsKey(productionDocument.DocumentId))
                    {
                        productionDocument.Fields = _documentDetails[productionDocument.DocumentId];
                    }
                    productionDocument.DocumentFields = _documentDetails;

                    var lstDocFields = ConstructDocumentFields(productionDocument);
                    if (lstDocFields != null && lstDocFields.Any())
                    {
                        lstBatesAndDpnFields.AddRange(lstDocFields);
                        ConstructIndexFields(productionDocument, lstDocFields, ref indexDocFields);
                    }
                }
                if (lstBatesAndDpnFields.Any())
                {
                    var status = _documentVaultMngr.BulkAddOrUpdateDocumentFields(_mMatterId, _mCollectionId,
                        lstBatesAndDpnFields);

                    if (status)
                    {
                        var indexManagerProxy = new IndexManagerProxy(Int64.Parse(_mMatterId), _mCollectionId);
                        var docs = new List<DocumentBeo>();
                        if (indexDocFields.Any())
                        {
                            docs.AddRange(from doc in indexDocFields
                                          let
                                              fields = doc.Value.ToDictionary<KeyValuePair<string, string>, string, string>
                                              (field => field.Key, field => field.Value)
                                          select new DocumentBeo() { Id = doc.Key, Fields = fields });
                        }
                        indexManagerProxy.BulkUpdateDocumentsAsync(docs);
                        Tracer.Trace("{0} fields calling the method UpdatesBatesFields", indexDocFields.Count());
                    }
                }
            }
            Send(lstProductionDocuments);
        }


        /// <summary>
        /// Helper method to construct the fields to update in search index
        /// </summary>
        /// <param name="document"></param>
        /// <param name="productionFields"></param>
        /// <param name="indexFields"></param>
        private void ConstructIndexFields(ProductionDocumentDetail document, List<DocumentFieldsBEO> productionFields,
            ref Dictionary<string, List<KeyValuePair<string, string>>> indexFields)
        {
            //determine the indexName for bate fields
            var indexBateFieldNames =
                            document.lstDsFieldsBeo.Where(f => f.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesNumber) ||
                                                f.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesBegin) ||
                                                f.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesEnd) ||
                                                f.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesRange) ||
                                                f.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.DPN)).Select(
                                                    f => f.Name).ToList();

            var docFields = new List<KeyValuePair<string, string>>();
            //loop through all the bates & dpn fields in dataset
            foreach (var bateField in indexBateFieldNames)
            {
                var lstFields = document.Fields.FirstOrDefault(f => f.Key == bateField);
                var field = productionFields.FirstOrDefault(f => f.FieldName == bateField);
                if (lstFields.Key != null)
                {
                    //determine if there is change in value
                    if (field != null && lstFields.Value != field.FieldValue)
                    {
                        docFields.Add(new KeyValuePair<string, string>(bateField,
                                                                         string.Format("{0},{1}", lstFields.Value,
                                                                                       field.FieldValue)));
                    }
                    else
                    {
                        docFields.Add(new KeyValuePair<string, string>(bateField, lstFields.Value));
                    }
                }
                else
                {
                    if (field != null)
                        docFields.Add(new KeyValuePair<string, string>(bateField, field.FieldValue));
                }
            }
            indexFields.Add(document.DocumentId, docFields);
        }


        /// <summary>
        /// constructs document fields and audit log
        /// </summary>
        /// <param name="productionDocumentDetail"></param>
        /// <returns></returns>
        private List<DocumentFieldsBEO> ConstructDocumentFields(ProductionDocumentDetail productionDocumentDetail)
        {
            List<DocumentFieldsBEO> lstDocumentFields = null;
            var lstProductionFields = productionDocumentDetail.lstProductionFields;
            var lstDsFeildsBeo = productionDocumentDetail.lstDsFieldsBeo;
            if (lstProductionFields != null && lstDsFeildsBeo != null)
            {
                lstDocumentFields = new List<DocumentFieldsBEO>();
                //Bates Number Fieldtype Id-3004
                FieldBEO productionFieldBeo = null;
                productionFieldBeo =
                    lstProductionFields.FirstOrDefault(
                        prodField =>
                            prodField.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesNumber));
                if (productionFieldBeo != null)
                {
                    lstDocumentFields.Add(new DocumentFieldsBEO
                    {
                        FieldId =
                            lstDsFeildsBeo.FirstOrDefault(
                                DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).ID,
                        FieldType =
                            new FieldDataTypeBusinessEntity
                            {
                                DataTypeId = (Int32) Constants.FieldTypeIds.BatesNumber,
                                DataTypeDisplayValue = Constants.BatesNumber
                            },
                        FieldValue =
                            !string.IsNullOrWhiteSpace(productionDocumentDetail.AllBates)
                                ? productionDocumentDetail.AllBates
                                : string.Empty,
                        FieldName =
                            lstDsFeildsBeo.FirstOrDefault(
                                DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).Name,
                        DocumentReferenceId = productionDocumentDetail.DocumentId
                    });
                    //Bates Begin Fieldtype Id-3005
                    productionFieldBeo =
                        lstProductionFields.FirstOrDefault(
                            prodField =>
                                prodField.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesBegin));
                    if (productionFieldBeo != null)
                    {
                        lstDocumentFields.Add(new DocumentFieldsBEO
                        {
                            FieldId =
                                lstDsFeildsBeo.FirstOrDefault(
                                    DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).ID,
                            FieldType =
                                new FieldDataTypeBusinessEntity
                                {
                                    DataTypeId = (Int32) Constants.FieldTypeIds.BatesBegin,
                                    DataTypeDisplayValue = Constants.BatesBegin
                                },
                            FieldValue =
                                !string.IsNullOrWhiteSpace(productionDocumentDetail.StartingBatesNumber)
                                    ? productionDocumentDetail.StartingBatesNumber
                                    : string.Empty,
                            FieldName =
                                lstDsFeildsBeo.FirstOrDefault(
                                    DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).Name,
                            DocumentReferenceId = productionDocumentDetail.DocumentId
                        });
                    }
                    //Bates End Fieldtype Id-3006
                    productionFieldBeo =
                        lstProductionFields.FirstOrDefault(
                            prodField =>
                                prodField.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesEnd));
                    if (productionFieldBeo != null)
                    {
                        lstDocumentFields.Add(new DocumentFieldsBEO
                        {
                            FieldId =
                                lstDsFeildsBeo.FirstOrDefault(
                                    DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).ID,
                            FieldType =
                                new FieldDataTypeBusinessEntity
                                {
                                    DataTypeId = (Int32) Constants.FieldTypeIds.BatesEnd,
                                    DataTypeDisplayValue = Constants.BatesEnd
                                },
                            FieldValue =
                                !string.IsNullOrWhiteSpace(productionDocumentDetail.EndingBatesNumber)
                                    ? productionDocumentDetail.EndingBatesNumber
                                    : string.Empty,
                            FieldName =
                                lstDsFeildsBeo.FirstOrDefault(
                                    DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).Name,
                            DocumentReferenceId = productionDocumentDetail.DocumentId
                        });
                    }
                    //Bates Range Field Type Id-3007
                    productionFieldBeo =
                        lstProductionFields.FirstOrDefault(
                            prodField =>
                                prodField.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.BatesRange));
                    if (productionFieldBeo != null)
                    {
                        lstDocumentFields.Add(new DocumentFieldsBEO
                        {
                            FieldId =
                                lstDsFeildsBeo.FirstOrDefault(
                                    DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).ID,
                            FieldType =
                                new FieldDataTypeBusinessEntity
                                {
                                    DataTypeId = (Int32) Constants.FieldTypeIds.BatesRange,
                                    DataTypeDisplayValue = Constants.BatesRange
                                },
                            FieldValue =
                                productionDocumentDetail.StartingBatesNumber + "-" +
                                productionDocumentDetail.EndingBatesNumber,
                            FieldName =
                                lstDsFeildsBeo.FirstOrDefault(
                                    DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).Name,
                            DocumentReferenceId = productionDocumentDetail.DocumentId
                        });
                    }
                }
                //DPN Fieldtype Id-3008
                productionFieldBeo =
                    lstProductionFields.FirstOrDefault(
                        prodField => prodField.FieldType.DataTypeId == Convert.ToInt32(Constants.FieldTypeIds.DPN));
                if (productionFieldBeo != null)
                {
                    lstDocumentFields.Add(new DocumentFieldsBEO
                    {
                        FieldId =
                            lstDsFeildsBeo.FirstOrDefault(
                                DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).ID,
                        FieldType =
                            new FieldDataTypeBusinessEntity
                            {
                                DataTypeId = (Int32) Constants.FieldTypeIds.DPN,
                                DataTypeDisplayValue = Constants.DPN
                            },
                        FieldValue =
                            !string.IsNullOrWhiteSpace(productionDocumentDetail.DocumentProductionNumber)
                                ? productionDocumentDetail.DocumentProductionNumber
                                : string.Empty,
                        FieldName =
                            lstDsFeildsBeo.FirstOrDefault(
                                DsField => DsField.Name.ToLower().Equals(productionFieldBeo.Name.ToLower())).Name,
                        DocumentReferenceId = productionDocumentDetail.DocumentId
                    });
                }
            }
            return lstDocumentFields;
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        private void Send(ICollection jobDetails)
        {
            var message = new PipeMessageEnvelope
            {
                Body = jobDetails
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(jobDetails.Count);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed; // to detect redundant calls


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }
                // shared cleanup logic
                disposed = true;
            }
        }
    }
}