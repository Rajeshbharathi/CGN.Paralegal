#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionValidation.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  ConversionValidationWorker
//      </description>
//      <changelog>
//           <date value="03/1/2012">Bugs Fixed #93417-Made a fix to include conversion validation as part of job progress </date>
//          <date value="05-12-2013">Task # 134432-ADM 03 -Re Convresion</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.External;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using System.Threading;

namespace LexisNexis.Evolution.Worker
{
    public class ConversionValidation: WorkerBase
    {
        INearNativeConverter _nearNativeConverter = null;
        #region Job Framework functions
        protected override void BeginWork()
        {
            _nearNativeConverter = new NearNativeConversionAdapter(true, WorkAssignment.JobId.ToString());
        }

        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            DocumentCollection documentCollection = null;
            #region Extract document details from incoming message object.
            documentCollection = envelope.Body as DocumentCollection;

            #region Assertion
            documentCollection.ShouldNotBe(null);

            #endregion
            try
            {
                //get the documents yet to converted
                List<ConversionDocumentBEO> documentsInConversionQueue = _nearNativeConverter.GetDocumentsQueuedForConversion(WorkAssignment.JobId);
                if(documentsInConversionQueue != null && documentsInConversionQueue.Any())
                {
                    //this is to avoid tight loop which consumes resources, especially network bandwidth
                    int waitTime = GetConversionValidationWaitTime(documentsInConversionQueue);
                    Thread.Sleep(waitTime);
                    InputDataPipe.Purge(); // We don't need more than one "try again later" message in that pipe
                    InputDataPipe.Send(envelope);
                }
                //IncreaseProcessedDocumentsCount(0);
  
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }
       
        protected override void EndWork()
        {
            base.EndWork();
            _nearNativeConverter = null;
        }

        #endregion

        #region Private Methods
        private int GetConversionValidationWaitTime(List<ConversionDocumentBEO> documentsInConversionQueue)
        {
            int waitTime = 0;
            if (documentsInConversionQueue.Count <= 100)
            {
                waitTime = 10;
            }
            else if (documentsInConversionQueue.Count >= 100 && documentsInConversionQueue.Count <= 1000)
            {
                waitTime = 30;
            }
            else if (documentsInConversionQueue.Count > 1000 && documentsInConversionQueue.Count <= 10000)
            {
                waitTime = 60;
            }
            else if (documentsInConversionQueue.Count > 10000)
            {
                waitTime = 120;
            }

            return waitTime * 1000; // Wait time is measured in MILLISECONDS!
        }
        #endregion Private Methods
        #endregion
    }
}
