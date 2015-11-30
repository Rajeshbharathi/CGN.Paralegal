
#region File Header
//---------------------------------------------------------------------------------------------------
// <copyright file="ConversionResultsExportJobParam.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kostya/Nagaraju</author>
//      <description>
//          This file contains the ConversionResultsExportJobParam class
//      </description>
//      <changelog>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="10/07/2013">Dev Bug  # 154336 -ADM -ADMIN - 006 - Import /Production Reprocessing reprocess all documents even with filter and all and other migration fixes
//          <date value="12/15/2014">Task # 180591 - Dynamic Reference data issue fix</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
using LexisNexis.Evolution.BusinessEntities.Conversion;
using LexisNexis.Evolution.External.VaultManager;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LexisNexis.Evolution.Worker
{
    using BusinessEntities;
    using Infrastructure;
    using Overdrive;
    using System;
    using System.IO;
    using TraceServices;
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class ConversionResultsExportWorker : WorkerBase
    {
        private IDocumentVaultManager _documentVaultManager;
        private ConversionResultsExportJobParam _conversionResultsExportJobParam;
        private TextWriter _textWriter;
        private FileStream _fileStream;
        private IEnumerator<DocumentConversionLogBeo> _iterator;

        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();

            _conversionResultsExportJobParam =
                Utils.SmartXmlDeserializer(BootParameters) as ConversionResultsExportJobParam;

            //Pre conditions
            _conversionResultsExportJobParam.ShouldNotBe(null);
            _conversionResultsExportJobParam.TargetFileName.ShouldNotBeEmpty();
            _conversionResultsExportJobParam.MatterId.ShouldBeGreaterThan(0);
            _conversionResultsExportJobParam.JobId.ShouldBeGreaterThan(0);

            //documentVaultManager = EVUnityContainer.Resolve<IDocumentVaultManager>(Constants.DocumentVaultManager);
            _documentVaultManager = new DocumentVaultManager();
            _documentVaultManager.Init(_conversionResultsExportJobParam.MatterId);

            var fileInfo = new FileInfo(_conversionResultsExportJobParam.TargetFileName);

            fileInfo.ShouldNotBe(null);

            fileInfo.Directory.Create();

            _fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);

           
            _textWriter = new StreamWriter(_fileStream);

            const string header =
                "DCN,CrossReferenceId,Status,Reason,ErrorDetails,LastModifiedDate,FileSize,MimeType,Documentset,FilePath";
           
            _textWriter.WriteLine(header);

            _iterator = ReadConversionResults().GetEnumerator();
        }

        /// <summary>
        /// Ends the work.
        /// </summary>
        protected override void EndWork()
        {
            base.EndWork();

            if (_textWriter != null)
            {
                _textWriter.Close();
            }

            if (_fileStream != null)
            {
                _fileStream.Close();
            }
        }

        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            var dtStart = DateTime.Now;
            while (_iterator.MoveNext()) 
            {
                WriteRecordsToFile(_iterator.Current);
                if (DateTime.Now - dtStart <= new TimeSpan(0, 0, 1)) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads the conversion results.
        /// </summary>
        /// <returns>conversion results for the job</returns>
        private IEnumerable<DocumentConversionLogBeo> ReadConversionResults()
        {
            switch (_conversionResultsExportJobParam.JobSelectionMode)
            {
                case ReProcessJobSelectionMode.All:
                    if (!string.IsNullOrEmpty(_conversionResultsExportJobParam.Filters))
                    {
                        foreach (var v in _documentVaultManager.GetConversionResultsWithFilters(_conversionResultsExportJobParam.MatterId,
                                                                         _conversionResultsExportJobParam.JobId,null,null,_conversionResultsExportJobParam.Filters))
                        {
                            yield return v;
                        }
                    }
                    else
                    {

                        foreach (var v in
                            _documentVaultManager.GetAllConversionResults(_conversionResultsExportJobParam.MatterId,
                                                                         _conversionResultsExportJobParam.JobId.ToString(
                                                                             CultureInfo.InvariantCulture))
                            )
                        {
                            yield return v;
                        }
                    }
                    break;
                case ReProcessJobSelectionMode.Selected:
                    _conversionResultsExportJobParam.SourceFileName.ShouldNotBeEmpty();
                    var documentIds = Utils.GetColumnListFromFile(_conversionResultsExportJobParam.SourceFileName, "DocId");
                    foreach (var v in
                                _documentVaultManager.GetSpecificConversionResults(_conversionResultsExportJobParam.MatterId,
                                _conversionResultsExportJobParam.JobId.ToString(CultureInfo.InvariantCulture), Utils.ToLong(documentIds))
                            )
                    {
                        yield return v;
                    }
                    break;
            }
        }

        /// <summary>
        /// Writes the records to file.
        /// </summary>
        /// <param name="documentConversionLogBeo">The document conversion log beo.</param>
        private void WriteRecordsToFile(DocumentConversionLogBeo documentConversionLogBeo)
        {
            if (documentConversionLogBeo == null) return;

            //DCN,CrossReferenceId,Status,Reason,LastModifiedDate,FileSize,MimeType,Document set,FilePath

            var documentConversionResult = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                                                         Utils.EscapeCsvText(documentConversionLogBeo.DCN),
                                                         Utils.EscapeCsvText(documentConversionLogBeo.CrossReferenceId),
                                                         Utils.EscapeCsvText(
                                                             documentConversionLogBeo.StatusDisplayText.ToString(
                                                                 CultureInfo.InvariantCulture)),
                                                         Utils.EscapeCsvText(documentConversionLogBeo.ErrorReason),
                                                         Utils.EscapeCsvText(documentConversionLogBeo.ErrorDetails),
                                                         Utils.EscapeCsvText(
                                                             documentConversionLogBeo.LastModifiedDate ?? string.Empty),
                                                         Utils.EscapeCsvText(
                                                             documentConversionLogBeo.Size.ToString(
                                                                 CultureInfo.InvariantCulture)),
                                                         Utils.EscapeCsvText(documentConversionLogBeo.MimeType),
                                                         Utils.EscapeCsvText(documentConversionLogBeo.DocumentSetName),
                                                         Utils.EscapeCsvText(documentConversionLogBeo.FileList != null
                                                                                 ? documentConversionLogBeo.FileList.
                                                                                       FirstOrDefault()
                                                                                 : null));
           _textWriter.WriteLine(documentConversionResult);
           IncreaseProcessedDocumentsCount(1);
        }

    }

       
}
