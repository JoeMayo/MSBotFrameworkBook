using System;

namespace MusicChatbot.Models
{
    public class Genres
    {
        public Categories Categories { get; set; }
    }

    public class Categories
    {
        public string Href { get; set; }
        public GenreItem[] Items { get; set; }
        public int Limit { get; set; }
        public string Next { get; set; }
        public int Offset { get; set; }
        public object Previous { get; set; }
        public int Total { get; set; }
    }

    [Serializable]
    public class GenreItem
    {
        public string Href { get; set; }
        public Icon[] Icons { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public class Icon
    {
        public int Height { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
    }
}