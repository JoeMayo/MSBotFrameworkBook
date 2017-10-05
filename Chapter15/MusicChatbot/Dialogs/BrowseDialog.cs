using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using MusicChatbot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicChatbot.Dialogs
{
    [Serializable]
    public class BrowseDialog : IDialog<object>
    {
        const string DoneCommand = "Done";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            List<string> genres = new GrooveService().GetGenres();
            genres.Add("Done");

            string promptMessage = "Which music category?";
            string retryMessage = "I don't know about that category, please select an item in the list.";

            var promptOptions =
                new PromptOptions<string>(
                    prompt: promptMessage,
                    retry: retryMessage,
                    options: genres,
                    speak: promptMessage,
                    retrySpeak: retryMessage);

            PromptDialog.Choice(
                context: context, 
                resume: ResumeAfterGenreAsync,
                promptOptions: promptOptions);

            return Task.CompletedTask;
        }

        async Task ResumeAfterGenreAsync(IDialogContext context, IAwaitable<string> result)
        {
            string genre = await result;

            if (genre == DoneCommand)
            {
                context.Done(this);
                return;
            }

            string message = $"Browsing Top 5 Tracks in {genre} genre";
            var reply = (context.Activity as Activity)
                .CreateReply($"## {message}");
            reply.Speak = message;
            reply.InputHint = InputHints.AcceptingInput;

            List<HeroCard> cards = GetHeroCardsForTracks(genre);
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

        List<HeroCard> GetHeroCardsForTracks(string genre)
        {
            List<Item> tracks = new GrooveService().GetTracks(genre);

            var cards =
                (from track in tracks
                 let artists =
                     string.Join(", ",
                        from artist in track.Artists
                        select artist.Artist.Name)
                 select new HeroCard
                 {
                     Title = track.Name,
                     Subtitle = artists,
                     Images = new List<CardImage>
                     {
                         new CardImage
                         {
                             Alt = track.Name,
                             Tap = BuildBuyCardAction(track),
                             Url = track.ImageUrl
                         }
                     },
                     Buttons = new List<CardAction>
                     {
                         BuildBuyCardAction(track)
                     }
                 })
                .ToList();
            return cards;
        }

        CardAction BuildBuyCardAction(Item track)
        {
            return new CardAction
            {
                Type = ActionTypes.OpenUrl,
                Title = "Buy",
                Value = track.Link
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