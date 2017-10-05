using System;

namespace AILib.Models
{

    public class NewsArticles
    {
        public string _type { get; set; }
        public string ReadLink { get; set; }
        public int TotalEstimatedMatches { get; set; }
        public Sort[] Sort { get; set; }
        public Value[] Value { get; set; }
    }

    public class Sort
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public string Url { get; set; }
    }

    public class Value
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public Image Image { get; set; }
        public string Description { get; set; }
        public About[] About { get; set; }
        public Provider[] Provider { get; set; }
        public DateTime DatePublished { get; set; }
        public string Category { get; set; }
        public Clusteredarticle[] ClusteredArticles { get; set; }
    }

    public class Image
    {
        public Thumbnail Thumbnail { get; set; }
    }

    public class Thumbnail
    {
        public string ContentUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class About
    {
        public string ReadLink { get; set; }
        public string Name { get; set; }
    }

    public class Provider
    {
        public string _type { get; set; }
        public string Name { get; set; }
    }

    public class Clusteredarticle
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public About1[] About { get; set; }
        public Provider1[] Provider { get; set; }
        public DateTime DatePublished { get; set; }
        public string Category { get; set; }
    }

    public class About1
    {
        public string ReadLink { get; set; }
        public string Name { get; set; }
    }

    public class Provider1
    {
        public string _type { get; set; }
        public string Name { get; set; }
    }
}