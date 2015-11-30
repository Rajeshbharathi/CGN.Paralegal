# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ExportLoadFilePipeline.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Lexisnexis</author>
//      <description>
//          This file contains the class for export load file pipeline
//      </description>
//      <changelog>
//          <date value="08/12/2013">Bug Fix # 149535</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.JobManagement;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    public class ExportLoadFilePipeline : EVPipeline
    {
        private const string ErrorDescription = "Failure encountered in executing the job. Please refer to overdrive log or contact administrator";
        private const string Description = "Description";
        private const string NumberofDocsSuccessful = "Number of Docs Successful";
        private const string NumberofDocsFailed = "Number of Docs Failed";

        internal override void SetPipelineTypeSpecificParameters(ActiveJob activeJob)
        {
            base.SetPipelineTypeSpecificParameters(activeJob);

            using (var stream = new StringReader(activeJob.BootParameters.ToString()))
            {
                var xmlStream = new XmlSerializer(typeof(ExportLoadJobDetailBEO));
                ExportLoadJobDetailBEO exportBEO = xmlStream.Deserialize(stream) as ExportLoadJobDetailBEO;
            }
        }


        protected override bool Completed()
        {
            base.Completed();

            if (finalizationRequested) // Completed() called for the second time 
            {
                return true; // We can let pipeline to declare completion now
            }

            // We get here if Completed() is called for the first time. 
            // Need to send special message to ExportLoadFileWriter.
            PipelineSection pipelineSection = FindPipelineSection("ExportLoadFileWriter");
            DataPipeName dataPipeName = pipelineSection.DataPipeName;
            using (var dataPipe = new Pipe(dataPipeName))
            {
                dataPipe.Open();

                var envelope = new PipeMessageEnvelope() { Label = "PleaseFinalize" };

                dataPipe.Send(envelope);
            }
            finalizationRequested = true;
            return false; // We cannot let the pipeline to declare completion yet
        }

        // This is the flag which basically checks if Completed() is called for the first time or for the second time
        private bool finalizationRequested = false;
    }
}
