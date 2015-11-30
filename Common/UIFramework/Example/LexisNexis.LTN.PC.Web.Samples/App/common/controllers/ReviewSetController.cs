using LexisNexis.LTN.PC.Web.Samples.models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace LexisNexis.LTN.PC.Web.Samples.App.common.controllers
{
   
    //NOTE: This is all test code for the client app. Not meant for production use.
    public class ReviewSetController : ApiController
    {
        [Route("api/reviewset")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                string json = File.ReadAllText(HttpContext.Current.Server.MapPath("~/app_data/reviewSet.json"));
                var result = JsonConvert.DeserializeObject<List<Review>>(json);
              

                return Request.CreateResponse<dynamic>(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

       
    }

}