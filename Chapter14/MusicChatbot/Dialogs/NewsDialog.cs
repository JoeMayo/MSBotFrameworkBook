using AILib;
using AILib.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using MusicChatbot.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicChatbot.Dialogs
{
    [Serializable]
    public class NewsDialog : IDialog<object>
    {
        const string DoneCommand = "Done";
        readonly string artistName;

        public NewsDialog(string artistName)
        {
            this.artistName = artistName;
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as IMessageActivity;

            if (activity.Text == DoneCommand)
            {
                context.Done(this);
                return;
            }

            NewsArticles articles = await new CognitiveService().SearchForNewsAsync(artistName);

            var reply = (context.Activity as Activity)
                .CreateReply($"## Reading news about {artistName}");

            List<ThumbnailCard> cards = GetHeroCardsForArticles(articles);
            cards.ForEach(card =>
                reply.Attachments.Add(card.ToAttachment()));

            ThumbnailCard doneCard = GetThumbnailCardForDone();
            reply.Attachments.Add(doneCard.ToAttachment());

            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            await
                new ConnectorClient(new Uri(reply.ServiceUrl))
                    .Conversations
                    .SendToConversationAsync(reply);

            context.Wait(MessageReceivedAsync);
        }

        List<ThumbnailCard> GetHeroCardsForArticles(NewsArticles articles)
        {
            var cards =
                (from article in articles.Value
                 select new ThumbnailCard
                 {
                     Title = article.Name,
                     Subtitle = "About: " + article.About.FirstOrDefault()?.Name,
                     Text = article.Description,
                     Images = new List<CardImage>
                     {
                         new CardImage
                         {
                             Alt = article.Description,
                             Tap = BuildViewCardAction(article.Url),
                             Url = article.Image.Thumbnail.ContentUrl
                         }
                     },
                     Buttons = new List<CardAction>
                     {
                         BuildViewCardAction(article.Url)
                     }
                 })
                .ToList();
            return cards;
        }

        CardAction BuildViewCardAction(string url)
        {
            return new CardAction
            {
                Type = ActionTypes.OpenUrl,
                Title = "Read",
                Value = url
            };
        }

        ThumbnailCard GetThumbnailCardForDone()
        {
            return new ThumbnailCard
            {
                Title = DoneCommand,
                Subtitle = "Click/Tap to exit",
                Images = new List<CardImage>
                {
                    new CardImage
                    {
                        Alt = "Smile",
                        Tap = BuildDoneCardAction(),
                        Url = new FileService().GetBinaryUrl("Smile.png")
                    }
                },
                Buttons = new List<CardAction>
                {
                    BuildDoneCardAction()
                }
            };
        }

        CardAction BuildDoneCardAction()
        {
            return new CardAction
            {
                Type = ActionTypes.PostBack,
                Title = DoneCommand,
                Value = DoneCommand
            };
        }
    }
}