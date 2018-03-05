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
        readonly bool useTestData = true; // set to false to use the Spotify API.

        public SpotifyService()
        {
            string useTestDataParam = ConfigurationManager.AppSettings["UseTestData"];
            useTestData = string.IsNullOrWhiteSpace(useTestDataParam) ? true : bool.Parse(useTestDataParam);
        }

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
            string responseJson;

            if (useTestData)
            {
                responseJson = GenreTestData;
            }
            else
            {
                string token = GetToken();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                    $"{BaseUrl}/v1/browse/categories");
                request.Method = WebRequestMethods.Http.Get;
                request.Accept = "application/json";
                request.Headers["Authorization"] = "Bearer " + token;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var sr = new StreamReader(response.GetResponseStream()))
                    responseJson = sr.ReadToEnd();
            }

            Genres genres = JsonConvert.DeserializeObject<Genres>(responseJson);
            var genreList =
                (from genre in genres.Categories.Items
                 select genre)
                .ToList();

            return genreList;
        }

        public List<Track> GetTracks(string genreID)
        {
            string responseJson;

            if (useTestData)
            {
                responseJson = TrackTestData;
            }
            else
            {
                string token = GetToken();

                HttpWebRequest playlistRequest = (HttpWebRequest)WebRequest.Create(
                    $"{BaseUrl}/v1/browse/categories/{genreID}/playlists?limit=1");
                playlistRequest.Method = WebRequestMethods.Http.Get;
                playlistRequest.Accept = "application/json";
                playlistRequest.Headers["Authorization"] = "Bearer " + token;

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
            }

            TracksRoot tracks = JsonConvert.DeserializeObject<TracksRoot>(responseJson);
            var trackList =
                (from item in tracks.Items
                 select item.Track)
                .Take(5)
                .ToList();

            if (useTestData)
            {
                var fileService = new FileService();
                trackList.ForEach(track =>
                {
                    track.Album.Images = new TrackImage[0];
                    track.Uri = "http://aka.ms/botbook";
                    track.Preview_url = fileService.GetBinaryUrl("Testing123.m4a");
                });
            }

            return trackList;
        }

        public string Search(SearchArguments args)
        {
            string responseJson;

            if (useTestData)
            {
                responseJson = SearchTestData;
            }
            else
            {
                string token = GetToken();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                    $"{BaseUrl}/v1/search?q={Uri.EscapeDataString(args.Query)}" +
                    $"&limit={args.MaxItems}&type={args.Filters}");
                request.Method = WebRequestMethods.Http.Get;
                request.Accept = "application/json";
                request.Headers["Authorization"] = "Bearer " + token;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var sr = new StreamReader(response.GetResponseStream()))
                    responseJson = sr.ReadToEnd();
            }

            return responseJson;
        }

        #region Genre Test Data

        const string GenreTestData = @"{
  ""categories"" : {
    ""href"" : ""https://api.spotify.com/v1/browse/categories?offset=0&limit=20"",
    ""items"" : [ {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/toplists"",
      ""icons"" : [ {
        ""height"" : 275,
        ""url"" : ""https://t.scdn.co/media/derived/toplists_11160599e6a04ac5d6f2757f5511778f_0_0_275_275.jpg"",
        ""width"" : 275
      } ],
      ""id"" : ""toplists"",
      ""name"" : ""Top Lists""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/pop"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/pop-274x274_447148649685019f5e2a03a39e78ba52_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""pop"",
      ""name"" : ""Pop""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/mood"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/original/mood-274x274_976986a31ac8c49794cbdc7246fd5ad7_274x274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""mood"",
      ""name"" : ""Mood""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/hiphop"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/original/hip-274_0a661854d61e29eace5fe63f73495e68_274x274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""hiphop"",
      ""name"" : ""Hip-Hop""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/workout"",
      ""icons"" : [ {
        ""height"" : null,
        ""url"" : ""https://t.scdn.co/media/links/workout-274x274.jpg"",
        ""width"" : null
      } ],
      ""id"" : ""workout"",
      ""name"" : ""Workout""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/chill"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/chill-274x274_4c46374f007813dd10b37e8d8fd35b4b_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""chill"",
      ""name"" : ""Chill""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/edm_dance"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/edm-274x274_0ef612604200a9c14995432994455a6d_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""edm_dance"",
      ""name"" : ""Electronic/Dance""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/focus"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/original/genre-images-square-274x274_5e50d72b846a198fcd2ca9b3aef5f0c8_274x274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""focus"",
      ""name"" : ""Focus""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/rock"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/rock_9ce79e0a4ef901bbd10494f5b855d3cc_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""rock"",
      ""name"" : ""Rock""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/party"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/links/partyicon_274x274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""party"",
      ""name"" : ""Party""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/decades"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/decades_9ad8e458242b2ac8b184e79ef336c455_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""decades"",
      ""name"" : ""Decades""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/country"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/icon-274x274_6a35972b380f65dc348e0c798fe626a4_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""country"",
      ""name"" : ""Country""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/sleep"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/sleep-274x274_0d4f836af8fab7bf31526968073e671c_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""sleep"",
      ""name"" : ""Sleep""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/latin"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/latin-274x274_befbbd1fbb8e045491576e317cb16cdf_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""latin"",
      ""name"" : ""Latin""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/rnb"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/r-b-274x274_fd56efa72f4f63764b011b68121581d8_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""rnb"",
      ""name"" : ""R&B""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/romance"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/romance-274x274_8100794c94847b6d27858bed6fa4d91b_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""romance"",
      ""name"" : ""Romance""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/indie_alt"",
      ""icons"" : [ {
        ""height"" : null,
        ""url"" : ""https://t.scdn.co/images/fe06caf056474bc58862591cd60b57fc.jpeg"",
        ""width"" : null
      } ],
      ""id"" : ""indie_alt"",
      ""name"" : ""Indie""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/jazz"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/jazz-274x274_d6f407453a1f43d3163c55cca624a764_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""jazz"",
      ""name"" : ""Jazz""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/gaming"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/categories/gaming2_274x274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""gaming"",
      ""name"" : ""Gaming""
    }, {
      ""href"" : ""https://api.spotify.com/v1/browse/categories/classical"",
      ""icons"" : [ {
        ""height"" : 274,
        ""url"" : ""https://t.scdn.co/media/derived/classical-274x274_abf78251ff3d90d2ceaf029253ca7cb4_0_0_274_274.jpg"",
        ""width"" : 274
      } ],
      ""id"" : ""classical"",
      ""name"" : ""Classical""
    } ],
    ""limit"" : 20,
    ""next"" : ""https://api.spotify.com/v1/browse/categories?offset=20&limit=20"",
    ""offset"" : 0,
    ""previous"" : null,
    ""total"" : 35
  }
}";

        #endregion

        #region Tracks Test Data

        const string TrackTestData = @"{
  ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DXcBWIGoYBM5M/tracks?offset=0&limit=100"",
  ""items"" : [ {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/6LuN9FCkKOj5PcnpouEgny""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/6LuN9FCkKOj5PcnpouEgny"",
          ""id"" : ""6LuN9FCkKOj5PcnpouEgny"",
          ""name"" : ""Khalid"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:6LuN9FCkKOj5PcnpouEgny""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2cWZOOzeOm4WmBJRnD5R7I""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2cWZOOzeOm4WmBJRnD5R7I"",
          ""id"" : ""2cWZOOzeOm4WmBJRnD5R7I"",
          ""name"" : ""Normani"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2cWZOOzeOm4WmBJRnD5R7I""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/4CEAev7neETRdqBFtzA8B9""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/4CEAev7neETRdqBFtzA8B9"",
        ""id"" : ""4CEAev7neETRdqBFtzA8B9"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/ea0c58be347de0c1ea9a2c4dadae238798baf468"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/0942f22399b0ba3e4f14533c347e9c7df62ad80f"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/7a65bfdbd03cbda8d9ee8c12cd4343708810e00a"",
          ""width"" : 64
        } ],
        ""name"" : ""Love Lies (with Normani)"",
        ""release_date"" : ""2018-02-14"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:4CEAev7neETRdqBFtzA8B9""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6LuN9FCkKOj5PcnpouEgny""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6LuN9FCkKOj5PcnpouEgny"",
        ""id"" : ""6LuN9FCkKOj5PcnpouEgny"",
        ""name"" : ""Khalid"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6LuN9FCkKOj5PcnpouEgny""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2cWZOOzeOm4WmBJRnD5R7I""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2cWZOOzeOm4WmBJRnD5R7I"",
        ""id"" : ""2cWZOOzeOm4WmBJRnD5R7I"",
        ""name"" : ""Normani"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2cWZOOzeOm4WmBJRnD5R7I""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 201707,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USRC11703646""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/45Egmo7icyopuzJN0oMEdk""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/45Egmo7icyopuzJN0oMEdk"",
      ""id"" : ""45Egmo7icyopuzJN0oMEdk"",
      ""name"" : ""Love Lies (with Normani)"",
      ""popularity"" : 92,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/d53d5678b946219bd6df0b3d04ce0b3554f167d4?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:45Egmo7icyopuzJN0oMEdk""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/64KEffDW9EtZ1y2vBYgq8T""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/64KEffDW9EtZ1y2vBYgq8T"",
          ""id"" : ""64KEffDW9EtZ1y2vBYgq8T"",
          ""name"" : ""Marshmello"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:64KEffDW9EtZ1y2vBYgq8T""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1zNqDE7qDGCsyzJwohVaoX""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1zNqDE7qDGCsyzJwohVaoX"",
          ""id"" : ""1zNqDE7qDGCsyzJwohVaoX"",
          ""name"" : ""Anne-Marie"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1zNqDE7qDGCsyzJwohVaoX""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1BmxOYHjQv1dKZRr13YRZM""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1BmxOYHjQv1dKZRr13YRZM"",
        ""id"" : ""1BmxOYHjQv1dKZRr13YRZM"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/ab9e1e2e78d4f25e10364403dc13d7cffded6daf"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/5843ed4a56177db9ae39a09d23319f87a78ed7d6"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/9d99ddd56d7cf8599b6c8764fe62577df649de93"",
          ""width"" : 64
        } ],
        ""name"" : ""FRIENDS"",
        ""release_date"" : ""2018-02-09"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1BmxOYHjQv1dKZRr13YRZM""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/64KEffDW9EtZ1y2vBYgq8T""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/64KEffDW9EtZ1y2vBYgq8T"",
        ""id"" : ""64KEffDW9EtZ1y2vBYgq8T"",
        ""name"" : ""Marshmello"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:64KEffDW9EtZ1y2vBYgq8T""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1zNqDE7qDGCsyzJwohVaoX""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1zNqDE7qDGCsyzJwohVaoX"",
        ""id"" : ""1zNqDE7qDGCsyzJwohVaoX"",
        ""name"" : ""Anne-Marie"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1zNqDE7qDGCsyzJwohVaoX""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 202620,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""GBAHS1800025""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/08bNPGLD8AhKpnnERrAc6G""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/08bNPGLD8AhKpnnERrAc6G"",
      ""id"" : ""08bNPGLD8AhKpnnERrAc6G"",
      ""name"" : ""FRIENDS"",
      ""popularity"" : 95,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/99b833cf96a8b56040859443d8688a2e322c6667?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:08bNPGLD8AhKpnnERrAc6G""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/246dkjvS1zLTtiykXe5h60""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/246dkjvS1zLTtiykXe5h60"",
          ""id"" : ""246dkjvS1zLTtiykXe5h60"",
          ""name"" : ""Post Malone"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:246dkjvS1zLTtiykXe5h60""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1Val8NiAXyp2yTBiwZ53Ju""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1Val8NiAXyp2yTBiwZ53Ju"",
        ""id"" : ""1Val8NiAXyp2yTBiwZ53Ju"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/c05e44a2209fe3903730058f06c1b757eb5b82cc"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/78b6c2137e1c9dbcd94cc2e8919461041098b7ec"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/6b1051c1f1a3464de008a1ad60eff96a49e4070b"",
          ""width"" : 64
        } ],
        ""name"" : ""Psycho (feat. Ty Dolla $ign)"",
        ""release_date"" : ""2018-02-23"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1Val8NiAXyp2yTBiwZ53Ju""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/246dkjvS1zLTtiykXe5h60""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/246dkjvS1zLTtiykXe5h60"",
        ""id"" : ""246dkjvS1zLTtiykXe5h60"",
        ""name"" : ""Post Malone"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:246dkjvS1zLTtiykXe5h60""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7c0XG5cIJTrrAgEC3ULPiq""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7c0XG5cIJTrrAgEC3ULPiq"",
        ""id"" : ""7c0XG5cIJTrrAgEC3ULPiq"",
        ""name"" : ""Ty Dolla $ign"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7c0XG5cIJTrrAgEC3ULPiq""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 220880,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71710836""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/7wrDRQgHlvDnimrRHfQZxt""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/7wrDRQgHlvDnimrRHfQZxt"",
      ""id"" : ""7wrDRQgHlvDnimrRHfQZxt"",
      ""name"" : ""Psycho (feat. Ty Dolla $ign)"",
      ""popularity"" : 90,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:7wrDRQgHlvDnimrRHfQZxt""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3TVXtAsR1Inumwj472S9r4""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3TVXtAsR1Inumwj472S9r4"",
          ""id"" : ""3TVXtAsR1Inumwj472S9r4"",
          ""name"" : ""Drake"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3TVXtAsR1Inumwj472S9r4""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1r0DOIO0iC0bGpMtWRFdde""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1r0DOIO0iC0bGpMtWRFdde"",
        ""id"" : ""1r0DOIO0iC0bGpMtWRFdde"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/6f3483e372bbb81cddb5fab16ebf50d2ac5009b9"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/2af1735e2281011bfc05353bfdee906338cbbb5b"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/8fdf1e6d19739b620107746d44332860d947f9df"",
          ""width"" : 64
        } ],
        ""name"" : ""Scary Hours"",
        ""release_date"" : ""2018-01-20"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1r0DOIO0iC0bGpMtWRFdde""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3TVXtAsR1Inumwj472S9r4""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3TVXtAsR1Inumwj472S9r4"",
        ""id"" : ""3TVXtAsR1Inumwj472S9r4"",
        ""name"" : ""Drake"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3TVXtAsR1Inumwj472S9r4""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 198960,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USCM51800004""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/2XW4DbS6NddZxRPm5rMCeY""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/2XW4DbS6NddZxRPm5rMCeY"",
      ""id"" : ""2XW4DbS6NddZxRPm5rMCeY"",
      ""name"" : ""God's Plan"",
      ""popularity"" : 100,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:2XW4DbS6NddZxRPm5rMCeY""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2qxJFvFYMEDqd7ui6kSAcq""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2qxJFvFYMEDqd7ui6kSAcq"",
          ""id"" : ""2qxJFvFYMEDqd7ui6kSAcq"",
          ""name"" : ""Zedd"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2qxJFvFYMEDqd7ui6kSAcq""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/6WY7D3jk8zTrHtmkqqo5GI""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/6WY7D3jk8zTrHtmkqqo5GI"",
          ""id"" : ""6WY7D3jk8zTrHtmkqqo5GI"",
          ""name"" : ""Maren Morris"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:6WY7D3jk8zTrHtmkqqo5GI""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4lDBihdpMlOalxy1jkUbPl""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4lDBihdpMlOalxy1jkUbPl"",
          ""id"" : ""4lDBihdpMlOalxy1jkUbPl"",
          ""name"" : ""Grey"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4lDBihdpMlOalxy1jkUbPl""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1D8u1ccrRXyFMOGTEXTgTX""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1D8u1ccrRXyFMOGTEXTgTX"",
        ""id"" : ""1D8u1ccrRXyFMOGTEXTgTX"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/3757dc06d7fa51f0fa238b7feb0cd3382502db0f"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/e7c05b51144bdb1e67b1672e94a7c091d8fbdaa5"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/09bde636590d102890c476b1acc011225374ba42"",
          ""width"" : 64
        } ],
        ""name"" : ""The Middle"",
        ""release_date"" : ""2018-01-23"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1D8u1ccrRXyFMOGTEXTgTX""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2qxJFvFYMEDqd7ui6kSAcq""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2qxJFvFYMEDqd7ui6kSAcq"",
        ""id"" : ""2qxJFvFYMEDqd7ui6kSAcq"",
        ""name"" : ""Zedd"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2qxJFvFYMEDqd7ui6kSAcq""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6WY7D3jk8zTrHtmkqqo5GI""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6WY7D3jk8zTrHtmkqqo5GI"",
        ""id"" : ""6WY7D3jk8zTrHtmkqqo5GI"",
        ""name"" : ""Maren Morris"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6WY7D3jk8zTrHtmkqqo5GI""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4lDBihdpMlOalxy1jkUbPl""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4lDBihdpMlOalxy1jkUbPl"",
        ""id"" : ""4lDBihdpMlOalxy1jkUbPl"",
        ""name"" : ""Grey"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4lDBihdpMlOalxy1jkUbPl""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 184732,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USUM71800463""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/2ARqIya5NAuvFVHSN3bL0m""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/2ARqIya5NAuvFVHSN3bL0m"",
      ""id"" : ""2ARqIya5NAuvFVHSN3bL0m"",
      ""name"" : ""The Middle"",
      ""popularity"" : 91,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:2ARqIya5NAuvFVHSN3bL0m""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
          ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
          ""name"" : ""Kendrick Lamar"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/7tYKF4w9nC0nq9CsPZTHyP""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/7tYKF4w9nC0nq9CsPZTHyP"",
          ""id"" : ""7tYKF4w9nC0nq9CsPZTHyP"",
          ""name"" : ""SZA"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:7tYKF4w9nC0nq9CsPZTHyP""
        } ],
        ""available_markets"" : [ ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3mau89iBea8nCPw3I8kKAk""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3mau89iBea8nCPw3I8kKAk"",
        ""id"" : ""3mau89iBea8nCPw3I8kKAk"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/de7fada56c73b2977b854bc69a5f800aaf1bf33a"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/88f240b294e747f3530c567224bc486676ead64e"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/2d962a964bc6db722a1547e034159fb3c5d0638f"",
          ""width"" : 64
        } ],
        ""name"" : ""All The Stars (with SZA)"",
        ""release_date"" : ""2018-01-04"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3mau89iBea8nCPw3I8kKAk""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
        ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
        ""name"" : ""Kendrick Lamar"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7tYKF4w9nC0nq9CsPZTHyP""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7tYKF4w9nC0nq9CsPZTHyP"",
        ""id"" : ""7tYKF4w9nC0nq9CsPZTHyP"",
        ""name"" : ""SZA"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7tYKF4w9nC0nq9CsPZTHyP""
      } ],
      ""available_markets"" : [ ],
      ""disc_number"" : 1,
      ""duration_ms"" : 235540,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71713947""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/66kQ7wr4d2LwwSjr7HXcyr""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/66kQ7wr4d2LwwSjr7HXcyr"",
      ""id"" : ""66kQ7wr4d2LwwSjr7HXcyr"",
      ""name"" : ""All The Stars (with SZA)"",
      ""popularity"" : 86,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:66kQ7wr4d2LwwSjr7HXcyr""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4GvEc3ANtPPjt1ZJllr5Zl""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4GvEc3ANtPPjt1ZJllr5Zl"",
          ""id"" : ""4GvEc3ANtPPjt1ZJllr5Zl"",
          ""name"" : ""Bazzi"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4GvEc3ANtPPjt1ZJllr5Zl""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5TR37lsXlmSmzFuIcXn5Dp""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5TR37lsXlmSmzFuIcXn5Dp"",
        ""id"" : ""5TR37lsXlmSmzFuIcXn5Dp"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/6bd1fbc1bba92a98b7a84d98379f06828ec07aae"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/b625a050e7146e04216d1a632397a15e020996e6"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/2390482b3e5872c252928e2722c430aa84cbcbc6"",
          ""width"" : 64
        } ],
        ""name"" : ""Mine"",
        ""release_date"" : ""2017-10-12"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5TR37lsXlmSmzFuIcXn5Dp""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4GvEc3ANtPPjt1ZJllr5Zl""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4GvEc3ANtPPjt1ZJllr5Zl"",
        ""id"" : ""4GvEc3ANtPPjt1ZJllr5Zl"",
        ""name"" : ""Bazzi"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4GvEc3ANtPPjt1ZJllr5Zl""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 133994,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USAT21704227""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6tHWl8ows5JOZq9Yfaqn3M""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6tHWl8ows5JOZq9Yfaqn3M"",
      ""id"" : ""6tHWl8ows5JOZq9Yfaqn3M"",
      ""name"" : ""Mine"",
      ""popularity"" : 97,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/8293dcef3f7625ddfe418e4f620fb6e770dd3911?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6tHWl8ows5JOZq9Yfaqn3M""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4WN5naL3ofxrVBgFpguzKo""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4WN5naL3ofxrVBgFpguzKo"",
          ""id"" : ""4WN5naL3ofxrVBgFpguzKo"",
          ""name"" : ""Rudimental"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4WN5naL3ofxrVBgFpguzKo""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4ScCswdRlyA23odg9thgIO""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4ScCswdRlyA23odg9thgIO"",
          ""id"" : ""4ScCswdRlyA23odg9thgIO"",
          ""name"" : ""Jess Glynne"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4ScCswdRlyA23odg9thgIO""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3JhNCzhSMTxs9WLGJJxWOY""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3JhNCzhSMTxs9WLGJJxWOY"",
          ""id"" : ""3JhNCzhSMTxs9WLGJJxWOY"",
          ""name"" : ""Macklemore"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3JhNCzhSMTxs9WLGJJxWOY""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/2sjjFDjZSCYD5eBCsi0fDW""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/2sjjFDjZSCYD5eBCsi0fDW"",
        ""id"" : ""2sjjFDjZSCYD5eBCsi0fDW"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/807829b25332e965cccb4a84c8404ce35740896d"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/6e4e360e10a4382fdedeca2ea14dd7c754811307"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/3b4bf6d482ca1ad5e992860a6b53a9c3f815295b"",
          ""width"" : 64
        } ],
        ""name"" : ""These Days (feat. Jess Glynne, Macklemore & Dan Caplen)"",
        ""release_date"" : ""2018-01-19"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:2sjjFDjZSCYD5eBCsi0fDW""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4WN5naL3ofxrVBgFpguzKo""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4WN5naL3ofxrVBgFpguzKo"",
        ""id"" : ""4WN5naL3ofxrVBgFpguzKo"",
        ""name"" : ""Rudimental"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4WN5naL3ofxrVBgFpguzKo""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4ScCswdRlyA23odg9thgIO""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4ScCswdRlyA23odg9thgIO"",
        ""id"" : ""4ScCswdRlyA23odg9thgIO"",
        ""name"" : ""Jess Glynne"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4ScCswdRlyA23odg9thgIO""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3JhNCzhSMTxs9WLGJJxWOY""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3JhNCzhSMTxs9WLGJJxWOY"",
        ""id"" : ""3JhNCzhSMTxs9WLGJJxWOY"",
        ""name"" : ""Macklemore"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3JhNCzhSMTxs9WLGJJxWOY""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2U3FuHYvL3vhkbDAXm24Ep""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2U3FuHYvL3vhkbDAXm24Ep"",
        ""id"" : ""2U3FuHYvL3vhkbDAXm24Ep"",
        ""name"" : ""Dan Caplen"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2U3FuHYvL3vhkbDAXm24Ep""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 210772,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""GBAHS1701239""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/5CLGzJsGqhCEECcpnFQA8x""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/5CLGzJsGqhCEECcpnFQA8x"",
      ""id"" : ""5CLGzJsGqhCEECcpnFQA8x"",
      ""name"" : ""These Days (feat. Jess Glynne, Macklemore & Dan Caplen)"",
      ""popularity"" : 96,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/57f9b45cd318070b04945fc6c8a7e50eaa7f8971?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:5CLGzJsGqhCEECcpnFQA8x""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/69GGBxA162lTqCwzJG5jLp""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/69GGBxA162lTqCwzJG5jLp"",
          ""id"" : ""69GGBxA162lTqCwzJG5jLp"",
          ""name"" : ""The Chainsmokers"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:69GGBxA162lTqCwzJG5jLp""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7ipPGzgSu86WmYyNyx2Kry""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7ipPGzgSu86WmYyNyx2Kry"",
        ""id"" : ""7ipPGzgSu86WmYyNyx2Kry"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/ff161161c734a4f24d3ce7cfccf4bde7ac8443d1"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/c466929867dad959ad2597dc6da682d6b6fcc182"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/56a128090e720bd581a116120ab6ed917507639d"",
          ""width"" : 64
        } ],
        ""name"" : ""Sick Boy...You Owe Me"",
        ""release_date"" : ""2018-02-16"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7ipPGzgSu86WmYyNyx2Kry""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/69GGBxA162lTqCwzJG5jLp""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/69GGBxA162lTqCwzJG5jLp"",
        ""id"" : ""69GGBxA162lTqCwzJG5jLp"",
        ""name"" : ""The Chainsmokers"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:69GGBxA162lTqCwzJG5jLp""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 190546,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USQX91702850""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/1USJRpWDFhxwhCajPXBeel""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/1USJRpWDFhxwhCajPXBeel"",
      ""id"" : ""1USJRpWDFhxwhCajPXBeel"",
      ""name"" : ""You Owe Me"",
      ""popularity"" : 86,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/74cc6e7fbc37176da1fb0a705664ccb9edb36001?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:1USJRpWDFhxwhCajPXBeel""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/6M2wZ9GZgrQXHCFfjv46we""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/6M2wZ9GZgrQXHCFfjv46we"",
          ""id"" : ""6M2wZ9GZgrQXHCFfjv46we"",
          ""name"" : ""Dua Lipa"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:6M2wZ9GZgrQXHCFfjv46we""
        } ],
        ""available_markets"" : [ ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/2vlhlrgMaXqcnhRqIEV9AP""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/2vlhlrgMaXqcnhRqIEV9AP"",
        ""id"" : ""2vlhlrgMaXqcnhRqIEV9AP"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/0e4574fac3ba5a4de37d349bb83ca04bcf0bc68a"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/88d1609a5c4131bd2d20c34239eac9f1831ceb74"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/6d02ffe7a35d4a90c8af6261efcb9d7e16580f3a"",
          ""width"" : 64
        } ],
        ""name"" : ""Dua Lipa"",
        ""release_date"" : ""2017-06-02"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:2vlhlrgMaXqcnhRqIEV9AP""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6M2wZ9GZgrQXHCFfjv46we""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6M2wZ9GZgrQXHCFfjv46we"",
        ""id"" : ""6M2wZ9GZgrQXHCFfjv46we"",
        ""name"" : ""Dua Lipa"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6M2wZ9GZgrQXHCFfjv46we""
      } ],
      ""available_markets"" : [ ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 218173,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""GBAHT1600301""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/1ny60YCHhEsxsJXWLhK7b0""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/1ny60YCHhEsxsJXWLhK7b0"",
      ""id"" : ""1ny60YCHhEsxsJXWLhK7b0"",
      ""name"" : ""IDGAF"",
      ""popularity"" : 83,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/f2ba95dc463d49eeb99f4dfb3d2201b3b14bf4b4?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 5,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:1ny60YCHhEsxsJXWLhK7b0""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/0ZED1XzwlLHW4ZaG4lOT6m""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/0ZED1XzwlLHW4ZaG4lOT6m"",
          ""id"" : ""0ZED1XzwlLHW4ZaG4lOT6m"",
          ""name"" : ""Julia Michaels"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:0ZED1XzwlLHW4ZaG4lOT6m""
        } ],
        ""available_markets"" : [ ""SE"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5pvLpqi54KJ3LiZ2pwdXoE""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5pvLpqi54KJ3LiZ2pwdXoE"",
        ""id"" : ""5pvLpqi54KJ3LiZ2pwdXoE"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/f63e2c91b1dc40876b2b13187e40d1a5b46fa7d4"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/d0b5256179daca2bdb1f5045737b4aec144ddf5a"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/e383c4040fb0e1218d0e44661a2478782566297c"",
          ""width"" : 64
        } ],
        ""name"" : ""Heaven [From \""Fifty Shades Freed (Original Motion Picture Soundtrack)\""]"",
        ""release_date"" : ""2018-01-26"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5pvLpqi54KJ3LiZ2pwdXoE""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/0ZED1XzwlLHW4ZaG4lOT6m""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/0ZED1XzwlLHW4ZaG4lOT6m"",
        ""id"" : ""0ZED1XzwlLHW4ZaG4lOT6m"",
        ""name"" : ""Julia Michaels"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:0ZED1XzwlLHW4ZaG4lOT6m""
      } ],
      ""available_markets"" : [ ""SE"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 191813,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USQ4E1703344""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/4QfNOZN7vrQCQKK1ZJ3XPb""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/4QfNOZN7vrQCQKK1ZJ3XPb"",
      ""id"" : ""4QfNOZN7vrQCQKK1ZJ3XPb"",
      ""name"" : ""Heaven"",
      ""popularity"" : 76,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:4QfNOZN7vrQCQKK1ZJ3XPb""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1Xyo4u8uXC1ZmMpatF05PJ""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1Xyo4u8uXC1ZmMpatF05PJ"",
          ""id"" : ""1Xyo4u8uXC1ZmMpatF05PJ"",
          ""name"" : ""The Weeknd"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1Xyo4u8uXC1ZmMpatF05PJ""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
          ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
          ""name"" : ""Kendrick Lamar"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7znxn446I0HDNWcV86iRLz""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7znxn446I0HDNWcV86iRLz"",
        ""id"" : ""7znxn446I0HDNWcV86iRLz"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/c5cb628bec010e79f2bde98df8c09da119617194"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/58f537e877808ee72fd13b854aa46d5a6d643cf6"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/f4c3b174c13d32367c7415e733fe161e58d3efab"",
          ""width"" : 64
        } ],
        ""name"" : ""Pray For Me (with Kendrick Lamar)"",
        ""release_date"" : ""2018-02-02"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7znxn446I0HDNWcV86iRLz""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1Xyo4u8uXC1ZmMpatF05PJ""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1Xyo4u8uXC1ZmMpatF05PJ"",
        ""id"" : ""1Xyo4u8uXC1ZmMpatF05PJ"",
        ""name"" : ""The Weeknd"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1Xyo4u8uXC1ZmMpatF05PJ""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
        ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
        ""name"" : ""Kendrick Lamar"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 211440,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71800001""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6ZNo7Vi0TE9ul1fhKd4S1M""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6ZNo7Vi0TE9ul1fhKd4S1M"",
      ""id"" : ""6ZNo7Vi0TE9ul1fhKd4S1M"",
      ""name"" : ""Pray For Me (with Kendrick Lamar)"",
      ""popularity"" : 89,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6ZNo7Vi0TE9ul1fhKd4S1M""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4nDoRrQiYLoBzwC5BhVJzF""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4nDoRrQiYLoBzwC5BhVJzF"",
          ""id"" : ""4nDoRrQiYLoBzwC5BhVJzF"",
          ""name"" : ""Camila Cabello"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4nDoRrQiYLoBzwC5BhVJzF""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/2vD3zSQr8hNlg0obNel4TE""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/2vD3zSQr8hNlg0obNel4TE"",
        ""id"" : ""2vD3zSQr8hNlg0obNel4TE"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/8ebf0216fa9d294177e79cfef03628ed68043454"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/ac7215afbceb58c8a7f3713eaf9d00ff3d959779"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/014f38920ba75a4efd3488b4626cf6e16f94c9e5"",
          ""width"" : 64
        } ],
        ""name"" : ""Camila"",
        ""release_date"" : ""2018-01-12"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:2vD3zSQr8hNlg0obNel4TE""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4nDoRrQiYLoBzwC5BhVJzF""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4nDoRrQiYLoBzwC5BhVJzF"",
        ""id"" : ""4nDoRrQiYLoBzwC5BhVJzF"",
        ""name"" : ""Camila Cabello"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4nDoRrQiYLoBzwC5BhVJzF""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 177160,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USSM11706920""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/5HwnezK198pJCEj1l2Adjy""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/5HwnezK198pJCEj1l2Adjy"",
      ""id"" : ""5HwnezK198pJCEj1l2Adjy"",
      ""name"" : ""She Loves Control"",
      ""popularity"" : 90,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/161df08e7f876e568b7f826dab9456a05d78264f?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 3,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:5HwnezK198pJCEj1l2Adjy""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/5p7f24Rk5HkUZsaS3BLG5F""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/5p7f24Rk5HkUZsaS3BLG5F"",
          ""id"" : ""5p7f24Rk5HkUZsaS3BLG5F"",
          ""name"" : ""Hailee Steinfeld"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:5p7f24Rk5HkUZsaS3BLG5F""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1okJ4NC308qbtY9LyHn6DO""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1okJ4NC308qbtY9LyHn6DO"",
          ""id"" : ""1okJ4NC308qbtY9LyHn6DO"",
          ""name"" : ""BloodPop®"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1okJ4NC308qbtY9LyHn6DO""
        } ],
        ""available_markets"" : [ ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/57fWbQLWV1JZdQwkQ2beG9""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/57fWbQLWV1JZdQwkQ2beG9"",
        ""id"" : ""57fWbQLWV1JZdQwkQ2beG9"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/79cf78c9b8ee88e638f810302c4bce7bb94dc5ba"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/3b90df9c2bdc282933f84f0e0d2a279b66f0b8ae"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/86062f23156f2a1865d1d3c795d4eee2eed8f489"",
          ""width"" : 64
        } ],
        ""name"" : ""Capital Letters [From \""Fifty Shades Freed (Original Motion Picture Soundtrack)\""]"",
        ""release_date"" : ""2018-01-12"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:57fWbQLWV1JZdQwkQ2beG9""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/5p7f24Rk5HkUZsaS3BLG5F""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/5p7f24Rk5HkUZsaS3BLG5F"",
        ""id"" : ""5p7f24Rk5HkUZsaS3BLG5F"",
        ""name"" : ""Hailee Steinfeld"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:5p7f24Rk5HkUZsaS3BLG5F""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1okJ4NC308qbtY9LyHn6DO""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1okJ4NC308qbtY9LyHn6DO"",
        ""id"" : ""1okJ4NC308qbtY9LyHn6DO"",
        ""name"" : ""BloodPop®"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1okJ4NC308qbtY9LyHn6DO""
      } ],
      ""available_markets"" : [ ],
      ""disc_number"" : 1,
      ""duration_ms"" : 219386,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USQ4E1703340""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/2bllegdYt2WoYdbRZyJ730""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/2bllegdYt2WoYdbRZyJ730"",
      ""id"" : ""2bllegdYt2WoYdbRZyJ730"",
      ""name"" : ""Capital Letters"",
      ""popularity"" : 82,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:2bllegdYt2WoYdbRZyJ730""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4xRYI6VqpkE3UwrDrAZL8L""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4xRYI6VqpkE3UwrDrAZL8L"",
          ""id"" : ""4xRYI6VqpkE3UwrDrAZL8L"",
          ""name"" : ""Logic"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4xRYI6VqpkE3UwrDrAZL8L""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/64KEffDW9EtZ1y2vBYgq8T""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/64KEffDW9EtZ1y2vBYgq8T"",
          ""id"" : ""64KEffDW9EtZ1y2vBYgq8T"",
          ""name"" : ""Marshmello"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:64KEffDW9EtZ1y2vBYgq8T""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/0VQf8Ysv00VqQm4FSvdad3""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/0VQf8Ysv00VqQm4FSvdad3"",
        ""id"" : ""0VQf8Ysv00VqQm4FSvdad3"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/62f6dfe5fa89762bd3f17abd47f73a51edda957d"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/694a726e750f487ebb83ac22d88002e723cac71a"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/8f1b1f5437a14e34c14114aee90c2e9862d96257"",
          ""width"" : 64
        } ],
        ""name"" : ""Everyday"",
        ""release_date"" : ""2018-03-02"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:0VQf8Ysv00VqQm4FSvdad3""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4xRYI6VqpkE3UwrDrAZL8L""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4xRYI6VqpkE3UwrDrAZL8L"",
        ""id"" : ""4xRYI6VqpkE3UwrDrAZL8L"",
        ""name"" : ""Logic"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4xRYI6VqpkE3UwrDrAZL8L""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/64KEffDW9EtZ1y2vBYgq8T""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/64KEffDW9EtZ1y2vBYgq8T"",
        ""id"" : ""64KEffDW9EtZ1y2vBYgq8T"",
        ""name"" : ""Marshmello"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:64KEffDW9EtZ1y2vBYgq8T""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 204826,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71802154""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6zOhgKfbMiQWToE6K13s2s""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6zOhgKfbMiQWToE6K13s2s"",
      ""id"" : ""6zOhgKfbMiQWToE6K13s2s"",
      ""name"" : ""Everyday"",
      ""popularity"" : 73,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6zOhgKfbMiQWToE6K13s2s""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/04gDigrS5kc9YWfZHwBETP""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/04gDigrS5kc9YWfZHwBETP"",
          ""id"" : ""04gDigrS5kc9YWfZHwBETP"",
          ""name"" : ""Maroon 5"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:04gDigrS5kc9YWfZHwBETP""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/0N6D8o0NqcjS9LEKpnlAHb""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/0N6D8o0NqcjS9LEKpnlAHb"",
        ""id"" : ""0N6D8o0NqcjS9LEKpnlAHb"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/542f2974c78d671dcb3aaa4f802d482bbf524794"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/5d094fe4ee210fbccdf55195bb7b6e2f197d81e7"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/0e587b8c52964b81c38f2b73094012bf04e2bd01"",
          ""width"" : 64
        } ],
        ""name"" : ""Wait (feat. A Boogie Wit da Hoodie)"",
        ""release_date"" : ""2018-01-26"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:0N6D8o0NqcjS9LEKpnlAHb""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/04gDigrS5kc9YWfZHwBETP""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/04gDigrS5kc9YWfZHwBETP"",
        ""id"" : ""04gDigrS5kc9YWfZHwBETP"",
        ""name"" : ""Maroon 5"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:04gDigrS5kc9YWfZHwBETP""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/31W5EY0aAly4Qieq6OFu6I""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/31W5EY0aAly4Qieq6OFu6I"",
        ""id"" : ""31W5EY0aAly4Qieq6OFu6I"",
        ""name"" : ""A Boogie Wit da Hoodie"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:31W5EY0aAly4Qieq6OFu6I""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 190476,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USUM71800126""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6K1yBz4AnZT2tCoSoUhJiq""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6K1yBz4AnZT2tCoSoUhJiq"",
      ""id"" : ""6K1yBz4AnZT2tCoSoUhJiq"",
      ""name"" : ""Wait (feat. A Boogie Wit da Hoodie)"",
      ""popularity"" : 77,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6K1yBz4AnZT2tCoSoUhJiq""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/6oMuImdp5ZcFhWP0ESe6mG""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/6oMuImdp5ZcFhWP0ESe6mG"",
          ""id"" : ""6oMuImdp5ZcFhWP0ESe6mG"",
          ""name"" : ""Migos"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:6oMuImdp5ZcFhWP0ESe6mG""
        } ],
        ""available_markets"" : [ ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/2mDzhes9hFHQiBEMkvVtkF""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/2mDzhes9hFHQiBEMkvVtkF"",
        ""id"" : ""2mDzhes9hFHQiBEMkvVtkF"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/77fe474fb8d74eff4c6f6d7093d2df33311d77af"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/1115f3e9de7818321a0e681d431737712b368b82"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/dbddc76e75b8fb1c286257dd1933017e58f4b41f"",
          ""width"" : 64
        } ],
        ""name"" : ""Stir Fry"",
        ""release_date"" : ""2017-12-20"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:2mDzhes9hFHQiBEMkvVtkF""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6oMuImdp5ZcFhWP0ESe6mG""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6oMuImdp5ZcFhWP0ESe6mG"",
        ""id"" : ""6oMuImdp5ZcFhWP0ESe6mG"",
        ""name"" : ""Migos"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6oMuImdp5ZcFhWP0ESe6mG""
      } ],
      ""available_markets"" : [ ],
      ""disc_number"" : 1,
      ""duration_ms"" : 192131,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71714081""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/4fndbjoz1qJyK6JcLdKfzm""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/4fndbjoz1qJyK6JcLdKfzm"",
      ""id"" : ""4fndbjoz1qJyK6JcLdKfzm"",
      ""name"" : ""Stir Fry"",
      ""popularity"" : 80,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:4fndbjoz1qJyK6JcLdKfzm""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/28ExwzUQsvgJooOI0X1mr3""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/28ExwzUQsvgJooOI0X1mr3"",
          ""id"" : ""28ExwzUQsvgJooOI0X1mr3"",
          ""name"" : ""Jay Rock"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:28ExwzUQsvgJooOI0X1mr3""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
          ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
          ""name"" : ""Kendrick Lamar"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1RyvyyTE3xzB2ZywiAwp0i""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1RyvyyTE3xzB2ZywiAwp0i"",
          ""id"" : ""1RyvyyTE3xzB2ZywiAwp0i"",
          ""name"" : ""Future"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1RyvyyTE3xzB2ZywiAwp0i""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1NXM5lF9YB7a3f1e4R48oH""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1NXM5lF9YB7a3f1e4R48oH"",
        ""id"" : ""1NXM5lF9YB7a3f1e4R48oH"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/79ea7775ce7b94320a808664a59045a50e5ef7c9"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/d4a6afd50c327f549dda2e112985ee233f5bd908"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/7669af0de1d21cf4eff7e92ab52965fec2354e1a"",
          ""width"" : 64
        } ],
        ""name"" : ""King's Dead (with Kendrick Lamar, Future & James Blake)"",
        ""release_date"" : ""2018-01-12"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1NXM5lF9YB7a3f1e4R48oH""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/28ExwzUQsvgJooOI0X1mr3""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/28ExwzUQsvgJooOI0X1mr3"",
        ""id"" : ""28ExwzUQsvgJooOI0X1mr3"",
        ""name"" : ""Jay Rock"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:28ExwzUQsvgJooOI0X1mr3""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
        ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
        ""name"" : ""Kendrick Lamar"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1RyvyyTE3xzB2ZywiAwp0i""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1RyvyyTE3xzB2ZywiAwp0i"",
        ""id"" : ""1RyvyyTE3xzB2ZywiAwp0i"",
        ""name"" : ""Future"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1RyvyyTE3xzB2ZywiAwp0i""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/53KwLdlmrlCelAZMaLVZqU""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/53KwLdlmrlCelAZMaLVZqU"",
        ""id"" : ""53KwLdlmrlCelAZMaLVZqU"",
        ""name"" : ""James Blake"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:53KwLdlmrlCelAZMaLVZqU""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 229670,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71714093""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/51rXHuKN8Loc4sUlKPODgH""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/51rXHuKN8Loc4sUlKPODgH"",
      ""id"" : ""51rXHuKN8Loc4sUlKPODgH"",
      ""name"" : ""King's Dead (with Kendrick Lamar, Future & James Blake)"",
      ""popularity"" : 87,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:51rXHuKN8Loc4sUlKPODgH""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/5pUo3fmmHT8bhCyHE52hA6""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/5pUo3fmmHT8bhCyHE52hA6"",
          ""id"" : ""5pUo3fmmHT8bhCyHE52hA6"",
          ""name"" : ""Liam Payne"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:5pUo3fmmHT8bhCyHE52hA6""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/5CCwRZC6euC8Odo6y9X8jr""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/5CCwRZC6euC8Odo6y9X8jr"",
          ""id"" : ""5CCwRZC6euC8Odo6y9X8jr"",
          ""name"" : ""Rita Ora"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:5CCwRZC6euC8Odo6y9X8jr""
        } ],
        ""available_markets"" : [ ""SE"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/05qNixNs1TgA2oZ3cvNsVB""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/05qNixNs1TgA2oZ3cvNsVB"",
        ""id"" : ""05qNixNs1TgA2oZ3cvNsVB"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/54c0a737714a3668363b8da4f79ce95ace3be726"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/f74cbd82140afe2ac5bded5b08518b4025743519"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/ae37a8e3d2f6f7909820f5f420ba44e25522573e"",
          ""width"" : 64
        } ],
        ""name"" : ""For You (Fifty Shades Freed) [From \""Fifty Shades Freed (Original Motion Picture Soundtrack)\""]"",
        ""release_date"" : ""2018-01-05"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:05qNixNs1TgA2oZ3cvNsVB""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/5pUo3fmmHT8bhCyHE52hA6""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/5pUo3fmmHT8bhCyHE52hA6"",
        ""id"" : ""5pUo3fmmHT8bhCyHE52hA6"",
        ""name"" : ""Liam Payne"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:5pUo3fmmHT8bhCyHE52hA6""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/5CCwRZC6euC8Odo6y9X8jr""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/5CCwRZC6euC8Odo6y9X8jr"",
        ""id"" : ""5CCwRZC6euC8Odo6y9X8jr"",
        ""name"" : ""Rita Ora"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:5CCwRZC6euC8Odo6y9X8jr""
      } ],
      ""available_markets"" : [ ""SE"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 244293,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USQ4E1703341""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/5hM6sP0Gh1jD57drszNueC""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/5hM6sP0Gh1jD57drszNueC"",
      ""id"" : ""5hM6sP0Gh1jD57drszNueC"",
      ""name"" : ""For You (Fifty Shades Freed) (& Rita Ora)"",
      ""popularity"" : 83,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:5hM6sP0Gh1jD57drszNueC""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/5Rl15oVamLq7FbSb0NNBNy""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/5Rl15oVamLq7FbSb0NNBNy"",
          ""id"" : ""5Rl15oVamLq7FbSb0NNBNy"",
          ""name"" : ""5 Seconds of Summer"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:5Rl15oVamLq7FbSb0NNBNy""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/0ALkEC4EjIkbBkrXckWbbi""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/0ALkEC4EjIkbBkrXckWbbi"",
        ""id"" : ""0ALkEC4EjIkbBkrXckWbbi"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/bd5cad24b1b2c1e73947fdfd3e05fb05de0a0172"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/0573ba959cb911053acd47d976284f51715cfb72"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/94387e263c161e11304ff219683f3554dbff00a5"",
          ""width"" : 64
        } ],
        ""name"" : ""Want You Back"",
        ""release_date"" : ""2018-02-22"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:0ALkEC4EjIkbBkrXckWbbi""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/5Rl15oVamLq7FbSb0NNBNy""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/5Rl15oVamLq7FbSb0NNBNy"",
        ""id"" : ""5Rl15oVamLq7FbSb0NNBNy"",
        ""name"" : ""5 Seconds of Summer"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:5Rl15oVamLq7FbSb0NNBNy""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 173073,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""GBUM71800363""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/7HB7Wj26nYVo8CquzM7yX3""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/7HB7Wj26nYVo8CquzM7yX3"",
      ""id"" : ""7HB7Wj26nYVo8CquzM7yX3"",
      ""name"" : ""Want You Back"",
      ""popularity"" : 78,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:7HB7Wj26nYVo8CquzM7yX3""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/31TPClRtHm23RisEBtV3X7""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/31TPClRtHm23RisEBtV3X7"",
          ""id"" : ""31TPClRtHm23RisEBtV3X7"",
          ""name"" : ""Justin Timberlake"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:31TPClRtHm23RisEBtV3X7""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7Hau1KXnaqrXt4JMx6DS4Y""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7Hau1KXnaqrXt4JMx6DS4Y"",
        ""id"" : ""7Hau1KXnaqrXt4JMx6DS4Y"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/cca4e1bd336d756d6265a3f47ef88596d05cc8ec"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/06ef45d49010cd24cd77f0115c402706a0310543"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/19a1c18ebc493b9f8c7786e6a599976bd30105e1"",
          ""width"" : 64
        } ],
        ""name"" : ""Say Something"",
        ""release_date"" : ""2018-01-25"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7Hau1KXnaqrXt4JMx6DS4Y""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/31TPClRtHm23RisEBtV3X7""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/31TPClRtHm23RisEBtV3X7"",
        ""id"" : ""31TPClRtHm23RisEBtV3X7"",
        ""name"" : ""Justin Timberlake"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:31TPClRtHm23RisEBtV3X7""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4YLtscXsxbVgi031ovDDdh""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4YLtscXsxbVgi031ovDDdh"",
        ""id"" : ""4YLtscXsxbVgi031ovDDdh"",
        ""name"" : ""Chris Stapleton"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4YLtscXsxbVgi031ovDDdh""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 278893,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USRC11703503""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/09ts3GnICqYEU5PkQCpJK3""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/09ts3GnICqYEU5PkQCpJK3"",
      ""id"" : ""09ts3GnICqYEU5PkQCpJK3"",
      ""name"" : ""Say Something"",
      ""popularity"" : 89,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/44072346a65f52cfff68599eaf94848074eab9c0?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:09ts3GnICqYEU5PkQCpJK3""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4TEJudQY2pXxVHPE3gD2EU""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4TEJudQY2pXxVHPE3gD2EU"",
          ""id"" : ""4TEJudQY2pXxVHPE3gD2EU"",
          ""name"" : ""BlocBoy JB"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4TEJudQY2pXxVHPE3gD2EU""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3TVXtAsR1Inumwj472S9r4""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3TVXtAsR1Inumwj472S9r4"",
          ""id"" : ""3TVXtAsR1Inumwj472S9r4"",
          ""name"" : ""Drake"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3TVXtAsR1Inumwj472S9r4""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7GGoJfKFOwDNuiLjjfzyCS""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7GGoJfKFOwDNuiLjjfzyCS"",
        ""id"" : ""7GGoJfKFOwDNuiLjjfzyCS"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/6b7a1cdbf9ebfd3ca696d597136b51fbf8d15471"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/7629502bb5e7dc2d6eac4342f553b89141d3f006"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/30b01ebddb7b9843d6210a7ef32127327ebb276c"",
          ""width"" : 64
        } ],
        ""name"" : ""Look Alive (feat. Drake)"",
        ""release_date"" : ""2018-02-09"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7GGoJfKFOwDNuiLjjfzyCS""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4TEJudQY2pXxVHPE3gD2EU""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4TEJudQY2pXxVHPE3gD2EU"",
        ""id"" : ""4TEJudQY2pXxVHPE3gD2EU"",
        ""name"" : ""BlocBoy JB"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4TEJudQY2pXxVHPE3gD2EU""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3TVXtAsR1Inumwj472S9r4""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3TVXtAsR1Inumwj472S9r4"",
        ""id"" : ""3TVXtAsR1Inumwj472S9r4"",
        ""name"" : ""Drake"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3TVXtAsR1Inumwj472S9r4""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 181263,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USWB11800211""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/4qKcDkK6siZ7Jp1Jb4m0aL""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/4qKcDkK6siZ7Jp1Jb4m0aL"",
      ""id"" : ""4qKcDkK6siZ7Jp1Jb4m0aL"",
      ""name"" : ""Look Alive (feat. Drake)"",
      ""popularity"" : 95,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/4c9c1f93e4b6032679d772a0a8c3c28cef5a42ae?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:4qKcDkK6siZ7Jp1Jb4m0aL""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/69GGBxA162lTqCwzJG5jLp""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/69GGBxA162lTqCwzJG5jLp"",
          ""id"" : ""69GGBxA162lTqCwzJG5jLp"",
          ""name"" : ""The Chainsmokers"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:69GGBxA162lTqCwzJG5jLp""
        } ],
        ""available_markets"" : [ ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/2QI0UclC8ipuXoyCva1C7K""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/2QI0UclC8ipuXoyCva1C7K"",
        ""id"" : ""2QI0UclC8ipuXoyCva1C7K"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/97cec10e75a0cbb85013f29c5d40093ef9d2899d"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/12328cad2427a0d5fd8559bd1f9d9728065ab75d"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/b2e98aec82f38b3381eeae05ba43a7020dcd93f0"",
          ""width"" : 64
        } ],
        ""name"" : ""Sick Boy"",
        ""release_date"" : ""2018-01-17"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:2QI0UclC8ipuXoyCva1C7K""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/69GGBxA162lTqCwzJG5jLp""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/69GGBxA162lTqCwzJG5jLp"",
        ""id"" : ""69GGBxA162lTqCwzJG5jLp"",
        ""name"" : ""The Chainsmokers"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:69GGBxA162lTqCwzJG5jLp""
      } ],
      ""available_markets"" : [ ],
      ""disc_number"" : 1,
      ""duration_ms"" : 193893,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USQX91702676""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/7jqDzXJS0K0Re8uphYNit0""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/7jqDzXJS0K0Re8uphYNit0"",
      ""id"" : ""7jqDzXJS0K0Re8uphYNit0"",
      ""name"" : ""Sick Boy"",
      ""popularity"" : 91,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:7jqDzXJS0K0Re8uphYNit0""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1Cs0zKBU1kc0i8ypK3B9ai""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1Cs0zKBU1kc0i8ypK3B9ai"",
          ""id"" : ""1Cs0zKBU1kc0i8ypK3B9ai"",
          ""name"" : ""David Guetta"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1Cs0zKBU1kc0i8ypK3B9ai""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/60d24wfXkVzDSfLS6hyCjZ""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/60d24wfXkVzDSfLS6hyCjZ"",
          ""id"" : ""60d24wfXkVzDSfLS6hyCjZ"",
          ""name"" : ""Martin Garrix"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:60d24wfXkVzDSfLS6hyCjZ""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4mHAu7NX2UNsnGXjviBD9e""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4mHAu7NX2UNsnGXjviBD9e"",
          ""id"" : ""4mHAu7NX2UNsnGXjviBD9e"",
          ""name"" : ""Brooks"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4mHAu7NX2UNsnGXjviBD9e""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5oU1ROIHS6IOWnb87GWhqU""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5oU1ROIHS6IOWnb87GWhqU"",
        ""id"" : ""5oU1ROIHS6IOWnb87GWhqU"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/2e55b9c247cc6b0b713fbbce9db0527a932f8748"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/1143cd331d7ff1c105fa8140d00edf286fb9f9a5"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/eb33214151082431206ab6e1ad4bbf83f487d58e"",
          ""width"" : 64
        } ],
        ""name"" : ""Like I Do"",
        ""release_date"" : ""2018-02-22"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5oU1ROIHS6IOWnb87GWhqU""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1Cs0zKBU1kc0i8ypK3B9ai""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1Cs0zKBU1kc0i8ypK3B9ai"",
        ""id"" : ""1Cs0zKBU1kc0i8ypK3B9ai"",
        ""name"" : ""David Guetta"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1Cs0zKBU1kc0i8ypK3B9ai""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/60d24wfXkVzDSfLS6hyCjZ""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/60d24wfXkVzDSfLS6hyCjZ"",
        ""id"" : ""60d24wfXkVzDSfLS6hyCjZ"",
        ""name"" : ""Martin Garrix"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:60d24wfXkVzDSfLS6hyCjZ""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4mHAu7NX2UNsnGXjviBD9e""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4mHAu7NX2UNsnGXjviBD9e"",
        ""id"" : ""4mHAu7NX2UNsnGXjviBD9e"",
        ""name"" : ""Brooks"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4mHAu7NX2UNsnGXjviBD9e""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 202500,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""GB28K1820005""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6RnkFd8Fqqgk1Uni8RgqCQ""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6RnkFd8Fqqgk1Uni8RgqCQ"",
      ""id"" : ""6RnkFd8Fqqgk1Uni8RgqCQ"",
      ""name"" : ""Like I Do"",
      ""popularity"" : 82,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/6c6d08d072afd9e943988190a29a484055376fd0?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6RnkFd8Fqqgk1Uni8RgqCQ""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/0du5cEVh5yTK9QJze8zA0C""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/0du5cEVh5yTK9QJze8zA0C"",
          ""id"" : ""0du5cEVh5yTK9QJze8zA0C"",
          ""name"" : ""Bruno Mars"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:0du5cEVh5yTK9QJze8zA0C""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4kYSro6naA4h99UJvo89HB""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4kYSro6naA4h99UJvo89HB"",
          ""id"" : ""4kYSro6naA4h99UJvo89HB"",
          ""name"" : ""Cardi B"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4kYSro6naA4h99UJvo89HB""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3mumK2ar9b4JPhVOZR0V2p""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3mumK2ar9b4JPhVOZR0V2p"",
        ""id"" : ""3mumK2ar9b4JPhVOZR0V2p"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/816dcd7b9b6651dd39ef483f41583514a3acad3c"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/056b3651a4483097848b5bf9f0b8903487a5b669"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/4a56ddc69f70e5648b0b6d6d45e466512eeed35a"",
          ""width"" : 64
        } ],
        ""name"" : ""Finesse (Remix) [feat. Cardi B]"",
        ""release_date"" : ""2017-12-20"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3mumK2ar9b4JPhVOZR0V2p""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/0du5cEVh5yTK9QJze8zA0C""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/0du5cEVh5yTK9QJze8zA0C"",
        ""id"" : ""0du5cEVh5yTK9QJze8zA0C"",
        ""name"" : ""Bruno Mars"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:0du5cEVh5yTK9QJze8zA0C""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4kYSro6naA4h99UJvo89HB""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4kYSro6naA4h99UJvo89HB"",
        ""id"" : ""4kYSro6naA4h99UJvo89HB"",
        ""name"" : ""Cardi B"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4kYSro6naA4h99UJvo89HB""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 217288,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USAT21705441""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/3Vo4wInECJQuz9BIBMOu8i""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/3Vo4wInECJQuz9BIBMOu8i"",
      ""id"" : ""3Vo4wInECJQuz9BIBMOu8i"",
      ""name"" : ""Finesse (Remix) [feat. Cardi B]"",
      ""popularity"" : 97,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/10fbdd7986417e30e3bbf06d3fa22efe1663b2ee?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:3Vo4wInECJQuz9BIBMOu8i""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/5JZ7CnR6gTvEMKX4g70Amv""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/5JZ7CnR6gTvEMKX4g70Amv"",
          ""id"" : ""5JZ7CnR6gTvEMKX4g70Amv"",
          ""name"" : ""Lauv"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:5JZ7CnR6gTvEMKX4g70Amv""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3RZDzcVgDoyt1LB5fhnB6x""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3RZDzcVgDoyt1LB5fhnB6x"",
        ""id"" : ""3RZDzcVgDoyt1LB5fhnB6x"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/f95fc296f26df7349587b41a0fbff48389670f6c"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/a71ab67e3722b488b353018a0b0c2072ae6a4081"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/a8b9849ce77850e0e34eaf14e65478d8513e5ee9"",
          ""width"" : 64
        } ],
        ""name"" : ""Getting Over You"",
        ""release_date"" : ""2018-02-14"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3RZDzcVgDoyt1LB5fhnB6x""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/5JZ7CnR6gTvEMKX4g70Amv""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/5JZ7CnR6gTvEMKX4g70Amv"",
        ""id"" : ""5JZ7CnR6gTvEMKX4g70Amv"",
        ""name"" : ""Lauv"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:5JZ7CnR6gTvEMKX4g70Amv""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 255885,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""GBWWP1803555""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/2BNkTvg1kHAAfGvqh56x5a""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/2BNkTvg1kHAAfGvqh56x5a"",
      ""id"" : ""2BNkTvg1kHAAfGvqh56x5a"",
      ""name"" : ""Getting Over You"",
      ""popularity"" : 82,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/5705186ac90104a4cc440cc688cf5eb3ae03077e?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:2BNkTvg1kHAAfGvqh56x5a""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/6s22t5Y3prQHyaHWUN1R1C""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/6s22t5Y3prQHyaHWUN1R1C"",
          ""id"" : ""6s22t5Y3prQHyaHWUN1R1C"",
          ""name"" : ""AJR"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:6s22t5Y3prQHyaHWUN1R1C""
        } ],
        ""available_markets"" : [ ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/6hpiZXzsZxlkTXY8zavu3N""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/6hpiZXzsZxlkTXY8zavu3N"",
        ""id"" : ""6hpiZXzsZxlkTXY8zavu3N"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/79b4ce3c6c0f88e0c3a12bfd7fb49b02568459f7"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/f51328284c20a6bd02d58e2752b453da76c4b263"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/004a17055ccba2aea9e10f12d10abd6653dca955"",
          ""width"" : 64
        } ],
        ""name"" : ""The Click"",
        ""release_date"" : ""2017-06-09"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:6hpiZXzsZxlkTXY8zavu3N""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6s22t5Y3prQHyaHWUN1R1C""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6s22t5Y3prQHyaHWUN1R1C"",
        ""id"" : ""6s22t5Y3prQHyaHWUN1R1C"",
        ""name"" : ""AJR"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6s22t5Y3prQHyaHWUN1R1C""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4LAz9VRX8Nat9kvIzgkg2v""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4LAz9VRX8Nat9kvIzgkg2v"",
        ""id"" : ""4LAz9VRX8Nat9kvIzgkg2v"",
        ""name"" : ""Rivers Cuomo"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4LAz9VRX8Nat9kvIzgkg2v""
      } ],
      ""available_markets"" : [ ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 218763,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""QMRSZ1701240""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/2QCndYqRherBtKjBpyySC6""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/2QCndYqRherBtKjBpyySC6"",
      ""id"" : ""2QCndYqRherBtKjBpyySC6"",
      ""name"" : ""Sober Up"",
      ""popularity"" : 79,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/2d132ed179e45df4f9f7bbc36c0cd7ea842567b7?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 4,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:2QCndYqRherBtKjBpyySC6""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4nDoRrQiYLoBzwC5BhVJzF""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4nDoRrQiYLoBzwC5BhVJzF"",
          ""id"" : ""4nDoRrQiYLoBzwC5BhVJzF"",
          ""name"" : ""Camila Cabello"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4nDoRrQiYLoBzwC5BhVJzF""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3HR8mnPQ7Go27WPMTNR2um""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3HR8mnPQ7Go27WPMTNR2um"",
        ""id"" : ""3HR8mnPQ7Go27WPMTNR2um"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/8962f25dbafbc90b040dba624a00c7346396064f"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/8a0d9b9ee342bd097a068d92da1ef711aea16121"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/b6c8545159775543f97cecb2ec8a180e906f7a9d"",
          ""width"" : 64
        } ],
        ""name"" : ""Never Be the Same"",
        ""release_date"" : ""2017-12-17"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3HR8mnPQ7Go27WPMTNR2um""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4nDoRrQiYLoBzwC5BhVJzF""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4nDoRrQiYLoBzwC5BhVJzF"",
        ""id"" : ""4nDoRrQiYLoBzwC5BhVJzF"",
        ""name"" : ""Camila Cabello"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4nDoRrQiYLoBzwC5BhVJzF""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 227087,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USSM11710323""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6VpQdig9pdpTSIFItgkJV5""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6VpQdig9pdpTSIFItgkJV5"",
      ""id"" : ""6VpQdig9pdpTSIFItgkJV5"",
      ""name"" : ""Never Be the Same"",
      ""popularity"" : 91,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/6f69fa346305ec36d6849b1b9745715d2bead70f?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6VpQdig9pdpTSIFItgkJV5""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3Isy6kedDrgPYoTS1dazA9""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3Isy6kedDrgPYoTS1dazA9"",
          ""id"" : ""3Isy6kedDrgPYoTS1dazA9"",
          ""name"" : ""Sean Paul"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3Isy6kedDrgPYoTS1dazA9""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1Cs0zKBU1kc0i8ypK3B9ai""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1Cs0zKBU1kc0i8ypK3B9ai"",
          ""id"" : ""1Cs0zKBU1kc0i8ypK3B9ai"",
          ""name"" : ""David Guetta"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1Cs0zKBU1kc0i8ypK3B9ai""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4obzFoKoKRHIphyHzJ35G3""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4obzFoKoKRHIphyHzJ35G3"",
          ""id"" : ""4obzFoKoKRHIphyHzJ35G3"",
          ""name"" : ""Becky G"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4obzFoKoKRHIphyHzJ35G3""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7rip3duZDpr0mHBsCXrpmy""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7rip3duZDpr0mHBsCXrpmy"",
        ""id"" : ""7rip3duZDpr0mHBsCXrpmy"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/ae0b8df93319fe5d35ba1976dec3439f921b5fe7"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/df048557cc36c0f68aa23f00d982309e0a361689"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/a25db6a002e09ce26cd6d3ed1879f4174cf390a6"",
          ""width"" : 64
        } ],
        ""name"" : ""Mad Love"",
        ""release_date"" : ""2018-02-15"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7rip3duZDpr0mHBsCXrpmy""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3Isy6kedDrgPYoTS1dazA9""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3Isy6kedDrgPYoTS1dazA9"",
        ""id"" : ""3Isy6kedDrgPYoTS1dazA9"",
        ""name"" : ""Sean Paul"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3Isy6kedDrgPYoTS1dazA9""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1Cs0zKBU1kc0i8ypK3B9ai""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1Cs0zKBU1kc0i8ypK3B9ai"",
        ""id"" : ""1Cs0zKBU1kc0i8ypK3B9ai"",
        ""name"" : ""David Guetta"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1Cs0zKBU1kc0i8ypK3B9ai""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4obzFoKoKRHIphyHzJ35G3""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4obzFoKoKRHIphyHzJ35G3"",
        ""id"" : ""4obzFoKoKRHIphyHzJ35G3"",
        ""name"" : ""Becky G"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4obzFoKoKRHIphyHzJ35G3""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 199944,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""GBUM71800437""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6y68QK2SwC38YxsbxHrA8I""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6y68QK2SwC38YxsbxHrA8I"",
      ""id"" : ""6y68QK2SwC38YxsbxHrA8I"",
      ""name"" : ""Mad Love"",
      ""popularity"" : 71,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6y68QK2SwC38YxsbxHrA8I""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4GvEc3ANtPPjt1ZJllr5Zl""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4GvEc3ANtPPjt1ZJllr5Zl"",
          ""id"" : ""4GvEc3ANtPPjt1ZJllr5Zl"",
          ""name"" : ""Bazzi"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4GvEc3ANtPPjt1ZJllr5Zl""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1psfXtJLKUMVzQPMgXfB38""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1psfXtJLKUMVzQPMgXfB38"",
        ""id"" : ""1psfXtJLKUMVzQPMgXfB38"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/6a64160f0abaefd489b2be0540a60ba762b9f01c"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/e3e7fdef31a5ab0fee196f54b6267ecc0d832aa9"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/78fe56909857c8cc04582d71debdc4bf2a6e3827"",
          ""width"" : 64
        } ],
        ""name"" : ""Beautiful"",
        ""release_date"" : ""2017-09-18"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1psfXtJLKUMVzQPMgXfB38""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4GvEc3ANtPPjt1ZJllr5Zl""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4GvEc3ANtPPjt1ZJllr5Zl"",
        ""id"" : ""4GvEc3ANtPPjt1ZJllr5Zl"",
        ""name"" : ""Bazzi"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4GvEc3ANtPPjt1ZJllr5Zl""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 180253,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""TCADE1707724""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/1OilOIju6R9ia8vilNjChh""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/1OilOIju6R9ia8vilNjChh"",
      ""id"" : ""1OilOIju6R9ia8vilNjChh"",
      ""name"" : ""Beautiful"",
      ""popularity"" : 82,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/1b56ec4789840b5f42d1026b691d333b98e97329?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:1OilOIju6R9ia8vilNjChh""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/1pPmIToKXyGdsCF6LmqLmI""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/1pPmIToKXyGdsCF6LmqLmI"",
          ""id"" : ""1pPmIToKXyGdsCF6LmqLmI"",
          ""name"" : ""Rich The Kid"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:1pPmIToKXyGdsCF6LmqLmI""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5LaCH6lgcr6LaNdb3ldZrp""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5LaCH6lgcr6LaNdb3ldZrp"",
        ""id"" : ""5LaCH6lgcr6LaNdb3ldZrp"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/110d168ef4ffd7358987d23e60ab748c25168379"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/c05a6f8c18e8f77f881c07c236f60bb81306f89d"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/3a8eb971e536d96cab3b816dabbd779a34b24806"",
          ""width"" : 64
        } ],
        ""name"" : ""New Freezer (feat. Kendrick Lamar)"",
        ""release_date"" : ""2017-09-25"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5LaCH6lgcr6LaNdb3ldZrp""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/1pPmIToKXyGdsCF6LmqLmI""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/1pPmIToKXyGdsCF6LmqLmI"",
        ""id"" : ""1pPmIToKXyGdsCF6LmqLmI"",
        ""name"" : ""Rich The Kid"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:1pPmIToKXyGdsCF6LmqLmI""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
        ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
        ""name"" : ""Kendrick Lamar"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 193173,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71708973""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/4pYZLpX23Vx8rwDpJCpPTA""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/4pYZLpX23Vx8rwDpJCpPTA"",
      ""id"" : ""4pYZLpX23Vx8rwDpJCpPTA"",
      ""name"" : ""New Freezer (feat. Kendrick Lamar)"",
      ""popularity"" : 85,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:4pYZLpX23Vx8rwDpJCpPTA""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/64M6ah0SkkRsnPGtGiRAbb""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/64M6ah0SkkRsnPGtGiRAbb"",
          ""id"" : ""64M6ah0SkkRsnPGtGiRAbb"",
          ""name"" : ""Bebe Rexha"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:64M6ah0SkkRsnPGtGiRAbb""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/6t5D6LEgHxqUVOxJItkzfb""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/6t5D6LEgHxqUVOxJItkzfb"",
        ""id"" : ""6t5D6LEgHxqUVOxJItkzfb"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/ba99c03404941dc54d0340da28e368fc7457e22a"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/c89ea26344e4eb73c641e9520d1df684c30722de"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/d0a9b0370891fe68f68467ea32cb60a35e346bf5"",
          ""width"" : 64
        } ],
        ""name"" : ""All Your Fault: Pt. 2"",
        ""release_date"" : ""2017-08-11"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:6t5D6LEgHxqUVOxJItkzfb""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/64M6ah0SkkRsnPGtGiRAbb""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/64M6ah0SkkRsnPGtGiRAbb"",
        ""id"" : ""64M6ah0SkkRsnPGtGiRAbb"",
        ""name"" : ""Bebe Rexha"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:64M6ah0SkkRsnPGtGiRAbb""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3b8QkneNDz4JHKKKlLgYZg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3b8QkneNDz4JHKKKlLgYZg"",
        ""id"" : ""3b8QkneNDz4JHKKKlLgYZg"",
        ""name"" : ""Florida Georgia Line"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3b8QkneNDz4JHKKKlLgYZg""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 163870,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USWB11701181""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/7iDa6hUg2VgEL1o1HjmfBn""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/7iDa6hUg2VgEL1o1HjmfBn"",
      ""id"" : ""7iDa6hUg2VgEL1o1HjmfBn"",
      ""name"" : ""Meant to Be (feat. Florida Georgia Line)"",
      ""popularity"" : 96,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/8965aca26c5af239f57cb6ec1570db1fd8be00f1?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 6,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:7iDa6hUg2VgEL1o1HjmfBn""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/6oMuImdp5ZcFhWP0ESe6mG""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/6oMuImdp5ZcFhWP0ESe6mG"",
          ""id"" : ""6oMuImdp5ZcFhWP0ESe6mG"",
          ""name"" : ""Migos"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:6oMuImdp5ZcFhWP0ESe6mG""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5ZnasU3vhbAxJxUYNXeqOq""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5ZnasU3vhbAxJxUYNXeqOq"",
        ""id"" : ""5ZnasU3vhbAxJxUYNXeqOq"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/1e7252d965c8f4ae5e191b57c1c82d7ea120fd7b"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/f5c897a632e2aa22e912f65cad2a0d4074f7e440"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/855661033f6180bb8468ca085d7b57859d70375c"",
          ""width"" : 64
        } ],
        ""name"" : ""Culture II"",
        ""release_date"" : ""2018-01-26"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5ZnasU3vhbAxJxUYNXeqOq""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6oMuImdp5ZcFhWP0ESe6mG""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6oMuImdp5ZcFhWP0ESe6mG"",
        ""id"" : ""6oMuImdp5ZcFhWP0ESe6mG"",
        ""name"" : ""Migos"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6oMuImdp5ZcFhWP0ESe6mG""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 255378,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71800980""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6DY2TWhO7ioSmVzK3kuHk8""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6DY2TWhO7ioSmVzK3kuHk8"",
      ""id"" : ""6DY2TWhO7ioSmVzK3kuHk8"",
      ""name"" : ""Narcos"",
      ""popularity"" : 82,
      ""preview_url"" : null,
      ""track_number"" : 3,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6DY2TWhO7ioSmVzK3kuHk8""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/14rP13jdQNgQvuPA2AkBgm""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/14rP13jdQNgQvuPA2AkBgm"",
          ""id"" : ""14rP13jdQNgQvuPA2AkBgm"",
          ""name"" : ""Glades"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:14rP13jdQNgQvuPA2AkBgm""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3xrrgkDzo0f6BPprS8tih4""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3xrrgkDzo0f6BPprS8tih4"",
        ""id"" : ""3xrrgkDzo0f6BPprS8tih4"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/97b9c5d8b62805f8f863a517cc2303660f4cc7eb"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/539e407dcb821b101eded2da4e4f064642ba322f"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/65bd1c0a3360724565b917f167cf979cd72af9f0"",
          ""width"" : 64
        } ],
        ""name"" : ""Do Right"",
        ""release_date"" : ""2017-12-15"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3xrrgkDzo0f6BPprS8tih4""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/14rP13jdQNgQvuPA2AkBgm""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/14rP13jdQNgQvuPA2AkBgm"",
        ""id"" : ""14rP13jdQNgQvuPA2AkBgm"",
        ""name"" : ""Glades"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:14rP13jdQNgQvuPA2AkBgm""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 194988,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""TCADJ1751740""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/45w70aQhNwke1yrQZO0ffm""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/45w70aQhNwke1yrQZO0ffm"",
      ""id"" : ""45w70aQhNwke1yrQZO0ffm"",
      ""name"" : ""Do Right"",
      ""popularity"" : 84,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/9264e15d97d3acc5af1f4f0d5ff9a2dbea82f34e?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:45w70aQhNwke1yrQZO0ffm""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2LZDXcxJWgsJfKXZv9a5eG""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2LZDXcxJWgsJfKXZv9a5eG"",
          ""id"" : ""2LZDXcxJWgsJfKXZv9a5eG"",
          ""name"" : ""Cashmere Cat"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2LZDXcxJWgsJfKXZv9a5eG""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/738wLrAtLtCtFOLvQBXOXp""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/738wLrAtLtCtFOLvQBXOXp"",
          ""id"" : ""738wLrAtLtCtFOLvQBXOXp"",
          ""name"" : ""Major Lazer"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:738wLrAtLtCtFOLvQBXOXp""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2jku7tDXc6XoB6MO2hFuqg""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2jku7tDXc6XoB6MO2hFuqg"",
          ""id"" : ""2jku7tDXc6XoB6MO2hFuqg"",
          ""name"" : ""Tory Lanez"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2jku7tDXc6XoB6MO2hFuqg""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3ZtKFp4rTpRzADaAZPAYdI""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3ZtKFp4rTpRzADaAZPAYdI"",
        ""id"" : ""3ZtKFp4rTpRzADaAZPAYdI"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/ab359616b5e2270d9473ade93838fc23e3017b81"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/52a6f220165689468f56abf309e3833630cccab5"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/c8ac3f58937bc25b7960f9a238ab9ea1aac3554f"",
          ""width"" : 64
        } ],
        ""name"" : ""Miss You (with Major Lazer & Tory Lanez)"",
        ""release_date"" : ""2018-01-16"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3ZtKFp4rTpRzADaAZPAYdI""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2LZDXcxJWgsJfKXZv9a5eG""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2LZDXcxJWgsJfKXZv9a5eG"",
        ""id"" : ""2LZDXcxJWgsJfKXZv9a5eG"",
        ""name"" : ""Cashmere Cat"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2LZDXcxJWgsJfKXZv9a5eG""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/738wLrAtLtCtFOLvQBXOXp""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/738wLrAtLtCtFOLvQBXOXp"",
        ""id"" : ""738wLrAtLtCtFOLvQBXOXp"",
        ""name"" : ""Major Lazer"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:738wLrAtLtCtFOLvQBXOXp""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2jku7tDXc6XoB6MO2hFuqg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2jku7tDXc6XoB6MO2hFuqg"",
        ""id"" : ""2jku7tDXc6XoB6MO2hFuqg"",
        ""name"" : ""Tory Lanez"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2jku7tDXc6XoB6MO2hFuqg""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 186231,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USUM71713848""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/0as7lPxkBPctZ7uVmchRNM""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/0as7lPxkBPctZ7uVmchRNM"",
      ""id"" : ""0as7lPxkBPctZ7uVmchRNM"",
      ""name"" : ""Miss You (with Major Lazer & Tory Lanez)"",
      ""popularity"" : 83,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:0as7lPxkBPctZ7uVmchRNM""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/360IAlyVv4PCEVjgyMZrxK""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/360IAlyVv4PCEVjgyMZrxK"",
          ""id"" : ""360IAlyVv4PCEVjgyMZrxK"",
          ""name"" : ""Miguel"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:360IAlyVv4PCEVjgyMZrxK""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/05LEST8E8mkEIl2LRfUkcI""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/05LEST8E8mkEIl2LRfUkcI"",
        ""id"" : ""05LEST8E8mkEIl2LRfUkcI"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/2cd29bcaee7d0defb3293cf8c546070222c61d6f"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/ab501873fbf2500af9f4094f165cece8a3f178c1"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/ea8f2721de78df42a75b8ed545cacf6dbf0a4ea4"",
          ""width"" : 64
        } ],
        ""name"" : ""War & Leisure"",
        ""release_date"" : ""2017-12-01"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:05LEST8E8mkEIl2LRfUkcI""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/360IAlyVv4PCEVjgyMZrxK""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/360IAlyVv4PCEVjgyMZrxK"",
        ""id"" : ""360IAlyVv4PCEVjgyMZrxK"",
        ""name"" : ""Miguel"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:360IAlyVv4PCEVjgyMZrxK""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/0Y5tJX1MQlPlqiwlOH1tJY""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/0Y5tJX1MQlPlqiwlOH1tJY"",
        ""id"" : ""0Y5tJX1MQlPlqiwlOH1tJY"",
        ""name"" : ""Travis Scott"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:0Y5tJX1MQlPlqiwlOH1tJY""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 259333,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USRC11701703""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/5WoaF1B5XIEnWfmb5NZikf""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/5WoaF1B5XIEnWfmb5NZikf"",
      ""id"" : ""5WoaF1B5XIEnWfmb5NZikf"",
      ""name"" : ""Sky Walker"",
      ""popularity"" : 89,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/346fef42d6f419f25ce78866478f3d06637b05b4?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 3,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:5WoaF1B5XIEnWfmb5NZikf""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4Q6nIcaBED8qUel8bBx6Cr""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4Q6nIcaBED8qUel8bBx6Cr"",
          ""id"" : ""4Q6nIcaBED8qUel8bBx6Cr"",
          ""name"" : ""Jax Jones"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4Q6nIcaBED8qUel8bBx6Cr""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/7hssUdpvtY5oiARaUDgFZ3""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/7hssUdpvtY5oiARaUDgFZ3"",
          ""id"" : ""7hssUdpvtY5oiARaUDgFZ3"",
          ""name"" : ""Ina Wroldsen"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:7hssUdpvtY5oiARaUDgFZ3""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3NTHQYWOxCJ05u8fc80RNt""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3NTHQYWOxCJ05u8fc80RNt"",
        ""id"" : ""3NTHQYWOxCJ05u8fc80RNt"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/82097d87addca2d0586117c1f7015c8c2a62c2ab"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/ce870529e433d01cc99a0c9afd289daaf7851e99"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/2430271111a3c69bc1d21b62ef0474d8fa39c95e"",
          ""width"" : 64
        } ],
        ""name"" : ""Breathe"",
        ""release_date"" : ""2017-12-01"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3NTHQYWOxCJ05u8fc80RNt""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4Q6nIcaBED8qUel8bBx6Cr""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4Q6nIcaBED8qUel8bBx6Cr"",
        ""id"" : ""4Q6nIcaBED8qUel8bBx6Cr"",
        ""name"" : ""Jax Jones"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4Q6nIcaBED8qUel8bBx6Cr""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7hssUdpvtY5oiARaUDgFZ3""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7hssUdpvtY5oiARaUDgFZ3"",
        ""id"" : ""7hssUdpvtY5oiARaUDgFZ3"",
        ""name"" : ""Ina Wroldsen"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7hssUdpvtY5oiARaUDgFZ3""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 207629,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""GBUM71706192""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/0KsB4TwgATg88aXCMBoO3Y""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/0KsB4TwgATg88aXCMBoO3Y"",
      ""id"" : ""0KsB4TwgATg88aXCMBoO3Y"",
      ""name"" : ""Breathe"",
      ""popularity"" : 74,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:0KsB4TwgATg88aXCMBoO3Y""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/02kJSzxNuaWGqwubyUba0Z""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/02kJSzxNuaWGqwubyUba0Z"",
          ""id"" : ""02kJSzxNuaWGqwubyUba0Z"",
          ""name"" : ""G-Eazy"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:02kJSzxNuaWGqwubyUba0Z""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/26VFTg2z8YR0cCuwLzESi2""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/26VFTg2z8YR0cCuwLzESi2"",
          ""id"" : ""26VFTg2z8YR0cCuwLzESi2"",
          ""name"" : ""Halsey"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:26VFTg2z8YR0cCuwLzESi2""
        } ],
        ""available_markets"" : [ ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7ejLzpQCRcDqXtbRoOrj9B""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7ejLzpQCRcDqXtbRoOrj9B"",
        ""id"" : ""7ejLzpQCRcDqXtbRoOrj9B"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/0fb6ed2206afc16989444a74fd7b3ab1774d515d"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/c639617caf0ef5d736c0ca4f33badcc576a0d758"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/dc831ec735a8822390386750bc4a649769d7f3b7"",
          ""width"" : 64
        } ],
        ""name"" : ""Him & I"",
        ""release_date"" : ""2017-11-30"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7ejLzpQCRcDqXtbRoOrj9B""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/02kJSzxNuaWGqwubyUba0Z""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/02kJSzxNuaWGqwubyUba0Z"",
        ""id"" : ""02kJSzxNuaWGqwubyUba0Z"",
        ""name"" : ""G-Eazy"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:02kJSzxNuaWGqwubyUba0Z""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/26VFTg2z8YR0cCuwLzESi2""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/26VFTg2z8YR0cCuwLzESi2"",
        ""id"" : ""26VFTg2z8YR0cCuwLzESi2"",
        ""name"" : ""Halsey"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:26VFTg2z8YR0cCuwLzESi2""
      } ],
      ""available_markets"" : [ ],
      ""disc_number"" : 1,
      ""duration_ms"" : 268866,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USRC11701867""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/3YU6vJbjYUG0tiJyXf9x5V""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/3YU6vJbjYUG0tiJyXf9x5V"",
      ""id"" : ""3YU6vJbjYUG0tiJyXf9x5V"",
      ""name"" : ""Him & I"",
      ""popularity"" : 90,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:3YU6vJbjYUG0tiJyXf9x5V""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/246dkjvS1zLTtiykXe5h60""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/246dkjvS1zLTtiykXe5h60"",
          ""id"" : ""246dkjvS1zLTtiykXe5h60"",
          ""name"" : ""Post Malone"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:246dkjvS1zLTtiykXe5h60""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5s0rmjP8XOPhP6HhqOhuyC""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5s0rmjP8XOPhP6HhqOhuyC"",
        ""id"" : ""5s0rmjP8XOPhP6HhqOhuyC"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/17e68a2e03de74a114bf6609d69f9bdbf6029f70"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/f244af855d54ed944974c5bd2d0a862c6829efb9"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/9868b309bd47b61e045cca4addd911ccdec11961"",
          ""width"" : 64
        } ],
        ""name"" : ""Stoney (Deluxe)"",
        ""release_date"" : ""2016-12-09"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5s0rmjP8XOPhP6HhqOhuyC""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/246dkjvS1zLTtiykXe5h60""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/246dkjvS1zLTtiykXe5h60"",
        ""id"" : ""246dkjvS1zLTtiykXe5h60"",
        ""name"" : ""Post Malone"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:246dkjvS1zLTtiykXe5h60""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 223346,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71614475""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/75ZvA4QfFiZvzhj2xkaWAh""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/75ZvA4QfFiZvzhj2xkaWAh"",
      ""id"" : ""75ZvA4QfFiZvzhj2xkaWAh"",
      ""name"" : ""I Fall Apart"",
      ""popularity"" : 91,
      ""preview_url"" : null,
      ""track_number"" : 7,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:75ZvA4QfFiZvzhj2xkaWAh""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4VMYDCV2IEDYJArk749S6m""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4VMYDCV2IEDYJArk749S6m"",
          ""id"" : ""4VMYDCV2IEDYJArk749S6m"",
          ""name"" : ""Daddy Yankee"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4VMYDCV2IEDYJArk749S6m""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1copqxpiZy0dn219MSyPR7""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1copqxpiZy0dn219MSyPR7"",
        ""id"" : ""1copqxpiZy0dn219MSyPR7"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/7141681d8caa9016998072da17543d05e3f00735"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/4425500242c05ecb22d2b957d79313b72cef5a2d"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/3b85134c0c0ca27e1fe38996ae7d0f6c4a842be6"",
          ""width"" : 64
        } ],
        ""name"" : ""Dura"",
        ""release_date"" : ""2018-01-18"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1copqxpiZy0dn219MSyPR7""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4VMYDCV2IEDYJArk749S6m""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4VMYDCV2IEDYJArk749S6m"",
        ""id"" : ""4VMYDCV2IEDYJArk749S6m"",
        ""name"" : ""Daddy Yankee"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4VMYDCV2IEDYJArk749S6m""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 200480,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""US2BU1700200""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6KuqAtoeVzxAYOaMveLNpH""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6KuqAtoeVzxAYOaMveLNpH"",
      ""id"" : ""6KuqAtoeVzxAYOaMveLNpH"",
      ""name"" : ""Dura"",
      ""popularity"" : 88,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6KuqAtoeVzxAYOaMveLNpH""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/7tYKF4w9nC0nq9CsPZTHyP""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/7tYKF4w9nC0nq9CsPZTHyP"",
          ""id"" : ""7tYKF4w9nC0nq9CsPZTHyP"",
          ""name"" : ""SZA"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:7tYKF4w9nC0nq9CsPZTHyP""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/7CajNmpbOovFoOoasH2HaY""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/7CajNmpbOovFoOoasH2HaY"",
          ""id"" : ""7CajNmpbOovFoOoasH2HaY"",
          ""name"" : ""Calvin Harris"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:7CajNmpbOovFoOoasH2HaY""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5enEsi887wD3qGoMCK4jLr""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5enEsi887wD3qGoMCK4jLr"",
        ""id"" : ""5enEsi887wD3qGoMCK4jLr"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/f71edc4bd18f11f615f070d8d668cdce60d7d375"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/ff66d428466bb47f87d86264bd35b83538d389c2"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/bd65b7b5ae7032b637f33d82c45a7013dd2adc70"",
          ""width"" : 64
        } ],
        ""name"" : ""The Weekend (Funk Wav Remix)"",
        ""release_date"" : ""2017-12-15"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5enEsi887wD3qGoMCK4jLr""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7tYKF4w9nC0nq9CsPZTHyP""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7tYKF4w9nC0nq9CsPZTHyP"",
        ""id"" : ""7tYKF4w9nC0nq9CsPZTHyP"",
        ""name"" : ""SZA"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7tYKF4w9nC0nq9CsPZTHyP""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7CajNmpbOovFoOoasH2HaY""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7CajNmpbOovFoOoasH2HaY"",
        ""id"" : ""7CajNmpbOovFoOoasH2HaY"",
        ""name"" : ""Calvin Harris"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7CajNmpbOovFoOoasH2HaY""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 171805,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USRC11702939""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/0P6AWOA4LG1XOctzaVu5tt""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/0P6AWOA4LG1XOctzaVu5tt"",
      ""id"" : ""0P6AWOA4LG1XOctzaVu5tt"",
      ""name"" : ""The Weekend - Funk Wav Remix"",
      ""popularity"" : 87,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/215471369e674156c24562a8a49b745fe77b472d?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:0P6AWOA4LG1XOctzaVu5tt""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
          ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
          ""name"" : ""Kendrick Lamar"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/4eLPsYPBmXABThSJ821sqY""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/4eLPsYPBmXABThSJ821sqY"",
        ""id"" : ""4eLPsYPBmXABThSJ821sqY"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/661e1a935e2eacdd45c05ef618565535e7bed2ad"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/70429aaeceb7f8f6c087133382728223e0004b29"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/f2e751ee3dbfec80737094585f59a76806a51797"",
          ""width"" : 64
        } ],
        ""name"" : ""DAMN."",
        ""release_date"" : ""2017-04-14"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:4eLPsYPBmXABThSJ821sqY""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2YZyLoL8N0Wb9xBt1NhZWg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2YZyLoL8N0Wb9xBt1NhZWg"",
        ""id"" : ""2YZyLoL8N0Wb9xBt1NhZWg"",
        ""name"" : ""Kendrick Lamar"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2YZyLoL8N0Wb9xBt1NhZWg""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3qBKjEOanahMxlRojwCzhI""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3qBKjEOanahMxlRojwCzhI"",
        ""id"" : ""3qBKjEOanahMxlRojwCzhI"",
        ""name"" : ""Zacari"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3qBKjEOanahMxlRojwCzhI""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 213400,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71703088""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/6PGoSes0D9eUDeeAafB2As""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/6PGoSes0D9eUDeeAafB2As"",
      ""id"" : ""6PGoSes0D9eUDeeAafB2As"",
      ""name"" : ""LOVE. FEAT. ZACARI."",
      ""popularity"" : 89,
      ""preview_url"" : null,
      ""track_number"" : 10,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:6PGoSes0D9eUDeeAafB2As""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/7d3WFRME3vBY2cgoP38RDo""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/7d3WFRME3vBY2cgoP38RDo"",
          ""id"" : ""7d3WFRME3vBY2cgoP38RDo"",
          ""name"" : ""Lil Skies"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:7d3WFRME3vBY2cgoP38RDo""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/04ei5kNgmDuNAydFhhIHnD""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/04ei5kNgmDuNAydFhhIHnD"",
          ""id"" : ""04ei5kNgmDuNAydFhhIHnD"",
          ""name"" : ""Landon Cube"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:04ei5kNgmDuNAydFhhIHnD""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/6WRXWFeEuHKO11fRgMopzu""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/6WRXWFeEuHKO11fRgMopzu"",
        ""id"" : ""6WRXWFeEuHKO11fRgMopzu"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/14cfe6670c2d0b871f098e604806f25b76ff6956"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/e5eea949b07222706328ba45f8fa495e0cac94ef"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/51753a3988370227a0fece3262ea003f42ebafbb"",
          ""width"" : 64
        } ],
        ""name"" : ""Nowadays (feat. Landon Cube)"",
        ""release_date"" : ""2017-12-20"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:6WRXWFeEuHKO11fRgMopzu""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7d3WFRME3vBY2cgoP38RDo""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7d3WFRME3vBY2cgoP38RDo"",
        ""id"" : ""7d3WFRME3vBY2cgoP38RDo"",
        ""name"" : ""Lil Skies"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7d3WFRME3vBY2cgoP38RDo""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/04ei5kNgmDuNAydFhhIHnD""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/04ei5kNgmDuNAydFhhIHnD"",
        ""id"" : ""04ei5kNgmDuNAydFhhIHnD"",
        ""name"" : ""Landon Cube"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:04ei5kNgmDuNAydFhhIHnD""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 203907,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USAT21705314""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/689uBlyIufk2LUhAwjny4w""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/689uBlyIufk2LUhAwjny4w"",
      ""id"" : ""689uBlyIufk2LUhAwjny4w"",
      ""name"" : ""Nowadays (feat. Landon Cube)"",
      ""popularity"" : 86,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/7cc14bbfe96d99fc85079bdfe0e07a13a68837d7?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:689uBlyIufk2LUhAwjny4w""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/0haZhu4fFKt0Ag94kZDiz2""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/0haZhu4fFKt0Ag94kZDiz2"",
          ""id"" : ""0haZhu4fFKt0Ag94kZDiz2"",
          ""name"" : ""Sofia Reyes"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:0haZhu4fFKt0Ag94kZDiz2""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/07YZf4WDAMNwqr4jfgOZ8y""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/07YZf4WDAMNwqr4jfgOZ8y"",
          ""id"" : ""07YZf4WDAMNwqr4jfgOZ8y"",
          ""name"" : ""Jason Derulo"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:07YZf4WDAMNwqr4jfgOZ8y""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3EiLUeyEcA6fbRPSHkG5kb""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3EiLUeyEcA6fbRPSHkG5kb"",
          ""id"" : ""3EiLUeyEcA6fbRPSHkG5kb"",
          ""name"" : ""De La Ghetto"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3EiLUeyEcA6fbRPSHkG5kb""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/1jHSAfCHKUFx5imuezI7HE""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/1jHSAfCHKUFx5imuezI7HE"",
        ""id"" : ""1jHSAfCHKUFx5imuezI7HE"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/d4c7994426515e1167372fd827698066be74a5e2"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/2feacdd6c83bfaa4a8a2598168b81ca6ee427b0d"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/1bf61336765a8eefa68b56e1be61121badabee92"",
          ""width"" : 64
        } ],
        ""name"" : ""1, 2, 3 (feat. Jason Derulo & De La Ghetto)"",
        ""release_date"" : ""2018-02-16"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:1jHSAfCHKUFx5imuezI7HE""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/0haZhu4fFKt0Ag94kZDiz2""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/0haZhu4fFKt0Ag94kZDiz2"",
        ""id"" : ""0haZhu4fFKt0Ag94kZDiz2"",
        ""name"" : ""Sofia Reyes"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:0haZhu4fFKt0Ag94kZDiz2""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/07YZf4WDAMNwqr4jfgOZ8y""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/07YZf4WDAMNwqr4jfgOZ8y"",
        ""id"" : ""07YZf4WDAMNwqr4jfgOZ8y"",
        ""name"" : ""Jason Derulo"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:07YZf4WDAMNwqr4jfgOZ8y""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3EiLUeyEcA6fbRPSHkG5kb""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3EiLUeyEcA6fbRPSHkG5kb"",
        ""id"" : ""3EiLUeyEcA6fbRPSHkG5kb"",
        ""name"" : ""De La Ghetto"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3EiLUeyEcA6fbRPSHkG5kb""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 201526,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USWL11700238""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/4QtiVmuA88tPQiCOHZuQ5b""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/4QtiVmuA88tPQiCOHZuQ5b"",
      ""id"" : ""4QtiVmuA88tPQiCOHZuQ5b"",
      ""name"" : ""1, 2, 3 (feat. Jason Derulo & De La Ghetto)"",
      ""popularity"" : 83,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/93d0ac52041e580e2dfb7a451f1f9c6443677761?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:4QtiVmuA88tPQiCOHZuQ5b""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/7dGJo4pcD2V6oG8kP0tJRR""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/7dGJo4pcD2V6oG8kP0tJRR"",
          ""id"" : ""7dGJo4pcD2V6oG8kP0tJRR"",
          ""name"" : ""Eminem"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:7dGJo4pcD2V6oG8kP0tJRR""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/15YmkgHUd5PAgvl1XI9SL5""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/15YmkgHUd5PAgvl1XI9SL5"",
        ""id"" : ""15YmkgHUd5PAgvl1XI9SL5"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/48cde6a9fc699ddaa963e726af905a6a5dea3d7f"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/a3f1610882a405a909f81526f068ab41357e1f60"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/5ca965e910fe7e6350371e266e602a87824c70af"",
          ""width"" : 64
        } ],
        ""name"" : ""Revival"",
        ""release_date"" : ""2017-12-15"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:15YmkgHUd5PAgvl1XI9SL5""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7dGJo4pcD2V6oG8kP0tJRR""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7dGJo4pcD2V6oG8kP0tJRR"",
        ""id"" : ""7dGJo4pcD2V6oG8kP0tJRR"",
        ""name"" : ""Eminem"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7dGJo4pcD2V6oG8kP0tJRR""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/6eUKZXaKkcviH0Ku9w2n3V""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/6eUKZXaKkcviH0Ku9w2n3V"",
        ""id"" : ""6eUKZXaKkcviH0Ku9w2n3V"",
        ""name"" : ""Ed Sheeran"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:6eUKZXaKkcviH0Ku9w2n3V""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 221013,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""USUM71712944""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/5UEnHoDYpsxlfzWLZIc7LD""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/5UEnHoDYpsxlfzWLZIc7LD"",
      ""id"" : ""5UEnHoDYpsxlfzWLZIc7LD"",
      ""name"" : ""River (feat. Ed Sheeran)"",
      ""popularity"" : 95,
      ""preview_url"" : null,
      ""track_number"" : 5,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:5UEnHoDYpsxlfzWLZIc7LD""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3EXdLajEO02ziZ90P90bSW""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3EXdLajEO02ziZ90P90bSW"",
          ""id"" : ""3EXdLajEO02ziZ90P90bSW"",
          ""name"" : ""Lil Xan"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3EXdLajEO02ziZ90P90bSW""
        } ],
        ""available_markets"" : [ ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5Xlz4LJwOZ8UHM9G6zqKYi""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5Xlz4LJwOZ8UHM9G6zqKYi"",
        ""id"" : ""5Xlz4LJwOZ8UHM9G6zqKYi"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/aebdad3f3351adf0332c6ae805c8755998e6374d"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/9a1d78b9d6d0bba2be9caac6070d7528453b4b6d"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/d840e9d60b06a68ac6cd7a407e6d0181be3f2a38"",
          ""width"" : 64
        } ],
        ""name"" : ""Betrayed"",
        ""release_date"" : ""2017-07-20"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5Xlz4LJwOZ8UHM9G6zqKYi""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3EXdLajEO02ziZ90P90bSW""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3EXdLajEO02ziZ90P90bSW"",
        ""id"" : ""3EXdLajEO02ziZ90P90bSW"",
        ""name"" : ""Lil Xan"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3EXdLajEO02ziZ90P90bSW""
      } ],
      ""available_markets"" : [ ],
      ""disc_number"" : 1,
      ""duration_ms"" : 187250,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""TCADE1753080""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/3al2hpm92xE0pBalqWQHdD""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/3al2hpm92xE0pBalqWQHdD"",
      ""id"" : ""3al2hpm92xE0pBalqWQHdD"",
      ""name"" : ""Betrayed"",
      ""popularity"" : 37,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:3al2hpm92xE0pBalqWQHdD""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/586uxXMyD5ObPuzjtrzO1Q""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/586uxXMyD5ObPuzjtrzO1Q"",
          ""id"" : ""586uxXMyD5ObPuzjtrzO1Q"",
          ""name"" : ""Sofi Tukker"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:586uxXMyD5ObPuzjtrzO1Q""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7ard1FRMQE8uwlLUBD9Ve3""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7ard1FRMQE8uwlLUBD9Ve3"",
        ""id"" : ""7ard1FRMQE8uwlLUBD9Ve3"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/fd3fdfd21dd6f48bbe3b30181d88248519f89e4c"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/9716d48672458f70d88efd9ea858d1105109d27c"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/ae3cc5d00e80a9a4165720e7c42a6ff0794db293"",
          ""width"" : 64
        } ],
        ""name"" : ""Best Friend"",
        ""release_date"" : ""2017-09-12"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7ard1FRMQE8uwlLUBD9Ve3""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/586uxXMyD5ObPuzjtrzO1Q""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/586uxXMyD5ObPuzjtrzO1Q"",
        ""id"" : ""586uxXMyD5ObPuzjtrzO1Q"",
        ""name"" : ""Sofi Tukker"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:586uxXMyD5ObPuzjtrzO1Q""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4j5KBTO4tk7up54ZirNGvK""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4j5KBTO4tk7up54ZirNGvK"",
        ""id"" : ""4j5KBTO4tk7up54ZirNGvK"",
        ""name"" : ""NERVO"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4j5KBTO4tk7up54ZirNGvK""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/2x7EATekOPhFGRx3syMGEC""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/2x7EATekOPhFGRx3syMGEC"",
        ""id"" : ""2x7EATekOPhFGRx3syMGEC"",
        ""name"" : ""The Knocks"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:2x7EATekOPhFGRx3syMGEC""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/0WuYfDB2hAYzybfAd75fvb""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/0WuYfDB2hAYzybfAd75fvb"",
        ""id"" : ""0WuYfDB2hAYzybfAd75fvb"",
        ""name"" : ""Alisa Ueno"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:0WuYfDB2hAYzybfAd75fvb""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 184880,
      ""explicit"" : true,
      ""external_ids"" : {
        ""isrc"" : ""QM37X1700011""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/1Cicn7ce1xoVlq8gthE2eX""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/1Cicn7ce1xoVlq8gthE2eX"",
      ""id"" : ""1Cicn7ce1xoVlq8gthE2eX"",
      ""name"" : ""Best Friend"",
      ""popularity"" : 82,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/006c89a7df0dfb2852041247f4f5ff989f254168?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:1Cicn7ce1xoVlq8gthE2eX""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""album"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/0LyfQWJT6nXafLPZqxe9Of""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/0LyfQWJT6nXafLPZqxe9Of"",
          ""id"" : ""0LyfQWJT6nXafLPZqxe9Of"",
          ""name"" : ""Various Artists"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:0LyfQWJT6nXafLPZqxe9Of""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/7ayBZIe1FHkNv0T5xFCX6F""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/7ayBZIe1FHkNv0T5xFCX6F"",
        ""id"" : ""7ayBZIe1FHkNv0T5xFCX6F"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/32dbd227fc8cc94fb7ec9ab5bddc8e9ca72db125"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/4cf8ca7bf42c2ea957a27ef330a6744cda9a34e7"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/232e378bc0efb75d23df50c0bd7ee70d69e7bde3"",
          ""width"" : 64
        } ],
        ""name"" : ""The Greatest Showman (Original Motion Picture Soundtrack)"",
        ""release_date"" : ""2017-12-08"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:7ayBZIe1FHkNv0T5xFCX6F""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/7HV2RI2qNug4EcQqLbCAKS""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/7HV2RI2qNug4EcQqLbCAKS"",
        ""id"" : ""7HV2RI2qNug4EcQqLbCAKS"",
        ""name"" : ""Keala Settle"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:7HV2RI2qNug4EcQqLbCAKS""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/63nv0hWWDob56Rk8GlNpN8""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/63nv0hWWDob56Rk8GlNpN8"",
        ""id"" : ""63nv0hWWDob56Rk8GlNpN8"",
        ""name"" : ""The Greatest Showman Ensemble"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:63nv0hWWDob56Rk8GlNpN8""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 234706,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USAT21704622""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/45aBsnKRWUzhwbcqOJLwfe""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/45aBsnKRWUzhwbcqOJLwfe"",
      ""id"" : ""45aBsnKRWUzhwbcqOJLwfe"",
      ""name"" : ""This Is Me"",
      ""popularity"" : 91,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/4e3a8d43df85b32a1d056f2c3e6c745d6da46359?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 7,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:45aBsnKRWUzhwbcqOJLwfe""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/3mPc8WGusz2XF3Tvs3AKCR""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/3mPc8WGusz2XF3Tvs3AKCR"",
          ""id"" : ""3mPc8WGusz2XF3Tvs3AKCR"",
          ""name"" : ""Carlie Hanson"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:3mPc8WGusz2XF3Tvs3AKCR""
        } ],
        ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/5vVwIRD3zJ9nSdh6zwh6MU""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/5vVwIRD3zJ9nSdh6zwh6MU"",
        ""id"" : ""5vVwIRD3zJ9nSdh6zwh6MU"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/07d4f0d1441bccde4ff160ae57340ad99d6807d5"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/b89602062d0ba650c4f3d6e64157a2b622bed6fd"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/37453b4b76447ee29c65737aabc344dda07e89ff"",
          ""width"" : 64
        } ],
        ""name"" : ""Only One"",
        ""release_date"" : ""2017-11-24"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:5vVwIRD3zJ9nSdh6zwh6MU""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3mPc8WGusz2XF3Tvs3AKCR""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3mPc8WGusz2XF3Tvs3AKCR"",
        ""id"" : ""3mPc8WGusz2XF3Tvs3AKCR"",
        ""name"" : ""Carlie Hanson"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3mPc8WGusz2XF3Tvs3AKCR""
      } ],
      ""available_markets"" : [ ""AD"", ""AR"", ""AT"", ""AU"", ""BE"", ""BG"", ""BO"", ""BR"", ""CA"", ""CH"", ""CL"", ""CO"", ""CR"", ""CY"", ""CZ"", ""DE"", ""DK"", ""DO"", ""EC"", ""EE"", ""ES"", ""FI"", ""FR"", ""GB"", ""GR"", ""GT"", ""HK"", ""HN"", ""HU"", ""ID"", ""IE"", ""IS"", ""IT"", ""JP"", ""LI"", ""LT"", ""LU"", ""LV"", ""MC"", ""MT"", ""MX"", ""MY"", ""NI"", ""NL"", ""NO"", ""NZ"", ""PA"", ""PE"", ""PH"", ""PL"", ""PT"", ""PY"", ""SE"", ""SG"", ""SK"", ""SV"", ""TH"", ""TR"", ""TW"", ""US"", ""UY"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 190731,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""QM24S1704952""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/3HMzCcVRL5fHNF0Uv73LFV""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/3HMzCcVRL5fHNF0Uv73LFV"",
      ""id"" : ""3HMzCcVRL5fHNF0Uv73LFV"",
      ""name"" : ""Only One"",
      ""popularity"" : 83,
      ""preview_url"" : ""https://p.scdn.co/mp3-preview/15678172f6b9fa34eb479686cd4d891391210e24?cid=dff377ef2bd7472b89146b4264590a2c"",
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:3HMzCcVRL5fHNF0Uv73LFV""
    }
  }, {
    ""added_at"" : ""2018-03-03T16:04:08Z"",
    ""added_by"" : null,
    ""is_local"" : false,
    ""track"" : {
      ""album"" : {
        ""album_type"" : ""single"",
        ""artists"" : [ {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/5p7f24Rk5HkUZsaS3BLG5F""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/5p7f24Rk5HkUZsaS3BLG5F"",
          ""id"" : ""5p7f24Rk5HkUZsaS3BLG5F"",
          ""name"" : ""Hailee Steinfeld"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:5p7f24Rk5HkUZsaS3BLG5F""
        }, {
          ""external_urls"" : {
            ""spotify"" : ""https://open.spotify.com/artist/4AVFqumd2ogHFlRbKIjp1t""
          },
          ""href"" : ""https://api.spotify.com/v1/artists/4AVFqumd2ogHFlRbKIjp1t"",
          ""id"" : ""4AVFqumd2ogHFlRbKIjp1t"",
          ""name"" : ""Alesso"",
          ""type"" : ""artist"",
          ""uri"" : ""spotify:artist:4AVFqumd2ogHFlRbKIjp1t""
        } ],
        ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/album/3ggBBGRhkDVAu7pQRXRPXO""
        },
        ""href"" : ""https://api.spotify.com/v1/albums/3ggBBGRhkDVAu7pQRXRPXO"",
        ""id"" : ""3ggBBGRhkDVAu7pQRXRPXO"",
        ""images"" : [ {
          ""height"" : 640,
          ""url"" : ""https://i.scdn.co/image/e94619a42d6d2e2ce623d84740a274b0812cc517"",
          ""width"" : 640
        }, {
          ""height"" : 300,
          ""url"" : ""https://i.scdn.co/image/d0dcca5ba4d1ba71f87e9512b5374760575935a9"",
          ""width"" : 300
        }, {
          ""height"" : 64,
          ""url"" : ""https://i.scdn.co/image/dfe8f960230a3e6f9610f808445694027a4d6b3a"",
          ""width"" : 64
        } ],
        ""name"" : ""Let Me Go (with Alesso, Florida Georgia Line & watt)"",
        ""release_date"" : ""2017-09-08"",
        ""release_date_precision"" : ""day"",
        ""type"" : ""album"",
        ""uri"" : ""spotify:album:3ggBBGRhkDVAu7pQRXRPXO""
      },
      ""artists"" : [ {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/5p7f24Rk5HkUZsaS3BLG5F""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/5p7f24Rk5HkUZsaS3BLG5F"",
        ""id"" : ""5p7f24Rk5HkUZsaS3BLG5F"",
        ""name"" : ""Hailee Steinfeld"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:5p7f24Rk5HkUZsaS3BLG5F""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4AVFqumd2ogHFlRbKIjp1t""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4AVFqumd2ogHFlRbKIjp1t"",
        ""id"" : ""4AVFqumd2ogHFlRbKIjp1t"",
        ""name"" : ""Alesso"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4AVFqumd2ogHFlRbKIjp1t""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/3b8QkneNDz4JHKKKlLgYZg""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/3b8QkneNDz4JHKKKlLgYZg"",
        ""id"" : ""3b8QkneNDz4JHKKKlLgYZg"",
        ""name"" : ""Florida Georgia Line"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:3b8QkneNDz4JHKKKlLgYZg""
      }, {
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/artist/4olE3I5QU0dvSR7LIpqTXc""
        },
        ""href"" : ""https://api.spotify.com/v1/artists/4olE3I5QU0dvSR7LIpqTXc"",
        ""id"" : ""4olE3I5QU0dvSR7LIpqTXc"",
        ""name"" : ""watt"",
        ""type"" : ""artist"",
        ""uri"" : ""spotify:artist:4olE3I5QU0dvSR7LIpqTXc""
      } ],
      ""available_markets"" : [ ""CA"", ""MX"", ""US"" ],
      ""disc_number"" : 1,
      ""duration_ms"" : 174800,
      ""explicit"" : false,
      ""external_ids"" : {
        ""isrc"" : ""USUM71709685""
      },
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/track/5Gu0PDLN4YJeW75PpBSg9p""
      },
      ""href"" : ""https://api.spotify.com/v1/tracks/5Gu0PDLN4YJeW75PpBSg9p"",
      ""id"" : ""5Gu0PDLN4YJeW75PpBSg9p"",
      ""name"" : ""Let Me Go (with Alesso, Florida Georgia Line & watt)"",
      ""popularity"" : 88,
      ""preview_url"" : null,
      ""track_number"" : 1,
      ""type"" : ""track"",
      ""uri"" : ""spotify:track:5Gu0PDLN4YJeW75PpBSg9p""
    }
  } ],
  ""limit"" : 100,
  ""next"" : null,
  ""offset"" : 0,
  ""previous"" : null,
  ""total"" : 50
}";

        #endregion

        #region Search Test Data

        const string SearchTestData = @"{
  ""playlists"" : {
    ""href"" : ""https://api.spotify.com/v1/search?query=pop&type=playlist&offset=0&limit=5"",
    ""items"" : [ {
      ""collaborative"" : false,
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/user/spotify/playlist/37i9dQZF1DX50QitC6Oqtn""
      },
      ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX50QitC6Oqtn"",
      ""id"" : ""37i9dQZF1DX50QitC6Oqtn"",
      ""images"" : [ {
        ""height"" : 300,
        ""url"" : ""https://i.scdn.co/image/8c09ff067692c95fd4fb2c6e4f1df16762f90f67"",
        ""width"" : 300
      } ],
      ""name"" : ""Love Pop"",
      ""owner"" : {
        ""display_name"" : ""Spotify"",
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/user/spotify""
        },
        ""href"" : ""https://api.spotify.com/v1/users/spotify"",
        ""id"" : ""spotify"",
        ""type"" : ""user"",
        ""uri"" : ""spotify:user:spotify""
      },
      ""public"" : null,
      ""snapshot_id"" : ""Vm93ExzBBcUl9GxOVxCIUHPeNiZLDaltsmIXlsrxS0jhMJAG7A1iQSHtWBDxCIT5IISyCP9fo5c="",
      ""tracks"" : {
        ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX50QitC6Oqtn/tracks"",
        ""total"" : 52
      },
      ""type"" : ""playlist"",
      ""uri"" : ""spotify:user:spotify:playlist:37i9dQZF1DX50QitC6Oqtn""
    }, {
      ""collaborative"" : false,
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/user/spotify/playlist/37i9dQZF1DX6aTaZa0K6VA""
      },
      ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX6aTaZa0K6VA"",
      ""id"" : ""37i9dQZF1DX6aTaZa0K6VA"",
      ""images"" : [ {
        ""height"" : 300,
        ""url"" : ""https://i.scdn.co/image/701531d346106f3f51555fbd405d6139d5204612"",
        ""width"" : 300
      } ],
      ""name"" : ""Pop Up"",
      ""owner"" : {
        ""display_name"" : ""Spotify"",
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/user/spotify""
        },
        ""href"" : ""https://api.spotify.com/v1/users/spotify"",
        ""id"" : ""spotify"",
        ""type"" : ""user"",
        ""uri"" : ""spotify:user:spotify""
      },
      ""public"" : null,
      ""snapshot_id"" : ""jKdaGJCxWX8+qQjRDHkPQHISadWrDMbbHR3AAB8UHlcTC/ARKQ3N/t7M1ycGJeKPT0IlquDgA2M="",
      ""tracks"" : {
        ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX6aTaZa0K6VA/tracks"",
        ""total"" : 50
      },
      ""type"" : ""playlist"",
      ""uri"" : ""spotify:user:spotify:playlist:37i9dQZF1DX6aTaZa0K6VA""
    }, {
      ""collaborative"" : false,
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/user/spotify/playlist/37i9dQZF1DX92MLsP3K1fI""
      },
      ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX92MLsP3K1fI"",
      ""id"" : ""37i9dQZF1DX92MLsP3K1fI"",
      ""images"" : [ {
        ""height"" : 300,
        ""url"" : ""https://i.scdn.co/image/d624ea9c97b08b6a1feb2dfe46218ef08b1e0cb2"",
        ""width"" : 300
      } ],
      ""name"" : ""Pop Up"",
      ""owner"" : {
        ""display_name"" : ""Spotify"",
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/user/spotify""
        },
        ""href"" : ""https://api.spotify.com/v1/users/spotify"",
        ""id"" : ""spotify"",
        ""type"" : ""user"",
        ""uri"" : ""spotify:user:spotify""
      },
      ""public"" : null,
      ""snapshot_id"" : ""3ARH0HTPRelpxUjRAwitl6U0GKCUQajk73Z1mNVCFAhf+oUI9HPCYNw1ZjmGP15Hl7sBUX26Wn4="",
      ""tracks"" : {
        ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX92MLsP3K1fI/tracks"",
        ""total"" : 57
      },
      ""type"" : ""playlist"",
      ""uri"" : ""spotify:user:spotify:playlist:37i9dQZF1DX92MLsP3K1fI""
    }, {
      ""collaborative"" : false,
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/user/spotify/playlist/37i9dQZF1DX3WvGXE8FqYX""
      },
      ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX3WvGXE8FqYX"",
      ""id"" : ""37i9dQZF1DX3WvGXE8FqYX"",
      ""images"" : [ {
        ""height"" : 300,
        ""url"" : ""https://i.scdn.co/image/c3ddf7250d594054021cdde181b50812a86e88a8"",
        ""width"" : 300
      } ],
      ""name"" : ""Women of Pop"",
      ""owner"" : {
        ""display_name"" : ""Spotify"",
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/user/spotify""
        },
        ""href"" : ""https://api.spotify.com/v1/users/spotify"",
        ""id"" : ""spotify"",
        ""type"" : ""user"",
        ""uri"" : ""spotify:user:spotify""
      },
      ""public"" : null,
      ""snapshot_id"" : ""mhKZOOh+6WedftC5NY8zXIiKCDnMVtXmTMHCHSm2o45yeOHXs2QQEMR5vqon+AV9Cez5t8YB6lw="",
      ""tracks"" : {
        ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DX3WvGXE8FqYX/tracks"",
        ""total"" : 151
      },
      ""type"" : ""playlist"",
      ""uri"" : ""spotify:user:spotify:playlist:37i9dQZF1DX3WvGXE8FqYX""
    }, {
      ""collaborative"" : false,
      ""external_urls"" : {
        ""spotify"" : ""https://open.spotify.com/user/spotify/playlist/37i9dQZF1DWUa8ZRTfalHk""
      },
      ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DWUa8ZRTfalHk"",
      ""id"" : ""37i9dQZF1DWUa8ZRTfalHk"",
      ""images"" : [ {
        ""height"" : 300,
        ""url"" : ""https://i.scdn.co/image/a2a81143d4be14b0505228518ebca8b8246c12ed"",
        ""width"" : 300
      } ],
      ""name"" : ""Pop Rising"",
      ""owner"" : {
        ""display_name"" : ""Spotify"",
        ""external_urls"" : {
          ""spotify"" : ""https://open.spotify.com/user/spotify""
        },
        ""href"" : ""https://api.spotify.com/v1/users/spotify"",
        ""id"" : ""spotify"",
        ""type"" : ""user"",
        ""uri"" : ""spotify:user:spotify""
      },
      ""public"" : null,
      ""snapshot_id"" : ""52xiPfd9Zo8/1xhU0V2CAhpDSMlGM8td0rlYeXsLtfKqzWD8bw4XSfe+fOnjbh5xDOtoHVsKtDs="",
      ""tracks"" : {
        ""href"" : ""https://api.spotify.com/v1/users/spotify/playlists/37i9dQZF1DWUa8ZRTfalHk/tracks"",
        ""total"" : 52
      },
      ""type"" : ""playlist"",
      ""uri"" : ""spotify:user:spotify:playlist:37i9dQZF1DWUa8ZRTfalHk""
    } ],
    ""limit"" : 5,
    ""next"" : ""https://api.spotify.com/v1/search?query=pop&type=playlist&offset=5&limit=5"",
    ""offset"" : 0,
    ""previous"" : null,
    ""total"" : 789369
  }
}";

        #endregion
    }
}