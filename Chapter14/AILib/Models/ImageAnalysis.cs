namespace AILib.Models
{
    public class ImageAnalysis
    {
        public Description Description { get; set; }
        public string RequestId { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class Description
    {
        public string[] Tags { get; set; }
        public Caption[] Captions { get; set; }
    }

    public class Caption
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
    }

    public class Metadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; }
    }

}