using MusicChatbot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicChatbot.Services
{
    public class GrooveService
    {
        const string BaseUrl = "https://music.xboxlive.com";

        public string GetToken()
        {
            string service = "https://login.live.com/accesstoken.srf";

            string clientId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            string clientSecret = ConfigurationManager.AppSettings["MicrosoftAppPassword"];
            string clientSecretEnc = System.Uri.EscapeDataString(clientSecret);

            string scope = "app.music.xboxlive.com";
            string scopeEnc = System.Uri.EscapeDataString(scope);

            string grantType = "client_credentials";

            string postData = 
                $"client_id={clientId}&client_secret={clientSecretEnc}" +
                $"&scope={scopeEnc}&grant_type={grantType}";

            string responseString = SendRequest("POST", service, postData);

            string token = ExtractTokenFromJson(responseString);
            return token;
        }

        string ExtractTokenFromJson(string json)
        {
            Match match = Regex.Match(
                json, ".*\"access_token\":\"(?<token>.*?)\".*", RegexOptions.IgnoreCase);

            string token = null;
            if (match.Success)
                token = match.Groups["token"].Value;

            return token;
        }

        string SendRequest(string method, string service, string postData)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(service);

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] data = encoding.GetBytes(postData);

            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            string responseString = null;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return responseString;
        }

        public List<string> GetGenres()
        {
            string token = GetToken();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/3/content/music/catalog/genres");
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            string responseJson;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            Genres genres = JsonConvert.DeserializeObject<Genres>(responseJson);
            var genreList =
                (from genre in genres.CatalogGenres
                 where genre.ParentName == null
                 select genre.Name)
                .ToList();

            return genreList;
        }

        public List<Item> GetTracks(string genre)
        {
            string token = GetToken();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/1/content/music/catalog/tracks/browse?genre={genre}&maxItems=5&extra=Tracks");
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            string responseJson;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            TracksRoot tracks = JsonConvert.DeserializeObject<TracksRoot>(responseJson);
            var genreList =
                (from track in tracks.Tracks.Items
                 select track)
                .Take(5)
                .ToList();

            return genreList;
        }

        public Preview GetPreview(string namespaceId)
        {
            string token = GetToken();

            string clientInstanceId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/1/content/{namespaceId}/preview?clientInstanceId={clientInstanceId}");
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            string responseJson;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            Preview preview = JsonConvert.DeserializeObject<Preview>(responseJson);
            return preview;
        }

        public string Search(SearchArguments args)
        {
            string token = GetToken();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/1/content/music/search?q={Uri.EscapeDataString(args.Query)}" +
                $"&maxItems={args.MaxItems}&filters={args.Filters}&source={args.Source}");
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            string responseJson;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            return responseJson;
        }
    }
}