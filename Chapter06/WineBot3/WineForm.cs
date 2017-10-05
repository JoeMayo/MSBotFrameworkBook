using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WineBot3
{
    [Serializable]
    [Template(TemplateUsage.String, "What {&} would you like to enter?")]
    [Template(TemplateUsage.NotUnderstood,
        "Sorry, I didn't get that.",
        "Please try again.",
        "My apologies, I didn't understand '{0}'.",
        "Excuse me, I didn't quite get that.",
        "Sorry, but I'm a chatbot and don't know what '{0}' means.")]
    public class WineForm
    {
        [Describe(Description="Type of Wine")]
        [Prompt(
            "Which {&} would you like? (current value: {}) {||}", 
            ChoiceStyle = ChoiceStyleOptions.PerLine)]
        public WineType WineType { get; set; }

        [Numeric(1, 100)]
        [Prompt(
            "Please enter a minimum rating (from 1 to 100).",
            "What rating, 1 to 100, would you like to search for?",
            "Minimum {&} (selected {&WineType}: {WineType}, current rating: {:000})")]
        public int Rating { get; set; }

        [Optional]
        public StockingType InStock { get; set; }

        [Pattern(".*")]
        [Template(
            TemplateUsage.StringHelp,
            "Additional words to filter search {?{0}}")]
        public string SearchTerms { get; set; }

        [Pattern(@".+@.+\..+")]
        public string EmailAddress { get; set; }

        public IForm<WineForm> BuildForm()
        {
            return new FormBuilder<WineForm>()
                .Message(
                    "I have a few questions on your wine search. " +
                    "You can type \"help\" at any time for more info.")
                .OnCompletion(DoSearch)
                .Build();
        }

        async Task DoSearch(IDialogContext context, WineForm wineInfo)
        {
            List[] wines =
                await new WineApi().SearchAsync(
                    (int)wineInfo.WineType,
                    wineInfo.Rating,
                    wineInfo.InStock == StockingType.InStock,
                    wineInfo.SearchTerms);

            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " +
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            await context.PostAsync(message);
        }
    }
}