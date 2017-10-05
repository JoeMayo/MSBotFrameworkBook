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
            const int WineTypeID = 4;
            string url = BaseUrl + "categorymap?filter=categories(490+4)&apikey=" + ApiKey;

            string result = await http.GetStringAsync(url);

            var wineCategories = JsonConvert.DeserializeObject<WineCategories>(result);

            var categories =
                (from cat in wineCategories.Categories
                 where cat.Id == WineTypeID
                 from attr in cat.Refinements
                 where attr.Id != WineTypeID
                 select attr)
                .ToArray();

            return categories;
        }

        public async Task<List[]> SearchAsync(int wineCategory, long rating, bool inStock, string searchTerms)
        {
            string url = 
                $"{BaseUrl}catalog" +
                $"?filter=categories({wineCategory})" +
                $"+rating({rating}|100)" +
                $"&inStock={inStock.ToString().ToLower()}" +
                $"&apikey={ApiKey}";

            if (searchTerms != "none")
                url += $"&search={Uri.EscapeUriString(searchTerms)}";

            string result = await http.GetStringAsync(url);

            var wineProducts = JsonConvert.DeserializeObject<WineProducts>(result);
            return wineProducts?.Products?.List ?? new List[0];
        }

        public async Task<byte[]> GetUserImageAsync(string url)
        {
            var responseMessage = await http.GetAsync(url);
            return await responseMessage.Content.ReadAsByteArrayAsync();
        }
    }
}