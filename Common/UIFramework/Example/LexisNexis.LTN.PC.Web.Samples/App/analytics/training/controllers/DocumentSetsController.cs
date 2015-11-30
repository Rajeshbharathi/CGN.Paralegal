using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using LexisNexis.LTN.PC.Web.Samples.Models;
using Microsoft.Data.OData;
using Newtonsoft.Json;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{

    public class DocumentSetsController : ODataController
    {
        private static readonly ODataValidationSettings ValidationSettings = new ODataValidationSettings {
            AllowedQueryOptions =
                AllowedQueryOptions.Filter | AllowedQueryOptions.Format | AllowedQueryOptions.InlineCount |
                AllowedQueryOptions.OrderBy | AllowedQueryOptions.Select | AllowedQueryOptions.Skip |
                AllowedQueryOptions.Top
        };

        /// <summary>
        /// Gets the document sets.
        /// </summary>
        /// <param name="queryOptions">The query options.</param>
        /// <returns></returns>
        public IHttpActionResult GetDocumentSets(ODataQueryOptions<DocumentSet> queryOptions) {
            try {
                queryOptions.Validate(ValidationSettings);
            }
            catch (ODataException ex) {
                return BadRequest(ex.Message);
            }
            try {
                string json = File.ReadAllText(HttpContext.Current.Server.MapPath("~/app_data/documentsets.json"));
                var list = JsonConvert.DeserializeObject<List<DocumentSet>>(json).AsQueryable();
                var results = queryOptions.ApplyTo(list) as IEnumerable<DocumentSet>;
                return Ok(results);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}