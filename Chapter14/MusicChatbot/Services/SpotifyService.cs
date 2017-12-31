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
    public class SpotifyService
    {
        const string BaseUrl = "https://api.spotify.com";

        public string GetToken()
        {
            string responseString = SendAuthorizationRequest();
            return ExtractTokenFromJson(responseString);
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

        string SendAuthorizationRequest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                "https://accounts.spotify.com/api/token");

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] data = encoding.GetBytes("grant_type=client_credentials");

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            string clientId = ConfigurationManager.AppSettings["ClientID"];
            string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];

            byte[] credentials = encoding.GetBytes(clientId + ":" + clientSecret);
            string authenticationValue = "Basic " + Convert.ToBase64String(credentials);
            request.Headers.Add("Authorization", authenticationValue);

            using (Stream stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            string responseString = null;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return responseString;
        }

        public List<GenreItem> GetGenres()
        {
            string token = GetToken();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/v1/browse/categories");
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            string responseJson;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            Genres genres = JsonConvert.DeserializeObject<Genres>(responseJson);
            var genreList =
                (from genre in genres.Categories.Items
                 select genre)
                .ToList();

            return genreList;
        }

        public List<Track> GetTracks(string genreID)
        {
            string token = GetToken();

            HttpWebRequest playlistRequest = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/v1/browse/categories/{genreID}/playlists?limit=1");
            playlistRequest.Method = WebRequestMethods.Http.Get;
            playlistRequest.Accept = "application/json";
            playlistRequest.Headers["Authorization"] = "Bearer " + token;

            string responseJson;
            using (var response = (HttpWebResponse)playlistRequest.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            PlayLists playList = JsonConvert.DeserializeObject<PlayLists>(responseJson);
            string tracksUrl = playList.Playlists.Items.FirstOrDefault()?.Tracks.Href;

            if (string.IsNullOrWhiteSpace(tracksUrl))
                return new List<Track>();

            HttpWebRequest tracksRequest = (HttpWebRequest)WebRequest.Create(tracksUrl);
            tracksRequest.Method = WebRequestMethods.Http.Get;
            tracksRequest.Accept = "application/json";
            tracksRequest.Headers["Authorization"] = "Bearer " + token;

            using (var response = (HttpWebResponse)tracksRequest.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
                responseJson = sr.ReadToEnd();

            TracksRoot tracks = JsonConvert.DeserializeObject<TracksRoot>(responseJson);

            var trackList =
                (from item in tracks.Items
                 select item.Track)
                .Take(5)
                .ToList();

            return trackList;
        }

        public string Search(SearchArguments args)
        {
            string token = GetToken();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                $"{BaseUrl}/v1/search?q={Uri.EscapeDataString(args.Query)}" +
                $"&limit={args.MaxItems}&type={args.Filters}");
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
