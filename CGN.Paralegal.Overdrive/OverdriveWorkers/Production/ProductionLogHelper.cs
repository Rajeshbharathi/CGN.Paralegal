
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.Worker.Data;

namespace OverdriveWorkers.Production
{
    /// <summary>
    /// ProductionLogHelper class is a utility class that has utility methods for production logs 
    /// </summary>
    internal static class ProductionLogHelper
    {
        /// <summary>
        /// Reports the errors.
        /// </summary>
        /// <param name="logPipe"></param>
        /// <param name="errorDocumentDetails">The error document details.</param>
        /// <param name="pipelineId">The pipeline identifier.</param>
        /// <param name="workerId">The worker identifier.</param>
        /// <param name="workerRoleTypeId"></param>
        /// <returns></returns>
        internal static IEnumerable<JobWorkerLog<ProductionParserLogInfo>> SendProductionLogs(Pipe logPipe, IEnumerable<ProductionDocumentDetail> errorDocumentDetails, string pipelineId, string workerId, string workerRoleTypeId)
        {
            if(errorDocumentDetails==null) return null;
             var productionParserLogInfos = new List<JobWorkerLog<ProductionParserLogInfo>>();
            try
            {
               
                var jobRunId = Convert.ToInt64(pipelineId);
                foreach (var errorDocument in errorDocumentDetails)
                {


                    var parserLog = new JobWorkerLog<ProductionParserLogInfo>
                    {
                        JobRunId =jobRunId,
                        CorrelationId = errorDocument.CorrelationId,
                        WorkerRoleType = workerRoleTypeId,
                        WorkerInstanceId = workerId,
                        IsMessage = false,
                        Success = false,
                        CreatedBy = errorDocument.CreatedBy,
                        LogInfo =
                            new ProductionParserLogInfo
                            {
                                Information = errorDocument.ErrorMessage,
                                BatesNumber = errorDocument.AllBates,
                                DatasetName = errorDocument.OriginalDatasetName,
                                DCN = errorDocument.DCNNumber,
                                ProductionDocumentNumber = errorDocument.DocumentProductionNumber,
                                ProductionName = errorDocument.Profile.ProfileName
                            }
                    };
                  productionParserLogInfos.Add(parserLog);
                  if (logPipe == null) 
                      Trace.TraceError("log pipe is empty");
                  logPipe.Open();
                  var message = new PipeMessageEnvelope()
                  {
                      Body = productionParserLogInfos
                  };
                  logPipe.Send(message);
                }
              
            }
            catch (Exception exception)
            {
                var dcns = String.Join(",", errorDocumentDetails.Select(errorDoc => errorDoc.DCNNumber));
                exception.AddDbgMsg("Failed to send documents to Redact-It and failed in reporting errors");
                exception.AddDbgMsg(string.Format("Document Control Numbers:{0}", dcns));
                exception.Trace().Swallow();
            }
            return productionParserLogInfos;
        }

        
    }
}
