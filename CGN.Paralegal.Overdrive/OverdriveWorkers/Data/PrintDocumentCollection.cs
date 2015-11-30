//---------------------------------------------------------------------------------------------------
// <copyright file="PrintDocumentCollection.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Madhavan Murrali</author>
//      <description>
//          This file contains the PrintDocumentCollection.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.Worker
{
    [Serializable]
    public class PrintDocumentCollection
    {
        public List<DocumentResult> Documents { get; set; }
        public bool isMerge { get; set; }
        public string mergeLimit { get; set; }
        public string mergeType { get; set; }
        public string DatasetName { get; set; }
        public int TotalDocumentCount { get; set; }
    }
}
