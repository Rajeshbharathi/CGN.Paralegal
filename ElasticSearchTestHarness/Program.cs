using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGN.Paralegal.BusinessEntities.Search;
using CGN.Paralegal.DAL;

namespace CGN.Paralegal.ElasticSearchTestHarness
{
    class Program
    {
        static void Main(string[] args)
        {

            // Change to your number of menuitems.
            const int maxMenuItems = 3;
            int selector = 0;
            bool good = false;
            while (selector != maxMenuItems)
            {
                Console.Clear();
                DrawTitle();
                DrawMenu(maxMenuItems);
                good = int.TryParse(Console.ReadLine(), out selector);
                if (good)
                {
                    switch (selector)
                    {
                        case 1:
                            AddDocuments();
                            break;
                        case 2:
                            SearchDocuments();
                            break;
                        // possibly more cases here
                        default:
                            if (selector != maxMenuItems)
                            {
                                ErrorMessage();
                            }
                            break;
                    }
                }
                else
                {
                    ErrorMessage();
                }
                Console.ReadKey();
            }
        }

        private static void AddDocuments()
        {
            SearchAdapter.Instance.CreateIndex();
            var documents = new List<PLSearchResult>() {
                new PLSearchResult()
                {
                    PLID = 5,
                    Name = "Martin",
                    Snippet = "Test for martin",
                    Rating = "4"
                },
                new PLSearchResult()
                {
                    PLID = 6,
                    Name = "Luther King",
                    Snippet = "Test for Luther King",
                    Rating = "4"
                }
            
            };
            
            documents.ForEach(act => 
                SearchAdapter.Instance.AddParalegal(act)
            );
            
        }

        private static void SearchDocuments()
        {
            var request = new PLSearchRequest(){
                SearchQuery = "martin"
            };
            var results = SearchAdapter.Instance.Search(request);

                results.ForEach(result => Console.WriteLine("Found Item {0}",result.Name));
        }
        private static void ErrorMessage()
        {
            Console.WriteLine("Typing error, press key to continue.");
        }
        private static void DrawStarLine()
        {
            Console.WriteLine("************************");
        }
        private static void DrawTitle()
        {
            DrawStarLine();
            Console.WriteLine("+++   Elastic Search Test Harness   +++");
            DrawStarLine();
        }
        private static void DrawMenu(int maxitems)
        {
            DrawStarLine();
            Console.WriteLine(" 1. Add Documents");
            Console.WriteLine(" 2. Search");
            // more here
            Console.WriteLine(" 3. Exit program");
            DrawStarLine();
            Console.WriteLine("Make your choice: type 1, 2,... or {0} for exit", maxitems);
            DrawStarLine();
        }


    }
}
