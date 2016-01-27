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

        public static List<PLDetail> GetTop10PLs()
        {
            var plEntity = new PLMasterEntities();
            var searchResults = plEntity.Para_legal.Take(10).ToList();

            var plDetails = searchResults.ConvertAll<PLDetail>(fn =>
            {
                var plDetail = new PLDetail()
                {
                    ParalegalId = fn.Para_legal_id,
                    Name = fn.Para_legal_name
                };

                return plDetail;
            });
            return plDetails; 

        }

        public static List<AreaOfPractise> GetTop10AOP()
        {
            var plEntity = new PLMasterEntities();
            var searchResults = plEntity.Area_of_Law.Take(10).ToList();
            var allaops = searchResults.ConvertAll<AreaOfPractise>(new Converter<Area_of_Law, AreaOfPractise>(ConvertAOP));
            return allaops.Take<AreaOfPractise>(10).ToList<AreaOfPractise>();
        }

        public static List<BusinessEntities.Search.Location> GetTop10Cities()
        {
            var plEntity = new PLMasterEntities();
            var searchResults = plEntity.Locations.Take(10).ToList();
            var allCities = searchResults.ConvertAll<BusinessEntities.Search.Location>(
                new Converter<DAL.Location, BusinessEntities.Search.Location>(ConvertLocation));
            return allCities.Take<BusinessEntities.Search.Location>(10).ToList<BusinessEntities.Search.Location>();

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
