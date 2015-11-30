using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{
    public class ControlsetController : ApiController
    {
        /// <summary>
        /// Calculate Sample size
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="datasetId">Dataset Id</param>
        /// <param name="controlSet">Control Set</param>
        [Route("api/controlset/samplesize/{matterId}/{datasetId}")]
        public HttpResponseMessage PostCalculateSampleSize(string matterId, string datasetId, ControlSet controlSet)
        {
            try
            {
                //TODO: Data will be fetched from WCF Service
                var size = 345; 
                return Request.CreateResponse(HttpStatusCode.OK, size);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Create ControlSet
        /// </summary>
        /// <param name="matterId">Matter Id</param>
        /// <param name="datasetId">Dataset Id</param>
        /// <param name="projectId">Project Id</param>
        /// <param name="controlSet">ControlSet</param>
        [Route("api/controlset/{matterId}/{datasetId}/{projectId}")]
        public HttpResponseMessage PostCreateControlset(string matterId, string datasetId, string projectId,ControlSet controlSet)
        {
            try
            {
                //TODO:Controlset will be created by WCF Service
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


    }
}