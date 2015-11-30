#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ReviewsetSearchRecord.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Cognizant</author>
//      <description>
//          Entity For Reviewset Document Search Worker
//      </description>
//      <changelog>
//          <date value="12-Jan-2012"></date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

#region Namespaces
using System;
using LexisNexis.Evolution.BusinessEntities;
using System.Collections.Generic;
using LexisNexis.Evolution.DataContracts;
#endregion
namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ReviewsetSearchRecord
    {
        /// <summary>
        /// This contains the reviewset creation details and description
        /// </summary>
        public ReviewsetRecord ReviewsetDetails { get; set; }

        /// <summary>
        /// This contains the document search entity
        /// </summary>
        public DocumentQueryEntity QueryEntity { get; set; }

        /// <summary>
        /// This contains the total document count from the search performed in startup worker
        /// </summary>
        public int TotalDocumentCount { get; set; }
    }
}
