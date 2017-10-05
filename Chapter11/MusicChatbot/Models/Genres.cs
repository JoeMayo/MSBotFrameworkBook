namespace MusicChatbot.Models
{

    public class Genres
    {
        public Cataloggenre[] CatalogGenres { get; set; }
        public string Culture { get; set; }
    }

    public class Cataloggenre
    {
        public string ParentName { get; set; }
        public bool HasEditorialPlaylists { get; set; }
        public string Name { get; set; }
    }
}