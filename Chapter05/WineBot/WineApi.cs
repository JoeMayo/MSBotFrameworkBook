using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WineBot
{
    public class WineApi
    {
        const string BaseUrl = "http://services.wine.com/api/beta2/service.svc/json/";

        static HttpClient http;

        public WineApi()
        {
            http = new HttpClient();
        }

        string ApiKey => ConfigurationManager.AppSettings["WineApiKey"];

        public async Task<Refinement[]> GetWineCategoriesAsync()
        {
            return new Refinement[]
            {
                new Refinement { Name = "Red Wine" },
                new Refinement { Name = "White Wine" },
                new Refinement { Name = "Champagne And Sparkling" },
                new Refinement { Name = "Rose Wine" },
                new Refinement { Name = "Dessert, Sherry, and Port" },
                new Refinement { Name = "Sake" },
            };
        }

        public async Task<List[]> SearchAsync(int wineCategory, long rating, bool inStock, string searchTerms)
        {
            var wineList = new List[10];

            for (int i = 0; i < 10; i++)
                wineList[i] = new List { Name = $"Wine Type {i}" };

            return wineList;
        }

        public async Task<byte[]> GetUserImageAsync(string url)
        {
            var responseMessage = await http.GetAsync(url);
            return await responseMessage.Content.ReadAsByteArrayAsync();
        }
    }
}