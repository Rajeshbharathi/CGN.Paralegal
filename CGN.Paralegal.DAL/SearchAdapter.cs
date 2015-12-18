using CGN.Paralegal.BusinessEntities.Search;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGN.Paralegal.DAL
{
    public  class SearchAdapter
    {
        static SearchAdapter singleInstance;
        static ElasticClient esClient;
        static ConnectionSettings esconnSetting;
        private SearchAdapter()
        {
            esconnSetting = new ConnectionSettings(
                                new Uri(ESConnectionSetting.Node), "lt028310");
            esClient = new ElasticClient(esconnSetting);
            
        }

        public void CreateIndex()
        {
            var response = esClient.CreateIndex(new CreateIndexRequest("lt028310")
                {
                    IndexSettings = new IndexSettings()
                                        {
                                            NumberOfReplicas = 0,
                                            NumberOfShards = 1
                                        }
                });
        }
        public void AddParalegal(PLSearchResult paralegal)
        {

          var respose = esClient.Index(paralegal, i=> i.Id(paralegal.PLID)
                                                        );
        }

        public List<PLSearchResult> Search(PLSearchRequest request)
        {
            var results = esClient.Search<PLSearchResult>(
                s => s.QueryString(request.SearchQuery)
               /* src => src.Query(
                        q => q.MatchPhrase(request.SearchQuery)
                    )*/
                );

                return results.Documents.ToList<PLSearchResult>();
        }
        public static SearchAdapter Instance
        {
            get {
                if (singleInstance == null)
                    singleInstance = new SearchAdapter();

                return singleInstance; 
                }            
        }
        


    }
}
