# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="JobSearchHan.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Srini</author>
//      <description>
//          Actual back end  process which does the bulk tagging
//      </description>
//      <changelog>
//	        <date value="12-19-2011">Bug Fix #81330</date>
//          <date value="06/05/2012">BugFix#100563,100565,100575,100621 and 99768</date>
//          <date value="06/07/2012">Task # 101476(CR BVT Fixes)</date>
//          <date value="11/20/2012">Task # 113201(BVT Fixes)</date>
//          <date value="02/13/2013">Bug Fix #127361</date>
//          <date value="02/16/2013">Bug Fix #127175</date>
//          <date value="07/24/2013">Bug # 142090 - [Bulk Printing]: Bulk print job is getting failed when a field with blank value is chosen for file 
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.ServiceContracts;
using LexisNexis.Evolution.ServiceImplementation;
using LexisNexis.Evolution.Infrastructure.Common;

namespace LexisNexis.Evolution.BatchJobs.Utilities
{
	public class JobSearchHandler
	{
		///Constants
		internal const int MaxDocChunkSize = 500;
		internal const int MaxBinStatesSize = 100;

		/// <summary>
		/// holds one instance of the search service
		/// Methods Used 
		/// 1.GetSearchResultsInternal
		/// 2.GetDocumentWithMatchContext
		/// 3.GetBinStates
		/// 4.GetFamilyDocuments
		/// 5.GetDocumentCount
		/// </summary>
		private static readonly IRvwReviewerSearchService RvwSearchServiceInstance = null;


