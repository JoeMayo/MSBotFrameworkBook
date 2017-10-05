namespace WineBot
{
    public class WineProducts
    {
        public Status Status { get; set; }
        public Products Products { get; set; }
    }

    public class Products
    {
        public List[] List { get; set; }
        public int Offset { get; set; }
        public int Total { get; set; }
        public string Url { get; set; }
    }

    public class List
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public Appellation Appellation { get; set; }
        public Label[] Labels { get; set; }
        public string Type { get; set; }
        public Varietal Varietal { get; set; }
        public Vineyard Vineyard { get; set; }
        public string Vintage { get; set; }
        public Community Community { get; set; }
        public string Description { get; set; }
        public Geolocation1 GeoLocation { get; set; }
        public float PriceMax { get; set; }
        public float PriceMin { get; set; }
        public float PriceRetail { get; set; }
        public Productattribute[] ProductAttributes { get; set; }
        public Ratings Ratings { get; set; }
        public object Retail { get; set; }
        public Vintages Vintages { get; set; }
    }

    public class Appellation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public Region Region { get; set; }
    }

    public class Region
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public object Area { get; set; }
    }

    public class Varietal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public Winetype WineType { get; set; }
    }

    public class Winetype
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class Vineyard
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public Geolocation GeoLocation { get; set; }
    }

    public class Geolocation
    {
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public string Url { get; set; }
    }

    public class Community
    {
        public Reviews Reviews { get; set; }
        public string Url { get; set; }
    }

    public class Reviews
    {
        public int HighestScore { get; set; }
        public object[] List { get; set; }
        public string Url { get; set; }
    }

    public class Geolocation1
    {
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public string Url { get; set; }
    }

    public class Ratings
    {
        public int HighestScore { get; set; }
        public object[] List { get; set; }
    }

    public class Vintages
    {
        public object[] List { get; set; }
    }

    public class Label
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class Productattribute
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }
}