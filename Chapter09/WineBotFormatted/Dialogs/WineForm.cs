using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WineBotLib;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Bot.Connector;

namespace WineBotFormatted.Dialogs
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
                .OnCompletion(WineFormCompletedAsync)
                .Build();
        }

        async Task WineFormCompletedAsync(IDialogContext context, WineForm wineResults)
        {
            List[] wines =
                await new WineApi().SearchAsync(
                    (int)wineResults.WineType,
                    (int)wineResults.Rating,
                    wineResults.InStock == StockingType.InStock,
                    "");

            var message = new StringBuilder();

            if (wines.Any())
            {
                message.AppendLine("# Top Matching Wines ");

                foreach (var wine in wines)
                    message.AppendLine($"* {wine.Name}");
            }
            else
            {
                message.Append("_Sorry, No wines found matching your criteria._");
            }

            //var reply = (context.Activity as Activity).CreateReply(message.ToString());
            //reply.TextFormat = "plain";
            //await context.PostAsync(reply);

            await context.PostAsync(message.ToString());
        }
    }
}