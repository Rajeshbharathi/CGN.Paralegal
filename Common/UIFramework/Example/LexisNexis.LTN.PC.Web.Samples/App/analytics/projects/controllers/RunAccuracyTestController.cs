using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{
    public class RunAccuracyTestController : ApiController
    {
        /// <summary>
        /// Run Accuracy Test
        /// </summary>
        /// <param name="projectId">Project Id</param>
        /// <param name="controlsetId">ControlSet Id</param>
        [Route("api/runaccuracytest/{projectId}/{controlsetId}")]
        public HttpResponseMessage PostRunAccuracyTest(string projectId, string controlsetId)
        {
            try
            {
                //TODO: Run Accuracy Test will be created by WCF Service
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}