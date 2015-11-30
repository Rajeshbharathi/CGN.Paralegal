//---------------------------------------------------------------------------------------------------
// <copyright file="PrintPipeline.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Madhavan Murrali</author>
//      <description>
//          This file contains the PrintPipeline.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//          <date value="08/30/2013">Bug 146858 </date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.Jobs;
using LexisNexis.Evolution.TraceServices;

namespace LexisNexis.Evolution.Overdrive
{
    public class PrintPipeline : EVPipeline
    {
        public string JobName { get; set; }
        public string JobTypeName { get; set; }
        private BulkPrintServiceRequestBEO bulkPrintServiceRequestBEO;


        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);

            using (var stream = new StringReader(activeJob.BootParameters.ToString()))
            {
                var xmlStream = new XmlSerializer(typeof(BulkPrintServiceRequestBEO));
                bulkPrintServiceRequestBEO = xmlStream.Deserialize(stream) as BulkPrintServiceRequestBEO;
                if (bulkPrintServiceRequestBEO != null)
                {
                    JobName = bulkPrintServiceRequestBEO.Name;
                    JobTypeName = "Print Job";
                }
            }
        }

      

        protected override bool Completed()
        {
            base.Completed();
            // Clean source directory...
            var mSharedLocation = bulkPrintServiceRequestBEO.FolderPath;
            var sourceLocation = Path.Combine(Path.Combine(mSharedLocation, bulkPrintServiceRequestBEO.Name), "SourceDirectory");
            //Fix Bulk print failed due to deletion of directory failed. Just log the error and continue if cleanup issue only
            try
            {
                Directory.Delete(sourceLocation, true);
            }
            catch (IOException ioEx)
            {

                Tracer.Info("Print Validation Worker - IO Exception - {0}", ioEx.Message);
                ioEx.AddDbgMsg("Directory = {0}", sourceLocation).Trace().Swallow();
            }
            //Update job completion status
            DatabaseBroker.UpdateTaskCompletionStatus(Convert.ToInt32(PipelineId));
            return true;
        }


        /// <summary>
        /// Delete the folder.
        /// </summary>
        /// <param name="folderToCleanUp">Folder to clean up</param>
        /// <returns>Success or failure</returns>
        private void SafeDeleteFolder(string folderToCleanUp)
        {

            if (Directory.Exists(folderToCleanUp))
            {
                Directory.Delete(folderToCleanUp, true);
            }

        }

    }
}
