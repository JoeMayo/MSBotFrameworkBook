using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Linq;
using System.Threading.Tasks;
using WineBotLib;

namespace WineBotConfirm
{
    [Serializable]
    public class WineForm
    {
        public WineType WineType { get; set; }
        [Optional]
        public RatingType Rating { get; set; }
        public StockingType InStock { get; set; }

        public IForm<WineForm> BuildForm()
        {
            ActiveDelegate<WineForm> shouldShowContest =
                wineForm => DateTime.Now.DayOfWeek == DayOfWeek.Friday;

            var prompt = new PromptAttribute
            {
                Patterns =
                    new[]
                    {
                        "Hi, May I ask a few questions?",
                        "How are you today? Can I ask a few questions?",
                        "Thanks for visiting - would you answer a few questions?"
                    }
            };

            int numberOfBackOrderDays = 15;

            MessageDelegate<WineForm> generateMessage =
                async wineForm =>
                    await Task.FromResult(
                        new PromptAttribute(
                            $"Delivery back order is {numberOfBackOrderDays} days. Are you sure?"));

            return new FormBuilder<WineForm>()
                .Confirm(prompt)
                .Confirm(
                    "You can type \"help\" at any time for more info. Would you like to proceed?")
                .Confirm(
                    "Would you like to enter a contest for free bottle of Wine?",
                    shouldShowContest)
                .Confirm(
                    $"Low rated wines are limited in stock - are you sure?",
                    wineForm => wineForm.Rating == RatingType.Low,
                    new[] { nameof(Rating) })
                .Confirm(
                    generateMessage,
                    wineForm => wineForm.InStock == StockingType.OutOfStock)
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