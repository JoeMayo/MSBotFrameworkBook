using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using System.Linq;
using System.Collections.Generic;

namespace MusicChatbot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public const string WelcomeMessage =
            "### Welcome to Music Chatbot!\n" +
            "Here are some of the things you can do:\n" +
            "* *Profile* to manage your profile information.\n" +
            "* *Browse* to find the music you like.\n" +
            "* *Playlist* for listening to favorite tunes.\n\n" +
            "Type \"Go\" to get started!";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity activity = await result;

            RootMenuItem choice;
            if (Enum.TryParse(activity.Text, out choice))
            {
                switch (choice)
                {
                    case RootMenuItem.Profile:
                        await context.Forward(new ProfileDialog(), ResumeAfterDialogAsync, activity);
                        break;
                    case RootMenuItem.Browse:
                        await context.Forward(new BrowseDialog(), ResumeAfterDialogAsync, activity);
                        break;
                    case RootMenuItem.Playlist:
                        await context.Forward(new PlaylistDialog(), ResumeAfterDialogAsync, activity);
                        break;
                    case RootMenuItem.Search:
                        await context.Forward(new SearchDialog(), ResumeAfterDialogAsync, activity);
                        break;
                    default:
                        await context.PostAsync(WelcomeMessage);
                        context.Wait(MessageReceivedAsync);
                        break;
                }
            }
            else
            {
                await ShowMenuAsync(context);
            }
        }

        async Task ResumeAfterDialogAsync(IDialogContext context, IAwaitable<object> result)
        {
            await ShowMenuAsync(context);
        }

        async Task ShowMenuAsync(IDialogContext context)
        {
            var options = Enum.GetValues(typeof(RootMenuItem)).Cast<RootMenuItem>().ToArray();

            var reply = (context.Activity as Activity).CreateReply("What would you like to do?");

            reply.SuggestedActions = new SuggestedActions
            {
                To = new List<string> { context.Activity.From.Id },
                Actions =
                    (from option in options
                     let text = option.ToString()
                     select new CardAction
                     {
                         Title = text,
                         Type = ActionTypes.ImBack,
                         Value = text,
                         DisplayText = text,
                         Text = text
                     })
                    .ToList()
            };

            await
                new ConnectorClient(new Uri(reply.ServiceUrl))
                    .Conversations
                    .SendToConversationAsync(reply);

            context.Wait(MessageReceivedAsync);
        }
    }
}