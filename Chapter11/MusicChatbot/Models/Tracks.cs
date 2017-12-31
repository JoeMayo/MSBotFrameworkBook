using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MusicChatbot.Models
{
    public class TracksRoot
    {
        public string Href { get; set; }
        public TrackItem[] Items { get; set; }
        public int Limit { get; set; }
        public object Next { get; set; }
        public int Offset { get; set; }
        public object Previous { get; set; }
        public int Total { get; set; }
    }

    public class TrackItem
    {
        public DateTime Added_at { get; set; }
        public object Added_by { get; set; }
        public bool Is_local { get; set; }
        public Track Track { get; set; }
    }

    public class Track
    {
        public Album Album { get; set; }
        public Artist1[] Artists { get; set; }
        public string[] Available_markets { get; set; }
        public int Disc_number { get; set; }
        public int Duration_ms { get; set; }
        public bool _explicit { get; set; }
        public External_Ids External_ids { get; set; }
        public External_Urls2 External_urls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public int Popularity { get; set; }
        public string Preview_url { get; set; }
        public int Track_number { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }

    public class Album
    {
        public string Album_type { get; set; }
        public Artist[] Artists { get; set; }
        public string[] Available_markets { get; set; }
        public External_Urls External_urls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public TrackImage[] Images { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }

    public class External_Urls
    {
        public string Spotify { get; set; }
    }

    public class Artist
    {
        public External_Urls1 External_urls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }

    public class External_Urls1
    {
        public string Spotify { get; set; }
    }

    public class TrackImage
    {
        public int Height { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
    }

    public class External_Ids
    {
        public string Isrc { get; set; }
    }

    public class External_Urls2
    {
        public string Spotify { get; set; }
    }

    public class Artist1
    {
        public External_Urls3 External_urls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }

    public class External_Urls3
    {
        public string Spotify { get; set; }
    }
}