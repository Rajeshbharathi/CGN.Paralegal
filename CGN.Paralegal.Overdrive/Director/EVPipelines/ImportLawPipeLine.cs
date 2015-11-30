#region File Header

//---------------------------------------------------------------------------------------------------
// <copyright file="ImportLawPipeline.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Nirajan</author>
//      <description>
//          This file contains the ImportLawPipeline class
//      </description>
//      <changelog>
//          <date value="06-11-2013">Bug Fix  # 142099 and 142102 - Auditing the law import events</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

#endregion

#region

using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.TraceServices;

#endregion

namespace LexisNexis.Evolution.Overdrive
{
    /// <summary>
    ///     ImportLawPipeline
    /// </summary>
    public class ImportLawPipeline : ImportPipeline
    {
        /// <summary>
        ///     Gets or sets the name of the law case.
        /// </summary>
        /// <value>
        ///     The name of the law case.
        /// </value>
        private string LawCaseName { get; set; }

        /// <summary>
        ///     Sets the pipeline type specific parameters.
        /// </summary>
        /// <param name="activeJob">The active job.</param>
        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);
            PipelineType.ToString().ShouldBe("ImportLaw");

            using (var stream = new StringReader(activeJob.BootParameters.ToString()))
            {
                var xmlStream = new XmlSerializer(typeof (LawImportBEO));
                var lawImportBeo = xmlStream.Deserialize(stream) as LawImportBEO;
                if (null == lawImportBeo)
                {
                    return;
                }
                JobName = lawImportBeo.ImportJobName;
                JobTypeName = "Law Import Job";
                LawCaseName = lawImportBeo.LawCaseName;

                ThreadsLinkingRequested = lawImportBeo.CreateThreads;
                FamiliesLinkingRequested = lawImportBeo.CreateFamilyGroups;
                MatterId = lawImportBeo.MatterId;

                // For the purposes of PeriodicPipelineServicingHook in ImportPipeline Overlay for Law import should always be false.
                // Overlay = lawImportBEO.ImportOptions != ImportOptionsBEO.AppendNew;
                IsOverlay = false;
            }
          
        }

       
    }
}
