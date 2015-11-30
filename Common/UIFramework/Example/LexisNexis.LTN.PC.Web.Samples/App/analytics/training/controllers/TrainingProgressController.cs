using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{

    public class TrainingProgressController : ApiController
    {
        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns></returns>
        [Route("api/trainingprogress")]
        public HttpResponseMessage GetAll() {
            try {
                var results = new List<TrainingProgressResult>();
                var random = new Random();
                for (int x = 1; x <= 10; x++) {
                    var result = new TrainingProgressResult {Name = "Test " + x, Precision = random.Next(5, 100)};
                    result.Recall = (result.Precision*random.Next(70, 85)/100);
                    result.Accuracy = (result.Precision*random.Next(70, 85)/100);
                    results.Add(result);
                }
                return Request.CreateResponse<IEnumerable<TrainingProgressResult>>(HttpStatusCode.OK, results);
            }
            catch (Exception ex) {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}