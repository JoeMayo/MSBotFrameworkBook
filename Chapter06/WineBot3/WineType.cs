using Microsoft.Bot.Builder.FormFlow;

namespace WineBot3
{
    public enum WineType
    {
        None = 0,
        RedWine = 124,
        WhiteWine = 125,

        [Terms("[S|s|C|c]hamp.*", MaxPhrase=2)]
        ChampagneAndSparkling = 123,

        RoseWine = 126,

        [Terms(
            "desert", "shery", "prt",
            "dessert", "sherry", "port",
            "Dessert, Sherry, and Port")]
        [Describe("Dessert, Sherry, and Port")]
        DessertSherryAndPort = 128,

        Sake = 134
    }
}