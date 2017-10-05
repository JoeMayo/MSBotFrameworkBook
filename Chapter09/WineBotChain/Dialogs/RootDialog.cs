using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WineBotLib;
using static Microsoft.Bot.Builder.Dialogs.Chain;

namespace WineBotChain.Dialogs
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
            string prompt = "Which chain demo?";
            var options = new[]
            {
                "From",
                "LINQ",
                "Loop",
                "Switch"
            };

            PromptDialog.Choice(context, ResumeAfterChoiceAsync, options, prompt);

            return Task.CompletedTask;
        }

        async Task ResumeAfterChoiceAsync(IDialogContext context, IAwaitable<string> result)
        {
            string choice = await result;

            switch (choice)
            {
                case "From":
                    await DoChainFromAsync(context);
                    break;
                case "LINQ":
                    await DoChainLinqAsync(context);
                    break;
                case "Loop":
                    await DoChainLoopAsync(context);
                    break;
                case "Switch":
                    DoChainSwitch(context);
                    break;
                default:
                    await context.PostAsync($"'{choice}' isn't implemented.");
                    break;
            }
        }

        async Task<string> ProcessWineResultsAsync(WineForm wineResult)
        {
            List[] wines =
                await new WineApi().SearchAsync(
                    (int)wineResult.WineType,
                    (int)wineResult.Rating,
                    wineResult.InStock == StockingType.InStock,
                    "");

            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " +
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            return message;
        }

        async Task ResumeAfterWineFormAsync(IDialogContext context, IAwaitable<WineForm> result)
        {
            WineForm wineResult = await result;

            string message = await ProcessWineResultsAsync(wineResult);

            await context.PostAsync(message);

            context.Wait(MessageReceivedAsync);
        }

        async Task DoChainFromAsync(IDialogContext context)
        {
            IDialog<WineForm> chain =
                Chain.From(() => FormDialog.FromForm<WineForm>(new WineForm().BuildForm));

            await context.Forward(
                chain, 
                ResumeAfterWineFormAsync,
                context.Activity.AsMessageActivity());
        }

        async Task DoChainLinqAsync(IDialogContext context)
        {
            var chain =
                from wineForm in FormDialog.FromForm(new WineForm().BuildForm)
                from searchTerm in new PromptDialog.PromptString("Search Terms?", "Search Terms?", 1)
                where wineForm.WineType.ToString().Contains("Wine")
                select Task.Run(() => ProcessWineResultsAsync(wineForm)).Result;

            await context.Forward(
                chain,
                ResumeAfterChainLinqAsync,
                context.Activity.AsMessageActivity());
        }

        async Task ResumeAfterChainLinqAsync(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string response = await result;
                await context.PostAsync(response);
                context.Wait(MessageReceivedAsync);
            }
            catch (WhereCanceledException wce)
            {
                await context.PostAsync($"Where cancelled: {wce.Message}");
            }
        }

        async Task DoChainLoopAsync(IDialogContext context)
        {
            IDialog<WineForm> chain =
                Chain.From(() => FormDialog.FromForm(new WineForm().BuildForm, FormOptions.PromptInStart))
                     .Do(async (ctx, result) =>
                     {
                         try
                         {
                             WineForm wineResult = await result;
                             string message = await ProcessWineResultsAsync(wineResult);
                             await ctx.PostAsync(message);
                         }
                         catch (FormCanceledException fce)
                         {
                             await ctx.PostAsync($"Cancelled: {fce.Message}");
                         }
                     })
                     .Loop();

            await context.Forward(
                chain,
                ResumeAfterWineFormAsync,
                context.Activity.AsMessageActivity());
        }

        void DoChainSwitch(IDialogContext context)
        {
            string prompt = "What would you like to do?";
            var options = new[]
            {
                "Search Wine",
                "Manage Profile"
            };

            PromptDialog.Choice(context, ResumeAfterMenuAsync, options, prompt);
        }

        async Task ResumeAfterMenuAsync(IDialogContext context, IAwaitable<string> result)
        {
            IDialog<string> chain =
                Chain
                    .PostToChain()
                    .Select(msg => msg.Text)
                    .Switch(
                        new RegexCase<IDialog<string>>(new Regex("^Search", RegexOptions.IgnoreCase),
                            (reContext, choice) =>
                            {
                                return DoSearchCase();
                            }),
                        new Case<string, IDialog<string>>(choice => choice.Contains("Manage"),
                            (manageContext, txt) =>
                            {
                                manageContext.PostAsync("What is your name?");
                                return DoManageCase();
                            }),
                        new DefaultCase<string, IDialog<string>>(
                            (defaultCtx, txt) =>
                            {
                                return Chain.Return("Not Implemented.");
                            })
            )
            .Unwrap()
            .PostToUser();

            await context.Forward(
                chain,
                ResumeAfterSwitchAsync,
                context.Activity.AsMessageActivity());
        }

        IDialog<string> DoSearchCase()
        {
            return
                Chain
                    .From(() => FormDialog.FromForm(new WineForm().BuildForm, FormOptions.PromptInStart))
                    .ContinueWith(async (ctx, res) =>
                    {
                        WineForm wineResult = await res;
                        string message = await ProcessWineResultsAsync(wineResult);
                        return Chain.Return(message);
                    });
        }

        IDialog<string> DoManageCase()
        {
            return
                Chain
                    .PostToChain()
                    .Select(msg => $"Hi {msg.Text}'! What is your email?")
                    .PostToUser()
                    .WaitToBot()
                    .Then(async (ctx, res) => (await res).Text)
                    .Select(msg => $"Thanks - your email, {msg}, is updated");
        }

        async Task ResumeAfterSwitchAsync(IDialogContext context, IAwaitable<string> result)
        {
            string message = await result;
            context.Done(message);
        }
    }
}