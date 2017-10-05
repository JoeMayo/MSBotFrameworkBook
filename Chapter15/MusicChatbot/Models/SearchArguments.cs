namespace MusicChatbot.Models
{
    public class SearchArguments
    {
        public string Query { get; set; }
        public string MaxItems { get; set; }
        public string Filters { get; set; }
        public string Source { get; set; }
    }
}