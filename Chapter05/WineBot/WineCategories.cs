using System;

namespace WineBot
{
    public class WineCategories
    {
        public Status Status { get; set; }
        public Category[] Categories { get; set; }
    }

    public class Category
    {
        public string Description { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public Refinement[] Refinements { get; set; }
    }

    [Serializable]
    public class Refinement
    {
        public string Description { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}