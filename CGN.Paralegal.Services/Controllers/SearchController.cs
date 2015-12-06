using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CGN.Paralegal.SearchContracts.Search;
using CGN.Paralegal.Business.Search;
using CGN.Paralegal.ServiceContracts.Search;

namespace CGN.Paralegal.Services.Controllers
{
    public class SearchController : ApiController
    {
        [Route("api/search/byAOP/{AOPName}") ]
        public List<PLSearchResult> GetPLByAOP(string AOPName)
        {
            var query = new CGN.Paralegal.BusinessEntities.Search.PLSearchRequest();
            var outlist = new PLSearchResult();
            query.SearchQuery = String.Format("{0}:{1}", "AOP", AOPName);
            var srcResult = SearchBO.GetParalegalByAOP(query);

            return srcResult.ConvertAll<PLSearchResult>(new Converter<CGN.Paralegal.BusinessEntities.Search.PLSearchResult,PLSearchResult>(ConvertToSearchResult));
        }

        [Route("api/search/byCity/{CityName}")]
        public List<PLSearchResult> GetPLByCity(string CityName)
        {
            var query = new CGN.Paralegal.BusinessEntities.Search.PLSearchRequest();
            query.SearchQuery = String.Format("{0}:{1}", "AOP", CityName);
            var srcResult = SearchBO.GetParalegalByCity(query);
            return srcResult.ConvertAll<PLSearchResult>(new Converter<CGN.Paralegal.BusinessEntities.Search.PLSearchResult,PLSearchResult>(ConvertToSearchResult));
        }

        [Route("api/search/getallaop")]
        public List<AreaOfPractise> GetAllAOPs()
        {
            var srcResult = SearchBO.GetAllAOP();
            return srcResult.ConvertAll<AreaOfPractise>(
                new Converter<BusinessEntities.Search.AreaOfPractise, AreaOfPractise>
                    (ConvertToSrcAOP));
        }

        [Route("api/search/getallcities")]
        public List<Location> GetAllCities()
        {
            var srcResult = SearchBO.GetAllCities();
            return srcResult.ConvertAll<Location>(
                new Converter<BusinessEntities.Search.Location, Location>
                    (ConvertToSrcLocation));
        }
        private AreaOfPractise ConvertToSrcAOP(BusinessEntities.Search.AreaOfPractise AOP)
        {
            return AOP.toAOP();
        }
        private Location ConvertToSrcLocation(BusinessEntities.Search.Location location)
        {
            return location.toLocation();
        }
        private PLSearchResult ConvertToSearchResult( CGN.Paralegal.BusinessEntities.Search.PLSearchResult searchResult)
        {
            return searchResult.toSearchResult();
        }



    }
}
