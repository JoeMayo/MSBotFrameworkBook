using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;

namespace WineBot2
{
    [Serializable]
    public class WineForm
    {
        public WineType WineType { get; set; }
        public RatingType Rating { get; set; }
        public StockingType InStock { get; set; }

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
                    (int)WineType, 
                    (int)Rating, 
                    InStock == StockingType.InStock, 
                    "");

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