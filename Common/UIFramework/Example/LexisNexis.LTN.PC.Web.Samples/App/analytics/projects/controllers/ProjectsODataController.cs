using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using LexisNexis.LTN.PC.Web.Samples.Models;
using Microsoft.Data.OData;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{

    public class ProjectsODataController : ODataController
    {
        private static readonly ODataValidationSettings ValidationSettings = new ODataValidationSettings
        {
            AllowedQueryOptions =
                AllowedQueryOptions.Filter | AllowedQueryOptions.Format | AllowedQueryOptions.InlineCount |
                AllowedQueryOptions.OrderBy | AllowedQueryOptions.Select | AllowedQueryOptions.Skip |
                AllowedQueryOptions.Top
        };

        /// <summary>
        /// Get Projects OData
        /// </summary>
        public IHttpActionResult GetProjectsOData(ODataQueryOptions<Project> queryOptions)
        {
            try
            {
                queryOptions.Validate(ValidationSettings);
            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }
            try
            {
                //TODO : Data will be fetched from WCF Service
                var projects = GetData(); 
                var results = queryOptions.ApplyTo(projects.AsQueryable()) as IEnumerable<Project>;
                return Ok(results);
            }
            catch (Exception ex)
            {

                return InternalServerError(ex);
            }
        }

        private static IEnumerable<Project> GetData()
        {
            //TODO : WCF call fetch projects, below need to be removed once this done.
            return new List<Project>
                                         {
                                             new Project {ProjectId = 1001,ProjectName = "Thoughtworks",MatterName = "ILF",Source = "DS1",CreatedBy = "Paramasx", CreatedOn = DateTime.UtcNow,Documents = 222},
                                             new Project {ProjectId = 1002,ProjectName = "DigitalCam",MatterName = "LAW",Source = "DS3",CreatedBy = "Systemadmin", CreatedOn = DateTime.UtcNow,Documents = 1922},
                                             new Project {ProjectId = 1003,ProjectName = "MultiTiff",MatterName = "DCB",Source = "DS1",CreatedBy = "Paramasx", CreatedOn = DateTime.UtcNow,Documents = 3456},
                                             new Project {ProjectId = 1004,ProjectName = "SingleTiff",MatterName = "Case0908",Source = "DS5",CreatedBy = "niranjax", CreatedOn = DateTime.UtcNow,Documents = 7654},
                                             new Project {ProjectId =1005,ProjectName = "Case4-Thoughtworks",MatterName = "Case2",Source = "DS6",CreatedBy = "Paramasx", CreatedOn = DateTime.UtcNow,Documents = 9865},
                                             new Project {ProjectId = 1006,ProjectName = "OfficeDepot",MatterName = "Classic",Source = "case",CreatedBy = "niranjax", CreatedOn = DateTime.UtcNow,Documents = 6},
                                             new Project {ProjectId = 1007,ProjectName = "Mail vs Thread",MatterName = "Load File",Source = "DS1",CreatedBy = "Paramasx", CreatedOn = DateTime.UtcNow,Documents = 122},
                                             new Project {ProjectId = 1008,ProjectName = "EV ALL",MatterName = "LAW",Source = "DS3",CreatedBy = "Systemadmin", CreatedOn = DateTime.UtcNow,Documents = 1652},
                                             new Project {ProjectId = 1009,ProjectName = "Edocs",MatterName = "Edocs Import",Source = "DS1",CreatedBy = "Paramasx", CreatedOn = DateTime.UtcNow,Documents = 36},
                                             new Project {ProjectId = 1010,ProjectName = "Imageworks",MatterName = "Case0908",Source = "DS5",CreatedBy = "niranjax", CreatedOn = DateTime.UtcNow,Documents = 7004},
                                             new Project {ProjectId =1011,ProjectName = "Case2",MatterName = "Case2",Source = "DS6",CreatedBy = "Paramasx", CreatedOn = DateTime.UtcNow,Documents = 9215},
                                             new Project {ProjectId = 1012,ProjectName = "Target",MatterName = "TG",Source = "Dataset",CreatedBy = "niranjax", CreatedOn = DateTime.UtcNow,Documents = 611}
                                        };
        }
    }
}