using AILib.Models;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AILib
{
    [Serializable]
    public class CognitiveService
    {
        const string AccessKey = "Ocp-Apim-Subscription-Key";

        public async Task<NewsArticles> SearchForNewsAsync(string artistName)
        {
            const string BaseUrl = "https://api.cognitive.microsoft.com/bing/v5.0";
            string url = $"{BaseUrl}/news/search?" +
                $"q={Uri.EscapeUriString(artistName)}&" +
                $"category=Entertainment_Music&" +
                $"count=5";

            string accessKey = ConfigurationManager.AppSettings["SearchKey"];
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(AccessKey, accessKey);

            string response = await client.GetStringAsync(url);

            NewsArticles articles = JsonConvert.DeserializeObject<NewsArticles>(response);
            return articles;
        }

        public async Task<ImageAnalysis> AnalyzeImageAsync(string imageUrl)
        {
            const string BaseUrl = "https://westus.api.cognitive.microsoft.com/vision/v1.0";
            string url = $"{BaseUrl}/analyze?visualFeatures=Description";

            string accessKey = ConfigurationManager.AppSettings["VisionKey"];
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(AccessKey, accessKey);

            var content = new StringContent(
                $"{{ \"url\": \"{imageUrl}\" }}", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string jsonResult = await response.Content.ReadAsStringAsync();

            ImageAnalysis analysis = JsonConvert.DeserializeObject<ImageAnalysis>(jsonResult);
            return analysis;
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            const string BaseUrl = "https://api.microsofttranslator.com/V2/Http.svc/Detect";
            string encodedText = Uri.EscapeUriString(text);
            string url = $"{BaseUrl}?text={encodedText}";

            string accessKey = ConfigurationManager.AppSettings["TranslateKey"];
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(AccessKey, accessKey);

            string response = await client.GetStringAsync(url);
            response = XElement.Parse(response).Value;
            return response;
        }

        public async Task<string> TranslateTextAsync(string text, string language)
        {
            string encodedText = Uri.EscapeUriString(text);
            string encodedLang = Uri.EscapeUriString(language);
            const string BaseUrl = "https://api.microsofttranslator.com/V2/Http.svc/Translate";
            string url = $"{BaseUrl}?text={encodedText}&to={encodedLang}";

            string accessKey = ConfigurationManager.AppSettings["TranslateKey"];
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(AccessKey, accessKey);

            string response = await client.GetStringAsync(url);
            response = XElement.Parse(response).Value;
            return response;
        }

        public async Task<QnAAnswer> AskWineChatbotFaqAsync(string question)
        {
            const string BaseUrl = "https://westus.api.cognitive.microsoft.com/qnamaker/v2.0";
            string knowledgeBaseID = ConfigurationManager.AppSettings["QnAKnowledgeBaseID"];
            string url = $"{BaseUrl}//knowledgebases/{knowledgeBaseID}/generateAnswer";

            var client = new HttpClient();
            string accessKey = ConfigurationManager.AppSettings["QnASubscriptionKey"];
            client.DefaultRequestHeaders.Add(AccessKey, accessKey);

            var content = new StringContent(
                $"{{ \"question\": \"{question}\" }}", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string jsonResult = await response.Content.ReadAsStringAsync();

            QnAResponse qnaResponse = JsonConvert.DeserializeObject<QnAResponse>(jsonResult);
            return qnaResponse.Answers.FirstOrDefault();
        }
    }
}
