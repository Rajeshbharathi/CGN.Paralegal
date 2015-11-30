#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionDocCollection.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Henry</author>
//      <description>
//          This file contains all the  methods related to  ConversionDocCollection
//      </description>
//      <changelog>
//          <date value="05-15-2013">Initial: Reconversion Processing</date>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace OverdriveWorkers.Data
{
    [Serializable]
    public class ConversionDocCollection
    {
        /// <summary>
        /// Gets the documents.
        /// </summary>
        /// <remarks></remarks>
        public IEnumerable<ReconversionDocumentBEO> Documents { get; set; }

        /// <summary>
        /// Gets the DataSet.
        /// </summary>
        /// <remarks></remarks>
        public DatasetBEO DataSet { get; set; }

        /// <summary>
        /// Conversion configuration
        /// </summary>
        /// <remarks></remarks>
        public ConversionReprocessJobBeo JobConfig { get; set; }

        /// <summary>
        /// Base Job configuration
        /// </summary>
        /// <remarks></remarks>
        public Object BaseJobConfig { get; set; }

        /// <summary>
        /// Base Job Type Id
        /// </summary>
        /// <remarks></remarks>
        public int BaseJobTypeId { get; set; }

        /// <summary>
        /// user selected path to store heartbeat files
        /// </summary>
        /// <remarks></remarks>
        public string HeartbeatFilePath { get; set; }

        /// <summary>
        /// Get default heartbeat file path, which is HearbeatFilePath+DCNNumber_heartbeat.txt
        /// </summary>
        /// <remarks></remarks>
        public string GetDefaultHeartbeatFileFullPath(ReconversionDocumentBEO document)
        {
            if (document == null || document.DCNNumber == null) return "";


            
            if (string.IsNullOrEmpty(HeartbeatFilePath))
                throw new EVException().AddUsrMsg("Must specified root path for heartbeat files");

            //everything is good. push for conversion
            if (HeartbeatFilePath.EndsWith(@"\"))
            {
                return HeartbeatFilePath + document.DCNNumber +  "_heartbeat.txt";
            }

            return HeartbeatFilePath +@"\" +document.DCNNumber + "_heartbeat.txt";

            
        }

        
    }

}
