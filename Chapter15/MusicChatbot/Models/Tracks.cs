using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MusicChatbot.Models
{

    public class TracksRoot
    {
        public Tracks Tracks { get; set; }
        public string Culture { get; set; }
    }

    public class Tracks
    {
        public Item[] Items { get; set; }
        public string ContinuationToken { get; set; }
        public int TotalItemCount { get; set; }
    }

    public class Item
    {
        public DateTime ReleaseDate { get; set; }
        public string Duration { get; set; }
        public int TrackNumber { get; set; }
        public bool IsExplicit { get; set; }
        public string[] Genres { get; set; }
        public string[] Subgenres { get; set; }
        public string[] Rights { get; set; }
        public Album Album { get; set; }
        public Artist0[] Artists { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public Otherids OtherIds { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }
    }

    public class Album
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }
    }

    public class Otherids
    {
        public string musicisrc { get; set; }
    }


    public class Artist0
    {
        public string Role { get; set; }
        public Artist1 Artist { get; set; }
    }

    public class Artist1
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }
    }
}