    using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{

    //NOTE: This is all test code for the client app. Not meant for production use.
    public class ProjectsController : ApiController
    {
        [Route("api/projects/summary/{id}")]
        public HttpResponseMessage GetProjectSummary(int id)
        {
            var response = new
            {
                SourceDataSet = "DS4 Tag - Extracted Text Tag",
                TotalDocs = 67890,
                CrossRefField = "DCN",
                ControlSet =
                    "Statistical Sampling Method<br/>95% Confidence<br/>2.5% Margin of Error<br/>Stratify by Custodian",
                DatabaseServer = "10.196.116.230",
                AnalyticsServer = "10.196.116.231"
            };
            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, response);
        }

        [Route("api/projects/{id}")]
        public HttpResponseMessage GetProject(int id)
        {
            try
            {
                var response = new
                {
                    Id = id,
                    Name = "AR - DS4",
                    Description = "Assisted review project for DS4 dataset in Matter 4",
                    DocSource = "Tag",
                    Tag = "Extracted",
                    IdentifyRepeatedContent = true,
                    Confidence = 95,
                    MarginOfError = 2.5,
                    StratifyByCustodian = true,
                    CustodianField = "Custodian",
                    SampleSize = 1200,
                    LimitExamples = true,
                    NumOfExamples = 2000
                };
                return Request.CreateResponse<dynamic>(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/projects/{id}")]
        public HttpResponseMessage DeleteProject(int id)
        {
            try
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/projects/{id}")]
        public HttpResponseMessage PutProject(Project project)
        {
            try
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/projects/{id}")]
        public HttpResponseMessage PostProject(Project project)
        {
            try
            {
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/projects/samplesize")]
        public HttpResponseMessage GetCalculateSampleSize(Project project)
        {
            try
            {
                throw new Exception("while calculating sample size");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
       
    }
}