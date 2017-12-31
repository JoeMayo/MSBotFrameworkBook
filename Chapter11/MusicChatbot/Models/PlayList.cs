using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MusicChatbot.Models
{
    public class PlayLists
    {
        public Playlists Playlists { get; set; }
    }

    public class Playlists
    {
        public string Href { get; set; }
        public PlaylistItem[] Items { get; set; }
        public int Limit { get; set; }
        public string Next { get; set; }
        public int Offset { get; set; }
        public object Previous { get; set; }
        public int Total { get; set; }
    }

    public class PlaylistItem
    {
        public bool Collaborative { get; set; }
        public TrackExternal_Urls External_urls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public PlaylistImage[] Images { get; set; }
        public string Name { get; set; }
        public Owner Owner { get; set; }
        public object _public { get; set; }
        public string Snapshot_id { get; set; }
        public PlaylistTracks Tracks { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }

    public class TrackExternal_Urls
    {
        public string Spotify { get; set; }
    }

    public class Owner
    {
        public TrackExternal_Urls1 External_urls { get; set; }
        public string Href { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }

    public class TrackExternal_Urls1
    {
        public string Spotify { get; set; }
    }

    public class PlaylistTracks
    {
        public string Href { get; set; }
        public int Total { get; set; }
    }

    public class PlaylistImage
    {
        public int Height { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
    }
}