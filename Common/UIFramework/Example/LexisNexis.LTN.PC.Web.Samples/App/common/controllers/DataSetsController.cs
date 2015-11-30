using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;
using Newtonsoft.Json;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{

    //NOTE: This is all test code for the client app. Not meant for production use.
    public class DataSetsController : ApiController
    {
        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns></returns>
        [Route("api/datasets")]
        public HttpResponseMessage GetAll() {
            try {
                var results = Fetch("Name", 0, 30);
                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Fetches the specified order by.
        /// </summary>
        /// <param name="orderBy">The order by.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="take">The take.</param>
        /// <returns></returns>
        private static IEnumerable<DataSet> Fetch(string orderBy, int skip = 0, int take = 10)
        {
            var matters = (from p in GetAllDatasets().OrderBy(orderBy).Skip(skip).Take(take) select p).ToList();
            return matters;
        }

        /// <summary>
        /// Gets all datasets.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<DataSet> GetAllDatasets() {
            string json = File.ReadAllText(HttpContext.Current.Server.MapPath("~/app_data/projects.json"));
            var projects = JsonConvert.DeserializeObject<List<Project>>(json);
            var result = projects.Select(p => new DataSet {Name = p.Source});
            return result.ToList();
        }
    }
}