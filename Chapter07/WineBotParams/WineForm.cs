using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Linq;
using System.Threading.Tasks;
using WineBotLib;

namespace WineBotParams
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
            ActiveDelegate<WineForm> shouldShowSpecial =
                wineForm => DateTime.Now.DayOfWeek == DayOfWeek.Friday;

            var prompt = new PromptAttribute
            {
                Patterns =
                    new[]
                    {
                        "Hi, I have a few questions to ask.",
                        "How are you today? I just have a few questions.",
                        "Thanks for visiting - please answer a few questions."
                    }
            };

            int numberOfBackOrderDays = 15;

            MessageDelegate<WineForm> generateMessage =
                async wineForm => 
                    await Task.FromResult(
                        new PromptAttribute(
                            $"Note: Delivery back order is {numberOfBackOrderDays} days."));
            
            return new FormBuilder<WineForm>()
                .Message(prompt)
                .Message(
                    "You can type \"help\" at any time for more info.")
                .Message(
                    "It's your lucky day - 10% discounts on Friday!",
                    shouldShowSpecial)
                .Message(
                    $"Today you get an additional %5 off.",
                    wineForm => wineForm.Rating == RatingType.Low,
                    new[] { nameof(Rating) })
                .Message(
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