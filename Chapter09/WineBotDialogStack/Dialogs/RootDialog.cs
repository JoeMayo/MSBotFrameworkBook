using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using WineBotLib;
using System.Linq;

namespace WineBotDialogStack.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            string prompt = "What would you like to do?";
            var options = new[]
            {
                "Search Wine",
                "Manage Profile"
            };

            PromptDialog.Choice(context, ResumeAfterChoiceAsync, options, prompt);

            return Task.CompletedTask;
        }

        async Task ResumeAfterChoiceAsync(IDialogContext context, IAwaitable<string> result)
        {
            string choice = await result;

            if (choice.StartsWith("Search"))
                await context.Forward(
                    FormDialog.FromForm(new WineForm().BuildForm),
                    ResumeAfterWineSearchAsync,
                    context.Activity.AsMessageActivity());
            if (choice.StartsWith("Manage"))
                context.Call(new ProfileDialog(), ResumeAfterProfileAsync);
            else
                await context.PostAsync($"'{choice}' isn't implemented.");

        }

        async Task ResumeAfterWineSearchAsync(IDialogContext context, IAwaitable<WineForm> result)
        {
            WineForm wineResults = await result;

            List[] wines =
                await new WineApi().SearchAsync(
                    (int)wineResults.WineType,
                    (int)wineResults.Rating,
                    wineResults.InStock == StockingType.InStock,
                    "");

            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " +
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            await context.PostAsync(message);

            context.Wait(MessageReceivedAsync);
        }

        async Task ResumeAfterProfileAsync(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string email = await result;

                await context.PostAsync($"Your profile email is now {email}");
            }
            catch (ArgumentException ex)
            {
                await context.PostAsync($"Fail Message: {ex.Message}");
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}