		/// <summary>
		/// 
		/// </summary>
		public static IRvwReviewerSearchService RvwReviewerSearchServiceInstance
		{
			get
			{
				return RvwSearchServiceInstance ?? new RVWReviewerSearchService();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="documentQueryEntity"></param>
		/// <returns></returns>
		public static ReviewerSearchResults GetSearchResults(DocumentQueryEntity documentQueryEntity)
		{
			documentQueryEntity.QueryObject.LogSearchHistory = false;
			return ConvertRvwReviewerSearchResultsToReviewerSearchResults(RvwReviewerSearchServiceInstance.GetDocumentResults(documentQueryEntity));
		}

	    /// <summary>
	    /// 
	    /// </summary>
	    /// <param name="documentQueryEntity"></param>
	    /// <returns></returns>
	    public static ReviewerSearchResults GetDocuments(DocumentQueryEntity documentQueryEntity)
		{
	        var resultDocuments = new List<ResultDocument>();
			var rvwReviewerSearchResults = RvwReviewerSearchServiceInstance.GetSearchResults(documentQueryEntity);
			resultDocuments.AddRange(rvwReviewerSearchResults.Documents);
			rvwReviewerSearchResults.Documents.RemoveRange(0, rvwReviewerSearchResults.Documents.Count());
			rvwReviewerSearchResults.Documents.AddRange(resultDocuments);
		  
			return ConvertRvwReviewerSearchResultsToReviewerSearchResults(rvwReviewerSearchResults);
	}

        /// <summary>
        /// Gets all documents.
        /// </summary>
        /// <param name="documentQueryEntity">The document query entity.</param>
        /// <param name="includeFamilies">if set to <c>true</c> [include families].</param>
        /// <returns></returns>
	    public static ReviewerSearchResults GetAllDocuments(DocumentQueryEntity documentQueryEntity, bool includeFamilies)
		{
			//step 1:
			if (includeFamilies)
			{
              
				var nonFamilyDocQuery = new Query(EVSearchSyntax.NotContent + EVSystemFields.FamilyId.ToLower());
				documentQueryEntity.QueryObject.QueryList.Add(nonFamilyDocQuery);
			}

			documentQueryEntity.DocumentStartIndex = 0;
			documentQueryEntity.DocumentCount = GetMaximumDocumentChunkSize();

			RvwReviewerSearchResults rvwReviewerSearchResults = null;
			var resultDocuments = new List<ResultDocument>();
			bool bDocumentsPresent = true;

			while (bDocumentsPresent)
			{
				rvwReviewerSearchResults = RvwReviewerSearchServiceInstance.GetSearchResults(documentQueryEntity);
				documentQueryEntity.DocumentStartIndex = documentQueryEntity.DocumentStartIndex + documentQueryEntity.DocumentCount;
				if (rvwReviewerSearchResults.Documents.Count <= 0 || rvwReviewerSearchResults.Documents.Count < documentQueryEntity.DocumentCount)
					bDocumentsPresent = false;
				resultDocuments.AddRange(rvwReviewerSearchResults.Documents);
			}

			rvwReviewerSearchResults.Documents.RemoveRange(0, rvwReviewerSearchResults.Documents.Count);

			if (!includeFamilies)
			{
				rvwReviewerSearchResults.Documents.AddRange(resultDocuments);
				return ConvertRvwReviewerSearchResultsToReviewerSearchResults(rvwReviewerSearchResults);
			}


			//Step 2: get the bin states
			var binQueryEntity = new BinQueryEntity()
			{
				BinField = EVSystemFields.FamilyId.ToLower(),
				BinCount = GetMaximumBinStatesSize(),
				SearchObject = new SearchQueryEntity
				{
					MatterId = documentQueryEntity.QueryObject.MatterId,
					DatasetId = documentQueryEntity.QueryObject.DatasetId,
					ReviewsetId = documentQueryEntity.QueryObject.ReviewsetId,
					IsConceptSearchEnabled = documentQueryEntity.QueryObject.IsConceptSearchEnabled,
				}
			};
			binQueryEntity.SearchObject.QueryList.Add(documentQueryEntity.QueryObject.QueryList[0]);

			foreach (FamilyDocumentsQueryEntity familyDocumentsQueryEntity in from binstate in RvwReviewerSearchServiceInstance.GetBinStates(binQueryEntity).Bins
																			  where !String.IsNullOrEmpty(binstate.BinValue)
																			  select new FamilyDocumentsQueryEntity
																						 {
																							 MatterId = documentQueryEntity.QueryObject.MatterId,
																							 DatasetId = documentQueryEntity.QueryObject.DatasetId,
																							 ReviewsetId = documentQueryEntity.QueryObject.ReviewsetId,
																							 FamilyId = binstate.BinValue
																						 })
			{
				rvwReviewerSearchResults = RvwReviewerSearchServiceInstance.GetFamilyDocuments(familyDocumentsQueryEntity);
				resultDocuments.AddRange(rvwReviewerSearchResults.Documents);
			}
			rvwReviewerSearchResults.Documents.RemoveRange(0, rvwReviewerSearchResults.Documents.Count);
			rvwReviewerSearchResults.Documents.AddRange(resultDocuments);
			return ConvertRvwReviewerSearchResultsToReviewerSearchResults(rvwReviewerSearchResults);
		}

        /// <summary>
        /// This method performs the query search and returns the count of requested page results
        /// </summary>
        /// <param name="searchQueryEntity">The search query entity.</param>
        /// <returns></returns>
	  
	    public static long GetSearchResultsCount(SearchQueryEntity searchQueryEntity)
		{
			searchQueryEntity.LogSearchHistory = false;
			return RvwReviewerSearchServiceInstance.GetDocumentCount(searchQueryEntity);
		}

	    /// <summary>
	    /// Filter and get the final list of documents required
	    /// </summary>
	    /// <param name="documentListDetails">Object containing filter parameters</param>
	    /// <returns>List of Filtered documents</returns>
	    public static List<FilteredDocumentBusinessEntity> GetFilteredListOfDocuments(DocumentQueryEntity documentQueryEntity, DocumentOperationBusinessEntity documentListDetails)
		{
			List<FilteredDocumentBusinessEntity> filteredDocuments = null;
			if (documentListDetails != null)
			{
				switch (documentListDetails.GenerateDocumentMode)
				{
					case DocumentSelectMode.QueryAndExclude:
						{
							filteredDocuments = FetchFilteredSearchResultDocuments(documentQueryEntity,
								documentListDetails.DocumentsToExclude, true);
							break;
						}
					case DocumentSelectMode.UseSelectedDocuments:
						{
							filteredDocuments = FetchFilteredSearchResultDocuments(documentQueryEntity,
								documentListDetails.SelectedDocuments, false);
							break;
						}
				}
			}
			return filteredDocuments;
		}

		///// <summary>
		///// Fetch filtered list of search results, given the search context
		///// </summary>
		///// <param name="searchContext">Search context to get all search results</param>
		///// <param name="documentIds">List of document Ids to be excluded from the list</param>
		///// <param name="exclude">
		///// true if documentIds contain documents to be excluded, false if documentIds contain only the
		///// documents to be selected from search results and returned
		///// </param>
		///// <returns>Filtered list of documents</returns>
		public static List<FilteredDocumentBusinessEntity> FetchFilteredSearchResultDocuments(DocumentQueryEntity documentQueryEntity, List<string> documentIds, bool exclude)
		{
			List<FilteredDocumentBusinessEntity> filteredDocuments = new List<FilteredDocumentBusinessEntity>();
			if (!documentQueryEntity.OutputFields.Exists(x => string.Compare(x.FieldName, EVSystemFields.ReviewSetId, true) == 0))
			{
				documentQueryEntity.OutputFields.Add(new Field { FieldName = EVSystemFields.ReviewSetId });
			}
			//Fetch search results - initially fetches only first 10 documents
			ReviewerSearchResults searchResult;
			searchResult = GetAllDocuments(documentQueryEntity, false);
			if (searchResult != null)
			{
				//Filter search results
				if (exclude)
				{
					//Filter documents - Exclude documents in excludedDocuments from search result documents
					if (documentIds != null && documentIds.Count > 0)
					{
						filteredDocuments = searchResult.ResultDocuments.Where(x => documentIds.Find(y => string.Compare(x.DocumentID, y, true) == 0) == null).Select(
							z => new FilteredDocumentBusinessEntity()
							{
								Id = z.DocumentID,
								MatterId = z.MatterID.ToString(),
								CollectionId = z.CollectionID,
								IsLocked = z.IsLocked,
								DuplicateId = GetDuplicateIdOfDocument(z.Fields),
								DCN = z.DocumentControlNumber,
								FamilyId = z.FamilyID
							}).ToList();
					}
					else
					{
						searchResult.ResultDocuments.SafeForEach(o => filteredDocuments.Add(new FilteredDocumentBusinessEntity
						{
							Id = o.DocumentID,
							MatterId = o.MatterID.ToString(),
							CollectionId = o.CollectionID,
							IsLocked = o.IsLocked,
							DuplicateId = GetDuplicateIdOfDocument(o.Fields),
							DCN = o.DocumentControlNumber,
							FamilyId = o.FamilyID
						}));
					}
				}
				else
				{
					if (documentIds != null && documentIds.Count > 0)
					{
						//Filter documents - Select documents in selectedDocumentIds list from search result documents
						filteredDocuments = searchResult.ResultDocuments.Where(x => documentIds.Find(y => string.Compare(x.DocumentID, y, true) == 0) != null).Select(
							z => new FilteredDocumentBusinessEntity()
							{
								Id = z.DocumentID,
								MatterId = z.MatterID.ToString(),
								CollectionId = z.CollectionID,
								IsLocked = z.IsLocked,
								DuplicateId = GetDuplicateIdOfDocument(z.Fields),
								DCN = z.DocumentControlNumber,
								FamilyId = z.FamilyID
							}).ToList();
					}
				}
				filteredDocuments.ForEach(document =>
					{
						DocumentResult matchedDocument = searchResult.ResultDocuments.Find(x => string.Compare(x.DocumentID, document.Id, true) == 0);
						FieldResult reviewSetField = matchedDocument != null ? matchedDocument.Fields.Find(x => string.Compare(x.Name, EVSystemFields.ReviewSetId, true) == 0) : null;
						document.ReviewsetId = reviewSetField != null ? reviewSetField.Value : string.Empty;
					}
				);
			}
			return filteredDocuments;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rvwReviewerSearchResults"></param>
		/// <returns></returns>
		private static ReviewerSearchResults ConvertRvwReviewerSearchResultsToReviewerSearchResults(RvwReviewerSearchResults rvwReviewerSearchResults)
		{
			QueryContainerBEO queryBEO = new QueryContainerBEO();
			queryBEO.QuerySearchTerms.AddRange(rvwReviewerSearchResults.MatchContextQueries);

			ReviewerSearchResults reviewerSearchResults = new ReviewerSearchResults()
			{
				TotalRecordCount = rvwReviewerSearchResults.Documents.Count,
				TotalHitCount = rvwReviewerSearchResults.TotalHitResultCount,
				SearchRequest = new RVWSearchBEO
				{
					QueryContainerEntity = queryBEO
				}
			};

			foreach (ResultDocument resultDocument in rvwReviewerSearchResults.Documents)
			{
				reviewerSearchResults.ResultDocuments.Add(ConvertResultDocumentToDocumentResult(resultDocument));
			}
			return reviewerSearchResults;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rvwReviewerSearchResults"></param>
		/// <returns></returns>
		private static DocumentResult ConvertResultDocumentToDocumentResult(ResultDocument resultDocument)
		{
			DocumentResult docResult = new DocumentResult
			{
				DocumentID = resultDocument.DocumentId.DocumentId,
				IsLocked = resultDocument.IsLocked,
				RedactableDocumentSetId = resultDocument.RedactableDocumentSetId,
				MatterID = long.Parse(resultDocument.DocumentId.MatterId),
				CollectionID = resultDocument.DocumentId.CollectionId,
				//family id is needed so that bulk tagging/ reviweset creation can be done for family options
				FamilyID = resultDocument.DocumentId.FamilyId,
				ParentID = resultDocument.DocumentId != null && resultDocument.DocumentId.Parent != null ? resultDocument.DocumentId.Parent.DocumentId : null
			};
			//ToDo:Why after overlay , two dcn fields are returned here 
			//One with dcn value and other with empty value 
			foreach (FieldResult fieldResult in resultDocument.FieldValues.Select(ConvertDocumentFieldToFieldResult))
			{
				docResult.Fields.Add(fieldResult);
				if (fieldResult.DataTypeId == 3000)
					docResult.DocumentControlNumber = string.IsNullOrEmpty(docResult.DocumentControlNumber)
														  ? fieldResult.Value
														  : docResult.DocumentControlNumber;
			}
			return docResult;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rvwReviewerSearchResults"></param>
		/// <returns></returns>
		private static FieldResult ConvertDocumentFieldToFieldResult(DocumentField docField)
		{
			FieldResult newField = new FieldResult
			{
				Name = docField.FieldName,
				Value = docField.Value,
				ID = Convert.ToInt32(docField.Id),
				DataTypeId = Convert.ToInt32(docField.Type)
			};
			return newField;
		}

		///// <summary>
		///// Get duplicate id of document
		///// </summary>
		///// <param name="documentFieldValues">Field values of document</param>
		///// <returns>Duplicate Id</returns>
		private static string GetDuplicateIdOfDocument(List<FieldResult> documentFieldValues)
		{
			string duplicateId = string.Empty;
			FieldResult duplicateField = documentFieldValues.Find(x => string.Compare(x.Name, EVSystemFields.Duplicate.ToLower(), true) == 0);
			if (duplicateField != null)
			{
				duplicateId = duplicateField.Value;
			}
			return duplicateId;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private static int GetMaximumDocumentChunkSize()
		{
			try
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings.Get("SEARCH_MAX_CHUNKSIZE"));
			}
			catch (Exception)
			{
				return MaxDocChunkSize;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private static int GetMaximumBinStatesSize()
		{
			try
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings.Get("SEARCH_MAX_BINSTATESIZE"));
			}
			catch (Exception)
			{
				return MaxBinStatesSize;
			}
		}
	}
}
