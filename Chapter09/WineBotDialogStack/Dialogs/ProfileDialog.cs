using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading.Tasks;

namespace WineBotDialogStack.Dialogs
{
    [Serializable]
    public class ProfileDialog : IDialog<string>
    {
        public Task StartAsync(IDialogContext context)
        {
            string prompt = "What would you like to do?";
            var options = new[]
            {
                "Change Email",
                "Reset",
                "Fail",
            };

            PromptDialog.Choice(context, MessageReceivedAsync, options, prompt);

            return Task.CompletedTask;
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            string choice = await result;

            switch (choice)
            {
                case "Change Email":
                    string prompt = "What is your email address?";
                    PromptDialog.Text(context, ResumeAfterEmailAsync, prompt);
                    break;
                case "Fail":
                    context.Fail(new ArgumentException("Testing Fail."));
                    break;
                case "Reset":
                    context.Reset();
                    break;
                default:
                    await context.PostAsync($"'{choice}' isn't implemented.");
                    break;
            }
        }

        async Task ResumeAfterEmailAsync(IDialogContext context, IAwaitable<string> result)
        {
            string email = await result;
            context.Done(email);
        }
    }
}