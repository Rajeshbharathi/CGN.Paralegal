using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{
    public class PredictionSetController : ApiController
    {
        /// <summary>
        /// Create Prediction Set
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="datasetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        [Route("api/predictionset/{matterId}/{datasetId}/{projectId}")]
        public HttpResponseMessage PostCreatePredictionSetJob(string matterId, string datasetId, string projectId)
        {
            try
            {
                //TODO: PredictionSet will be created by WCF Service
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}