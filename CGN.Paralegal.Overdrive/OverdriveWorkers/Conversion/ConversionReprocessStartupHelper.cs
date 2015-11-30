#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionReprocessStartupHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Henry</author>
//      <description>
//          This file contains all the  methods related to  ConversionReprocessStartupHelper
//      </description>
//      <changelog>
//          <date value="05-15-2013">Initial: Reconversion Processing</date>
//          <date value="07-03-2013">Bug # 146561 and 145022 - Fix to show  all the documents are listing out in production manage conversion screen</date>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="10/07/2013">Dev Bug  # 154336 -ADM -ADMIN - 006 - Import /Production Reprocessing reprocess all documents even with filter and all and other migration fixes
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.Infrastructure.DBManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Vault;
using LexisNexis.Evolution.Worker.Data;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    using System.Data.Common;

    using LexisNexis.Evolution.Business.Relationships;
    using LexisNexis.Evolution.External.VaultManager;

    public class ConversionReprocessStartupHelper
    {

        /// <summary>
        /// Sends the specified document batch to next worker in the pipeline.
        /// </summary>
        /// <param name="docCollection">The document batch.</param>
        public static List<ProductionDocumentDetail> ConvertToProductionDocumentList(
            ConversionDocCollection docCollection)
        {
            List<ProductionDocumentDetail> docList = null;
            var baseConfig = docCollection.BaseJobConfig as ProductionDetailsBEO;

            if (docCollection != null && docCollection.Documents != null && docCollection.Documents.Any())
            {
                var modelDoc = ProductionStartupHelper.ConstructProductionModelDocument(baseConfig);

                docList = new List<ProductionDocumentDetail>();

                int docNumber = 0;

                foreach (var doc in docCollection.Documents)
                {
                    ProductionDocumentDetail newDoc =
                        ConvertToProductionDocumentDetail((ReconversionProductionDocumentBEO) doc, modelDoc);
                    //To set Bates running number for selected reprocess document
                    int batesRunningNumber = 0;
                    if (!string.IsNullOrEmpty(newDoc.StartingBatesNumber))
                        batesRunningNumber=    Convert.ToInt32(newDoc.StartingBatesNumber.Replace(newDoc.Profile.ProductionPrefix, ""));
                    newDoc.StartBatesRunningNumber = (batesRunningNumber > 0)
                                                         ? (batesRunningNumber -
                                                            Convert.ToInt32(newDoc.Profile.ProductionStartingNumber))
                                                         : batesRunningNumber;

                    //use doc number in the sequence for correctationId, will be used as part of key in inserting to EV_JOB_ProductionTaskFlags table
                    newDoc.CorrelationId = ++docNumber;

                    //create the folder if it doesnot exists
                    if (!string.IsNullOrEmpty(newDoc.ExtractionLocation)&&!Directory.Exists(newDoc.ExtractionLocation))
                        Directory.CreateDirectory(newDoc.ExtractionLocation);

                    //add the new doc to list
                    docList.Add(newDoc);
                }

            }

            return docList;
        }


        /// <summary>
        /// Convert the document to the ProductionDocumentDetail that can be used by ProductionNearNativeConversionHelper class
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="modelDoc">The model document</param>
        private static ProductionDocumentDetail ConvertToProductionDocumentDetail(
            ReconversionProductionDocumentBEO document, ProductionDocumentDetail modelDoc)
        {
            var pDoc = new ProductionDocumentDetail
            {
                MatterId = modelDoc.MatterId,
                CreatedBy = modelDoc.CreatedBy,
                DocumentSelectionContext = modelDoc.DocumentSelectionContext,
                DatasetCollectionId = modelDoc.DatasetCollectionId,
                OriginalCollectionId = modelDoc.OriginalCollectionId,
                DocumentExclusionContext = modelDoc.DocumentExclusionContext,
                ProductionCollectionId = modelDoc.ProductionCollectionId,
                Profile = modelDoc.Profile,
                ArchivePath = modelDoc.ArchivePath,
                OriginalDatasetId = modelDoc.OriginalDatasetId,
                OriginalDatasetName = modelDoc.OriginalDatasetName,
                GetText = modelDoc.GetText,
                lstProductionFields = modelDoc.lstProductionFields,
                dataSetBeo = modelDoc.dataSetBeo,
                lstDsFieldsBeo = modelDoc.lstDsFieldsBeo,
                matterBeo = modelDoc.matterBeo,
                SearchServerDetails = modelDoc.SearchServerDetails
            };

            //same info for all docs
            //pDoc.ManageShareRootPath = modelDoc.ManageShareRootPath;
            pDoc.GetText = modelDoc.GetText;

            //pupulate from document



            pDoc.DocumentId = document.DocumentId;
            pDoc.OriginalDocumentReferenceId = document.DocumentId;
            pDoc.DCNNumber = document.DCNNumber;

            //populdate bates
            pDoc.StartingBatesNumber = document.StartingBatesNumber;
            pDoc.EndingBatesNumber = document.EndingBatesNumber;

            //comma seperated list of bates numbers
            pDoc.AllBates = document.AllBates;

            //get the number of pages by counting all bates
            if (document.AllBates != null)
                pDoc.NumberOfPages = (document.AllBates.Split(new char[] {','})).Length;

            //popudate DPN
            pDoc.DocumentProductionNumber = document.DocumentProductionNumber;

            //set NearNativeConversionPriority
            pDoc.NearNativeConversionPriority = document.NearNativeConversionPriority;
            //get the directory for the production location; ProductionPath contain the full path, including the file name
            if (String.IsNullOrEmpty(document.ProductionPath)) return pDoc;
            pDoc.ExtractionLocation = Path.GetDirectoryName(document.ProductionPath);
            return pDoc;
        }

        /// <summary>
        /// Gets the production document list.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="selectionMode">The selection mode.</param>
        /// <param name="matterId">The matter id.</param>
        /// <param name="baseJobConfig">The base job config.</param>
        /// <param name="redactableCollectionId">The redactable collection id.</param>
        /// <param name="jobId"></param>
        /// <param name="filters"> </param>
        /// <returns></returns>
        public virtual IEnumerable<ReconversionProductionDocumentBEO> GetProductionDocumentList(
            string filePath, ReProcessJobSelectionMode selectionMode, long matterId,
            ProductionDetailsBEO baseJobConfig, string redactableCollectionId, int jobId,string filters=null)
        {
            switch (selectionMode)
            {
                case ReProcessJobSelectionMode.Selected:
                    {
                        var docidList = GetDocumentIdListFromFile(filePath, Constants.DocId);
                        foreach (var v in GetProductionDocumentListForIdList(
                            docidList, Constants.DocId, matterId, baseJobConfig, redactableCollectionId, jobId))
                            yield return v;
                        break;
                    }
                case ReProcessJobSelectionMode.Csv:
                case ReProcessJobSelectionMode.CrossReference:
                    {
                        var dictIds = GetDocumentIdListFromFile(filePath, Constants.DCN, Constants.DocumentSetName);

                        //put all value in one list. Since there is only one production set, it should all be the same collectionId
                        var ids = new List<string>();
                        foreach (var dictId in dictIds)
                        {
                            ids.AddRange(dictId.Value);
                        }
                        foreach (var v in GetProductionDocumentListForIdList(
                            ids, Constants.DCN, matterId, baseJobConfig, redactableCollectionId, jobId)) yield return v;
                        break;
                    }
                case ReProcessJobSelectionMode.All:
                    IEnumerable<ReconversionProductionDocumentBEO> reconversionProductionDocumentBeos = null;
                    if (!string.IsNullOrEmpty(filters))
                    {
                        var documentVaultManager = new DocumentVaultManager();
                        documentVaultManager.Init(matterId);

                        IEnumerable<DocumentConversionLogBeo> documentConversionLogBeos =
                            documentVaultManager.GetConversionResultsWithFilters(matterId, jobId, null, null, filters
                                );
                        var docidList =
                            documentConversionLogBeos.Select(
                                documentConversionLogBeo =>
                                documentConversionLogBeo.DocId.ToString(CultureInfo.InvariantCulture)).ToList();
                        reconversionProductionDocumentBeos = GetProductionDocumentListForIdList(
                            docidList, Constants.DocId, matterId, baseJobConfig, redactableCollectionId, jobId);
                    }
                    else
                    {
                        reconversionProductionDocumentBeos = GetProductionDocumentListForIdList
                            (
                                null, "ALL", matterId, baseJobConfig, redactableCollectionId, jobId);
                    }
                    if (reconversionProductionDocumentBeos == null) yield return null;
                    foreach (var reConversionProductionBeo in reconversionProductionDocumentBeos)
                        yield return reConversionProductionBeo;
                    break;
            }
        }

        /// <summary>
        /// Convert the document to the ProductionDocumentDetail that can be used by ProductionNearNativeConversionHelper class
        /// </summary>
        /// <param name="inputFilePath">Input file path that contain list of DCN</param>
        /// <param name="selectionMode">selection mode</param>
        /// <param name="matterId">matter id</param>
        /// <param name="datasetId"></param>
        /// <param name="jobId"></param>
        /// <param name="filters"> </param>
        public static IEnumerable<ReconversionDocumentBEO> GetImportDocumentList(
            string inputFilePath, ReProcessJobSelectionMode selectionMode, long matterId, long datasetId, long jobId,
            string filters = null)
        {
            switch (selectionMode)
            {
                case ReProcessJobSelectionMode.Selected:
                    {
                        List<string> docidList = GetDocumentIdListFromFile(inputFilePath, Constants.DocId);
                        //for import, the list of ids are native or image set document ids
                        foreach (var v in GetImportDocumentListForIDList(docidList, Constants.DocId, null, matterId))
                            yield return v;
                        break;
                    }
                case ReProcessJobSelectionMode.CrossReference:
                    {
                        List<string> docidList = GetDocumentIdListFromFile(inputFilePath, Constants.DCN);
                        foreach (var v in GetImportDocumentListForIDList(docidList, Constants.DCN, null, matterId))
                            yield return v;
                        break;
                    }
                case ReProcessJobSelectionMode.Csv:
                    var dictIds = GetDocumentIdListFromFile(inputFilePath, Constants.DCN, Constants.DocumentSetName);
                    List<DocumentSetBEO> lstDocumentSet = DataSetBO.GetAllDocumentSet(datasetId.ToString());
                    foreach (var key in dictIds.Keys)
                    {
                        //get collectionId for document Set Name
                        var firstOrDefault = lstDocumentSet.FirstOrDefault(d => d.DocumentSetName.Equals(key));
                        if (firstOrDefault != null)
                        {
                            string collectionId = firstOrDefault.DocumentSetId;

                            //collection id could be either native set id or image set ids
                            foreach (
                                var v in
                                    GetImportDocumentListForIDList(dictIds[key], Constants.DCN, collectionId, matterId))
                                yield return v;
                        }
                    }
                    break;
                case ReProcessJobSelectionMode.All:
                    foreach (var reConversionDocumentBeo in GetReconversionDocumentBeosForJobId(matterId, jobId, filters))
                        yield return reConversionDocumentBeo;
                    break;
            }
        }

        /// <summary>
        /// Convert the document to the ProductionDocumentDetail that can be used by ProductionNearNativeConversionHelper class
        /// </summary>
        /// <param name="idList">idList</param>
        /// <param name="idType">id type</param>
        /// <param name="collectionId">collection id</param>
        /// <param name="matterId">matter id</param>

        public static IEnumerable<ReconversionDocumentBEO> GetImportDocumentListForIDList(List<string> idList,
                                                                                          string idType,
                                                                                          string collectionId,
                                                                                          long matterId)
        {
            //Convert the list of DCN to list of documentId
            var vault = VaultRepository.CreateRepository(matterId);

            var dsResult = vault.GetImportReprocessDocumentList(idList, idType, collectionId);

            ReconversionDocumentBEO doc = null;
            List<ReconversionDocumentBEO> documents = new List<ReconversionDocumentBEO>();

            if (dsResult != null && dsResult.Tables.Count > 0)
            {
                foreach (DataRow dr in dsResult.Tables[0].Rows)
                {
                    string docRefId = dr[Constants.ColumnDocReferenceId].ToString();
                    string docSetId = dr[Constants.ColumnCollectionId].ToString();
                    string dcn = dr[Constants.ColumnDocTitle].ToString();

                    if (documents.Any(f => f.CollectionId == docSetId && f.DocumentId == docRefId))
                    //new document (need to check this due to that multiple rows are returned in case of one document with multiple file path associated)
                    {
                        //add file path to list of file path for the document
                        documents.FirstOrDefault(f => f.CollectionId == docSetId && f.DocumentId == docRefId).FileList.Add(dr[Constants.ColumnDocText].ToString());
                    }
                    else
                    {
                        doc = new ReconversionDocumentBEO { DocumentId = docRefId, CollectionId = docSetId, DCNNumber = dcn };
                        doc.FileList.Add(dr[Constants.ColumnDocText].ToString());//add file path to list of file path for the document
                        documents.Add(doc);
                    }
                    
                }
            }
            return documents.AsEnumerable();
        }

        /// <summary>
        /// Gets the reconversion document beos for job id.
        /// </summary>
        /// <param name="matterId">The matter id.</param>
        /// <param name="jobId">The job id.</param>
        /// <param name="filters">The filters.</param>
        /// <returns></returns>
        public static IEnumerable<ReconversionDocumentBEO> GetReconversionDocumentBeosForJobId(long matterId, long jobId,
                                                                                               string filters)
        {

            IDataReader dataReader = null;

            if (!string.IsNullOrEmpty(filters))
            {
                //user can filter the conversion results then do select all
                //filters has filter key : value with comma a delimiter
                var documentVaultManager = new DocumentVaultManager();
                documentVaultManager.Init(matterId);
                dataReader = documentVaultManager.GetConversionResultsDataReader(matterId, jobId, null, null, filters);
            }
            else
            {
                var evDbManager = new EVDbManager(matterId);
                const string sqlCommand =
                    @"SELECT dm.DocID,dm.DocTitle,dm.DocReferenceID, dm.CollectionID, ps.DCN ,T.DocText
                FROM [dbo].[DOC_ProcessSet] AS ps 
                INNER JOIN [dbo].[DOC_DocumentMaster] AS dm ON ps.[DocID] = dm.[DocID] 
                LEFT OUTER JOIN  dbo.DOC_DocumentText T ON DM.DocID = T.DocID AND T.TextTypeID = 2 
                WHERE ps.JobID = @JobID 
                ORDER BY  DM.DocTitle";
                var dbCommand = evDbManager.CreateTextCommand(sqlCommand);
                dbCommand.CommandTimeout = 0;
                evDbManager.AddInParameter(dbCommand, "JobID", DbType.Int64, jobId);
                dataReader = evDbManager.ExecuteDataReader(dbCommand);
            }

            Debug.Assert(dataReader!=null);
            ReconversionDocumentBEO reconversionDocumentBeo = null;
            while (dataReader.Read())
            {
                string docRefId = dataReader["DocReferenceID"].ToString();
                string collectionId = dataReader["CollectionID"].ToString();
                string dcn = dataReader["DCN"].ToString();

                //new document (need to check this due to that multiple rows are returned in case of one document with multiple file path associated)
                if (reconversionDocumentBeo == null ||
                    !docRefId.Equals(reconversionDocumentBeo.DocumentId) ||
                    !collectionId.Equals(reconversionDocumentBeo.CollectionId) ||
                    !dcn.Equals(reconversionDocumentBeo.DCNNumber))
                {

                    if (reconversionDocumentBeo != null)
                    {
                        //query sorts the results in DCN order
                        //accumulate the file list of the document (DCN) then yield
                        //Example:(DCN File1),(DCN,File2) and (DCN001, File3)
                        //yield DCN,<File1,File2,File3> 
                        yield return reconversionDocumentBeo;
                    }
                    reconversionDocumentBeo = new ReconversionDocumentBEO()
                                                  {
                                                      DocumentId = docRefId,
                                                      CollectionId = collectionId,
                                                      DCNNumber = dcn
                                                  };
                }
                reconversionDocumentBeo.FileList.Add(dataReader[Constants.ColumnDocText].ToString());
                //add file path to list of file path for the document

            }
            dataReader.Close();
            if (reconversionDocumentBeo != null) yield return reconversionDocumentBeo;
        }


        /// <summary>
        /// Gets the production document list for ID list.
        /// </summary>
        /// <param name="idList">The id list.</param>
        /// <param name="idType">Type of the id.</param>
        /// <param name="matterId">The matter id.</param>
        /// <param name="baseJobConfig">The base job config.</param>
        /// <param name="redactableCollectionId">The redactable collection id.</param>
        /// <param name="jobId"> </param>
        /// <returns></returns>
        public static IEnumerable<ReconversionProductionDocumentBEO> GetProductionDocumentListForIdList(
            List<string> idList, string idType, long matterId,
            ProductionDetailsBEO baseJobConfig, string redactableCollectionId, int jobId)
        {

            //init field id value
            int batesFieldId = -1;
            int batesBeginFieldId = -1;
            int batesEndFieldId = -1;
            int dpnFieldId = -1;

            //bates field: 3004
            var batesField = baseJobConfig.Profile.ProductionFields.FirstOrDefault(f => f.FieldType.DataTypeId == 3004);
            if (batesField != null) batesFieldId = batesField.ID;

            //bates begin: 3005
            var batesBeginField =
                baseJobConfig.Profile.ProductionFields.FirstOrDefault(f => f.FieldType.DataTypeId == 3005);
            if (batesBeginField != null) batesBeginFieldId = batesBeginField.ID;

            //bates end: 3006
            var batesEndField =
                baseJobConfig.Profile.ProductionFields.FirstOrDefault(f => f.FieldType.DataTypeId == 3006);
            if (batesEndField != null) batesEndFieldId = batesEndField.ID;

            //dpn field: 3008
            var dpnField = baseJobConfig.Profile.ProductionFields.FirstOrDefault(f => f.FieldType.DataTypeId == 3008);
            if (dpnField != null) dpnFieldId = dpnField.ID;


            //get the collection id for the native collection
            string nativeCollectionId = baseJobConfig.OriginalCollectionId;


            //find the production collection id at the time of the original production
            string productionCollectionId = baseJobConfig.CollectionId;

            //Get the list of production documents for reprocessing
            var docList = new List<ReconversionProductionDocumentBEO>();

            var vault = VaultRepository.CreateRepository(matterId);

            DataSet dsResult = vault.GetProductionReprocessDocumentList(idList, idType, redactableCollectionId,
                                                                        nativeCollectionId, productionCollectionId,
                                                                        dpnFieldId, batesFieldId, batesBeginFieldId,
                                                                        batesEndFieldId, jobId);

            ReconversionProductionDocumentBEO doc = null;

            //process returned records
            if (dsResult != null && dsResult.Tables.Count > 0)
            {
                foreach (DataRow dr in dsResult.Tables[0].Rows)
                {
                    string docReferenceId = dr[Constants.ColumnDocReferenceId].ToString();

                    if (doc == null || !docReferenceId.Equals(doc.DocumentId))
                    //new document (need to check this due to that multiple rows are returned, one row for each field)
                    {
                        doc = new ReconversionProductionDocumentBEO();
                        doc.DocumentId = docReferenceId;
                        doc.DCNNumber = dr[Constants.DCN].ToString();
                        doc.ProductionPath = dr[Constants.ProductionPath].ToString();
                        docList.Add(doc);
                    }

                    int fieldId = dr[Constants.ColumnFieldId] != DBNull.Value
                                      ? Convert.ToInt32(dr[Constants.ColumnFieldId].ToString())
                                      : -1;
                    string fieldValue = dr[Constants.ColumnFieldvalue] != DBNull.Value
                                            ? dr[Constants.ColumnFieldvalue].ToString()
                                            : "";

                    if (dpnFieldId > 0 && dpnFieldId == fieldId) doc.DocumentProductionNumber = fieldValue;
                    if (batesFieldId > 0 && batesFieldId == fieldId) doc.AllBates = fieldValue;
                    if (batesBeginFieldId > 0 && batesBeginFieldId == fieldId) doc.StartingBatesNumber = fieldValue;
                    if (batesEndFieldId > 0 && batesEndFieldId == fieldId) doc.EndingBatesNumber = fieldValue;
                }


                //if any of the bates or dpn contain comma,  ie, contain multiple bates or dpn
                string bates = docList[0].StartingBatesNumber;
                string dpn = docList[0].DocumentProductionNumber;

                //extrac the correct dpn if dpn contain concatenated ids, which could happen because multiple productions done on 
                //the same collection, with same dpn field name
                //sample dpn sequence: "dd003, dd103, k45k005, k45k105, dd203, kkk001, ddd002, dd303, k45k205"
                if (dpn != null && dpn.Contains(","))
                {
                    RecalculateCorrectDpnNumbers(docList, baseJobConfig,
                                                 vault.GetDocumentCountForCollection(new Guid(productionCollectionId)));
                }

                //extrac the correct bates info if bates contain concatenated ids, which could happen because multiple productions done on 
                //the same collection, with same bates field name
                //if bates is configures, the file name for the production contain the beginBates number. 
                if ((bates != null && bates.Contains(",")))
                {
                    RecalculateCorrectBatesNumbers(docList);
                }

            }

            return docList;
        }

        /// <summary>
        /// Recalculate the correct dpn numbers. This is used for dpn numbers that were concatenated together, during multiple production
        /// </summary>
        /// <param name="docList">docList</param>
        /// <param name="baseJobConfig">baseJobConfig</param>
        /// <param name="productionSetDocCount">productionSetDocCount</param>
        private static void RecalculateCorrectDpnNumbers(List<ReconversionProductionDocumentBEO> docList,
                                                         ProductionDetailsBEO baseJobConfig, int productionSetDocCount)
        {
            string dpnPrefix = baseJobConfig.Profile.DpnPrefix;
            int dpnStart = Convert.ToInt32(baseJobConfig.Profile.DpnStartingNumber);
            int dpnEnd = dpnStart + productionSetDocCount - 1;

            //retrieve the correct dpn for each document for reprocessing
            foreach (var d in docList)
            {
                d.DocumentProductionNumber = ParseForCorrectDpnNumber(d.DocumentProductionNumber, dpnPrefix,
                                                                      dpnStart, dpnEnd);
            }
        }


        /// <summary>
        /// Prase, calcuate and return the correct bates or dpn from a sequence of concatenated values
        /// </summary>
        /// <param name="valuseCsv">valuseCsv</param>
        /// <param name="prefix">prefix</param>
        /// <param name="startNumber">startNumber</param>        
        /// <param name="endNumber">endNumber</param>
        private static string ParseForCorrectDpnNumber(string valuseCsv, string prefix, int startNumber, int endNumber)
        {
            string validValue = null;
            int validCount = 0;
            List<string> values = valuseCsv.Split(',').Select(p => p.Trim()).ToList();

            foreach (var s in values)
            {
                //s could be like "dd005", or "d45df005", ie. prefix can contain numbers
                //so we get the ending number first
                Regex regex = new Regex(@"(\d+)$",
                                        RegexOptions.Compiled |
                                        RegexOptions.CultureInvariant);

                Match match = regex.Match(s);

                var sNumer = match.Groups[1].Value;
                int dpnNumber = Convert.ToInt32(sNumer);

                var sPrefix = s.Substring(0, s.Length - sNumer.Length);

                //valid value must have prefix match and number fall in the range
                if (prefix.Equals(sPrefix) && dpnNumber >= startNumber && dpnNumber <= endNumber)
                {
                    validValue = s;
                    validCount++;
                }
            }

            if (validValue == null)
                throw new EVException().AddUsrMsg("Can not extract correct bates or dpn from sequence: " + valuseCsv);

            if (validCount > 1)
                throw new EVException().AddUsrMsg(
                    "Ambiguous bates or dpn sequence. (more than one possible bates or dpn found:" + valuseCsv);


            //found correct bates or dpn 
            return validValue;
        }

        /// <summary>
        /// Recalculate the correct bates numbers. This is used for bates numbers that were concatenated together, during multiple production
        /// </summary>
        /// <param name="docList">docList</param>
        private static void RecalculateCorrectBatesNumbers(List<ReconversionProductionDocumentBEO> docList)
        {
            //retrieve the correct dpn for each document for reprocessing
            foreach (var d in docList)
            {
                //if bates is configured for a production, the file name of output file is the start bates number
                int batesIndexInSequence = GetIndexInCsvSequence(Path.GetFileNameWithoutExtension(d.ProductionPath),
                                                                 d.StartingBatesNumber);
                d.StartingBatesNumber = d.StartingBatesNumber.Split(',')[batesIndexInSequence];
                d.EndingBatesNumber = d.EndingBatesNumber.Split(',')[batesIndexInSequence];

                //Retrieve all the bates for this documnet
                int allBatesStartIndex = GetIndexInCsvSequence(d.StartingBatesNumber, d.AllBates);
                int allBatesEndIndex = GetIndexInCsvSequence(d.EndingBatesNumber, d.AllBates);

                string[] allBates = d.AllBates.Split(',');
                var sb = new StringBuilder();
                for (int i = allBatesStartIndex; i <= allBatesEndIndex; i++)
                {
                    sb.Append(allBates[i]).Append(",");
                }

                //get all bates. trim the ending comma as well
                d.AllBates = sb.ToString().Substring(0, sb.Length - 1);
            }
        }

        /// <summary>
        /// return the index position of a string in a sequence of comma seperated strings
        /// </summary>
        /// <param name="value">value</param>
        /// <param name="csv">csv</param>    
        ///     
        private static int GetIndexInCsvSequence(string value, string csv)
        {
            int index = -1;
            string[] values = csv.Split(',');
            if (values != null && values.Length >= 0)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i].Trim().Equals(value.Trim())) index = i;
                }
            }

            return index;
        }



        /// <summary>
        /// Get the list of docId values in input file
        /// </summary>
        /// <param name="inputFilePath">Input file path that contain list of DCN</param>
        /// <param name="idFieldName">id field name</param>
        public static List<string> GetDocumentIdListFromFile(string inputFilePath, string idFieldName)
        {
            int idFieldIndex = -1;

            var docIdList = new List<string>();

            using (var readFile = new StreamReader(inputFilePath))
            {
                string line;

                while ((line = readFile.ReadLine()) != null)
                {
                    var row = line.Split(',');
                    if (idFieldIndex < 0) //this is the header row.
                        idFieldIndex = GetDocIdFieldIndex(row, idFieldName);
                    else
                        docIdList.Add(row[idFieldIndex]);
                }
            }

            return docIdList;
        }

        /// <summary>
        /// Get the list of docId values in input file for each dataset name
        /// </summary>
        /// <param name="inputFilePath">Input file path that contain list of doc ids</param>
        /// <param name="idFieldName">id field name</param>
        /// <param name="docSetNameFieldName">document set name field name</param>
        public static Dictionary<string, List<string>> GetDocumentIdListFromFile(string inputFilePath,
                                                                                 string idFieldName,
                                                                                 string docSetNameFieldName)
        {
            int idFieldIndex = -1;
            int dsnFieldIndex = -1;

            var dictDocId = new Dictionary<string, List<string>>();

            using (var readFile = new StreamReader(inputFilePath))
            {
                string line;

                while ((line = readFile.ReadLine()) != null)
                {
                    var row = line.Split(',');
                    if (idFieldIndex < 0) //this is the header row.
                    {
                        idFieldIndex = GetDocIdFieldIndex(row, idFieldName);
                        dsnFieldIndex = GetDocIdFieldIndex(row, docSetNameFieldName);
                    }
                    else
                    {
                        string docSetName = row[dsnFieldIndex]; //get docSetName value
                        if (!dictDocId.Keys.Contains(docSetName)) //check if already exists; if not, create the list
                            dictDocId.Add(docSetName, new List<string>());

                        //add the id to the corresponding id list
                        dictDocId[docSetName].Add(row[idFieldIndex]);
                    }
                }
            }

            return dictDocId;
        }

        /// <summary>
        /// Get the list of DCN values in input file
        /// </summary>
        /// <param name="headerFields">List of header fields</param>
        /// <param name="fieldName">field name to get index for</param>
        private static int GetDocIdFieldIndex(string[] headerFields, string fieldName)
        {
            if (headerFields != null && headerFields.Length > 0)
            {
                //expecting the field has a name of fieldName
                for (int i = 0; i < headerFields.Length; i++)
                {
                    if (fieldName.ToUpper().Equals(headerFields[i].ToUpper()))
                        return i;
                }
            }

            //field not found
            return -1;
        }


    }
}
