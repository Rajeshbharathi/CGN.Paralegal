using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGN.Paralegal.BusinessEntities.Search;
using CGN.Paralegal.DAL;

namespace CGN.Paralegal.Business.Search
{
    public class SearchBO
    {

        private static string GetSearchText(PLSearchRequest srcReq )
        {
            var queryString = srcReq.SearchQuery;
            var partsOfString = queryString.Split(':');
           return partsOfString.Count() < 2 ?  null: partsOfString[1];
            
        }
        private static AreaOfPractise  ConvertAOP(Area_of_Law AOP)
        {
            return AOP.toAOPEntity();
        }

        private static BusinessEntities.Search.Location ConvertLocation(DAL.Location location)
        {
            return location.toLocation();
        }
        public static List<AreaOfPractise> GetAllAOP()
        {
            var plEntity = new PLMasterEntities();
            var searchResults = plEntity.Area_of_Law.ToList();
            return searchResults.ConvertAll<AreaOfPractise>(new Converter<Area_of_Law, AreaOfPractise>(ConvertAOP));
        }

        public static List<BusinessEntities.Search.Location> GetAllCities()
        {
            var plEntity = new PLMasterEntities();
            var searchResults = plEntity.Locations.ToList();
            return searchResults.ConvertAll<BusinessEntities.Search.Location>(
                new Converter<DAL.Location, BusinessEntities.Search.Location>(ConvertLocation));
        }

        public static List<PLSearchResult> GetParalegalByAOP(PLSearchRequest srcRequest)
        {
            var plEntity = new PLMasterEntities();
            var srcQuery ="";
            var outList = new List<PLSearchResult>();

            if((srcQuery = GetSearchText(srcRequest)) == null) return null;

            if (!plEntity.PLByAOPs.Select(area => area.Law_name == srcQuery).Any()) return null;

            var searchResults = plEntity.PLByAOPs.Where(area => area.Law_name == srcQuery);
            
            foreach (var srcresult in searchResults)
                outList.Add(srcresult.toSearchResult());
            return outList;
            //if(plEntity.Area_of_Law.Join())
        }

        public static List<PLSearchResult> GetParalegalByCity(PLSearchRequest srcRequest)
        {
            var plEntity = new PLMasterEntities();
            var srcQuery = "";
            var outList = new List<PLSearchResult>();

            if ((srcQuery = GetSearchText(srcRequest)) == null) return null;

            if (!plEntity.PLByCities.Select(area => area.Location_name == srcQuery).Any()) return null;

            var searchResults = plEntity.PLByCities.Where(area => area.Location_name == srcQuery);

            foreach (var srcresult in searchResults)
                outList.Add(srcresult.toSearchResult());
            return outList;
            
        }

        public static PLSearchResult GetParalegalByExperience(PLSearchRequest srcRequest)
        {
            throw new NotImplementedException();
        }

        public static PLSearchResult GetParalegalByRating(PLSearchRequest srcRequest)
        {
            throw new NotImplementedException();
        }

        
    }
}
