#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetRecord.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Cognizant</author>
//      <description>
//          Entity For Reviewset creation
//      </description>
//      <changelog>
//          <date value="12-Jan-2012"></date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces
using System;
using LexisNexis.Evolution.BusinessEntities;
using System.Collections.Generic;
#endregion

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class TagRecord
    {

        public int Id
        {
            get; set;
        }

        public int ParentId
        {
            get;
            set;
        }


        public string Name
        {
            get;
            set;
        }

        public string TagDisplayName
        {
            get;
            set;
        }


        public bool IsOperationTagging
        {
            get; set; 
        }

        public bool IsTagAllFamily
        {
            get; set;
        }

        public bool IsTagAllDuplicates
        {
            get; set;
        } 


        /// <summary>
        /// Gets or sets the review set ID.
        /// </summary>
        public string ReviewSetId { get; set; }

        /// <summary>
        /// Gets or sets the name of the review set.
        /// </summary>
        public string ReviewSetName {get;set;}
                
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string ReviewSetDescription { get; set; }

        /// <summary>
        /// Gets or Sets the Dataset Id
        /// </summary>
         
        public long DatasetId  { get; set; }

        /// <summary>
        /// Gets or Sets the Dataset Id
        /// </summary>
        public long MatterId { get; set; }

        /// <summary>
        /// Gets or Sets the Collection Id
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// Gets or sets the Binder folder Id.
        /// </summary>
        public long BinderFolderId { get; set; }

        /// <summary>
        /// Gets or sets the Binder ID.
        /// </summary>
        public string BinderId { get; set; }

        /// <summary>
        /// Gets or sets the Binder Name.
        /// </summary>
        public string BinderName { get; set; }

        /// <summary>
        /// Gets or sets the number of documents per Review Set.
        /// </summary>
        /// <value>The number of documents.</value>
        public long NumberOfDocuments { get; set; }
                
        /// <summary>
        /// Gets or sets the reviewed.
        /// </summary>
        /// <value>The reviewed.</value>
        public long NumberOfReviewedDocs {get; set;}
               
        /// <summary>
        /// Gets or sets the number of review set.
        /// </summary>
        /// <value>The number of review set.</value>
        public int NumberOfReviewSets {get; set;}

        /// <summary>
        /// Gets or sets the number of batches that documents will be sent to next workers
        /// </summary>
        public int NumberOfBatches { get; set; }
              
        /// <summary>
        /// Gets or sets the number of documents per review set.
        /// </summary>
        /// <value>The number of documents per set.</value>
        public int NumberOfDocumentsPerSet {get; set;}

        /// <summary>
        /// Gets or sets the status ID.
        /// </summary>
        /// <value>The status ID.</value>
        public int StatusId {get; set;}

        /// <summary>
        /// Gets or sets the name of the Review Set Group.
        /// </summary>
        /// <value>The name of the Review Set Group.</value>
        public string ReviewSetGroup { get; set; }

        private List<ReviewsetUserBusinessEntity> reviewSetUserList;
        /// <summary>
        /// Gets and sets the value of ReviewsetUserList.
        /// </summary>
        /// <value>The value of MyStatus</value>
        public List<ReviewsetUserBusinessEntity> ReviewSetUserList
        {
            get 
            {
                if (reviewSetUserList == null)
                {
                    reviewSetUserList = new List<ReviewsetUserBusinessEntity>();
                }
                return reviewSetUserList; 
            }
        }       
        
        /// <summary>
        /// Gets or sets the keep families together
        /// </summary>
        /// <value>Keep families together</value>        
        public bool KeepFamilyTogether {get; set;
        }
        /// <summary>
        /// Gets or sets the keepduplicates together
        /// </summary>
        /// <value>Keep duplicates together</value>
        public bool KeepDuplicatesTogether {get; set;}

        /// <summary>
        /// Gets or sets the reviewset logic.
        /// </summary>
        /// <value>The reviewset logic.</value>        
        public string ReviewSetLogic {get; set;}
        
        /// <summary>
        /// Gets or sets the spliting option.
        /// </summary>
        /// <value>The spliting option.</value>
        public string SplittingOption {get; set;}        
                    
        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate {get; set;}

        /// <summary>
        /// Gets or sets the due date.
        /// </summary>
        /// <value>The due date.</value>
        public DateTime DueDate {get; set;}

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The due date.</value>
        public string Activity {get; set;}

        /// <summary>
        /// Gets or Sets the created user
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or Sets the Search Dataset tags used to created the reviewset
        /// </summary>
        public string SearchQuery { get; set; }
        private List<RVWTagBEO> _dsTags = new List<RVWTagBEO>();
        /// <summary>
        /// dataset tags
        /// </summary>
        public List<RVWTagBEO> DsTags
        {
            get
            {
                return _dsTags;
            }
            set
            {
                _dsTags = value;

            }
        }

        private List<RVWTagBehaviorBEO> m_TagBehaviors;

        /// <summary>
        /// get or set TagBehaviors
        /// Optional during creation
        /// RVWTagBehaviorBEO is set as part of Master data
        /// </summary>
        public List<RVWTagBehaviorBEO> TagBehaviors
        {
            get { return m_TagBehaviors ?? (m_TagBehaviors = new List<RVWTagBehaviorBEO>()); }
        }

        /// <summary>
        /// Gets or sets the actual reviewset id being splitted
        /// </summary>
        public string SplitReviewSetId { get; set; }

        /// <summary>
        /// Gets or sets the name of the review set being splitted.
        /// </summary>
        public string SplitReviewSetName { get; set; }

        /// <summary>
        /// Gets or sets the documents present in the reviewset being splitted.
        /// </summary>
        public int SplitPreDocumentCount { get; set; }

        /// <summary>
        /// Gets or sets the reviewset assigned users list.
        /// </summary>
        public string AssignTo { get; set; }
    }
}
