#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="EDocsOutlookEmailGeneratorWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Keerti/Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  EDocsOutlookEmailGeneratorWorker
//      </description>
//      <changelog>
//           <date value="01/11/2012">Bugs Fixed #95197</date>
//           <date value="01/17/2012">Bugs Fixed #95197-Made a fix for email redact issue by using overdrive non-linear pipeline </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion
#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Worker.Data;
using LexisNexis.Evolution.DocumentExtractionUtilities;
#endregion


namespace LexisNexis.Evolution.Worker
{
    public class EDocsOutlookEmailGeneratorWorker : WorkerBase
    {
        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        protected override void ProcessMessage(PipeMessageEnvelope envelope)
        {
            base.ProcessMessage(envelope);

            try
            {
                EDocsDocumentCollection eDocsDocumentCollection = envelope.Body as EDocsDocumentCollection;
                if (eDocsDocumentCollection != null
                && eDocsDocumentCollection.OutlookMailStoreDataEntity != null
                && eDocsDocumentCollection.OutlookMailStoreDataEntity.Any())
                {
                    foreach (OutlookMailStoreEntity outlookMailStoreEntity in eDocsDocumentCollection.OutlookMailStoreDataEntity)
                    {
                        if (outlookMailStoreEntity.EntryIdAndEmailMessagePairs != null && outlookMailStoreEntity.EntryIdAndEmailMessagePairs.Any())
                        {

                            string pstFilePath = outlookMailStoreEntity.PSTFile.FullName;
                            OutlookMessageExtractor outlookMessageExtractor = new OutlookMessageExtractor();

                            // extract .msg files
                            outlookMailStoreEntity.EntryIdAndEmailMessagePairs
                                .ToList().SafeForEach(keyValues => GenerateMsgFile(outlookMessageExtractor, pstFilePath, keyValues));

                            // framework call for increasing count of documents processed.
                            IncreaseProcessedDocumentsCount(outlookMailStoreEntity.EntryIdAndEmailMessagePairs.Count());

                        }
                    }
                }

                else
                {
                    throw new EVException().AddResMsg(ErrorCodes.EDocsCollectionEntityEmpty);
                }
            }
            catch (Exception ex)
            {
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }

        /// <summary>
        /// Generates the MSG file.
        /// </summary>
        /// <param name="outlookMessageExtractor">The outlook message extractor.</param>
        /// <param name="pstFilePath">The PST file path.</param>
        /// <param name="keyValues">The key values.</param>
        private void GenerateMsgFile(OutlookMessageExtractor outlookMessageExtractor, string pstFilePath, KeyValuePair<string, string> keyValues)
        {
            try
            {
                outlookMessageExtractor.ExtractMessage(pstFilePath, keyValues.Key, keyValues.Value);
            }
            catch (Exception ex)
            {
                if (ex.GetErrorCode().Equals(ErrorCodes.EmptyMsgFileGenerationFaliure))
                {
                    throw;
                }
                ex.AddResMsg(ErrorCodes.EDocsEmailGenaratorWorker_MsgGenerationFailed);
                ReportToDirector(ex);
                ex.Trace().Swallow();
            }
        }
    }
}
