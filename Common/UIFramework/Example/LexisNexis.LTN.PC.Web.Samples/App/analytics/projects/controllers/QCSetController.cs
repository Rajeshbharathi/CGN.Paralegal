using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{
    public class QcSetController : ApiController
    {
        /// <summary>
        /// Calculate Sample Size
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="datasetId">Dataset Id</param>
        /// <param name="qcSet">QC Set</param>
        [Route("api/qcset/samplesize/{matterId}/{datasetId}")]
        public HttpResponseMessage PostCalculateSampleSize(string matterId, string datasetId, QCSet qcSet)
        {
            try
            {
                //TODO : Data will be fetched from WCF Service
                var size = 345; 
                return Request.CreateResponse(HttpStatusCode.OK, size);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Create QC Set
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="datasetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="qcSet">QC Set</param>
        [Route("api/qcset/{matterId}/{datasetId}/{projectId}")]
        public HttpResponseMessage PostCreateQcSet(string matterId, string datasetId, string projectId, QCSet qcSet)
        {
            try
            {
                //TODO : QC Set will be created by WCF Service
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}